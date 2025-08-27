using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace II.Yandex;
class YandexProgram
{
    static async Task YandexMain(string[] args)
    {
        // Ваши учетные данные из Yandex Cloud
        string folderId = "b1g2ab3cd4ef567890gh"; // ID папки из Yandex Cloud
        string apiKey = "AQVN..."; // API ключ

        var client = new YandexGPTClient(folderId, apiKey);

        try
        {
            // Простой запрос
            await SimpleChat(client);

            // Чат с историей
            await ChatWithHistory(client);

            // Чат с системным промптом
            await ChatWithSystemPrompt(client);
        }
        catch (YandexGPTException ex)
        {
            Console.WriteLine($"Ошибка YandexGPT: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Общая ошибка: {ex.Message}");
        }
        finally
        {
            client.Dispose();
        }
    }

    static async Task SimpleChat(YandexGPTClient client)
    {
        Console.WriteLine("=== Простой чат ===");

        var response = await client.CompleteChatAsync(
            "Расскажи интересный факт о космосе"
        );

        Console.WriteLine($"Ответ: {response.Text}");
        Console.WriteLine($"Токенов использовано: {response.Usage?.totalTokens}");
        Console.WriteLine();
    }

    static async Task ChatWithHistory(YandexGPTClient client)
    {
        Console.WriteLine("=== Чат с историей ===");

        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Text = "Привет! Как дела?" },
            new ChatMessage { Role = "assistant", Text = "Привет! Дела отлично, спасибо! Чем могу помочь?" },
            new ChatMessage { Role = "user", Text = "Расскажи короткую шутку" }
        };

        var response = await client.CompleteChatWithHistoryAsync(messages);

        Console.WriteLine($"Ответ: {response.Text}");
        Console.WriteLine();
    }

    static async Task ChatWithSystemPrompt(YandexGPTClient client)
    {
        Console.WriteLine("=== Чат с системным промптом ===");

        var response = await client.CompleteChatAsync(
            prompt: "Объясни принцип работы нейронных сетей",
            systemPrompt: "Ты - опытный преподаватель информатики. Объясняй сложные темы простым языком с примерами.",
            temperature: 0.7,
            maxTokens: 1500
        );

        Console.WriteLine($"Ответ: {response.Text}");
        Console.WriteLine();
    }
}

// Пример использования сессии чата
class ChatSessionExample
{
    public static async Task RunChatSession()
    {
        string folderId = "your-folder-id";
        string apiKey = "your-api-key";

        var client = new YandexGPTClient(folderId, apiKey);
        var session = new YandexGPTChatSession(
            client,
            "Ты - полезный AI ассистент, который отвечает кратко и по делу."
        );

        try
        {
            Console.WriteLine("Начинаем чат сессию:");

            var response1 = await session.SendMessageAsync("Привет!");
            Console.WriteLine($"AI: {response1}");

            var response2 = await session.SendMessageAsync("Какая сегодня погода?");
            Console.WriteLine($"AI: {response2}");

            var response3 = await session.SendMessageAsync("А что ты говорил в прошлом сообщении?");
            Console.WriteLine($"AI: {response3}");

        }
        finally
        {
            client.Dispose();
        }
    }
}