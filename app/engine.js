// Slick RNG engine (JavaScript port of the Rust engine).
// Multiway Monte Carlo CFR: external sampling, regret-matching+, quadratic averaging.
// Runs inside a Web Worker (browser) or under Node for tests.
'use strict';

// ---------------------------------------------------------------- cards
const RANKS = '23456789TJQKA', SUITS = 'cdhs';
const NUM_COMBOS = 1326;
const COMBO_A = new Uint8Array(NUM_COMBOS), COMBO_B = new Uint8Array(NUM_COMBOS);
(function () { let i = 0; for (let a = 0; a < 52; a++) for (let b = 0; b < a; b++) { COMBO_A[i] = a; COMBO_B[i] = b; i++; } })();
function comboIndex(a, b) { if (a < b) { const t = a; a = b; b = t; } return (a * (a - 1)) / 2 + b; }
function parseCard(s) {
  const r = RANKS.indexOf(s[0].toUpperCase()), su = SUITS.indexOf(s[1].toLowerCase());
  if (r < 0 || su < 0 || s.length !== 2) throw new Error(`bad card "${s}"`);
  return r * 4 + su;
}
function parseCards(s) {
  const t = s.replace(/[\s,]/g, '');
  if (t.length % 2) throw new Error(`bad board "${s}"`);
  const out = [];
  for (let i = 0; i < t.length; i += 2) { const c = parseCard(t.slice(i, i + 2)); if (out.includes(c)) throw new Error('duplicate card on board'); out.push(c); }
  return out;
}
function cardStr(c) { return RANKS[c >> 2] + SUITS[c & 3]; }
function comboStr(i) { return cardStr(COMBO_A[i]) + cardStr(COMBO_B[i]); }
function comboClass(i) {
  const a = COMBO_A[i], b = COMBO_B[i], ra = Math.max(a >> 2, b >> 2), rb = Math.min(a >> 2, b >> 2);
  if (ra === rb) return RANKS[ra] + RANKS[rb];
  return RANKS[ra] + RANKS[rb] + ((a & 3) === (b & 3) ? 's' : 'o');
}

// ---------------------------------------------------------------- evaluator
// returns category<<20 | kickers; higher is better
function topBits(mask, n) {
  let out = 0, m = mask, k = 0;
  while (k < n && m) { const r = 31 - Math.clz32(m); out = (out << 4) | r; m &= ~(1 << r); k++; }
  return out << (4 * (5 - n));
}
function straightHigh(mask) {
  const m = (mask << 1) | ((mask >> 12) & 1);
  const s = m & (m >> 1) & (m >> 2) & (m >> 3) & (m >> 4);
  return s ? (31 - Math.clz32(s)) + 3 : -1;
}
const _cnt = new Uint8Array(13), _sm = new Int32Array(4);
function evaluate(cards, n) {
  _cnt.fill(0); _sm.fill(0); let all = 0;
  for (let i = 0; i < n; i++) { const c = cards[i], r = c >> 2; _sm[c & 3] |= 1 << r; _cnt[r]++; all |= 1 << r; }
  for (let s = 0; s < 4; s++) {
    const sm = _sm[s];
    let bits = sm; bits = bits - ((bits >> 1) & 0x55555555); bits = (bits & 0x33333333) + ((bits >> 2) & 0x33333333);
    const pc = (((bits + (bits >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
    if (pc >= 5) { const h = straightHigh(sm); return h >= 0 ? (8 << 20) | (h << 16) : (5 << 20) | topBits(sm, 5); }
  }
  let quads = 0, trips = 0, pairs = 0, npairs = 0;
  for (let r = 0; r < 13; r++) { const c = _cnt[r]; if (c === 4) quads |= 1 << r; else if (c === 3) trips |= 1 << r; else if (c === 2) { pairs |= 1 << r; npairs++; } }
  if (quads) { const q = 31 - Math.clz32(quads); return (7 << 20) | (q << 16) | (topBits(all & ~(1 << q), 1) >> 12); }
  if (trips) {
    const t = 31 - Math.clz32(trips), rp = (pairs | trips) & ~(1 << t);
    if (rp) return (6 << 20) | (t << 16) | ((31 - Math.clz32(rp)) << 12);
  }
  const sh = straightHigh(all);
  if (sh >= 0) return (4 << 20) | (sh << 16);
  if (trips) { const t = 31 - Math.clz32(trips); return (3 << 20) | (t << 16) | (topBits(all & ~(1 << t), 2) >> 8); }
  if (npairs >= 2) {
    const p1 = 31 - Math.clz32(pairs), p2 = 31 - Math.clz32(pairs & ~(1 << p1));
    return (2 << 20) | (p1 << 16) | (p2 << 12) | (topBits(all & ~(1 << p1) & ~(1 << p2), 1) >> 8);
  }
  if (pairs) { const p = 31 - Math.clz32(pairs); return (1 << 20) | (p << 16) | (topBits(all & ~(1 << p), 3) >> 4); }
  return topBits(all, 5);
}

// ---------------------------------------------------------------- ranges
function classCombos(hi, lo, kind) {
  const v = [];
  for (let s1 = 0; s1 < 4; s1++) for (let s2 = 0; s2 < 4; s2++) {
    const a = hi * 4 + s1, b = lo * 4 + s2; if (a === b) continue;
    const ok = kind === 'p' ? (hi === lo && s1 < s2) : kind === 's' ? (hi !== lo && s1 === s2) : kind === 'o' ? (hi !== lo && s1 !== s2) : (hi !== lo || s1 < s2);
    if (ok) v.push(comboIndex(a, b));
  }
  return v;
}
function parseClass(t) {
  if (t.length < 2 || t.length > 3) throw new Error(`bad hand "${t}"`);
  const r1 = RANKS.indexOf(t[0].toUpperCase()), r2 = RANKS.indexOf(t[1].toUpperCase());
  if (r1 < 0 || r2 < 0) throw new Error(`bad hand "${t}"`);
  const hi = Math.max(r1, r2), lo = Math.min(r1, r2);
  let kind;
  if (hi === lo) { if (t.length === 3) throw new Error(`bad pair "${t}"`); kind = 'p'; }
  else if (t.length === 3) { kind = t[2].toLowerCase(); if (kind !== 's' && kind !== 'o') throw new Error(`bad hand "${t}"`); }
  else kind = 'a';
  return [hi, lo, kind];
}
function expandToken(t) {
  if (t.length === 4 && /^[2-9TJQKA][cdhs][2-9TJQKA][cdhs]$/i.test(t)) {
    const a = parseCard(t.slice(0, 2)), b = parseCard(t.slice(2)); if (a === b) throw new Error(`bad combo "${t}"`);
    return [comboIndex(a, b)];
  }
  const out = [];
  if (t.endsWith('+')) {
    const [hi, lo, kind] = parseClass(t.slice(0, -1));
    if (kind === 'p') for (let r = hi; r < 13; r++) out.push(...classCombos(r, r, 'p'));
    else for (let r = lo; r < hi; r++) out.push(...classCombos(hi, r, kind));
  } else if (t.includes('-')) {
    const [a, b] = t.split('-'); const [h1, l1, k1] = parseClass(a), [h2, l2, k2] = parseClass(b);
    if (k1 !== k2) throw new Error(`bad span "${t}"`);
    if (k1 === 'p') for (let r = Math.min(h1, h2); r <= Math.max(h1, h2); r++) out.push(...classCombos(r, r, 'p'));
    else { if (h1 !== h2) throw new Error(`span must keep its top card: "${t}"`); for (let r = Math.min(l1, l2); r <= Math.max(l1, l2); r++) out.push(...classCombos(h1, r, k1)); }
  } else { const [hi, lo, kind] = parseClass(t); out.push(...classCombos(hi, lo, kind)); }
  return out;
}
function parseRange(s) {
  const w = new Float32Array(NUM_COMBOS);
  for (const raw of s.split(',')) {
    const tok = raw.trim(); if (!tok) continue;
    let hand = tok, weight = 1;
    if (tok.includes(':')) { const [h, x] = tok.split(':'); hand = h.trim(); weight = parseFloat(x); if (!(weight >= 0 && weight <= 1)) throw new Error(`bad weight in "${tok}"`); }
    for (const c of expandToken(hand)) w[c] = weight;
  }
  return w;
}

// ---------------------------------------------------------------- tree
// sizes: "33%, 75%, a" (bets) / "3x, 50%, a" (raises)
function parseSizes(s, isRaise) {
  const v = [];
  for (let t of (s || '').split(',')) {
    t = t.trim().toLowerCase(); if (!t) continue;
    if (t === 'a' || t === 'allin' || t === 'all-in') v.push({ k: 'a' });
    else if (t.endsWith('%')) { const x = parseFloat(t); if (!(x > 0)) throw new Error(`bad size "${t}"`); v.push({ k: 'p', x: x / 100 }); }
    else if (t.endsWith('x')) { if (!isRaise) throw new Error(`"${t}": x-sizes are for raises`); const x = parseFloat(t); if (!(x > 1)) throw new Error(`bad size "${t}"`); v.push({ k: 'm', x }); }
    else throw new Error(`bad size "${t}" (use 50%, 3x or a)`);
  }
  return v;
}

const K_ACTION = 0, K_CHANCE = 1, K_FOLD = 2, K_SHOW = 3;

function buildTree(cfg) {
  const n = cfg.stacks.length, nodes = [];
  const canAct = (st, q) => !st.folded[q] && !st.allin[q];
  const nextActor = (st, p) => { for (let k = 1; k <= n; k++) { const q = (p + k) % n; if (canAct(st, q) && st.needs[q]) return q; } return -1; };
  const active = (st) => st.folded.reduce((a, f) => a + (f ? 0 : 1), 0);
  const clone = (st) => ({ street: st.street, pot: st.pot, sb: st.sb.slice(), inv: st.inv.slice(), folded: st.folded.slice(), allin: st.allin.slice(), needs: st.needs.slice(), toAct: st.toAct, raises: st.raises, lastRaise: st.lastRaise });
  const push = (st, node) => { node.street = st.street; node.inv = Float64Array.from(st.inv); node.folded = st.folded.slice(); node.pot = st.pot; nodes.push(node); if (nodes.length > 3e6) throw new Error('Tree too large. Use fewer bet sizes or a lower raise cap.'); return nodes.length - 1; };

  function pushSized(acts, targets, allinTo, pot) {
    targets.sort((a, b) => a - b);
    const chosen = []; let hasAllin = false;
    for (let t of targets) {
      t = Math.round(t * 100) / 100;
      if (t >= allinTo * (1 - 1e-9)) { hasAllin = true; continue; }
      if (chosen.length && (t - chosen[chosen.length - 1]) / Math.max(pot, 1e-9) < cfg.mergeThreshold) continue;
      chosen.push(t);
    }
    if (chosen.length) { const last = chosen[chosen.length - 1]; if (allinTo - last <= cfg.allinThreshold * (pot + 2 * last)) hasAllin = true; }
    for (const t of chosen) acts.push({ t: 'bet', to: t });
    if (hasAllin) acts.push({ t: 'allin', to: allinTo });
  }

  function genActions(st, p) {
    const maxBet = Math.max(...st.sb), toCall = maxBet - st.sb[p], rem = cfg.stacks[p] - st.inv[p], allinTo = st.sb[p] + rem;
    const sizes = cfg.sizes[st.street], acts = [];
    let others = false; for (let q = 0; q < n; q++) if (q !== p && canAct(st, q)) others = true;
    if (toCall > 1e-9) {
      acts.push({ t: 'fold' });
      if (rem <= toCall + 1e-9) { acts.push({ t: 'allin', to: allinTo }); return acts; }
      acts.push({ t: 'call' });
      if (!others || st.raises >= cfg.raiseCap) return acts;
      const potAfter = st.pot + toCall, minTo = maxBet + Math.max(st.lastRaise, toCall);
      const targets = sizes.raises.map((s) => Math.max(s.k === 'p' ? maxBet + s.x * potAfter : s.k === 'm' ? maxBet * s.x : allinTo, minTo));
      pushSized(acts, targets, allinTo, st.pot + toCall);
    } else {
      acts.push({ t: 'check' });
      if (!others) return acts;
      const targets = sizes.bets.filter((s) => s.k !== 'm').map((s) => Math.max(s.k === 'p' ? s.x * st.pot : allinTo, Math.min(1, allinTo)));
      pushSized(acts, targets, allinTo, st.pot);
    }
    return acts;
  }

  function apply(st, p, a) {
    const s = clone(st), maxBet = Math.max(...st.sb);
    s.needs[p] = false;
    const putTo = (total) => { const add = total - s.sb[p]; s.sb[p] = total; s.inv[p] += add; s.pot += add; if (cfg.stacks[p] - s.inv[p] <= 1e-9) s.allin[p] = true; };
    if (a.t === 'fold') s.folded[p] = true;
    else if (a.t === 'call') putTo(maxBet);
    else if (a.t === 'bet' || a.t === 'allin') {
      putTo(a.to);
      if (a.to > maxBet + 1e-9) {
        const raise = a.to - maxBet, full = raise >= Math.max(st.lastRaise, 1e-9) - 1e-9;
        if (maxBet > 0) s.raises++;
        if (full || maxBet === 0) s.lastRaise = raise;
        for (let q = 0; q < n; q++) if (q !== p && !s.folded[q] && !s.allin[q]) s.needs[q] = true;
      }
    }
    s.toAct = p;
    return s;
  }

  function afterAction(st) {
    if (active(st) === 1) return push(st, { kind: K_FOLD });
    const q = nextActor(st, st.toAct);
    if (q >= 0) { st.toAct = q; return build(st); }
    let canBet = 0; for (let i = 0; i < n; i++) if (canAct(st, i)) canBet++;
    if (st.street === 2 || canBet <= 1) return push(st, { kind: K_SHOW });
    const s = clone(st); s.street++; s.sb.fill(0); s.raises = 0; s.lastRaise = 0;
    for (let i = 0; i < n; i++) s.needs[i] = !s.folded[i] && !s.allin[i];
    s.toAct = nextActor(s, n - 1);
    const id = push(s, { kind: K_CHANCE, child: -1 });
    nodes[id].child = build(s);
    return id;
  }

  function build(st) {
    if (active(st) === 1) return push(st, { kind: K_FOLD });
    const p = st.toAct, acts = genActions(st, p);
    const id = push(st, { kind: K_ACTION, player: p, actions: acts, children: [] });
    const ch = acts.map((a) => afterAction(apply(st, p, a)));
    nodes[id].children = ch;
    return id;
  }

  const st0 = { street: cfg.startStreet, pot: cfg.pot, sb: new Array(n).fill(0), inv: new Array(n).fill(0), folded: new Array(n).fill(false), allin: cfg.stacks.map((s) => s <= 0), needs: new Array(n).fill(true), toAct: 0, raises: 0, lastRaise: 0 };
  st0.toAct = nextActor(st0, n - 1);
  let canStart = 0; for (let i = 0; i < n; i++) if (!st0.allin[i]) canStart++;
  if (canStart <= 1 || st0.toAct < 0) push(st0, { kind: K_SHOW }); // all-in before the flop: just run it out
  else build(st0);
  // precompute side pots for terminals
  for (const nd of nodes) {
    if (nd.kind === K_FOLD) nd.winner = nd.folded.indexOf(false);
    if (nd.kind === K_SHOW) {
      const alive = []; for (let i = 0; i < n; i++) if (!nd.folded[i]) alive.push(i);
      const levels = [...new Set(alive.map((i) => Math.round(nd.inv[i] * 1e6) / 1e6))].sort((a, b) => a - b);
      let prev = 0, dist = 0; nd.layers = [];
      levels.forEach((lv, li) => {
        let amt = 0; for (let i = 0; i < n; i++) amt += Math.min(nd.inv[i], lv) - Math.min(nd.inv[i], prev);
        if (li === 0) amt += cfg.pot;
        if (li === levels.length - 1) amt = nd.pot - dist;
        nd.layers.push({ amount: amt, elig: alive.filter((i) => nd.inv[i] >= lv - 1e-9) });
        dist += amt; prev = lv;
      });
    }
  }
  return nodes;
}

function actionLabel(a) {
  const f = (x) => (Math.abs(x - Math.round(x)) < 1e-6 ? String(Math.round(x)) : x.toFixed(1));
  return a.t === 'fold' ? 'Fold' : a.t === 'check' ? 'Check' : a.t === 'call' ? 'Call' : a.t === 'bet' ? `Bet ${f(a.to)}` : `All-in ${f(a.to)}`;
}

// ---------------------------------------------------------------- ICM
function icmEquity(stacks, start, payouts) {
  const n = stacks.length, eq = new Float64Array(n);
  const alive = [], busted = [];
  for (let i = 0; i < n; i++) (stacks[i] > 1e-9 ? alive : busted).push(i);
  busted.sort((x, y) => start[y] - start[x]);
  busted.forEach((i, k) => { const pl = alive.length + k; if (pl < payouts.length) eq[i] = payouts[pl]; });
  const a = alive.length, paid = Math.min(payouts.length, a);
  if (!a || !paid) return eq;
  const total = alive.reduce((s, i) => s + stacks[i], 0), size = 1 << a;
  const prob = new Float64Array(size), sumS = new Float64Array(size), pc = new Uint8Array(size);
  for (let m = 1; m < size; m++) { const low = 31 - Math.clz32(m & -m); sumS[m] = sumS[m & (m - 1)] + stacks[alive[low]]; pc[m] = pc[m & (m - 1)] + 1; }
  prob[0] = 1;
  for (let m = 0; m < size; m++) {
    const p = prob[m]; if (p === 0 || pc[m] >= paid) continue;
    const rem = total - sumS[m]; if (rem <= 0) continue;
    for (let j = 0; j < a; j++) { if (m & (1 << j)) continue; const pj = (p * stacks[alive[j]]) / rem; eq[alive[j]] += pj * payouts[pc[m]]; prob[m | (1 << j)] += pj; }
  }
  return eq;
}

// Large fields: Malmuth-Harville finishing orders sampled with exponential races
// (player j's key = Exp(1) / stack; sorting keys gives an exact MH order). The same
// samples are reused for every evaluation, so equity differences between outcomes are
// smooth and consistent for the solver. Only the stacks of players in the hand change.
class IcmModel {
  constructor(nIn, others, payouts, samples) {
    this.nIn = nIn; this.payouts = payouts; this.paid = payouts.length; this.samples = samples;
    const nO = others.length;
    this.eIn = new Float64Array(samples * nIn);
    this.oKeys = []; // sorted keys of the other players, per sample
    let seed = 0x9e3779b9;
    const rnd = () => { seed ^= seed << 13; seed >>>= 0; seed ^= seed >>> 17; seed ^= seed << 5; seed >>>= 0; return (seed + 0.5) / 4294967296; };
    for (let k = 0; k < samples; k++) {
      for (let i = 0; i < nIn; i++) this.eIn[k * nIn + i] = -Math.log(rnd());
      const ks = new Float64Array(nO);
      for (let j = 0; j < nO; j++) ks[j] = -Math.log(rnd()) / others[j];
      ks.sort(); this.oKeys.push(ks);
    }
    this.nOthers = nO;
  }
  // fin: final stacks of the players in the hand; start: their stacks before the hand
  equity(fin, start) {
    const n = this.nIn, eq = new Float64Array(n), pay = this.payouts, paid = this.paid;
    const alive = [], busted = [];
    for (let i = 0; i < n; i++) (fin[i] > 1e-9 ? alive : busted).push(i);
    const aliveTotal = this.nOthers + alive.length;
    busted.sort((x, y) => start[y] - start[x]).forEach((i, k) => { const pl = aliveTotal + k; if (pl < paid) eq[i] = pay[pl]; });
    if (!alive.length) return eq;
    const key = new Float64Array(n);
    for (let k = 0; k < this.samples; k++) {
      const ok = this.oKeys[k], base = k * n;
      for (const i of alive) key[i] = this.eIn[base + i] / fin[i];
      for (const i of alive) {
        const ki = key[i];
        let lo = 0, hi = ok.length; while (lo < hi) { const m = (lo + hi) >> 1; if (ok[m] < ki) lo = m + 1; else hi = m; }
        let place = lo; for (const j of alive) if (j !== i && key[j] < ki) place++;
        if (place < paid) eq[i] += pay[place];
      }
    }
    for (const i of alive) eq[i] /= this.samples;
    return eq;
  }
}

// ---------------------------------------------------------------- abstraction
const VOL_LEVELS = 4, VOL_CUTS = [0.06, 0.13, 0.21];
function riverPercentiles(board5) {
  const arr = [], cards = new Uint8Array(7); for (let i = 0; i < 5; i++) cards[i] = board5[i];
  for (let c = 0; c < NUM_COMBOS; c++) {
    const a = COMBO_A[c], b = COMBO_B[c]; if (board5.includes(a) || board5.includes(b)) continue;
    cards[5] = a; cards[6] = b; arr.push(evaluate(cards, 7) * 2048 + c);
  }
  arr.sort((x, y) => x - y);
  const out = new Float32Array(NUM_COMBOS).fill(-1), n = arr.length;
  let i = 0;
  while (i < n) {
    const s = Math.floor(arr[i] / 2048); let j = i;
    while (j < n && Math.floor(arr[j] / 2048) === s) j++;
    const pct = (i + (j - i) * 0.5) / n;
    for (let k = i; k < j; k++) out[arr[k] % 2048] = pct;
    i = j;
  }
  return out;
}
function boardKey(extra) { const v = extra.slice().sort((a, b) => a - b); let k = 0; for (const c of v) k = k * 53 + c + 1; return k; }
function quantize(pct, B) { const o = new Uint16Array(NUM_COMBOS); for (let i = 0; i < NUM_COMBOS; i++) o[i] = pct[i] < 0 ? 65535 : Math.min(B - 1, Math.floor(pct[i] * B)); return o; }
function buildAbstraction(board, turnB, riverB) {
  const abs = { turnB, riverB, river: new Map(), turn: new Map() };
  const deck = []; for (let c = 0; c < 52; c++) if (!board.includes(c)) deck.push(c);
  if (board.length === 3 && (turnB > 0 || riverB > 0)) {
    const rp = new Map();
    for (let i = 0; i < deck.length; i++) for (let j = i + 1; j < deck.length; j++) {
      const t = deck[i], r = deck[j];
      rp.set(boardKey([t, r]), riverPercentiles([board[0], board[1], board[2], t, r]));
    }
    if (riverB > 0) for (const [k, p] of rp) abs.river.set(k, quantize(p, riverB));
    if (turnB > 0) for (const t of deck) {
      const sum = new Float64Array(NUM_COMBOS), sq = new Float64Array(NUM_COMBOS), cnt = new Uint16Array(NUM_COMBOS);
      for (const r of deck) { if (r === t) continue; const p = rp.get(boardKey([t, r])); for (let c = 0; c < NUM_COMBOS; c++) { const x = p[c]; if (x >= 0) { sum[c] += x; sq[c] += x * x; cnt[c]++; } } }
      const b = new Uint16Array(NUM_COMBOS).fill(65535);
      for (let c = 0; c < NUM_COMBOS; c++) {
        if (!cnt[c] || COMBO_A[c] === t || COMBO_B[c] === t) continue;
        const m = sum[c] / cnt[c], sd = Math.sqrt(Math.max(0, sq[c] / cnt[c] - m * m));
        const h = Math.min(turnB - 1, Math.floor(m * turnB)); let v = 0; for (const cut of VOL_CUTS) if (sd > cut) v++;
        b[c] = h * VOL_LEVELS + v;
      }
      abs.turn.set(t, b);
    }
  } else if (board.length === 4 && riverB > 0) {
    for (const r of deck) abs.river.set(boardKey([r]), quantize(riverPercentiles([...board, r]), riverB));
  }
  return abs;
}

// ---------------------------------------------------------------- solver
const KEY_MUL = 4194304; // 2^22
class Solver {
  constructor(spot) {
    this.spot = spot;
    this.n = spot.stacks.length;
    this.board = spot.board;
    this.start = spot.board.length - 3;
    this.nodes = buildTree({ stacks: spot.stacks, pot: spot.pot, startStreet: this.start, sizes: spot.sizes, raiseCap: spot.raiseCap, allinThreshold: spot.allinThreshold, mergeThreshold: spot.mergeThreshold });
    this.ranges = spot.ranges;
    this.pr = this.ranges.map((w, p) => {
      const combos = [], cum = []; let acc = 0;
      for (let c = 0; c < NUM_COMBOS; c++) if (w[c] > 0 && !this.board.includes(COMBO_A[c]) && !this.board.includes(COMBO_B[c])) { acc += w[c]; combos.push(c); cum.push(acc); }
      if (!combos.length) throw new Error(`${spot.names[p]}'s range is empty on this board.`);
      return { combos, cum: Float64Array.from(cum) };
    });
    this.abs = spot.turnBuckets > 0 || spot.riverBuckets > 0 ? buildAbstraction(this.board, spot.turnBuckets, spot.riverBuckets) : null;
    this.index = new Map();
    this.T = new Float32Array(1 << 20);
    this.used = 0;
    this.iters = 0;
    this.icmCache = new Map();
    this.sig = []; this.val = [];
    for (let d = 0; d < 200; d++) { this.sig.push(new Float64Array(16)); this.val.push(new Float64Array(16)); }
    // per-deal scratch
    this.combo = new Int32Array(this.n); this.strength = new Int32Array(this.n);
    this.key = []; for (let p = 0; p < this.n; p++) this.key.push(new Int32Array(3));
    this.full = new Uint8Array(5); this.used52 = new Uint8Array(52); this.c7 = new Uint8Array(7);
    this.decisionPoints = this.nodes.filter((x) => x.kind === K_ACTION).length;
    this.count = 0;
    for (const nd of this.nodes) {
      if (nd.kind !== K_ACTION) continue;
      if (nd.street === this.start) nd.keyRange = NUM_COMBOS;
      else if (nd.street === 1 && this.abs && this.abs.turnB > 0) nd.keyRange = this.abs.turnB * VOL_LEVELS;
      else if (nd.street === 2 && this.abs && this.abs.riverB > 0) nd.keyRange = this.abs.riverB;
      else nd.keyRange = 0;
    }
  }

  // dense per-node index when the key space is small (starting street, buckets)
  slotNode(nd, id, hk, na, create) {
    if (nd.keyRange) {
      let idx = nd.idx;
      if (!idx) { if (!create) return -1; idx = nd.idx = new Int32Array(nd.keyRange).fill(-1); }
      let o = idx[hk];
      if (o >= 0) return o;
      if (!create) return -1;
      o = this.alloc(na); idx[hk] = o; this.count++;
      return o;
    }
    return this.slot(id * KEY_MUL + hk, na, create);
  }

  alloc(na) {
    const o = this.used; this.used += 2 * na;
    if (this.used > this.T.length) {
      const bigger = new Float32Array(Math.max(this.T.length * 2, this.used));
      bigger.set(this.T); this.T = bigger;
    }
    return o;
  }

  slot(key, na, create) {
    let o = this.index.get(key);
    if (o !== undefined) return o;
    if (!create) return -1;
    o = this.alloc(na);
    this.index.set(key, o); this.count++;
    return o;
  }

  handKey(combo, street, full) {
    if (street === this.start) return combo;
    const abs = this.abs, flop = this.start === 0;
    if (street === 1) {
      if (abs && abs.turnB > 0) return abs.turn.get(full[3])[combo];
      return combo + NUM_COMBOS * (full[3] + 1);
    }
    if (abs && abs.riverB > 0) return abs.river.get(flop ? boardKey([full[3], full[4]]) : boardKey([full[4]]))[combo];
    const path = flop ? 53 + full[3] * 52 + full[4] : full[4] + 1;
    return combo + NUM_COMBOS * path;
  }

  sampleDeal() {
    const n = this.n, used = this.used52;
    for (;;) {
      used.fill(0); for (const c of this.board) used[c] = 1;
      let ok = true;
      for (let p = 0; p < n && ok; p++) {
        const pr = this.pr[p], cum = pr.cum, x = Math.random() * cum[cum.length - 1];
        let lo = 0, hi = cum.length - 1;
        while (lo < hi) { const mid = (lo + hi) >> 1; if (cum[mid] <= x) lo = mid + 1; else hi = mid; }
        const c = pr.combos[lo], a = COMBO_A[c], b = COMBO_B[c];
        if (used[a] || used[b]) { ok = false; break; }
        used[a] = used[b] = 1; this.combo[p] = c;
      }
      if (ok) break;
    }
    const full = this.full;
    for (let i = 0; i < this.board.length; i++) full[i] = this.board[i];
    for (let s = this.board.length; s < 5; s++) { let c; do c = (Math.random() * 52) | 0; while (used[c]); used[c] = 1; full[s] = c; }
    const c7 = this.c7; for (let i = 0; i < 5; i++) c7[i] = full[i];
    for (let p = 0; p < n; p++) {
      const c = this.combo[p]; c7[5] = COMBO_A[c]; c7[6] = COMBO_B[c];
      this.strength[p] = evaluate(c7, 7);
      for (let st = this.start; st < 3; st++) this.key[p][st] = this.handKey(c, st, full);
    }
  }

  // payoff for player p at a terminal node under the current deal
  utility(nd, p) {
    if (this.spot.payoff.type === 'icm') return this.icmUtility(nd)[p];
    let won = 0;
    if (nd.kind === K_FOLD) won = nd.winner === p ? nd.pot : 0;
    else for (const L of nd.layers) {
      if (!L.elig.includes(p)) continue;
      let best = -1, cnt = 0;
      for (const i of L.elig) { const s = this.strength[i]; if (s > best) { best = s; cnt = 1; } else if (s === best) cnt++; }
      if (this.strength[p] === best) won += L.amount / cnt;
    }
    return won - nd.inv[p];
  }

  finalStacks(nd) {
    const n = this.n, fin = new Float64Array(n);
    for (let i = 0; i < n; i++) fin[i] = this.spot.stacks[i] - nd.inv[i];
    if (nd.kind === K_FOLD) { fin[nd.winner] += nd.pot; return fin; }
    for (const L of nd.layers) {
      let best = -1; const w = [];
      for (const i of L.elig) { const s = this.strength[i]; if (s > best) { best = s; w.length = 0; w.push(i); } else if (s === best) w.push(i); }
      for (const i of w) fin[i] += L.amount / w.length;
    }
    return fin;
  }

  icmUtility(nd) {
    const fin = this.finalStacks(nd);
    let k = ''; for (let i = 0; i < fin.length; i++) k += Math.round(fin[i] * 1000) + ',';
    let v = this.icmCache.get(k);
    if (v) return v;
    const { payouts, others } = this.spot.payoff;
    if (this.n + others.length <= 14) {
      const all = [...fin, ...others], start = [...this.spot.stacks, ...others];
      v = icmEquity(all, start, payouts).slice(0, this.n);
    } else {
      if (!this.icmModel) this.icmModel = new IcmModel(this.n, others, payouts, 6000);
      v = this.icmModel.equity(fin, this.spot.stacks);
    }
    if (this.icmCache.size < 300000) this.icmCache.set(k, v);
    return v;
  }

  traverse(id, p, w, wa, depth) {
    const nd = this.nodes[id];
    if (nd.kind === K_FOLD || nd.kind === K_SHOW) return this.utility(nd, p);
    if (nd.kind === K_CHANCE) return this.traverse(nd.child, p, w, wa, depth);
    const q = nd.player, na = nd.actions.length, T = this.T;
    const off = this.slotNode(nd, id, this.key[q][nd.street], na, true);
    const sig = this.sig[depth];
    let sum = 0;
    for (let a = 0; a < na; a++) { const r = T[off + a]; const x = r > 0 ? r : 0; sig[a] = x; sum += x; }
    if (sum > 0) for (let a = 0; a < na; a++) sig[a] /= sum; else for (let a = 0; a < na; a++) sig[a] = 1 / na;
    if (q === p) {
      const val = this.val[depth]; let v = 0;
      for (let a = 0; a < na; a++) { val[a] = this.traverse(nd.children[a], p, w, wa, depth + 1); v += sig[a] * val[a]; }
      const T2 = this.T; // table may have grown during recursion
      for (let a = 0; a < na; a++) { const r = T2[off + a] + w * (val[a] - v); T2[off + a] = r > 0 ? r : 0; }
      return v;
    }
    for (let a = 0; a < na; a++) T[off + na + a] += wa * sig[a];
    let x = Math.random(), pick = na - 1;
    for (let a = 0; a < na; a++) { x -= sig[a]; if (x < 0) { pick = a; break; } }
    return this.traverse(nd.children[pick], p, w, wa, depth + 1);
  }

  run(ms) {
    const t0 = Date.now(); let k = 0;
    while (Date.now() - t0 < ms) {
      for (let j = 0; j < 50; j++) {
        const w = Math.floor(this.iters / 1000) + 1, wa = w * w;
        this.sampleDeal();
        for (let p = 0; p < this.n; p++) this.traverse(0, p, w, wa, 0);
        this.iters++; k++;
      }
    }
    return k;
  }

  avgStrategy(id, handKey, na) {
    const out = new Float64Array(na), off = this.slotNode(this.nodes[id], id, handKey, na, false);
    if (off < 0) { out.fill(1 / na); return out; }
    let s = 0; for (let a = 0; a < na; a++) { out[a] = this.T[off + na + a]; s += out[a]; }
    if (s > 0) { for (let a = 0; a < na; a++) out[a] /= s; return out; }
    for (let a = 0; a < na; a++) { const r = Math.max(0, this.T[off + a]); out[a] = r; s += r; }
    if (s > 0) for (let a = 0; a < na; a++) out[a] /= s; else out.fill(1 / na);
    return out;
  }

  estimateEV(samples) {
    const n = this.n, sum = new Float64Array(n), sq = new Float64Array(n);
    for (let i = 0; i < samples; i++) {
      this.sampleDeal();
      let id = 0;
      for (;;) {
        const nd = this.nodes[id];
        if (nd.kind === K_FOLD || nd.kind === K_SHOW) {
          for (let p = 0; p < n; p++) { const u = this.utility(nd, p); sum[p] += u; sq[p] += u * u; }
          break;
        }
        if (nd.kind === K_CHANCE) { id = nd.child; continue; }
        const s = this.avgStrategy(id, this.key[nd.player][nd.street], nd.actions.length);
        let x = Math.random(), pick = s.length - 1;
        for (let a = 0; a < s.length; a++) { x -= s[a]; if (x < 0) { pick = a; break; } }
        id = nd.children[pick];
      }
    }
    return Array.from({ length: n }, (_, p) => { const m = sum[p] / samples; return { ev: m, se: Math.sqrt(Math.max(0, sq[p] / samples - m * m) / samples) }; });
  }

  // path: array of {a: actionIndex} or {card: c}
  report(path) {
    let id = 0; const board = this.board.slice(); const steps = []; const history = [];
    for (const s of path) {
      const nd = this.nodes[id];
      if (nd.kind === K_ACTION) {
        if (s.a === undefined || s.a >= nd.children.length) throw new Error('Invalid action in path.');
        steps.push({ id, player: nd.player, street: nd.street, a: s.a, na: nd.actions.length });
        history.push({ type: 'action', player: nd.player, label: actionLabel(nd.actions[s.a]) });
        id = nd.children[s.a];
      } else if (nd.kind === K_CHANCE) {
        if (s.card === undefined || board.includes(s.card)) throw new Error('Pick a card that is not on the board.');
        board.push(s.card); history.push({ type: 'card', card: s.card });
        id = nd.child;
      } else throw new Error('The hand is over at this point.');
    }
    const nd = this.nodes[id];
    const base = { history, board: board.slice(), pot: nd.pot, street: nd.street };
    if (nd.kind === K_CHANCE) return { ...base, kind: 'chance', street: nd.street };
    if (nd.kind !== K_ACTION) return { ...base, kind: 'terminal', folded: nd.folded.slice() };
    const full = new Uint8Array(5); board.forEach((c, i) => (full[i] = c));
    const keyFor = (c, st) => this.handKey(c, st, full);
    const p = nd.player, na = nd.actions.length, combos = [];
    for (const c of this.pr[p].combos) {
      if (board.includes(COMBO_A[c]) || board.includes(COMBO_B[c])) continue;
      let reach = this.ranges[p][c];
      for (const s of steps) if (s.player === p) { reach *= this.avgStrategy(s.id, keyFor(c, s.street), s.na)[s.a]; }
      combos.push({ c, reach, s: Array.from(this.avgStrategy(id, keyFor(c, nd.street), na)) });
    }
    const total = combos.reduce((a, x) => a + x.reach, 0);
    const overall = new Array(na).fill(0);
    if (total > 0) for (const x of combos) for (let a = 0; a < na; a++) overall[a] += (x.reach * x.s[a]) / total;
    const classes = {};
    for (const x of combos) {
      const k = comboClass(x.c); const e = classes[k] || (classes[k] = { w: 0, s: new Array(na).fill(0), combos: [] });
      e.w += x.reach; for (let a = 0; a < na; a++) e.s[a] += x.reach * x.s[a];
      e.combos.push({ name: comboStr(x.c), reach: x.reach, s: x.s });
    }
    for (const k in classes) { const e = classes[k]; if (e.w > 0) for (let a = 0; a < na; a++) e.s[a] /= e.w; }
    return { ...base, kind: 'action', player: p, actions: nd.actions.map(actionLabel), actionTypes: nd.actions.map((a) => a.t), overall, classes, totalReach: total };
  }

  stats() { return { iters: this.iters, infosets: this.count, bytes: this.used * 4 + this.index.size * 40 }; }
}

function makeSpot(j) {
  if (!j.players || j.players.length < 2 || j.players.length > 9) throw new Error('Use 2 to 9 players.');
  const names = j.players.map((p, i) => p.name || `P${i + 1}`);
  const stacks = j.players.map((p, i) => { const s = Number(p.stack); if (!(s >= 0)) throw new Error(`${names[i]}: stack can't be negative.`); return s; });
  if (stacks.filter((s) => s > 0).length === 0 && stacks.length > 0) { /* everyone all-in: straight to showdown */ }
  const ranges = j.players.map((p, i) => { try { return parseRange(p.range || ''); } catch (e) { throw new Error(`${names[i]} range: ${e.message}`); } });
  const board = parseCards(j.board || '');
  if (board.length < 3 || board.length > 5) throw new Error('The board needs 3, 4 or 5 cards.');
  const pot = Number(j.pot); if (!(pot > 0)) throw new Error('Pot must be above 0.');
  const sz = j.sizes || {};
  const sizes = ['flop', 'turn', 'river'].map((s) => {
    try { return { bets: parseSizes((sz[s] || {}).bet, false), raises: parseSizes((sz[s] || {}).raise, true) }; }
    catch (e) { throw new Error(`${s} sizes: ${e.message}`); }
  });
  let payoff = { type: 'chips' };
  if (j.payoff && j.payoff.type === 'icm') {
    const payouts = (j.payoff.payouts || []).map(Number).filter((x) => x >= 0);
    const others = (j.payoff.others || []).map(Number).filter((x) => x > 0);
    if (!payouts.length) throw new Error('ICM needs at least one payout.');
    if (names.length + others.length > 200) throw new Error('ICM supports up to 200 players left.');
    payoff = { type: 'icm', payouts, others };
  }
  const start = board.length - 3, multi = names.length > 2;
  const ab = j.abstraction || {};
  const turnBuckets = ab.turnBuckets ?? (start === 0 ? (multi ? 30 : 50) : 0);
  const riverBuckets = ab.riverBuckets ?? (start === 0 ? (multi ? 100 : 200) : start === 1 && names.length > 3 ? 100 : 0);
  return { names, stacks, ranges, board, pot, sizes, raiseCap: j.raiseCap ?? 2, allinThreshold: j.allinThreshold ?? 0.15, mergeThreshold: j.mergeThreshold ?? 0.1, payoff, turnBuckets, riverBuckets };
}

// ---------------------------------------------------------------- preflop solver
// Monte Carlo CFR over the full preflop betting tree. Every iteration deals real
// hands and a real board, so all-in pots are settled exactly. Pots that reach the
// flop with chips behind are settled by equity with a positional realization
// factor (the standard preflop-solver model; postflop play itself is not solved here).
const CLASS_OF = new Uint8Array(NUM_COMBOS), CLASS_NAMES = [], CLASS_SIZE = [];
(function () {
  const idx = new Map();
  for (let c = 0; c < NUM_COMBOS; c++) {
    const k = comboClass(c);
    if (!idx.has(k)) { idx.set(k, CLASS_NAMES.length); CLASS_NAMES.push(k); CLASS_SIZE.push(0); }
    CLASS_OF[c] = idx.get(k); CLASS_SIZE[idx.get(k)]++;
  }
})();
const NCLASS = 169;
// heads-up preflop equity of class i vs class j (all-in to the river), 120k deals per pair
const EQ169_B64 = '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';
const EQ169 = (() => {
  const bin = typeof atob === 'function' ? atob(EQ169_B64) : Buffer.from(EQ169_B64, 'base64').toString('binary');
  const u8 = new Uint8Array(bin.length); for (let i = 0; i < bin.length; i++) u8[i] = bin.charCodeAt(i);
  const u16 = new Uint16Array(u8.buffer), f = new Float32Array(u16.length);
  for (let i = 0; i < u16.length; i++) f[i] = u16[i] / 65535;
  return f;
})();

class PreflopSolver {
  // cfg: { names, stacks, posts, ante, sizes:{open,b3,b4,b5}, hu, payoff, postOrder }
  constructor(cfg) {
    this.cfg = cfg; this.n = cfg.names.length;
    this.nodes = []; this.buildTree();
    let off = 0;
    for (const nd of this.nodes) if (nd.kind === 'act') { nd.off = off; off += NCLASS * 2 * nd.actions.length; }
    this.T = new Float32Array(off);
    this.iters = 0; this.nextReset = 2000; this.icmCache = new Map();
    this.deck = new Uint8Array(52); for (let i = 0; i < 52; i++) this.deck[i] = i;
    this.hand = new Int32Array(this.n); this.cls = new Int32Array(this.n); this.str = new Int32Array(this.n);
    this.c7 = new Uint8Array(7); this.sig = []; this.val = [];
    for (let d = 0; d < 64; d++) { this.sig.push(new Float64Array(8)); this.val.push(new Float64Array(8)); }
    const p = cfg.payoff || { type: 'chips' };
    if (p.type === 'icm' && this.n + p.others.length > 14) this.icmModel = new IcmModel(this.n, p.others, p.payouts, 4000);
  }

  buildTree() {
    const cfg = this.cfg, n = this.n, st = cfg.stacks;
    const s0 = { put: cfg.posts.slice(), folded: new Array(n).fill(false), allin: cfg.posts.map((x, i) => x >= st[i] - 1e-9), needs: new Array(n).fill(true),
      bet: Math.max(...cfg.posts), level: 0, lastRaise: Math.max(1, Math.max(...cfg.posts)), cur: -1, acted: new Array(n).fill(false), vol: new Array(n).fill(false) };
    const clone = (s) => ({ put: s.put.slice(), folded: s.folded.slice(), allin: s.allin.slice(), needs: s.needs.slice(), bet: s.bet, level: s.level, lastRaise: s.lastRaise, cur: s.cur, acted: s.acted.slice(), vol: s.vol.slice() });
    const live = (s) => s.folded.reduce((a, f) => a + (f ? 0 : 1), 0);
    const next = (s) => {
      if (live(s) < 2) return -1;
      for (let k = 1; k <= n; k++) {
        const i = (s.cur + k + n) % n;
        if (!s.folded[i] && !s.allin[i] && s.needs[i]) {
          let others = 0; for (let j = 0; j < n; j++) if (j !== i && !s.folded[j] && !s.allin[j]) others++;
          if (!others && s.put[i] >= s.bet - 1e-9) return -1;
          return i;
        }
      }
      return -1;
    };
    const sizeFor = (s, L) => {
      let to = L === 1 ? cfg.sizes.open : L === 2 ? cfg.sizes.b3 : L === 3 ? cfg.sizes.b4 : cfg.sizes.b5;
      const minTo = s.bet + s.lastRaise; if (to < minTo) to = Math.max(minTo, s.bet * 2.2);
      return Math.round(to * 10) / 10;
    };
    // Tree rules (standard preflop-solver simplifications that keep the tree solvable):
    //  first in: fold / raise / jam (the small blind may also complete)
    //  vs an open: fold / call / 3-bet / jam, with at most two cold callers
    //  vs a 3-bet: players already in may fold / call / 4-bet / jam; cold players fold or jam
    //  vs a 4-bet or more: players already in fold / call / jam; cold players fold
    const opts = (s, p) => {
      const o = [], toCall = s.bet - s.put[p], left = st[p] - s.put[p], inVol = s.vol[p];
      let others = 0; for (let j = 0; j < n; j++) if (j !== p && !s.folded[j] && !s.allin[j]) others++;
      const callers = s.vol.filter((v, j) => v && !s.folded[j]).length - 1; // minus the raiser
      if (toCall > 1e-9) {
        o.push({ k: 'fold' });
        if (left <= toCall + 1e-9) { if (s.level <= 1 || inVol || s.level === 2) o.push({ k: 'call' }); return o; }
        if (s.level === 0) { if (cfg.names[p] === 'SB') o.push({ k: 'call' }); }
        else if (s.level === 1) { if (callers < 2 || cfg.names[p] === 'BB') o.push({ k: 'call' }); }
        else if (inVol) o.push({ k: 'call' });
      } else o.push({ k: 'check' });
      if (!others) return o;
      const L = s.level + 1;
      if (s.level >= 3 && !inVol) return o;
      if (L <= 3 && (s.level < 2 || inVol)) { const to = sizeFor(s, L); if (to < st[p] * 0.95) o.push({ k: 'raise', to, L }); }
      if (L === 4 && inVol && st[p] > 150) { const to = sizeFor(s, L); if (to < st[p] * 0.95) o.push({ k: 'raise', to, L }); }
      o.push({ k: 'allin', to: st[p], L });
      return o;
    };
    const apply = (s, p, a) => {
      const t = clone(s); t.needs[p] = false; t.acted[p] = true; t.cur = p;
      if (a.k !== 'fold' && a.k !== 'check') t.vol[p] = true;
      if (a.k === 'fold') t.folded[p] = true;
      else if (a.k === 'call') { t.put[p] = Math.min(t.bet, st[p]); if (t.put[p] >= st[p] - 1e-9) t.allin[p] = true; }
      else if (a.k === 'raise' || a.k === 'allin') {
        const to = Math.min(a.to, st[p]), raise = to - t.bet; t.put[p] = to; if (to >= st[p] - 1e-9) t.allin[p] = true;
        if (raise > 1e-9) { t.level++; if (raise >= t.lastRaise - 1e-9) t.lastRaise = raise; t.bet = to; for (let j = 0; j < n; j++) if (j !== p && !t.folded[j] && !t.allin[j]) t.needs[j] = true; }
      }
      return t;
    };
    const build = (s) => {
      const p = next(s);
      if (p < 0) {
        const lv = live(s); let free = 0; for (let j = 0; j < n; j++) if (!s.folded[j] && !s.allin[j]) free++;
        const nd = { kind: lv === 1 ? 'fold' : free >= 2 ? 'flop' : 'show', put: s.put.slice(), folded: s.folded.slice() };
        this.prepTerminal(nd); this.nodes.push(nd); return this.nodes.length - 1;
      }
      const os = opts(s, p);
      const id = this.nodes.length; this.nodes.push({ kind: 'act', player: p, actions: os, children: [], level: s.level, bet: s.bet, put: s.put.slice() });
      if (this.nodes.length > 400000) throw new Error('Preflop tree too large. Use fewer players or bigger sizes.');
      for (const a of os) this.nodes[id].children.push(build(apply(s, p, a)));
      return id;
    };
    build(s0);
  }

  prepTerminal(nd) {
    const n = this.n, pot = nd.put.reduce((a, b) => a + b, 0) + (this.cfg.ante || 0); nd.pot = pot;
    const alive = []; for (let i = 0; i < n; i++) if (!nd.folded[i]) alive.push(i);
    nd.alive = alive;
    if (nd.kind === 'fold') { nd.winner = alive[0]; return; }
    const levels = [...new Set(alive.map((i) => Math.round(nd.put[i] * 1e6) / 1e6))].sort((a, b) => a - b);
    let prev = 0, dist = 0; nd.layers = [];
    levels.forEach((lv, li) => {
      let amt = 0; for (let i = 0; i < n; i++) amt += Math.min(nd.put[i], lv) - Math.min(nd.put[i], prev);
      if (li === 0) amt += this.cfg.ante || 0;
      if (li === levels.length - 1) amt = pot - dist;
      nd.layers.push({ amount: amt, elig: alive.filter((i) => nd.put[i] >= lv - 1e-9) }); dist += amt; prev = lv;
    });
    if (nd.kind === 'flop') {
      // realization by postflop order: first to act realizes least, last to act most
      const order = (this.cfg.postOrder || alive).filter((i) => alive.includes(i)), k = order.length;
      nd.R = new Float64Array(n);
      order.forEach((i, r) => { nd.R[i] = k === 1 ? 1 : 0.82 + 0.18 * (r / (k - 1)); });
    }
  }

  finalStacks(nd) {
    const n = this.n, fin = new Float64Array(n), st = this.cfg.stacks;
    for (let i = 0; i < n; i++) fin[i] = st[i] - nd.put[i];
    if (nd.kind === 'fold') { fin[nd.winner] += nd.pot; return fin; }
    if (nd.kind === 'flop' && nd.layers.length === 1) {
      const el = nd.layers[0].elig; let best = -1; const w = [];
      for (const i of el) { const s = this.str[i]; if (s > best) { best = s; w.length = 0; w.push(i); } else if (s === best) w.push(i); }
      let sumRW = 0; for (const i of w) sumRW += nd.R[i] / w.length;
      const rest = (1 - sumRW) / el.length;
      for (const i of el) fin[i] += nd.pot * ((w.includes(i) ? nd.R[i] / w.length : 0) + rest);
      return fin;
    }
    for (const L of nd.layers) {
      let best = -1; const w = [];
      for (const i of L.elig) { const s = this.str[i]; if (s > best) { best = s; w.length = 0; w.push(i); } else if (s === best) w.push(i); }
      for (const i of w) fin[i] += L.amount / w.length;
    }
    return fin;
  }

  utility(nd) {
    const fin = this.finalStacks(nd), p = this.cfg.payoff || { type: 'chips' };
    if (p.type !== 'icm') { const u = new Float64Array(this.n); for (let i = 0; i < this.n; i++) u[i] = fin[i] - this.cfg.stacks[i]; return u; }
    let k = ''; for (let i = 0; i < fin.length; i++) k += Math.round(fin[i] * 100) + ',';
    let v = this.icmCache.get(k); if (v) return v;
    if (this.icmModel) v = this.icmModel.equity(fin, this.cfg.stacks);
    else v = icmEquity([...fin, ...p.others], [...this.cfg.stacks, ...p.others], p.payouts).slice(0, this.n);
    if (this.icmCache.size < 400000) this.icmCache.set(k, v);
    return v;
  }

  deal() {
    const d = this.deck, n = this.n;
    for (let i = 0; i < n * 2 + 5; i++) { const j = i + ((Math.random() * (52 - i)) | 0); const t = d[i]; d[i] = d[j]; d[j] = t; }
    const c7 = this.c7; for (let b = 0; b < 5; b++) c7[b] = d[n * 2 + b];
    for (let p = 0; p < n; p++) {
      const a = d[2 * p], b = d[2 * p + 1];
      this.hand[p] = comboIndex(a, b); this.cls[p] = CLASS_OF[this.hand[p]];
      c7[5] = a; c7[6] = b; this.str[p] = evaluate(c7, 7);
    }
  }

  // strength of every hand class for the traverser on this board, using a combo of the
  // class that doesn't collide with the board or the other players' cards
  classStrengths(p) {
    if (!this.byClass) { this.byClass = Array.from({ length: NCLASS }, () => []); for (let c = 0; c < NUM_COMBOS; c++) this.byClass[CLASS_OF[c]].push(c); this.sv = new Int32Array(NCLASS); this.used = new Uint8Array(52); }
    const used = this.used; used.fill(0);
    for (let b = 0; b < 5; b++) used[this.c7[b]] = 1;
    for (let q = 0; q < this.n; q++) if (q !== p) { used[COMBO_A[this.hand[q]]] = 1; used[COMBO_B[this.hand[q]]] = 1; }
    // Importance weight: the deal was drawn without knowing this class, so weight each
    // class by how many of its combos fit around the dealt cards (keeps card removal exact).
    if (!this.cw) { this.cw = new Float64Array(NCLASS); const n7 = 2 * (this.n - 1) + 5; this.kFree = ((52 - n7) * (51 - n7)) / (52 * 51); }
    const c7 = this.c7;
    for (let c = 0; c < NCLASS; c++) {
      const list = this.byClass[c], st = (Math.random() * list.length) | 0; let s = -1, avail = 0, pick = -1;
      for (let k = 0; k < list.length; k++) { const cb = list[(st + k) % list.length]; if (!used[COMBO_A[cb]] && !used[COMBO_B[cb]]) { avail++; if (pick < 0) pick = cb; } }
      if (pick >= 0) { c7[5] = COMBO_A[pick]; c7[6] = COMBO_B[pick]; s = evaluate(c7, 7); }
      this.sv[c] = s; this.cw[c] = avail / (list.length * this.kFree);
    }
  }

  // traverser p's payoff for each class at a terminal
  // heads-up terminals: exact class-vs-class equity instead of a sampled board
  termVec2(nd, p, o, out) {
    if (!this.byClass) { this.byClass = Array.from({ length: NCLASS }, () => []); for (let c = 0; c < NUM_COMBOS; c++) this.byClass[CLASS_OF[c]].push(c); }
    const oc = this.cls[o], oh = this.hand[o], oa = COMBO_A[oh], ob = COMBO_B[oh], k2 = (50 * 49) / (52 * 51);
    const icm = this.cfg.payoff && this.cfg.payoff.type === 'icm';
    let winU, loseU;
    if (icm) {
      const sp = this.str[p], so = this.str[o];
      this.str[p] = 1; this.str[o] = 0; winU = this.utility(nd)[p];
      this.str[p] = 0; this.str[o] = 1; loseU = this.utility(nd)[p];
      this.str[p] = sp; this.str[o] = so;
    }
    let both = 0, onlyP = 0;
    if (!icm && nd.kind === 'show') for (const L of nd.layers) { if (L.elig.length === 2) both += L.amount; else if (L.elig[0] === p) onlyP += L.amount; }
    for (let c = 0; c < NCLASS; c++) {
      const list = this.byClass[c]; let avail = 0;
      for (const cb of list) if (COMBO_A[cb] !== oa && COMBO_A[cb] !== ob && COMBO_B[cb] !== oa && COMBO_B[cb] !== ob) avail++;
      const wgt = avail / (list.length * k2);
      const eq = EQ169[c * NCLASS + oc];
      let share;
      if (nd.kind === 'flop') { const rp = nd.R[p], ro = nd.R[o]; share = rp * eq + (1 - (rp * eq + ro * (1 - eq))) / 2; }
      else share = eq;
      let u;
      if (icm) u = share * winU + (1 - share) * loseU;
      else if (nd.kind === 'flop') u = nd.pot * share - nd.put[p];
      else u = both * eq + onlyP - nd.put[p];
      out[c] = u * wgt;
    }
  }

  termVec(nd, p, out) {
    if (nd.kind !== 'fold' && nd.alive.length === 2 && nd.alive.includes(p)) { this.termVec2(nd, p, nd.alive[0] === p ? nd.alive[1] : nd.alive[0], out); return; }
    if (this.svFor !== p) { this.classStrengths(p); this.svFor = p; }
    const save = this.str[p], icm = this.cfg.payoff && this.cfg.payoff.type === 'icm';
    if (nd.kind === 'fold' && !icm) { const u = (nd.winner === p ? nd.pot : 0) - nd.put[p]; for (let c = 0; c < NCLASS; c++) out[c] = u * this.cw[c]; return; }
    const memo = new Map();
    for (let c = 0; c < NCLASS; c++) {
      this.str[p] = this.sv[c];
      // payoff depends only on how p's strength ranks against the others: memoize by rank
      let rank = 0, tie = 0; for (let q = 0; q < this.n; q++) if (q !== p && !nd.folded[q]) { if (this.str[q] < this.sv[c]) rank++; else if (this.str[q] === this.sv[c]) tie++; }
      const key = nd.kind === 'fold' ? 0 : rank * 16 + tie;
      let v = memo.get(key);
      if (v === undefined) { v = this.utility(nd)[p]; memo.set(key, v); }
      out[c] = v * this.cw[c];
    }
    this.str[p] = save;
  }

  traverse(id, p, w, wa, depth) {
    const nd = this.nodes[id], out = this.vec[depth];
    if (nd.kind !== 'act') { this.termVec(nd, p, out); return out; }
    const q = nd.player, na = nd.actions.length, T = this.T;
    if (q === p) {
      const kids = [];
      for (let a = 0; a < na; a++) { const v = this.traverse(nd.children[a], p, w, wa, depth + 1); kids.push(Float64Array.from(v)); }
      const sig = this.sig[depth];
      for (let c = 0; c < NCLASS; c++) {
        const off = nd.off + c * 2 * na; let sum = 0;
        for (let a = 0; a < na; a++) { const r = T[off + a]; sig[a] = r > 0 ? r : 0; sum += sig[a]; }
        if (sum > 0) for (let a = 0; a < na; a++) sig[a] /= sum; else for (let a = 0; a < na; a++) sig[a] = 1 / na;
        let v = 0; for (let a = 0; a < na; a++) v += sig[a] * kids[a][c];
        for (let a = 0; a < na; a++) { const r = T[off + a] + w * (kids[a][c] - v); T[off + a] = r > 0 ? r : 0; }
        out[c] = v;
      }
      return out;
    }
    const off = nd.off + this.cls[q] * 2 * na, sig = this.sig[depth];
    let sum = 0; for (let a = 0; a < na; a++) { const r = T[off + a]; sig[a] = r > 0 ? r : 0; sum += sig[a]; }
    if (sum > 0) for (let a = 0; a < na; a++) sig[a] /= sum; else for (let a = 0; a < na; a++) sig[a] = 1 / na;
    for (let a = 0; a < na; a++) T[off + na + a] += wa * sig[a];
    let x = Math.random(), pick = na - 1; for (let a = 0; a < na; a++) { x -= sig[a]; if (x < 0) { pick = a; break; } }
    const v = this.traverse(nd.children[pick], p, w, wa, depth + 1);
    if (v !== out) out.set(v);
    return out;
  }

  run(ms) {
    if (!this.vec) { this.vec = []; for (let d = 0; d < 64; d++) this.vec.push(new Float64Array(NCLASS)); }
    const t0 = Date.now();
    while (Date.now() - t0 < ms) for (let j = 0; j < 20; j++) {
      const w = Math.floor(this.iters / 500) + 1, wa = w * w;
      this.deal(); this.svFor = -1;
      for (let p = 0; p < this.n; p++) { this.traverse(0, p, w, wa, 0); }
      this.iters++;
      // forget the noisy early average at 4^k checkpoints (as the postflop engine does)
      if (this.iters === this.nextReset) { this.resetAverages(); this.nextReset *= 4; }
    }
  }

  resetAverages() {
    for (const nd of this.nodes) if (nd.kind === 'act') {
      const na = nd.actions.length;
      for (let c = 0; c < NCLASS; c++) { const off = nd.off + c * 2 * na; for (let a = 0; a < na; a++) this.T[off + na + a] = 0; }
    }
  }

  avg(id, cls) {
    const nd = this.nodes[id], na = nd.actions.length, off = nd.off + cls * 2 * na, out = new Array(na).fill(0); let s = 0;
    for (let a = 0; a < na; a++) { out[a] = this.T[off + na + a]; s += out[a]; }
    if (s > 0) return out.map((x) => x / s);
    return out.fill(1 / na);
  }

  // walk a path of {k, to?} from the root, matching actions by kind (raises by size)
  follow(path) {
    let id = 0; const steps = [];
    for (const e of path) {
      const nd = this.nodes[id]; if (nd.kind !== 'act') return null;
      let a = nd.actions.findIndex((x) => x.k === e.k && (e.k !== 'raise' || e.to === undefined || Math.abs(x.to - e.to) < 0.05));
      if (a < 0) return null;
      steps.push({ id, player: nd.player, a }); id = nd.children[a];
    }
    return { id, steps };
  }

  // per player: probability (0..1) that each hand class takes this line
  reachByClass(steps) {
    const r = Array.from({ length: this.n }, () => new Float64Array(NCLASS).fill(1));
    for (const s of steps) for (let c = 0; c < NCLASS; c++) r[s.player][c] *= this.avg(s.id, c)[s.a];
    return r;
  }

  stats() { return { iters: this.iters, nodes: this.nodes.length, bytes: this.T.length * 4 }; }
}

const SlickEngine = { PreflopSolver, CLASS_NAMES, CLASS_OF, IcmModel, Solver, makeSpot, parseRange, parseCards, cardStr, comboClass, comboStr, evaluate, icmEquity, buildTree, NUM_COMBOS, COMBO_A, COMBO_B };

// ---------------------------------------------------------------- worker protocol
if (typeof self !== 'undefined' && typeof self.postMessage === 'function' && typeof window === 'undefined') {
  let solver = null, running = false, deadline = 0, maxIters = Infinity;
  let pf = null, pfRunning = false, pfDeadline = 0, pfKey = null;
  const pfLoop = () => {
    if (!pfRunning || !pf) return;
    try { pf.run(200); } catch (e) { pfRunning = false; self.postMessage({ type: 'error', message: e.message }); return; }
    const done = Date.now() >= pfDeadline;
    self.postMessage({ type: 'pfProgress', key: pfKey, iters: pf.iters, done });
    if (done) { pfRunning = false; return; }
    setTimeout(pfLoop, 0);
  };
  const loop = () => {
    if (!running || !solver) return;
    try { solver.run(150); } catch (e) { running = false; self.postMessage({ type: 'error', message: e.message }); return; }
    const st = solver.stats();
    const done = Date.now() >= deadline || st.iters >= maxIters || st.bytes > 1.8e9;
    self.postMessage({ type: 'progress', ...st, done, memoryCapped: st.bytes > 1.8e9 });
    if (done) { running = false; return; }
    setTimeout(loop, 0);
  };
  self.onmessage = (ev) => {
    const m = ev.data;
    try {
      if (m.cmd === 'build') {
        running = false;
        const t0 = Date.now();
        solver = new Solver(makeSpot(m.spot));
        self.postMessage({ type: 'built', nodes: solver.nodes.length, decisionPoints: solver.decisionPoints, buckets: [solver.spot.turnBuckets, solver.spot.riverBuckets], names: solver.spot.names, board: solver.board, ms: Date.now() - t0 });
      } else if (m.cmd === 'solve') {
        if (!solver) throw new Error('Build the spot first.');
        deadline = Date.now() + m.seconds * 1000; maxIters = m.iterations || Infinity; running = true; loop();
      } else if (m.cmd === 'stop') { running = false; self.postMessage({ type: 'progress', ...solver.stats(), done: true }); }
      else if (m.cmd === 'report') self.postMessage({ type: 'report', reqId: m.reqId, report: solver.report(m.path) });
      else if (m.cmd === 'ev') self.postMessage({ type: 'ev', ev: solver.estimateEV(m.samples || 30000) });
      // ---- preflop solver (runs in its own worker)
      else if (m.cmd === 'pfBuild') {
        pfRunning = false; pf = new PreflopSolver(m.cfg); pfKey = m.key;
        self.postMessage({ type: 'pfBuilt', key: pfKey, nodes: pf.nodes.length });
      } else if (m.cmd === 'pfSolve') { pfDeadline = Date.now() + m.seconds * 1000; pfRunning = true; pfLoop(); }
      else if (m.cmd === 'pfStop') { pfRunning = false; if (pf) self.postMessage({ type: 'pfProgress', key: pfKey, iters: pf.iters, done: true }); }
      else if (m.cmd === 'pfQuery') {
        if (!pf || m.key !== pfKey) { self.postMessage({ type: 'pfResult', reqId: m.reqId, ok: false }); return; }
        const f = pf.follow(m.path);
        if (!f) { self.postMessage({ type: 'pfResult', reqId: m.reqId, ok: false }); return; }
        const reach = pf.reachByClass(f.steps).map((r) => Array.from(r));
        const nd = pf.nodes[f.id];
        if (nd.kind !== 'act') { self.postMessage({ type: 'pfResult', reqId: m.reqId, ok: true, terminal: true, reach, iters: pf.iters }); return; }
        const strat = []; for (let c = 0; c < NCLASS; c++) strat.push(pf.avg(f.id, c));
        self.postMessage({ type: 'pfResult', reqId: m.reqId, ok: true, player: nd.player, actions: nd.actions, strat, reach, iters: pf.iters });
      }
    } catch (e) { self.postMessage({ type: 'error', message: e.message, reqId: m.reqId }); }
  };
}
if (typeof module !== 'undefined') module.exports = SlickEngine;
