// The ranges leading into each multiway spot of a run: opener's raise range and each flatter's call range (width and
// the hands taken at least half the time). Diagnoses a hero range that is off because the ranges in front of it are.
//   node mwpre.js out/mw100_<tag>.json
const path = require('path');
const E = require('../../app/engine.js');
const SIZE = new Array(169).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const d = require(path.resolve(process.argv[2]));
const seen = new Set();
for (const sp of d.spots) {
  const key = sp.pre.map((q) => q.seat + ':' + q.act).join(' > ');
  console.log(`\n${sp.title}   [${key}]`);
  for (const q of sp.pre) {
    const k = q.seat + ':' + q.act + ':' + q.w.join(',');
    let w = 0; q.w.forEach((p, c) => (w += SIZE[c] * p));
    const hands = q.w.map((p, c) => [p, c]).filter(([p]) => p >= 0.5).sort((a, b) => b[0] - a[0]).map(([p, c]) => E.CLASS_NAMES[c] + (p < 0.9 ? `(${Math.round(p * 100)})` : ''));
    console.log(`  ${q.seat.padEnd(4)} ${q.act.padEnd(5)} ${((100 * w) / 1326).toFixed(1).padStart(5)}%`, seen.has(k) ? '(as above)' : hands.join(' '));
    seen.add(k);
  }
}
