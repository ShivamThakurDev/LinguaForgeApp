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
        // a1-articles: 2 seeded via HasData + 2 from JSON, matched by natural key — no dupes.
        Assert.Equal(4, await ctx.Db.VocabItems.CountAsync(x => x.LessonKey == "a1-articles"));
        Assert.Equal(3, await ctx.Db.Exercises.CountAsync(x => x.LessonKey == "a1-articles"));
    }

    [Fact]
    public async Task EvaluateAnswer_grades_against_stored_answer()
    {
        using var ctx = new SqliteTestContext();
        await ContentSeeder.SeedAsync(ctx.Db);
        var user = await AddUserAsync(ctx.Db);

        var service = new LessonService(ctx.NewContext());
        var derHund = await ctx.Db.Exercises.FirstAsync(x => x.LessonKey == "a1-articles" && x.Order == 1);

        var correct = await service.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto
        {
            ExerciseId = derHund.Id,
            SubmittedAnswer = "der"
        });

        Assert.NotNull(correct);
        Assert.True(correct!.IsCorrect);
        Assert.Equal(10, correct.EarnedXp);

        var wrong = await service.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto
        {
            ExerciseId = derHund.Id,
            SubmittedAnswer = "die"
        });

        Assert.NotNull(wrong);
        Assert.False(wrong!.IsCorrect);
        Assert.Equal(0, wrong.EarnedXp);
        Assert.Equal("der", wrong.CorrectAnswer); // server reveals the answer only after grading

        // Both attempts were recorded server-side.
        Assert.Equal(2, await ctx.Db.QuizAttempts.CountAsync(x => x.UserId == user.Id));
    }

    [Fact]
    public async Task EvaluateAnswer_returns_null_for_unknown_exercise()
    {
        using var ctx = new SqliteTestContext();
        var user = await AddUserAsync(ctx.Db);
        var service = new LessonService(ctx.NewContext());

        var result = await service.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto
        {
            ExerciseId = Guid.NewGuid(),
            SubmittedAnswer = "anything"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task LessonCompletion_XP_is_server_computed_and_ignores_client_value()
    {
        using var ctx = new SqliteTestContext();
        await ContentSeeder.SeedAsync(ctx.Db);
        var user = await AddUserAsync(ctx.Db);

        var lessonService = new LessonService(ctx.NewContext());
        var articleExercises = await ctx.Db.Exercises
            .Where(x => x.LessonKey == "a1-articles")
            .OrderBy(x => x.Order)
            .ToListAsync();

        // Answer two exercises correctly (Hund -> der, Katze -> die).
        await lessonService.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto { ExerciseId = articleExercises[0].Id, SubmittedAnswer = "der" });
        await lessonService.EvaluateAnswerAsync(user.Id, new SubmitAnswerRequestDto { ExerciseId = articleExercises[1].Id, SubmittedAnswer = "die" });

        var progressService = new UserProgressService(ctx.NewContext());
        var progress = await progressService.RecordLessonCompletionAsync(new CompleteLessonRequestDto
        {
            UserId = user.Id,
            LessonKey = "a1-articles",
            LessonTitle = "Articles",
            AccuracyPercent = 1,        // bogus client values...
            EarnedXp = 9999             // ...must be ignored by the server
        });

        // 2 distinct correct exercises * 10 = 20 lesson XP, + 50 first-lesson badge bonus = 70.
        Assert.Equal(70, progress.TotalXp);
        Assert.NotEqual(9999, progress.TotalXp);
        Assert.Equal(1, progress.CurrentStreakDays);
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
