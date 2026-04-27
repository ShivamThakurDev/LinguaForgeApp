namespace LinguaForge.Infrastructure.Configuration
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "LinguaForge.Api";
        public string Audience { get; set; } = "LinguaForge.Client";
        public int ExpiryMinutes { get; set; } = 180;
    }
}
