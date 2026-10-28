// Fit per-class realization multipliers for the preflop model from real postflop solves (realize/solve_full.js output).
//   node fit.js <outDir> [level] [matchup-prefix ...]
// For each matchup the real pot share of each class (ev / pot, averaged over flops) is compared with the preflop
// model's share (termVec2: R, gamma, playability, implied odds, vs the opponent's range). A multiplier M[c] on the
// hand's playability factor is fitted so the model share matches the real one; printed as JSON for engine.js.
const fs = require('fs');
const E = require('../../../app/engine.js');
const [dir, levelStr, ...prefixes] = process.argv.slice(2);
const LEVEL = +(levelStr || 2), NC = 169, IDX = {}; E.CLASS_NAMES.forEach((n, i) => (IDX[n] = i));
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const files = fs.readdirSync(dir).filter((f) => /^a-real-.*\.json$/.test(f));
// group by matchup
const groups = {};
for (const f of files) {
  const r = JSON.parse(fs.readFileSync(dir + '/' + f, 'utf8')), key = f.replace(/^a-real-/, '').replace(/-\d+\.json$/, '');
  const cfgFile = __dirname + '/cfgs/' + f.replace(/^a-real-/, ''); if (!fs.existsSync(cfgFile)) continue;
  const cfg = JSON.parse(fs.readFileSync(cfgFile, 'utf8')); if ((cfg.level || 1) !== LEVEL) continue;
  if (prefixes.length && !prefixes.some((p) => key.startsWith(p))) continue;
  const g = groups[key] || (groups[key] = { cfg, sum: [new Float64Array(NC), new Float64Array(NC)], cnt: [new Float64Array(NC), new Float64Array(NC)], eq: [new Float64Array(NC), new Float64Array(NC)], flops: 0 });
  for (const flop in r.flops) { g.flops++; for (let p = 0; p < 2; p++) { const o = r.flops[flop].classes[cfg.names[p]]; for (const h in o) { const c = IDX[h]; g.sum[p][c] += o[h].ev * o[h].n; g.cnt[p][c] += o[h].n; g.eq[p][c] += o[h].eq * o[h].n; } } }
}
const keys = Object.keys(groups); if (!keys.length) { console.log('no data'); process.exit(1); }
console.log('matchups:', keys.map((k) => `${k} (${groups[k].flops} flops)`).join(', '));
// preflop model pieces (mirrors PreflopSolver.prepTerminal / termVec2 for a heads-up flop terminal)
const M = Object.assign({ rLow: 0.73, gammaK: 0.15, gRe: 1, playSpan: 5, playPos: 1, playNeg: 2, implied: 0.8 }, JSON.parse(fs.readFileSync(__dirname + '/../model_mw.json', 'utf8')).model || {});
const parseRange = (s) => { const w = new Float64Array(NC); for (const t of s.split(',')) { const [h, x] = t.split(':'); w[IDX[h]] = +x; } return w; };
function nodeFor(cfg) {
  const pot = cfg.pot, behind = Math.min(...cfg.stacks), spr = behind / pot, level = cfg.level || 1;
  const ramp = Math.min(1, Math.max(0, (spr - 0.25) / 1.75));
  const R = [1 - (1 - M.rLow) * ramp, 1];   // OOP first, IP last
  const gamma = 1 + M.gammaK * (1 + M.gRe * Math.max(0, level - 1)) * Math.log(1 + spr);
  const cl1 = (x) => Math.min(1, Math.max(0, x)), span = M.playSpan;
  return { pot, R, gamma, level, playK: cl1((spr - 1) / span), playKp: cl1((spr - 1) / (M.pairSpan ?? span)), ioK: cl1((spr - 1) / (M.ioSpan ?? span)), playRe: 1 + (M.pRe ?? 0) * Math.max(0, level - 1) };
}
const mult = new Float64Array(NC).fill(1);
function share(nd, c, o, p) {
  const eq = E.EQ169[c * NC + o], hu = true;
  const pl = (1 + E.playTerm(nd, c, E.PLAY169[c], M.playPos, M.playNeg, hu)) * mult[c], plo = (1 + E.playTerm(nd, o, E.PLAY169[o], M.playPos, M.playNeg, hu)) * mult[o];
  const a = nd.R[p] * pl * Math.pow(eq, nd.gamma), b = nd.R[1 - p] * plo * Math.pow(1 - eq, nd.gamma);
  let s = a + b > 0 ? a / (a + b) : 0.5;
  const io = M.implied;
  if (io > 0) { const tp = io * Math.max(0, E.PLAY169[c]) * E.ioTerm(nd, c), to = io * Math.max(0, E.PLAY169[o]) * E.ioTerm(nd, o); s = Math.min(1, Math.max(0, s + tp * (1 - s) - to * s)); }
  return s;
}
function modelShares(g) {
  const nd = nodeFor(g.cfg), W = g.cfg.ranges.map(parseRange), out = [new Float64Array(NC), new Float64Array(NC)];
  for (let p = 0; p < 2; p++) { const wo = W[1 - p]; for (let c = 0; c < NC; c++) { if (!(W[p][c] > 0)) continue; let s = 0, t = 0; for (let o = 0; o < NC; o++) { const w = wo[o] * SIZE[o]; if (!(w > 0)) continue; s += w * share(nd, c, o, p); t += w; } out[p][c] = t > 0 ? s / t : 0.5; } }
  return out;
}
const CAP = 0.97;
function err(print) {
  let se = 0, n = 0; const rows = [];
  for (const k of keys) { const g = groups[k], ms = modelShares(g); for (let p = 0; p < 2; p++) for (let c = 0; c < NC; c++) { if (g.cnt[p][c] < 200) continue; const real = Math.min(CAP, g.sum[p][c] / g.cnt[p][c] / g.cfg.pot), w = g.cnt[p][c]; se += w * (ms[p][c] - real) ** 2; n += w; rows.push({ k, p, c, real, model: ms[p][c], w }); } }
  if (print) { rows.sort((a, b) => Math.abs(b.model - b.real) * b.w - Math.abs(a.model - a.real) * a.w); console.log(' worst:', rows.slice(0, 16).map((r) => `${r.k}/${groups[r.k].cfg.names[r.p]} ${E.CLASS_NAMES[r.c]} real ${r.real.toFixed(2)} model ${r.model.toFixed(2)}`).join('; ')); }
  return Math.sqrt(se / n);
}
console.log('rms share error before', err(true).toFixed(4));
for (let it = 0; it < 40; it++) {
  const num = new Float64Array(NC), den = new Float64Array(NC);
  for (const k of keys) { const g = groups[k], ms = modelShares(g); for (let p = 0; p < 2; p++) for (let c = 0; c < NC; c++) { if (g.cnt[p][c] < 200) continue; const real = Math.min(CAP, g.sum[p][c] / g.cnt[p][c] / g.cfg.pot); num[c] += g.cnt[p][c] * Math.log(Math.max(0.05, real) / Math.max(0.05, ms[p][c])); den[c] += g.cnt[p][c]; } }
  for (let c = 0; c < NC; c++) if (den[c] > 0) mult[c] = Math.min(2.5, Math.max(0.4, mult[c] * Math.exp(0.5 * num[c] / den[c] * den[c] / (den[c] + 2000))));   // shrink thin classes toward 1
}
console.log('rms share error after ', err(true).toFixed(4));
const out = {}; for (let c = 0; c < NC; c++) out[E.CLASS_NAMES[c]] = +mult[c].toFixed(3);
console.log('multipliers (hand: factor on playability, 1 = unchanged):');
console.log(JSON.stringify(out));
fs.writeFileSync(__dirname + `/mult_L${LEVEL}.json`, JSON.stringify({ level: LEVEL, matchups: keys, mult: Array.from(mult, (x) => +x.toFixed(4)) }));
