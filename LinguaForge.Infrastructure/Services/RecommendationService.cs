using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;
using LinguaForge.Domain.Entities;
using LinguaForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LinguaForge.Infrastructure.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly LinguaForgeDbContext _dbContext;

        public RecommendationService(LinguaForgeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task RecordQuizOutcomeAsync(Guid userId, QuizEvaluationRequestDto request, QuizEvaluationResponseDto response, CancellationToken cancellationToken = default)
        {
            _dbContext.QuizAttempts.Add(new QuizAttempt
            {
                UserId = userId,
                LessonKey = request.LessonKey,
                Topic = NormalizeTopic(request.Topic),
                ExerciseType = request.ExerciseType,
                Question = request.Question,
                SubmittedAnswer = request.SubmittedAnswer,
                CorrectAnswer = request.CorrectAnswer,
                IsCorrect = response.IsCorrect,
                ScorePercent = response.ScorePercent,
                Feedback = response.Feedback,
                CreatedAtUtc = DateTime.UtcNow
            });

            var topicCode = NormalizeTopic(response.WeakTopic);
            if (!response.IsCorrect && !string.IsNullOrWhiteSpace(topicCode))
            {
                var weakTopic = await _dbContext.WeakTopics
                    .SingleOrDefaultAsync(x => x.UserId == userId && x.TopicCode == topicCode, cancellationToken);

                if (weakTopic is null)
                {
                    weakTopic = new WeakTopic
                    {
                        UserId = userId,
                        TopicCode = topicCode,
                        MistakeCount = 1,
                        LastMistakeAtUtc = DateTime.UtcNow,
                        LastUpdatedAtUtc = DateTime.UtcNow
                    };
                    _dbContext.WeakTopics.Add(weakTopic);
                }
                else
                {
                    weakTopic.MistakeCount += 1;
                    weakTopic.LastMistakeAtUtc = DateTime.UtcNow;
                    weakTopic.LastUpdatedAtUtc = DateTime.UtcNow;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<RecommendationDto> GetNextRecommendationAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var strongestWeakTopic = await _dbContext.WeakTopics
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.MistakeCount)
                .ThenByDescending(x => x.LastUpdatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var recentMistake = await _dbContext.QuizAttempts
                .Where(x => x.UserId == userId && !x.IsCorrect)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var topic = strongestWeakTopic?.TopicCode
                ?? recentMistake?.Topic
                ?? "articles";

            var lessonKey = await _dbContext.VocabItems
                .Where(x => x.LessonKey.Contains(topic))
                .Select(x => x.LessonKey)
                .FirstOrDefaultAsync(cancellationToken)
                ?? "a1-articles";

            var level = await _dbContext.Users
                .Where(x => x.Id == userId)
                .Select(x => x.Level <= 2 ? "A1" : "A2")
                .SingleOrDefaultAsync(cancellationToken)
                ?? "A1";

            return new RecommendationDto
            {
                Topic = topic,
                LessonKey = lessonKey,
                Level = level,
                SuggestedExerciseType = recentMistake?.ExerciseType == "fill-in" ? "fill-in" : "mcq",
                PriorityScore = strongestWeakTopic?.MistakeCount ?? 1,
                Reason = strongestWeakTopic is not null
                    ? $"You have missed {strongestWeakTopic.MistakeCount} recent {topic} exercises."
                    : "Start with a foundational grammar drill to build momentum."
            };
        }

        private static string NormalizeTopic(string topic)
            => string.IsNullOrWhiteSpace(topic) ? string.Empty : topic.Trim().ToLowerInvariant();
    }
}
