// Fit the preflop model's heads-up flop valuation to GTO Wizard's own realized pot shares (caller side, from the
// per-hand EVs at 100bb re-raised nodes: realize/gwshare.py -> gw_caller_share.json; ranges in realize/gwcfgs).
// Parameters: aggRe (last raiser's realization factor), rLow (first-to-act factor) and 16 hand-category multipliers
// (the PLAY_DEFAULT categories) per raise level, applied like REAL_MULT in engine.js. Coordinate descent on rms.
//   node fitgw.js <level> [--write]     writes realize/mult_L<level>.json (169 multipliers) and prints aggRe/rLow
const fs = require('fs');
const E = require('../../../app/engine.js');
const S2 = require('../solver2.js');
const LEVEL = +(process.argv[2] || 2), WRITE = process.argv.includes('--write');
const NC = 169, IDX = {}; E.CLASS_NAMES.forEach((n, i) => (IDX[n] = i));
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
for (let l = 1; l <= 3; l++) E.REAL_MULT[l].fill(1);
const GW = JSON.parse(fs.readFileSync(__dirname + '/gw_caller_share.json', 'utf8'));
const data = [];
for (const mk in GW) {
  const cfg = JSON.parse(fs.readFileSync(__dirname + `/gwcfgs/${mk}.json`, 'utf8')); if (cfg.level !== LEVEL) continue;
  const W = cfg.ranges.map((s) => { const w = new Float64Array(NC); for (const t of s.split(',')) { const [h, x] = t.split(':'); w[IDX[h]] = +x; } return w; });
  const pts = Object.entries(GW[mk].share).map(([h, s]) => ({ c: IDX[h], s: Math.min(0.97, s), w: SIZE[IDX[h]] * (W[cfg.caller][IDX[h]] || 0.01) }));
  // folds: the call share must stay below the price (hinge), weighted like a full-range hand
  const lim = GW[mk].cost / GW[mk].pot - 0.01; for (const h of GW[mk].folds || []) pts.push({ c: IDX[h], s: lim, w: SIZE[IDX[h]], hinge: true });
  data.push({ mk, cfg, W, pts });
}
// aggressor-side targets from our real postflop solves (realize outputs), lower weight (REALW, default 0.3)
const RDIR = process.env.RDIR, REALW = +(process.env.REALW || 0.3);
if (RDIR) for (const mk of fs.readdirSync(__dirname + '/cfgs').map((f) => f.replace(/-\d+\.json$/, '')).filter((v, i, a) => a.indexOf(v) === i)) {
  const cfg0 = JSON.parse(fs.readFileSync(__dirname + `/cfgs/${mk}-00.json`, 'utf8')); if ((cfg0.level || 1) !== LEVEL) continue;
  const AGG = { sb3b_btn: 0, bb3b_btn: 0, co_btn3b: 1, bb3b_utg: 0, sb_btn4b: 1, bb_btn: 1, bb_co: 1, bb_utg: 1 }; const agg = AGG[mk]; if (agg === undefined) continue;
  const files = fs.readdirSync(RDIR).filter((f) => f.startsWith('a-real-' + mk + '-')); if (files.length < 6) continue;
  const sum = new Float64Array(NC), cnt = new Float64Array(NC);
  for (const f of files) { const r = JSON.parse(fs.readFileSync(RDIR + '/' + f, 'utf8')); for (const fl in r.flops) { const o = r.flops[fl].classes[cfg0.names[agg]]; for (const h in o) { sum[IDX[h]] += o[h].ev * o[h].n; cnt[IDX[h]] += o[h].n; } } }
  const W = cfg0.ranges.map((s) => { const w = new Float64Array(NC); for (const t of s.split(',')) { const [h, x] = t.split(':'); w[IDX[h]] = +x; } return w; });
  const pts = []; for (let c = 0; c < NC; c++) if (cnt[c] > 300) pts.push({ c, s: Math.min(0.97, sum[c] / cnt[c] / cfg0.pot), w: REALW * SIZE[c] * W[agg][c] });
  data.push({ mk: mk + '/agg', cfg: { ...cfg0, agg, caller: agg }, W, pts });
}
console.log('level', LEVEL, 'matchups', data.map((d) => `${d.mk} (${d.pts.length} hands)`).join(', '));
// hand categories (same split as engine buildPlay)
const RANKS = '23456789TJQKA';
const CATS = ['pairLo', 'pairMid', 'pairHi', 'saWheel', 'saOther', 'sGap1', 'sGap2', 'sGap3', 'sGap4', 'sBroad', 'oBroad', 'oAceHi', 'oAceLo', 'oGap1', 'oGap2', 'oGap3'];
const catOf = new Array(NC);
for (let i = 0; i < NC; i++) {
  const nm = E.CLASS_NAMES[i], hi = RANKS.indexOf(nm[0]), lo = RANKS.indexOf(nm[1]), gap = hi - lo, suited = nm[2] === 's', pair = nm.length === 2; let k;
  if (pair) k = hi <= RANKS.indexOf('9') ? 'pairLo' : hi <= RANKS.indexOf('J') ? 'pairMid' : 'pairHi';
  else if (suited) { if (hi === 12) k = lo <= RANKS.indexOf('5') ? 'saWheel' : 'saOther'; else if (gap === 1) k = 'sGap1'; else if (gap === 2) k = 'sGap2'; else if (gap === 3) k = 'sGap3'; else k = 'sGap4'; if (hi >= RANKS.indexOf('T') && lo >= RANKS.indexOf('T')) k = 'sBroad'; }
  else { if (hi >= RANKS.indexOf('T') && lo >= RANKS.indexOf('T')) k = 'oBroad'; else if (hi === 12) k = lo >= RANKS.indexOf('T') ? 'oAceHi' : 'oAceLo'; else if (gap === 1) k = 'oGap1'; else if (gap === 2) k = 'oGap2'; else k = 'oGap3'; }
  catOf[i] = CATS.indexOf(k);
}
const P = { aggRe: 1, aggG: 1, rLow: S2.MODEL.rLow, theta: new Float64Array(CATS.length) };
const M = { gammaK: S2.MODEL.gammaK, gRe: S2.MODEL.gRe, playSpan: 5, playPos: S2.MODEL.pPos, playNeg: S2.MODEL.pNeg, implied: S2.MODEL.io };
const mult = new Float64Array(NC);
function nodeFor(cfg) {
  const pot = cfg.pot, spr = Math.min(...cfg.stacks) / pot, level = cfg.level, ramp = Math.min(1, Math.max(0, (spr - 0.25) / 1.75));
  const R = [1 - (1 - P.rLow) * ramp, 1]; if (level >= 2 && P.aggRe) R[cfg.agg] *= 1 + (P.aggRe - 1) * ramp;
  const gamma = 1 + M.gammaK * (1 + M.gRe * Math.max(0, level - 1)) * Math.log(1 + spr), cl1 = (x) => Math.min(1, Math.max(0, x));
  return { pot, R, gamma, level, agg: level >= 2 ? cfg.agg : -1, playK: cl1((spr - 1) / M.playSpan), playKp: cl1((spr - 1) / M.playSpan), ioK: cl1((spr - 1) / M.playSpan), playRe: 1 };
}
function share(nd, c, o, p) {
  const eq = E.EQ169[c * NC + o];
  const pl = (1 + E.playTerm(nd, c, E.PLAY169[c], M.playPos, M.playNeg, true)) * mult[c], plo = (1 + E.playTerm(nd, o, E.PLAY169[o], M.playPos, M.playNeg, true)) * mult[o];
  const gp = nd.gamma * (nd.agg === p ? P.aggG : 1), go = nd.gamma * (nd.agg === 1 - p ? P.aggG : 1);
  const a = nd.R[p] * pl * Math.pow(eq, gp), b = nd.R[1 - p] * plo * Math.pow(1 - eq, go);
  let s = a + b > 0 ? a / (a + b) : 0.5;
  const io = M.implied; if (io > 0) { const tp = io * Math.max(0, E.PLAY169[c]) * E.ioTerm(nd, c), to = io * Math.max(0, E.PLAY169[o]) * E.ioTerm(nd, o); s = Math.min(1, Math.max(0, s + tp * (1 - s) - to * s)); }
  return s;
}
function rms(print) {
  for (let c = 0; c < NC; c++) mult[c] = Math.exp(P.theta[catOf[c]]);
  let se = 0, n = 0; const rows = [];
  for (const d of data) {
    const nd = nodeFor(d.cfg), p = d.cfg.caller, wo = d.W[1 - p];
    for (const pt of d.pts) { let s = 0, t = 0; for (let o = 0; o < NC; o++) { const w = wo[o] * SIZE[o]; if (!(w > 0)) continue; s += w * share(nd, pt.c, o, p); t += w; } const m = s / t; const e = pt.hinge ? Math.max(0, m - pt.s) : m - pt.s; se += pt.w * e * e; n += pt.w; rows.push({ mk: d.mk, h: E.CLASS_NAMES[pt.c] + (pt.hinge ? '(fold)' : ''), gw: pt.s, m, w: pt.w, e }); }
  }
  if (print) { rows.sort((a, b) => Math.abs(b.e) * b.w - Math.abs(a.e) * a.w); console.log('  worst:', rows.slice(0, 14).map((r) => `${r.mk} ${r.h} GW ${r.gw.toFixed(2)} model ${r.m.toFixed(2)}`).join('; ')); }
  return Math.sqrt(se / n);
}
// categories with no direct (caller-side) data stay at 1
const catN = new Array(CATS.length).fill(0), catEq = new Array(CATS.length).fill(0); for (const d of data) if (!d.mk.endsWith('/agg')) for (const pt of d.pts) { catN[catOf[pt.c]]++; if (!pt.hinge) catEq[catOf[pt.c]]++; }
console.log('data points per category:', CATS.map((k, i) => `${k} ${catN[i]}`).join(', '));
console.log('rms before', rms(true).toFixed(4));
// coordinate descent
const tryStep = (get, set, step) => { const base = rms(); const v0 = get(); set(v0 + step); if (rms() < base) return true; set(v0 - step); if (rms() < base) return true; set(v0); return false; };
for (let round = 0; round < 12; round++) {
  let moved = false; const st = round < 4 ? 0.1 : round < 8 ? 0.04 : 0.015;
  moved |= tryStep(() => P.aggRe, (v) => (P.aggRe = Math.max(0.5, Math.min(3, v))), st * 2);
  if (process.env.FIXRLOW !== '1') moved |= tryStep(() => P.rLow, (v) => (P.rLow = Math.max(0.3, Math.min(1.2, v))), st);
  if (process.env.FITAGGG === '1') moved |= tryStep(() => P.aggG, (v) => (P.aggG = Math.max(0.5, Math.min(3, v))), st * 2);
  for (let k = 0; k < CATS.length; k++) if (catN[k] >= 2) moved |= tryStep(() => P.theta[k], (v) => (P.theta[k] = Math.max(-1.2, Math.min(catEq[k] >= 2 ? 1.2 : 0, v))), st);   // fold-only categories can only go down
  console.log(`round ${round} rms ${rms().toFixed(4)} aggRe ${P.aggRe.toFixed(2)} aggG ${P.aggG.toFixed(2)} rLow ${P.rLow.toFixed(2)}`);
  if (!moved) break;
}
console.log('rms after', rms(true).toFixed(4));
console.log('categories:', CATS.map((k, i) => `${k} x${Math.exp(P.theta[i]).toFixed(2)}`).join('  '));
if (WRITE) { fs.writeFileSync(__dirname + `/mult_L${LEVEL}.json`, JSON.stringify({ level: LEVEL, aggRe: +P.aggRe.toFixed(3), aggG: +P.aggG.toFixed(3), rLow: +P.rLow.toFixed(3), cats: Object.fromEntries(CATS.map((k, i) => [k, +Math.exp(P.theta[i]).toFixed(3)])), mult: Array.from(mult, (x) => +x.toFixed(4)) })); console.log('wrote mult_L' + LEVEL + '.json'); }
