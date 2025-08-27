using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace II.Yandex;
public class YandexGPTClient
{
    private readonly HttpClient _httpClient;
    private readonly string _folderId;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

    public YandexGPTClient(string folderId, string apiKey)
    {
        _folderId = folderId ?? throw new ArgumentNullException(nameof(folderId));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Api-Key {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("x-folder-id", _folderId);
    }

    public async Task<YandexGPTResponse> CompleteChatAsync(string prompt,
        string systemPrompt = null,
        double temperature = 0.6,
        int maxTokens = 2000)
    {
        try
        {
            var request = CreateCompletionRequest(prompt, systemPrompt, temperature, maxTokens);
            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_baseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new YandexGPTException($"API Error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<YandexGPTApiResponse>(responseContent);

            return new YandexGPTResponse
            {
                Text = apiResponse?.result?.alternatives?[0]?.message?.text ?? "Нет ответа",
                Usage = apiResponse?.result?.usage
            };
        }
        catch (Exception ex)
        {
            throw new YandexGPTException($"Ошибка при обращении к YandexGPT: {ex.Message}", ex);
        }
    }

    public async Task<YandexGPTResponse> CompleteChatWithHistoryAsync(
        List<ChatMessage> messages,
        double temperature = 0.6,
        int maxTokens = 2000)
    {
        try
        {
            var request = new
            {
                modelUri = $"gpt://{_folderId}/yandexgpt-lite",
                completionOptions = new
                {
                    stream = false,
                    temperature = temperature,
                    maxTokens = maxTokens
                },
                messages = messages.Select(m => new
                {
                    role = m.Role,
                    text = m.Text
                }).ToArray()
            };

            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_baseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new YandexGPTException($"API Error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<YandexGPTApiResponse>(responseContent);

            return new YandexGPTResponse
            {
                Text = apiResponse?.result?.alternatives?[0]?.message?.text ?? "Нет ответа",
                Usage = apiResponse?.result?.usage
            };
        }
        catch (Exception ex)
        {
            throw new YandexGPTException($"Ошибка при обращении к YandexGPT: {ex.Message}", ex);
        }
    }

    private object CreateCompletionRequest(string prompt, string systemPrompt, double temperature, int maxTokens)
    {
        var messages = new List<object>();

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new
            {
                role = "system",
                text = systemPrompt
            });
        }

        messages.Add(new
        {
            role = "user",
            text = prompt
        });

        return new
        {
            modelUri = $"gpt://{_folderId}/yandexgpt-lite",
            completionOptions = new
            {
                stream = false,
                temperature = temperature,
                maxTokens = maxTokens
            },
            messages = messages.ToArray()
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Модели данных
public class ChatMessage
{
    public string Role { get; set; } // "system", "user", "assistant"
    public string Text { get; set; }
}

public class YandexGPTResponse
{
    public string Text { get; set; }
    public Usage Usage { get; set; }
}

public class YandexGPTException : Exception
{
    public YandexGPTException(string message) : base(message) { }
    public YandexGPTException(string message, Exception innerException) : base(message, innerException) { }
}

// Внутренние модели для десериализации API ответа
internal class YandexGPTApiResponse
{
    public Result result { get; set; }
}

internal class Result
{
    public Alternative[] alternatives { get; set; }
    public Usage usage { get; set; }
    public string modelVersion { get; set; }
}

internal class Alternative
{
    public Message message { get; set; }
    public string status { get; set; }
}

internal class Message
{
    public string role { get; set; }
    public string text { get; set; }
}

public class Usage
{
    public string inputTextTokens { get; set; }
    public string completionTokens { get; set; }
    public string totalTokens { get; set; }
}