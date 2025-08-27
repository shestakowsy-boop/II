namespace II.Yandex;
// Дополнительный класс для удобной работы с чатом
public class YandexGPTChatSession
{
    private readonly YandexGPTClient _client;
    private readonly List<ChatMessage> _history;
    private readonly string _systemPrompt;

    public YandexGPTChatSession(YandexGPTClient client, string systemPrompt = null)
    {
        _client = client;
        _history = new List<ChatMessage>();
        _systemPrompt = systemPrompt;

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            _history.Add(new ChatMessage { Role = "system", Text = systemPrompt });
        }
    }

    public async Task<string> SendMessageAsync(string userMessage)
    {
        _history.Add(new ChatMessage { Role = "user", Text = userMessage });

        var response = await _client.CompleteChatWithHistoryAsync(_history);

        _history.Add(new ChatMessage { Role = "assistant", Text = response.Text });

        return response.Text;
    }

    public void ClearHistory()
    {
        _history.Clear();
        if (!string.IsNullOrEmpty(_systemPrompt))
        {
            _history.Add(new ChatMessage { Role = "system", Text = _systemPrompt });
        }
    }

    public List<ChatMessage> GetHistory()
    {
        return new List<ChatMessage>(_history);
    }
}
