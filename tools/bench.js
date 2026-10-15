const P = require('./plo_core.js'), E = require('../app/engine.js');
// correctness vs brute force with the general evaluator
const deck = [...Array(52).keys()], sh = () => { for (let i = 51; i > 0; i--) { const j = (Math.random() * (i + 1)) | 0; [deck[i], deck[j]] = [deck[j], deck[i]]; } };
const c5 = new Uint8Array(5); let bad = 0;
for (let t = 0; t < 20000; t++) {
  sh(); const h = deck.slice(0, 4), b = deck.slice(4, 9);
  let best = 0; for (let x = 0; x < 4; x++) for (let y = x + 1; y < 4; y++) for (let i = 0; i < 5; i++) for (let j = i + 1; j < 5; j++) for (let k = j + 1; k < 5; k++) { c5[0] = h[x]; c5[1] = h[y]; c5[2] = b[i]; c5[3] = b[j]; c5[4] = b[k]; best = Math.max(best, E.evaluate(c5, 5)); }
  if (best !== P.omaha(h, P.boardInfo(b))) bad++;
}
console.log('mismatches', bad);
sh(); const b = deck.slice(4, 9), bi = P.boardInfo(b), hs = []; for (let i = 0; i < 1000; i++) { sh(); hs.push(deck.slice(0, 4)); }
let t0 = Date.now(), s = 0; for (let r = 0; r < 300; r++) for (const h of hs) s += P.omaha(h, bi);
console.log('omaha evals/sec', Math.round(300000 / ((Date.now() - t0) / 1000)));
const seen = new Set(); t0 = Date.now(); let n = 0;
for (let a = 0; a < 52; a++) for (let b2 = a + 1; b2 < 52; b2++) for (let c = b2 + 1; c < 52; c++) for (let d = c + 1; d < 52; d++) { seen.add(P.canon([a, b2, c, d])); n++; }
console.log('combos', n, 'classes', seen.size, (Date.now() - t0) + 'ms');
