// RYE Masterclass test spots (rye_notes batch1 §3), on equal-stack 8-max chip-EV tables with a BB ante.
// Most course answers are his recommendations (some exploitative), so this is a directional agreement check.
//   node bench.js <stack> <secs>
const { PF2, label, E } = require('./solver2.js');
const S = +(process.argv[2] || 20), SECS = +(process.argv[3] || 180);
const nm = ['UTG', 'UTG1', 'UTG2', 'HJ', 'CO', 'BTN', 'SB', 'BB'];
const cfg = { names: nm, stacks: nm.map(() => S), posts: nm.map((x) => (x === 'SB' ? 0.5 : x === 'BB' ? 1 : 0)), ante: 1, payoff: { type: 'chips' }, postOrder: [6, 7, 0, 1, 2, 3, 4, 5] };
const s = new PF2(cfg); s.run(SECS * 1000); s.evalPass(30000);
const N = E.CLASS_NAMES;
const f = (keys) => { let id = 0; for (const k of keys) { const nd = s.nodes[id]; if (!nd || nd.kind !== 'act') return -1; const a = nd.actions.findIndex((x) => x.k === k); if (a < 0) return -1; id = nd.children[a]; } return id; };
const F = (n) => new Array(n).fill('fold');
const SPOTS = {
  15: [
    ['#14 UTG+1 first in: ATs/KQs/AQo jam, AJo min-raise, ATo min-raise-fold, 66 fold', ['fold'], ['ATs', 'KQs', 'AQo', 'AJo', 'ATo', '66']],
    ['#15 UTG first in: 77 min-raise', [], ['77']],
    ['#16 HJ first in: KQo jam, 44/55 jam, 33 not', F(3), ['KQo', '44', '55', '33']],
  ],
  10: [['#18 BTN first in 10bb: K8o, 54s, 86s jam; 65s', F(5), ['K8o', '54s', '86s', '65s']],
       ['B4#3 MP3/HJ first in 10bb: 86s, T7s, J7s jam (Medium)', F(3), ['86s', 'T7s', 'J7s']]],
  12: [['B4#1 HJ first in 12bb: A5o+, KTo, QTo, JTo jam (High, HRC)', F(3), ['A5o', 'A7o', 'KTo', 'QTo', 'JTo', 'K7s']]],
  13: [['B4#6 BTN/CO first in 13bb: A4o jam (High); K9o, Q5s jam', F(4), ['A4o', 'K9o', 'Q5s']],
       ['B4#4 UTG first in 13bb: AKo/AQo jam not min-raise (Medium)', [], ['AKo', 'AQo']]],
  22: [['B4#31 cold vs UTG min-raise 22bb: AQo flat (High)', ['raise', 'fold'], ['AQo']]],
  11: [['#18 BTN first in 11bb: fold K8o, 54s, 86s; jam 65s', F(5), ['K8o', '54s', '86s', '65s']]],
  8: [['#19 BTN first in 8bb: jam K7o, Q8o, Q3s', F(5), ['K7o', 'Q8o', 'Q3s']]],
  20: [
    ['#21 BB vs CO open: AQo, A5s jam', [...F(4), 'raise', 'fold', 'fold'], ['AQo', 'A5s']],
    ['#22 BB vs UTG open: small pairs jam', ['raise', ...F(6)], ['22', '33', '44', '55', '66']],
    ['#10 UTG opens, faces HJ 3-bet: 99/TT/AQo call, AK jam', ['raise', 'fold', 'fold', 'raise', 'fold', 'fold', 'fold', 'fold'], ['99', 'TT', 'AQo', 'AKo', 'AKs']],
    ['#40 UTG first in 20bb: T9s bottom, 98s borderline, 87s/76s/65s/K9s fold', [], ['T9s', '98s', '87s', '76s', '65s', 'K9s', 'A5s', 'KQo', 'ATo']],
    ['#20 BTN vs CO min-raise: AA/KK/AKs 3-bet small', [...F(4), 'raise'], ['AA', 'KK', 'AKs']],
    ['#39 CO first in 20bb: QTo open', F(4), ['QTo']],
    ['B4#19 BTN first in 20bb: KTo, low offsuit Ax min-raise > jam (High); A8o close; 88 min-raise', F(5), ['KTo', 'A4o', 'A6o', 'A8o', '88', 'K9s', 'Q9s', 'J9s']],
    ['B4#22 BB vs BTN min-raise 20bb: 87o flat, rejam worse (High)', [...F(5), 'raise', 'fold'], ['87o', 'JTs', 'QJs', 'T9s']],
    ['B4#17 CO first in 20bb: 22-66 jam; A2o-A7o/QJo/KTo min-raise (Medium)', F(4), ['22', '44', '66', 'A2o', 'A7o', 'QJo', 'KTo']],
  ],
  30: [
    ['#2 UTG opens, HJ 3-bets (cEV defends KJs/KTs/QTs/JTs)', ['raise', 'fold', 'fold', 'raise', 'fold', 'fold', 'fold', 'fold'], ['KJs', 'KTs', 'QTs', 'JTs']],
    ['#11 CO opens, faces BTN 3-bet to ~6: A4s 4-bet/jam', [...F(4), 'raise', 'raise', 'fold', 'fold'], ['A4s']],
    ['#12 CO opens, faces BTN 3-bet: QJo/KQo/KJs 4-bet-jam', [...F(4), 'raise', 'raise', 'fold', 'fold'], ['QJo', 'KQo', 'KJs']],
    ['#1 BTN opens, SB jams (cEV calls 77, ATs, AJo, A8s, A9o, 44, KTs, A7s)', [...F(5), 'raise', 'allin', 'fold'], ['77', 'ATs', 'AJo', 'A8s', 'A9o', '44', 'KTs', 'A7s', 'KQs', 'ATo']],
  ],
};
console.log(`=== ${S}bb 8-max chip EV, BB ante, iters ${s.iters}`);
for (const [title, path, hands] of SPOTS[S] || []) {
  const id = f(path); if (id < 0) { console.log(title, ': path not in tree'); continue; }
  const nd = s.nodes[id]; console.log(`\n${title}\n  ${cfg.names[nd.player]} opts ${nd.actions.map((a) => label(nd, a, cfg)).join(' / ')}`);
  for (const h of hands) { const c = N.indexOf(h), st = s.avg(id, c), ev = s.evOf2(id, c) || []; const b = ev.length ? ev.indexOf(Math.max(...ev)) : st.indexOf(Math.max(...st));
    console.log(`  ${h.padEnd(4)} -> ${label(nd, nd.actions[b], cfg).padEnd(10)} st ${st.map((x) => x.toFixed(2)).join(' ')}  ev ${ev.map((x) => x.toFixed(2)).join(' ')}`); }
}
