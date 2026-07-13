namespace LinguaForge.Domain.Entities
{
    /// <summary>
    /// A single graded exercise that belongs to a lesson (identified by LessonKey).
    /// The correct answer lives here on the server and is never sent to the client,
    /// so scoring is authoritative and cannot be tampered with from the browser.
    /// </summary>
    public class Exercise
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string LessonKey { get; set; } = string.Empty;
        public string CefrLevel { get; set; } = "A1";
        public int Order { get; set; }

        /// <summary>"mcq" (multiple choice) or "blank" (type the answer).</summary>
        public string Type { get; set; } = "mcq";

        /// <summary>Instruction shown to the learner, e.g. "Choose the correct article".</summary>
        public string PromptText { get; set; } = string.Empty;

        /// <summary>The question itself, e.g. "___ Hund".</summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>JSON array of option strings for mcq; empty for blank.</summary>
        public string OptionsJson { get; set; } = "[]";

        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public int XpReward { get; set; } = 10;
    }
}
