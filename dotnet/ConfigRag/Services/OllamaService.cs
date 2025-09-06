using System.Text.Json;

namespace ConfigRag.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://localhost:11434/");
    }

    public async Task<float[]> EmbedAsync(string text)
    {
        var resp = await _httpClient.PostAsJsonAsync("api/embeddings", new
        {
            model = "embeddinggemma",
            prompt = text
        });

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        //_logger.LogInformation("Embedding API response: {json}", json);
        return json.GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }

    public async Task<string> ChatAsync(string context, string question)
    {
        var resp = await _httpClient.PostAsJsonAsync("api/chat", new
        {
            model = "llama3",
            messages = new object[]
            {
                new { role = "system", content = "You are a helpful assistant for tenant configs." },
                new { role = "user", content = $"Context:\n{context}\n\nQuestion: {question}" }
            },
            stream = false // Disable streaming to get a single JSON response
        });
        
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        _logger.LogInformation("Chat API response: {json}", json);
        return json.GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(string context, string question)
    {
        var resp = await _httpClient.PostAsJsonAsync("api/chat", new
        {
            model = "llama3",
            messages = new object[]
            {
                new { role = "system", content = "You are a helpful assistant for tenant configs." },
                new { role = "user", content = $"Context:\n{context}\n\nQuestion: {question}" }
            },
            stream = true // Enable streaming
        });

        using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var contentStr = ParseStreamingLine(line);
            if (!string.IsNullOrEmpty(contentStr))
            {
                yield return contentStr;
            }
        }
    }

    private string? ParseStreamingLine(string line)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(line);
            
            // Check if this is the final message
            if (json.TryGetProperty("done", out var done) && done.GetBoolean())
            {
                return null; // Signal end of stream
            }

            // Extract the content from the streaming response
            if (json.TryGetProperty("message", out var message) && 
                message.TryGetProperty("content", out var content))
            {
                return content.GetString();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Failed to parse streaming JSON: {line}, Error: {error}", line, ex.Message);
        }

        return null;
    }
}