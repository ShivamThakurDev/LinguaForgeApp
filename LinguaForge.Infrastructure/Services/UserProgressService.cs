using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;
using LinguaForge.Domain.Entities;
using LinguaForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LinguaForge.Infrastructure.Services
{
    public class UserProgressService : IUserProgressService
    {
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

            var heatmap = await _dbContext.LessonProgresses
                .Where(x => x.UserId == user.Id && x.IsCompleted)
                .GroupBy(x => DateOnly.FromDateTime(x.UpdatedAtUtc.Date))
                .Select(group => new HeatmapPointDto
                {
                    Date = group.Key,
                    Xp = group.Sum(x => x.EarnedXp)
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

        public async Task<UserProgressDto> RecordLessonCompletionAsync(CompleteLessonRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await GetOrCreateUserAsync(request.UserId, cancellationToken);
            await EnsureStarterBadgesAsync(cancellationToken);

            var progress = await _dbContext.LessonProgresses
                .SingleOrDefaultAsync(x => x.UserId == user.Id && x.LessonKey == request.LessonKey, cancellationToken);

            if (progress is null)
            {
                progress = new LessonProgress
                {
                    UserId = user.Id,
                    LessonKey = request.LessonKey,
                    LessonTitle = request.LessonTitle,
                    Attempts = 1,
                    IsCompleted = true,
                    AccuracyPercent = request.AccuracyPercent,
                    EarnedXp = request.EarnedXp,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _dbContext.LessonProgresses.Add(progress);
                user.TotalXp += request.EarnedXp;
            }
            else
            {
                progress.Attempts += 1;
                progress.IsCompleted = true;
                progress.AccuracyPercent = request.AccuracyPercent;
                progress.EarnedXp = request.EarnedXp;
                progress.UpdatedAtUtc = DateTime.UtcNow;
                user.TotalXp += Math.Max(0, request.EarnedXp / 2);
            }

            UpdateStreak(user);
            user.Level = Math.Clamp((user.TotalXp / 100) + 1, 1, 50);

            await UnlockBadgesAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetProgressAsync(user.Id, cancellationToken);
        }

        public async Task<UserProgressDto> AwardQuizXpAsync(Guid userId, int earnedXp, CancellationToken cancellationToken = default)
        {
            var user = await GetOrCreateUserAsync(userId, cancellationToken);
            await EnsureStarterBadgesAsync(cancellationToken);

            user.TotalXp += Math.Max(0, earnedXp);
            UpdateStreak(user);
            user.Level = Math.Clamp((user.TotalXp / 100) + 1, 1, 50);

            await UnlockBadgesAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetProgressAsync(user.Id, cancellationToken);
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

            void Unlock(string code)
            {
                var badge = badges.SingleOrDefault(x => x.Code == code);
                if (badge is null || existing.Contains(badge.Id))
                {
                    return;
                }

                _dbContext.UserBadges.Add(new UserBadge
                {
                    UserId = user.Id,
                    BadgeId = badge.Id,
                    UnlockedAtUtc = DateTime.UtcNow
                });
                user.TotalXp += badge.BonusXp;
            }

            if (completedCount >= 1)
            {
                Unlock("first_lesson");
            }

            if (user.CurrentStreakDays >= 7)
            {
                Unlock("seven_day_streak");
            }

            var learnedWordCount = await _dbContext.VocabItems.CountAsync(cancellationToken);
            if (learnedWordCount >= 100)
            {
                Unlock("hundred_words");
            }
        }
    }
}
