// Solve one chart table and dump EVERY reachable decision (strategy + EV per hand class) for the Solver-page library.
//   node solve_full.js <cfg.json> <solveSecs> <evalSecs> <out.json>      (TARGET_ITERS, MINP env)
const fs = require('fs');
const { PF2, label, E } = require('./solver2.js');
const [file, SECS = 600, EVS = 60, OUT] = process.argv.slice(2);
const D = JSON.parse(fs.readFileSync(file, 'utf8'));
const cfg = { ...D.cfg }; delete cfg.allowLimps; delete cfg.leave1; delete cfg.lite; cfg.maxNodes = 900000;
const t0 = Date.now(), s = new PF2(cfg), NC = 169, nP = cfg.names.length;
const SIZE = new Array(NC).fill(0); for (let i = 0; i < E.NUM_COMBOS; i++) SIZE[E.CLASS_OF[i]]++;
const TARGET = +(process.env.TARGET_ITERS || 0), MINP = +(process.env.MINP || 1e-4);
for (let k = 0; k < SECS / 30 || (TARGET > 0 && s.iters < TARGET && Date.now() - t0 < 3 * SECS * 1000); k++) { s.run(30000); process.stderr.write(`  ${Math.round((Date.now() - t0) / 1000)}s iters ${s.iters}\n`); }
s.evalPass(EVS * 1000);
const nodes = [];
const b64 = (u8) => Buffer.from(u8.buffer, u8.byteOffset, u8.byteLength).toString('base64');
function walk(id, reach, path, prob) {
  const nd = s.nodes[id]; if (nd.kind !== 'act') return;
  const q = nd.player, na = nd.actions.length, strat = []; let den = 0;
  for (let c = 0; c < NC; c++) { strat.push(s.avg(id, c)); den += SIZE[c] * reach[q][c]; }
  if (den <= 0) return;
  const S8 = new Uint8Array(NC * na), R8 = new Uint8Array(NC), E16 = new Int16Array(NC * na), mx = Math.max(...reach[q]);
  for (let c = 0; c < NC; c++) {
    R8[c] = Math.round(255 * reach[q][c] / mx);
    const ev = s.evOf2(id, c) || [];
    for (let a = 0; a < na; a++) { S8[c * na + a] = Math.round(255 * strat[c][a]); E16[c * na + a] = ev.length ? Math.max(-32000, Math.min(32000, Math.round(ev[a] * 100))) : -32768; }
  }
  nodes.push({ p: path.join('|'), h: q, a: nd.actions.map((x) => x.k), to: nd.actions.map((x) => x.to || 0), lab: nd.actions.map((x) => label(nd, x, cfg)), prob: +prob.toPrecision(4), s: b64(S8), r: b64(R8), e: b64(new Uint8Array(E16.buffer)) });
  for (let a = 0; a < na; a++) {
    let num = 0; for (let c = 0; c < NC; c++) num += SIZE[c] * reach[q][c] * strat[c][a];
    const pa = num / den, p2 = prob * pa; if (p2 < MINP) continue;
    walk(nd.children[a], reach.map((r, p) => (p === q ? Float64Array.from(r, (x, c) => x * strat[c][a]) : r)), path.concat([nd.actions[a].k]), p2);
  }
}
walk(0, Array.from({ length: nP }, () => new Float64Array(NC).fill(1)), [], 1);
fs.writeFileSync(OUT, JSON.stringify({ file: file.split('/').pop(), names: cfg.names, stacks: cfg.stacks, ante: cfg.ante, iters: s.iters, secs: Math.round((Date.now() - t0) / 1000), nodes }));
console.log('saved', OUT, nodes.length, 'nodes', Math.round(fs.statSync(OUT).size / 1024), 'KB, iters', s.iters, 'in', Math.round((Date.now() - t0) / 1000), 's');
