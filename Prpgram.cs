
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        await Example1_SimpleIgnoreSSL();
      //  await Example2_GlobalSSLConfig();
        await Example3_ProductionSSLConfig();
    }

    // Пример 1: Простое игнорирование SSL ошибок (для разработки)
    static async Task Example1_SimpleIgnoreSSL()
    {
        Console.WriteLine("=== Пример 1: Игнорирование SSL ошибок ===");

        var client = new GigaChatClient(
"4253e9c1-0929-4e42-a04c-9bb5abab7073",
 "NDI1M2U5YzEtMDkyOS00ZTQyLWEwNGMtOWJiNWFiYWI3MDczOjE4Mzg4MWQ1LTIwNWItNDVjZS1iMDNjLTBkZTYxM2IzNTU2Yw==",

    ignoreSslErrors: true // Игнорируем SSL ошибки
        );

        try
        {
            var response = await client.CompleteChatAsync("Привет! Расскажи о себе");
            Console.WriteLine($"Ответ: {response.Text}");
        }
        catch (GigaChatException ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        finally
        {
            client.Dispose();
        }
    }

    //// Пример 2: Глобальная настройка SSL
    //static async Task Example2_GlobalSSLConfig()
    //{
    //    Console.WriteLine("\n=== Пример 2: Глобальная настройка SSL ===");

    //    // Глобально настраиваем SSL для доменов Сбера
    //    SslHelper.ConfigureSSLForSberDomains();

    //    var client = new GigaChatClient(
    //        clientId: "your-client-id",
    //        clientSecret: "your-client-secret",
    //        ignoreSslErrors: false // Используем кастомную проверку
    //    );

    //    try
    //    {
    //        var response = await client.CompleteChatAsync("Какая погода в Москве?");
    //        Console.WriteLine($"Ответ: {response.Text}");
    //    }
    //    catch (GigaChatException ex)
    //    {
    //        Console.WriteLine($"Ошибка: {ex.Message}");
    //    }
    //    finally
    //    {
    //        client.Dispose();
    //    }
    //}

    // Пример 3: Безопасная конфигурация для продакшена
    static async Task Example3_ProductionSSLConfig()
    {
        Console.WriteLine("\n=== Пример 3: Продакшенная конфигурация ===");

        var client = new GigaChatClient(
            clientId: "your-client-id",
            clientSecret: "your-client-secret",
            ignoreSslErrors: false // Не игнорируем SSL в продакшене
        );

        try
        {
            var response = await client.CompleteChatAsync("Расскажи про безопасность");
            Console.WriteLine($"Ответ: {response.Text}");
        }
        catch (GigaChatException ex)
        {
            if (ex.Message.Contains("SSL") || ex.Message.Contains("certificate"))
            {
                Console.WriteLine("SSL ошибка! Рекомендации:");
                Console.WriteLine("1. Установите корневые сертификаты Минцифры РФ");
                Console.WriteLine("2. Обновите операционную систему");
                Console.WriteLine("3. Для тестирования используйте ignoreSslErrors=true");
            }
            else
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
        finally
        {
            client.Dispose();
        }
    }
}

// Класс для работы с SSL сертификатами
public static class CertificateManager
{
    /// <summary>
    /// Проверяет, установлены ли корневые сертификаты Минцифры РФ
    /// </summary>
    public static bool AreRussianCertificatesInstalled()
    {
        try
        {
            using (var store = new System.Security.Cryptography.X509Certificates.X509Store(
                System.Security.Cryptography.X509Certificates.StoreName.Root,
                System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser))
            {
                store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly);

                var certificates = store.Certificates;
                foreach (var cert in certificates)
                {
                    if (cert.Subject.Contains("Russian Trust Network") ||
                        cert.Subject.Contains("Минцифры") ||
                        cert.Issuer.Contains("ПАО Сбербанк"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Выводит инструкции по установке сертификатов
    /// </summary>
    public static void PrintCertificateInstallationInstructions()
    {
        Console.WriteLine("=== Инструкции по установке сертификатов ===");
        Console.WriteLine("1. Скачайте корневые сертификаты с сайта Минцифры РФ:");
        Console.WriteLine("   https://digital.gov.ru/ru/activity/directions/883/");
        Console.WriteLine();
        Console.WriteLine("2. Для Windows:");
        Console.WriteLine("   - Запустите certmgr.msc");
        Console.WriteLine("   - Доверенные корневые центры сертификации");
        Console.WriteLine("   - Установите сертификаты");
        Console.WriteLine();
        Console.WriteLine("3. Для Linux:");
        Console.WriteLine("   - Скопируйте .crt файлы в /usr/local/share/ca-certificates/");
        Console.WriteLine("   - Выполните: sudo update-ca-certificates");
        Console.WriteLine();
        Console.WriteLine("4. Альтернатива для разработки:");
        Console.WriteLine("   - Используйте ignoreSslErrors=true");
    }
}

// Расширенный пример с обработкой ошибок SSL
public class RobustGigaChatClient
{
    private readonly GigaChatClient _client;
    private readonly bool _fallbackToIgnoreSSL;

    public RobustGigaChatClient(string clientId, string clientSecret, bool fallbackToIgnoreSSL = true)
    {
        _fallbackToIgnoreSSL = fallbackToIgnoreSSL;

        // Сначала пытаемся использовать безопасный способ
        _client = new GigaChatClient(clientId, clientSecret, ignoreSslErrors: false);
    }

    public async Task<GigaChatResponse> CompleteChatAsync(string message)
    {
        try
        {
            // Пытаемся выполнить запрос с проверкой SSL
            return await _client.CompleteChatAsync(message);
        }
        catch (GigaChatException ex) when (ex.Message.Contains("SSL") && _fallbackToIgnoreSSL)
        {
            Console.WriteLine("SSL ошибка, переключаемся на небезопасный режим...");

            // Создаем новый клиент с отключенной проверкой SSL
            _client.Dispose();
            var fallbackClient = new GigaChatClient(
                _client.GetType().GetField("_clientId",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)?.GetValue(_client) as string,
                _client.GetType().GetField("_clientSecret",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)?.GetValue(_client) as string,
                ignoreSslErrors: true
            );

            return await fallbackClient.CompleteChatAsync(message);
        }
    }
}