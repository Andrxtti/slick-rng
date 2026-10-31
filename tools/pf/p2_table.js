// SOLVER_PLAN P2, step 1: the realization table from P1's heads-up postflop solves.
//   node tools/pf/p2_table.js   -> tools/pf/p2/real_table.json, real_summary.md
// Per pot type x depth x position (OOP/IP) x hand class: realization = postflop result / (all-in equity x pot), the share of
// its pot equity a hand actually takes home when the hand is played out (realize/solve_full.js), averaged over the 12 flops
// weighted by samples. Classes with too few samples fall back to their bucket (pairs / suited / offsuit, by equity rank),
// then to the position's mean. SPR comes from p1/index.json. P2 step 2 makes the preflop solver read this table.
const fs = require('fs'), path = require('path');
const E = require('../../app/engine.js');
const NAMES = E.CLASS_NAMES, NC = 169, MIN_N = 400;
const dir = path.join(__dirname, 'p1', 'out'), index = JSON.parse(fs.readFileSync(path.join(__dirname, 'p1', 'index.json'), 'utf8'));
const acc = {};   // key type|depth -> [OOP, IP] -> class -> {ev, eqpot, n}
for (const f of fs.readdirSync(dir).filter((x) => x.endsWith('.json'))) {
  const m = f.match(/^(.+)-d(\d+)-\d+\.json$/); if (!m) continue;
  const D = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8')), key = `${m[1]}|${m[2]}`, pot = D.cfg.pot;
  const A = acc[key] || (acc[key] = [{}, {}]);
  for (const fl of Object.values(D.flops)) D.cfg.names.forEach((who, pi) => {
    for (const [h, v] of Object.entries(fl.classes[who] || {})) {
      const a = A[pi][h] || (A[pi][h] = { ev: 0, eqpot: 0, n: 0, flops: 0 });
      a.ev += v.ev * v.n; a.eqpot += v.eq * pot * v.n; a.n += v.n; a.flops++;
    }
  });
}
const table = {}, md = ['# P2 realization table (from P1 heads-up postflop solves)', '',
  'Realization = postflop result / (equity x pot), by pot type, depth and position; mean over classes weighted by samples.', '',
  '| pot type | bb | SPR | flops done | OOP realization | IP realization | classes measured (OOP/IP) |', '|---|---|---|---|---|---|---|'];
for (const [key, A] of Object.entries(acc).sort()) {
  const [type, d] = key.split('|'), ix = index.find((x) => x.id === type && x.d === +d) || {};
  const out = A.map((P) => {
    const R = {}; let sw = 0, se = 0;
    for (const [h, a] of Object.entries(P)) if (a.n >= MIN_N && a.eqpot > 0) { R[h] = +(a.ev / a.eqpot).toFixed(4); sw += a.eqpot; se += a.ev; }
    return { R, mean: sw ? +(se / sw).toFixed(4) : null, measured: Object.keys(R).length, flops: Math.max(0, ...Object.values(P).map((a) => a.flops)) };
  });
  table[key] = { type, depth: +d, spr: ix.spr, pot: ix.pot, oop: out[0], ip: out[1] };
  md.push(`| ${type} | ${d} | ${ix.spr} | ${out[0].flops} | ${out[0].mean} | ${out[1].mean} | ${out[0].measured}/${out[1].measured} |`);
}
fs.mkdirSync(path.join(__dirname, 'p2'), { recursive: true });
fs.writeFileSync(path.join(__dirname, 'p2', 'real_table.json'), JSON.stringify(table));
fs.writeFileSync(path.join(__dirname, 'p2', 'real_summary.md'), md.join('\n') + '\n');
console.log(md.slice(4).join('\n'));
