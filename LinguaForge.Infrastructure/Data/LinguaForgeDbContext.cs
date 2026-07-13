using LinguaForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinguaForge.Infrastructure.Data
{
    public class LinguaForgeDbContext : DbContext
    {
        public LinguaForgeDbContext(DbContextOptions<LinguaForgeDbContext> options) : base(options)
        {
        }

        public DbSet<Translation> Translations => Set<Translation>();
        public DbSet<User> Users => Set<User>();
        public DbSet<AuthCredential> AuthCredentials => Set<AuthCredential>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
        public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
        public DbSet<WeakTopic> WeakTopics => Set<WeakTopic>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<VocabItem> VocabItems => Set<VocabItem>();
        public DbSet<Exercise> Exercises => Set<Exercise>();
        public DbSet<Badge> Badges => Set<Badge>();
        public DbSet<UserBadge> UserBadges => Set<UserBadge>();
        public DbSet<XpEvent> XpEvents => Set<XpEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Email).IsUnique();
                entity.Property(x => x.UserName).HasMaxLength(120);
                entity.Property(x => x.Email).HasMaxLength(200);
            });

            modelBuilder.Entity<AuthCredential>(entity =>
            {
                entity.HasKey(x => x.UserId);
                entity.HasOne(x => x.User)
                    .WithOne(x => x.AuthCredential)
                    .HasForeignKey<AuthCredential>(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LessonProgress>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.UserId, x.LessonKey }).IsUnique();
                entity.Property(x => x.LessonKey).HasMaxLength(100);
                entity.Property(x => x.LessonTitle).HasMaxLength(200);
                entity.HasOne(x => x.User)
                      .WithMany(x => x.LessonProgresses)
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuizAttempt>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.LessonKey).HasMaxLength(100);
                entity.Property(x => x.Topic).HasMaxLength(80);
                entity.Property(x => x.ExerciseType).HasMaxLength(40);
                entity.Property(x => x.Question).HasMaxLength(400);
                entity.Property(x => x.SubmittedAnswer).HasMaxLength(400);
                entity.Property(x => x.CorrectAnswer).HasMaxLength(400);
                entity.Property(x => x.Feedback).HasMaxLength(1000);
                entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
                entity.HasOne(x => x.User)
                    .WithMany(x => x.QuizAttempts)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WeakTopic>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.TopicCode).HasMaxLength(80);
                entity.HasIndex(x => new { x.UserId, x.TopicCode }).IsUnique();
                entity.HasOne(x => x.User)
                    .WithMany(x => x.WeakTopics)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<VocabItem>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.LessonKey).HasMaxLength(100);
                entity.Property(x => x.German).HasMaxLength(100);
                entity.Property(x => x.English).HasMaxLength(100);
                entity.Property(x => x.PartOfSpeech).HasMaxLength(40);
                entity.Property(x => x.CefrLevel).HasMaxLength(5);
            });

            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.HasKey(x => x.LessonKey);
                entity.Property(x => x.LessonKey).HasMaxLength(100);
                entity.Property(x => x.Level).HasMaxLength(5);
                entity.Property(x => x.Title).HasMaxLength(120);
                entity.Property(x => x.Description).HasMaxLength(400);
                entity.HasIndex(x => new { x.Level, x.Order });
            });

            modelBuilder.Entity<Exercise>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.LessonKey).HasMaxLength(100);
                entity.Property(x => x.CefrLevel).HasMaxLength(5);
                entity.Property(x => x.Type).HasMaxLength(20);
                entity.Property(x => x.PromptText).HasMaxLength(200);
                entity.Property(x => x.Question).HasMaxLength(400);
                entity.Property(x => x.OptionsJson).HasMaxLength(1000);
                entity.Property(x => x.CorrectAnswer).HasMaxLength(200);
                entity.Property(x => x.Explanation).HasMaxLength(400);
                entity.Property(x => x.Topic).HasMaxLength(80);
                entity.HasIndex(x => new { x.CefrLevel, x.LessonKey, x.Order });
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.Property(x => x.TokenHash).HasMaxLength(100);
                entity.HasOne(x => x.User)
                      .WithMany()
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<XpEvent>(entity =>
            {
                entity.HasKey(x => x.Id);
                // Idempotency: a given (user, reason, source) can award XP at most once.
                entity.HasIndex(x => new { x.UserId, x.Reason, x.SourceId }).IsUnique();
                entity.HasOne(x => x.User)
                      .WithMany()
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Badge>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Code).IsUnique();
                entity.Property(x => x.Code).HasMaxLength(100);
                entity.Property(x => x.Name).HasMaxLength(120);
                entity.Property(x => x.Description).HasMaxLength(280);
            });

            modelBuilder.Entity<UserBadge>(entity =>
            {
                entity.HasKey(x => new { x.UserId, x.BadgeId });
                entity.HasOne(x => x.User)
                      .WithMany(x => x.UserBadges)
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Badge)
                      .WithMany(x => x.UserBadges)
                      .HasForeignKey(x => x.BadgeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Badge>().HasData(
                new Badge { Id = Guid.Parse("8104f6da-625e-4651-9f88-09c784b0af31"), Code = "first_lesson", Name = "First lesson", Description = "Complete your first lesson", BonusXp = 50 },
                new Badge { Id = Guid.Parse("5f3bf897-ab87-4d49-94ce-eb2ef2f5070f"), Code = "seven_day_streak", Name = "7-day streak", Description = "Keep a 7-day learning streak", BonusXp = 50 },
                new Badge { Id = Guid.Parse("769b8501-d0e8-4f87-9500-c838622bb58e"), Code = "hundred_words", Name = "100 words", Description = "Learn 100 vocabulary items", BonusXp = 100 }
            );

            modelBuilder.Entity<VocabItem>().HasData(
                new VocabItem { Id = Guid.Parse("f2f4f4ce-c284-4475-bf9d-f8188ad028ec"), LessonKey = "a1-greetings", German = "Guten Morgen", English = "Good morning", PartOfSpeech = "phrase", CefrLevel = "A1" },
                new VocabItem { Id = Guid.Parse("4b9038c0-9920-4312-81ab-6f9e2f06ba06"), LessonKey = "a1-greetings", German = "Danke", English = "Thank you", PartOfSpeech = "interjection", CefrLevel = "A1" },
                new VocabItem { Id = Guid.Parse("bf6e7a36-ed48-4058-af65-751e55f102b2"), LessonKey = "a1-articles", German = "der Hund", English = "the dog", PartOfSpeech = "noun", CefrLevel = "A1" },
                new VocabItem { Id = Guid.Parse("b24c6ea5-ad96-4eba-8d55-69e0f71cdb2f"), LessonKey = "a1-articles", German = "die Katze", English = "the cat", PartOfSpeech = "noun", CefrLevel = "A1" }
            );

            modelBuilder.Entity<Exercise>().HasData(
                // Lesson: a1-articles
                new Exercise { Id = Guid.Parse("a1e10001-0000-0000-0000-000000000001"), LessonKey = "a1-articles", CefrLevel = "A1", Order = 1, Type = "mcq", PromptText = "Choose the correct article", Question = "___ Hund", OptionsJson = "[\"der\",\"die\",\"das\"]", CorrectAnswer = "der", Explanation = "\"Hund\" is masculine, so it takes \"der\".", Topic = "articles", XpReward = 10 },
                new Exercise { Id = Guid.Parse("a1e10002-0000-0000-0000-000000000002"), LessonKey = "a1-articles", CefrLevel = "A1", Order = 2, Type = "mcq", PromptText = "Choose the correct article", Question = "___ Katze", OptionsJson = "[\"der\",\"die\",\"das\"]", CorrectAnswer = "die", Explanation = "\"Katze\" is feminine, so it takes \"die\".", Topic = "articles", XpReward = 10 },
                new Exercise { Id = Guid.Parse("a1e10003-0000-0000-0000-000000000003"), LessonKey = "a1-articles", CefrLevel = "A1", Order = 3, Type = "blank", PromptText = "Type the German for this word", Question = "the dog", OptionsJson = "[]", CorrectAnswer = "der Hund", Explanation = "\"the dog\" is \"der Hund\".", Topic = "vocabulary", XpReward = 10 },
                // Lesson: a1-greetings
                new Exercise { Id = Guid.Parse("a1e20001-0000-0000-0000-000000000004"), LessonKey = "a1-greetings", CefrLevel = "A1", Order = 1, Type = "mcq", PromptText = "Pick the correct translation", Question = "How do you say \"Thank you\"?", OptionsJson = "[\"Danke\",\"Bitte\",\"Hallo\",\"Tsch\\u00fcss\"]", CorrectAnswer = "Danke", Explanation = "\"Danke\" means \"thank you\".", Topic = "greetings", XpReward = 10 },
                new Exercise { Id = Guid.Parse("a1e20002-0000-0000-0000-000000000005"), LessonKey = "a1-greetings", CefrLevel = "A1", Order = 2, Type = "blank", PromptText = "Type the German for this phrase", Question = "Good morning", OptionsJson = "[]", CorrectAnswer = "Guten Morgen", Explanation = "\"Good morning\" is \"Guten Morgen\".", Topic = "greetings", XpReward = 10 }
            );
        }
    }
}
