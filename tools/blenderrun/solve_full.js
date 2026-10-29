// Run Blender command lines from the PC worker ("node solve_full.js <cfg> <secs> <evs> <out>"), one after another:
// cfg { "exe": "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe", "steps": [["-b", "--version"], ...] }
// Used for installing extensions (-c extension ...) and for headless scripts (-b -P script.py).
const fs = require('fs'), { spawnSync } = require('child_process');
const [cfgFile, secs, , OUT] = process.argv.slice(2), cfg = JSON.parse(fs.readFileSync(cfgFile, 'utf8'));
const res = { steps: [] };
for (const args of cfg.steps) {
  const t0 = Date.now(), r = spawnSync(cfg.exe, args, { encoding: 'utf8', windowsHide: true, timeout: (+secs || 600) * 1000, maxBuffer: 64 << 20 });
  res.steps.push({ args, code: r.status, ms: Date.now() - t0, err: r.error ? String(r.error) : null, out: (r.stdout || '').slice(-3000), stderr: (r.stderr || '').slice(-1500) });
  fs.writeFileSync(OUT + '.part', JSON.stringify(res, null, 1));
}
fs.writeFileSync(OUT, JSON.stringify(res, null, 1));
