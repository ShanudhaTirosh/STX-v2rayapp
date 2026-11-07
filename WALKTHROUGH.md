# STX V2Ray Client — Rebranding, Glassmorphism & Routing Fix Walkthrough

This document outlines all changes applied to transform the portable VPN bundle into **STX V2Ray Client**, complete with a monochrome frosted glassmorphism UI, permanent routing fixes for 3x-ui web dashboards and loopback/local tools (Antigravity), and performance optimizations.

---

## 1. Rebranding to "STX V2Ray Client"

All assemblies, UI text, metadata, logos, icons, tray notifications, and settings namespaces have been rebranded:

| Component | Previous Value | Updated Value |
| :--- | :--- | :--- |
| **Product Name** | `PXN Shield VPN` | `STX V2Ray Client` |
| **Company** | `PXN Stores LK` | `STX Networks` |
| **Window Title** | `PXN Shield VPN` | `STX V2Ray Client` |
| **Tray Icon & Menus** | `PXN Shield` / `Open PXN Shield` | `STX V2Ray Client` / `Open STX V2Ray Client` |
| **Local AppData Dir** | `%LocalAppData%\PxnShieldVpn` | `%LocalAppData%\StxV2rayClient` |
| **Virtual Host Mapping** | `pxn.local` | `stx.local` (with `pxn.local` preserved as compatibility alias) |
| **Single Instance Mutex** | `PxnShieldVpn.SingleInstance` | `StxV2RayClient.SingleInstance` |
| **Settings File** | `pxn-settings.json` | `stx-settings.json` (with backward-compatible fallback) |
| **Web UI Branding** | 100% of strings in `index-FlWWI2Ov.js` | Replaced all PXN occurrences with STX |
| **Logo & Icon** | Purple shield logo & icon | Sleek monochrome STX metallic frosted shield logo & icon |

---

## 2. Monochrome Frosted Glassmorphism UI Facelift

The web interface (`wwwroot/assets/index-DAICIOx_.css` and `wwwroot/index.html`) was redesigned with a modern black and white glassmorphic aesthetic:

- **Deep Pitch-Black Base**: Background updated to `#000000` with subtle ambient radial gradients (`radial-gradient(ellipse at 50% -10%, #1c1c22 0%, #08080a 65%, #000000 100%)`).
- **Frosted Glass Cards (`.card`)**: Translucent dark glass (`rgba(18, 18, 22, 0.62)`) with `backdrop-filter: blur(24px) saturate(180%)`, soft white border lines (`1px solid rgba(255, 255, 255, 0.12)`), and top rim lighting (`inset 0 1px 0 rgba(255, 255, 255, 0.14)`).
- **Glassmorphic Navigation Bar**: Translucent glass with `backdrop-filter: blur(28px)` and frosted divider border.
- **High-Contrast Monochrome Buttons**:
  - Primary button (`.btn-primary`): Crisp solid white background with bold black text, hover lift, and white ambient glow (`box-shadow: 0 0 24px rgba(255, 255, 255, 0.45)`).
  - Ghost button (`.btn-ghost`): Frosted dark glass with white typography and border highlights.
- **Glass Input Fields & Modals**: Soft dark translucent backgrounds with focus ring accents and high-blur dialog backdrops.
- **Brand Emblem**: Replaced the original logo with a custom-rendered 432x432 anti-aliased monochrome frosted shield emblem featuring crisp "STX" lettering and fine tech grid lines.

---

## 3. Permanent Loopback & 3x-ui Dashboard Routing Fix

### The Problem
1. **3x-ui Web Dashboard Inaccessible**: Previously, Sing-box TUN routing in `CoreConfigSingboxService.cs` only routed direct for traffic matching `Environment.ProcessPath` (`PxnShield.exe`) on the specific node port (`directProbe.port`). Any web browser (Chrome, Edge, Firefox, cURL) connecting to `http://<server-ip>:2053` had its traffic routed through the VPN tunnel (`proxy`). When the remote VPS received tunnel traffic addressed to its own external IP/panel port, it looped or rejected the connection.
2. **Loopback / Developer Tools Broken (Antigravity, Local Dev Servers)**: When TUN was activated or system proxy was enabled, local connections on `127.0.0.1` and `localhost` were captured, breaking local development environments, language servers, and Antigravity IDE.

### The Permanent Fix
1. **Universal Server Address Bypass**:
   - In `CoreConfigSingboxService.cs` and `CoreConfigV2rayService.cs`, top-priority `direct` routing rules are now generated for the active node's IP (`_node.Address`), domain name, and resolved IP (`PxnServerAddress.Get(_node.Address)` / `DirectProbe?.Ip`) **for all applications and across all ports**:
     - Web browsers opening `http://<vps-ip>:2053` connect directly across the local physical adapter to the VPS, completely bypassing the proxy tunnel.
     - SSH (`port 22`), web hosting (`port 80/443`), and any server admin services remain directly reachable.
2. **Universal Loopback & Private Network Direct Rules**:
   - Top-priority rules in both Sing-box (`tun_singbox_rules`) and Xray (`SampleTunRules`) route `127.0.0.0/8`, `::1/128`, `localhost`, `*.local`, `*.localhost`, and `ip_is_private` directly.
   - Antigravity IDE, debuggers, Vite/Next.js/Node local servers, and local web hooks function without interference.
3. **Comprehensive Windows System Proxy Exceptions**:
   - In `SysProxyHandler.cs`, `GetWindowsProxyString` now injects a comprehensive bypass list whenever system proxy mode is used:
     `<local>;localhost;127.*;10.*;172.16.*;...;192.168.*;[::1]` plus the active server IP.

---

## 4. Performance & Core Optimizations

- **Sing-box TUN Stack**: Configured with `"stack": "mixed"` (gVisor user-space network stack for TCP with system stack for UDP/ICMP), reducing TCP latency, packet loss, and CPU overhead on Windows.
- **Endpoint-Independent NAT**: Enabled in `tun_singbox_inbound` for seamless P2P and gaming connectivity.
- **TCP Fast Open (TFO)**: Supported via `PxnTcpFastOpen` and core configs.
- **WebView2 Acceleration**: Hardened settings with GPU cache cleaning and disabled devtools overhead in production mode.

---

## 5. Verification & Deliverables

- **Solution Build**: `dotnet build src\PxnShield.sln -c Release` compiles cleanly with **0 Errors**.
- **Distribution Package**:
  - `dist\STXClient.exe` (166 MB single-file self-contained executable)
  - `dist\PxnShield.exe` (compatibility link)
  - `dist\wwwroot\` (rebranded web UI with black & white glassmorphism styling)
  - `dist\bin\` (Sing-box, Xray, wintun.dll, GeoIP/GeoSite databases)
  - `dist\README.txt` (STX V2Ray Client documentation)
- **Assembly Verification**:
  - `FileVersionInfo`: Description: `STX V2Ray Client`, Product: `STX V2Ray Client`, Company: `STX Networks`, Copyright: `Copyright © STX Networks`, Version: `2.0.0.0`.

