// Claims this folder so the parent site's offline worker leaves the game alone. No caching here:
// Unity caches its own data files, and the page always comes fresh from the network.
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (e) => e.waitUntil(self.clients.claim()));
