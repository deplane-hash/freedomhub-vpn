using ServiceLib.Handler;
using ServiceLib.Handler.Fmt;

var valid = "vless://12345678-1234-4123-8123-123456789abc@62.171.165.206:443?encryption=none&security=reality&type=tcp&sni=dl.google.com&flow=xtls-rprx-vision&pbk=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA&sid=12345678&fp=chrome";
void Expect(bool result, string name) { if (!result) throw new Exception(name); }
Expect(FreedomHubConnectionPolicy.IsAllowedLink(valid), "Correct REALITY URI rejected");
foreach (var invalid in new[] {valid.Replace("security=reality", "security=none"), valid.Replace(":443?", ":44300?"), valid.Replace("62.171.165.206", "5.45.110.86"), valid.Replace("type=tcp", "type=ws"), valid.Replace("dl.google.com", "microsoft.com"), valid.Replace("flow=xtls-rprx-vision", "flow="), valid.Replace("vless://", "vmess://")})
    Expect(!FreedomHubConnectionPolicy.IsAllowedLink(invalid), "Legacy or incorrect URI accepted");
var profile = VLESSFmt.Resolve(valid, out var error) ?? throw new Exception(error);
Expect(FreedomHubConnectionPolicy.IsAllowed(profile), "Imported REALITY profile rejected");
profile.StreamSecurity = "none";
Expect(!FreedomHubConnectionPolicy.IsAllowed(profile), "Plain TCP profile accepted");
Console.WriteLine("REALITY-only link and imported-profile contract checks passed.");

if (OperatingSystem.IsWindows())
{
    const string settings = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    var names = new[] { "ProxyEnable", "ProxyServer", "ProxyOverride", "AutoConfigURL" };
    using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(settings, true);
    var previous = names.ToDictionary(n => n, n => (Value: key.GetValue(n), Kind: key.GetValue(n) is null ? Microsoft.Win32.RegistryValueKind.Unknown : key.GetValueKind(n)));
    using var connectionKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(settings + @"\Connections", true);
    var connectionNames = connectionKey.GetValueNames();
    var connections = connectionNames.ToDictionary(n => n, n => (Value: connectionKey.GetValue(n), Kind: connectionKey.GetValueKind(n)));
    try
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var elevated = new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        Console.WriteLine($"Windows proxy smoke-test token elevated: {elevated}");
        Expect(ServiceLib.Handler.SysProxy.ProxySettingWindows.SetProxy("127.0.0.1:19876", "localhost;127.*", 2), "HKCU proxy application failed");
        Expect(ServiceLib.Handler.SysProxy.ProxySettingWindows.VerifyCurrentUserProxy("127.0.0.1:19876"), "Applied proxy verification failed");
        Expect(!ServiceLib.Handler.SysProxy.ProxySettingWindows.VerifyCurrentUserProxy("127.0.0.1:19877"), "Incorrect proxy accepted");
        Expect(ServiceLib.Handler.SysProxy.ProxySettingWindows.UnsetProxy(), "Proxy removal failed");
        Expect(ServiceLib.Handler.SysProxy.ProxySettingWindows.VerifyCurrentUserProxy(null, 1), "Disabled proxy verification failed");
        Console.WriteLine("Actual Windows LAN proxy apply/verify/wrong-value/clear checks passed.");
    }
    finally
    {
        foreach (var item in previous) { if (item.Value.Value is null) key.DeleteValue(item.Key, false); else key.SetValue(item.Key, item.Value.Value, item.Value.Kind); }
        foreach (var n in connectionKey.GetValueNames()) if (!connections.ContainsKey(n)) connectionKey.DeleteValue(n, false);
        foreach (var item in connections) if (item.Value.Value is not null) connectionKey.SetValue(item.Key, item.Value.Value, item.Value.Kind);
    }
}
