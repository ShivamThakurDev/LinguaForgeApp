namespace LinguaForge.Domain.Entities
{
    public class AuthCredential
    {
        public Guid UserId { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
