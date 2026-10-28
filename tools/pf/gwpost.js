// Postflop check vs GTO Wizard: BTN opens 2.5, BB calls, flop Ks7d2c (100bb MTT chipEV, pot 6.5, stacks 97.5).
// Ranges come from the GW node weights (gw/post_K72.txt); we solve the same flop with our VecSolver and compare
// BTN's c-bet strategy and BB's reply to the 33% bet, hand by hand (combo-weighted), plus the overall frequencies.
//   node gwpost.js [secs] [wasm=1]
const fs = require('fs');
const E = require('../../app/engine.js');
const SECS = +(process.argv[2] || 60);
const lines = fs.readFileSync(__dirname + '/gw/post_K72.txt', 'utf8').split('\n').filter((l) => l && !l.startsWith('#'));
const parse = (l) => { const [node, acts, hands] = l.split('\t'); const H = {}; for (const t of hands.split(' ')) { const m = t.match(/^(\w+):([^@]*)@(\d+)(?:\|([\d.-]+))?$/); if (!m) continue; const mix = {}; for (const p of m[2].split(',')) { if (!p) continue; const [k, v] = p.split('='); mix[+k] = +v / 100; } H[m[1]] = { mix, w: +m[3] / 100, ev: m[4] === undefined ? null : +m[4] }; } return { node, acts: acts.split('/'), H }; };
const G = {}; for (const l of lines) { const r = parse(l); G[r.node] = r; }
const btn = G['BTN|X'], bb = G['BB|X-R2.1'];
const rangeOf = (H) => Object.entries(H).map(([h, x]) => `${h}:${x.w.toFixed(2)}`).join(',');
const spot = E.makeSpot({ players: [
  { name: 'BB', stack: 97.5, range: rangeOf(bb.H) },
  { name: 'BTN', stack: 97.5, range: rangeOf(btn.H) } ],
  board: 'Ks7d2c', pot: 6.5, sizes: { flop: { bet: '33%, 100%', raise: '2.7x' }, turn: { bet: '50%, 100%', raise: '2.5x' }, river: { bet: '50%, 100%, a', raise: '2.5x, a' } } });
// what the app runs for a full-range flop spot: the sampled engine with card abstraction (exact CFR needs >4GB here)
const sv = new E.Solver(spot);
console.log('sampled solver, nodes', sv.nodes.length, 'buckets', spot.turnBuckets, spot.riverBuckets);
const t0 = Date.now();
while (Date.now() - t0 < SECS * 1000) sv.run(5000);
const ev = sv.estimateEV(40000);
console.log(`${((Date.now() - t0) / 1000).toFixed(0)}s iters ${sv.iters} | value BB ${ev[0].ev.toFixed(3)} BTN ${ev[1].ev.toFixed(3)} (bb, after the flop)`);
const SIZE = {}; for (let i = 0; i < E.NUM_COMBOS; i++) { const k = E.comboClass(i); SIZE[k] = (SIZE[k] || 0) + 1; }
const cmp = (title, rep, ref, map) => {
  // map: our action index -> GW action index (or null = not in GW tree)
  console.log(`\n== ${title}: ours ${rep.actions.join(' / ')} | GW ${ref.acts.join(' / ')}`);
  let ours = new Array(ref.acts.length).fill(0), theirs = new Array(ref.acts.length).fill(0), tot = 0, l1 = 0, agree = 0;
  const rows = [];
  for (const h in ref.H) {
    const e = rep.classes[h]; if (!e) continue;
    const w = ref.H[h].w * SIZE[h]; tot += w;
    const o = new Array(ref.acts.length).fill(0); rep.actions.forEach((_, a) => { if (map[a] !== null && map[a] !== undefined) o[map[a]] += e.s[a]; });
    const t = new Array(ref.acts.length).fill(0); for (const k in ref.H[h].mix) t[+k] = ref.H[h].mix[k];
    for (let a = 0; a < o.length; a++) { ours[a] += w * o[a]; theirs[a] += w * t[a]; }
    const d = o.reduce((s, x, a) => s + Math.abs(x - t[a]), 0) / 2; l1 += w * d;
    const top = (v) => v.indexOf(Math.max(...v)); if (top(o) === top(t)) agree += w;
    rows.push({ h, d, o, t });
  }
  const pct = (v) => v.map((x) => (100 * x / tot).toFixed(1) + '%').join(' / ');
  console.log(`overall ours ${pct(ours)} | GW ${pct(theirs)}`);
  console.log(`mean |diff| per hand ${(100 * l1 / tot).toFixed(1)} pts, top-action agreement ${(100 * agree / tot).toFixed(1)}%`);
  rows.sort((a, b) => b.d - a.d);
  console.log('worst:', rows.slice(0, 12).map((r) => `${r.h} ours ${r.o.map((x) => Math.round(100 * x)).join('/')} GW ${r.t.map((x) => Math.round(100 * x)).join('/')}`).join('; '));
};
// BTN after BB checks: our root is BB to act (action 0 = check presumably)
const root = sv.report([]);
const chk = root.actions.findIndex((l) => /check/i.test(l));
console.log('BB first action (ours):', root.actions.map((l, a) => `${l} ${(100 * root.overall[a]).toFixed(1)}%`).join('  '), '| GW: Check 100%');
const r1 = sv.report([{ a: chk }]);
// map our BTN actions to GW's: bet 33% -> 0, check -> 1, bet 100% -> 0 (count as a bet)
cmp('BTN c-bet on Ks7d2c', r1, btn, r1.actions.map((l) => (/check/i.test(l) ? 1 : 0)));
const b33 = r1.actions.findIndex((l) => /2\.1|bet 2/i.test(l));
const r2 = sv.report([{ a: chk }, { a: b33 >= 0 ? b33 : 1 }]);
cmp('BB vs 33% c-bet', r2, bb, r2.actions.map((l) => (/fold/i.test(l) ? 2 : /call/i.test(l) ? 1 : 0)));
