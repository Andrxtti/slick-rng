// Proves each model setting reaches the solver's own terminal valuation: builds the real PF2 tree for a table, takes one
// heads-up 3-bet-pot flop terminal (BB 3-bets the button, button calls) and prints R, gamma and the pot share of a few
// hands vs one opponent class, through PF2.prepTerminal + PF2.termVec2 (the code the solves run).
//   node probe_model.js [cfg=cfgs_full/m8-d100.json] [hands=76s,JJ,A9o,K4s,AQo] [vs=AQs]
// Run it under different env (AGGRE, PLAYREAL, PLAYPOT ...): a setting that does not move these numbers is dead.
const fs = require('fs');
const S = require('./solver2.js'), E = S.E;
const [cfgFile = 'cfgs_full/m8-d100.json', handsStr = '76s,JJ,A9o,K4s,AQo', vs = 'AQs'] = process.argv.slice(2);
const D = JSON.parse(fs.readFileSync(cfgFile, 'utf8')); const cfg = { ...D.cfg }; delete cfg.allowLimps; delete cfg.leave1; delete cfg.lite; cfg.maxNodes = 900000;
const s = new S.PF2(cfg), nm = cfg.names, BTN = nm.indexOf('BTN'), BB = nm.indexOf('BB');
const t = s.nodes.find((n) => n.kind === 'flop' && n.level === 2 && n.alive.length === 2 && n.alive.includes(BTN) && n.alive.includes(BB) && n.aggr === BB);
if (!t) { console.log('no BB-3-bet-vs-BTN flop terminal in this tree'); process.exit(1); }
const IDX = {}; E.CLASS_NAMES.forEach((x, i) => (IDX[x] = i));
s.cw = new Float64Array(169).fill(1); s.cls = new Int32Array(s.n);
const share = (p, o, c) => { s.cls[o] = IDX[vs]; s.cls[p] = c; const out = new Float64Array(169); s.termVec2(t, p, o, out); return (out[c] + t.put[p]) / t.pot; };
console.log(`env AGGRE=${process.env.AGGRE ?? '(default)'} PLAYREAL=${process.env.PLAYREAL ?? '(default)'} PLAYPOT=${process.env.PLAYPOT ?? '(default)'} | cfg.aggRe ${s.cfg.aggRe}`);
console.log(`terminal: level ${t.level} aggr ${nm[t.aggr]} pot ${t.pot.toFixed(2)} R BB ${t.R[BB].toFixed(3)} BTN ${t.R[BTN].toFixed(3)} gamma ${t.gamma.toFixed(3)} ramp ${t.ramp}`);
for (const h of handsStr.split(',')) console.log(`  ${h.padEnd(4)} vs ${vs}: BB share ${share(BB, BTN, IDX[h]).toFixed(4)}  BTN share ${share(BTN, BB, IDX[h]).toFixed(4)}  REAL_MULT[2] ${E.REAL_MULT[2][IDX[h]].toFixed(3)}  PLAY_RE ${E.PLAY_RE[IDX[h]].toFixed(3)}`);
