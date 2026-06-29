namespace LinguaForge.Infrastructure.Configuration
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "LinguaForge.Api";
        public string Audience { get; set; } = "LinguaForge.Client";

        /// <summary>Access-token lifetime. Short by design; refresh tokens cover longevity.</summary>
        public int ExpiryMinutes { get; set; } = 15;

        /// <summary>Refresh-token lifetime in days.</summary>
        public int RefreshTokenDays { get; set; } = 30;
    }
}
