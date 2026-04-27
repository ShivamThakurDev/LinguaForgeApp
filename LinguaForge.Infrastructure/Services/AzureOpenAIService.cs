using Azure;
using Azure.AI.OpenAI;
using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;
using LinguaForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.Json;

namespace LinguaForge.Infrastructure.Services
{
    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly AzureOpenAIOptions _options;

        public AzureOpenAIService(IOptions<AzureOpenAIOptions> options)
        {
            _options = options.Value;
        }

        public async Task<QuizExerciseDto> GenerateExerciseAsync(string topic, string level, string exerciseType, CancellationToken cancellationToken = default)
        {
            if (!CanCallAzure())
            {
                return BuildFallbackExercise(topic, level, exerciseType);
            }

            try
            {
                var chatClient = BuildChatClient();
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a German teacher. Return ONLY valid JSON with keys: exerciseType, question, options [{id,text}], correctOptionId, explanation, promptText. For fill-in exercises, set options to [] and put the expected answer text in correctOptionId."),
                    new UserChatMessage($"Generate one {exerciseType} exercise for CEFR {level} on topic '{topic}'.")
                };

                var completion = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
                var raw = completion.Value.Content[0].Text;

                return ParseExercise(raw) ?? BuildFallbackExercise(topic, level, exerciseType);
            }
            catch
            {
                return BuildFallbackExercise(topic, level, exerciseType);
            }
        }

        public async Task<QuizEvaluationResponseDto> EvaluateExerciseAsync(QuizEvaluationRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!CanCallAzure())
            {
                return BuildFallbackEvaluation(request);
            }

            try
            {
                var chatClient = BuildChatClient();
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a German teacher. Return ONLY valid JSON with keys: isCorrect, scorePercent, earnedXp, feedback, correctedAnswer, weakTopic."),
                    new UserChatMessage($"""
                        Evaluate this German learning answer.
                        Topic: {request.Topic}
                        Level: {request.Level}
                        ExerciseType: {request.ExerciseType}
                        Question: {request.Question}
                        CorrectAnswer: {request.CorrectAnswer}
                        SubmittedAnswer: {request.SubmittedAnswer}
                        PromptText: {request.PromptText}
                        """)
                };

                var completion = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
                var raw = completion.Value.Content[0].Text;

                return ParseEvaluation(raw) ?? BuildFallbackEvaluation(request);
            }
            catch
            {
                return BuildFallbackEvaluation(request);
            }
        }

        public async Task<string> GetChatResponseAsync(IReadOnlyList<ChatMessageDto> conversationHistory, CancellationToken cancellationToken = default)
        {
            if (!CanCallAzure())
            {
                return "I can help you practice German. Try writing one sentence about your day, and I will correct it.";
            }

            try
            {
                var chatClient = BuildChatClient();
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a friendly German tutor. Keep responses concise and actionable.")
                };

                foreach (var item in conversationHistory)
                {
                    var role = item.Role?.Trim().ToLowerInvariant();
                    if (role == "assistant")
                    {
                        messages.Add(new AssistantChatMessage(item.Content));
                    }
                    else
                    {
                        messages.Add(new UserChatMessage(item.Content));
                    }
                }

                var completion = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
                return completion.Value.Content[0].Text;
            }
            catch
            {
                return "I had trouble reaching the tutor model. Please try again in a moment.";
            }
        }

        private ChatClient BuildChatClient()
        {
            var endpoint = new Uri(_options.Endpoint);
            var client = new AzureOpenAIClient(endpoint, new AzureKeyCredential(_options.ApiKey));
            return client.GetChatClient(_options.DeploymentName);
        }

        private bool CanCallAzure() =>
            !string.IsNullOrWhiteSpace(_options.Endpoint) &&
            !string.IsNullOrWhiteSpace(_options.ApiKey) &&
            !string.IsNullOrWhiteSpace(_options.DeploymentName);

        private static QuizExerciseDto? ParseExercise(string raw)
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var dto = new QuizExerciseDto
            {
                ExerciseType = root.GetProperty("exerciseType").GetString() ?? "mcq",
                Question = root.GetProperty("question").GetString() ?? string.Empty,
                CorrectOptionId = root.GetProperty("correctOptionId").GetString() ?? "A",
                Explanation = root.GetProperty("explanation").GetString() ?? string.Empty,
                PromptText = root.TryGetProperty("promptText", out var prompt) ? (prompt.GetString() ?? string.Empty) : string.Empty
            };

            if (root.TryGetProperty("options", out var options))
            {
                foreach (var option in options.EnumerateArray())
                {
                    dto.Options.Add(new QuizOptionDto
                    {
                        Id = option.GetProperty("id").GetString() ?? string.Empty,
                        Text = option.GetProperty("text").GetString() ?? string.Empty
                    });
                }
            }

            return dto;
        }

        private static QuizEvaluationResponseDto? ParseEvaluation(string raw)
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            return new QuizEvaluationResponseDto
            {
                IsCorrect = root.GetProperty("isCorrect").GetBoolean(),
                ScorePercent = root.GetProperty("scorePercent").GetInt32(),
                EarnedXp = root.GetProperty("earnedXp").GetInt32(),
                Feedback = root.GetProperty("feedback").GetString() ?? string.Empty,
                CorrectedAnswer = root.GetProperty("correctedAnswer").GetString() ?? string.Empty,
                WeakTopic = root.GetProperty("weakTopic").GetString() ?? string.Empty
            };
        }

        private static QuizExerciseDto BuildFallbackExercise(string topic, string level, string exerciseType)
        {
            return new QuizExerciseDto
            {
                ExerciseType = exerciseType,
                Question = $"({level}) Choose the correct article for: Hund",
                Options = new List<QuizOptionDto>
                {
                    new() { Id = "A", Text = "der Hund" },
                    new() { Id = "B", Text = "die Hund" },
                    new() { Id = "C", Text = "das Hund" }
                },
                CorrectOptionId = "A",
                Explanation = $"'Hund' is masculine, so use 'der'. Topic: {topic}.",
                PromptText = "Most animals with natural male gender use der."
            };
        }

        private static QuizEvaluationResponseDto BuildFallbackEvaluation(QuizEvaluationRequestDto request)
        {
            var normalizedSubmitted = request.SubmittedAnswer.Trim();
            var normalizedCorrect = request.CorrectAnswer.Trim();
            var isCorrect = string.Equals(normalizedSubmitted, normalizedCorrect, StringComparison.OrdinalIgnoreCase);

            return new QuizEvaluationResponseDto
            {
                IsCorrect = isCorrect,
                ScorePercent = isCorrect ? 100 : 35,
                EarnedXp = isCorrect ? 10 : 0,
                Feedback = isCorrect
                    ? "Correct. Nice work keeping the grammar aligned with the prompt."
                    : $"Not quite. The expected answer is '{request.CorrectAnswer}'. Focus on {request.Topic}.",
                CorrectedAnswer = request.CorrectAnswer,
                WeakTopic = request.Topic
            };
        }
    }
}
