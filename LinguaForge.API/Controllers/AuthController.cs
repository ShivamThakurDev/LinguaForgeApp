using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;
using LinguaForge.Application.UseCaseServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinguaForge.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // The refresh token lives in an HttpOnly cookie scoped to the auth path, so it is sent
        // only on /api/v1/auth calls and is unreadable from JavaScript. (LF-103)
        private const string RefreshCookieName = "refreshToken";
        private const string RefreshCookiePath = "/api/v1/auth";

        private readonly AuthAppService _authAppService;
        private readonly ILoginThrottle _loginThrottle;

        public AuthController(AuthAppService authAppService, ILoginThrottle loginThrottle)
        {
            _authAppService = authAppService;
            _loginThrottle = loginThrottle;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] AuthRegisterRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "email and password are required." });
            }

            try
            {
                var response = await _authAppService.RegisterAsync(request, cancellationToken);
                SetRefreshCookie(response);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] AuthLoginRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "email and password are required." });
            }

            // Brute-force / credential-stuffing guard: block repeated failures for this IP+email.
            // The response stays generic so it never reveals whether the account exists. (LF-105)
            var throttleKey = ThrottleKey(request.Email);
            if (_loginThrottle.IsLocked(throttleKey, out var retryAfter))
            {
                return TooManyLoginAttempts(retryAfter);
            }

            try
            {
                var response = await _authAppService.LoginAsync(request, cancellationToken);
                _loginThrottle.RegisterSuccess(throttleKey);
                SetRefreshCookie(response);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _loginThrottle.RegisterFailure(throttleKey);
                return Unauthorized(new { error = ex.Message });
            }
        }

        // Anonymous: the access token is expired by the time this is called. The credential is the
        // refresh token, read from the HttpOnly cookie — never the request body. (LF-103)
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[RefreshCookieName];
            try
            {
                var response = await _authAppService.RefreshAsync(refreshToken ?? string.Empty, cancellationToken);
                SetRefreshCookie(response);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Rotation/reuse detection may have invalidated the chain; drop the stale cookie.
                ClearRefreshCookie();
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[RefreshCookieName];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authAppService.LogoutAsync(refreshToken, cancellationToken);
            }

            ClearRefreshCookie();
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            var user = await _authAppService.GetUserAsync(userId, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        /// <summary>Partition login throttling by caller IP and normalized email.</summary>
        private string ThrottleKey(string email)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"{ip}|{email.Trim().ToLowerInvariant()}";
        }

        private IActionResult TooManyLoginAttempts(TimeSpan retryAfter)
        {
            Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { error = "Too many attempts. Please try again later." });
        }

        private void SetRefreshCookie(AuthResponseDto response)
        {
            Response.Cookies.Append(RefreshCookieName, response.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,                       // sent only over HTTPS (see DEPLOYMENT.md)
                SameSite = SameSiteMode.Strict,
                Path = RefreshCookiePath,
                Expires = response.RefreshTokenExpiresAtUtc,
                IsEssential = true                   // auth cookie: exempt from consent gating
            });
        }

        private void ClearRefreshCookie()
        {
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = RefreshCookiePath
            });
        }
    }
}
