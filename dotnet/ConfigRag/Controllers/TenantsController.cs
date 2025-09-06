namespace ConfigRag.Controllers;

using Microsoft.AspNetCore.Mvc;
using Services;
using Models;
using System.Text.Json;

[ApiController]
[Route("[controller]")]
public class TenantsController : ControllerBase
{
    private readonly QdrantService _qdrant;
    private readonly OllamaService _ollama;

    public TenantsController(QdrantService qdrant, OllamaService ollama)
    {
        _qdrant = qdrant;
        _ollama = ollama;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] TenantConfig input)
    {
        var vector = await _ollama.EmbedAsync(JsonSerializer.Serialize(input.Config));
        await _qdrant.UpsertTenantAsync(input.TenantId, vector, input.Config);
        return Ok(new { status = "ok", input.TenantId });
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequest body)
    {
        var question = body.Question;

        // 1. Embed the question
        var qVector = await _ollama.EmbedAsync(question);

        // 2. Search for similar tenant configs
        var results = await _qdrant.SearchAsync(qVector, 5);

        // 3. Build context string for LLM
        var context = string.Join("\n", results.Select(r =>
            $"{r.TenantId}: {JsonSerializer.Serialize(r.Config)}"
        ));

        // 4. Ask Ollama with context + user question
        var answer = await _ollama.ChatAsync(context, question);

        return Ok(new
        {
            question,
            answer,
            matches = results.Select(r => new
            {
                tenantId = r.TenantId,
                config = r.Config
            })
        });
    }

    [HttpPost("ask-stream")]
    public async Task AskStream([FromBody] ChatRequest body)
    {
        var question = body.Question;

        // 1. Embed the question
        var qVector = await _ollama.EmbedAsync(question);

        // 2. Search for similar tenant configs
        var results = await _qdrant.SearchAsync(qVector, 5);

        // 3. Build context string for LLM
        var context = string.Join("\n", results.Select(r =>
            $"{r.TenantId}: {JsonSerializer.Serialize(r.Config)}"
        ));

        // Set response headers for streaming
        Response.ContentType = "text/plain; charset=utf-8";
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        // 4. Stream the response from Ollama
        await foreach (var chunk in _ollama.ChatStreamAsync(context, question))
        {
            await Response.WriteAsync(chunk);
            await Response.Body.FlushAsync();
        }
    }
}
