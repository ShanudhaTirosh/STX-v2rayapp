# Changelog

## 2.1.0 — audit release
Security / privacy
- Browsing destinations are no longer written to disk: Xray's access log was unset (= printed to stdout and saved); now `"none"`.
- All logs pass through a redactor (account UUIDs, share links, subscription tokens, JWTs, visited hosts, Windows user name). Logs roll over at 8 MB and are deleted after 7 days.
- Host bridge: `hostFetch` only reaches https backends on an allow-list; malformed bridge messages can no longer crash the app; messages from any origin other than the bundled pages are ignored; the window refuses to navigate away from the bundled pages; permissions and downloads are denied; DevTools, context menu and browser shortcuts are off in Release builds.
- Subscription-usage check validates TLS again (it was switched off) and refuses local/private addresses; its log line no longer contains part of the subscription URL.
- Server certificate pinning (trust on first use) replaces blanket `allowInsecure`, which Xray stops supporting after 2026‑08‑01. Switch: `pinServerCertificates` (default on). If a pinned server later shows a different certificate the user is asked before it is trusted.
Reliability
- Stopping stray cores only touches processes from this install (previously `taskkill /IM xray.exe /IM sing-box.exe` also killed other VPN clients' cores).
- System proxy is saved before it is changed and restored on disconnect; crash/sign-out recovery at next start or with `--reset-proxy`. Disconnect no longer wipes a proxy STX never set.
- Reconnect is one locked operation (no window where another call can interleave), Disconnect cancels a connect in progress, failed connects stop the cores and clear the proxy.
- Automatic recovery: up to 3 attempts with 2/6/15 s pauses, visible progress and a clear final message; after a network change the watchdog reacts after ~2 failed probes instead of 3 × 20 s.
- Traffic counters were fed by two sources and double-counted; fixed. UI calls arriving before core initialisation wait instead of failing.
- "Clear cache" now targets the real WebView2 folder.
- Settings, tweaks and known-address files are written atomically.
Diagnostics / release
- Tray ▸ Save diagnostics (redacted zip, never uploaded); local crash reports in `guiLogs\crash`.
- Release build settings, packaging script, installer script, self-test suite (57 checks), `.gitignore` that keeps credentials out of source control.
- UI: visible keyboard focus, reduced-motion and high-contrast support, blur fallback.
