// What equity realization does the preflop solver's postflop model imply, vs published solver EQR?
const fs = require('fs'), vm = require('vm');
const src = fs.readFileSync(__dirname + '/../../app/engine.js', 'utf8') + '\n;globalThis.__X = { PLAY169, EQ169, CLASS_NAMES, CLASS_OF, NUM_COMBOS, COMBO_A, COMBO_B, parseRange };';
const ctx = { console, Buffer, module: {}, globalThis: null }; ctx.globalThis = ctx; vm.createContext(ctx); vm.runInContext(src, ctx);
const { PLAY169, EQ169, CLASS_NAMES, CLASS_OF, NUM_COMBOS, COMBO_A, COMBO_B, parseRange } = ctx.__X;
const NC = 169;
const byC = Array.from({ length: NC }, () => []); for (let i = 0; i < NUM_COMBOS; i++) byC[CLASS_OF[i]].push(i);
const W = new Float64Array(NC * NC);
for (let c = 0; c < NC; c++) for (const x of byC[c]) for (let o = 0; o < NC; o++) for (const y of byC[o]) { if (COMBO_A[x] === COMBO_A[y] || COMBO_A[x] === COMBO_B[y] || COMBO_B[x] === COMBO_A[y] || COMBO_B[x] === COMBO_B[y]) continue; W[c * NC + o]++; }
const rng = (s) => { const r = parseRange(s), v = new Float64Array(NC), n = new Float64Array(NC); for (let i = 0; i < NUM_COMBOS; i++) { v[CLASS_OF[i]] += r[i]; n[CLASS_OF[i]]++; } for (let c = 0; c < NC; c++) v[c] /= n[c]; return v; };
// model: share of the pot for class c (OOP) vs class o (IP)
function share(c, o, p, spr) {
  const eq = EQ169[c * NC + o];
  const ramp = Math.min(1, Math.max(0, (spr - p.s0) / p.s1));
  const Ro = 1 - (1 - p.rLow) * ramp, Ri = 1;
  const g = 1 + p.gK * Math.log(1 + spr), playK = Math.min(1, Math.max(0, (spr - 1) / 5)) * p.pK;
  const a = Ro * (1 + PLAY169[c] * playK) * Math.pow(eq, g), b = Ri * (1 + PLAY169[o] * playK) * Math.pow(1 - eq, g);
  return a / (a + b);
}
function eqr(oop, ip, p, spr) {
  let raw = 0, real = 0, w = 0;
  for (let c = 0; c < NC; c++) if (oop[c] > 0) for (let o = 0; o < NC; o++) if (ip[o] > 0) { const ww = W[c * NC + o] * oop[c] * ip[o]; if (!ww) continue; w += ww; raw += ww * EQ169[c * NC + o]; real += ww * share(c, o, p, spr); }
  return { oopEq: raw / w, oop: real / raw, ip: (1 - real / w) / (1 - raw / w) };
}
const T = [
  { n: 'MTT 20bb BTN vs BB', spr: 3.3, ip: rng('22+,A2s+,A2o+,K2s+,K7o+,Q4s+,Q9o+,J6s+,J9o+,T6s+,T9o,96s+,98o,85s+,75s+,64s+,54s'), oop: rng('22-99,A2s-AJs,A2o-AJo,K2s+,K5o+,Q2s+,Q7o+,J4s+,J7o+,T5s+,T7o+,95s+,97o+,85s+,87o,74s+,76o,63s+,65o,53s+,43s'), t: [0.79, 1.16] },
  { n: 'cash 100bb LJ vs BB', spr: 17.7, ip: rng('22+,A2s+,ATo+,K9s+,KJo+,QTs+,QJo,J9s+,T9s,98s,87s,76s,65s'), oop: rng('22-JJ,A2s-AQs,ATo-AQo,K6s+,KTo+,Q8s+,QTo+,J8s+,JTo,T7s+,97s+,86s+,75s+,64s+,54s'), t: [0.87, 1.14] },
  { n: 'HU 30bb SB limp BB check', spr: 12.6, ip: rng('22-99,A2s-ATs,A2o-ATo,K2s+,K2o+,Q2s+,Q2o+,J2s+,J4o+,T2s+,T5o+,92s+,95o+,82s+,85o+,72s+,74o+,62s+,64o+,52s+,54o,42s+,32s'), oop: rng('22-66,A2o-A7o,K2s-K9s,K2o-KTo,Q2s-QTs,Q2o-QJo,J2s-JTs,J2o+,T2s+,T2o+,92s+,92o+,82s+,82o+,72s+,72o+,62s+,62o+,52s+,52o+,42s+,42o+,32s,32o'), t: [0.85, 1.14] },
];
const cur = { rLow: 0.7, gK: 0, pK: 1, s0: 1.0, s1: 2.0 };
const run = (p, tag) => { let err = 0; const rows = T.map((x) => { const r = eqr(x.oop, x.ip, p, x.spr); err += (r.oop - x.t[0]) ** 2 + (r.ip - x.t[1]) ** 2; return `${x.n}: OOP ${r.oop.toFixed(2)} (target ${x.t[0]}) IP ${r.ip.toFixed(2)} (target ${x.t[1]}) oopEq ${(100 * r.oopEq).toFixed(1)}%`; }); console.log(tag, JSON.stringify(p), 'err', err.toFixed(4)); rows.forEach((r) => console.log('   ', r)); return err; };
run(cur, 'CURRENT');
// fit: OOP penalty that does not fade until stacks are nearly all in, plus strength curve and playability
let best = null;
for (const rLow of [0.55, 0.6, 0.65, 0.7, 0.75, 0.8]) for (const gK of [0, 0.1, 0.2, 0.3, 0.4]) for (const pK of [1, 2, 3]) for (const s0 of [0.25]) for (const s1 of [0.5, 1, 1.75]) {
  const p = { rLow, gK, pK, s0, s1 }; let err = 0;
  for (const x of T) { const r = eqr(x.oop, x.ip, p, x.spr); err += (r.oop - x.t[0]) ** 2 + (r.ip - x.t[1]) ** 2; }
  if (!best || err < best.err) best = { p, err };
}
run(best.p, 'FIT');
