using System.Reflection;
using System.Text.Json;
using LinguaForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinguaForge.Infrastructure.Data
{
    /// <summary>
    /// Seeds course content (lessons, vocabulary, exercises) from the embedded course JSON.
    /// Idempotent: rows are matched by natural key (lesson key + word / order), so running
    /// it on every startup updates existing content and inserts new lessons without
    /// creating duplicates. Adding a lesson is a pure JSON edit — no code change.
    /// </summary>
    public static class ContentSeeder
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task SeedAsync(LinguaForgeDbContext dbContext, CancellationToken cancellationToken = default)
        {
            var course = LoadCourse();
            if (course is null)
            {
                return;
            }

            var level = string.IsNullOrWhiteSpace(course.Level) ? "A1" : course.Level.ToUpperInvariant();

            foreach (var lesson in course.Lessons)
            {
                await UpsertLessonAsync(dbContext, level, lesson, cancellationToken);
                await UpsertVocabularyAsync(dbContext, level, lesson, cancellationToken);
                await UpsertExercisesAsync(dbContext, level, lesson, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static async Task UpsertLessonAsync(
            LinguaForgeDbContext dbContext, string level, ContentLesson lesson, CancellationToken cancellationToken)
        {
            var existing = await dbContext.Lessons
                .SingleOrDefaultAsync(x => x.LessonKey == lesson.LessonKey, cancellationToken);

            if (existing is null)
            {
                dbContext.Lessons.Add(new Lesson
                {
                    LessonKey = lesson.LessonKey,
                    Level = level,
                    Order = lesson.Order,
                    Title = lesson.Title,
                    Description = lesson.Description
                });
            }
            else
            {
                existing.Level = level;
                existing.Order = lesson.Order;
                existing.Title = lesson.Title;
                existing.Description = lesson.Description;
            }
        }

        private static async Task UpsertVocabularyAsync(
            LinguaForgeDbContext dbContext, string level, ContentLesson lesson, CancellationToken cancellationToken)
        {
            var existingItems = await dbContext.VocabItems
                .Where(x => x.LessonKey == lesson.LessonKey)
                .ToListAsync(cancellationToken);

            foreach (var vocab in lesson.Vocabulary)
            {
                var match = existingItems.FirstOrDefault(x =>
                    string.Equals(x.German, vocab.German, StringComparison.OrdinalIgnoreCase));

                if (match is null)
                {
                    dbContext.VocabItems.Add(new VocabItem
                    {
                        LessonKey = lesson.LessonKey,
                        CefrLevel = level,
                        German = vocab.German,
                        English = vocab.English,
                        PartOfSpeech = vocab.PartOfSpeech
                    });
                }
                else
                {
                    match.CefrLevel = level;
                    match.English = vocab.English;
                    match.PartOfSpeech = vocab.PartOfSpeech;
                }
            }
        }

        private static async Task UpsertExercisesAsync(
            LinguaForgeDbContext dbContext, string level, ContentLesson lesson, CancellationToken cancellationToken)
        {
            var existingExercises = await dbContext.Exercises
                .Where(x => x.LessonKey == lesson.LessonKey)
                .ToListAsync(cancellationToken);

            foreach (var exercise in lesson.Exercises)
            {
                var optionsJson = JsonSerializer.Serialize(exercise.Options ?? new List<string>());
                var match = existingExercises.FirstOrDefault(x => x.Order == exercise.Order);

                if (match is null)
                {
                    dbContext.Exercises.Add(new Exercise
                    {
                        LessonKey = lesson.LessonKey,
                        CefrLevel = level,
                        Order = exercise.Order,
                        Type = exercise.Type,
                        PromptText = exercise.PromptText,
                        Question = exercise.Question,
                        OptionsJson = optionsJson,
                        CorrectAnswer = exercise.CorrectAnswer,
                        Explanation = exercise.Explanation,
                        Topic = exercise.Topic,
                        XpReward = exercise.XpReward
                    });
                }
                else
                {
                    match.CefrLevel = level;
                    match.Type = exercise.Type;
                    match.PromptText = exercise.PromptText;
                    match.Question = exercise.Question;
                    match.OptionsJson = optionsJson;
                    match.CorrectAnswer = exercise.CorrectAnswer;
                    match.Explanation = exercise.Explanation;
                    match.Topic = exercise.Topic;
                    match.XpReward = exercise.XpReward;
                }
            }
        }

        private static ContentCourse? LoadCourse()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("a1-course.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
            {
                return null;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<ContentCourse>(json, JsonOptions);
        }

        // --- JSON shapes ---

        private sealed class ContentCourse
        {
            public string Level { get; set; } = "A1";
            public List<ContentLesson> Lessons { get; set; } = new();
        }

        private sealed class ContentLesson
        {
            public string LessonKey { get; set; } = string.Empty;
            public int Order { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<ContentVocab> Vocabulary { get; set; } = new();
            public List<ContentExercise> Exercises { get; set; } = new();
        }

        private sealed class ContentVocab
        {
            public string German { get; set; } = string.Empty;
            public string English { get; set; } = string.Empty;
            public string PartOfSpeech { get; set; } = string.Empty;
        }

        private sealed class ContentExercise
        {
            public int Order { get; set; }
            public string Type { get; set; } = "mcq";
            public string PromptText { get; set; } = string.Empty;
            public string Question { get; set; } = string.Empty;
            public List<string> Options { get; set; } = new();
            public string CorrectAnswer { get; set; } = string.Empty;
            public string Explanation { get; set; } = string.Empty;
            public string Topic { get; set; } = string.Empty;
            public int XpReward { get; set; } = 10;
        }
    }
}
