// Soft-flag review of a solved library: things that are legal but unusual enough to eyeball before shipping.
//   node audit.js out/*.json
const fs = require('fs'); const E = require('../../app/engine.js'); const N = E.CLASS_NAMES, NC = 169;
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const EQR = []; for (let c = 0; c < NC; c++) { let s = 0, w = 0; for (let o = 0; o < NC; o++) { s += E.EQ169[c * NC + o] * SIZE[o]; w += SIZE[o]; } EQR.push(s / w); }
const ord = [...Array(NC).keys()].sort((a, b) => EQR[b] - EQR[a]); const RANK = new Array(NC); ord.forEach((c, i) => (RANK[c] = i));
const flags = {};
const add = (k, s) => { (flags[k] = flags[k] || []).push(s); };
for (const f of process.argv.slice(2)) {
  const D = JSON.parse(fs.readFileSync(f, 'utf8')); if (!D.spots) continue;
  for (const n of D.spots) {
    const hero = n.v.seats[n.h], pos = hero[0], st = hero[1], line = n.v.seats.filter((s) => s[4] && s[4] !== 'Fold').map((s) => `${s[0]}:${s[4]}`).join(' ');
    for (const [c, strat, ev] of n.r) {
      const b = ev.indexOf(Math.max(...ev)), k = n.a[b], lab = n.v.opts[b], h = N[c];
      const tag = `${D.file} ${pos} ${st}bb [${n.v.cat}] ${line} | ${h} -> ${lab}`;
      if (k === 'fold' && RANK[c] < 12 && n.v.cat !== 'Facing a 3-bet or more' && D.gid !== 'sat') add('strong hand folds (top 12)', tag);
      if ((k === 'allin' || k === 'raise') && n.v.cat === 'First in' && RANK[c] >= 110 && st >= 12 && !['BTN', 'SB', 'CO'].includes(pos)) add('weak hand opens from early/mid seat (12bb+)', tag);
      if (k === 'call' && n.v.cat === 'Facing a raise' && st <= 12 && pos !== 'BB') add('flat with 12bb or less (not BB)', tag);
      if (k === 'call' && n.v.cat === 'Facing a 3-bet or more' && st <= 25) add('calls a 3-bet with 25bb or less', tag);
      if (k === 'raise' && n.v.cat === 'Facing a raise' && st <= 18) add('small 3-bet at 18bb or less', tag);
    }
  }
}
for (const k in flags) { console.log(`\n## ${k}: ${flags[k].length}`); flags[k].slice(0, 12).forEach((s) => console.log('  ' + s)); }
