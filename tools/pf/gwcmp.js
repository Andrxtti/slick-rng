// Three-way comparison on the 100bb multiway nodes: GTO Wizard (MTT chip EV 8-max, read from the user's own account into
// gw/gw100.txt, local only), the course charts (mw_charts.js, local only) and our solves (out/mw100_<tag>.json).
//   node gwcmp.js out/mw100_<tag>.json [out/mw100_<tag2>.json]
// Hand agreement = hand-weighted share of the fold/call/raise mix that two sources have in common.
const fs = require('fs'), path = require('path');
const E = require('../../app/engine.js'), N = E.CLASS_NAMES;
const SIZE = new Array(169).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
let X = null; try { X = require('./mw_charts.js').expand(E); } catch (e) { X = {}; }
// GTO Wizard action path (UTG, UTG1, LJ, HJ, CO, BTN, SB, BB; R<size> raise, C call, F fold) -> our spot title
const MAP = {
  'R2.1-C-F-F-F|5': 'BTN vs EP open + EP flat', 'R2.1-F-C-F-F|5': 'BTN vs EP open + MP flat', 'R2.1-F-F-F-C|5': 'BTN vs EP open + CO flat',
  'F-F-F-R2.1-C|5': 'BTN vs HJ open + CO flat', 'F-F-R2.1-F-C|5': 'BTN vs MP open + CO flat', 'R2.1-C-F|3': 'HJ vs EP open + EP flat',
  'R2.1-C|2': 'EP+2 vs EP open + EP+1 flat', 'R2.1-F-C-F-F-F|6': 'SB vs EP open + EP/MP flat', 'F-F-F-F-R2.2-C|6': 'SB vs CO open + BTN flat',
  'R2.1-F-F-F-F-C|6': 'SB vs EP open + LP flat', 'R2.1-F-F-F-F-F-C|7': 'BB vs EP open + SB flat', 'R2.1-F-F-F-F-C-F|7': 'BB vs EP open + BTN/CO flat',
  'R2.1-F-C-F-F-F-F|7': 'BB vs EP open + EP/MP flat', 'F-F-R2.1-F-F-C-F|7': 'BB vs MP open + LP flat', 'F-F-R2.1-F-F-F-C|7': 'BB vs MP open + SB flat',
  'F-F-F-F-R2.2-C-F|7': 'BB vs CO open + BTN flat', 'F-F-F-F-F-R2.5-C|7': 'BB vs BTN open + SB flat',
  'F-F-F-F-R2.2|5': 'BTN vs CO open', 'F-F-F-F-F-R2.5|6': 'SB vs BTN open', 'R2.1-F-F-F-F|5': 'BTN vs UTG open', 'R2.1-F-F-F-F-F-F|7': 'BB vs UTG open',
  'F-F-F-F-F-R2.5-F|7': 'BB vs BTN open', 'R2.1|1': 'UTG1 vs UTG open', 'R2.1-F-F-F|4': 'CO vs UTG open',
  'F-F-F-F-F-R2.5-R11.25-F|8': 'BTN vs SB 3-bet', 'F-F-F-F-F-R2.5-R11.25-F-R24.75|9': 'SB vs BTN 4-bet', 'R2.1-F-F-F-C-C-R11.55-F|8': 'UTG vs CO/BTN flats + SB squeeze',
};
// keys are normalized to a stack and a path with raise sizes stripped ("25|R-F-F-F-F-F-C|7"), so one map serves all depths
const norm = (k) => k.replace(/R[\d.]+/g, 'R');
const GW = {};
for (const f of fs.readdirSync(path.join(__dirname, 'gw')).filter((x) => /^gw\d+\.txt$/.test(x))) for (const line of fs.readFileSync(path.join(__dirname, 'gw', f), 'utf8').trim().split('\n')) {
  const [key, body] = line.split('\t'); const parts = key.split('|'); const k = Math.round(+parts[0]) + '|' + norm(parts.slice(1).join('|')); const p = new Array(169);
  for (const tok of body.trim().split(/\s+/)) { const [h, codes] = tok.split(':'); const o = { fold: 0, call: 0, raise: 0 }; for (const m of codes.matchAll(/([arcf])(\d+)/g)) o[m[1] === 'f' ? 'fold' : m[1] === 'c' ? 'call' : 'raise'] += +m[2] / 100; const s = o.fold + o.call + o.raise; if (s > 0) for (const a in o) o[a] /= s; p[N.indexOf(h)] = o; }
  GW[k] = p;
}
const ours = (sp, c) => { const st = sp.st[c], o = { fold: 0, call: 0, raise: 0 }; sp.acts.forEach((k, i) => { o[k === 'allin' ? 'raise' : k] += st[i]; }); return o; };
// hands GTO Wizard lists at the node (at a 3-bet or 4-bet node only the hands that got there have an entry); the
// others count neither in widths nor in agreement
const has = (o) => o && o.fold + o.call + o.raise > 0.5;
const width = (f, g) => { let w = 0, r = 0, n = 0; for (let c = 0; c < 169; c++) { if (g && !has(g(c))) continue; const o = f(c); n += SIZE[c]; w += SIZE[c] * (1 - o.fold); r += SIZE[c] * o.raise; } return [(100 * w) / n, (100 * r) / n]; };
const agree = (f, g) => { let a = 0, n = 0; for (let c = 0; c < 169; c++) { const q = g(c); if (!has(q)) continue; const o = f(c); n += SIZE[c]; a += SIZE[c] * (Math.min(o.fold, q.fold) + Math.min(o.call, q.call) + Math.min(o.raise, q.raise)); } return (100 * a) / n; };
const runs = process.argv.slice(2).map((f) => ({ tag: path.basename(f, '.json').replace(/^mw\d+_/, ''), d: require(path.resolve(f)) }));
const STACK = runs.length ? Math.round(runs[0].d.stack || 100) : 100;
if (runs.some((r) => Math.round(r.d.stack || 100) !== STACK)) throw new Error('all runs must share one stack');
console.log(`stack ${STACK}bb: GTO Wizard MTT chip EV 8-max vs ${STACK === 100 ? 'the course charts (100bb) and ' : ''}our solves`);
const hdr = ['spot'.padEnd(29), 'width: GW / RYE', ...runs.map((r) => r.tag.slice(0, 12).padStart(12)), ' | agree with GW: RYE', ...runs.map((r) => r.tag.slice(0, 12).padStart(12))].join('  ');
console.log(hdr);
const tot = { rye: [], runs: runs.map(() => []) };
for (const [k0, title] of Object.entries(MAP)) {
  const g = GW[STACK + '|' + norm(k0)]; if (!g) continue;
  const gw = (c) => g[c], ch = STACK === 100 ? X[title] : null; const [wg, rg] = width(gw, gw);
  const row = [title.padEnd(29), `${wg.toFixed(0).padStart(3)}/${ch ? width((c) => ch.p[c])[0].toFixed(0).padStart(3) : ' - '}  (sq ${rg.toFixed(0)}/${ch ? width((c) => ch.p[c])[1].toFixed(0) : '-'})`];
  for (const r of runs) { const sp = r.d.spots.find((s) => s.title === title); row.push(sp ? width((c) => ours(sp, c), gw)[0].toFixed(0).padStart(12) : '-'.padStart(12)); }
  const ag = ch ? agree((c) => ch.p[c], gw) : NaN; row.push(` | ${isNaN(ag) ? '   -' : ag.toFixed(0).padStart(3) + '%'}`); if (!isNaN(ag)) tot.rye.push(ag);
  runs.forEach((r, i) => { const sp = r.d.spots.find((s) => s.title === title); const a = sp ? agree((c) => ours(sp, c), gw) : NaN; row.push(isNaN(a) ? '-'.padStart(12) : (a.toFixed(0) + '%').padStart(12)); if (!isNaN(a)) tot.runs[i].push(a); });
  console.log(row.join('  '));
}
const mean = (a) => (a.reduce((s, x) => s + x, 0) / Math.max(a.length, 1)).toFixed(1);
console.log('\nmean hand agreement with GTO Wizard: RYE charts ' + mean(tot.rye) + '%' + runs.map((r, i) => `, ${r.tag} ${mean(tot.runs[i])}%`).join(''));
if (process.env.HANDS) { // per-hand view of one spot: HANDS="BB vs EP open + SB flat"
  const title = process.env.HANDS, k = Object.keys(MAP).find((x) => MAP[x] === title), g = GW[STACK + '|' + norm(k)], ch = STACK === 100 ? X[title] : null;
  const fmt = (o) => `${Math.round(o.raise * 100)}/${Math.round(o.call * 100)}/${Math.round(o.fold * 100)}`;
  console.log(`\n${title}  (raise/call/fold %)  GW | RYE | ${runs.map((r) => r.tag).join(' | ')}`);
  for (let c = 0; c < 169; c++) { const parts = [N[c].padEnd(4), fmt(g[c]).padStart(10), ch ? fmt(ch.p[c]).padStart(10) : ''].concat(runs.map((r) => { const sp = r.d.spots.find((s) => s.title === title); return sp ? fmt(ours(sp, c)).padStart(10) : ''; })); console.log(parts.join(' ')); }
}
