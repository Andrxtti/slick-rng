// Fetch live site files from the PC and report version markers and md5s (the worker runs "node solve_full.js <cfg> <secs> <evs> <out>").
// cfg json: { "base": "https://.../", "files": ["sw.js", "lib/m6-d80.json"] }
const fs = require('fs'), crypto = require('crypto');
const [cfgFile, , , OUT] = process.argv.slice(2), cfg = JSON.parse(fs.readFileSync(cfgFile, 'utf8'));
(async () => {
  const res = { base: cfg.base, files: {} };
  for (const f of cfg.files) {
    try {
      const r = await fetch(cfg.base + f + '?n=' + Date.now(), { cache: 'no-store' }); const b = Buffer.from(await r.arrayBuffer());
      const t = b.toString('utf8');
      res.files[f] = { status: r.status, bytes: b.length, md5: crypto.createHash('md5').update(b).digest('hex'), marks: [...new Set((t.match(/slick-rng-4\.\d+|BETA 4\.\d+|Beta 4\.\d+/g) || []))] };
    } catch (e) { res.files[f] = { error: String(e) }; }
  }
  fs.writeFileSync(OUT, JSON.stringify(res, null, 1));
})();
