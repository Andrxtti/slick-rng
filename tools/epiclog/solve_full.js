// Read the Epic Games Launcher's own log on the PC (worker runs "node solve_full.js <cfg> <secs> <evs> <out>").
// Reports the log tail and the lines about engine installs, errors and dialogs, so a stuck "Install Engine" can be diagnosed.
const fs = require('fs'), path = require('path'), os = require('os');
const OUT = process.argv[5];
const dir = path.join(os.homedir(), 'AppData/Local/EpicGamesLauncher/Saved/Logs');
const res = { dir, files: [] };
try {
  const files = fs.readdirSync(dir).map((f) => ({ f, t: fs.statSync(path.join(dir, f)).mtimeMs })).sort((a, b) => b.t - a.t);
  res.files = files.slice(0, 6).map((x) => [x.f, new Date(x.t).toISOString()]);
  const main = path.join(dir, 'EpicGamesLauncher.log');
  if (fs.existsSync(main)) {
    const t = fs.readFileSync(main, 'utf8'); res.bytes = t.length;
    const lines = t.split(/\r?\n/);
    res.hits = lines.filter((l) => /install|engine|UE_5|error|fail|modal|dialog|prereq|eula|license|denied|disk/i.test(l)).slice(-120).map((l) => l.slice(0, 300));
    res.tail = lines.slice(-60).map((l) => l.slice(0, 300));
  }
} catch (e) { res.err = String(e); }
fs.writeFileSync(OUT, JSON.stringify(res, null, 1));
