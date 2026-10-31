// Compare solver output (out/mw100_<tag>.json) with the RYE multiway charts (mw_charts.js).
//   node mwcmp.js out/mw100_a.json [out/mw100_b.json]   (two runs: also reports run-to-run noise)
const E = require('../../app/engine.js');
const { expand } = require('./mw_charts.js');
const X = expand(E), N = E.CLASS_NAMES;
const runs = process.argv.slice(2).map((f) => require(require('path').resolve(f)));
const SIZE = new Array(169).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const VERB = !!process.env.VERB;
const ours = (sp, c) => { const st = sp.st[c], o = { fold: 0, call: 0, raise: 0 }; sp.acts.forEach((k, i) => { o[k === 'allin' ? 'raise' : k] += st[i]; }); return o; };
const avgRuns = (k, c) => { const o = { fold: 0, call: 0, raise: 0 }; for (const r of runs) { const q = ours(r.spots[k], c); for (const a in o) o[a] += q[a] / runs.length; } return o; };
let tot = { agree: 0, n: 0 };
const rows = [];
runs[0].spots.forEach((sp, k) => {
  const ch = X[sp.title]; if (!ch) return;
  let w = 0, wc = 0, rq = 0, rc = 0, agree = 0, noise = 0, ovN = 0, ovD = 0; const miss = [];
  for (let c = 0; c < 169; c++) {
    const o = avgRuns(k, c), q = ch.p[c], s = SIZE[c];
    w += s * (1 - o.fold); wc += s * (1 - q.fold); rq += s * o.raise; rc += s * q.raise;
    const ag = Math.min(o.fold, q.fold) + Math.min(o.call, q.call) + Math.min(o.raise, q.raise); agree += s * ag;
    ovN += s * (Math.min(o.call, q.call) + Math.min(o.raise, q.raise)); ovD += s * Math.max(1 - o.fold, 1 - q.fold);
    if (runs.length > 1) { const a = ours(runs[0].spots[k], c), b = ours(runs[1].spots[k], c); noise += s * (Math.abs(a.fold - b.fold) + Math.abs(a.call - b.call) + Math.abs(a.raise - b.raise)) / 2; }
    const d = 1 - ag; if (d > 0.45) miss.push([N[c], ch.code[c], `${(o.fold * 100) | 0}/${(o.call * 100) | 0}/${(o.raise * 100) | 0}`, d * s]);
  }
  miss.sort((a, b) => b[3] - a[3]);
  tot.agree += agree; tot.n += 1326;
  rows.push({ t: sp.title, w: (100 * w) / 1326, wc: (100 * wc) / 1326, rq: (100 * rq) / 1326, rc: (100 * rc) / 1326, ag: (100 * agree) / 1326, ov: (100 * ovN) / Math.max(ovD, 1e-9), noise: (100 * noise) / 1326, miss });
});
console.log('spot'.padEnd(30), ' width ours/chart   squeeze ours/chart   hands agreeing (all / continue range)', runs.length > 1 ? '  run-to-run diff' : '');
for (const r of rows) {
  console.log(r.t.padEnd(30), `${r.w.toFixed(1).padStart(5)} / ${r.wc.toFixed(1).padStart(4)}     ${r.rq.toFixed(1).padStart(4)} / ${r.rc.toFixed(1).padStart(4)}         ${r.ag.toFixed(1)}% / ${r.ov.toFixed(0)}%`, runs.length > 1 ? `        ${r.noise.toFixed(1)}%` : '');
  if (VERB) console.log('     biggest misses (hand chart-code ours fold/call/raise):', r.miss.slice(0, 14).map((m) => `${m[0]} ${m[1]} ${m[2]}`).join(' | '));
}
console.log('overall agreement', ((100 * tot.agree) / tot.n).toFixed(1) + '%');
