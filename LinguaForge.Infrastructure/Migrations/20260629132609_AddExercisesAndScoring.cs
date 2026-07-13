using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LinguaForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExercisesAndScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExerciseId",
                table: "QuizAttempts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CefrLevel = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PromptText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Question = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    XpReward = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Exercises",
                columns: new[] { "Id", "CefrLevel", "CorrectAnswer", "Explanation", "LessonKey", "OptionsJson", "Order", "PromptText", "Question", "Topic", "Type", "XpReward" },
                values: new object[,]
                {
                    { new Guid("a1e10001-0000-0000-0000-000000000001"), "A1", "der", "\"Hund\" is masculine, so it takes \"der\".", "a1-articles", "[\"der\",\"die\",\"das\"]", 1, "Choose the correct article", "___ Hund", "articles", "mcq", 10 },
                    { new Guid("a1e10002-0000-0000-0000-000000000002"), "A1", "die", "\"Katze\" is feminine, so it takes \"die\".", "a1-articles", "[\"der\",\"die\",\"das\"]", 2, "Choose the correct article", "___ Katze", "articles", "mcq", 10 },
                    { new Guid("a1e10003-0000-0000-0000-000000000003"), "A1", "der Hund", "\"the dog\" is \"der Hund\".", "a1-articles", "[]", 3, "Type the German for this word", "the dog", "vocabulary", "blank", 10 },
                    { new Guid("a1e20001-0000-0000-0000-000000000004"), "A1", "Danke", "\"Danke\" means \"thank you\".", "a1-greetings", "[\"Danke\",\"Bitte\",\"Hallo\",\"Tsch\\u00fcss\"]", 1, "Pick the correct translation", "How do you say \"Thank you\"?", "greetings", "mcq", 10 },
                    { new Guid("a1e20002-0000-0000-0000-000000000005"), "A1", "Guten Morgen", "\"Good morning\" is \"Guten Morgen\".", "a1-greetings", "[]", 2, "Type the German for this phrase", "Good morning", "greetings", "blank", 10 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_CefrLevel_LessonKey_Order",
                table: "Exercises",
                columns: new[] { "CefrLevel", "LessonKey", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "QuizAttempts");
        }
    }
}
