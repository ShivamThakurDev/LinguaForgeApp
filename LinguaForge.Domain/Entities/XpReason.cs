namespace LinguaForge.Domain.Entities
{
    /// <summary>
    /// Why an <see cref="XpEvent"/> was granted. Combined with SourceId it forms the
    /// idempotency key, so the same reason+source can only ever award XP once.
    /// </summary>
    public enum XpReason
    {
        ExerciseFirstCorrect = 1,
        LessonCompletion = 2,
        BadgeBonus = 3,
        StreakBonus = 4
    }
}
