namespace LinguaForge.Infrastructure.Configuration
{
    /// <summary>
    /// Login brute-force protection settings (bound from <c>Auth:Login</c>). Defaults are
    /// conservative: 5 failures in a 60s window trips a 5-minute lockout for that IP+email. (LF-105)
    /// </summary>
    public class LoginThrottleOptions
    {
        public const string SectionName = "Auth:Login";

        /// <summary>Failed attempts allowed within <see cref="WindowSeconds"/> before lockout.</summary>
        public int PermitLimit { get; set; } = 5;

        /// <summary>Rolling window (seconds) over which failures are counted.</summary>
        public int WindowSeconds { get; set; } = 60;

        /// <summary>How long (seconds) the IP+email pair stays locked out once the limit trips.</summary>
        public int LockoutSeconds { get; set; } = 300;
    }
}
