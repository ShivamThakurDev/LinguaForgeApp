namespace LinguaForge.Application.Interface
{
    /// <summary>
    /// Tracks failed sign-in attempts per caller (IP + email) and temporarily locks the pair
    /// out after too many failures, to blunt brute-force / credential-stuffing. State is
    /// in-process; the caller supplies an opaque partition key. (LF-105)
    /// </summary>
    public interface ILoginThrottle
    {
        /// <summary>
        /// True when the key is currently locked out. <paramref name="retryAfter"/> is the time
        /// remaining on the lockout (only meaningful when the return value is true).
        /// </summary>
        bool IsLocked(string key, out TimeSpan retryAfter);

        /// <summary>Records a failed sign-in; trips the lockout once the failure threshold is hit.</summary>
        void RegisterFailure(string key);

        /// <summary>Clears the failure counter for the key after a successful sign-in.</summary>
        void RegisterSuccess(string key);
    }
}
