// Run a fixed list of Windows commands from the PC worker ("node solve_full.js <cfg> <secs> <evs> <out>") and report
// their output: cfg { "cmds": ["git --version", "where git"] }. Each runs through cmd /c.
const fs = require('fs'), { spawnSync } = require('child_process');
const [cfgFile, secs, , OUT] = process.argv.slice(2), cfg = JSON.parse(fs.readFileSync(cfgFile, 'utf8'));
const res = [];
for (const c of cfg.cmds) {
  const r = spawnSync('cmd', ['/c', c], { encoding: 'utf8', windowsHide: true, timeout: (+secs || 120) * 1000, maxBuffer: 16 << 20 });
  res.push({ cmd: c, code: r.status, out: (r.stdout || '').slice(-3000), err: (r.stderr || '').slice(-1500) });
}
fs.writeFileSync(OUT, JSON.stringify(res, null, 1));
