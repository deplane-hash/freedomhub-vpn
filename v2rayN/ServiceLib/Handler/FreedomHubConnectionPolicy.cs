using System.Web;

namespace ServiceLib.Handler;

public static class FreedomHubConnectionPolicy
{
    public const string Address = "62.171.165.206";
    public const int Port = 443;
    public const string ServerName = "dl.google.com";

    public static bool IsAllowedLink(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) || uri.Scheme != "vless"
            || uri.Host != Address || uri.Port != Port || !Guid.TryParse(uri.UserInfo, out _)) return false;
        var query = HttpUtility.ParseQueryString(uri.Query);
        return query["security"] == "reality" && query["type"] == "tcp"
            && query["sni"] == ServerName && query["flow"] == "xtls-rprx-vision"
            && !string.IsNullOrWhiteSpace(query["pbk"]);
    }

    public static bool IsAllowed(ProfileItem profile) => profile.ConfigType == EConfigType.VLESS
        && profile.Address == Address && profile.Port == Port && profile.StreamSecurity == "reality"
        && profile.Sni == ServerName && profile.GetProtocolExtra().Flow == "xtls-rprx-vision"
        && (profile.Network == "tcp" || profile.Network == "raw");
}
