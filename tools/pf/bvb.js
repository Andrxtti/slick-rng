// Blind vs blind benchmark: folded to the SB, 8-max ChipEV, BB ante (12.5% of BB each = 1bb dead).
// Reference: GTO Preflop "Blind vs Blind Strategy: The Solved Game From 3bb to 100bb" (8-max ChipEV, 12.5% ante).
//   node bvb.js <depth> <secs>
const { PF2, label, E } = require('./solver2.js');
const S = +(process.argv[2] || 15), SECS = +(process.argv[3] || 60);
const REF = {
  10: 'SB fold 21.9 / limp 29.0 / raise 0 / jam 49.1',
  12: 'SB fold 20.1 / limp 37.4 / raise 2.5bb 3.8 / jam 38.8',
  15: 'SB fold 15.7 / limp 49.9 / raise 2.5bb 11.2 / jam 23.2 ; BB vs limp: check 55.9 / raise 3bb 30.0 / jam 14.1 ; BB vs 2.5 open: fold 23.8 / call 57.5 / jam 18.7 ; BB calls SB jam 30.4',
  20: 'SB fold 13.2 / limp 58.0 / raise 3bb 16.9 / jam 11.9',
  25: 'SB fold 12.4 / limp 60.6 / raise 3bb 21.4 / jam 5.6',
  30: 'SB fold 14.8 / limp 58.5 / raise 3.5bb 25.3 / jam 1.3',
  40: 'SB fold 15.3 / limp 59.2 / raise 3.5bb 25.5 / jam 0',
};
const cfg = { names: ['SB', 'BB'], stacks: [S, S], posts: [0.5, 1], ante: 1, payoff: { type: 'chips' }, postOrder: [0, 1] };
if (process.env.SBOPEN) cfg.sbOpen = +process.env.SBOPEN;
const s = new PF2(cfg);
const SIZE = new Array(169).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const freq = (id, reach) => { const nd = s.nodes[id]; const out = {}; nd.actions.forEach((a) => (out[label(nd, a, cfg)] = 0)); let tot = 0; for (let c = 0; c < 169; c++) { const w = SIZE[c] * (reach ? reach[c] : 1); tot += w; s.avg(id, c).forEach((x, i) => (out[label(nd, nd.actions[i], cfg)] += w * x)); } for (const k in out) out[k] = (100 * out[k] / tot).toFixed(1) + '%'; return out; };
const child = (id, k) => { const nd = s.nodes[id]; const a = nd.actions.findIndex((x) => x.k === k); return a < 0 ? -1 : nd.children[a]; };
s.run(SECS * 1000);
console.log(`--- ${S}bb  iters ${s.iters}`);
console.log('REF', REF[S] || '-');
console.log('SB first in:', JSON.stringify(freq(0)));
const lim = child(0, 'call'), op = child(0, 'raise'), jm = child(0, 'allin');
if (lim >= 0) console.log('BB vs limp:', JSON.stringify(freq(lim)));
if (op >= 0) console.log('BB vs open:', JSON.stringify(freq(op)));
if (jm >= 0) console.log('BB vs jam:', JSON.stringify(freq(jm)));
