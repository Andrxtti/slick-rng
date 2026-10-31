const fs = require('fs'); const { PF2, label, E } = require('./solver2.js');
const [file, SECS = 60] = process.argv.slice(2);
const D = JSON.parse(fs.readFileSync(file, 'utf8')); const cfg = { ...D.cfg }; delete cfg.allowLimps; delete cfg.leave1;
const s = new PF2(cfg); const N = E.CLASS_NAMES, ci = (n) => N.indexOf(n);
const follow = (keys) => { let id = 0; for (const k of keys) { const nd = s.nodes[id]; const a = nd.actions.findIndex((x) => x.k === k); if (a < 0) return -1; id = nd.children[a]; } return id; };
const SIZE = new Array(169).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const freq = (id, reach) => { const nd = s.nodes[id]; const f = new Array(nd.actions.length).fill(0); let d = 0; for (let c = 0; c < 169; c++) { const w = SIZE[c] * (reach ? reach[c] : 1); d += w; s.avg(id, c).forEach((x, a) => (f[a] += w * x)); } return f.map((x) => (100 * x / d).toFixed(1)); };
for (let t = 0; t < SECS / 20; t++) {
  s.run(20000); s.EV2 = null; s.evalPass(5000);
  const bb = follow(['fold', 'fold', 'fold', 'raise', 'fold']);  // BTN opens, SB folds -> BB
  const nd = s.nodes[bb];
  const show = (h) => { const c = ci(h); return h + ' ' + s.avg(bb, c).map((x) => x.toFixed(2)).join('/') + ' ev ' + (s.evOf2(bb, c) || []).map((x) => x.toFixed(2)).join('/'); };
  const btnVsJam = nd.children[nd.actions.findIndex((a) => a.k === 'allin')];
  const btnReach = new Array(169).fill(0).map((_, c) => s.avg(follow(['fold', 'fold', 'fold']), c)[1]);
  console.log(`${Math.round((t + 1) * 20)}s it ${s.iters} | BB acts ${nd.actions.map((a) => label(nd, a, cfg)).join(',')} freq ${freq(bb).join('/')} | ${show('JTs')} | ${show('KQo')} | ${show('A5s')} | BTN vs jam call% ${freq(btnVsJam, btnReach)[1]}`);
}
