// Exact heads-up push/fold Nash (class level, exact card-removal weights) vs the CFR solver on the same game.
const { PF2, E } = require('./solver2.js');
const S = +(process.argv[2] || 10), SECS = +(process.argv[3] || 30);
const NC = 169, EQ = E.EQ169, CO = E.CLASS_OF, A = E.COMBO_A, B = E.COMBO_B, NAMES = E.CLASS_NAMES;
// W[c][o]: compatible combo pairs
const byC = Array.from({ length: NC }, () => []); for (let i = 0; i < E.NUM_COMBOS; i++) byC[CO[i]].push(i);
const W = new Float64Array(NC * NC);
for (let c = 0; c < NC; c++) for (const x of byC[c]) for (let o = 0; o < NC; o++) for (const y of byC[o]) { if (A[x] === A[y] || A[x] === B[y] || B[x] === A[y] || B[x] === B[y]) continue; W[c * NC + o]++; }
// fictitious play
let jam = new Float64Array(NC).fill(0.5), call = new Float64Array(NC).fill(0.5);
const avgJ = new Float64Array(NC), avgC = new Float64Array(NC);
for (let it = 1; it <= 4000; it++) {
  const bj = new Float64Array(NC), bc = new Float64Array(NC);
  for (let c = 0; c < NC; c++) { // SB best response
    let ev = 0, w = 0; for (let o = 0; o < NC; o++) { const ww = W[c * NC + o]; if (!ww) continue; w += ww; const q = EQ[c * NC + o]; ev += ww * ((1 - call[o]) * 1 + call[o] * (q * 2 * S - S)); }
    bj[c] = ev / w > -0.5 ? 1 : 0;
  }
  for (let o = 0; o < NC; o++) { // BB best response facing a jam
    let ev = 0, w = 0; for (let c = 0; c < NC; c++) { const ww = W[c * NC + o] * jam[c]; if (!ww) continue; w += ww; ev += ww * (EQ[o * NC + c] * 2 * S - S); }
    bc[o] = w > 0 && ev / w > -1 ? 1 : 0;
  }
  const a = 1 / (it + 1); for (let c = 0; c < NC; c++) { jam[c] += a * (bj[c] - jam[c]); call[c] += a * (bc[c] - call[c]); }
}
const comb = (c) => byC[c].length, tot = 1326;
const pct = (v) => (100 * v.reduce((s, x, c) => s + x * comb(c), 0) / tot).toFixed(1);
console.log(`EXACT ${S}bb HU push/fold: SB jams ${pct(jam)}%  BB calls ${pct(call)}%`);
// CFR solver on the same game
const cfg = { names: ['SB', 'BB'], stacks: [S, S], posts: [0.5, 1], ante: 0, hu: true, payoff: { type: 'chips' }, postOrder: [1, 0], pushFold: true };
const sv = new PF2(cfg); sv.run(SECS * 1000);
const root = 0, bbNode = sv.nodes[sv.nodes[0].children[sv.nodes[0].actions.findIndex((a) => a.k === 'allin')]];
const bbId = sv.nodes[0].children[sv.nodes[0].actions.findIndex((a) => a.k === 'allin')];
const sj = new Float64Array(NC), bcall = new Float64Array(NC);
for (let c = 0; c < NC; c++) { sj[c] = sv.avg(0, c)[sv.nodes[0].actions.findIndex((a) => a.k === 'allin')]; bcall[c] = sv.avg(bbId, c)[sv.nodes[bbId].actions.findIndex((a) => a.k === 'call')]; }
console.log(`CFR   (${SECS}s, ${sv.iters} iters): SB jams ${pct(sj)}%  BB calls ${pct(bcall)}%`);
let worst = [];
for (let c = 0; c < NC; c++) { worst.push([Math.abs(sj[c] - jam[c]), 'SB ' + NAMES[c], jam[c].toFixed(2), sj[c].toFixed(2)]); worst.push([Math.abs(bcall[c] - call[c]), 'BB ' + NAMES[c], call[c].toFixed(2), bcall[c].toFixed(2)]); }
worst.sort((x, y) => y[0] - x[0]);
console.log('biggest differences (exact vs cfr):'); worst.slice(0, 12).forEach((w) => console.log('  ', w[1], w[2], w[3]));
console.log('hands off by >0.25:', worst.filter((w) => w[0] > 0.25).length);
