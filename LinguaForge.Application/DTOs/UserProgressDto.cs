namespace LinguaForge.Application.DTOs
{
    public class UserProgressDto
    {
        public Guid UserId { get; set; }
        public int TotalXp { get; set; }
        public int CurrentStreakDays { get; set; }
        public int Level { get; set; }
        public IReadOnlyList<ProgressBadgeDto> Badges { get; set; } = Array.Empty<ProgressBadgeDto>();
        public IReadOnlyList<HeatmapPointDto> Heatmap { get; set; } = Array.Empty<HeatmapPointDto>();
    }
}