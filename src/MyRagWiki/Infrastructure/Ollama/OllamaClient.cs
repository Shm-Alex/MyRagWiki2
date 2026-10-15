using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MyRagWiki.Infrastructure.Ollama;

public class OllamaClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public OllamaClient(string baseUrl)
    {
        _baseUrl = baseUrl;
        _http = new HttpClient();

        // ⚡ УВЕЛИЧИВАЕМ ТАЙМАУТ ДО 10 МИНУТ
        // Локальным LLM нужно время на загрузку модели в память при первом запросе
        _http.Timeout = TimeSpan.FromMinutes(10);
    }

    public async Task<bool> IsAliveAsync()
    {
        try
        {
            // Для проверки alive ставим короткий таймаут отдельно, если нужно, 
            // но здесь оставим как есть, это быстрый запрос
            var resp = await _http.GetAsync($"{_baseUrl}/api/tags");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<float[]> GetEmbeddingAsync(string text, string model)
    {
        var req = new { model, input = new[] { text } };
        var content = new StringContent(
            JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

        var resp = await _http.PostAsync($"{_baseUrl}/api/embed", content);
        var json = await resp.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("embeddings", out var embeddings)
            && embeddings.GetArrayLength() > 0)
        {
            var first = embeddings[0];
            var result = new float[first.GetArrayLength()];
            for (int i = 0; i < result.Length; i++)
                result[i] = first[i].GetSingle();
            return result;
        }
        return Array.Empty<float>();
    }

    public async Task<string> GenerateAsync(string prompt, string model)
    {
        var req = new { model, prompt, stream = false };
        var content = new StringContent(
            JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

        // Этот вызов теперь будет ждать до 10 минут
        var resp = await _http.PostAsync($"{_baseUrl}/api/generate", content);
        var json = await resp.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("response", out var r)
            ? r.GetString() ?? ""
            : json;
    }
}