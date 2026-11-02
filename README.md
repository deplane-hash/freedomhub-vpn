# FreedomHub VPN (Windows client)

A Windows VPN client for the FreedomHub service. This is a rebranded build of
**[v2rayN](https://github.com/2dust/v2rayN)** (GPL-3.0) with custom branding and
a simplified one-click connect UI.

## License

This project is licensed under the **GNU General Public License v3.0** (GPL-3.0),
the same license as the upstream v2rayN project. See [LICENSE](LICENSE).

## Attribution

* Original client: v2rayN (https://github.com/2dust/v2rayN)
* The server is powered by the open-source [Xray-core](https://github.com/XTLS/Xray-core) engine.

## Changes vs upstream v2rayN

* Branding: app name `FreedomHub VPN`, custom icon, dark theme with green accents.
* Simplified toolbar: a single Connect/Power button with an animated progress state.
* Neutralized the built-in update check to the upstream repository.
* Tray tooltip renamed to `FreedomHub VPN`.

Full diff: `freedomhub-v2rayn.patch` (apply on top of the upstream v2rayN tag this
branch is based on).

## Build

Requires .NET SDK 10+ with Windows targeting pack.

```bash
dotnet publish v2rayN/v2rayN.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:EnableWindowsTargeting=true \
  -p:EnableCompressionInSingleFile=true \
  -o out
```

The resulting `FreedomHubVPN.exe` is a single-file, self-contained Windows
executable (no .NET runtime needed on the client machine).

## Server / engine

The client connects to a VLESS + REALITY endpoint on TCP 443 with dl.google.com TLS camouflage provisioned by the FreedomHub
service (Xray-core). Connection details are delivered per-user via the
subscription URL provided in the app.

## Signing

Signed releases use the free DigiCert Open Source certificate (see
`oss/SIGNING-STEPS.txt`). The signing pipeline is in `oss/sign-vpn.sh`.

## Connection policy

The FreedomHub UI exposes only its REALITY profile. Legacy direct/CDN/location profiles are not selected as fallbacks, and the core reload path rejects other destinations and transports. Google camouflage does not change the destination IP or make Google the VPN provider.
