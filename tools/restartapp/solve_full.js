// Restart a Windows app from the PC worker ("node solve_full.js <cfg> <secs> <evs> <out>").
// cfg json: { "image": "EpicGamesLauncher.exe", "exe": "C:/.../EpicGamesLauncher.exe", "wait": 8 }
const fs = require('fs'), { spawnSync, spawn } = require('child_process');
const [cfgFile, , , OUT] = process.argv.slice(2), cfg = JSON.parse(fs.readFileSync(cfgFile, 'utf8'));
const res = {};
const k = spawnSync('taskkill', ['/IM', cfg.image, '/F', '/T'], { encoding: 'utf8', windowsHide: true }); res.kill = (k.stdout || '') + (k.stderr || '');
setTimeout(() => {
  const p = spawn(cfg.exe, [], { detached: true, stdio: 'ignore' }); p.unref(); res.started = cfg.exe;
  setTimeout(() => { const t = spawnSync('tasklist', ['/FI', `IMAGENAME eq ${cfg.image}`], { encoding: 'utf8', windowsHide: true }); res.running = t.stdout; fs.writeFileSync(OUT, JSON.stringify(res, null, 1)); process.exit(0); }, (cfg.wait || 8) * 1000);
}, 3000);
