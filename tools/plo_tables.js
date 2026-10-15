// Offline: PLO4 hand-class features, 256 strategy buckets, bucket-vs-bucket all-in equity.
// node tools/plo_tables.js  ->  app/plo_tables.json
'use strict';
const P = require('./plo_core.js');
const t00 = Date.now();
const C = P.buildClasses(); console.log('classes', C.nClass, Date.now() - t00, 'ms');
let seed = 12345; const rnd = () => { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296; };
const deck = new Uint8Array(52), rest = new Uint8Array(52);
function equityVs(h, nOpp, samples) {
  let w = 0; const used = new Uint8Array(52); for (const c of h) used[c] = 1;
  let m = 0; for (let c = 0; c < 52; c++) if (!used[c]) rest[m++] = c;
  const opp = new Uint8Array(4), board = new Uint8Array(5);
  for (let s = 0; s < samples; s++) {
    const need = 5 + 4 * nOpp;
    for (let i = 0; i < need; i++) { const j = i + ((rnd() * (m - i)) | 0); const t = rest[i]; rest[i] = rest[j]; rest[j] = t; }
    for (let i = 0; i < 5; i++) board[i] = rest[i];
    const bi = P.boardInfo(board), me = P.omaha(h, bi);
    let best = 0, ties = 0, beat = false;
    for (let o = 0; o < nOpp; o++) { for (let i = 0; i < 4; i++) opp[i] = rest[5 + o * 4 + i]; const v = P.omaha(opp, bi); if (v > me) { beat = true; break; } if (v === me) ties++; }
    if (!beat) w += 1 / (1 + ties);
  }
  return w / samples;
}
const N = C.nClass, fs = require('fs'), FEAT = __dirname + '/plo_features.bin';
let E1 = new Float32Array(N), E3 = new Float32Array(N);
if (fs.existsSync(FEAT)) { const f = new Float32Array(new Uint8Array(fs.readFileSync(FEAT)).buffer); E1 = f.slice(0, N); E3 = f.slice(N); console.log('features from cache'); }
else {
  for (let c = 0; c < N; c++) {
    const k = C.rep[c], h = [C.combos[k * 4], C.combos[k * 4 + 1], C.combos[k * 4 + 2], C.combos[k * 4 + 3]];
    E1[c] = equityVs(h, 1, 4000); E3[c] = equityVs(h, 3, 2000);
    if (c % 2000 === 0) console.log('features', c, Date.now() - t00, 'ms');
  }
  const f = new Float32Array(2 * N); f.set(E1); f.set(E3, N); fs.writeFileSync(FEAT, Buffer.from(f.buffer));
}
// 256 buckets. Strength = average of a class's heads-up and 4-way equity percentiles; style =
// 4-way percentile minus heads-up percentile (rundowns and suited connectors play better
// multiway, ragged big pairs better heads-up). 32 strength slices x 8 style splits, with combo
// mass weighted 1 + 30*q^3 by strength percentile q so strong (playable) hands get finer buckets.
const B = 256, S1 = 32, S2 = 8, bucket = new Uint8Array(N);
const total0 = C.size.reduce((a, b) => a + b, 0);
const pct = (E) => { const o = [...Array(N).keys()].sort((a, b) => E[a] - E[b]), q = new Float64Array(N); let acc0 = 0; for (const c of o) { q[c] = (acc0 + C.size[c] / 2) / total0; acc0 += C.size[c]; } return q; };
const q1 = pct(E1), q3 = pct(E3), strength = new Float64Array(N), style = new Float64Array(N);
for (let c = 0; c < N; c++) { strength[c] = (q1[c] + q3[c]) / 2; style[c] = q3[c] - q1[c]; }
const qs = pct(strength), wt = new Float64Array(N);
for (let c = 0; c < N; c++) wt[c] = C.size[c] * (1 + 30 * qs[c] ** 3);
const ord = [...Array(N).keys()].sort((a, b) => strength[a] - strength[b]);
const total = wt.reduce((a, b) => a + b, 0);
let acc = 0; const slices = Array.from({ length: S1 }, () => []);
for (const c of ord) { const s = Math.min(S1 - 1, Math.floor((acc / total) * S1)); slices[s].push(c); acc += wt[c]; }
slices.forEach((sl, s) => {
  const tot = sl.reduce((a, c) => a + wt[c], 0); let a2 = 0;
  sl.sort((x, y) => style[x] - style[y]).forEach((c) => { const t = Math.min(S2 - 1, Math.floor((a2 / tot) * S2)); bucket[c] = s * S2 + t; a2 += wt[c]; });
});
// bucket-vs-bucket heads-up all-in equity: the same number of samples for every pair
const bOfCombo = new Uint8Array(P.NC4); for (let k = 0; k < P.NC4; k++) bOfCombo[k] = bucket[C.classOf[k]];
const lists = Array.from({ length: B }, () => []); for (let k = 0; k < P.NC4; k++) lists[bOfCombo[k]].push(k);
const W = new Float64Array(B * B), Nn = new Float64Array(B * B), used = new Uint8Array(52), bd = new Uint8Array(5), h1 = new Uint8Array(4), h2 = new Uint8Array(4);
const PER = 240;
for (let a = 0; a < B; a++) {
  for (let b = a; b < B; b++) {
    for (let s = 0; s < PER; s++) {
      let k1, k2;
      do { k1 = lists[a][(rnd() * lists[a].length) | 0]; k2 = lists[b][(rnd() * lists[b].length) | 0];
        for (let i = 0; i < 4; i++) { h1[i] = C.combos[k1 * 4 + i]; h2[i] = C.combos[k2 * 4 + i]; }
      } while (h1.some((x) => h2.includes(x)));
      used.fill(0); for (let i = 0; i < 4; i++) { used[h1[i]] = 1; used[h2[i]] = 1; }
      for (let i = 0; i < 5; i++) { let c; do { c = (rnd() * 52) | 0; } while (used[c]); used[c] = 1; bd[i] = c; }
      const bi = P.boardInfo(bd), v1 = P.omaha(h1, bi), v2 = P.omaha(h2, bi), r = v1 > v2 ? 1 : v1 === v2 ? 0.5 : 0;
      W[a * B + b] += r; Nn[a * B + b]++; W[b * B + a] += 1 - r; Nn[b * B + a]++;
    }
  }
  if (a % 32 === 0) console.log('hu table row', a, Date.now() - t00, 'ms');
}
const EQ = new Uint16Array(B * B); let minN = 1e9;
for (let i = 0; i < B * B; i++) { minN = Math.min(minN, Nn[i]); EQ[i] = Math.round((Nn[i] ? W[i] / Nn[i] : 0.5) * 65535); }
// bucket summary for the app: mean heads-up / 4-way equity per bucket
const bE1 = new Float64Array(B), bE3 = new Float64Array(B), bW = new Float64Array(B);
for (let c = 0; c < N; c++) { const b = bucket[c]; bE1[b] += E1[c] * C.size[c]; bE3[b] += E3[c] * C.size[c]; bW[b] += C.size[c]; }
const out = {
  version: 3, buckets: B,
  classBucket: Buffer.from(bucket.buffer).toString('base64'),
  eq: Buffer.from(EQ.buffer).toString('base64'),
  e1: Array.from(bE1, (x, i) => +(x / bW[i]).toFixed(4)), e3: Array.from(bE3, (x, i) => +(x / bW[i]).toFixed(4)),
  classE1: Buffer.from(new Uint16Array(Array.from(E1, (x) => Math.round(x * 65535))).buffer).toString('base64'),
};
require('fs').writeFileSync(__dirname + '/../app/plo_tables.json', JSON.stringify(out));
console.log('done', Date.now() - t00, 'ms; min samples per cell', minN, '; AAKK-ish top bucket e1', out.e1[255], 'bottom', out.e1[0]);
