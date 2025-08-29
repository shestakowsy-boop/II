using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class GeminiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

    public GeminiApiClient(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    // Классы для сериализации запроса
    public class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public Part[] Parts { get; set; }
    }

    public class GenerateContentRequest
    {
        [JsonPropertyName("contents")]
        public Content[] Contents { get; set; }

        // Опционально: можно добавить generationConfig и safetySettings
        // [JsonPropertyName("generationConfig")]
        // public GenerationConfig GenerationConfig { get; set; }
        // [JsonPropertyName("safetySettings")]
        // public SafetySettings[] SafetySettings { get; set; }
    }

    // Классы для десериализации ответа
    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; }
        // Можно добавить другие поля, такие как finishReason
    }

    public class GenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public Candidate[] Candidates { get; set; }
        // Можно добавить другие поля, такие как promptFeedback
    }

    public async Task<string> GenerateText(string prompt, string model = "gemini-pro")
    {
        string requestUri = $"{ApiBaseUrl}{model}:generateContent?key={_apiKey}";

        var requestBody = new GenerateContentRequest
        {
            Contents = new Content[]
            {
                new Content
                {
                    Parts = new Part[]
                    {
                        new Part { Text = prompt }
                    }
                }
            }
        };

        string jsonBody = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
            response.EnsureSuccessStatusCode(); // Выбрасывает исключение при неуспешном коде HTTP

            string responseBody = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GenerateContentResponse>(responseBody);

            if (geminiResponse?.Candidates != null && geminiResponse.Candidates.Length > 0)
            {
                return geminiResponse.Candidates[0].Content.Parts[0].Text;
            }
            else
            {
                return "Не удалось сгенерировать контент.";
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Ошибка при выполнении HTTP-запроса: {e.Message}");
            if (e.StatusCode.HasValue)
            {
                Console.WriteLine($"Код статуса: {e.StatusCode.Value}");
            }
            return null;
        }
        catch (JsonException e)
        {
            Console.WriteLine($"Ошибка при десериализации JSON: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Произошла непредвиденная ошибка: {e.Message}");
            return null;
        }
    }

    public static async Task grMain(string[] args)
    {
        // Замените "YOUR_API_KEY" на ваш реальный API-ключ Gemini
        // Рекомендуется хранить ключ в переменных окружения или других безопасных местах.
        string geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "YOUR_API_KEY";

        if (geminiApiKey == "YOUR_API_KEY")
        {
            Console.WriteLine("Пожалуйста, установите переменную окружения GEMINI_API_KEY или замените 'YOUR_API_KEY' на ваш ключ.");
            return;
        }

        var client = new GeminiApiClient(geminiApiKey);
        string prompt = "Напиши короткое стихотворение о весне.";

        Console.WriteLine($"Отправляем запрос в Gemini с промптом: \"{prompt}\"");
        string generatedText = await client.GenerateText(prompt);

        if (generatedText != null)
        {
            Console.WriteLine("\nОтвет от Gemini:");
            Console.WriteLine(generatedText);
        }
    }
}
