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
            // Pregătim request-ul pentru OpenAI
            var request = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new {
                        role = "system",
                        content = "You are a strict content moderation assistant. Only answer with YES if the comment is completely safe, respectful, and follows community guidelines. Answer NO otherwise. Do not add anything else."
                    },
                    new
                    {
                        role = "user",
                        content = $"Comment to check: \"{comment}\""
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

                // ✅ Varianta sigură: verificăm dacă proprietățile există
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];

                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        var answer = content.GetString()?.Trim().ToUpperInvariant().Replace(".", "");

                        if (string.IsNullOrEmpty(answer))
                        {
                            // fallback: dacă nu primim răspuns valid → nu permitem comentariul
                            return false;
                        }

                        // doar dacă AI zice YES → comentariul e safe
                        return answer == "YES";
                    }
                }

                // fallback: dacă structura JSON nu e cum ne așteptăm
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Moderation failed");
                return false; // sau true, depinde de politica ta
            }
        }
    }
}
