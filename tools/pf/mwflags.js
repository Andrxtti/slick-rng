// Review aid for the flop-reaching multiway spots (the ones mwflop.js holds back): flags the dealt hands whose clear-best
// action breaks a standing rule, so a reviewer looks at those instead of every hand.
//   LIBDIR=out_mw node mwflags.js            -> one line per flagged hand, then a per-spot summary
// Rules: (junkcall) offsuit junk is never a flat in MTTs; (strongflat) TT+/AQ+ flatting at <= 15bb effective when a jam
// is on the menu; (junkjam) a jam with trash into two or more players; (foldprem) folding QQ+/AK.
const fs = require('fs');
const E = require('../../app/engine.js'), N = E.CLASS_NAMES, NC = 169;
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const EQR = []; for (let c = 0; c < NC; c++) { let s = 0, w = 0; for (let o = 0; o < NC; o++) { s += E.EQ169[c * NC + o] * SIZE[o]; w += SIZE[o]; } EQR.push(s / w); }
const RANK = (() => { const idx = [...Array(NC).keys()].sort((a, b) => EQR[b] - EQR[a]); const r = new Array(NC); idx.forEach((c, i) => (r[c] = i)); return r; })();
const LIB = __dirname + '/' + (process.env.LIBDIR || 'out');
const MW = JSON.parse(require('child_process').execSync('node ' + __dirname + '/mwflop.js', { env: { ...process.env, LIBDIR: process.env.LIBDIR || 'out' } }).toString());
const STRONG = new Set(['AA', 'KK', 'QQ', 'JJ', 'TT', 'AKs', 'AKo', 'AQs', 'AQo']), PREM = new Set(['AA', 'KK', 'QQ', 'AKs', 'AKo']);
const offsuit = (n) => n.length === 3 && n[2] === 'o', junk = (c) => RANK[c] >= 60;
const summary = [];
for (const key of Object.keys(MW).sort()) {
  const [file, p] = key.split('|'); const d = JSON.parse(fs.readFileSync(LIB + '/' + file));
  const sp = d.spots.find((n) => n.p.join(',') === p); if (!sp) continue;
  const hero = sp.v.seats[sp.h], eff = Math.min(...sp.v.seats.filter((x, i) => i === sp.h || (!x[3] && x[4])).map((x) => x[1]));
  const menu = sp.a, hasJam = menu.includes('allin'); const flags = [];
  for (const [c, st, ev] of sp.r) {
    const b = st.indexOf(Math.max(...st)), k = menu[b], n = N[c], es = ev.slice().sort((x, y) => y - x), m = (es[0] - es[1]).toFixed(2);
    let f = null;
    if (k === 'call' && offsuit(n) && junk(c)) f = 'junkcall';
    else if (k === 'call' && STRONG.has(n) && hasJam && eff <= 15) f = 'strongflat';
    else if (k === 'allin' && junk(c)) f = 'junkjam';
    else if (k === 'fold' && PREM.has(n)) f = 'foldprem';
    if (f) { flags.push(f); console.log(`${f.padEnd(10)} ${file.padEnd(18)} ${sp.v.cat.padEnd(15)} ${hero[0]} ${hero[1]}bb eff ${eff}  ${n.padEnd(4)} -> ${sp.v.opts[b].padEnd(10)} (${Math.round(st[b] * 100)}%, margin ${m})   [${sp.v.seats.filter((x) => x[4]).map((x) => x[0] + ' ' + x[4]).join(' | ')}]`); }
  }
  summary.push(`${file.padEnd(18)} ${p.padEnd(44)} ${sp.v.cat.padEnd(15)} ${hero[0].padEnd(5)} ${String(sp.r.length).padStart(2)} hands  flags: ${flags.length ? flags.join(' ') : '-'}`);
}
console.log('\n' + summary.join('\n'));
