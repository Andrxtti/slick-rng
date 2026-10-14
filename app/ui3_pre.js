(() => {
'use strict';
const RANKS = '23456789TJQKA', SUITS = 'cdhs', SYM = { c: '♣', d: '♦', h: '♥', s: '♠' };
const $ = (id) => document.getElementById(id);
const esc = (s) => String(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const src = $('engine-src').textContent;
const Engine = new Function(src + '\nreturn SlickEngine;')();
const fmt = (x) => (Math.abs(x - Math.round(x)) < 1e-9 ? String(Math.round(x)) : x.toFixed(1));

// ---------------------------------------------------------------- starting ranges (Slick RNG estimates, editable)
// open-raise by distance from the button: 0 BTN, 1 CO, 2 HJ, 3 LJ, 4, 5, 6 = earliest seat at a full table
const RFI_D = [
  '22+,A2s+,K2s+,Q4s+,J6s+,T6s+,95s+,85s+,74s+,64s+,53s+,43s,A2o+,K8o+,Q9o+,J9o+,T8o+,98o,87o',
  '22+,A2s+,K5s+,Q8s+,J8s+,T7s+,97s+,86s+,75s+,65s,54s,A8o+,A5o,KTo+,QTo+,JTo',
  '22+,A2s+,K8s+,Q9s+,J9s+,T8s+,97s+,87s,76s,65s,A9o+,KTo+,QJo',
  '33+,A2s+,K9s+,Q9s+,J9s+,T8s+,98s,87s,76s,ATo+,KJo+,QJo',
  '44+,A5s+,K9s+,Q9s+,J9s+,T9s,98s,87s,ATo+,KJo+',
  '55+,A8s+,KTs+,QTs+,JTs,T9s,98s,ATo+,KQo',
  '66+,A9s+,KTs+,QTs+,JTs,T9s,AJo+,KQo',
];
const RFI_SB = '22+,A2s+,K4s+,Q6s+,J7s+,T7s+,96s+,86s+,75s+,65s,54s,A5o+,K9o+,QTo+,JTo';
const RFI_HU_SB = '22+,A2s+,K2s+,Q2s+,J3s+,T5s+,95s+,85s+,74s+,64s+,53s+,43s,A2o+,K5o+,Q7o+,J8o+,T8o+,97o+,87o,76o';
const CALL = {
  early: '22-JJ,ATs-AQs,KJs+,QJs,JTs,T9s,98s,AQo',
  mid: '22-JJ,A9s-AQs,KTs+,QTs+,JTs,T9s,98s,87s,AJo-AQo,KQo',
  btn: '22-JJ,A2s-AJs,K9s+,Q9s+,J9s+,T8s+,97s+,87s,76s,65s,AJo-ATo,KQo,KJo,QJo',
  sb: '22-TT,ATs-AJs,KTs+,QTs+,JTs,T9s,98s,AQo',
  bb: '22-99,A2s-AJs,K2s+,Q5s+,J7s+,T7s+,96s+,85s+,75s+,64s+,53s+,43s,A2o-AJo,K9o+,Q9o+,J9o+,T9o,98o,87o,76o',
};
const ISO = 'TT+,AJs+,KQs,AQo+';
const THREEBET = 'TT+,AJs+,KQs,AQo+,A5s-A3s,K9s,76s,65s';
const FOURBET = 'QQ+,AKs,AKo,A5s:0.5,A4s:0.5';
const FIVEBET = 'KK+,AKs';
const CALL_3B = 'QQ-77,AQs-ATs,KQs-KJs,QJs,JTs,T9s,98s,AQo,AKo:0.5';
const COLD_3B = 'QQ-JJ,AKs,AQs';
const CALL_4B = 'QQ-TT,AKo,AKs:0.5,AQs';
const CALL_5B = 'QQ+,AKs,AKo';
const LIMP = '22-99,A2s-AJs,K9s+,Q9s+,J9s+,T8s+,97s+,86s+,75s+,65s,54s,A9o-AJo,KJo,QJo,JTo';
const LIMP_CALL = '22-99,A2s-AJs,K9s+,Q9s+,J9s+,T8s+,97s+,87s,76s,65s,AJo,KQo';
const SB_COMPLETE = '22-99,A2s-AJs,K2s+,Q5s+,J7s+,T7s+,96s+,85s+,75s+,64s+,54s,A2o-AJo,K8o+,Q9o+,J9o+,T9o,98o';
function jamRange(level, stack, d) {
  if (stack <= 12) return level <= 1 ? (d >= 4 ? '55+,A7s+,KTs+,QJs,A9o+,KQo' : '22+,A2s+,K7s+,Q9s+,J9s+,T9s,A5o+,KTo+,QJo') : '55+,A8s+,KTs+,AJo+,KQo';
  if (stack <= 25) return level <= 1 ? '77+,ATs+,KQs,AQo+' : level === 2 ? '88+,AJs+,KQs,AQo+,A5s' : 'TT+,AQs+,AKo';
  return level <= 3 ? '' : 'QQ+,AKs,AKo';
}
function callJamRange(stack) { return stack <= 12 ? '22+,A2s+,K8s+,QTs+,A7o+,KTo+' : stack <= 25 ? '66+,ATs+,KQs,AJo+' : 'QQ+,AKs,AKo'; }
const BB_CHECK = 'ANY';
const FT_PAY = [26.2, 18.0, 13.2, 10.2, 8.0, 6.6, 5.4, 4.6, 3.8];
const SIZES = {
  simple: { flop: ['33%', '3x'], turn: ['66%', '3x'], river: ['75%', '3x'], cap: 1 },
  standard: { flop: ['33%,75%', '3x'], turn: ['50%,100%', '3x'], river: ['50%,100%', '3x'], cap: 1 },
  complex: { flop: ['25%,50%,100%', '2.5x,a'], turn: ['33%,75%,125%', '2.5x,a'], river: ['33%,75%,125%,a', '2.5x,a'], cap: 2 },
};

// ---------------------------------------------------------------- table model
const S = { game: 'cash', table: '6', slots: [], stack: 100, left: 9, sz: { open: 2, b3: 6, b4: 14, b5: 30 }, board: [], sizing: 'simple', overrides: {}, hist: [], sel: null };
function slotCount() { return S.table === '6' ? 6 : S.table === '8' ? 8 : 9; }
function makeSlots() { S.slots = Array.from({ length: slotCount() }, (_, i) => ({ id: i, active: true })); }
function namesFor(n) {
  if (n === 2) return ['SB', 'BB'];
  const m = n - 2, base = ['UTG', 'UTG+1', 'UTG+2', 'LJ', 'HJ', 'CO', 'BTN'];
  const last = base.slice(7 - m);
  const early = last.filter((x) => x.startsWith('UTG')).length;
  if (early) for (let i = 0; i < early; i++) last[i] = i === 0 ? 'UTG' : `UTG+${i}`;
  else last[0] = 'UTG';
  return [...last, 'SB', 'BB'];
}
let T = null; // live table state built by replay()
function freshTable() {
  const act = S.slots.filter((s) => s.active), names = namesFor(act.length), m = act.length - 2;
  const seats = act.map((s, i) => ({ id: s.id, pos: names[i], d: names[i] === 'SB' || names[i] === 'BB' ? -1 : m - 1 - i,
    put: names[i] === 'SB' ? 0.5 : names[i] === 'BB' ? 1 : 0, folded: false, allin: false, needs: true, acts: [] }));
  T = { seats, bet: 1, level: 0, lastRaise: 1, cur: -1, hu: act.length === 2 };
}
function live() { return T.seats.filter((s) => !s.folded); }
function nextActor() {
  if (live().length < 2) return null;
  const n = T.seats.length;
  for (let k = 1; k <= n; k++) {
    const s = T.seats[(T.cur + k + n) % n];
    if (!s.folded && !s.allin && s.needs) {
      // everyone else all-in and nothing to call -> nothing to decide
      const others = live().filter((x) => x !== s && !x.allin).length;
      if (!others && s.put >= T.bet - 1e-9) return null;
      return s;
    }
  }
  return null;
}
const limperCount = () => T.seats.filter((s) => s.acts.some((a) => a.k === 'call' && a.facing === 0) && s.pos !== 'BB').length;
function raiseName(L) { return L === 1 ? 'Raise' : `${L + 1}-Bet`; }
function jamName(L) { return L === 1 ? 'Jam' : `${L + 1}B Jam`; }
function defaultRaise(L) {
  let to = L === 1 ? S.sz.open + limperCount() : L === 2 ? S.sz.b3 : L === 3 ? S.sz.b4 : L === 4 ? S.sz.b5 : T.bet * 2.2;
  const minTo = T.bet + T.lastRaise;
  if (to < minTo) to = Math.max(minTo, T.bet * 2.2);
  return Math.round(to * 10) / 10;
}
function minRaiseTo() { return T.bet + T.lastRaise; }
function options(seat) {
  const o = [], toCall = T.bet - seat.put, left = S.stack - seat.put;
  const othersCanAct = live().some((x) => x !== seat && !x.allin);
  if (toCall > 1e-9) {
    o.push({ k: 'fold', t: 'Fold', cls: 'fold' });
    if (left <= toCall + 1e-9) { o.push({ k: 'call', t: `Call all-in ${fmt(S.stack)}`, cls: 'pass' }); return o; }
    o.push({ k: 'call', t: T.level === 0 ? (seat.pos === 'SB' ? 'Complete' : 'Limp') : `Call ${fmt(T.bet)}`, cls: 'pass' });
  } else o.push({ k: 'check', t: 'Check', cls: 'pass' });
  if (!othersCanAct) return o;
  const L = T.level + 1, to = defaultRaise(L);
  if (to < S.stack - 1e-9) o.push({ k: 'raise', to, t: `${raiseName(L)} ${fmt(to)}`, cls: 'agg', L });
  o.push({ k: 'allin', to: S.stack, t: `${jamName(L)} ${fmt(S.stack)}`, cls: 'jam', L });
  return o;
}
function apply(seat, o) {
  const facing = T.level, vsAllin = T.seats.some((x) => x.allin && x.put >= T.bet - 1e-9 && x !== seat);
  if (o.k === 'fold') { seat.folded = true; seat.acts.push({ k: 'fold', label: 'Fold' }); }
  else if (o.k === 'check') seat.acts.push({ k: 'check', label: 'Check' });
  else if (o.k === 'call') {
    seat.put = Math.min(T.bet, S.stack); if (seat.put >= S.stack - 1e-9) seat.allin = true;
    seat.acts.push({ k: 'call', facing, vsAllin, label: o.t.replace(/ all-in.*/, ' AI') });
  } else {
    const to = Math.min(o.to, S.stack), raise = to - T.bet;
    seat.put = to; if (to >= S.stack - 1e-9) seat.allin = true;
    if (raise > 1e-9) {
      T.level++; if (raise >= T.lastRaise - 1e-9) T.lastRaise = raise; T.bet = to;
      for (const x of T.seats) if (x !== seat && !x.folded && !x.allin) x.needs = true;
    }
    seat.acts.push({ k: o.k, L: T.level, to, label: o.k === 'allin' ? `${jamName(T.level)} ${fmt(to)}` : `${raiseName(T.level)} ${fmt(to)}` });
  }
  seat.needs = false;
  T.cur = T.seats.indexOf(seat);
}
function pickOption(opts, e) {
  const by = (k) => opts.find((x) => x.k === k);
  if (e.k === 'raise') {
    if (e.to !== undefined) {
      if (e.to >= minRaiseTo() - 1e-9 && e.to < S.stack - 1e-9 && by('raise')) { const L = T.level + 1; return { k: 'raise', to: e.to, t: `${raiseName(L)} ${fmt(e.to)}`, cls: 'agg', L, custom: true }; }
    }
    return by('raise') || by('allin') || by('call') || by('check');
  }
  if (e.k === 'allin') return by('allin') || by('call');
  if (e.k === 'call') return by('call') || by('check');
  if (e.k === 'check') return by('check') || by('call');
  return by('fold') || by('check');
}
// rebuild the table from the history, keeping every action that still applies
function replay() {
  freshTable(); const kept = []; S.decisions = [];
  for (const e of S.hist) {
    const nx = nextActor();
    if (!nx || nx.id !== e.id) break;
    const opts = options(nx), o = pickOption(opts, e);
    if (!o) break;
    S.decisions.push({ idx: kept.length, id: e.id, k: o.k, to: o.to, custom: !!o.custom, opts });
    kept.push(o.custom ? { id: e.id, k: o.k, to: o.to } : { id: e.id, k: o.k });
    apply(nx, o);
  }
  S.hist = kept;
}
function withHist(h, fn) { const save = S.hist; S.hist = h; replay(); const out = fn(); S.hist = save; replay(); return out; }
function foldsTo(id) {
  return withHist(S.hist.slice(), () => {
    const folds = [];
    for (let g = 0; g < 20; g++) {
      const nx = nextActor(); if (!nx) return null;
      if (nx.id === id) return folds;
      folds.push({ id: nx.id, k: 'fold' }); apply(nx, { k: 'fold' });
    }
    return null;
  });
}
const seatById = (id) => T.seats.find((s) => s.id === id);

// ---------------------------------------------------------------- ranges from the action line
function rfiFor(seat) { if (T.hu && seat.pos === 'SB') return RFI_HU_SB; if (seat.pos === 'SB') return RFI_SB; return RFI_D[Math.min(6, Math.max(0, seat.d))]; }
function callFor(seat) { if (seat.pos === 'BB') return CALL.bb; if (seat.pos === 'SB') return CALL.sb; if (seat.d === 0) return CALL.btn; return seat.d <= 2 ? CALL.mid : CALL.early; }
function raiseRange(seat, L, limpers) { return L === 1 ? (limpers ? ISO : rfiFor(seat)) : L === 2 ? THREEBET : L === 3 ? FOURBET : FIVEBET; }
function callRange(seat, facing, vsAllin, prior) {
  if (vsAllin) return callJamRange(S.stack);
  if (facing === 0) return seat.pos === 'SB' ? SB_COMPLETE : LIMP;
  if (facing === 1) return prior.some((a) => a.k === 'call' && a.facing === 0) ? LIMP_CALL : callFor(seat);
  if (facing === 2) return prior.some((a) => a.k === 'raise' && a.L === 1) ? CALL_3B : COLD_3B;
  return facing === 3 ? CALL_4B : CALL_5B;
}
function rangeFor(seat) {
  if (S.overrides[seat.id]) return S.overrides[seat.id];
  const acts = seat.acts.filter((a) => a.k !== 'fold');
  if (!acts.length) return seat.pos === 'BB' ? BB_CHECK : 'ANY';
  const last = acts[acts.length - 1], prior = acts.slice(0, -1);
  if (last.k === 'check') return BB_CHECK;
  if (last.k === 'call') return callRange(seat, last.facing, last.vsAllin, prior);
  if (last.k === 'allin') return jamRange(last.L, S.stack, seat.d);
  return raiseRange(seat, last.L, last.L === 1 && limperCount() > 0);
}
const RCACHE = new Map();
function rw(text) {
  if (text === 'ANY') return new Float32Array(1326).fill(1);
  let w = RCACHE.get(text); if (!w) { w = text ? Engine.parseRange(text) : new Float32Array(1326); RCACHE.set(text, w); } return w;
}

// ---------------------------------------------------------------- spot for the solver
function postflopOrder() {
  const inH = live();
  if (T.hu) return [...inH].sort((a) => (a.pos === 'BB' ? -1 : 1));
  const blinds = inH.filter((s) => s.pos === 'SB' || s.pos === 'BB').sort((a) => (a.pos === 'SB' ? -1 : 1));
  return [...blinds, ...inH.filter((s) => s.pos !== 'SB' && s.pos !== 'BB')];
}
function potNow() { return T.seats.reduce((a, s) => a + s.put, 0) + (S.game === 'cash' ? 0 : 1); }
function buildSpot() {
  if (nextActor()) throw new Error('Finish the preflop action first.');
  const players = postflopOrder();
  if (players.length < 2) throw new Error('At least two players need to see the flop.');
  if (S.board.length < 3) throw new Error('Pick the flop cards first.');
  const sz = { fb: $('fb').value, fr: $('fr').value, tb: $('tb').value, tr: $('tr').value, rb: $('rb').value, rr: $('rr').value };
  const spot = {
    players: players.map((s) => ({ name: s.pos, stack: Math.max(0.01, S.stack - s.put), range: rangeFor(s) })),
    pot: potNow(), board: S.board.map(Engine.cardStr).join(''),
    sizes: { flop: { bet: sz.fb, raise: sz.fr }, turn: { bet: sz.tb, raise: sz.tr }, river: { bet: sz.rb, raise: sz.rr } },
    raiseCap: players.length > 2 ? Math.min(1, SIZES[S.sizing].cap) : SIZES[S.sizing].cap, allinThreshold: 0.15,
  };
  if (S.game === 'icm') {
    const left = Math.max(S.left, players.length), others = [];
    for (let i = 0; i < left - players.length; i++) others.push(S.stack);
    spot.payoff = { type: 'icm', payouts: FT_PAY.slice(0, left), others };
  }
  return spot;
}

// ---------------------------------------------------------------- preflop chart for the selected seat
function preflopContext() {
  const nx = nextActor();
  let id = S.sel;
  if (id === null || id === undefined) id = nx ? nx.id : (S.decisions.length ? S.decisions[S.decisions.length - 1].id : null);
  if (id === null || !seatById(id)) return null;
  const mine = S.decisions.filter((d) => d.id === id);
  if (nx && nx.id === id) return { id, base: S.hist.slice(), mode: 'next' };
  if (mine.length) { const d = mine[mine.length - 1]; return { id, base: S.hist.slice(0, d.idx), mode: 'edit', idx: d.idx, cur: d }; }
  const f = nx ? foldsTo(id) : null;
  return f ? { id, base: [...S.hist, ...f], mode: 'foldto' } : null;
}
function preflopReport() {
  const ctx = preflopContext(); if (!ctx) return null;
  return withHist(ctx.base, () => {
    const seat = seatById(ctx.id), opts = options(seat), na = opts.length;
    const limpers = limperCount(), raiser = [...T.seats].reverse().find((x) => x.acts.some((a) => (a.k === 'raise' || a.k === 'allin') && a.L === T.level));
    const vsAllin = T.seats.some((x) => x.allin && x !== seat);
    const reach = rw(seat.acts.some((a) => a.k !== 'fold') ? rangeFor(seat) : 'ANY');
    const rangeOf = (o) => {
      if (o.k === 'fold') return null;
      if (o.k === 'check') return 'ANY';
      if (o.k === 'allin') return jamRange(o.L, S.stack, seat.d);
      if (o.k === 'raise') return S.stack <= 15 ? '' : raiseRange(seat, o.L, o.L === 1 && limpers > 0);
      if (T.level === 0) return limpers ? (seat.pos === 'SB' ? SB_COMPLETE : LIMP) : '';
      return callRange(seat, T.level, vsAllin, seat.acts);
    };
    const order = ['allin', 'raise', 'call', 'check', 'fold'];
    const wts = opts.map((o) => { const r = rangeOf(o); return r === null ? null : rw(r); });
    const pri = opts.map((_, i) => i).sort((a, b) => order.indexOf(opts[a].k) - order.indexOf(opts[b].k));
    const classes = {}, overall = new Array(na).fill(0); let total = 0;
    for (let c = 0; c < 1326; c++) {
      const r = reach[c]; if (r <= 0) continue;
      const sv = new Array(na).fill(0); let rest = 1;
      for (const i of pri) { if (!wts[i]) continue; const x = Math.min(rest, wts[i][c]); sv[i] = x; rest -= x; }
      const fi = opts.findIndex((o) => o.k === 'fold'); sv[fi >= 0 ? fi : pri[pri.length - 1]] += rest;
      const k = Engine.comboClass(c), e = classes[k] || (classes[k] = { w: 0, s: new Array(na).fill(0), combos: [] });
      e.w += r; for (let a = 0; a < na; a++) { e.s[a] += r * sv[a]; overall[a] += r * sv[a]; }
      e.combos.push({ name: Engine.comboStr(c), reach: r, s: sv }); total += r;
    }
    for (const k in classes) { const e = classes[k]; for (let a = 0; a < na; a++) e.s[a] /= e.w; }
    for (let a = 0; a < na; a++) overall[a] /= total || 1;
    const sub = T.level === 0 ? (limpers ? `vs ${limpers} limp${limpers > 1 ? 's' : ''}` : 'first in')
      : `vs ${raiser ? raiser.pos : ''} ${T.level === 1 ? 'raise' : `${T.level + 1}-bet`}${vsAllin ? ' (all-in)' : ''}`;
    const curIndex = ctx.cur ? opts.findIndex((o) => o.k === ctx.cur.k) : -1;
    return { kind: 'action', preflop: true, name: seat.pos, sub, board: [], pot: potNow(), history: [],
      actions: opts.map((o) => o.t), actionTypes: opts.map((o) => (o.k === 'fold' ? 'fold' : o.k === 'raise' ? 'bet' : o.k === 'allin' ? 'allin' : 'call')),
      overall, classes, totalReach: total, cur: ctx.cur ? ctx.cur.k : null, curIndex,
      pick: (i) => { const o = opts[i], e = { id: ctx.id, k: o.k }; choose(ctx.mode === 'edit' ? [...S.hist.slice(0, ctx.idx), e, ...S.hist.slice(ctx.idx + 1)] : [...ctx.base, e]); } };
  });
}
function choose(newHist) {
  view = 'pre'; S.hist = newHist; replay();
  const nx = nextActor(); S.sel = nx ? nx.id : null;
  render(); markStale();
}

// ---------------------------------------------------------------- render: seats and action rows
let view = 'pre', preReport = null, customFor = null;
function render() {
  const nx = nextActor();
  const box = $('seats'); box.style.setProperty('--n', S.slots.length); box.innerHTML = '';
  for (const slot of S.slots) {
    const seat = slot.active ? seatById(slot.id) : null, b = document.createElement('button');
    if (!seat) {
      b.className = 'seat out'; b.innerHTML = '<b>Empty</b><small>sitting out</small>';
    } else {
      const isNow = nx === seat, lastAgg = seat.acts.some((a) => a.k === 'raise' || a.k === 'allin');
      b.className = `seat ${seat.folded ? 'fold' : lastAgg ? 'agg' : seat.acts.length ? 'pass' : ''} ${isNow ? 'now' : ''} ${S.sel === seat.id ? 'sel' : ''}`;
      const txt = seat.acts.length ? seat.acts.map((a) => a.label).join(' · ') : isNow ? 'to act' : seat.pos === 'SB' ? 'SB 0.5' : seat.pos === 'BB' ? 'BB 1' : '–';
      b.innerHTML = `<b>${seat.pos}</b><small>${txt}</small>`;
      b.setAttribute('aria-pressed', S.sel === seat.id);
      b.onclick = () => { if (b.dataset.lp) { delete b.dataset.lp; return; } S.sel = S.sel === seat.id ? null : seat.id; customFor = null; view = 'pre'; render(); };
    }
    // sit out / sit back in: right-click or long-press (custom table)
    const toggle = () => {
      if (S.table !== 'custom') return;
      const activeN = S.slots.filter((x) => x.active).length;
      if (slot.active && activeN <= 2) return;
      slot.active = !slot.active; if (S.sel === slot.id) S.sel = null;
      replay(); render(); markStale();
    };
    b.addEventListener('contextmenu', (e) => { e.preventDefault(); toggle(); });
    let lp = null;
    b.addEventListener('pointerdown', (e) => { if (e.pointerType !== 'mouse') lp = setTimeout(() => { b.dataset.lp = '1'; toggle(); }, 550); });
    ['pointerup', 'pointerleave', 'pointercancel'].forEach((ev) => b.addEventListener(ev, () => clearTimeout(lp)));
    if (!seat) b.onclick = () => {};
    box.appendChild(b);
  }
  $('seatHint').hidden = S.table !== 'custom';

  const pr = $('prompt'); pr.innerHTML = '';
  const seat = S.sel !== null && S.sel !== undefined ? seatById(S.sel) : null;
  const addRow = (label, opts, curK, onPick, rowKey) => {
    const row = document.createElement('div'); row.className = 'prow';
    row.innerHTML = `<span class="label">${label}</span>`;
    for (const o of opts) {
      const b = document.createElement('button'); b.className = `b-${o.cls}${curK === o.k ? ' cur' : ''}`; b.textContent = o.t;
      b.setAttribute('aria-pressed', curK === o.k); b.onclick = () => onPick(o); row.appendChild(b);
    }
    if (opts.some((o) => o.k === 'raise' || o.k === 'allin')) {
      const cb = document.createElement('button'); cb.className = 'b-agg'; cb.textContent = 'Custom…';
      cb.onclick = () => { customFor = customFor === rowKey ? null : rowKey; render(); }; row.appendChild(cb);
      if (customFor === rowKey) {
        const w = document.createElement('span'); w.className = 'custom';
        const minTo = withHistMin(rowKey);
        w.innerHTML = `<span class="muted">Raise to</span><input id="customTo" type="number" step="0.5" min="${minTo}" value="${fmt(minTo)}" aria-label="Custom raise size in bb"><span class="muted">bb</span>`;
        const ok = document.createElement('button'); ok.className = 'primary'; ok.textContent = 'Set';
        ok.onclick = () => { const v = parseFloat($('customTo').value); if (!(v > 0)) return; customFor = null; onPick({ k: v >= S.stack - 1e-9 ? 'allin' : 'raise', to: v, custom: true }); };
        w.appendChild(ok); row.appendChild(w);
      }
    }
    pr.appendChild(row);
  };
  if (seat) {
    const head = document.createElement('div'); head.className = 'phead'; head.innerHTML = `<span class="who">${seat.pos}</span><span class="muted mono">${fmt(S.stack - seat.put)} bb behind</span>`;
    pr.appendChild(head);
    const mine = S.decisions.filter((d) => d.id === seat.id);
    mine.forEach((d, n) => addRow(n === 0 ? 'First action' : `Decision ${n + 1}`, d.opts, d.k,
      (o) => choose([...S.hist.slice(0, d.idx), entry(seat.id, o), ...S.hist.slice(d.idx + 1)]), `d${d.idx}`));
    if (nx === seat) addRow(mine.length ? `Decision ${mine.length + 1}` : 'To act', options(seat), null, (o) => choose([...S.hist, entry(seat.id, o)]), 'next');
    else if (!mine.length && !seat.folded) {
      const f = nx ? foldsTo(seat.id) : null;
      if (f) { const opts = withHist([...S.hist, ...f], () => options(seatById(seat.id))); addRow(f.length ? 'Folds to you' : 'To act', opts, null, (o) => choose([...S.hist, ...f, entry(seat.id, o)]), 'foldto'); }
      else pr.insertAdjacentHTML('beforeend', '<div class="prow muted">This seat does not act again preflop.</div>');
    }
  }
  const foot = document.createElement('div'); foot.className = 'pfoot';
  foot.innerHTML = nx
    ? `<span class="muted">Next to act: <b>${nx.pos}</b></span><span class="muted mono">pot ${fmt(potNow())} bb</span><span class="grow"></span>`
    : `<span class="who" style="font-size:16px">${live().length >= 2 ? `${live().length} players to the flop` : 'Hand over preflop'}</span><span class="muted mono">pot ${fmt(potNow())} bb</span><span class="grow"></span>`;
  const clr = document.createElement('button'); clr.textContent = 'Clear action'; clr.onclick = () => { S.hist = []; S.overrides = {}; replay(); const n2 = nextActor(); S.sel = n2 ? n2.id : null; render(); markStale(); };
  foot.appendChild(clr); pr.appendChild(foot);
  renderBoard(); renderInHand();
  preReport = preflopReport();
  setView(view);
}
function entry(id, o) { return o.custom ? { id, k: o.k, to: o.to } : { id, k: o.k }; }
function withHistMin(rowKey) {
  if (rowKey.startsWith('d')) { const idx = +rowKey.slice(1); return withHist(S.hist.slice(0, idx), () => Math.max(minRaiseTo(), 1)); }
  if (rowKey === 'foldto') { const f = foldsTo(S.sel) || []; return withHist([...S.hist, ...f], () => minRaiseTo()); }
  return minRaiseTo();
}

// ---------------------------------------------------------------- board (exact cards only)
let activeSlot = null;
function renderBoard() {
  const box = $('slots'); box.innerHTML = '';
  for (let i = 0; i < 5; i++) {
    const c = S.board[i], b = document.createElement('button');
    if (c !== undefined) { const s = SUITS[c & 3]; b.className = `slot filled s-${s}`; b.textContent = RANKS[c >> 2] + SYM[s]; b.title = 'Change or remove this card'; }
    else { b.className = 'slot empty'; b.textContent = i < 3 ? 'Flop' : i === 3 ? 'Turn' : 'River'; }
    if (activeSlot === i) b.classList.add('active');
    b.disabled = i > S.board.length;
    b.onclick = () => { activeSlot = activeSlot === i ? null : i; renderBoard(); };
    box.appendChild(b);
  }
  const pk = $('picker'); pk.hidden = activeSlot === null; pk.innerHTML = '';
  if (activeSlot === null) return;
  if (S.board[activeSlot] !== undefined) {
    const rmb = document.createElement('button'); rmb.textContent = 'Remove'; rmb.style.gridColumn = 'span 3'; rmb.style.fontSize = '12px';
    rmb.onclick = () => { S.board = S.board.slice(0, activeSlot); activeSlot = null; renderBoard(); markStale(); }; pk.appendChild(rmb);
    for (let i = 0; i < 10; i++) pk.appendChild(document.createElement('span'));
  }
  for (let s = 3; s >= 0; s--) for (let r = 12; r >= 0; r--) {
    const c = r * 4 + s, b = document.createElement('button');
    b.className = `card s-${SUITS[s]}`; b.textContent = RANKS[r] + SYM[SUITS[s]]; b.setAttribute('aria-label', Engine.cardStr(c));
    b.disabled = S.board.includes(c) && S.board[activeSlot] !== c;
    b.onclick = () => { S.board[activeSlot] = c; activeSlot = S.board.length < 3 ? S.board.length : null; renderBoard(); markStale(); };
    pk.appendChild(b);
  }
}
function renderInHand() {
  const box = $('inhand'); box.innerHTML = '';
  if (nextActor()) return;
  for (const s of postflopOrder()) {
    const w = rw(rangeFor(s)); let n = 0; for (const x of w) n += x;
    const d = document.createElement('span'); d.className = 'pchip';
    d.innerHTML = `<b>${s.pos}</b><span class="muted mono">${fmt(S.stack - s.put)}bb · ${(n / 13.26).toFixed(0)}%</span>`;
    const b = document.createElement('button'); b.textContent = S.overrides[s.id] ? 'Range ✎' : 'Range'; b.onclick = () => openRange(s);
    d.appendChild(b); box.appendChild(d);
  }
}

// ---------------------------------------------------------------- range editor
const CLASS_N = (c) => (c.length === 2 ? 6 : c[2] === 's' ? 4 : 12);
function classAt(i, j) { const r1 = 12 - i, r2 = 12 - j; return i === j ? RANKS[r1] + RANKS[r2] : i < j ? RANKS[r1] + RANKS[r2] + 's' : RANKS[r2] + RANKS[r1] + 'o'; }
function classWeights(text) {
  const w = rw(text), acc = {};
  for (let c = 0; c < 1326; c++) { const k = Engine.comboClass(c); (acc[k] = acc[k] || [0, 0]); acc[k][0] += w[c]; acc[k][1]++; }
  const r = {}; for (const k in acc) r[k] = acc[k][0] / acc[k][1]; return r;
}
let rm = null;
function openRange(seat) {
  rm = { id: seat.id, pos: seat.pos, weights: classWeights(rangeFor(seat)), painting: false, value: 1 };
  $('rmTitle').textContent = `${seat.pos} range`;
  const g = $('rmGrid'); g.innerHTML = '';
  for (let r = 0; r < 13; r++) for (let c = 0; c < 13; c++) { const k = classAt(r, c), d = document.createElement('div'); d.className = 'rcell'; d.dataset.k = k; d.innerHTML = `<div class="w"></div><span>${k}</span>`; g.appendChild(d); }
  drawRange(); $('rangeModal').hidden = false;
}
function drawRange() {
  let n = 0;
  $('rmGrid').querySelectorAll('.rcell').forEach((d) => { const w = rm.weights[d.dataset.k] || 0; n += w * CLASS_N(d.dataset.k); d.querySelector('.w').style.height = `${w * 100}%`; d.classList.toggle('on', w >= 0.5); });
  $('rmCount').textContent = `${n.toFixed(0)} combos · ${(n / 13.26).toFixed(1)}%`;
}
function paintAt(x, y) { const el = document.elementFromPoint(x, y)?.closest('.rcell'); if (el) { rm.weights[el.dataset.k] = rm.value; drawRange(); } }
$('rmGrid').addEventListener('pointerdown', (e) => { const el = e.target.closest('.rcell'); if (!el) return; const w = +$('rmWeight').value / 100, cur = rm.weights[el.dataset.k] || 0; rm.value = Math.abs(cur - w) < 1e-6 ? 0 : w; rm.painting = true; paintAt(e.clientX, e.clientY); });
window.addEventListener('pointermove', (e) => { if (rm && rm.painting) paintAt(e.clientX, e.clientY); });
window.addEventListener('pointerup', () => { if (rm) rm.painting = false; });
$('rmWeight').addEventListener('input', (e) => { $('rmWeightVal').textContent = `${e.target.value}%`; });
$('rmReset').onclick = () => { const o = S.overrides[rm.id]; delete S.overrides[rm.id]; rm.weights = classWeights(rangeFor(seatById(rm.id))); if (o) S.overrides[rm.id] = o; rm.reset = true; drawRange(); };
$('rmClear').onclick = () => { rm.weights = {}; drawRange(); };
$('rmCancel').onclick = () => { $('rangeModal').hidden = true; rm = null; };
$('rmDone').onclick = () => {
  const parts = [];
  for (let r = 0; r < 13; r++) for (let c = 0; c < 13; c++) { const k = classAt(r, c), w = rm.weights[k] || 0; if (w > 0) parts.push(w >= 0.999 ? k : `${k}:${+w.toFixed(2)}`); }
  if (rm.reset && !parts.length) delete S.overrides[rm.id]; else S.overrides[rm.id] = parts.join(',');
  $('rangeModal').hidden = true; rm = null; renderInHand(); markStale();
};
document.addEventListener('keydown', (e) => { if (e.key === 'Escape' && !$('rangeModal').hidden) $('rmCancel').click(); });
function setView(v) { view = v; $('tabPre').setAttribute('aria-pressed', v === 'pre'); $('tabPost').setAttribute('aria-pressed', v === 'post'); renderNode(); }

