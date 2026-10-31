// Clear the Epic Games Launcher's web cache from the PC worker ("node solve_full.js <cfg> <secs> <evs> <out>"), Epic's own
// fix for a stale library: close the launcher, set the Saved/webcache* folders aside (renamed, not deleted, so it can be
// undone), start the launcher again and report what is in Saved.
const fs = require('fs'), path = require('path'), os = require('os'), { spawnSync, spawn } = require('child_process');
const [cfgFile, , , OUT] = process.argv.slice(2), cfg = JSON.parse(fs.readFileSync(cfgFile, 'utf8'));
const saved = path.join(os.homedir(), 'AppData/Local/EpicGamesLauncher/Saved'), res = { saved, moved: [] };
for (const im of ['EpicGamesLauncher.exe', 'EpicWebHelper.exe']) { const k = spawnSync('taskkill', ['/IM', im, '/F', '/T'], { encoding: 'utf8', windowsHide: true }); res['kill ' + im] = ((k.stdout || '') + (k.stderr || '')).trim(); }
setTimeout(() => {
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  try {
    res.before = fs.readdirSync(saved);
    for (const d of res.before) if (/^webcache/i.test(d) && !/\.bak-/.test(d)) {
      try { fs.renameSync(path.join(saved, d), path.join(saved, d + '.bak-' + stamp)); res.moved.push(d); } catch (e) { res.moved.push(d + ' FAILED ' + e.code); }
    }
    res.after = fs.readdirSync(saved);
  } catch (e) { res.err = String(e); }
  const p = spawn(cfg.exe, [], { detached: true, stdio: 'ignore' }); p.unref(); res.started = cfg.exe;
  setTimeout(() => { const t = spawnSync('tasklist', ['/FI', 'IMAGENAME eq EpicGamesLauncher.exe'], { encoding: 'utf8', windowsHide: true }); res.running = t.stdout; fs.writeFileSync(OUT, JSON.stringify(res, null, 1)); process.exit(0); }, (cfg.wait || 10) * 1000);
}, 4000);
