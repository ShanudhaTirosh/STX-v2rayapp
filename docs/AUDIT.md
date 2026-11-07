# STX V2Ray Client — technical audit (v2.0.0 → 2.1.0)

**What this app is:** a Windows-only WPF host (.NET 8) with a WebView2 UI (a prebuilt React bundle), a v2rayN-derived Core library, and bundled Xray + sing-box (TUN). It talks to a Supabase backend for accounts/configs and is built around zero-rated "SNI-spoof" configs.

## Read this first — what was and was not verified
| | |
|---|---|
| Compiled | Core library and all non-UI host code (TunnelController, HostBridge, policies) compile with 0 errors on .NET 8. |
| Unit-checked | 57 self-tests pass (`tests/STX.SelfTest`, `dotnet run -c Release`): redaction, atomic writes, proxy backup/restore/crash recovery, process-path matching, fetch/origin policy, certificate-pin decision and a real TLS handshake hash. |
| **Not compiled** | `MainWindow.cs`, `App.cs`, `TrayService.cs`, `WebView2HostView.cs` (WPF/WinForms/WebView2) — no Windows SDK here. Edits are small and reviewed, but **build on Windows first**. |
| **Not tested** | Real connections to your servers, TUN mode, Windows registry proxy writes, installer, packaging script, UAC/auto-start, the WebView navigation lock. No stress/long-run testing was possible. |
| Not changed | Bundled `xray.exe` (26.1.23; upstream is 26.10.31) and `sing-box.exe` (version unverified). The UI source is not in the zip (only the minified bundle), so UI changes are limited to the CSS accessibility block. |

## Prioritised findings
### Critical
1. **TLS verification is off for every config** (`allowInsecure=true`; your live config uses SNI `m.tiktok.com`). Anyone on the network path can impersonate the server and read the account ID and all traffic. It is also a deadline: Xray announced `allowInsecure` stops working on **2026‑08‑01** (already past), which is why the app is frozen on a 9‑month‑old core. *Fix:* trust‑on‑first‑use pinning (`pinnedPeerCertSha256`, which works with a spoofed SNI) with fallback and a change prompt. Your bundled core contains the option (checked in the binary), **but verify against your servers** — the safest permanent fix is for the backend to send `pcs=<sha256>` in each config link. After that you can upgrade the cores.
2. **Every visited hostname was logged to disk** (Xray access log unset → stdout → daily log, also shown through the `getLog` bridge call). Fixed.
3. **`hostFetch` bridge = open proxy for the page**, no origin checks, DevTools on, navigation unrestricted. Fixed (allow‑list, origin check, navigation lock; DevTools off in Release).
4. **Killing other VPN clients:** `taskkill /IM xray.exe /IM sing-box.exe` + "kill if path unreadable". Fixed.
### High
5. System proxy left pointing at a dead port after a crash/sign‑out; disconnect also reset users' own proxy/PAC to "direct" even if STX never changed it. Fixed (backup/restore, `--reset-proxy`, sign‑out hook).
6. Subscription-usage request disabled certificate validation and logged 40 chars of the token URL. Fixed.
7. Auto‑reconnect made one unreported attempt (the "give up" branch was unreachable because each rebuild reset its counter) and waited ≥60 s after a network switch. Fixed.
8. A bad bridge message (`id` as string) crashed the process (exception in `async void` outside the `try`). Fixed.
### Medium
9. Traffic counters double‑counted (two sources). 10. UI calls before init → null reference. 11. Failed connect left cores/proxy behind. 12. Disconnect blocked up to ~25 s during a connect. 13. "Clear cache" pointed at a folder that does not exist. 14. Non‑atomic config writes. 15. Logs never rotated or pruned.
### Open items for you (not code changes I could make safely)
- **Your upload contains live credentials** (`guiConfigs/guiNDB.db`, `binConfigs/config.json`, logs with the Windows user path). Treat that account/UUID as exposed if the zip was shared, and never ship those folders (the packaging script excludes them).
- Account/config data in `guiNDB.db` is plaintext on disk (portable design). Consider per‑user ACLs or DPAPI if you drop portability.
- The Supabase anon key in the bundle is normal, but it is only safe if Row Level Security is correct — audit that on the backend.
- The web bundle ships a browser mock (`example.invalid`, "v9.9.9"); harmless but should be stripped in the UI build. Branding still mixes STX Enterprises / STX V2Ray Client / STXClient / PXN in several places.
- No auto‑updater for the app itself; cores should be updated after pinning is verified (`xray` 26.10.31, sing-box 1.14.x) and checksum‑verified.
- Code is decompiler output (`goto IL_…`, no comments); worth restoring from your real repo.
- Platform: Windows 10/11 x64 only (manifest, registry proxy, Wintun). Linux/macOS code paths are v2rayN leftovers and untested.

## Research used
Xray TLS docs (pinnedPeerCertSha256 overrides normal validation; leaf‑hash match succeeds immediately), v2rayN / v2rayNG release notes announcing the 2026‑08‑01 removal of `allowInsecure`, Xray latest release tag (v26.10.31), sing-box latest tag (v1.14.4), and WebView2 hardening settings.

## How to adopt
1. Build on Windows: `dotnet build src\STXEnterprises.sln -c Release`; run `dotnet run --project tests\STX.SelfTest -c Release`.
2. Smoke test: connect proxy mode and TUN, kill the app while connected (proxy must restore on next start), switch Wi‑Fi ↔ hotspot, Save diagnostics, check logs contain no hostnames/UUIDs.
3. Test one config end‑to‑end with pinning on; if anything fails set `"pinServerCertificates": false` in `guiConfigs\stx-settings.json` and tell me what happened.
4. `scripts\package-release.ps1 -Version 2.1.0` (then `iscc installer\STX.iss`), code‑sign, publish the .sha256.
