const CACHE = 'cf-beta-dream-9d5359ab71';
self.addEventListener('install', (e) => { e.waitUntil(caches.open(CACHE).then((c) => c.addAll(['./', 'manifest.webmanifest'])).then(() => self.skipWaiting())); });
self.addEventListener('activate', (e) => { e.waitUntil(caches.keys().then((ks) => Promise.all(ks.filter((k) => k.startsWith('cf-beta-dream-') && k !== CACHE).map((k) => caches.delete(k)))).then(() => self.clients.claim())); });
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
