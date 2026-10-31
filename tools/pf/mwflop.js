// Which multiway training spots depend on the multiway postflop model? A spot (two or more opponents still in with a
// voluntary action) is "showdown-only" when nothing below the hero's decision reaches a flop: every line ends in a fold
// or an all-in showdown, which is exact all-in math. Prints JSON {"<out file>|<path>": true} for the spots that DO reach
// a flop (export_spots.py holds those back while the multiway model is unverified).
//   node mwflop.js
const fs = require('fs');
const { PF2 } = require('./solver2.js');
const VOL = ['Raise', 'Call', 'Jam', 'Complete', 'Limp'];
const OUT = __dirname + '/' + (process.env.LIBDIR || 'out'), res = {}, trees = {};   // LIBDIR=out_final to scan another library dir
for (const f of fs.readdirSync(OUT).filter((x) => x.endsWith('.json') && !x.startsWith('mw') && !x.startsWith('ref') && !x.endsWith('probe.json'))) {
  const d = JSON.parse(fs.readFileSync(OUT + '/' + f)); if (!d.spots || !d.file) continue;
  for (const n of d.spots) {
    const seats = n.v.seats, h = n.h;
    if (seats.filter((x, i) => i !== h && !x[3] && VOL.some((v) => x[4].startsWith(v))).length < 2) continue;
    if (!trees[f]) { const D = JSON.parse(fs.readFileSync(__dirname + '/cfgs/' + d.file)); const cfg = { ...D.cfg }; delete cfg.allowLimps; delete cfg.leave1; delete cfg.lite; cfg.maxNodes = 900000; trees[f] = new PF2(cfg); }
    const s = trees[f]; let id = 0;
    for (const k of n.p) { const nd = s.nodes[id]; id = nd.children[nd.actions.findIndex((a) => a.k === k)]; }
    let flop = false; const st = [id];
    while (st.length && !flop) { const x = s.nodes[st.pop()]; if (x.kind === 'flop') flop = true; else if (x.kind === 'act') st.push(...x.children); }
    if (flop) res[f + '|' + n.p.join(',')] = true;
  }
}
process.stdout.write(JSON.stringify(res));
