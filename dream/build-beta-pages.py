# Build the two beta-test copies for GitHub Pages:
#   docs/beta/dream/  (The Dream build)  and  docs/beta/arena/  (the Punch-Out Arena build)
# Usage: python3 dream/build-beta-pages.py <dream-built.html> <arena-built.html> <docs-dir>
import sys, pathlib, json
dream, arena, docs = map(pathlib.Path, sys.argv[1:4])
def page(src, title, short):
    body = src.read_text(encoding='utf-8')
    head = f'''<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<meta name="theme-color" content="#090908">
<meta name="robots" content="noindex">
<meta name="apple-mobile-web-app-capable" content="yes">
<meta name="mobile-web-app-capable" content="yes">
<meta name="apple-mobile-web-app-status-bar-style" content="black-translucent">
<meta name="apple-mobile-web-app-title" content="{short}">
<link rel="manifest" href="manifest.webmanifest">
<link rel="icon" type="image/png" sizes="32x32" href="../../icons/favicon-32.png">
<link rel="apple-touch-icon" href="../../icons/apple-touch-icon.png">
<style>html,body{{background:#090908;color-scheme:dark}} header.top{{top:0;padding-top:env(safe-area-inset-top,0px)}} @media (pointer:coarse){{input,select{{font-size:16px}}}}</style>
</head>
<body>
'''
    man = {"name": title, "short_name": short, "id": "./", "start_url": "./", "scope": "./", "display": "standalone", "background_color": "#090908", "theme_color": "#090908",
           "icons": [{"src": "../../icons/icon-192.png", "sizes": "192x192", "type": "image/png"}, {"src": "../../icons/icon-512.png", "sizes": "512x512", "type": "image/png"}]}
    return head + body + '\n</body>\n</html>\n', json.dumps(man, indent=2)
for name, src, title, short in [('dream', dream, 'Confirmed Fire: The Dream (beta)', 'CF Dream'), ('arena', arena, 'Confirmed Fire: Arena (beta)', 'CF Arena')]:
    out = docs / 'beta' / name; out.mkdir(parents=True, exist_ok=True)
    html, man = page(src, title, short)
    (out / 'index.html').write_text(html, encoding='utf-8'); (out / 'manifest.webmanifest').write_text(man, encoding='utf-8')
    print('wrote', out)
