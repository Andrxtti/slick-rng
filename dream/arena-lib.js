// ================================================================ ARENA: hands from the pre-solved library
// Same library as The Dream: every hand is a real decision from a long offline solve, dealt instantly,
// with no solving on the phone. Each round of a fight gets harder.
let AR_COMBOS = null;
function arCombosOf(cls) {
  if (!AR_COMBOS) { AR_COMBOS = Array.from({ length: 169 }, () => []); for (let a = 1; a < 52; a++) for (let b = 0; b < a; b++) AR_COMBOS[Engine.CLASS_OF[Engine.comboIndex(a, b)]].push([a, b]); }
  return AR_COMBOS[cls];
}
function arLibTable(run) {
  const gid = run.tables[run.t], pool = typeof drLib === 'function' && drLib()[gid]; if (!pool || !pool.length) return null;
  run.usedT = run.usedT || []; const fresh = pool.filter((t) => !run.usedT.includes(t)), list = fresh.length ? fresh : pool;
  const t = list[(run.rng() * list.length) | 0]; run.usedT.push(t); return t;
}
function arLibDeal(run) {
  const lib = run.lib, rng = run.rng, seen = (run.seenN = run.seenN || new Set());
  let list = lib.n.map((n, i) => ({ n, i })).filter((x) => !seen.has(x.i)); if (!list.length) list = lib.n.map((n, i) => ({ n, i }));
  const tot = list.reduce((a, x) => a + Math.sqrt(x.n.w), 0); let x = rng() * tot, pick = list[0];
  for (const it of list) { x -= Math.sqrt(it.n.w); if (x < 0) { pick = it; break; } }
  seen.add(pick.i);
  const node = pick.n, d = Math.min(1, 0.3 + 0.17 * run.t);
  // half the time the right play is to fold, half the time to play
  const fi = node.a.indexOf('fold'), isFold = (r) => r[1].indexOf(Math.max(...r[1])) === fi;
  const folds = node.r.filter(isFold), plays = node.r.filter((r) => !isFold(r)), want = rng() < 0.5 ? plays : folds, rows = want.length >= 2 ? want : node.r;
  const L = rows.length, start = Math.round((1 - d) * 0.55 * L), width = Math.max(2, Math.round(0.45 * L));
  const win = rows.slice(Math.max(0, Math.min(start, L - 2)), Math.min(L, start + width)), [cls, strat] = win[(rng() * win.length) | 0];
  const combos = arCombosOf(cls), [a, b] = combos[(rng() * combos.length) | 0];
  const deck = Array.from({ length: 52 }, (_, i) => i).filter((c) => c !== a && c !== b);
  for (let i = deck.length - 1; i > 0; i--) { const j = (rng() * (i + 1)) | 0; [deck[i], deck[j]] = [deck[j], deck[i]]; }
  const seats = run.libSeats, hands = seats.map((_, i) => (i === node.h ? [a, b] : [deck[2 * i], deck[2 * i + 1]]));
  const st = []; st[cls] = strat;
  return { seats, hands, hero: node.h, path: node.p.map((k) => ({ k })), res: { actions: node.a.map((k) => ({ k })), strat: st }, cls };
}
