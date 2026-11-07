STX Enterprises - Portable Edition

1. Unzip this folder anywhere you like.
2. Run STXEnterprises.exe.

Windows will ask for administrator permission. That is expected: building the
tunnel means creating a network adapter and changing the Windows proxy, and
neither is possible without it.

The first time, Windows may also show a blue "Windows protected your PC" panel.
That is SmartScreen reacting to an app it has not seen before, not a virus
warning. Click "More info", then "Run anyway".

Everything the app needs is in this folder. Your configs, database and logs are
written here too; the only thing stored elsewhere is the browser-engine cache in
%LocalAppData%\STX Enterprises. To move the app, move the folder. To remove it,
quit STX first, then delete the folder (and that cache folder if you like).

If the app is ever killed while connected and your internet seems dead, start STX
again (it repairs the Windows proxy on launch) or run:  STXEnterprises.exe --reset-proxy

To send logs to support, use the tray icon > Save diagnostics. The file is redacted
(no account IDs, server links or visited sites) and is never uploaded automatically.

Features & Improvements:
- High-performance Sing-box TUN engine with mixed gVisor user-space network stack
- Direct loopback & 3x-ui server dashboard access bypass
- Modern Black & White Frosted Glassmorphism UI
- Zero MB / Cloudflare DoH server bootstrapping
- Full support for VLESS, VMess, Trojan, Shadowsocks, WireGuard

Support: STX Enterprises (https://stxenterprises.com)
