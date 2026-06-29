namespace LinguaForge.Application.DTOs
{
    public class AuthResponseDto
    {
        /// <summary>Short-lived access token (JWT).</summary>
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>Long-lived rotating refresh token (raw value, returned once).</summary>
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAtUtc { get; set; }

        public AuthUserDto User { get; set; } = new();
    }
}
