using System.Text;
using System.Text.Json;

namespace Proiect_Licenta.Services
{
    public class CommentModerationService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<CommentModerationService> _logger;

        public CommentModerationService(HttpClient http, IConfiguration config, ILogger<CommentModerationService> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        public async Task<bool> IsSafeAsync(string comment)
        {
            var request = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new {
                        role = "system",
                        content = "You are an advanced multilingual content moderation engine. Your sole task is to determine if a comment violates basic safety guidelines (hate speech, severe insults, spam, illegal content, or explicit threats).\n\n" +
                                  "CRITICAL RULES:\n" +
                                  "1. Languages: The comment might be in Romanian, English, or other languages. Accept all languages equally.\n" +
                                  "2. Typos & Slang: Do not reject a comment for bad grammar, misspellings, or innocent regional slang.\n" +
                                  "3. Evaluation: If a comment is safe, friendly, a normal question, or constructive criticism, it is safe.\n\n" +
                                  "OUTPUT FORMAT: Reply with exactly 'YES' if the comment is safe to post. Reply with exactly 'NO' if it contains explicit toxicity or safety violations. Do not include punctuation or any other text."
                    },
                    new
                    {
                        role = "user",
                        content = $"Comment to evaluate: \"{comment}\""
                    }
                }
            };

            var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/chat/completions"
            );

            httpRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    _config["OpenAI:ApiKey"]
                );

            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await _http.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];

                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        var answer = content.GetString()?.Trim().ToUpperInvariant().Replace(".", "");

                        if (string.IsNullOrEmpty(answer))
                        {
                            return false;
                        }

                        return answer == "YES";
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Moderation failed");
                return false;
            }
        }
    }
}