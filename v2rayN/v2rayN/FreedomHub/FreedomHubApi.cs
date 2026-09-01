using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace v2rayN.Views;

/// <summary>Minimal client for the FreedomHub VPN web API (client_login / client_config).</summary>
public sealed class FreedomHubAccount
{
    public bool Success { get; set; }
    public string Error { get; set; } = "";
    public string Token { get; set; } = "";
    public string Plan { get; set; } = "";
    public double QuotaGb { get; set; }
    public double UsedGb { get; set; }
    public double RemainingGb { get; set; }
    public bool OverQuota { get; set; }
    public string Week { get; set; } = "";
    public string Resets { get; set; } = "";
    public string VlessDirectDe { get; set; } = "";
    public string VlessDirectNl { get; set; } = "";
    public string VlessCdn { get; set; } = "";
}

public static class FreedomHubApi
{
    private static readonly string[] BaseHosts =
    [
        "https://freedomhub.at",
        "https://freedomhub.nothingbox.net",
    ];

    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All | DecompressionMethods.GZip,
            AllowAutoRedirect = false,
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "FreedomHubVPN/14");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "no-cache");
        return client;
    }

    public static async Task<string?> GetBaseAsync()
    {
        foreach (var host in BaseHosts)
        {
            try
            {
                using var resp = await Client.GetAsync(host + "/vpn.php?api=status");
                if (resp.IsSuccessStatusCode) return host;
            }
            catch
            {
                // try next host
            }
        }
        return null;
    }

    public static async Task<FreedomHubAccount> LoginAsync(string baseUrl, string username, string password)
    {
        var payload = JsonSerializer.Serialize(new { username, password });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var resp = await Client.PostAsync(baseUrl + "/vpn.php?api=client_login", content);
        return ParseAccount(resp);
    }

    public static async Task<FreedomHubAccount> ConfigAsync(string baseUrl, string token)
    {
        using var resp = await Client.GetAsync(baseUrl + "/vpn.php?api=client_config&key=" + Uri.EscapeDataString(token));
        return ParseAccount(resp);
    }

    private static FreedomHubAccount ParseAccount(HttpResponseMessage resp)
    {
        var account = new FreedomHubAccount();
        string raw;
        try
        {
            raw = resp.IsSuccessStatusCode ? resp.Content.ReadAsStringAsync().GetAwaiter().GetResult() : "";
        }
        catch
        {
            raw = "";
        }

        if (string.IsNullOrWhiteSpace(raw)) return account;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            account.Success = root.TryGetProperty("success", out var ok) && ok.ValueKind == JsonValueKind.True;
            account.Error = ReadString(root, "error");
            account.Token = ReadString(root, "token");
            account.Plan = ReadString(root, "plan");
            account.Week = ReadString(root, "week");
            account.Resets = ReadString(root, "resets");
            account.VlessCdn = ReadString(root, "vless_link_cdn");
            account.OverQuota = root.TryGetProperty("over_quota", out var oq) && oq.ValueKind == JsonValueKind.True;
            account.QuotaGb = ReadDouble(root, "quota_gb");
            account.UsedGb = ReadDouble(root, "used_gb");
            account.RemainingGb = ReadDouble(root, "remaining_gb");
            account.VlessDirectDe = ReadString(root, "vless_link_de");
            account.VlessDirectNl = ReadString(root, "vless_link_nl");
            if (account.VlessDirectDe == "") account.VlessDirectDe = ReadString(root, "vless_link_direct");
        }
        catch
        {
            account.Error = "Could not read the server response.";
        }

        return account;
    }

    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static double ReadDouble(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.Number or JsonValueKind.String
            && double.TryParse(value.ToString(), out var parsed) ? parsed : 0;
}