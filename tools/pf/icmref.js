// ICM version of the Pairrd 30bb reference table (RYE Masterclass frames: "250 man field, 50% left, 30bb avg").
// Payouts are a standard 15%-paid ladder (the frame does not show Pairrd's exact one), so compare directionally:
//   cEV -> ICM: BTN RFI 48.2 -> 45.7; HJ vs UTG call 9.3 -> 3.9, 3-bet 5.2 -> 6.9; BB vs UTG (40bb) defend 67.4 -> 47.6
//   node icmref.js <secs>
const { PF2, label, E } = require('./solver2.js');
const SECS = +(process.argv[2] || 180), S = +(process.env.STACK || 30), LEFT = +(process.env.LEFT || 125), FIELD = 250;
const nm = ['UTG', 'UTG1', 'UTG2', 'HJ', 'CO', 'BTN', 'SB', 'BB'];
const paid = Math.round(FIELD * 0.15);
const raw = []; for (let i = 1; i <= paid; i++) raw.push(1 / Math.pow(i, 0.9));
const tot = raw.reduce((a, b) => a + b, 0), payouts = raw.map((x) => (100 * x) / tot);
const cfg = { names: nm, stacks: nm.map(() => S), posts: nm.map((x) => (x === 'SB' ? 0.5 : x === 'BB' ? 1 : 0)), ante: 1, postOrder: [6, 7, 0, 1, 2, 3, 4, 5],
  payoff: { type: 'icm', payouts, others: new Array(LEFT - 8).fill(S) } };
const s = new PF2(cfg);
const SIZE = new Array(169).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const f = (keys) => { let id = 0; for (const k of keys) { const nd = s.nodes[id]; if (nd.kind !== 'act') return -1; const a = nd.actions.findIndex((x) => x.k === k); if (a < 0) return -1; id = nd.children[a]; } return id; };
const freq = (id) => { const nd = s.nodes[id]; const out = {}; nd.actions.forEach((a) => (out[label(nd, a, cfg)] = 0)); for (let c = 0; c < 169; c++) s.avg(id, c).forEach((x, i) => (out[label(nd, nd.actions[i], cfg)] += SIZE[c] * x)); for (const k in out) out[k] = (100 * out[k] / 1326).toFixed(1) + '%'; return out; };
const t0 = Date.now();
for (let k = 1; k <= SECS / 60; k++) {
  s.run(60000);
  console.log(`--- ${Math.round((Date.now() - t0) / 1000)}s iters ${s.iters}  (${S}bb, ${LEFT} left of ${FIELD}, ${paid} paid)`);
  console.log('BTN RFI (ICM ref 45.7 at 30bb):', JSON.stringify(freq(f(['fold', 'fold', 'fold', 'fold', 'fold']))));
  console.log('HJ vs UTG (ICM ref call 3.9 / 3B 6.9 at 30bb):', JSON.stringify(freq(f(['raise', 'fold', 'fold']))));
  console.log('BB vs UTG (ICM ref 40bb/30% left: call 42.3 / 3B 5.4):', JSON.stringify(freq(f(['raise', 'fold', 'fold', 'fold', 'fold', 'fold', 'fold']))));
}
