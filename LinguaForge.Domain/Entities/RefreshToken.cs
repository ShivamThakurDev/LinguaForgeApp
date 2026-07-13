namespace LinguaForge.Domain.Entities
{
    /// <summary>
    /// A rotating refresh token. Only the SHA-256 hash is stored, so a database leak does
    /// not expose usable tokens. On use the token is revoked and replaced (rotation);
    /// presenting an already-revoked token signals reuse/theft.
    /// </summary>
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAtUtc { get; set; }
        public Guid? ReplacedByTokenId { get; set; }

        public User? User { get; set; }

        public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
    }
}
