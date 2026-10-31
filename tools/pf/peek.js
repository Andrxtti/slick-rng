// Print a library table's dealt spots in poker terms, for eyeballing: node peek.js out/x.json [maxSpots]
const fs = require('fs'); const E = require('../../app/engine.js'); const N = E.CLASS_NAMES;
const d = JSON.parse(fs.readFileSync(process.argv[2], 'utf8')); const max = +(process.argv[3] || 99);
console.log(d.file, d.unit, 'spots', d.spots.length, 'model', JSON.stringify(d.model || {}), 'held back', d.fragile, 'bvb skipped', d.bvbSkip);
for (const n of d.spots.slice(0, max)) {
  const hero = n.v.seats[n.h];
  const line = n.v.seats.filter((s) => s[4]).map((s) => `${s[0]}(${s[1]}):${s[4]}`).join(' ');
  console.log(`\n${hero[0]} ${hero[1]}bb [${n.v.cat}] pot ${n.v.pot}  ${line || 'folded to hero'}  opts ${n.v.opts.join('/')}`);
  const by = {}; for (const [c, st, ev] of n.r) { const b = ev.indexOf(Math.max(...ev)); (by[n.v.opts[b]] = by[n.v.opts[b]] || []).push(N[c]); }
  for (const k in by) console.log(`   ${k.padEnd(12)} ${by[k].join(' ')}`);
}
