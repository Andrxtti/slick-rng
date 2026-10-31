// Hand-by-hand review of the multiway spots in a solved library table: for every spot that mwflop.js holds back (two or
// more opponents in, a flop reachable below the decision), print the seats, the menu, and which hands take each option
// (by best EV), so each one can be checked against the course's short-stack multiway guidance before it is dealt.
//   node mwreview.js <library out json> [...]
const fs = require('fs'), path = require('path');
const E = require('../../app/engine.js'), N = E.CLASS_NAMES;
const MW = JSON.parse(require('child_process').execSync('node ' + __dirname + '/mwflop.js').toString());
for (const f of process.argv.slice(2)) {
  const d = JSON.parse(fs.readFileSync(f)); if (!d.spots) continue;
  const base = path.basename(f).replace(/^.*?(bubble|shove|sat|ring|cash)/, '$1');
  for (const n of d.spots) {
    const key = base + '|' + n.p.join(','); if (!MW[key]) continue;
    const hero = n.v.seats[n.h];
    const who = n.v.seats.map((s, i) => `${s[0]}${i === n.h ? '*' : ''} ${s[1]}bb${s[4] ? ' ' + s[4] : ''}`).join(' | ');
    console.log(`\n${base}  ${n.v.cat}  hero ${hero[0]} ${hero[1]}bb   [${who}]`);
    const by = n.v.opts.map(() => []);
    for (const [c, st, ev] of n.r) { const b = ev.indexOf(Math.max(...ev)); const mix = st[b] < 0.8 ? `(${Math.round(st[b] * 100)})` : ''; by[b].push([N[c], mix, ev[b] - Math.max(...ev.filter((_, i) => i !== b))]); }
    n.v.opts.forEach((o, i) => { if (by[i].length) console.log(`  ${o.padEnd(10)} ${by[i].map((x) => x[0] + x[1]).join(' ')}`); });
    console.log(`  (${n.r.length} hands dealt; ${n.w !== undefined ? 'weight ' + n.w : ''})`);
  }
}
