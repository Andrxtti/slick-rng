// The heads-up 3-bet / 4-bet structure, solved where it is dense: a 3-handed BTN / SB / BB table (the same game as
// GTO Wizard's 8-max nodes after five folds; the BB ante is the same 1bb). Spot titles match gwcmp.js, so
//   node hu.js <secs> <tag> [stack]   -> out/mw<stack>_<tag>.json, then  node gwcmp.js out/mw<stack>_<tag>.json
const fs = require('fs');
const { PF2, label, E } = require('./solver2.js');
const SECS = +(process.argv[2] || 300), TAG = process.argv[3] || 'hu', S = +(process.argv[4] || 100);
const nm = ['BTN', 'SB', 'BB'];
const cfg = { names: nm, stacks: nm.map(() => S), posts: [0, 0.5, 1], ante: 1, payoff: { type: 'chips' }, postOrder: [1, 2, 0], maxNodes: 2e6 };
const SPOTS = [
  ['SB vs BTN open', ['raise']],
  ['BB vs BTN open', ['raise', 'fold']],
  ['BB vs BTN open + SB flat', ['raise', 'call']],
  ['BTN vs SB 3-bet', ['raise', 'raise', 'fold']],
  ['SB vs BTN 4-bet', ['raise', 'raise', 'fold', 'raise']],
];
const s = new PF2(cfg);
const f = (keys) => { let id = 0; for (const k of keys) { const nd = s.nodes[id]; if (!nd || nd.kind !== 'act') return -1; const a = nd.actions.findIndex((x) => x.k === k); if (a < 0) return -1; id = nd.children[a]; } return id; };
const ids = SPOTS.map(([t, p]) => { const id = f(p); if (id < 0 || s.nodes[id].kind !== 'act') throw new Error('no node for ' + t); return id; });
console.log('tree', s.nodes.length, 'nodes;', SPOTS.map(([t], k) => t + ': ' + s.nodes[ids[k]].actions.map((a) => a.k + (a.to ? ' ' + a.to : '')).join('/')).join(' | '));
const t0 = Date.now(), step = Math.min(60, SECS);
for (let done = 0; done < SECS; done += step) { s.run(step * 1000); console.log(`${TAG} ${Math.round((Date.now() - t0) / 1000)}s iters ${s.iters}`); }
s.evalPass(+(process.env.EVSECS || 30) * 1000);
const out = { stack: S, tag: TAG, iters: s.iters, model: require('./solver2.js').MODEL, mw: require('./solver2.js').MW, spots: SPOTS.map(([t, p], k) => {
  const id = ids[k], nd = s.nodes[id];
  return { title: t, path: p, hero: nm[nd.player], acts: nd.actions.map((a) => a.k), labels: nd.actions.map((a) => label(nd, a, cfg)),
    st: Array.from({ length: 169 }, (_, c) => s.avg(id, c).map((x) => +x.toFixed(4))), ev: Array.from({ length: 169 }, (_, c) => (s.evOf2(id, c) || []).map((x) => +x.toFixed(3))) };
}) };
fs.writeFileSync(`${__dirname}/out/mw${S}_${TAG}.json`, JSON.stringify(out));
console.log('saved', `out/mw${S}_${TAG}.json`);
