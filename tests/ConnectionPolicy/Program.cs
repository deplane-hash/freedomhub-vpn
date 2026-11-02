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
