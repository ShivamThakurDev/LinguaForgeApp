using Microsoft.EntityFrameworkCore;

namespace LinguaForge.Infrastructure.Data
{
    public static class DbBootstrapper
    {
        public static async Task InitializeAsync(LinguaForgeDbContext dbContext, CancellationToken cancellationToken = default)
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);

            var sql = """
                IF OBJECT_ID(N'[dbo].[AuthCredentials]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[AuthCredentials](
                        [UserId] uniqueidentifier NOT NULL PRIMARY KEY,
                        [PasswordHash] nvarchar(max) NOT NULL,
                        [PasswordSalt] nvarchar(max) NOT NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        CONSTRAINT [FK_AuthCredentials_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
                    );
                END;

                IF OBJECT_ID(N'[dbo].[QuizAttempts]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[QuizAttempts](
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [UserId] uniqueidentifier NOT NULL,
                        [LessonKey] nvarchar(100) NOT NULL,
                        [Topic] nvarchar(80) NOT NULL,
                        [ExerciseType] nvarchar(40) NOT NULL,
                        [Question] nvarchar(400) NOT NULL,
                        [SubmittedAnswer] nvarchar(400) NOT NULL,
                        [CorrectAnswer] nvarchar(400) NOT NULL,
                        [IsCorrect] bit NOT NULL,
                        [ScorePercent] int NOT NULL,
                        [Feedback] nvarchar(1000) NOT NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        CONSTRAINT [FK_QuizAttempts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_QuizAttempts_UserId_CreatedAtUtc] ON [dbo].[QuizAttempts]([UserId], [CreatedAtUtc] DESC);
                END;

                IF OBJECT_ID(N'[dbo].[WeakTopics]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[WeakTopics](
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [UserId] uniqueidentifier NOT NULL,
                        [TopicCode] nvarchar(80) NOT NULL,
                        [MistakeCount] int NOT NULL,
                        [LastMistakeAtUtc] datetime2 NULL,
                        [LastUpdatedAtUtc] datetime2 NOT NULL,
                        CONSTRAINT [FK_WeakTopics_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX [IX_WeakTopics_UserId_TopicCode] ON [dbo].[WeakTopics]([UserId], [TopicCode]);
                END;
                """;

            await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }
}
