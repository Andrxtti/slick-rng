// SOLVER_PLAN P2, the check: do the leaf values reproduce P1? For every finished P1 solve, each class's measured postflop
// result (realize/solve_full.js ev) against the P2 leaf formula: sum over the opponent's range of
// pot * R_me[c]*eq / (R_me[c]*eq + R_opp[o]*(1-eq)), with R from p2/real_table.json for that pot type and depth.
//   node tools/pf/p2_check.js   -> per pot type x depth: mean absolute error in bb and as a share of the pot
// "Done" (SOLVER_PLAN P2): the error is small next to the pot (target: under 5% of the pot on average).
const fs = require('fs'), path = require('path');
const E = require('../../app/engine.js');
const NAMES = E.CLASS_NAMES, NC = 169, EQ = E.EQ169, IDX = Object.fromEntries(NAMES.map((h, i) => [h, i]));
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const T = JSON.parse(fs.readFileSync(path.join(__dirname, 'p2', 'real_table.json'), 'utf8'));
const dir = path.join(__dirname, 'p1', 'out'), cfgdir = path.join(__dirname, 'p1', 'cfgs');
const R = (side) => (c) => side.R[NAMES[c]] ?? side.mean;
const parse = (s) => { const w = new Array(NC).fill(0); for (const t of s.split(',')) { const [h, x] = t.split(':'); w[IDX[h]] = +x * SIZE[IDX[h]]; } return w; };
// measured: each class's result averaged over the flops solved so far (the table is a flop average too)
const meas = {}, cfgs = {};
for (const f of fs.readdirSync(dir).filter((x) => x.endsWith('.json'))) {
  const m = f.match(/^(.+)-d(\d+)-\d+\.json$/); if (!m) continue;
  const key = `${m[1]}|${m[2]}`, D = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8'));
  cfgs[key] = cfgs[key] || { D, cfg: JSON.parse(fs.readFileSync(path.join(cfgdir, f), 'utf8')) };
  const M = meas[key] || (meas[key] = [{}, {}]);
  for (const fl of Object.values(D.flops)) D.cfg.names.forEach((who, pi) => {
    for (const [h, v] of Object.entries(fl.classes[who] || {})) { const a = M[pi][h] || (M[pi][h] = { ev: 0, n: 0 }); a.ev += v.ev * v.n; a.n += v.n; }
  });
}
const res = {};
for (const [key, M] of Object.entries(meas)) {
  const row = T[key]; if (!row || row.oop.mean == null) continue;
  const { D, cfg } = cfgs[key];
  const W = [parse(cfg.ranges[0]), parse(cfg.ranges[1])], Rs = [R(row.oop), R(row.ip)], pot = D.cfg.pot;
  const acc = res[key] || (res[key] = { err: 0, w: 0, bias: 0 });
  D.cfg.names.forEach((who, pi) => {
    const op = 1 - pi;
    for (const [h, v0] of Object.entries(M[pi])) {
      if (v0.n < 400) continue; const v = { ev: v0.ev / v0.n, n: v0.n }, c = IDX[h]; let s = 0, ws = 0;
      for (let o = 0; o < NC; o++) { const w = W[op][o]; if (!(w > 0)) continue; const eq = EQ[c * NC + o], a = Rs[pi](c) * eq, b = Rs[op](o) * (1 - eq); s += w * (a + b > 0 ? a / (a + b) : 0.5); ws += w; }
      const pred = pot * (s / ws), meas = v.ev;
      acc.err += v.n * Math.abs(pred - meas); acc.bias += v.n * (pred - meas); acc.w += v.n;
    }
  });
}
console.log('| pot type | bb | pot | mean abs error (bb) | error / pot | bias (bb) |\n|---|---|---|---|---|---|');
for (const [k, a] of Object.entries(res).sort()) {
  const row = T[k], e = a.err / a.w;
  console.log(`| ${row.type} | ${row.depth} | ${row.pot} | ${e.toFixed(3)} | ${((e / row.pot) * 100).toFixed(1)}% | ${(a.bias / a.w).toFixed(3)} |`);
}
