// Fit the re-raised-pot valuation to GTO Wizard's own per-hand EVs, through the solver's OWN terminal code
// (PF2.prepTerminal + PF2.termVec2 in solver2.js), so the fitted numbers mean in the solve exactly what they meant here.
// Targets (realize/gwshare.py -> gw_caller_share.json, from gw/ev100.txt): for each 100bb re-raised node, the calling
// hands' realized pot share: pure call -> (EV + cost) / pot, call/fold mix -> cost / pot, fold -> below cost / pot (hinge).
// Opponent ranges: GW's own (realize/gwcfgs), since GW's EVs are against GW's ranges.
// Parameters: aggRe (the last raiser's realization factor in heads-up re-raised pots) and one multiplier per hand
// category on the playability factor (REAL_MULT, shared by 3-bet and 4-bet pots). Categories without data stay at 1.
//   PLAYREAL=none node realize/fitgw2.js [--write]      -> realize/mult_fit.json, ../model_real.json (with --write)
const fs = require('fs');
const S = require('../solver2.js'), E = S.E;
const WRITE = process.argv.includes('--write'), MARGIN = +(process.env.MARGIN || 0.01);
if (process.env.PLAYREAL !== 'none') { console.error('run with PLAYREAL=none (the fit starts from an empty table)'); process.exit(1); }
const NC = 169, IDX = {}; E.CLASS_NAMES.forEach((n, i) => (IDX[n] = i));
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const GW = JSON.parse(fs.readFileSync(__dirname + '/gw_caller_share.json', 'utf8'));
// one small two-player PF2 whose prepTerminal/termVec2 we call on hand-made terminals (the tree itself is not used)
const STK = 100.125, DEAD = 1.5;   // GW 100bb: every stack 100.125; the folded small blind and the big-blind ante are dead money
const s = new S.PF2({ names: ['SB', 'BB'], stacks: [STK, STK], posts: [0.5, 1], ante: 0, payoff: { type: 'chips' }, postOrder: [0, 1] });
s.cfg.postOrder = [0, 1]; s.cw = new Float64Array(NC).fill(1); s.cls = new Int32Array(2);
const rangeOf = (str) => { const w = new Float64Array(NC); for (const t of str.split(',')) { const [h, x] = t.split(':'); w[IDX[h]] = +x; } return w; };
const data = [];
for (const mk of Object.keys(GW)) {
  const cfg = JSON.parse(fs.readFileSync(__dirname + `/gwcfgs/${mk}.json`, 'utf8')); if (cfg.level < 2) continue;
  const put = (cfg.pot - DEAD) / 2;
  const W = cfg.ranges.map(rangeOf), p = cfg.caller;
  const pts = Object.entries(GW[mk].share).map(([h, sh]) => ({ c: IDX[h], s: Math.min(0.97, sh), w: SIZE[IDX[h]] * Math.max(0.05, W[p][IDX[h]] || 0) }));
  const lim = GW[mk].cost / GW[mk].pot - MARGIN;
  for (const h of GW[mk].folds || []) pts.push({ c: IDX[h], s: lim, w: SIZE[IDX[h]], hinge: true });
  data.push({ mk, cfg, put, W, p, pts, level: cfg.level });
}
// HOLDOUT=<matchup>: fit without it, then report its error before and after (does the fit generalize?)
const HOLD = process.env.HOLDOUT; let held = null;
if (HOLD) { const i = data.findIndex((d) => d.mk === HOLD); if (i < 0) { console.error('no matchup ' + HOLD); process.exit(1); } held = data.splice(i, 1)[0]; }
// the terminal for a matchup, built and prepped by the solver's own code (OOP is player 0: postOrder [0, 1])
function terminal(d) {
  const nd = { kind: 'flop', put: [d.put, d.put], folded: [false, false], level: d.level, aggr: d.cfg.agg };
  s.cfg.ante = DEAD; s.prepTerminal(nd); return nd;
}
// model pot share of every hand class for the caller, averaged over the opponent's range (one termVec2 per opponent class)
function shares(d) {
  const nd = terminal(d), p = d.p, o = 1 - p, wo = d.W[o], acc = new Float64Array(NC), out = new Float64Array(NC); let tot = 0;
  for (let oc = 0; oc < NC; oc++) {
    const w = wo[oc] * SIZE[oc]; if (!(w > 0)) continue;
    s.cls[o] = oc; s.termVec2(nd, p, o, out);
    for (let c = 0; c < NC; c++) acc[c] += w * (out[c] + nd.put[p]) / nd.pot;
    tot += w;
  }
  for (let c = 0; c < NC; c++) acc[c] /= tot;
  return { sh: acc, nd };
}
// hand categories (the engine's PLAY_DEFAULT split)
const RANKS = '23456789TJQKA';
const CATS = ['pairLo', 'pairMid', 'pairHi', 'saWheel', 'saOther', 'sGap1', 'sGap2', 'sGap3', 'sGap4', 'sBroad', 'oBroad', 'oAceHi', 'oAceLo', 'oGap1', 'oGap2', 'oGap3'];
const catOf = E.CLASS_NAMES.map((nm) => {
  const hi = RANKS.indexOf(nm[0]), lo = RANKS.indexOf(nm[1]), gap = hi - lo, suited = nm[2] === 's', pair = nm.length === 2; let k;
  if (pair) k = hi <= RANKS.indexOf('9') ? 'pairLo' : hi <= RANKS.indexOf('J') ? 'pairMid' : 'pairHi';
  else if (suited) { k = hi === 12 ? (lo <= RANKS.indexOf('5') ? 'saWheel' : 'saOther') : gap === 1 ? 'sGap1' : gap === 2 ? 'sGap2' : gap === 3 ? 'sGap3' : 'sGap4'; if (hi >= RANKS.indexOf('T') && lo >= RANKS.indexOf('T')) k = 'sBroad'; }
  else { if (hi >= RANKS.indexOf('T') && lo >= RANKS.indexOf('T')) k = 'oBroad'; else if (hi === 12) k = lo >= RANKS.indexOf('T') ? 'oAceHi' : 'oAceLo'; else k = gap === 1 ? 'oGap1' : gap === 2 ? 'oGap2' : 'oGap3'; }
  return CATS.indexOf(k);
});
const P = { aggRe: 1, theta: new Float64Array(CATS.length) };
const apply = () => { s.cfg.aggRe = P.aggRe; for (let l = 2; l <= 3; l++) for (let c = 0; c < NC; c++) E.REAL_MULT[l][c] = Math.exp(P.theta[catOf[c]]); };
function rms(print) {
  apply(); let se = 0, n = 0; const rows = [];
  for (const d of data) {
    const { sh } = shares(d);
    for (const pt of d.pts) { const m = sh[pt.c], e = pt.hinge ? Math.max(0, m - pt.s) : m - pt.s; se += pt.w * e * e; n += pt.w; rows.push({ mk: d.mk, h: E.CLASS_NAMES[pt.c] + (pt.hinge ? '(fold)' : ''), gw: pt.s, m, w: pt.w, e }); }
  }
  if (print) { rows.sort((a, b) => Math.abs(b.e) * b.w - Math.abs(a.e) * a.w); console.log('  worst:', rows.slice(0, 12).map((r) => `${r.mk} ${r.h} GW ${r.gw.toFixed(2)} model ${r.m.toFixed(2)}`).join('; ')); }
  return Math.sqrt(se / n);
}
const catN = new Array(CATS.length).fill(0), catEq = new Array(CATS.length).fill(0);
for (const d of data) for (const pt of d.pts) { catN[catOf[pt.c]]++; if (!pt.hinge) catEq[catOf[pt.c]]++; }
console.log('matchups:', data.map((d) => `${d.mk} L${d.level} (${d.pts.length})`).join(', '));
console.log('targets per category:', CATS.map((k, i) => `${k} ${catN[i]}/${catEq[i]}`).join(' '));
const heldErr = () => { if (!held) return null; apply(); const { sh } = shares(held); let se = 0, n = 0; for (const pt of held.pts) { const m = sh[pt.c], e = pt.hinge ? Math.max(0, m - pt.s) : m - pt.s; se += pt.w * e * e; n += pt.w; } return Math.sqrt(se / n); };
const heldBefore = heldErr();
console.log('rms before', rms(true).toFixed(4));
const tryStep = (get, set, step) => { const base = rms(); const v0 = get(); set(v0 + step); if (rms() < base - 1e-6) return true; set(v0 - step); if (rms() < base - 1e-6) return true; set(v0); return false; };
for (let round = 0; round < 16; round++) {
  const st = round < 5 ? 0.1 : round < 10 ? 0.04 : 0.015; let moved = false;
  moved |= tryStep(() => P.aggRe, (v) => (P.aggRe = Math.max(0.6, Math.min(2.5, v))), st * 2);
  // a category moves only with at least two targets; fold-only categories may only go down
  for (let k = 0; k < CATS.length; k++) if (catN[k] >= 2) moved |= tryStep(() => P.theta[k], (v) => (P.theta[k] = Math.max(-1.0, Math.min(catEq[k] >= 2 ? 1.0 : 0, v))), st);
  console.log(`round ${round} rms ${rms().toFixed(4)} aggRe ${P.aggRe.toFixed(3)}`);
  if (!moved && round >= 10) break;
}
console.log('rms after', rms(true).toFixed(4));
if (held) console.log(`HELD-OUT ${HOLD}: rms ${heldBefore.toFixed(4)} -> ${heldErr().toFixed(4)}`);
console.log('categories:', CATS.map((k, i) => `${k} x${Math.exp(P.theta[i]).toFixed(2)}`).join('  '));
const mult = Array.from(E.REAL_MULT[2], (x) => +x.toFixed(4));
const res = { aggRe: +P.aggRe.toFixed(3), cats: Object.fromEntries(CATS.map((k, i) => [k, +Math.exp(P.theta[i]).toFixed(3)])), rms: +rms().toFixed(4), l2: mult, l3: mult };
if (!HOLD) fs.writeFileSync(__dirname + '/mult_fit.json', JSON.stringify(res));
if (WRITE && !HOLD) { fs.writeFileSync(__dirname + '/../model_real.json', JSON.stringify({ l2: mult, l3: mult })); console.log('wrote realize/mult_fit.json and model_real.json; aggRe', res.aggRe); }
