// SOLVER_PLAN P0: compare two runs of p0_grid.js (before/after) and check the regression spots.
//   node tools/pf/p0_compare.js <before-tag> <after-tag>   -> prints the deltas, PASS/FAIL per check, exit 1 on a FAIL
// The checks are directional until P1's ground-truth solves give exact targets (each names the audit finding it guards).
const fs = require('fs'), path = require('path');
const dir = path.join(__dirname, 'p0'), [A, B] = process.argv.slice(2);
const load = (t, k) => JSON.parse(fs.readFileSync(path.join(dir, `${k}_${t}.json`), 'utf8'));
const ga = load(A, 'grid'), gb = load(B, 'grid'), sa = load(A, 'spots'), sb = load(B, 'spots');
const key = (r) => `${r.fmt}|${r.depth}|${r.type}`, mb = new Map(gb.map((r) => [key(r), r]));
console.log('grid: square | mix before -> after (fold/call/raise/jam %) | local regret');
for (const r of ga) {
  const s = mb.get(key(r)); if (!s) { console.log(`${key(r)} | gone`); continue; }
  const f = (m) => ['fold', 'call', 'raise', 'jam'].map((k) => (m[k] * 100).toFixed(0)).join('/');
  if (Math.abs(r.regret - s.regret) > 0.01 || ['fold', 'call', 'raise', 'jam'].some((k) => Math.abs(r.mix[k] - s.mix[k]) > 0.03))
    console.log(`${key(r)} | ${f(r.mix)} -> ${f(s.mix)} | ${r.regret.toFixed(3)} -> ${s.regret.toFixed(3)}`);
}
const spot = (S, id) => S.find((x) => x.id === id) || {};
const share = (sp, re) => Object.entries(sp.mix || {}).filter(([l]) => re.test(l)).reduce((t, [, v]) => t + v, 0);
const hand = (sp, h, re) => Object.entries((sp.hands || {})[h] || {}).filter(([l]) => re.test(l)).reduce((t, [, v]) => t + v[0], 0);
const CHECKS = [
  ['bb-vs-btn-40', 'small 3-bet share rises (was 5%)', (a, b) => share(b, /^3-Bet/) > share(a, /^3-Bet/) + 0.03],
  ['bb-vs-btn-40', 'calling falls (was 74%)', (a, b) => share(b, /^Call/) < share(a, /^Call/) - 0.05],
  ['bb-vs-btn-40', 'TT, JJ, QQ no longer pure jams', (a, b) => ['TT', 'JJ', 'QQ'].every((h) => hand(b, h, /^Jam/) < 0.9)],
  ['bb-vs-utg-40', 'small 3-bet above 2% (was 0.4%)', (a, b) => share(b, /^3-Bet/) > 0.02],
  ['btn-vs-bb3b-40', 'a small 4-bet exists (was ~0%)', (a, b) => share(b, /^4-Bet/) > 0.01],
  ['btn-vs-bb3b-60', 'a small 4-bet exists (was ~0%)', (a, b) => share(b, /^4-Bet/) > 0.01],
  ['bb-vs-btn-25', 'BB folds something (was 0%)', (a, b) => share(b, /^Fold/) > 0.02],
  ['bb-vs-btn-25', '72o folds', (a, b) => hand(b, '72o', /^Fold/) > 0.9],
  ['bb-vs-btn-20', 'BB folds something vs the min-raise (was 0%)', (a, b) => share(b, /^Fold/) > 0.02],
  ['bb-vs-btn-30', 'BB folds more than 1% (was 0.9%)', (a, b) => share(b, /^Fold/) > 0.02],
  ['bb-vs-btn-100', '100bb does not regress: 3-bet share within 3 points', (a, b) => Math.abs(share(b, /^3-Bet/) - share(a, /^3-Bet/)) <= 0.03],
];
let fail = 0;
console.log('\nregression spots:');
for (const [id, what, test] of CHECKS) {
  const a = spot(sa, id), b = spot(sb, id); let ok;
  if (b.missing) ok = false; else ok = test(a, b);
  if (!ok) fail++;
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${id}: ${what}${b.missing ? ' (' + b.missing + ')' : ''}`);
}
console.log(`\n${CHECKS.length - fail} of ${CHECKS.length} checks pass`);
process.exit(fail ? 1 : 0);
