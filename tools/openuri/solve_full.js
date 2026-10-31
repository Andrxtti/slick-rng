// Open a URI with Windows' default handler from the PC worker ("node solve_full.js <cfg> <secs> <evs> <out>").
// cfg json: { "uri": "com.epicgames.launcher://ue/library" }
const fs = require('fs'), { spawnSync } = require('child_process');
const [cfgFile, , , OUT] = process.argv.slice(2), cfg = JSON.parse(fs.readFileSync(cfgFile, 'utf8'));
const r = spawnSync('cmd.exe', ['/c', 'start', '""', cfg.uri], { encoding: 'utf8', windowsHide: true, timeout: 20000 });
fs.writeFileSync(OUT, JSON.stringify({ uri: cfg.uri, code: r.status, out: r.stdout, err: r.stderr }, null, 1));
