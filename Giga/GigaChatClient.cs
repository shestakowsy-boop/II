using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class GigaChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _scope;
    private readonly string _baseUrl = "https://gigachat.devices.sberbank.ru/api/v1";
    private readonly string _authUrl = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

    private string _accessToken;
    private DateTime _tokenExpiresAt;

    public GigaChatClient(string clientId, string clientSecret, string scope = "GIGACHAT_API_PERS", bool ignoreSslErrors = true)
    {
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));

        var handler = new HttpClientHandler();

        if (ignoreSslErrors)
        {
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            handler = new HttpClientHandler
            {
                // Всегда возвращает true для всех сертификатов
                ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GigaChatClient/1.0");
    }

    public async Task<GigaChatResponse> CompleteChatAsync(string message,
        string model = "GigaChat",
        double temperature = 1.0,
        int maxTokens = 512)
    {
        await EnsureValidTokenAsync();

        try
        {
            var request = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = message }
                },
                temperature = temperature,
                max_tokens = maxTokens,
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Очищаем предыдущий заголовок Authorization
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new GigaChatException($"API Error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<GigaChatApiResponse>(responseContent,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return new GigaChatResponse
            {
                Text = apiResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "Нет ответа",
                Model = apiResponse?.Model,
                Usage = apiResponse?.Usage,
                Created = apiResponse?.Created
            };
        }
        catch (Exception ex) when (!(ex is GigaChatException))
        {
            throw new GigaChatException($"Ошибка при обращении к GigaChat: {ex.Message}", ex);
        }
    }

    private async Task EnsureValidTokenAsync()
    {
        if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiresAt)
        {
            await RefreshTokenAsync();
        }
    }

    private async Task RefreshTokenAsync()
    {
        try
        {
            // ИСПРАВЛЕНИЕ: Правильное формирование Basic Auth заголовка
            var credentials = $"{_clientId}:{_clientSecret}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            // Создаем новый HttpRequestMessage для токена
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _authUrl);

          

            // Сначала устанавливаем Content
            var formParams = new List<KeyValuePair<string, string>>
{
    new KeyValuePair<string, string>("scope", _scope)
};
            tokenRequest.Content = new FormUrlEncodedContent(formParams);

            // Потом добавляем заголовки (БЕЗ Content-Type)
            tokenRequest.Headers.Add("Authorization", $"Basic {_clientSecret}");
            tokenRequest.Headers.Add("RqUID", Guid.NewGuid().ToString());


            tokenRequest.Content = new FormUrlEncodedContent(formParams);

            Console.WriteLine($"Отправляем запрос на получение токена...");
            Console.WriteLine($"URL: {_authUrl}");
            Console.WriteLine($"Scope: {_scope}");
            Console.WriteLine($"Client ID: {_clientId}");
            Console.WriteLine($"Authorization: Basic {encodedCredentials.Substring(0, 10)}...");

            var response = await _httpClient.SendAsync(tokenRequest);

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Статус ответа: {response.StatusCode}");
            Console.WriteLine($"Содержимое ответа: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                throw new GigaChatException($"Ошибка аутентификации: {response.StatusCode} - {responseContent}");
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (tokenResponse?.AccessToken == null)
            {
                throw new GigaChatException($"Не удалось получить токен доступа. Ответ: {responseContent}");
            }

            _accessToken = tokenResponse.AccessToken;
           // _tokenExpiresAt = DateTime.Parse(tokenResponse.ExpiresAt );

           // Console.WriteLine($"Токен получен успешно. Истекает: {_tokenExpiresAt}");
        }
        catch (Exception ex) when (!(ex is GigaChatException))
        {
            throw new GigaChatException($"Ошибка при обновлении токена: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Модели данных
public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
}

public class GigaChatResponse
{
    public string Text { get; set; }
    public string Model { get; set; }
    public GigaChatUsage Usage { get; set; }
    public long? Created { get; set; }
}

public class GigaChatException : Exception
{
    public GigaChatException(string message) : base(message) { }
    public GigaChatException(string message, Exception innerException) : base(message, innerException) { }
}

// Внутренние модели для десериализации
internal class GigaChatApiResponse
{
    public string Model { get; set; }
    public GigaChatChoice[] Choices { get; set; }
    public GigaChatUsage Usage { get; set; }
    public long Created { get; set; }
}

internal class GigaChatChoice
{
    public GigaChatMessage Message { get; set; }
}

internal class GigaChatMessage
{
    public string Content { get; set; }
}

public class GigaChatUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

internal class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }
}