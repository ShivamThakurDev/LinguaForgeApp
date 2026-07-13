using LinguaForge.Application.DTOs;
using LinguaForge.Domain.Entities;

namespace LinguaForge.Application.Interface
{
    public interface IUserProgressService
    {
        Task<UserProgressDto> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a lesson complete (idempotent) and grants the completion bonus + any badge
        /// XP. XP/accuracy are computed server-side; the client only names the lesson.
        /// </summary>
        Task<UserProgressDto> RecordLessonCompletionAsync(Guid userId, string lessonKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Idempotently grants XP via the ledger. Returns the amount actually awarded
        /// (0 if this (reason, source) was already granted to the user).
        /// </summary>
        Task<int> AwardXpAsync(Guid userId, int amount, XpReason reason, Guid sourceId, CancellationToken cancellationToken = default);
    }
}
