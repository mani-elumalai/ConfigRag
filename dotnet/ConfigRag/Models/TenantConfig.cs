namespace ConfigRag.Models;

public class TenantConfig
{
    public string TenantId { get; set; }
    public Dictionary<string, object> Config { get; set; } = new();
}