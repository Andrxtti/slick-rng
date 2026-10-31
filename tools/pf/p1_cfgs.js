// SOLVER_PLAN P1: heads-up postflop ground truth at 10-40bb. Writes one realize/solve_full.js config per pot type x depth
// x flop (the 12-flop set of the 100bb realization runs) into tools/pf/p1/cfgs/, with both ranges taken from the library
// at that depth (reach to the node times the action: realize/librange.py's logic, with the engine's class names).
//   node tools/pf/p1_cfgs.js [libdir]        then: bash tools/pf/p1_run.sh   (sequential, resumable)
// Pot types: SRP BTN vs BB and UTG vs BB (the opener IP), BvB SB complete / BB check (limped) and SB open / BB call
// (SB OOP), BB small 3-bet / BTN call. Pot = both investments + the dead SB + the 1bb ante; stacks = depth - investment.
const fs = require('fs'), path = require('path');
const E = require('../../app/engine.js');
const NAMES = E.CLASS_NAMES, NC = 169;
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const libdir = process.argv[2] || path.join(__dirname, '../../app/lib'), out = path.join(__dirname, 'p1', 'cfgs');
fs.mkdirSync(out, { recursive: true });
const FLOPS = ['3d3c8h', '7h6d2d', '8c7c2h', '9s6h3h', 'Ac9h4c', 'AhKd6s', 'JdTd5s', 'KcQd4c', 'Ks7d2c', 'QcJh9s', 'Qh8s3d', 'Tc5d2s'];
const DEPTHS = [10, 15, 20, 25, 30, 40];
const F = (k) => Array(k).fill('fold').join('|');
// pot type: [OOP seat, IP seat, path to the last decision, the actions that close it (OOP's, IP's), aggressor index, level]
const POTS = {
  bb_btn: { oop: 7, ip: 5, path: `${F(5)}|raise|fold|call`, level: 1, agg: 1 },
  bb_utg: { oop: 7, ip: 0, path: `raise|${F(6)}|call`, level: 1, agg: 1 },
  sb_bb_limp: { oop: 6, ip: 7, path: `${F(6)}|call|check`, level: 0, agg: -1 },
  sb_bb_open: { oop: 6, ip: 7, path: `${F(6)}|raise|call`, level: 1, agg: 0 },
  bb3b_btn: { oop: 7, ip: 5, path: `${F(5)}|raise|fold|raise|call`, level: 2, agg: 0 },
};

const S = (n) => { const b = Buffer.from(n.s, 'base64'), na = n.a.length; return (c, a) => b[c * na + a] / 255; };
function ranges(D, pot) {
  const nodes = new Map(D.nodes.map((n) => [n.p, n])), keys = pot.path.split('|');
  const r = { [pot.oop]: new Array(NC).fill(1), [pot.ip]: new Array(NC).fill(1) };
  const put = new Array(D.names.length).fill(0); put[6] = 0.5; put[7] = 1;   // blinds (the ante is in the pot already)
  for (let i = 0; i < keys.length; i++) {
    const n = nodes.get(keys.slice(0, i).join('|')); if (!n) return null;   // the line isn't in the library's tree
    const j = n.a.indexOf(keys[i]); if (j < 0) return null;
    const to = n.to[j], s = n.h;
    if (keys[i] === 'raise' || keys[i] === 'allin') put[s] = to;
    else if (keys[i] === 'call') put[s] = Math.max(...put);
    if (r[s]) { const st = S(n); for (let c = 0; c < NC; c++) r[s][c] *= st(c, j); }
  }
  const inv = put[pot.oop];
  return { r, pot: +(put[pot.oop] + put[pot.ip] + (pot.oop !== 6 && pot.ip !== 6 ? 0.5 : 0) + 1).toFixed(2), behind: +(D.stacks[0] - inv).toFixed(2) };
}
const str = (r) => r.map((w, c) => [NAMES[c], w]).filter(([, w]) => w > 0.004).map(([h, w]) => `${h}:${w.toFixed(3)}`).join(',');
const combos = (r) => r.reduce((t, w, c) => t + w * SIZE[c], 0);

let n = 0; const index = [];
for (const d of DEPTHS) {
  const fp = path.join(libdir, `m8-d${d}.json`); if (!fs.existsSync(fp)) continue;
  const D = JSON.parse(fs.readFileSync(fp, 'utf8'));
  for (const [id, pot] of Object.entries(POTS)) {
    const R = ranges(D, pot);
    if (!R) { index.push({ id, d, skip: 'line not in the tree' }); continue; }
    const co = [combos(R.r[pot.oop]), combos(R.r[pot.ip])];
    if (Math.min(...co) < 20) { index.push({ id, d, skip: `ranges too thin (${co.map((x) => x.toFixed(0)).join('/')} combos)` }); continue; }
    const base = { names: [D.names[pot.oop], D.names[pot.ip]], stacks: [R.behind, R.behind], pot: R.pot, ranges: [str(R.r[pot.oop]), str(R.r[pot.ip])], level: pot.level, agg: pot.agg, depth: d, type: id };
    FLOPS.forEach((f, k) => { fs.writeFileSync(path.join(out, `${id}-d${d}-${String(k).padStart(2, '0')}.json`), JSON.stringify({ ...base, flops: [f] })); n++; });
    index.push({ id, d, pot: R.pot, behind: R.behind, spr: +(R.behind / R.pot).toFixed(2), combos: co.map((x) => +x.toFixed(0)) });
  }
}
fs.writeFileSync(path.join(__dirname, 'p1', 'index.json'), JSON.stringify(index, null, 1));
console.log(`${n} configs`); for (const x of index) console.log(JSON.stringify(x));
