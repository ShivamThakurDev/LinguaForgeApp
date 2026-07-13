using System.Text.Json.Serialization;

namespace LinguaForge.Application.DTOs
{
    public class AuthResponseDto
    {
        /// <summary>Short-lived access token (JWT).</summary>
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>
        /// Long-lived rotating refresh token (raw value). Carried from the service to the API
        /// layer, which delivers it as an <c>HttpOnly; Secure; SameSite=Strict</c> cookie — it is
        /// <see cref="JsonIgnoreAttribute"/>d so it is never serialized into the JSON body where
        /// JavaScript (and therefore XSS) could read it. (LF-103)
        /// </summary>
        [JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTime RefreshTokenExpiresAtUtc { get; set; }

        public AuthUserDto User { get; set; } = new();
    }
}
