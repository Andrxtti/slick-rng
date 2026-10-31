// Hard poker-truth checks over every harvested spot: a hit means a solver or tree bug, never a "close spot".
//   node sanity.js out/*.json
const fs = require('fs');
const E = require('../../app/engine.js');
const NAMES = E.CLASS_NAMES;
const files = process.argv.slice(2).filter((f) => f.endsWith('.json') && !f.includes('ref'));
let bad = 0, rows = 0;
const say = (f, n, c, why) => { bad++; console.log(`${f} [${n.v.cat}] ${n.v.seats[n.h][0]} ${NAMES[c]}: ${why}  opts=${n.v.opts.join('/')}  st=${JSON.stringify(n._st)} ev=${JSON.stringify(n._ev)}`); };
for (const f of files) {
  const D = JSON.parse(fs.readFileSync(f, 'utf8')); if (!D.spots) continue;
  const sat = D.gid === 'sat', chips = D.unit === 'bb';
  for (const n of D.spots) {
    const jams = n.p.filter((k) => k === 'allin').length, raises = n.p.filter((k) => k === 'raise').length;
    for (const [c, st, ev] of n.r) {
      rows++; n._st = st; n._ev = ev;
      const best = ev.indexOf(Math.max(...ev)), k = n.a[best], nm = NAMES[c];
      if (!sat && nm === 'AA' && k === 'fold') say(f, n, c, 'AA folds');
      if (!sat && nm === 'KK' && k === 'fold' && (chips || (jams === 0 && raises <= 1))) say(f, n, c, 'KK folds');
      if (chips && ['QQ', 'AKs'].includes(nm) && k === 'fold' && jams + raises <= 1) say(f, n, c, nm + ' folds to one bet in chip EV');
      if (!sat && n.v.cat === 'First in' && ['AA', 'KK', 'QQ', 'JJ', 'AKs', 'AKo', 'AQs'].includes(nm) && k === 'fold') say(f, n, c, 'premium folds first in');
      if (n.v.cat === 'First in' && k === 'fold' && n.a.includes('check')) say(f, n, c, 'folds when it can check');
      if (st.some((x) => !(x >= 0 && x <= 1))) say(f, n, c, 'strategy out of range');
      if (Math.abs(st.reduce((a, b) => a + b, 0) - 1) > 0.03) say(f, n, c, 'strategy does not sum to 1');
    }
    // the menu itself: no limps outside the SB, no raise that leaves under ~1bb behind
    n.a.forEach((k, i) => {
      if (k === 'call' && n.v.opts[i] === 'Complete' && n.v.seats[n.h][0] !== 'SB') { bad++; console.log(`${f}: ${n.v.seats[n.h][0]} offered a limp`); }
      if (k === 'raise') { const left = n.v.seats[n.h][1] - n.to[i]; if (left < 2) { bad++; console.log(`${f}: raise to ${n.to[i]} leaves ${left.toFixed(1)}bb behind`); } }
    });
  }
}
console.log(`${files.length} tables, ${rows} dealt hands, ${bad} problems`);
process.exit(bad ? 1 : 0);
