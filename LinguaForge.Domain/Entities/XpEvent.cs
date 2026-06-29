namespace LinguaForge.Domain.Entities
{
    /// <summary>
    /// Append-only ledger entry — the single source of truth for XP. User.TotalXp is a
    /// cached projection (= SUM(Amount)). The unique index (UserId, Reason, SourceId)
    /// makes every award idempotent: re-granting the same exercise/lesson/badge is a no-op,
    /// which structurally prevents double counting rather than relying on call-site care.
    /// </summary>
    public class XpEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public int Amount { get; set; }
        public XpReason Reason { get; set; }

        /// <summary>The entity the XP came from (exercise / lesson / badge id).</summary>
        public Guid SourceId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
