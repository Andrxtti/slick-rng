// Our solver on the 18 multiway matchups of the RYE 100bb "Multi-way" chart (8-max, 100bb, BB ante, chip EV).
//   node mw.js <secs> <tag> [stack]     -> out/mw<stack>_<tag>.json (average strategy + EVs at each node)
// Seat map for 8-max: EP = UTG, EP+1 = UTG1, MP / EP+2 = UTG2, then HJ, CO, BTN (BU), SB, BB. LP flat = BTN unless noted.
const fs = require('fs');
const { PF2, label, E } = require('./solver2.js');
const SECS = +(process.argv[2] || 600), TAG = process.argv[3] || 'a', S = +(process.argv[4] || 100);
const nm = ['UTG', 'UTG1', 'UTG2', 'HJ', 'CO', 'BTN', 'SB', 'BB'];
const cfg = { names: nm, stacks: nm.map(() => S), posts: nm.map((x) => (x === 'SB' ? 0.5 : x === 'BB' ? 1 : 0)), ante: 1, payoff: { type: 'chips' }, postOrder: [6, 7, 0, 1, 2, 3, 4, 5], maxNodes: 2e6 };
const F = (n) => new Array(n).fill('fold');
// action paths from the first seat (UTG) to the hero's decision
const SPOTS = [
  ['BTN vs EP open + EP flat', ['raise', 'call', 'fold', 'fold', 'fold']],
  ['BTN vs EP open + MP flat', ['raise', 'fold', 'call', 'fold', 'fold']],
  ['BTN vs EP open + CO flat', ['raise', 'fold', 'fold', 'fold', 'call']],
  ['BTN vs HJ open + CO flat', [...F(3), 'raise', 'call']],
  ['BTN vs MP open + CO flat', [...F(2), 'raise', 'fold', 'call']],
  ['HJ vs EP open + EP flat', ['raise', 'call', 'fold']],
  ['EP+2 vs EP open + EP+1 flat', ['raise', 'call']],
  ['MP vs EP open + EP flat', ['raise', 'call']],
  ['SB vs EP open + EP/MP flat', ['raise', 'fold', 'call', 'fold', 'fold', 'fold']],
  ['SB vs CO open + BTN flat', [...F(4), 'raise', 'call']],
  ['SB vs EP open + LP flat', ['raise', ...F(4), 'call']],
  ['BB vs EP open + SB flat', ['raise', ...F(5), 'call']],
  ['BB vs EP open + BTN/CO flat', ['raise', ...F(4), 'call', 'fold']],
  ['BB vs EP open + EP/MP flat', ['raise', 'fold', 'call', ...F(4)]],
  ['BB vs MP open + LP flat', [...F(2), 'raise', 'fold', 'fold', 'call', 'fold']],
  ['BB vs MP open + SB flat', [...F(2), 'raise', ...F(3), 'call']],
  ['BB vs CO open + BTN flat', [...F(4), 'raise', 'call', 'fold']],
  ['BB vs BTN open + SB flat', [...F(5), 'raise', 'call']],
  // heads-up nodes (one opener), for the same fit: where the offsuit-ace flats and the suited calls show up
  ['BTN vs CO open', [...F(4), 'raise']],
  ['SB vs BTN open', [...F(5), 'raise']],
  ['BTN vs UTG open', ['raise', ...F(4)]],
  ['BB vs UTG open', ['raise', ...F(6)]],
  ['BB vs BTN open', [...F(5), 'raise', 'fold']],
  ['UTG1 vs UTG open', ['raise']],
  ['CO vs UTG open', ['raise', 'fold', 'fold', 'fold']],
  // 3-bet / 4-bet structure (GTO Wizard reference nodes read Oct 26)
  ['BTN vs SB 3-bet', [...F(5), 'raise', 'raise', 'fold']],
  ['SB vs BTN 4-bet', [...F(5), 'raise', 'raise', 'fold', 'raise']],
  ['UTG vs CO/BTN flats + SB squeeze', ['raise', 'fold', 'fold', 'fold', 'call', 'call', 'raise', 'fold']],
];
const s = new PF2(cfg);
const f = (keys) => { let id = 0; for (const k of keys) { const nd = s.nodes[id]; if (!nd || nd.kind !== 'act') return -1; const a = nd.actions.findIndex((x) => x.k === k); if (a < 0) return -1; id = nd.children[a]; } return id; };
const SIZE = new Array(169).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const ids = SPOTS.map(([t, p]) => { const id = f(p); if (id < 0 || s.nodes[id].kind !== 'act') throw new Error('no node for ' + t); return id; });
const width = (id) => { const nd = s.nodes[id], fi = nd.actions.findIndex((a) => a.k === 'fold'); let w = 0; for (let c = 0; c < 169; c++) w += SIZE[c] * (1 - s.avg(id, c)[fi]); return (100 * w) / 1326; };
const t0 = Date.now(), step = Math.min(300, SECS);
for (let done = 0; done < SECS; done += step) {
  s.run(step * 1000);
  console.log(`${TAG} ${Math.round((Date.now() - t0) / 1000)}s iters ${s.iters}: ` + ids.map((id) => width(id).toFixed(1)).join(' '));
}
s.evalPass(+(process.env.EVSECS || 90) * 1000);
const out = { stack: S, tag: TAG, iters: s.iters, model: require('./solver2.js').MODEL, mw: require('./solver2.js').MW, spots: SPOTS.map(([t, p], k) => {
  const id = ids[k], nd = s.nodes[id];
  // the ranges that got here: for every voluntary action on the path, that seat's frequency of taking it by hand class
  const pre = []; let x = 0;
  for (const k of p) { const n0 = s.nodes[x], ai = n0.actions.findIndex((a) => a.k === k); if (k !== 'fold') pre.push({ seat: nm[n0.player], act: k, w: Array.from({ length: 169 }, (_, c) => +s.avg(x, c)[ai].toFixed(4)) }); x = n0.children[ai]; }
  return { title: t, path: p, hero: nm[nd.player], acts: nd.actions.map((a) => a.k), labels: nd.actions.map((a) => label(nd, a, cfg)), pre,
    st: Array.from({ length: 169 }, (_, c) => s.avg(id, c).map((x) => +x.toFixed(4))), ev: Array.from({ length: 169 }, (_, c) => (s.evOf2(id, c) || []).map((x) => +x.toFixed(3))) };
}) };
fs.writeFileSync(`${__dirname}/out/mw${S}_${TAG}.json`, JSON.stringify(out));
console.log('saved', `out/mw${S}_${TAG}.json`);
// SCAN="play,gam,r;play,gam,r;..." re-scores the solved tree under other multiway settings and reports each hero's
// best response (pure, by EV) against the charts. Everyone else keeps the solved strategy, so this is a local probe.
if (process.env.SCAN) {
  const { MW } = require('./solver2.js'), { score } = require('./mw_charts.js');
  const br = (id, c) => { const nd = s.nodes[id], ev = s.evOf2(id, c), o = { fold: 0, call: 0, raise: 0 }; if (!ev) return { fold: 1, call: 0, raise: 0 };
    let b = 0; ev.forEach((v, i) => { if (v > ev[b]) b = i; }); const k = nd.actions[b].k; o[k === 'allin' ? 'raise' : k] = 1; return o; };
  for (const combo of process.env.SCAN.split(';')) {
    const [play, gam, r] = combo.split(',').map(Number); MW.on = 1; MW.play = play; MW.gam = gam; MW.r = r;
    s.EV2.fill(0); s.EW2.fill(0); s.evalPass(+(process.env.SCANSECS || 45) * 1000);
    let ag = 0, ov = 0; const parts = [];
    SPOTS.forEach(([t], k) => { const m = score(E, t, (c) => br(ids[k], c)); ag += m.ag / SPOTS.length; ov += m.ov / SPOTS.length; parts.push(`${m.w.toFixed(0)}/${m.wc.toFixed(0)}`); });
    console.log(`SCAN play ${play} gam ${gam} r ${r}: agree ${ag.toFixed(1)}% continue-overlap ${ov.toFixed(0)}% | widths ours/chart ${parts.join(' ')}`);
  }
}
