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
            return BuildResponse(user);
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

            return BuildResponse(user);
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

        private AuthResponseDto BuildResponse(User user)
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

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAtUtc = expiresAtUtc,
                User = new AuthUserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email
                }
            };
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static byte[] HashPassword(string password, byte[] salt)
            => Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    }
}
