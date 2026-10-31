// Our solver on the Pairrd reference table (RYE videos): 8-max, everyone 30bb, chip EV, BB ante. Compare the frequencies.
const { PF2, label, E } = require('./solver2.js');
const SECS = +(process.argv[2] || 240), ANTE = process.argv[3] || 'bb';
const nm = ['UTG', 'UTG1', 'UTG2', 'HJ', 'CO', 'BTN', 'SB', 'BB'], S = +(process.env.STACK || 30);
const posts = nm.map((x) => (x === 'SB' ? 0.5 : x === 'BB' ? 1 : 0));
const cfg = { names: nm, stacks: nm.map(() => S), posts, ante: ANTE === 'bb' ? 1 : 1, payoff: { type: 'chips' }, postOrder: [6, 7, 0, 1, 2, 3, 4, 5] };
const s = new PF2(cfg);
const SIZE = new Array(169).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const f = (keys) => { let id = 0; for (const k of keys) { const nd = s.nodes[id]; if (nd.kind !== 'act') return -1; const a = nd.actions.findIndex((x) => x.k === k); if (a < 0) return -1; id = nd.children[a]; } return id; };
const freq = (id, reach) => { const nd = s.nodes[id]; const out = {}; let tot = 0; nd.actions.forEach((a) => (out[label(nd, a, cfg)] = 0)); for (let c = 0; c < 169; c++) { const w = SIZE[c] * (reach ? reach[c] : 1); tot += SIZE[c]; s.avg(id, c).forEach((x, i) => (out[label(nd, nd.actions[i], cfg)] += w * x)); } for (const k in out) out[k] = (100 * out[k] / 1326).toFixed(1) + '%'; return out; };
const reachOf = (id, a) => Array.from({ length: 169 }, (_, c) => s.avg(id, c)[a]);
const t0 = Date.now();
for (let k = 1; k <= SECS / 60; k++) {
  s.run(60000);
  const utg = 0, hjNode = f(['raise', 'fold', 'fold']);
  const utgR = reachOf(0, s.nodes[0].actions.findIndex((a) => a.k === 'raise'));
  const btn = f(['fold', 'fold', 'fold', 'fold', 'fold']);
  const btnR = reachOf(btn, s.nodes[btn].actions.findIndex((a) => a.k === 'raise'));
  const btnVsSbJam = f(['fold', 'fold', 'fold', 'fold', 'fold', 'raise', 'allin', 'fold']);
  const utgVsHj = f(['raise', 'fold', 'fold', 'raise', 'fold', 'fold', 'fold', 'fold']);
  console.log(`--- ${Math.round((Date.now() - t0) / 1000)}s iters ${s.iters}`);
  console.log('UTG RFI (ref cEV 17.4%):', JSON.stringify(freq(0)));
  console.log('BTN RFI (ref 48.2%):', JSON.stringify(freq(btn)));
  console.log('HJ vs UTG open (ref call 9.3 / 3B 5.2 / jam 0):', JSON.stringify(freq(hjNode)));
  console.log('UTG vs HJ 3B (ref jam 3.8 / call 7.4 / fold 6.8 of 18):', JSON.stringify(freq(utgVsHj, utgR)));
  console.log('BTN vs SB 3B-jam (ref call 13.4 of 48.2):', JSON.stringify(freq(btnVsSbJam, btnR)));
  const bbVsUtg = f(['raise', 'fold', 'fold', 'fold', 'fold', 'fold', 'fold']);
  console.log('BB vs UTG open (R6 40bb cEV: call 61.1 / 3B 6.4):', JSON.stringify(freq(bbVsUtg)));
  const sbVsBtn = f(['fold', 'fold', 'fold', 'fold', 'fold', 'raise']), bbVsBtn = f(['fold', 'fold', 'fold', 'fold', 'fold', 'raise', 'fold']);
  console.log('SB vs BTN open (of BTN range):', JSON.stringify(freq(sbVsBtn)));
  console.log('BB vs BTN open:', JSON.stringify(freq(bbVsBtn)));
  const btnVsBb3 = f(['fold', 'fold', 'fold', 'fold', 'fold', 'raise', 'fold', 'raise']);
  if (btnVsBb3 >= 0) console.log('BTN vs BB small 3B (of BTN range):', JSON.stringify(freq(btnVsBb3, btnR)));
}
