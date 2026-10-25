// One line per solver run: how close it lands to the 18 RYE multiway charts (mw_charts.js, local reference data).
//   node mwsum.js out/mw100_*.json
// width = % of hands continuing (call + squeeze); sq = % of hands squeezing; agree = hand-weighted share of the
// strategy that matches the chart (fold/call/raise); overlap = agreement inside the continue range only.
// Groups: BTN (5 overcall spots), LP (HJ, EP+2, MP), SB (3), BB (7). "err" is the mean absolute width error in % of hands.
const path = require('path');
const E = require('../../app/engine.js');
const { expand } = require('./mw_charts.js');
const X = expand(E);
const SIZE = new Array(169).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const grp = (t) => (t.startsWith('BTN') ? 'BTN' : t.startsWith('BB') ? 'BB' : t.startsWith('SB') ? 'SB' : 'LP');
const ours = (sp, c) => { const st = sp.st[c], o = { fold: 0, call: 0, raise: 0 }; sp.acts.forEach((k, i) => { o[k === 'allin' ? 'raise' : k] += st[i]; }); return o; };
const rows = [];
for (const f of process.argv.slice(2)) {
  let d; try { d = require(path.resolve(f)); } catch (e) { continue; }
  const m = d.mw || {}, tag = d.tag, set = m.on ? `play ${m.play} gam ${m.gam} r ${m.r}` : 'engine default (MW off)';
  const G = { BTN: [], LP: [], SB: [], BB: [] }; let ag = 0, ov = 0, n = 0;
  for (const sp of d.spots) {
    const ch = X[sp.title]; if (!ch) continue;
    let w = 0, wc = 0, rq = 0, rc = 0, agree = 0, ovN = 0, ovD = 0;
    for (let c = 0; c < 169; c++) {
      const o = ours(sp, c), q = ch.p[c], s = SIZE[c];
      w += s * (1 - o.fold); wc += s * (1 - q.fold); rq += s * o.raise; rc += s * q.raise;
      const a = Math.min(o.fold, q.fold) + Math.min(o.call, q.call) + Math.min(o.raise, q.raise); agree += s * a;
      ovN += s * (Math.min(o.call, q.call) + Math.min(o.raise, q.raise)); ovD += s * Math.max(1 - o.fold, 1 - q.fold);
    }
    G[grp(sp.title)].push({ w: (100 * w) / 1326, wc: (100 * wc) / 1326, rq: (100 * rq) / 1326, rc: (100 * rc) / 1326 });
    ag += (100 * agree) / 1326; ov += (100 * ovN) / Math.max(ovD, 1e-9); n++;
  }
  const mean = (a, k) => a.reduce((s, x) => s + x[k], 0) / Math.max(a.length, 1);
  const err = (a) => a.reduce((s, x) => s + Math.abs(x.w - x.wc), 0) / Math.max(a.length, 1);
  const allErr = Object.values(G).flat();
  rows.push({ tag, set, iters: d.iters, ag: ag / n, ov: ov / n, err: err(allErr), sq: mean(allErr, 'rq'), sqc: mean(allErr, 'rc'),
    g: Object.fromEntries(Object.entries(G).map(([k, a]) => [k, `${mean(a, 'w').toFixed(1)}/${mean(a, 'wc').toFixed(1)}`])) , gerr: Object.fromEntries(Object.entries(G).map(([k, a]) => [k, err(a)])) });
}
rows.sort((a, b) => b.ag - a.ag);
console.log('run'.padEnd(16), 'setting'.padEnd(26), 'agree  overlap  width err   squeeze ours/chart   mean width ours/chart by group (BTN | LP | SB | BB)   group width err');
for (const r of rows) console.log(r.tag.padEnd(16), r.set.padEnd(26), `${r.ag.toFixed(1).padStart(5)}% ${r.ov.toFixed(0).padStart(5)}%   ${r.err.toFixed(1).padStart(5)}      ${r.sq.toFixed(1)} / ${r.sqc.toFixed(1)}          ${r.g.BTN} | ${r.g.LP} | ${r.g.SB} | ${r.g.BB}      ${['BTN', 'LP', 'SB', 'BB'].map((k) => k + ' ' + r.gerr[k].toFixed(1)).join('  ')}`);
