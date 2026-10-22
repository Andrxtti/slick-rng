# Build the two beta-test copies for GitHub Pages:
#   docs/beta/dream/  (The Dream build)  and  docs/beta/arena/  (the Punch-Out Arena build)
# Usage: python3 dream/build-beta-pages.py <dream-built.html> <arena-built.html> <docs-dir>
import sys, pathlib, json, hashlib
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
<script>if ('serviceWorker' in navigator && location.protocol === 'https:') addEventListener('load', () => navigator.serviceWorker.register('sw.js').catch(() => {{}}));</script>
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
    ver = hashlib.sha1(html.encode('utf-8')).hexdigest()[:10]
    # its own scoped worker: fresh page when online (falls back to the cached copy after 4s or offline), fonts cached
    sw = '''const CACHE = 'cf-beta-%s-%s';
self.addEventListener('install', (e) => { e.waitUntil(caches.open(CACHE).then((c) => c.addAll(['./', 'manifest.webmanifest'])).then(() => self.skipWaiting())); });
self.addEventListener('activate', (e) => { e.waitUntil(caches.keys().then((ks) => Promise.all(ks.filter((k) => k.startsWith('cf-beta-%s-') && k !== CACHE).map((k) => caches.delete(k)))).then(() => self.clients.claim())); });
self.addEventListener('fetch', (e) => {
  const req = e.request; if (req.method !== 'GET') return;
  const url = new URL(req.url);
  if (req.mode === 'navigate') {
    const net = fetch(req).then((res) => { const copy = res.clone(); caches.open(CACHE).then((c) => c.put('./', copy)); return res; });
    const slow = new Promise((ok) => setTimeout(() => caches.match('./').then((hit) => hit && ok(hit)), 4000));
    e.respondWith(Promise.race([net, slow]).catch(() => caches.match('./')));
    return;
  }
  if (!(url.origin === location.origin || url.host === 'fonts.googleapis.com' || url.host === 'fonts.gstatic.com')) return;
  e.respondWith(caches.match(req).then((hit) => hit || fetch(req).then((res) => { if (res.ok || res.type === 'opaque') { const c2 = res.clone(); caches.open(CACHE).then((c) => c.put(req, c2)); } return res; })));
});
''' % (name, ver, name)
    (out / 'sw.js').write_text(sw, encoding='utf-8')
    print('wrote', out)
