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
  build(st0);
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
    const all = [...fin, ...others], start = [...this.spot.stacks, ...others];
    v = icmEquity(all, start, payouts).slice(0, this.n);
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
  const stacks = j.players.map((p, i) => { const s = Number(p.stack); if (!(s > 0)) throw new Error(`${names[i]}: stack must be above 0.`); return s; });
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
    if (names.length + others.length > 20) throw new Error('ICM supports up to 20 players left.');
    payoff = { type: 'icm', payouts, others };
  }
  const start = board.length - 3, multi = names.length > 2;
  const ab = j.abstraction || {};
  const turnBuckets = ab.turnBuckets ?? (start === 0 ? (multi ? 30 : 50) : 0);
  const riverBuckets = ab.riverBuckets ?? (start === 0 ? (multi ? 100 : 200) : start === 1 && names.length > 3 ? 100 : 0);
  return { names, stacks, ranges, board, pot, sizes, raiseCap: j.raiseCap ?? 2, allinThreshold: j.allinThreshold ?? 0.15, mergeThreshold: j.mergeThreshold ?? 0.1, payoff, turnBuckets, riverBuckets };
}

const SlickEngine = { Solver, makeSpot, parseRange, parseCards, cardStr, comboClass, comboStr, evaluate, icmEquity, buildTree, NUM_COMBOS, COMBO_A, COMBO_B };

// ---------------------------------------------------------------- worker protocol
if (typeof self !== 'undefined' && typeof self.postMessage === 'function' && typeof window === 'undefined') {
  let solver = null, running = false, deadline = 0, maxIters = Infinity;
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
    } catch (e) { self.postMessage({ type: 'error', message: e.message, reqId: m.reqId }); }
  };
}
if (typeof module !== 'undefined') module.exports = SlickEngine;
