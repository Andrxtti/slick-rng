// Decompose a hand's action EVs at one node, with everyone else playing the shipped library strategy.
//   node probe_ev.js <cfg.json> <lib.json> <path e.g. raise|fold|fold|fold|fold> <hands: 88,76s,A5s,AQo> [evalSecs]
// Prints each action's EV, then (for the raise action) the EV conditional on each reply of the next player,
// by locking that player's reply and re-evaluating. Mean-strategy EVs, as the page shows them.
const fs = require('fs');
const { PF2, label, E } = require('./solver2.js');
const [cfgFile, libFile, pathStr, handsStr, secsStr, replyPrefix] = process.argv.slice(2);   // replyPrefix: actions after the raise (cold folds) before the player whose reply we decompose
const SECS = +(secsStr || 6);
const D = JSON.parse(fs.readFileSync(cfgFile, 'utf8')), lib = JSON.parse(fs.readFileSync(libFile, 'utf8'));
const cfg = { ...D.cfg }; delete cfg.allowLimps; delete cfg.leave1; delete cfg.lite; cfg.maxNodes = 900000;
const s = new PF2(cfg), NC = 169, IDX = {}; E.CLASS_NAMES.forEach((n, i) => (IDX[n] = i));
const b64 = (x) => Buffer.from(x, 'base64');
// load the library's strategies into the solver's average-strategy sums
let loaded = 0;
for (const n of lib.nodes) {
  const f = s.follow(n.p ? n.p.split('|').map((k) => ({ k })) : []); if (!f) continue;
  const nd = s.nodes[f.id]; if (nd.kind !== 'act') continue;
  const na = nd.actions.length; if (na !== n.a.length) continue;
  const S = b64(n.s);
  for (let c = 0; c < NC; c++) { const off = nd.off + c * 2 * na; for (let a = 0; a < na; a++) s.T[off + na + a] = S[c * na + a] / 255; }
  loaded++;
}
const lock = (id, a) => { const nd = s.nodes[id], na = nd.actions.length; for (let c = 0; c < NC; c++) { const off = nd.off + c * 2 * na; for (let k = 0; k < na; k++) s.T[off + na + k] = k === a ? 1 : 0; } };
const unlockFrom = (id, saved) => { s.T.set(saved); };
const f = s.follow(pathStr ? pathStr.split('|').map((k) => ({ k })) : []);
const nd = s.nodes[f.id], hands = handsStr.split(',');
console.log('library nodes loaded', loaded, '| node', pathStr, 'player', cfg.names[nd.player], 'actions', nd.actions.map((x) => label(nd, x, cfg)).join(' / '));
const evalNow = () => { s.EV2 = null; s.evalPass(SECS * 1000); };
evalNow();
const fmt = (v) => (v === null ? '  n/a ' : (v >= 0 ? '+' : '') + v.toFixed(2));
for (const h of hands) { const ev = s.evOf2(f.id, IDX[h]); console.log(`${h.padEnd(4)} ${ev ? ev.map((v, a) => `${label(nd, nd.actions[a], cfg)} ${fmt(v)}`).join('  ') : 'n/a'}`); }
// decompose the raise: next player's reply locked one at a time
const ra = nd.actions.findIndex((x) => x.k === 'raise'); if (ra < 0) process.exit(0);
let cid = nd.children[ra]; const pre = replyPrefix ? replyPrefix.split('|') : [];
for (const k of pre) { const x = s.nodes[cid], a = x.actions.findIndex((y) => y.k === k); lock(cid, a); cid = x.children[a]; }
const child = s.nodes[cid]; if (child.kind !== 'act') process.exit(0);
const saved = Float64Array.from(s.T);
const replyProb = (c) => s.avg(cid, c);
console.log(`\nafter the raise, ${cfg.names[child.player]} replies with ${child.actions.map((x) => label(child, x, cfg)).join(' / ')}`);
const rows = {}; for (const h of hands) rows[h] = [];
for (let a = 0; a < child.actions.length; a++) {
  s.T.set(saved); lock(cid, a); evalNow();
  for (const h of hands) { const ev = s.evOf2(f.id, IDX[h]); rows[h].push(ev ? ev[ra] : null); }
}
s.T.set(saved);
// reply frequencies against the actual range that raised (combo-weighted, from the library)
const libNode = lib.nodes.find((n) => n.p === pathStr), S0 = libNode ? b64(libNode.s) : null;
for (const h of hands) {
  const c = IDX[h], pr = replyProb(c);
  console.log(`${h.padEnd(4)} raise EV by reply: ` + rows[h].map((v, a) => `${label(child, child.actions[a], cfg)} ${fmt(v)}`).join('  '));
}
// how the next player replies on average (weighted by his range vs this raise): from the library node after the raise
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const after = lib.nodes.find((n) => n.p === [pathStr, 'raise', ...pre].filter(Boolean).join('|'));
if (after) { const S = b64(after.s), na = after.a.length, R = b64(after.r); let tot = 0; const sum = new Array(na).fill(0);
  for (let c = 0; c < NC; c++) { const w = R[c] / 255 * SIZE[c]; tot += w; for (let a = 0; a < na; a++) sum[a] += w * S[c * na + a] / 255; }
  console.log('reply mix overall:', after.lab.map((l, a) => `${l} ${(100 * sum[a] / tot).toFixed(0)}%`).join('  ')); }
