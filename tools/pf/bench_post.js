const E = require('../../app/engine.js');
const spot = E.makeSpot({ players: [
  { name: 'BB', stack: 37.8, range: '22+,A2+,K2s+,K5o+,Q2s+,Q8o+,J4s+,J8o+,T6s+,T8o+,96s+,98o,85s+,87o,74s+,76o,64s+,53s+,43s' },
  { name: 'BTN', stack: 37.8, range: '22+,A2s+,A4o+,K2s+,K9o+,Q5s+,Q9o+,J7s+,J9o+,T7s+,T9o,97s+,86s+,76s,65s,54s' } ],
  board: process.argv[2] || 'Ks7d2c', pot: 5.5, sizes: { flop: { bet: '33%, 75%', raise: '3x' }, turn: { bet: '66%', raise: '3x' }, river: { bet: '75%, a', raise: 'a' } } });
console.log('wasm', !!E.VCFR_WASM);
const sv = E.VCFR_WASM ? new E.VecSolverW(spot, E.VCFR_WASM, { memLimit: 2e9 }) : new E.VecSolver(spot, { memLimit: 2e9 });
console.log('nodes', sv.nodes.length);
const t0 = Date.now();
for (let k = 0; k < 6; k++) { sv.run(5000); const ex = sv.exploitability ? sv.exploitability() : null; console.log(((Date.now() - t0) / 1000).toFixed(0) + 's', 'iters', sv.iters, 'exploit', ex && JSON.stringify(ex).slice(0, 120)); }
