// Restart a Windows app elevated ("Run as administrator") from the PC worker ("node solve_full.js <cfg> <secs> <evs> <out>").
// The Windows consent prompt appears on the desktop; the person accepts it. cfg: { "image", "exe", "wait" }.
const fs = require('fs'), { spawnSync } = require('child_process');
const [cfgFile, , , OUT] = process.argv.slice(2), cfg = JSON.parse(fs.readFileSync(cfgFile, 'utf8'));
const res = {};
const k = spawnSync('taskkill', ['/IM', cfg.image, '/F', '/T'], { encoding: 'utf8', windowsHide: true }); res.kill = ((k.stdout || '') + (k.stderr || '')).trim().split(/\r?\n/).length + ' lines';
setTimeout(() => {
  const ps = spawnSync('powershell', ['-NoProfile', '-Command', `try { Start-Process -FilePath '${cfg.exe}' -Verb RunAs; 'started elevated' } catch { 'ERROR ' + $_.Exception.Message }`], { encoding: 'utf8', windowsHide: true, timeout: 150000 });
  res.start = ((ps.stdout || '') + (ps.stderr || '')).trim();
  setTimeout(() => { const t = spawnSync('tasklist', ['/FI', `IMAGENAME eq ${cfg.image}`], { encoding: 'utf8', windowsHide: true }); res.running = t.stdout; fs.writeFileSync(OUT, JSON.stringify(res, null, 1)); process.exit(0); }, (cfg.wait || 10) * 1000);
}, 4000);
