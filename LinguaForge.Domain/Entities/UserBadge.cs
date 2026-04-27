namespace LinguaForge.Domain.Entities
{
    public class UserBadge
    {
        public Guid UserId { get; set; }
        public Guid BadgeId { get; set; }
        public DateTime UnlockedAtUtc { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Badge? Badge { get; set; }
    }
}