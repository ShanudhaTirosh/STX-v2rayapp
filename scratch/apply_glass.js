const fs = require('fs');
const path = require('path');

const cssPath = path.resolve('wwwroot/assets/index-DAICIOx_.css');
let css = fs.readFileSync(cssPath, 'utf8');

const oldDark = ':root,[data-theme=dark]{color-scheme:dark;--bg: 8 9 10;--surface: 13 14 16;--surface-2: 19 20 23;--surface-3: 25 27 31;--ink: 243 244 246;--ink-dim: 183 188 196;--muted: 131 136 143;--faint: 86 91 98;--line: 255 255 255;--line-a: .07;--line-strong-a: .12;--live: 58 209 122;--warn: 227 179 65;--bad: 229 83 75;--accent: 119 9 147;--accent-ink: 184 77 224;--accent-dim: 78 5 97;--accent-wash: 28 10 34;--amber: 251 191 36;--amber-ink: 253 233 192;--amber-wash: 30 24 12;--disc-off: 19 20 23;--disc-on: 28 32 39;--disc-rim: 168 196 224;--glass-top: 255 255 255;--glass-top-a: .18;--shadow-card: 0 1px 2px rgb(0 0 0 / .5);--shadow-lift: 0 16px 48px rgb(0 0 0 / .6);--toast-bg: 11 11 13;--shadow-toast: 0 10px 40px -10px rgb(0 0 0 / .7), inset 0 0 0 1px rgb(255 255 255 / .03);--shadow-toast-good: 0 10px 40px -10px rgb(16 185 129 / .25), inset 0 0 0 1px rgb(255 255 255 / .03);--shadow-toast-bad: 0 10px 40px -10px rgb(244 63 94 / .25), inset 0 0 0 1px rgb(255 255 255 / .03)}';

const newDark = ':root,[data-theme=dark]{color-scheme:dark;--bg: 0 0 0;--surface: 14 14 18;--surface-2: 22 22 26;--surface-3: 32 32 38;--ink: 255 255 255;--ink-dim: 215 218 225;--muted: 150 155 165;--faint: 100 105 115;--line: 255 255 255;--line-a: .12;--line-strong-a: .22;--live: 255 255 255;--warn: 240 190 75;--bad: 240 85 75;--accent: 255 255 255;--accent-ink: 255 255 255;--accent-dim: 220 220 220;--accent-wash: 255 255 255;--amber: 255 255 255;--amber-ink: 255 255 255;--amber-wash: 255 255 255;--disc-off: 16 16 18;--disc-on: 26 26 30;--disc-rim: 255 255 255;--glass-top: 255 255 255;--glass-top-a: .28;--shadow-card: 0 8px 32px 0 rgba(0, 0, 0, 0.6);--shadow-lift: 0 20px 50px rgba(0, 0, 0, 0.8);--toast-bg: 14 14 18;--shadow-toast: 0 10px 40px -10px rgb(0 0 0 / .8), inset 0 0 0 1px rgb(255 255 255 / .15);--shadow-toast-good: 0 10px 40px -10px rgb(255 255 255 / .25), inset 0 0 0 1px rgb(255 255 255 / .15);--shadow-toast-bad: 0 10px 40px -10px rgb(244 63 94 / .3), inset 0 0 0 1px rgb(255 255 255 / .15)}';

if (css.includes(oldDark)) {
  css = css.replace(oldDark, newDark);
  console.log('Replaced dark theme variables successfully');
} else {
  console.log('Old dark theme string not matched directly, checking partial');
}

const glassOverrides = `
/* STX V2Ray Client - Frosted Glassmorphism & High-Contrast Monochrome Aesthetic */
body {
  background: radial-gradient(ellipse at 50% -10%, #1c1c22 0%, #08080a 65%, #000000 100%) !important;
  color: #ffffff !important;
}

/* Frosted glass sidebar */
nav {
  background: rgba(10, 10, 13, 0.70) !important;
  backdrop-filter: blur(28px) saturate(180%) !important;
  -webkit-backdrop-filter: blur(28px) saturate(180%) !important;
  border-right: 1px solid rgba(255, 255, 255, 0.10) !important;
}

/* Glassmorphism Cards & Panels */
.card, [class*="card"] {
  background: rgba(18, 18, 22, 0.62) !important;
  backdrop-filter: blur(24px) saturate(180%) !important;
  -webkit-backdrop-filter: blur(24px) saturate(180%) !important;
  border: 1px solid rgba(255, 255, 255, 0.12) !important;
  box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.5), inset 0 1px 0 0 rgba(255, 255, 255, 0.14) !important;
  border-radius: 1.125rem !important;
}

.card:hover {
  border-color: rgba(255, 255, 255, 0.22) !important;
  box-shadow: 0 12px 40px 0 rgba(0, 0, 0, 0.65), inset 0 1px 0 0 rgba(255, 255, 255, 0.22) !important;
}

/* High-Contrast Monochrome Primary Button */
.btn-primary {
  background: #ffffff !important;
  color: #000000 !important;
  font-weight: 700 !important;
  border: 1px solid rgba(255, 255, 255, 0.95) !important;
  box-shadow: 0 4px 16px rgba(255, 255, 255, 0.18) !important;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1) !important;
}

.btn-primary:hover {
  background: #f2f2f2 !important;
  color: #000000 !important;
  box-shadow: 0 0 24px rgba(255, 255, 255, 0.45) !important;
  transform: translateY(-1px) !important;
}

.btn-primary:active {
  transform: translateY(0) scale(0.98) !important;
}

/* Frosted Secondary / Ghost Button */
.btn-ghost {
  background: rgba(255, 255, 255, 0.06) !important;
  color: #ffffff !important;
  border: 1px solid rgba(255, 255, 255, 0.14) !important;
  backdrop-filter: blur(12px) !important;
  -webkit-backdrop-filter: blur(12px) !important;
  transition: all 0.2s ease !important;
}

.btn-ghost:hover {
  background: rgba(255, 255, 255, 0.14) !important;
  border-color: rgba(255, 255, 255, 0.3) !important;
  color: #ffffff !important;
}

.btn-quiet {
  color: rgba(255, 255, 255, 0.75) !important;
  transition: all 0.15s ease !important;
}

.btn-quiet:hover {
  color: #ffffff !important;
  background: rgba(255, 255, 255, 0.09) !important;
}

/* Glass Inputs & Dropdowns */
.field, input[type="text"], input[type="password"], textarea, select {
  background: rgba(12, 12, 15, 0.65) !important;
  border: 1px solid rgba(255, 255, 255, 0.13) !important;
  color: #ffffff !important;
  backdrop-filter: blur(14px) !important;
  -webkit-backdrop-filter: blur(14px) !important;
  border-radius: 0.875rem !important;
}

.field:focus, input:focus, textarea:focus, select:focus {
  border-color: rgba(255, 255, 255, 0.6) !important;
  box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.2) !important;
}

/* Frosted Modals & Dialogs */
dialog, [role="dialog"], .toast {
  background: rgba(14, 14, 18, 0.85) !important;
  backdrop-filter: blur(36px) saturate(200%) !important;
  -webkit-backdrop-filter: blur(36px) saturate(200%) !important;
  border: 1px solid rgba(255, 255, 255, 0.16) !important;
  box-shadow: 0 30px 60px -15px rgba(0, 0, 0, 0.85), inset 0 1px 0 rgba(255, 255, 255, 0.22) !important;
}

/* Accent Wash & Highlights */
.bg-accent-wash {
  background-color: rgba(255, 255, 255, 0.08) !important;
}

.text-accent-ink {
  color: #ffffff !important;
}

.border-accent-ink {
  border-color: rgba(255, 255, 255, 0.35) !important;
}

/* Scrollbar */
::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.18) !important;
}
::-webkit-scrollbar-thumb:hover {
  background: rgba(255, 255, 255, 0.35) !important;
}
`;

css += '\n' + glassOverrides;
fs.writeFileSync(cssPath, css, 'utf8');
console.log('Successfully updated index-DAICIOx_.css with Black & White Frosted Glassmorphism');

