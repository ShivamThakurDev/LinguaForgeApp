using System.Security.Cryptography;
using System.Text;
using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;
using LinguaForge.Domain.Entities;
using LinguaForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LinguaForge.Infrastructure.Services
{
    public class UserProgressService : IUserProgressService
    {
        private const int LessonCompletionBonusXp = 10;

        private readonly LinguaForgeDbContext _dbContext;

        public UserProgressService(LinguaForgeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserProgressDto> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetOrCreateUserAsync(userId, cancellationToken);
            await EnsureStarterBadgesAsync(cancellationToken);

            var badges = await _dbContext.UserBadges
                .Where(x => x.UserId == user.Id)
                .Include(x => x.Badge)
                .OrderByDescending(x => x.UnlockedAtUtc)
                .ToListAsync(cancellationToken);

            // Heatmap is derived from the XP ledger — the single source of XP truth.
            var heatmap = await _dbContext.XpEvents
                .Where(x => x.UserId == user.Id)
                .GroupBy(x => DateOnly.FromDateTime(x.CreatedAtUtc.Date))
                .Select(group => new HeatmapPointDto
                {
                    Date = group.Key,
                    Xp = group.Sum(x => x.Amount)
                })
                .OrderBy(x => x.Date)
                .Take(60)
                .ToListAsync(cancellationToken);

            return new UserProgressDto
            {
                UserId = user.Id,
                TotalXp = user.TotalXp,
                CurrentStreakDays = user.CurrentStreakDays,
                Level = user.Level,
                Badges = badges.Select(x => new ProgressBadgeDto
                {
                    Code = x.Badge?.Code ?? string.Empty,
                    Name = x.Badge?.Name ?? string.Empty,
                    Description = x.Badge?.Description ?? string.Empty,
                    UnlockedAtUtc = x.UnlockedAtUtc
                }).ToList(),
                Heatmap = heatmap
            };
        }

        public async Task<UserProgressDto> RecordLessonCompletionAsync(Guid userId, string lessonKey, CancellationToken cancellationToken = default)
        {
            var user = await GetOrCreateUserAsync(userId, cancellationToken);
            await EnsureStarterBadgesAsync(cancellationToken);

            // Title comes from the content, never the client.
            var lesson = await _dbContext.Lessons.SingleOrDefaultAsync(x => x.LessonKey == lessonKey, cancellationToken);
            var lessonTitle = lesson?.Title ?? lessonKey.Replace("-", " ");

            // Accuracy is derived from server-graded attempts (display only).
            var totalExercises = await _dbContext.Exercises.CountAsync(x => x.LessonKey == lessonKey, cancellationToken);
            var correctExerciseCount = await _dbContext.QuizAttempts
                .Where(x => x.UserId == user.Id && x.LessonKey == lessonKey && x.IsCorrect && x.ExerciseId != null)
                .Select(x => x.ExerciseId)
                .Distinct()
                .CountAsync(cancellationToken);
            var accuracyPercent = totalExercises > 0
                ? (int)Math.Round(100.0 * correctExerciseCount / totalExercises)
                : 100;

            var progress = await _dbContext.LessonProgresses
                .SingleOrDefaultAsync(x => x.UserId == user.Id && x.LessonKey == lessonKey, cancellationToken);

            if (progress is null)
            {
                progress = new LessonProgress
                {
                    UserId = user.Id,
                    LessonKey = lessonKey,
                    LessonTitle = lessonTitle,
                    Attempts = 1,
                    IsCompleted = true,
                    AccuracyPercent = accuracyPercent,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _dbContext.LessonProgresses.Add(progress);
            }
            else
            {
                progress.Attempts += 1;
                progress.IsCompleted = true;
                progress.AccuracyPercent = accuracyPercent;
                progress.LessonTitle = lessonTitle;
                progress.UpdatedAtUtc = DateTime.UtcNow;
            }

            UpdateStreak(user);

            // Persist the completion before evaluating badges so UnlockBadgesAsync (which
            // counts completed lessons from the DB) sees this lesson.
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Completion bonus — idempotent per lesson, so re-completing awards nothing.
            await AwardXpAsync(user.Id, LessonCompletionBonusXp, XpReason.LessonCompletion, DeterministicGuid(lessonKey), cancellationToken);
            await UnlockBadgesAsync(user, cancellationToken);

            return await GetProgressAsync(user.Id, cancellationToken);
        }

        public async Task<int> AwardXpAsync(Guid userId, int amount, XpReason reason, Guid sourceId, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var user = await GetOrCreateUserAsync(userId, cancellationToken);

            // Idempotency: this (user, reason, source) may only ever award XP once. The
            // unique DB index is the hard guarantee; this check avoids the common-case throw.
            var alreadyGranted = await _dbContext.XpEvents
                .AnyAsync(x => x.UserId == user.Id && x.Reason == reason && x.SourceId == sourceId, cancellationToken);
            if (alreadyGranted)
            {
                return 0;
            }

            _dbContext.XpEvents.Add(new XpEvent
            {
                UserId = user.Id,
                Amount = amount,
                Reason = reason,
                SourceId = sourceId,
                CreatedAtUtc = DateTime.UtcNow
            });

            user.TotalXp += amount;
            user.Level = Math.Clamp((user.TotalXp / 100) + 1, 1, 50);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return amount;
        }

        private async Task<User> GetOrCreateUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user is not null)
            {
                return user;
            }

            user = new User
            {
                Id = userId == Guid.Empty ? Guid.NewGuid() : userId,
                UserName = "Learner",
                Email = $"learner+{Guid.NewGuid():N}@linguaforge.local",
                TotalXp = 0,
                CurrentStreakDays = 0,
                Level = 1
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return user;
        }

        private static void UpdateStreak(User user)
        {
            var today = DateTime.UtcNow.Date;
            if (user.LastLessonCompletedOnUtc is null)
            {
                user.CurrentStreakDays = 1;
            }
            else
            {
                var lastDate = user.LastLessonCompletedOnUtc.Value.Date;
                if (lastDate == today)
                {
                    return;
                }

                user.CurrentStreakDays = lastDate == today.AddDays(-1)
                    ? user.CurrentStreakDays + 1
                    : 1;
            }

            user.LastLessonCompletedOnUtc = DateTime.UtcNow;
        }

        private async Task EnsureStarterBadgesAsync(CancellationToken cancellationToken)
        {
            if (await _dbContext.Badges.AnyAsync(cancellationToken))
            {
                return;
            }

            _dbContext.Badges.AddRange(
                new Badge { Code = "first_lesson", Name = "First lesson", Description = "Complete your first lesson", BonusXp = 50 },
                new Badge { Code = "seven_day_streak", Name = "7-day streak", Description = "Keep a 7-day learning streak", BonusXp = 50 },
                new Badge { Code = "hundred_words", Name = "100 words", Description = "Learn 100 vocabulary items", BonusXp = 100 }
            );

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task UnlockBadgesAsync(User user, CancellationToken cancellationToken)
        {
            var completedCount = await _dbContext.LessonProgresses.CountAsync(x => x.UserId == user.Id && x.IsCompleted, cancellationToken);
            var existing = await _dbContext.UserBadges.Where(x => x.UserId == user.Id).Select(x => x.BadgeId).ToListAsync(cancellationToken);
            var badges = await _dbContext.Badges.ToListAsync(cancellationToken);
            var learnedWordCount = await _dbContext.VocabItems.CountAsync(cancellationToken);

            var earnedCodes = new List<string>();
            if (completedCount >= 1) earnedCodes.Add("first_lesson");
            if (user.CurrentStreakDays >= 7) earnedCodes.Add("seven_day_streak");
            if (learnedWordCount >= 100) earnedCodes.Add("hundred_words");

            foreach (var code in earnedCodes)
            {
                var badge = badges.SingleOrDefault(x => x.Code == code);
                if (badge is null || existing.Contains(badge.Id))
                {
                    continue;
                }

                _dbContext.UserBadges.Add(new UserBadge
                {
                    UserId = user.Id,
                    BadgeId = badge.Id,
                    UnlockedAtUtc = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Badge bonus flows through the same idempotent ledger as all other XP.
                await AwardXpAsync(user.Id, badge.BonusXp, XpReason.BadgeBonus, badge.Id, cancellationToken);
            }
        }

        // Stable per-key id so ledger idempotency works without a Guid lesson primary key.
        private static Guid DeterministicGuid(string value)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(value));
            return new Guid(hash);
        }
    }
}
