// PLO core: fast 5-card lookup evaluator, Omaha (2 hole + 3 board) best hand,
// 4-card combo indexing and suit-isomorphic hand classes.
'use strict';
const E = require('../app/engine.js');
// ---- 5-card evaluator from two lookup tables (built once from the general evaluator)
const P13 = [1, 13, 169, 2197, 28561];
const RANK5 = new Int32Array(371293), FLUSH5 = new Int32Array(8192);
(function build() {
  const cards = new Uint8Array(5);
  for (let a = 0; a < 13; a++) for (let b = 0; b < 13; b++) for (let c = 0; c < 13; c++) for (let d = 0; d < 13; d++) for (let e = 0; e < 13; e++) {
    const r = [a, b, c, d, e], cnt = new Uint8Array(13); let ok = true;
    for (const x of r) if (++cnt[x] > 4) ok = false;
    if (!ok) continue;
    // suits chosen so no flush: give each card a different suit pattern
    for (let i = 0; i < 5; i++) cards[i] = r[i] * 4 + (i % 4 === 0 && i ? 1 : i % 4);
    // make sure no 5 same suit (only 4 suits, positions 0..4 -> suits 0,1,2,3,1)
    RANK5[a * P13[4] + b * P13[3] + c * P13[2] + d * P13[1] + e] = E.evaluate(cards, 5);
  }
  for (let m = 0; m < 8192; m++) {
    let n = 0, x = m; while (x) { n += x & 1; x >>= 1; } if (n !== 5) continue;
    let k = 0; for (let r = 0; r < 13; r++) if (m >> r & 1) cards[k++] = r * 4; // all clubs
    FLUSH5[m] = E.evaluate(cards, 5);
  }
})();
// board triples and hole pairs (indexes into the 5 board / 4 hole cards)
const T3 = []; for (let a = 0; a < 5; a++) for (let b = a + 1; b < 5; b++) for (let c = b + 1; c < 5; c++) T3.push([a, b, c]);
const P2 = []; for (let a = 0; a < 4; a++) for (let b = a + 1; b < 4; b++) P2.push([a, b]);
// pre-digest a board: for each triple, rank code partial and suit info
function boardInfo(board) {
  const out = new Int32Array(10 * 3);
  for (let t = 0; t < 10; t++) {
    const [i, j, k] = T3[t], x = board[i], y = board[j], z = board[k];
    out[t * 3] = (x >> 2) * P13[4] + (y >> 2) * P13[3] + (z >> 2) * P13[2];
    out[t * 3 + 1] = (x & 3) === (y & 3) && (y & 3) === (z & 3) ? (x & 3) : -1;
    out[t * 3 + 2] = (1 << (x >> 2)) | (1 << (y >> 2)) | (1 << (z >> 2));
  }
  return out;
}
function omaha(h, bi) {
  let best = 0;
  for (let p = 0; p < 6; p++) {
    const a = h[P2[p][0]], b = h[P2[p][1]], ra = (a >> 2) * 13 + (b >> 2), suited = (a & 3) === (b & 3) ? (a & 3) : -2, hm = (1 << (a >> 2)) | (1 << (b >> 2));
    for (let t = 0; t < 10; t++) {
      const o = t * 3;
      let v = RANK5[bi[o] + ra];
      if (suited === bi[o + 1]) { const f = FLUSH5[bi[o + 2] | hm]; if (f > v) v = f; }
      if (v > best) best = v;
    }
  }
  return best;
}
// ---- 4-card combos: index by the combinatorial number system (sorted a<b<c<d)
const NC4 = 270725;
const C2 = new Int32Array(53), C3 = new Int32Array(53), C4 = new Int32Array(53);
for (let n = 0; n <= 52; n++) { C2[n] = n * (n - 1) / 2; C3[n] = n * (n - 1) * (n - 2) / 6; C4[n] = n * (n - 1) * (n - 2) * (n - 3) / 24; }
function idx4(a, b, c, d) {
  let t;
  if (a > b) { t = a; a = b; b = t; } if (c > d) { t = c; c = d; d = t; } if (a > c) { t = a; a = c; c = t; } if (b > d) { t = b; b = d; d = t; } if (b > c) { t = b; b = c; c = t; }
  return a + C2[b] + C3[c] + C4[d];
}
// suit-isomorphic class key: the four per-suit rank masks, sorted (suits are interchangeable)
function classKey(a, b, c, d) {
  const m = [0, 0, 0, 0]; m[a & 3] |= 1 << (a >> 2); m[b & 3] |= 1 << (b >> 2); m[c & 3] |= 1 << (c >> 2); m[d & 3] |= 1 << (d >> 2);
  m.sort((x, y) => y - x);
  return ((m[0] * 8192 + m[1]) * 8192 + m[2]) * 8192 + m[3];
}
// every combo -> class id (classes numbered by ascending key), and a representative per class
function buildClasses() {
  const keyOf = new Float64Array(NC4), combos = new Int32Array(NC4 * 4); let i = 0;
  for (let d = 3; d < 52; d++) for (let c = 2; c < d; c++) for (let b = 1; b < c; b++) for (let a = 0; a < b; a++) {
    const k = idx4(a, b, c, d); keyOf[k] = classKey(a, b, c, d); combos[k * 4] = a; combos[k * 4 + 1] = b; combos[k * 4 + 2] = c; combos[k * 4 + 3] = d; i++;
  }
  const keys = Array.from(new Set(keyOf)).sort((x, y) => x - y), id = new Map(keys.map((k, j) => [k, j]));
  const classOf = new Uint16Array(NC4), size = new Int32Array(keys.length), rep = new Int32Array(keys.length).fill(-1);
  for (let k = 0; k < NC4; k++) { const c = id.get(keyOf[k]); classOf[k] = c; size[c]++; if (rep[c] < 0) rep[c] = k; }
  return { classOf, size, rep, combos, nClass: keys.length };
}
module.exports = { RANK5, FLUSH5, boardInfo, omaha, idx4, NC4, classKey, buildClasses, P13 };
