using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;
using LinguaForge.Domain.Entities;
using LinguaForge.Infrastructure.Configuration;
using LinguaForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LinguaForge.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly LinguaForgeDbContext _dbContext;
        private readonly JwtOptions _jwtOptions;

        public AuthService(LinguaForgeDbContext dbContext, IOptions<JwtOptions> jwtOptions)
        {
            _dbContext = dbContext;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task<AuthResponseDto> RegisterAsync(AuthRegisterRequestDto request, CancellationToken cancellationToken = default)
        {
            var email = NormalizeEmail(request.Email);
            if (await _dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            var user = new User
            {
                UserName = string.IsNullOrWhiteSpace(request.UserName) ? email.Split('@')[0] : request.UserName.Trim(),
                Email = email
            };

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = HashPassword(request.Password, salt);

            _dbContext.Users.Add(user);
            _dbContext.Add(new AuthCredential
            {
                UserId = user.Id,
                PasswordHash = Convert.ToBase64String(hash),
                PasswordSalt = Convert.ToBase64String(salt),
                CreatedAtUtc = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return await IssueAuthResponseAsync(user, cancellationToken);
        }

        public async Task<AuthResponseDto> LoginAsync(AuthLoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var email = NormalizeEmail(request.Email);
            var user = await _dbContext.Users
                .Include(x => x.AuthCredential)
                .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

            if (user?.AuthCredential is null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var salt = Convert.FromBase64String(user.AuthCredential.PasswordSalt);
            var expectedHash = Convert.FromBase64String(user.AuthCredential.PasswordHash);
            var actualHash = HashPassword(request.Password, salt);

            if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            return await IssueAuthResponseAsync(user, cancellationToken);
        }

        public async Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            var hash = HashToken(refreshToken);
            var stored = await _dbContext.RefreshTokens
                .Include(x => x.User)
                .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

            if (stored is null || stored.User is null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            if (!stored.IsActive)
            {
                // A revoked-but-presented token means the chain may be compromised:
                // revoke every active token for the user as a defensive measure.
                if (stored.RevokedAtUtc is not null)
                {
                    await RevokeAllActiveAsync(stored.UserId, cancellationToken);
                }
                throw new UnauthorizedAccessException("Refresh token is no longer valid.");
            }

            // Rotate: revoke the presented token and issue a fresh pair.
            var response = await IssueAuthResponseAsync(stored.User, cancellationToken, replaces: stored);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return response;
        }

        public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var hash = HashToken(refreshToken);
            var stored = await _dbContext.RefreshTokens
                .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

            if (stored is not null && stored.RevokedAtUtc is null)
            {
                stored.RevokedAtUtc = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<AuthUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .Where(x => x.Id == userId)
                .Select(x => new AuthUserDto
                {
                    Id = x.Id,
                    UserName = x.UserName,
                    Email = x.Email
                })
                .SingleOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Builds a short-lived access JWT and a new rotating refresh token. When
        /// <paramref name="replaces"/> is supplied (refresh flow), the old token is revoked
        /// and linked to the new one. The caller is responsible for the surrounding save
        /// in the refresh path; register/login save here.
        /// </summary>
        private async Task<AuthResponseDto> IssueAuthResponseAsync(
            User user, CancellationToken cancellationToken, RefreshToken? replaces = null)
        {
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            var (refreshRaw, refreshEntity) = CreateRefreshToken(user.Id);
            _dbContext.RefreshTokens.Add(refreshEntity);

            if (replaces is not null)
            {
                replaces.RevokedAtUtc = DateTime.UtcNow;
                replaces.ReplacedByTokenId = refreshEntity.Id;
            }
            else
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAtUtc = expiresAtUtc,
                RefreshToken = refreshRaw,
                RefreshTokenExpiresAtUtc = refreshEntity.ExpiresAtUtc,
                User = new AuthUserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email
                }
            };
        }

        private (string raw, RefreshToken entity) CreateRefreshToken(Guid userId)
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var entity = new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(raw),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
                CreatedAtUtc = DateTime.UtcNow
            };
            return (raw, entity);
        }

        private async Task RevokeAllActiveAsync(Guid userId, CancellationToken cancellationToken)
        {
            var active = await _dbContext.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var t in active)
            {
                t.RevokedAtUtc = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static string HashToken(string raw)
            => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static byte[] HashPassword(string password, byte[] salt)
            => Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    }
}
