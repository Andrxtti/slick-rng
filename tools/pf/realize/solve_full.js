// Equity realization per hand class from real postflop solves (sampled engine with card abstraction, as the app runs).
// Same command line as solve_full.js so the PC worker can run it with cwd tools/pf/realize:
//   node solve_full.js <cfg.json> <secsPerFlop> <evSamplesK> <out.json>
// cfg: { names: [OOP, IP], stacks: [a, b], pot, ranges: ['AA:1,AKs:0.4,...', '...'], flops: ['Ks7d2c', ...], sizes }
// out: per flop, per player, per class: mean utility (chips won - chips put in postflop), all-in equity vs the
// opponent's range on that flop, sample count. Realization = ev / (eq * pot) - how much of its pot share a hand gets.
const fs = require('fs');
const E = require('../../../app/engine.js');
const [cfgFile, secsStr, evsStr, outFile] = process.argv.slice(2);
const cfg = JSON.parse(fs.readFileSync(cfgFile, 'utf8')), SECS = +(secsStr || 120), SAMPLES = 1000 * +(evsStr || 600);
const NC = 169, IDX = {}; E.CLASS_NAMES.forEach((n, i) => (IDX[n] = i));
const sizes = cfg.sizes || { flop: { bet: '33%, 75%', raise: '2.7x' }, turn: { bet: '66%', raise: '2.5x' }, river: { bet: '75%, a', raise: '2.5x, a' } };
const res = { cfg: { names: cfg.names, stacks: cfg.stacks, pot: cfg.pot }, flops: {} };
for (const flop of cfg.flops) {
  const spot = E.makeSpot({ players: cfg.names.map((n, i) => ({ name: n, stack: cfg.stacks[i], range: cfg.ranges[i] })), board: flop, pot: cfg.pot, sizes });
  const sv = new E.Solver(spot);
  const t0 = Date.now(); while (Date.now() - t0 < SECS * 1000) sv.run(5000);
  // play out deals under the average strategies; also score the all-in equity of each dealt hand
  const n = sv.n, sum = [new Float64Array(NC), new Float64Array(NC)], cnt = [new Float64Array(NC), new Float64Array(NC)], eqs = [new Float64Array(NC), new Float64Array(NC)];
  for (let i = 0; i < SAMPLES; i++) {
    sv.sampleDeal();
    let id = 0;
    for (;;) {
      const nd = sv.nodes[id];
      if (nd.kind === 2 || nd.kind === 3) { for (let p = 0; p < n; p++) { const c = E.CLASS_OF[sv.combo[p]]; sum[p][c] += sv.utility(nd, p); cnt[p][c]++; } break; }
      if (nd.kind === 1) { id = nd.child; continue; }
      const s = sv.avgStrategy(id, sv.key[nd.player][nd.street], nd.actions.length);
      let x = Math.random(), pick = s.length - 1;
      for (let a = 0; a < s.length; a++) { x -= s[a]; if (x < 0) { pick = a; break; } }
      id = nd.children[pick];
    }
    const s0 = sv.strength[0], s1 = sv.strength[1], w0 = s0 > s1 ? 1 : s0 === s1 ? 0.5 : 0;
    eqs[0][E.CLASS_OF[sv.combo[0]]] += w0; eqs[1][E.CLASS_OF[sv.combo[1]]] += 1 - w0;
  }
  const out = {};
  for (let p = 0; p < n; p++) {
    const o = {}; for (let c = 0; c < NC; c++) if (cnt[p][c] > 0) o[E.CLASS_NAMES[c]] = { ev: +(sum[p][c] / cnt[p][c]).toFixed(3), eq: +(eqs[p][c] / cnt[p][c]).toFixed(4), n: cnt[p][c] };
    out[cfg.names[p]] = o;
  }
  const ev = sv.estimateEV(20000);
  res.flops[flop] = { iters: sv.iters, ev: ev.map((x) => +x.ev.toFixed(3)), classes: out };
  console.log(flop, 'iters', sv.iters, 'EV', ev.map((x) => x.ev.toFixed(2)).join(' / '), `${((Date.now() - t0) / 1000).toFixed(0)}s`);
}
fs.writeFileSync(outFile, JSON.stringify(res));
