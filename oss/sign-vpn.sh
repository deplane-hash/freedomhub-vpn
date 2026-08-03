#!/usr/bin/env bash
# Sign the FreedomHub VPN Windows build with a code-signing certificate
# (e.g. the free DigiCert Open Source cert). Uses osslsigncode (Linux).
#
# Usage:
#   ./sign-vpn.sh /path/to/cert.p12 [PFX_PASSWORD] [timestamp_uri]
#
# Defaults timestamp to DigiCert's RFC3161 service.

set -euo pipefail

CERT="${1:?usage: sign-vpn.sh <cert.p12> [password] [timestamp]}"
PASS="${2:-}"
TS="${3:-http://timestamp.digicert.com}"
OUT_DIR="${OUT_DIR:-/opt/v2rayN-out}"
EXE="$OUT_DIR/FreedomHubVPN.exe"

if [[ ! -f "$EXE" ]]; then
  echo "error: $EXE not found (build first)" >&2
  exit 1
fi

NAME="FreedomHub VPN"

sign_file() {
  local f="$1"
  if osslsigncode sign -pkcs12 "$CERT" -pass "$PASS" -h sha256 -t "$TS" \
      -n "$NAME" -in "$f" -out "$f.signed" >/dev/null 2>&1; then
    mv -f "$f.signed" "$f"
    echo "signed: $f"
  else
    echo "FAIL:  $f"
  fi
}

# Sign the single-file exe and the WPF native DLLs that accompany it.
sign_file "$EXE"
for dll in "$OUT_DIR"/*.dll; do
  [[ -f "$dll" ]] || continue
  sign_file "$dll"
done

echo
echo "Done. Verify with:"
echo "  osslsigncode verify \"$EXE\""