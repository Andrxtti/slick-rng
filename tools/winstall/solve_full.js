// Install a Windows package with winget, through the PC worker ("node solve_full.js <cfg> <secs> <evs> <out>").
// cfg json: { "id": "EpicGames.EpicGamesLauncher", "check": ["C:/Program Files (x86)/Epic Games/Launcher/..."] }
// An installer that needs admin rights shows Windows' own Yes/No prompt; only the person at the PC can answer it.
const fs = require('fs'), { spawnSync } = require('child_process');
const [cfgFile, secs, , OUT] = process.argv.slice(2);
const cfg = JSON.parse(fs.readFileSync(cfgFile, 'utf8'));
const res = { id: cfg.id, t0: new Date().toISOString() };
const run = (args, ms) => { const r = spawnSync('winget', args, { encoding: 'utf8', timeout: ms, windowsHide: true }); return { code: r.status, err: r.error ? String(r.error) : null, out: (r.stdout || '').slice(-4000), stderr: (r.stderr || '').slice(-2000) }; };
res.version = run(['--version'], 30000);
res.listBefore = run(['list', '--id', cfg.id, '-e', '--accept-source-agreements'], 120000);
res.install = run(['install', '--id', cfg.id, '-e', '--silent', '--accept-source-agreements', '--accept-package-agreements'], (+secs || 1200) * 1000);
res.listAfter = run(['list', '--id', cfg.id, '-e'], 120000);
res.check = (cfg.check || []).map((p) => [p, fs.existsSync(p)]);
res.t1 = new Date().toISOString();
fs.writeFileSync(OUT, JSON.stringify(res, null, 1));
