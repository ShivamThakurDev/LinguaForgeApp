using System.Text.Json;
using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;
using LinguaForge.Domain.Entities;
using LinguaForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LinguaForge.Infrastructure.Services
{
    public class LessonService : ILessonService
    {
        private readonly LinguaForgeDbContext _dbContext;
        private readonly IUserProgressService _userProgressService;

        public LessonService(LinguaForgeDbContext dbContext, IUserProgressService userProgressService)
        {
            _dbContext = dbContext;
            _userProgressService = userProgressService;
        }

        public async Task<IReadOnlyList<LessonDto>> GetLessonsAsync(string level, CancellationToken cancellationToken = default)
        {
            var normalizedLevel = string.IsNullOrWhiteSpace(level) ? "A1" : level.ToUpperInvariant();

            var lessons = await _dbContext.Lessons
                .Where(x => x.Level == normalizedLevel)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.LessonKey)
                .ToListAsync(cancellationToken);

            if (lessons.Count == 0)
            {
                return Array.Empty<LessonDto>();
            }

            var vocab = await _dbContext.VocabItems
                .Where(x => x.CefrLevel == normalizedLevel)
                .ToListAsync(cancellationToken);

            var exercises = await _dbContext.Exercises
                .Where(x => x.CefrLevel == normalizedLevel)
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken);

            var vocabByLesson = vocab
                .GroupBy(x => x.LessonKey)
                .ToDictionary(group => group.Key, group => group.OrderBy(v => v.German).ToList());

            var exercisesByLesson = exercises
                .GroupBy(x => x.LessonKey)
                .ToDictionary(group => group.Key, group => group.ToList());

            return lessons.Select(lesson => new LessonDto
            {
                LessonKey = lesson.LessonKey,
                Level = normalizedLevel,
                Title = lesson.Title,
                Description = lesson.Description,
                Vocabulary = vocabByLesson.TryGetValue(lesson.LessonKey, out var lessonVocab)
                    ? lessonVocab.Select(v => new LessonVocabDto
                    {
                        German = v.German,
                        English = v.English,
                        PartOfSpeech = v.PartOfSpeech,
                        AudioUrl = v.AudioUrl
                    }).ToList()
                    : new List<LessonVocabDto>(),
                Exercises = exercisesByLesson.TryGetValue(lesson.LessonKey, out var lessonExercises)
                    ? lessonExercises.Select(ToClientExercise).ToList()
                    : new List<LessonExerciseDto>()
            }).ToList();
        }

        public async Task<SubmitAnswerResultDto?> EvaluateAnswerAsync(Guid userId, SubmitAnswerRequestDto request, CancellationToken cancellationToken = default)
        {
            var exercise = await _dbContext.Exercises
                .SingleOrDefaultAsync(x => x.Id == request.ExerciseId, cancellationToken);

            if (exercise is null)
            {
                return null;
            }

            var isCorrect = string.Equals(
                (request.SubmittedAnswer ?? string.Empty).Trim(),
                exercise.CorrectAnswer.Trim(),
                StringComparison.OrdinalIgnoreCase);

            _dbContext.QuizAttempts.Add(new QuizAttempt
            {
                UserId = userId,
                ExerciseId = exercise.Id,
                LessonKey = exercise.LessonKey,
                Topic = exercise.Topic,
                ExerciseType = exercise.Type,
                Question = exercise.Question,
                SubmittedAnswer = request.SubmittedAnswer ?? string.Empty,
                CorrectAnswer = exercise.CorrectAnswer,
                IsCorrect = isCorrect,
                ScorePercent = isCorrect ? 100 : 0,
                Feedback = exercise.Explanation,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Grant XP for the FIRST correct answer only. The ledger's idempotency key
            // (user, ExerciseFirstCorrect, exerciseId) makes retries award 0 — no farming.
            var awardedXp = isCorrect
                ? await _userProgressService.AwardXpAsync(userId, exercise.XpReward, XpReason.ExerciseFirstCorrect, exercise.Id, cancellationToken)
                : 0;

            return new SubmitAnswerResultDto
            {
                IsCorrect = isCorrect,
                EarnedXp = awardedXp,
                CorrectAnswer = exercise.CorrectAnswer,
                Explanation = exercise.Explanation
            };
        }

        private static LessonExerciseDto ToClientExercise(Exercise exercise)
        {
            IReadOnlyList<string> options = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(exercise.OptionsJson))
            {
                try
                {
                    options = JsonSerializer.Deserialize<List<string>>(exercise.OptionsJson) ?? new List<string>();
                }
                catch (JsonException)
                {
                    options = Array.Empty<string>();
                }
            }

            return new LessonExerciseDto
            {
                Id = exercise.Id,
                Type = exercise.Type,
                PromptText = exercise.PromptText,
                Question = exercise.Question,
                Options = options,
                Topic = exercise.Topic
            };
        }
    }
}
