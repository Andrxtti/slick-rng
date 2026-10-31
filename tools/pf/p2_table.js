// SOLVER_PLAN P2, step 1: the realization table from P1's heads-up postflop solves.
//   node tools/pf/p2_table.js   -> tools/pf/p2/real_table.json, real_summary.md
// Per pot type x depth x position (OOP/IP) x hand class: realization = postflop result / (all-in equity x pot), the share of
// its pot equity a hand actually takes home when the hand is played out (realize/solve_full.js), averaged over the 12 flops
// weighted by samples. Classes with too few samples fall back to their bucket (pairs / suited / offsuit, by equity rank),
// then to the position's mean. SPR comes from p1/index.json. P2 step 2 makes the preflop solver read this table.
const fs = require('fs'), path = require('path');
const E = require('../../app/engine.js');
const NAMES = E.CLASS_NAMES, NC = 169, MIN_N = 120;
const dir = path.join(__dirname, 'p1', 'out'), index = JSON.parse(fs.readFileSync(path.join(__dirname, 'p1', 'index.json'), 'utf8'));
const acc = {};   // key type|depth -> [OOP, IP] -> class -> {ev, eqpot, n}
for (const f of fs.readdirSync(dir).filter((x) => x.endsWith('.json'))) {
  const m = f.match(/^(.+)-d(\d+)-\d+\.json$/); if (!m) continue;
  const D = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8')), key = `${m[1]}|${m[2]}`, pot = D.cfg.pot;
  const A = acc[key] || (acc[key] = [{}, {}]);
  for (const fl of Object.values(D.flops)) D.cfg.names.forEach((who, pi) => {
    for (const [h, v] of Object.entries(fl.classes[who] || {})) {
      const a = A[pi][h] || (A[pi][h] = { ev: 0, eqpot: 0, n: 0, flops: 0 });
      a.ev += v.ev * v.n; a.eqpot += v.eq * pot * v.n; a.n += v.n; a.flops++;
    }
  });
}
// fixed point: R_c <- R_c * measured / predicted, where predicted = pot * mean over the opponent's range of
// R_c*eq / (R_c*eq + R_o*(1-eq)) (the leaf solver2.js realTab values); classes under MIN_N keep the side's mean
const IDX = Object.fromEntries(NAMES.map((h, i) => [h, i])), EQ = E.EQ169;
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const parse = (s) => { const w = new Array(NC).fill(0); for (const t of s.split(',')) { const [h, x] = t.split(':'); w[IDX[h]] = +x * SIZE[IDX[h]]; } return w; };
function fit(key, A, out) {
  const [type, d] = key.split('|'), cfg = JSON.parse(fs.readFileSync(path.join(__dirname, 'p1', 'cfgs', `${type}-d${d}-00.json`), 'utf8'));
  const W = [parse(cfg.ranges[0]), parse(cfg.ranges[1])], pot = cfg.pot;
  const Rv = out.map((o) => Float64Array.from(NAMES, (h) => o.R[h] ?? o.mean ?? 1));
  for (let it = 0; it < 60; it++) for (let pi = 0; pi < 2; pi++) {
    const op = 1 - pi;
    for (const [h, a] of Object.entries(A[pi])) {
      if (a.n < MIN_N || !(out[pi].R[h] > 0)) continue;
      const c = IDX[h], meas = a.ev / a.n; let s = 0, ws = 0;
      for (let o = 0; o < NC; o++) { const w = W[op][o]; if (!(w > 0)) continue; const eq = EQ[c * NC + o], x = Rv[pi][c] * eq, y = Rv[op][o] * (1 - eq); s += w * (x + y > 0 ? x / (x + y) : 0.5); ws += w; }
      const pred = pot * s / ws; if (pred > 1e-6 && meas > 0) Rv[pi][c] = Math.min(4, Math.max(0.05, Rv[pi][c] * Math.pow(meas / pred, 0.7)));
    }
  }
  out.forEach((o, pi) => { for (const h of Object.keys(o.R)) o.R[h] = +Rv[pi][IDX[h]].toFixed(4); });
}
const table = {}, md = ['# P2 realization table (from P1 heads-up postflop solves)', '',
  'Realization = postflop result / (equity x pot), by pot type, depth and position; mean over classes weighted by samples.', '',
  '| pot type | bb | SPR | flops done | OOP realization | IP realization | classes measured (OOP/IP) |', '|---|---|---|---|---|---|---|'];
for (const [key, A] of Object.entries(acc).sort()) {
  const [type, d] = key.split('|'), ix = index.find((x) => x.id === type && x.d === +d) || {};
  const out = A.map((P) => {
    const R = {}; let sw = 0, se = 0;
    for (const [h, a] of Object.entries(P)) if (a.n >= MIN_N && a.eqpot > 0) { R[h] = +(a.ev / a.eqpot).toFixed(4); sw += a.eqpot; se += a.ev; }
    return { R, mean: sw ? +(se / sw).toFixed(4) : null, measured: Object.keys(R).length, flops: Math.max(0, ...Object.values(P).map((a) => a.flops)) };
  });
  fit(key, A, out);   // refine R so the pairwise leaf formula reproduces each class's measured result
  table[key] = { type, depth: +d, spr: ix.spr, pot: ix.pot, oop: out[0], ip: out[1] };
  md.push(`| ${type} | ${d} | ${ix.spr} | ${out[0].flops} | ${out[0].mean} | ${out[1].mean} | ${out[0].measured}/${out[1].measured} |`);
}
fs.mkdirSync(path.join(__dirname, 'p2'), { recursive: true });
fs.writeFileSync(path.join(__dirname, 'p2', 'real_table.json'), JSON.stringify(table));
fs.writeFileSync(path.join(__dirname, 'p2', 'real_summary.md'), md.join('\n') + '\n');
console.log(md.slice(4).join('\n'));
