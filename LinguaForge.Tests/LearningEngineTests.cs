using LinguaForge.Application.DTOs;
using LinguaForge.Domain.Entities;
using LinguaForge.Infrastructure.Data;
using LinguaForge.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LinguaForge.Tests;

public class LearningEngineTests
{
    [Fact]
    public async Task ContentSeeder_loads_full_a1_course()
    {
        using var ctx = new SqliteTestContext();

        await ContentSeeder.SeedAsync(ctx.Db);

        var lessons = await ctx.Db.Lessons.Where(x => x.Level == "A1").ToListAsync();
        Assert.Equal(6, lessons.Count);

        var articles = lessons.Single(x => x.LessonKey == "a1-articles");
        Assert.Equal("Articles", articles.Title);

        var articleExercises = await ctx.Db.Exercises.CountAsync(x => x.LessonKey == "a1-articles");
        Assert.Equal(3, articleExercises);
    }

    [Fact]
    public async Task ContentSeeder_is_idempotent()
    {
        using var ctx = new SqliteTestContext();

        await ContentSeeder.SeedAsync(ctx.Db);
        await ContentSeeder.SeedAsync(ctx.NewContext()); // run again over the same db

        Assert.Equal(6, await ctx.Db.Lessons.CountAsync());
        Assert.Equal(4, await ctx.Db.VocabItems.CountAsync(x => x.LessonKey == "a1-articles"));
        Assert.Equal(3, await ctx.Db.Exercises.CountAsync(x => x.LessonKey == "a1-articles"));
    }

    [Fact]
    public async Task EvaluateAnswer_grades_against_stored_answer_and_awards_xp_once()
    {
        using var ctx = new SqliteTestContext();
        await ContentSeeder.SeedAsync(ctx.Db);
        var user = await AddUserAsync(ctx.Db);
        var (lessonService, _) = CreateServices(ctx);

        var derHund = await ctx.Db.Exercises.FirstAsync(x => x.LessonKey == "a1-articles" && x.Order == 1);

        var correct = await lessonService.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto
        {
            ExerciseId = derHund.Id,
            SubmittedAnswer = "der"
        });

        Assert.NotNull(correct);
        Assert.True(correct!.IsCorrect);
        Assert.Equal(10, correct.EarnedXp); // first correct → Xp granted

        // Re-answering the same exercise correctly must award 0 (ledger idempotency).
        var again = await lessonService.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto
        {
            ExerciseId = derHund.Id,
            SubmittedAnswer = "der"
        });
        Assert.Equal(0, again!.EarnedXp);

        var wrong = await lessonService.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto
        {
            ExerciseId = derHund.Id,
            SubmittedAnswer = "die"
        });
        Assert.False(wrong!.IsCorrect);
        Assert.Equal(0, wrong.EarnedXp);
        Assert.Equal("der", wrong.CorrectAnswer);

        Assert.Equal(3, await ctx.Db.QuizAttempts.CountAsync(x => x.UserId == user.Id));
        // Exactly one XP event for that exercise despite two correct submissions.
        Assert.Equal(1, await ctx.Db.XpEvents.CountAsync(x =>
            x.UserId == user.Id && x.Reason == XpReason.ExerciseFirstCorrect && x.SourceId == derHund.Id));
    }

    [Fact]
    public async Task EvaluateAnswer_returns_null_for_unknown_exercise()
    {
        using var ctx = new SqliteTestContext();
        var user = await AddUserAsync(ctx.Db);
        var (lessonService, _) = CreateServices(ctx);

        var result = await lessonService.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto
        {
            ExerciseId = Guid.NewGuid(),
            SubmittedAnswer = "anything"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task AwardXp_is_idempotent_per_reason_and_source()
    {
        using var ctx = new SqliteTestContext();
        var user = await AddUserAsync(ctx.Db);
        var (_, progressService) = CreateServices(ctx);
        var source = Guid.NewGuid();

        var first = await progressService.AwardXpAsync(user.Id, 25, XpReason.LessonCompletion, source);
        var second = await progressService.AwardXpAsync(user.Id, 25, XpReason.LessonCompletion, source);

        Assert.Equal(25, first);
        Assert.Equal(0, second); // duplicate → no award
        Assert.Equal(25, (await ctx.Db.Users.SingleAsync(x => x.Id == user.Id)).TotalXp);
        Assert.Equal(1, await ctx.Db.XpEvents.CountAsync(x => x.UserId == user.Id));
    }

    [Fact]
    public async Task LessonCompletion_grants_server_computed_xp_via_ledger()
    {
        using var ctx = new SqliteTestContext();
        await ContentSeeder.SeedAsync(ctx.Db);
        var user = await AddUserAsync(ctx.Db);
        var (lessonService, progressService) = CreateServices(ctx);

        var exercises = await ctx.Db.Exercises
            .Where(x => x.LessonKey == "a1-articles").OrderBy(x => x.Order).ToListAsync();

        await lessonService.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto { ExerciseId = exercises[0].Id, SubmittedAnswer = "der" });
        await lessonService.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto { ExerciseId = exercises[1].Id, SubmittedAnswer = "die" });

        var progress = await progressService.RecordLessonCompletionAsync(user.Id, "a1-articles");

        // 2 first-correct (20) + completion bonus (10) + first-lesson badge (50) = 80.
        Assert.Equal(80, progress.TotalXp);
        Assert.Equal(1, progress.CurrentStreakDays);
        Assert.Equal(4, await ctx.Db.XpEvents.CountAsync(x => x.UserId == user.Id));
    }

    [Fact]
    public async Task LessonCompletion_is_idempotent()
    {
        using var ctx = new SqliteTestContext();
        await ContentSeeder.SeedAsync(ctx.Db);
        var user = await AddUserAsync(ctx.Db);
        var (lessonService, progressService) = CreateServices(ctx);

        var ex1 = await ctx.Db.Exercises.FirstAsync(x => x.LessonKey == "a1-articles" && x.Order == 1);
        await lessonService.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto { ExerciseId = ex1.Id, SubmittedAnswer = "der" });

        var first = await progressService.RecordLessonCompletionAsync(user.Id, "a1-articles");
        var second = await progressService.RecordLessonCompletionAsync(user.Id, "a1-articles");

        // Re-completing the same lesson grants no additional completion/badge XP.
        Assert.Equal(first.TotalXp, second.TotalXp);
    }

    private static (LessonService lessonService, UserProgressService progressService) CreateServices(SqliteTestContext ctx)
    {
        // One shared context, mirroring the scoped DbContext a real request gets.
        var progressService = new UserProgressService(ctx.Db);
        var lessonService = new LessonService(ctx.Db, progressService);
        return (lessonService, progressService);
    }

    private static async Task<User> AddUserAsync(LinguaForgeDbContext db)
    {
        var user = new User
        {
            UserName = "Tester",
            Email = $"tester+{Guid.NewGuid():N}@linguaforge.local"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
