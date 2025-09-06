namespace ConfigRag.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Grpc.Net.Client;

public class TenantSearchResult
{
    public string TenantId { get; set; } = string.Empty;
    public Dictionary<string, object> Config { get; set; } = new();
}
public class QdrantService
{
    private readonly QdrantClient _client;
    private readonly string _collectionName = "tenants";

    public QdrantService()
    {
        // Use the REST API port (6333) that you have exposed in Docker
        _client = new QdrantClient("localhost", 6334, https: false);

        try
        {
            _client.CreateCollectionAsync(_collectionName, new VectorParams
            {
                Size = 768, // embedding size for nomic-embed-text
                Distance = Distance.Cosine
            }).Wait();
        }
        catch
        {
            // Collection already exists
        }
    }

    private static Guid ToDeterministicGuid(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));

        return new Guid(bytes.Take(16).ToArray());
    }

    public async Task UpsertTenantAsync(string tenantId, float[] vector, Dictionary<string, object> config)
    {
        var point = new PointStruct
        {
            Id = ToDeterministicGuid(tenantId),
            Vectors =  vector 
        };

        // Add payload values explicitly using Value fields
        point.Payload.Add("tenantId", new Value { StringValue = tenantId });

        // Store the whole config as JSON string (easy to deserialize later)
        var configJson = JsonSerializer.Serialize(config);
        point.Payload.Add("config", new Value { StringValue = configJson });

        await _client.UpsertAsync(_collectionName, new[] { point });
    }

    public async Task<List<TenantSearchResult>>  SearchAsync(float[] queryVector, int limit = 5)
    {
        var results = await _client.SearchAsync(_collectionName, queryVector, limit: (ulong)limit);

        var output = new List<TenantSearchResult>();

        foreach (var r in results)
        {
            string tenantId = "";
            if (r.Payload.TryGetValue("tenantId", out var tVal))
            {
                tenantId = tVal?.StringValue ?? "";
            }

            string configJson = "{}";
            if (r.Payload.TryGetValue("config", out var cVal))
            {
                configJson = cVal?.StringValue ?? "{}";
            }

            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson) ?? new Dictionary<string, object>();

            output.Add(new TenantSearchResult
            {
                TenantId = tenantId,
                Config = config
            });
        }

        return output;
    }
}
