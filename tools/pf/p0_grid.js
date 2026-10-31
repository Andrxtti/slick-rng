// SOLVER_PLAN P0: baseline + test harness for a preflop library (default: the shipped docs/lib, read only).
//   node tools/pf/p0_grid.js [libdir] [tag]   -> tools/pf/p0/grid_<tag>.json, grid_<tag>.md, spots_<tag>.json
// The grid: format x depth x node type (open, vs open, vs limp, BvB, squeeze, vs jam, vs 3-bet, cold vs 3-bet, vs 4-bet),
// hero's action mix (fold / call-check / sized raise / jam), weighted by reach x combos x node probability, and the local
// regret: per hand, the best action's EV minus the EV of the mixed strategy (bb), weighted the same way. Local regret is a
// convergence proxy, NOT the exploitability of the whole multi-player tree (there is no full best-response tool).
// The spots: the cloud audit's regression spots (INBOX 19:55) with each hand class's mix and EVs, to compare before/after.
const fs = require('fs'), path = require('path');
const E = require('../../app/engine.js');
const NC = 169, NAMES = E.CLASS_NAMES;
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const libdir = process.argv[2] || path.join(__dirname, '../../docs/lib'), tag = process.argv[3] || 'shipped';
const out = path.join(__dirname, 'p0'); fs.mkdirSync(out, { recursive: true });

const b64 = (s) => Buffer.from(s, 'base64');
function decode(n) {
  const na = n.a.length, s8 = b64(n.s), e8 = b64(n.e), r8 = b64(n.r);
  const e16 = new Int16Array(e8.buffer, e8.byteOffset, e8.byteLength >> 1);
  const strat = [], ev = [], reach = [];
  for (let c = 0; c < NC; c++) {
    strat.push(Array.from(s8.subarray(c * na, c * na + na), (v) => v / 255));
    ev.push(Array.from(e16.subarray(c * na, c * na + na), (v) => v / 100));
    reach.push(r8.length === NC ? r8[c] / 255 : 1);
  }
  return { strat, ev, reach };
}
const kind = (a) => (a === 'fold' ? 'fold' : a === 'call' || a === 'check' ? 'call' : a === 'allin' ? 'jam' : 'raise');

// who acted with each token of the path (players fold out; order is UTG.. BB, cycling over those still in)
function seatsOf(p, nP) {
  const toks = p ? p.split('|') : [], live = [...Array(nP).keys()], who = []; let i = 0;
  for (const t of toks) { const s = live[i % live.length]; who.push(s); if (t === 'fold') { live.splice(i % live.length, 1); } else i++; }
  return { toks, who };
}
function nodeType(p, hero, nP) {
  const { toks, who } = seatsOf(p, nP);
  const raises = [], calls = []; let heroRaised = false;
  toks.forEach((t, k) => { if (t === 'raise' || t === 'allin') { raises.push({ s: who[k], jam: t === 'allin' }); if (who[k] === hero) heroRaised = true; } else if (t === 'call') calls.push(who[k]); });
  const sb = nP - 2, bb = nP - 1;
  if (raises.length === 0) return calls.length ? (hero === bb && calls.length === 1 && calls[0] === sb ? 'BvB vs limp' : 'vs limp') : hero === sb ? 'BvB open' : 'open';
  const last = raises[raises.length - 1];
  if (raises.length === 1) {
    if (last.jam) return 'vs jam';
    const callsAfter = toks.slice(toks.findIndex((t) => t === 'raise')).filter((t) => t === 'call').length;
    if (callsAfter > 0) return 'squeeze';
    return hero === bb && last.s === sb ? 'BvB vs open' : 'vs open';
  }
  if (raises.length === 2) return heroRaised ? (last.jam ? 'vs 3-bet jam' : 'vs 3-bet') : 'cold vs 3-bet';
  return heroRaised ? (last.jam ? 'vs 4-bet jam' : 'vs 4-bet') : 'cold vs 4-bet';
}

const grid = {}, files = fs.readdirSync(libdir).filter((f) => /^m\d-d\d+\.json$/.test(f)).sort();
for (const f of files) {
  const D = JSON.parse(fs.readFileSync(path.join(libdir, f), 'utf8')), nP = D.names.length;
  const fmt = `${nP}max`, depth = Math.round(D.stacks[0]);
  for (const n of D.nodes) {
    const t = nodeType(n.p, n.h, nP), d = decode(n), key = `${fmt}|${depth}|${t}`;
    const g = grid[key] || (grid[key] = { fmt, depth, type: t, nodes: 0, w: 0, mix: { fold: 0, call: 0, raise: 0, jam: 0 }, regret: 0 });
    g.nodes++;
    for (let c = 0; c < NC; c++) {
      const w = (n.prob || 0) * d.reach[c] * SIZE[c]; if (w <= 0) continue;
      const st = d.strat[c], ev = d.ev[c]; let mixed = 0, best = -1e9;
      st.forEach((x, a) => { g.mix[kind(n.a[a])] += w * x; mixed += x * ev[a]; best = Math.max(best, ev[a]); });
      g.w += w; g.regret += w * (best - mixed);
    }
  }
}
const rows = Object.values(grid).map((g) => ({ ...g, mix: Object.fromEntries(Object.entries(g.mix).map(([k, v]) => [k, +(v / (g.w || 1)).toFixed(4)])), regret: +(g.regret / (g.w || 1)).toFixed(4) }));
rows.sort((a, b) => a.fmt.localeCompare(b.fmt) || a.depth - b.depth || a.type.localeCompare(b.type));
fs.writeFileSync(path.join(out, `grid_${tag}.json`), JSON.stringify(rows, null, 1));
const md = [`# P0 grid (${tag}): hero action mix by depth x node type, weighted by reach x combos x node probability`, '',
  'Local regret = best action EV minus the mixed strategy EV, bb per hand (a convergence proxy, not full exploitability).', '',
  '| fmt | bb | node type | nodes | fold | call | raise | jam | local regret |', '|---|---|---|---|---|---|---|---|---|'];
for (const r of rows) md.push(`| ${r.fmt} | ${r.depth} | ${r.type} | ${r.nodes} | ${(r.mix.fold * 100).toFixed(1)} | ${(r.mix.call * 100).toFixed(1)} | ${(r.mix.raise * 100).toFixed(1)} | ${(r.mix.jam * 100).toFixed(1)} | ${r.regret.toFixed(3)} |`);
fs.writeFileSync(path.join(out, `grid_${tag}.md`), md.join('\n') + '\n');

// regression spots (cloud audit, INBOX 19:55): 8-max paths; hero is the player to act there
const F = (k) => Array(k).fill('fold').join('|');
const SPOTS = [
  { id: 'bb-vs-btn-40', file: 'm8-d40.json', p: `${F(5)}|raise|fold`, why: 'BB vs BTN 2.2x, 40bb: called 74%, TT-QQ/AQ/AJo jammed, small 3-bet only AA/KK + offsuit-ace blockers' },
  { id: 'bb-vs-utg-40', file: 'm8-d40.json', p: `raise|${F(6)}`, why: 'BB vs UTG, 40bb: small 3-bet 0.4%, jam 4.9%' },
  { id: 'btn-vs-bb3b-40', file: 'm8-d40.json', p: `${F(5)}|raise|fold|raise`, why: 'BTN facing BB small 3-bet, 40bb: folds 37-42%, calls 42-50%, small 4-bet ~0' },
  { id: 'btn-vs-bb3b-60', file: 'm8-d60.json', p: `${F(5)}|raise|fold|raise`, why: 'the same at 60bb' },
  { id: 'bb-vs-btn-25', file: 'm8-d25.json', p: `${F(5)}|raise|fold`, why: 'BB vs BTN, 25bb: fold 0% (72o calls at -0.99 vs -1.00), small 3-bet 0%' },
  { id: 'bb-vs-btn-20', file: 'm8-d20.json', p: `${F(5)}|raise|fold`, why: '20-30bb BB defends 99-100% vs a BTN min-raise' },
  { id: 'bb-vs-btn-30', file: 'm8-d30.json', p: `${F(5)}|raise|fold`, why: 'the same at 30bb' },
  { id: 'bb-vs-btn-100', file: 'm8-d100.json', p: `${F(5)}|raise|fold`, why: '100bb looked sane (3-bet 16%): must not regress' },
];
const KEY_HANDS = ['AA', 'KK', 'QQ', 'JJ', 'TT', '99', '66', 'AKs', 'AQs', 'AQo', 'AJo', 'A5s', 'KQo', 'QJs', 'T9s', '72o'];
const spots = [];
for (const sp of SPOTS) {
  const fp = path.join(libdir, sp.file); if (!fs.existsSync(fp)) { spots.push({ ...sp, missing: 'file' }); continue; }
  const D = JSON.parse(fs.readFileSync(fp, 'utf8')); const n = D.nodes.find((x) => x.p === sp.p);
  if (!n) { spots.push({ ...sp, missing: 'node (pruned or not in the tree)' }); continue; }
  const d = decode(n); let w = 0; const mix = {}; n.lab.forEach((l) => (mix[l] = 0));
  for (let c = 0; c < NC; c++) { const ww = d.reach[c] * SIZE[c]; w += ww; d.strat[c].forEach((x, a) => (mix[n.lab[a]] += ww * x)); }
  for (const k in mix) mix[k] = +(mix[k] / w).toFixed(4);
  const hands = {}; for (const h of KEY_HANDS) { const c = NAMES.indexOf(h); if (c >= 0) hands[h] = Object.fromEntries(n.lab.map((l, a) => [l, [+d.strat[c][a].toFixed(2), d.ev[c][a]]])); }
  spots.push({ ...sp, hero: D.names[n.h], actions: n.lab, mix, hands });
}
fs.writeFileSync(path.join(out, `spots_${tag}.json`), JSON.stringify(spots, null, 1));
console.log(`${files.length} files, ${rows.length} grid squares, ${spots.length} spots -> ${out}`);
for (const s of spots) console.log(s.id, s.missing || JSON.stringify(s.mix));
