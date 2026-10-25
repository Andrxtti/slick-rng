// Solve one library table with the clean tree and harvest its training spots (labels straight from the tree).
//   node solve_lib.js <cfg.json> <solveSecs> <evalSecs> <out.json>
const fs = require('fs');
const { PF2, label, E, MODEL } = require('./solver2.js');
const [file, SECS = 240, EVS = 60, OUT] = process.argv.slice(2);
const D = JSON.parse(fs.readFileSync(file, 'utf8'));
const cfg = { ...D.cfg }; delete cfg.allowLimps; delete cfg.leave1; delete cfg.lite; cfg.maxNodes = 900000;
const t0 = Date.now(); const s = new PF2(cfg);
const NC = 169, SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const nP = cfg.names.length;
process.stderr.write(`${file.split('/').pop()} players ${nP} nodes ${s.nodes.length}\n`);
for (let k = 0; k < SECS / 30; k++) { s.run(30000); process.stderr.write(`  ${Math.round((Date.now() - t0) / 1000)}s iters ${s.iters}\n`); }
// two independent evaluation passes: a hand is only trusted when both agree on the best play
s.evalPass(EVS * 500); const EVA = Float64Array.from(s.EV2), EWA = Float64Array.from(s.EW2);
s.EV2.fill(0); s.EW2.fill(0); s.evalPass(EVS * 500); const EVB = Float64Array.from(s.EV2), EWB = Float64Array.from(s.EW2);
for (let i = 0; i < s.EV2.length; i++) s.EV2[i] = EVA[i] + EVB[i]; for (let i = 0; i < s.EW2.length; i++) s.EW2[i] = EWA[i] + EWB[i];
// model-robustness passes: the same strategies, re-scored with a harsher and a kinder postflop model. A hand is only
// dealt when its answer does not hinge on how well we model postflop play.
const PERT = [], base = { rLow: s.cfg.rLow, gammaK: s.cfg.gammaK };
const reprep = () => { for (const nd of s.nodes) if (nd.kind === 'flop') s.prepTerminal(nd); };
const kInit0 = MODEL.kInit, pA0 = MODEL.pA;
// harsher, kinder, and "the rest of the stacks get in on low-SPR flops" (all-in twin on)
for (const d of [{ rLow: -0.06, gammaK: +0.2, kInit: 0, pA: pA0 }, { rLow: +0.06, gammaK: -0.2, kInit: 1, pA: pA0 }, { rLow: 0, gammaK: 0, kInit: kInit0, pA: pA0 > 0 ? 0 : 2.5 }]) {
  s.cfg.rLow = Math.min(1, base.rLow + d.rLow); s.cfg.gammaK = Math.max(0, base.gammaK + d.gammaK); MODEL.kInit = d.kInit; MODEL.pA = d.pA; reprep();
  s.EV2.fill(0); s.EW2.fill(0); s.evalPass(EVS * 500);
  PERT.push({ EV: Float64Array.from(s.EV2), EW: Float64Array.from(s.EW2) });
}
s.cfg.rLow = base.rLow; s.cfg.gammaK = base.gammaK; MODEL.kInit = kInit0; MODEL.pA = pA0; reprep();
for (let i = 0; i < s.EV2.length; i++) s.EV2[i] = EVA[i] + EVB[i]; for (let i = 0; i < s.EW2.length; i++) s.EW2[i] = EWA[i] + EWB[i];
const evPass = (EVx, EWx, id, c) => { const nd = s.nodes[id], na = nd.actions.length, w = EWx[nd.ewOff + c]; if (!(w > 0)) return null; const o = []; for (let a = 0; a < na; a++) o.push(EVx[nd.evOff + c * na + a] / w); return o; };
// ---- walk the tree with everyone's average strategy: how often each line happens, and who still holds what
const found = [];
function walk(id, reach, acted, path, prob, clean) {
  const nd = s.nodes[id]; if (nd.kind !== 'act') return;
  const q = nd.player, na = nd.actions.length;
  const strat = []; let den = 0; for (let c = 0; c < NC; c++) { strat.push(s.avg(id, c)); den += SIZE[c] * reach[q][c]; }
  if (den <= 0) return;
  found.push({ id, h: q, path: path.slice(), prob, first: !acted[q], reachHero: Float64Array.from(reach[q]), clean });
  for (let a = 0; a < na; a++) {
    let num = 0, noise = 0; for (let c = 0; c < NC; c++) { const w = SIZE[c] * reach[q][c] * strat[c][a]; num += w; if (strat[c][a] < 0.12) noise += w; }
    const pa = num / den; if (pa < 1e-4) continue;
    const k = nd.actions[a].k;
    // quality of the range behind each non-fold action on the way: a line the solver barely uses, or one made
    // mostly of unconverged dust, never leads to a training spot
    const ok = clean && (k === 'fold' || k === 'check' || (pa >= 0.05 && noise / num <= 0.25));
    const r2 = reach.map((r, p) => (p === q ? Float64Array.from(r, (x, c) => x * strat[c][a]) : r));
    const ac2 = acted.slice(); ac2[q] = true;
    walk(nd.children[a], r2, ac2, path.concat([{ id, q, a, k, to: nd.actions[a].to }]), prob * pa, ok);
  }
}
walk(0, Array.from({ length: nP }, () => new Float64Array(NC).fill(1)), new Array(nP).fill(false), [], 1, true);
// ---- pick the spots
const unit = (cfg.payoff && cfg.payoff.type === 'icm') ? 'icm' : 'bb', tol = unit === 'icm' ? 0.015 : 0.05;
// credibility gate: a hand is only dealt when the best play beats the next best by a clear margin
const GAP = unit === 'icm' ? 0.02 : 0.12;
const PREMIUM = new Set(['AA', 'KK', 'QQ', 'JJ', 'AKs', 'AKo']), NAMES = E.CLASS_NAMES;
const EQR = (() => { const v = []; for (let c = 0; c < NC; c++) { let s2 = 0, w = 0; for (let o = 0; o < NC; o++) { s2 += E.EQ169[c * NC + o] * SIZE[o]; w += SIZE[o]; } v.push(s2 / w); } return v; })();
const RANK = (() => { const idx = [...Array(NC).keys()].sort((a, b) => EQR[b] - EQR[a]); const r = new Array(NC); idx.forEach((c, i) => (r[c] = i)); return r; })();
const out = [];
let firsts = 0, seconds = 0, fragile = 0, bvbSkip = 0;
const cands = found.filter((f) => f.clean && s.nodes[f.id].actions.some((a) => a.k === 'fold'));
cands.sort((x, y) => y.prob - x.prob);
for (const f of cands) {
  const nd = s.nodes[f.id], keys = nd.actions.map((a) => a.k);
  const nonfold = f.path.filter((e) => e.k !== 'fold' && e.k !== 'check');
  // blind vs blind at 25bb or less: our limp/raise/jam mix there does not match solved BvB references closely enough
  // (we jam more and limp less), so neither blind is quizzed in it
  { const hn = cfg.names[f.h], sb = cfg.names.indexOf('SB'), bb = cfg.names.indexOf('BB');
    const others = f.path.filter((e) => e.q !== sb && e.q !== bb);
    if ((hn === 'SB' || hn === 'BB') && sb >= 0 && bb >= 0 && others.every((e) => e.k === 'fold') && Math.min(cfg.stacks[sb], cfg.stacks[bb]) <= 25 + 1e-9) { bvbSkip++; continue; } }
  const jams = nonfold.filter((e) => e.k === 'allin').length; if (jams >= 2) continue;
  if (f.first) { if (f.prob < 0.002 || firsts >= 16) continue; }
  else { const mine = f.path.filter((e) => e.q === f.h); if (seconds >= 8 || nonfold.length > 2 || !['raise', 'allin'].includes(nonfold[nonfold.length - 1].k) || f.prob < 0.0005 || !mine.length) continue; }
  const aggro = nonfold.length > 0 && nonfold.some((e) => e.k === 'raise' || e.k === 'allin');
  const rows = []; let tried = 0, incons = 0; const best = new Array(NC).fill('.');
  const mx = Math.max(...f.reachHero);
  for (let c = 0; c < NC; c++) {
    if (!f.first && !(f.reachHero[c] / mx >= 0.2)) continue;
    const st = s.avg(f.id, c), ev = s.evOf2(f.id, c) || s.evOf(f.id, c); if (!ev) continue;
    const top = Math.max(...st), fav = st.indexOf(top), es = ev.slice().sort((x, y) => y - x);
    tried++; if (ev[fav] < es[0] - tol) { incons++; continue; }
    if (top >= 0.55) best[c] = String(fav);
    if (top < 0.6) continue;                       // genuine mixes are never dealt: either answer would be right
    if (es[0] - es[1] < GAP) continue;             // near-ties are never dealt either
    { const ea = evPass(EVA, EWA, f.id, c), eb = evPass(EVB, EWB, f.id, c); if (!ea || !eb) continue;
      const ba = ea.indexOf(Math.max(...ea)), bb2 = eb.indexOf(Math.max(...eb)); if (ba !== fav || bb2 !== fav) continue;   // unstable: the two passes disagree
      const ga = ea[fav] - Math.max(...ea.filter((_, i) => i !== fav)), gb = eb[fav] - Math.max(...eb.filter((_, i) => i !== fav)); if (Math.min(ga, gb) < GAP * 0.6) continue; }
    { let robust = true; for (const P of PERT) { const ep = evPass(P.EV, P.EW, f.id, c); if (!ep) { robust = false; break; } const bp = ep.indexOf(Math.max(...ep)); const gp = ep[fav] - Math.max(...ep.filter((_, i) => i !== fav)); if (bp !== fav || gp < GAP * 0.3) { robust = false; break; } }
      if (!robust) { fragile++; continue; } }
    const name = NAMES[c];
    if (PREMIUM.has(name) && (!aggro || ((name === 'AA' || name === 'KK') && top >= 0.97))) continue;
    if (keys[fav] === 'fold' && RANK[c] >= 62 && st[fav] >= 0.9) continue;   // trash nobody is tempted to play
    rows.push([c, +(es[0] - es[1]).toPrecision(3), st.map((x) => Math.round(x * 100) / 100), ev.map((x) => +x.toPrecision(4))]);
  }
  if (tried && incons / tried > 0.2) continue;     // not converged here
  if (rows.length < (f.first ? 8 : 5)) continue;
  rows.sort((a, b) => a[1] - b[1]);
  let keep = rows.slice(0, Math.max(6, Math.floor(rows.length * 0.85)));
  const tops = keep.map((r) => r[2].indexOf(Math.max(...r[2]))).filter((_, i) => Math.max(...keep[i][2]) >= 0.95);
  if (tops.length && Math.max(...[...new Set(tops)].map((x) => tops.filter((y) => y === x).length)) > 0.85 * keep.length) continue;
  if (keep.length > 30) keep = [...Array(30)].map((_, i) => keep[Math.round((i * (keep.length - 1)) / 29)]);
  // the table as the player sees it, built from the same tree that was solved
  const put = cfg.posts.slice(), folded = new Array(nP).fill(false), acts = Array.from({ length: nP }, () => []);
  for (const e of f.path) { const pn = s.nodes[e.id]; acts[e.q].push(label(pn, pn.actions[e.a], cfg)); if (e.k === 'fold') folded[e.q] = true; else if (e.k === 'call') put[e.q] = Math.min(pn.bet, cfg.stacks[e.q]); else if (e.k === 'raise' || e.k === 'allin') put[e.q] = Math.min(e.to, cfg.stacks[e.q]); }
  const pot = put.reduce((a, b) => a + b, 0) + (cfg.ante || 0);
  const vsAllin = f.path.some((e) => e.k === 'allin');
  const cat = vsAllin ? 'Facing a jam' : nd.level === 0 ? (cfg.names[f.h] === 'BB' ? 'Facing a limp' : 'First in') : nd.level === 1 ? 'Facing a raise' : 'Facing a 3-bet or more';
  const opts = nd.actions.map((a) => label(nd, a, cfg));
  out.push({ h: f.h, p: f.path.map((e) => e.k), a: keys, to: nd.actions.map((a) => a.to || 0), w: f.first ? Math.max(1, Math.round(f.prob * 1000)) : 9, r: keep.map((r) => [r[0], r[2], r[3]]), g: best.join(''),
    v: { seats: cfg.names.map((nm, i) => [nm, +(+cfg.stacks[i]).toFixed(2), +(+put[i]).toFixed(2), folded[i] ? 1 : 0, acts[i].join(' · ')]), pot: +pot.toFixed(2), opts, cat } });
  if (f.first) firsts++; else seconds++;
}
// self-check: every label must describe its own action
for (const n of out) n.a.forEach((k, i) => { const l = n.v.opts[i].toLowerCase(); const ok = k === 'fold' ? l === 'fold' : k === 'check' ? l === 'check' : k === 'call' ? (l.startsWith('call') || l === 'complete') : k === 'allin' ? l.startsWith('jam') : /^(raise|3-bet|4-bet)/.test(l); if (!ok) throw new Error('label mismatch ' + k + ' / ' + l); });
const res = { gid: D.gid, unit, snap: D.snap, file: file.split('/').pop(), model: { rLow: base.rLow, gammaK: base.gammaK, s0: MODEL.s0, s1: MODEL.s1, kInit: MODEL.kInit, pA: MODEL.pA, jamCap3: +(process.env.JAMCAP3 || 25) }, fragile, bvbSkip, iters: s.iters, nodes: s.nodes.length, secs: Math.round((Date.now() - t0) / 1000), spots: out };
fs.writeFileSync(OUT, JSON.stringify(res));
process.stderr.write(`done ${res.file}: ${out.length} spots (${firsts} first, ${seconds} second), ${fragile} model-sensitive hands held back, ${bvbSkip} short BvB spots skipped, in ${res.secs}s\n`);
