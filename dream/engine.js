// ================================================================ THE DREAM: engine
// State, money, chapters, unlocks, and playing an event: solver-graded decisions decide your
// stack, your finish and your payout.

var DR = null;
const drMoney = (n) => Math.round(n).toLocaleString('en-US');
const drMoneyShort = (n) => (n >= 1e6 ? `${+(n / 1e6).toFixed(n >= 1e7 ? 1 : 2)}M` : n >= 1e4 ? `${Math.round(n / 1000)}K` : drMoney(n));
const drOrd = (n) => { const v = n % 100; return n + (v >= 11 && v <= 13 ? 'th' : ['th', 'st', 'nd', 'rd'][n % 10] || 'th'); };
const drPaid = (ev) => (ev.kind === 'sat' ? ev.seats : ev.kind === 'sng' ? ev.paid : typicalPayouts(ev.field, ev.prizeBase || ev.buyin || 1, 0).paid);
const drTicket = (s, ev) => !!(s.tickets && s.tickets[ev.id] > 0);
const drTitleCount = (s) => Object.values(s.titles).reduce((a, b) => a + b, 0);
const drStakeSize = (s) => [300, 300, 1500, 6000, 25000, 120000, 60000, 150000][Math.min(7, s.ch)];

function drDefault() {
  return { v: 1, made: false, name: '', look: { ...DR_LOOK_DEFAULT }, br: 0, peak: 0, ch: 0, story: {}, queue: [], job: null,
    titles: { local: 0, room: 0, ring: 0, bracelet: 0, tour: 0, glo: 0, titan: 0, me: 0 }, legends: {}, met: {}, events: {},
    dec: 0, perfect: 0, good: 0, close: 0, mistakes: 0, played: 0, cashes: 0, ft: 0, flags: {},
    eq: { outfit: 'office', eyes: 'readers', head: 'none', ear: 'none', ring: 'none', wrist: 'none', watch: 'digital', chain: 'none', home: 'studio', ride: 'bus', patch: 'none', sleeve: 'none' }, dye: {},
    owned: {}, newItems: [], debt: 0, ledger: [], trophies: [], lastEvent: null };
}
function drLoad() {
  try {
    const v = JSON.parse(localStorage.getItem(DR_KEY) || 'null');
    if (!v || v.v !== 1) return drDefault();
    const d = drDefault();
    return { ...d, ...v, titles: { ...d.titles, ...(v.titles || {}) }, eq: { ...d.eq, ...(v.eq || {}) }, look: { ...d.look, ...(v.look || {}) }, dye: { ...(v.dye || {}) }, flags: v.flags || {} };
  } catch { return drDefault(); }
}
function drSave() { try { localStorage.setItem(DR_KEY, JSON.stringify(DR.st)); } catch { /* storage unavailable */ } }

function drAdd(s, amt, why) {
  s.br = Math.max(0, Math.round(s.br + amt)); s.peak = Math.max(s.peak, s.br);
  s.ledger.push({ d: Date.now(), amt: Math.round(amt), why }); if (s.ledger.length > 200) s.ledger.splice(0, s.ledger.length - 200);
}
// profit pays back Slick's stake first (half of it until the debt is gone)
function drRepay(s, profit) {
  if (s.debt <= 0 || profit <= 0) return 0;
  const pay = Math.min(s.debt, Math.round(profit / 2)); s.debt -= pay; drAdd(s, -pay, 'Paid Slick back'); return pay;
}

// ---------------------------------------------------------------- Player of the Year race (this month)
// You earn points for cashes and titles; the legends of your chapter are racing too (simulated until online play).
const drMonth = (d = new Date()) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
function drPoyPoints(ev, place) {
  const P = drPaid(ev); if (!place || place > P || ev.kind !== 'mtt' || ev.daily) return 0;
  return Math.round(60 * Math.log10(ev.field) * (1 + (P - place) / P) + (place === 1 ? 150 : place <= 9 ? 50 : 0));
}
function drPoyBoard(s) {
  const m = drMonth(), day = new Date().getDate(), rows = [];
  const pool = DR_LEGENDS.filter((L) => L.ch <= Math.max(2, s.ch) && L.ch >= Math.max(1, s.ch - 2));
  for (const L of pool) {
    let h = 2166136261; for (const ch of L.id + m) { h ^= ch.charCodeAt(0); h = Math.imul(h, 16777619); }
    const rate = 40 + ((h >>> 0) % 120), pts = Math.round(rate * day * (0.6 + ((h >>> 8) % 100) / 125) * Math.min(1, 0.12 + 0.13 * s.ch));
    rows.push({ n: L.name, nick: L.nick, p: pts });
  }
  rows.push({ n: s.name, p: (s.poy && s.poy[m]) || 0, me: true });
  return rows.sort((a, b) => b.p - a.p);
}

// ---------------------------------------------------------------- unlocks
function drItemKey(it) { return it.slot + ':' + it.id; }
// the story so far: big moments, newest first in the Story tab
function drMoment(s, icon, text) { s.timeline = s.timeline || []; s.timeline.push({ d: Date.now(), icon, text }); if (s.timeline.length > 150) s.timeline.splice(0, s.timeline.length - 150); }
function drUnlocks(s) {
  const fresh = [];
  for (const it of DR_ITEMS) {
    const k = drItemKey(it);
    if (s.owned[k] || !it.test(s)) continue;
    s.owned[k] = true;
    if (it.req && it.req !== 'Where it all started' && it.id !== 'none') fresh.push(it);
    // wear the newest thing you earn: later in the list is the upgrade
    const cur = DR_ITEMS.findIndex((x) => x.slot === it.slot && x.id === s.eq[it.slot]), mine = DR_ITEMS.indexOf(it);
    // never swap out league gear you chose to wear
    const wearingLeague = DR_LG_ITEMS.some((x) => x.slot === it.slot && x.id === s.eq[it.slot]);
    if (it.id !== 'none' && mine > cur && it.slot !== 'head' && it.slot !== 'sleeve' && !wearingLeague) s.eq[it.slot] = it.id;
  }
  s.newItems = [...(s.newItems || []), ...fresh.map(drItemKey)];
  return fresh;
}
const drOwns = (s, slot, id) => !!s.owned[slot + ':' + id];
function drRep(s) { for (const [t, label] of DR_REPUTATION) if (t(s)) return typeof label === 'function' ? label(s) : label; return 'Dreamer'; }

// ---------------------------------------------------------------- chapters
const drToday = (d = new Date()) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
const drDailyDone = (s) => !!(s.daily && s.daily[drToday()]);
// the daily freeroll pays like a small tournament at your level, funded by the sponsor
function drDailyEvent(s) { return { ...DR_DAILY, prizeBase: [15, 15, 60, 250, 1000, 4000, 2000, 5000][Math.min(7, s.ch)] }; }
function drAvailable(s) {
  if (s.ch === 0) return DR_EVENTS.filter((e) => e.id === 'local');
  return DR_EVENTS.filter((e) => e.ch >= 1 && e.ch <= s.ch);
}
function drAfford(s, ev) { return ev.free || drTicket(s, ev) || s.br >= ev.buyin; }
function drRisk(s, ev) {
  if (ev.free) return ['free', 'Free seat'];
  if (drTicket(s, ev)) return ['free', '🎟 You have a seat'];
  if (s.br < ev.buyin) return ['no', `Need $${drMoney(ev.buyin)}`];
  const r = ev.buyin / Math.max(1, s.br);
  return r <= 0.05 ? ['safe', 'Safe for your roll'] : r <= 0.2 ? ['shot', 'Taking a shot'] : ['yolo', 'Betting the dream'];
}
// after anything happens: next chapter, rock bottom, first title
function drProgress(s) {
  if (s.ch >= 1 && s.ch < 7 && DR_CHAPTERS[s.ch].test(s) && !s.story['c' + (s.ch + 1)] && !s.queue.includes('c' + (s.ch + 1))) s.queue.push('c' + (s.ch + 1));
  if (s.ch >= 1) {
    const live = drAvailable(s).filter((e) => !e.free), cheapest = Math.min(...live.map((e) => e.buyin));
    if (s.br < cheapest && !s.queue.includes('broke')) {
      const stake = Math.max(drStakeSize(s), cheapest - s.br);
      drAdd(s, stake, "Slick's stake"); s.debt += stake; drMoment(s, '🥧', `Went broke. Slick staked you $${drMoney(stake)}`); s.flags.stakes = (s.flags.stakes || 0) + 1; s.queue.push('broke');
    }
  }
}

// ---------------------------------------------------------------- an event
const DR_STAGE = { early: { gid: 'ring', label: 'Early', long: 'Early levels' }, middle: { gid: 'ring', label: 'Middle', long: 'Middle levels' }, bubble: { gid: 'bubble', label: 'Bubble', long: 'The bubble' },
  deep: { gid: 'shove', label: 'Deep', long: 'Deep run' }, satbubble: { gid: 'sat', label: 'Bubble', long: 'Satellite bubble' }, final: { gid: 'shove', label: 'Final', long: 'Final table' }, cash: { gid: 'cash', label: 'Cash', long: 'Cash session' } };
// bigger fields mean longer days: more levels to survive before the final table.
// The story also moves the focus: early chapters are mostly deep-stacked early levels,
// the middle chapters live in the middle stages, and the late chapters are ICM poker
// (bubble, deep run, final table).
function drStagesFor(ev) { return drPlanFor(ev).stages; }
function drPlanFor(ev, ch) {
  if (ev.kind === 'sng') return { stages: ev.stagesFix.slice() };
  if (ev.kind === 'cash') return { stages: ['cash'] };
  if (ev.kind === 'sat') return { stages: ['early', 'satbubble'] };
  const c = ch == null ? ((typeof DR !== 'undefined' && DR.st && DR.st.ch) || 0) : ch, big = ev.field >= 1500;
  const plan = c <= 1 ? [['early', 4], ['middle', 2], ['bubble', 1], ['final', 1]]
    : c === 2 ? [['early', 2], ['middle', 4], ['bubble', 1], ['final', 1]]
    : c === 3 ? [['early', 1], ['middle', 3], ['bubble', 2], ['final', 2]]
    : c === 4 ? [['early', 1], ['middle', 1], ['bubble', 3], ...(big ? [['deep', 2]] : []), ['final', 3]]
    : [['early', 1], ['bubble', 3], ['deep', 2], ['final', 4]];
  return { stages: plan.map((p) => p[0]), perS: plan.map((p) => p[1]) };
}
// antes: online (CF Tilt Poker, the Daily Grind, the online sit & go modes) = every player antes;
// live events (card room, casino, Vegas, the tours, the Main) = one big-blind ante
function drLiveEv(ev) { return !(ev.online || ev.daily || ev.kind === 'sng'); }
// hands to play at the current stage
function drPer(run) { return run.per || (run.perS && run.perS[Math.min(run.si, run.perS.length - 1)]) || (run.ev.kind === 'cash' ? DR_CASH_HANDS : DR_HANDS); }
// which pre-solved tables fit a stage: early levels are deep stacks, the final table is the last nine
const DR_STAGE_FIT = { early: (t) => t.avg >= 32, middle: (t) => t.avg < 32, deep: (t) => t.left > 9, final: (t) => t.left <= 9 };
// poker is poker: even perfect play gets coolered sometimes. A bigger stack survives more of them.
const DR_COOLERS = [
  'Kings into aces. All the chips go in, and you never had a chance.',
  'You flop a set. He flops the higher set. That is the whole story.',
  'Ace-king versus queens, a coin flip for your tournament. The coin lands on its edge, then on queens.',
  'Your all-in is called by the one hand in his range that beats you.',
  'Runner-runner. You did nothing wrong. The deck did.',
];
function drCoolerRoll(run) {
  const ev = run.ev, p = Math.max(0.02, Math.min(0.22, 0.05 + 0.025 * Math.log10(ev.field) - (run.stack - 100) / 700));
  return run.rng() < p;
}

function drPickBoss(s, ev, rng) {
  if (ev.boss) return ev.boss;
  if (!ev.legends) return null;
  const fresh = ev.legends.filter((id) => !s.legends[id]), pool = fresh.length ? fresh : ev.legends;
  return pool[(rng() * pool.length) | 0];
}
function drStart(id) {
  const s = DR.st, ev = id === 'daily' ? drDailyEvent(s) : drEvent(id);
  if (DR.run || !ev || !drAfford(s, ev)) return;
  if (ev.daily && drDailyDone(s)) return;
  if (drTicket(s, ev)) { s.tickets[ev.id]--; s.ledger.push({ d: Date.now(), amt: 0, why: `Satellite seat: ${ev.name}` }); DR.usedTicket = true; }
  else { DR.usedTicket = false; if (!ev.free) drAdd(s, -ev.buyin, `Buy-in: ${ev.name}`); }
  s.lastEvent = id; drSave();
  const seed = ev.daily ? `daily|${drToday()}` : `${id}|${Date.now()}|${Math.random()}`, rng = arRng(seed);
  DR.saved = snapSetup(); TR.random = true; window.__trRand = true;
  if (S.variant !== 'nlh') setVariant('nlh');
  const plan = drPlanFor(ev);
  DR.run = { ev, stages: plan.stages, perS: plan.perS, si: 0, h: 0, stack: 100, results: [], total: 0, rng,
    state: 'loading', spot: null, boss: drPickBoss(s, ev, rng), fails: 0, bust: false, combo: 0, t0: performance.now(), tip: 0 };
  DR.view = 'run'; drRender(); drTable();
}
async function drTable() {
  const run = DR.run; if (!run) return;
  run.state = 'loading'; run.spot = null; drRender();
  const stage = run.stages[run.si];
  if (run.ev.review) {
    // the mistake deck: rebuild the exact table you got wrong
    const item = run.queue[run.si], t = item && drLib()[item.g] && drLib()[item.g][item.t];
    if (!t) { drFinish(); return; }
    window.__pfHold = true; applySetup(t.s);
    run.lib = t; run.libRef = { g: item.g, t: item.t }; run.libSeats = withHist([], () => T.seats.map((x) => ({ id: x.id, pos: x.pos })));
    await sleep(350); if (DR.run !== run) return;
    run.state = 'ready'; drRender(); return;
  }
  const libGid = DR_STAGE[stage].gid, pool = typeof drLib === 'function' && drLib()[libGid];
  if (pool && pool.length) {
    // a pre-solved table: no waiting, no phone CPU, answers from a long offline solve
    run.usedTables = run.usedTables || [];
    const fit = DR_STAGE_FIT[stage], bb = drLiveEv(run.ev), anteOk = (i) => !pool[i].s.t222 || !!pool[i].s.t222.bbAnte === bb;
    const all = pool.map((_, i) => i).filter((i) => anteOk(i) && (!fit || !pool[i].s.t222 || fit(pool[i].s.t222))), any = pool.map((_, i) => i).filter(anteOk), okI = all.length ? all : any.length ? any : pool.map((_, i) => i);
    const fresh = okI.filter((i) => !run.usedTables.includes(libGid + i)), pickFrom = fresh.length ? fresh : okI;
    const ti = pickFrom[(run.rng() * pickFrom.length) | 0]; run.usedTables.push(libGid + ti);
    window.__pfHold = true; applySetup(pool[ti].s);
    run.lib = pool[ti]; run.libRef = { g: libGid, t: ti }; run.seenNodes = new Set(); run.libSeats = withHist([], () => T.seats.map((x) => ({ id: x.id, pos: x.pos })));
    await sleep(500); if (DR.run !== run) return;
    run.state = 'ready'; drRender(); return;
  }
  run.lib = null; window.__pfHold = false;
  for (let attempt = 0; attempt < 3; attempt++) {
    const gid = DR_STAGE[stage].gid, setup = arSetup(gid === 'sat' ? 'bubble' : gid, run.rng); if (setup.t222) setup.t222.bbAnte = drLiveEv(run.ev);
    // satellite bubble: every seat pays the same, so ICM squeezes hardest here
    if (gid === 'sat') { const t = setup.t222; t.payouts = Array(Math.max(1, t.paid)).fill('1').join(','); t.pko = { on: false, b: {}, frac: 75 }; }
    applySetup(setup); run.t0 = performance.now();
    while (performance.now() - run.t0 < (window.__drSolveMs || AR_SOLVE_MS)) {
      await sleep(250); if (DR.run !== run) return;
      if (pfState.done || pfState.error || pfState.deep) break;
      const bar = $('drLoadBar'); if (bar) bar.style.width = `${Math.min(100, ((performance.now() - run.t0) / AR_SOLVE_MS) * 100)}%`;
    }
    if (!pfState.error && !pfState.deep) break;
  }
  run.state = 'ready'; drRender();
}
async function drHand() {
  const run = DR.run; if (!run) return;
  run.state = 'dealing'; drRender();
  if (run.lib) {
    await sleep(350); if (DR.run !== run) return;
    if (run.ev.review) { const it = run.queue[run.si], node = run.lib.n[it.n], row = node && node.r.find((r) => r[0] === it.c);
      if (!row || node.p.join(',') !== it.p) { drFinish(); return; }
      run.spot = drBuildSpot(run, node, it.n, row, 0.5); }
    else run.spot = drLibDeal(run, drDifficulty(run));
    run.state = 'play'; drRender(); return;
  }
  const sp = await drDeal(run.rng, drDifficulty(run)); if (DR.run !== run) return;
  if (!sp) { if (++run.fails > 3) { drFinish(); return; } drTable(); return; }
  run.spot = sp; run.state = 'play'; drRender();
}
// ---------------------------------------------------------------- picking the hand you're dealt
// Every hand is a real decision. At your seat the solver ranks all 169 starting hands by how close
// the right play is to the next-best play (the EV gap). Trash that's an instant fold and monsters that
// play themselves are never dealt. Difficulty narrows the pool: early in the story you get clear but
// meaningful spots (AJo under the gun, 99 facing a jam); later only the razor-thin ones.
let DR_COMBOS = null;
function drCombosOf(cls) {
  if (!DR_COMBOS) { DR_COMBOS = Array.from({ length: 169 }, () => []); for (let a = 1; a < 52; a++) for (let b = 0; b < a; b++) DR_COMBOS[Engine.CLASS_OF[Engine.comboIndex(a, b)]].push([a, b]); }
  return DR_COMBOS[cls];
}
// 0 = the local card room, 1 = the Hall of Legends
function drDifficulty(run) {
  const ev = run.ev, stage = run.stages[Math.min(run.si, run.stages.length - 1)];
  let d;
  if (ev.kind === 'sng') d = ev.mode === 'casual' ? 0.4 : ev.mode === 'league' ? 0.6 : Math.max(0.3, Math.min(1, 0.35 + ((DR.st.rk || { r: 1000 }).r - 1000) / 1500));
  else if (ev.daily) d = 0.15 + 0.11 * Math.min(7, DR.st.ch);
  else d = [0.12, 0.22, 0.36, 0.5, 0.64, 0.78, 0.9, 1][Math.min(7, ev.id === 'main' ? 6 : ev.ch || 0)];
  if (stage === 'final' || stage === 'deep') d += 0.08;
  if (run.boss) d += 0.05;
  return Math.max(0, Math.min(1, d));
}
function drCandidates(res, d) {
  const rows = [];
  for (let c = 0; c < 169; c++) {
    const st = res.strat[c], ev = res.ev && res.ev[c]; if (!st || !ev) continue;
    const top = Math.max(...st); if (top < 0.6) continue; // a coin-flip mix has no "right answer" to learn
    const e = ev.slice().sort((x, y) => y - x), gap = e[0] - (e[1] == null ? e[0] : e[1]);
    if (!(gap >= 0)) continue;
    rows.push({ c, gap, top });
  }
  rows.sort((x, y) => x.gap - y.gap);
  const n = Math.max(8, Math.round(42 - 32 * d)); // easy: the 42 closest hands; legend: the 10 closest
  return rows.slice(0, n);
}
// a hand from a pre-solved table: a real situation (weighted by how often it happens), then a hand that
// makes it a decision. Each situation's hands are stored hardest-first; difficulty picks the window.
function drLibDeal(run, d) {
  const lib = run.lib, rng = run.rng, seen = (run.seenNodes = run.seenNodes || new Set());
  const drill = run.ev.drill && run.ev.drill !== 'icm' ? run.ev.drill : null, kindOk = (x) => !drill || drNodeKind(x.n) === drill;
  let nodes = lib.n.map((n, i) => ({ n, i })).filter((x) => !x.n.bad && !seen.has(x.i) && kindOk(x));
  if (!nodes.length) nodes = lib.n.map((n, i) => ({ n, i })).filter((x) => !x.n.bad && kindOk(x));
  // a drill table with none of that spot: look across every table of this stage
  if (!nodes.length && drill) { const pool = drLib()[DR_STAGE[run.stages[run.si]].gid] || []; for (const t of pool) { if (t.s.t222 && !!t.s.t222.bbAnte !== drLiveEv(run.ev)) continue; t.n.forEach((n, i) => { if (!n.bad && drNodeKind(n) === drill) nodes.push({ n, i: -1, t }); }); } }
  const list = nodes.length ? nodes : lib.n.map((n, i) => ({ n, i })).filter((x) => !x.n.bad);
  const tot = list.reduce((a, x) => a + Math.sqrt(x.n.w), 0); let x = rng() * tot, pick = list[0];
  for (const it of list) { x -= Math.sqrt(it.n.w); if (x < 0) { pick = it; break; } }
  seen.add(pick.i);
  if (pick.t && pick.t !== run.lib) { const g = DR_STAGE[run.stages[run.si]].gid; window.__pfHold = true; applySetup(pick.t.s); run.lib = pick.t; run.libRef = { g, t: drLib()[g].indexOf(pick.t) }; run.seenNodes = new Set(); run.libSeats = withHist([], () => T.seats.map((x) => ({ id: x.id, pos: x.pos }))); }
  const node = pick.n, lib2 = run.lib;
  // half the time the right play is to fold, half the time it's to play: folding everything can't win
  const fi = node.a.indexOf('fold'), isFold = (r) => r[1].indexOf(Math.max(...r[1])) === fi;
  const folds = node.r.filter(isFold), plays = node.r.filter((r) => !isFold(r));
  const want = rng() < 0.5 ? plays : folds, rows = want.length >= 2 ? want : node.r;
  // rows run hardest-first: difficulty slides the window from the clear end toward the razor-thin end
  const L = rows.length, start = Math.round((1 - d) * 0.55 * L), width = Math.max(2, Math.round(0.45 * L));
  const window_ = rows.slice(Math.max(0, Math.min(start, L - 2)), Math.min(L, start + width));
  const row = window_[(rng() * window_.length) | 0];
  return drBuildSpot(run, node, pick.i >= 0 ? pick.i : lib2.n.indexOf(node), row, d);
}
// a spot from a library table: your hand from the row's class, everyone else random
function drBuildSpot(run, node, ni, row, d) {
  const rng = run.rng, lib2 = run.lib, [cls, strat] = row;
  const combos = drCombosOf(cls), [a, b] = combos[(rng() * combos.length) | 0];
  const deck = Array.from({ length: 52 }, (_, i) => i).filter((c) => c !== a && c !== b);
  for (let i = deck.length - 1; i > 0; i--) { const j = (rng() * (i + 1)) | 0; [deck[i], deck[j]] = [deck[j], deck[i]]; }
  const seats = run.libSeats, hands = seats.map((_, i) => (i === node.h ? [a, b] : [deck[2 * i], deck[2 * i + 1]]));
  const st = [], evs = []; st[cls] = strat; evs[cls] = row[2] || null;
  const idx = node.r.indexOf(row), hard = node.r.length > 1 ? 1 - idx / (node.r.length - 1) : 0.5;
  return { grid: node.g || null, ref: run.libRef ? { ...run.libRef, n: ni, c: cls, p: node.p.join(',') } : null, seats, hands, hero: node.h, path: node.p.map((k) => ({ k })), res: { actions: node.a.map((k) => ({ k })), strat: st, ev: evs }, cls, diff: d, hard, unit: lib2.u === 'icm' && lib2.k ? 'bbicm' : lib2.u || 'bb', evScale: lib2.u === 'icm' && lib2.k ? lib2.k : 1 };
}
async function drDeal(rng, d = 0.3) {
  const seats = withHist([], () => T.seats.map((x) => ({ id: x.id, pos: x.pos })));
  const clsOf = (h) => Engine.CLASS_OF[Engine.comboIndex(h[0], h[1])];
  let fallback = null;
  for (let tries = 0; tries < 30; tries++) {
    const deck = Array.from({ length: 52 }, (_, i) => i);
    for (let i = 0; i < seats.length * 2; i++) { const j = i + ((rng() * (52 - i)) | 0); [deck[i], deck[j]] = [deck[j], deck[i]]; }
    const hands = seats.map((_, i) => [deck[2 * i], deck[2 * i + 1]]);
    const hero = (rng() * seats.length) | 0, path = [];
    let res = null;
    for (let step = 0; step < 40; step++) {
      res = await pfQueryAsync(path);
      if (!res.ok || res.terminal || res.player === hero) break;
      const strat = res.strat[clsOf(hands[res.player])]; let x = rng(), a = strat.length - 1;
      for (let i = 0; i < strat.length; i++) { x -= strat[i]; if (x < 0) { a = i; break; } }
      path.push({ k: res.actions[a].k });
    }
    if (!(res && res.ok && !res.terminal && res.player === hero)) continue;
    // real tables, not solver chaos: no three-way all-in pileups in front of you
    let jams = 0, callsAfterJam = 0; for (const e of path) { if (e.k === 'allin') jams++; else if (jams && e.k === 'call') callsAfterJam++; }
    if (jams + callsAfterJam >= 3 || jams >= 2) continue;
    // the big blind with a free check is not a decision
    if (!res.actions.some((a) => a.k === 'fold')) continue;
    const pool = drCandidates(res, d); if (pool.length < 4) continue;
    // deal yourself one of the hands that makes this spot a real decision
    const used = new Set(hands.flat().filter((_, i) => Math.floor(i / 2) !== hero));
    const order = pool.map((r) => r.c).sort(() => rng() - 0.5);
    for (const c of order) {
      const opts = drCombosOf(c).filter(([a, b]) => !used.has(a) && !used.has(b)); if (!opts.length) continue;
      hands[hero] = opts[(rng() * opts.length) | 0].slice();
      const grid = Array.from({ length: 169 }, (_, k) => { const st = res.strat[k]; if (!st) return '.'; const m = Math.max(...st); return m >= 0.55 ? String(st.indexOf(m)) : '.'; }).join('');
      const pi = pool.findIndex((r) => r.c === c), sp = { seats, hands, hero, path, res, cls: c, diff: d, hard: pool.length > 1 ? 1 - pi / (pool.length - 1) : 0.5, grid };
      if (path.length || rng() < 0.45) return sp; // favour spots with action in front of you
      fallback = fallback || sp; break;
    }
  }
  return fallback;
}
// ---------------------------------------------------------------- grading, Chess.com style
// Every decision gets a label from how much value it gave up versus the solver's best play
// (bb, or % of the prize pool when ICM is on), with mixed-strategy plays always respected.
const DR_LABELS = {
  brilliant: { n: 'Brilliant', icon: '!!', c: '#2ec4b6', score: 100, tier: 'perfect', life: 12, line: 'The hard, brave play, and the right one.' },
  great: { n: 'Great find', icon: '!', c: '#5aa9de', score: 100, tier: 'perfect', life: 11, line: 'A close spot most players miss. You found it.' },
  best: { n: 'Best', icon: '★', c: '#8bd17c', score: 100, tier: 'perfect', life: 10, line: "The solver's favourite play." },
  excellent: { n: 'Excellent', icon: '👍', c: '#a6d785', score: 95, tier: 'perfect', life: 8, line: 'Basically as good as the best play.' },
  good: { n: 'Good', icon: '✓', c: '#c9d47a', score: 82, tier: 'good', life: 5, line: 'A play the solver makes too.' },
  inaccuracy: { n: 'Inaccuracy', icon: '?!', c: '#f2c14e', score: 45, tier: 'close', life: -4, line: 'Not a disaster, but it leaks value.' },
  mistake: { n: 'Mistake', icon: '?', c: '#f08a4b', score: 15, tier: 'bad', life: -36, line: 'This costs real money over time.' },
  blunder: { n: 'Blunder', icon: '??', c: '#e5484d', score: 0, tier: 'bad', life: -50, line: 'A big leak. Study this one.' },
};
const DR_LOSS_STEPS = { bb: [0.05, 0.2, 0.6, 1.5], icm: [0.015, 0.06, 0.18, 0.5] };
function drLabelFor(strat, ev, j, unit, hard, kind) {
  const fmax = Math.max(...strat), sb = strat.indexOf(fmax), mine = strat[j];
  let loss = null, best = sb;
  if (ev && ev.length === strat.length && ev.every((x) => Number.isFinite(x))) { best = ev.indexOf(Math.max(...ev)); loss = Math.max(0, ev[best] - ev[j]); }
  const T = DR_LOSS_STEPS[unit] || DR_LOSS_STEPS.bb;
  let lab;
  if (loss === null) { const rel = fmax > 0 ? mine / fmax : 0; lab = mine < 0.05 ? (rel < 0.02 ? 'blunder' : 'mistake') : rel >= 0.9 ? 'best' : rel >= 0.5 ? 'good' : 'inaccuracy'; }
  else lab = loss <= 1e-9 ? 'best' : loss <= T[0] ? 'excellent' : loss <= T[1] ? 'good' : loss <= T[2] ? 'inaccuracy' : loss <= T[3] ? 'mistake' : 'blunder';
  // the solver's mix is the truth: a play it makes often is never worse than Good
  if (mine >= 0.6 || j === sb) lab = (lab === 'best' || lab === 'excellent') ? lab : 'best';
  else if (mine >= 0.25 && ['inaccuracy', 'mistake', 'blunder'].includes(lab)) lab = 'good';
  else if (mine < 0.05 && (lab === 'best' || lab === 'excellent' || lab === 'good')) lab = 'inaccuracy';
  // the solver has a clear answer and you picked something it almost never does: that's a real error, not a slip
  if (mine <= 0.12 && fmax >= 0.7) lab = fmax >= 0.88 && mine <= 0.04 && (loss == null || loss > T[0]) ? (lab === 'blunder' || loss == null || loss > T[1] ? 'blunder' : 'mistake') : lab === 'blunder' ? 'blunder' : 'mistake';
  // special finds in the hardest spots
  // rare on purpose: only the very closest spots, and Brilliant only for a brave play the solver doesn't make on autopilot
  if (lab === 'best' && hard >= 0.95) lab = (['allin', 'raise', 'raiseB'].includes(kind) || kind === 'call') && mine <= 0.8 ? 'brilliant' : 'great';
  return { lab, loss, bestIdx: best };
}
function drAnswer(k) {
  const run = DR.run; if (!run || run.state !== 'play') return;
  const sp = run.spot, j = sp.res.actions.findIndex((x) => x.k === k); if (j < 0) return;
  const strat = sp.res.strat[sp.cls], fmax = Math.max(...strat), rel = fmax > 0 ? strat[j] / fmax : 0;
  const unit = sp.unit || (pfConfig().payoff.type === 'icm' ? 'icm' : 'bb'), ev = sp.res.ev && sp.res.ev[sp.cls];
  const evS = ev && sp.evScale && sp.evScale !== 1 ? ev.map((x) => x * sp.evScale) : ev;
  const g = drLabelFor(strat, evS, j, unit === 'bbicm' ? 'bb' : unit, sp.hard == null ? 0.4 : sp.hard, k), L = DR_LABELS[g.lab];
  const tier = L.tier, ok = tier !== 'bad', pts = L.score - (tier === 'bad' ? 60 : 0);
  const before = run.stack; run.stack = Math.max(0, Math.min(200, run.stack + L.life));
  run.combo = ok && tier !== 'close' ? run.combo + 1 : 0; run.bestCombo = Math.max(run.bestCombo || 0, run.combo);
  // remember the hand so the results screen can replay it
  const keep = TR.spot; TR.spot = sp; const v = trSpotView(); TR.spot = keep;
  const lab = (i) => { const a = sp.res.actions[i], o = v.opts.find((x) => x.k === a.k); return o ? o.t : a.k; };
  const best = g.bestIdx, hero = v.seats[sp.hero];
  const hand = { cards: sp.hands[sp.hero].slice(), pos: hero.pos, bb: Math.round((hero.stack - hero.put) * 10) / 10, cat: trCategory(v), mine: lab(j), best: lab(best), mineK: sp.res.actions[j].k, bestK: sp.res.actions[best].k, bestF: Math.round(strat[best] * 100), myF: Math.round(strat[j] * 100),
    odds: (() => { const o = v.opts.find((x) => x.k === 'call'), m = o && /([\d.]+)/.exec(o.t), amt = m ? +m[1] - (hero.put || 0) : 0; return amt > 0 && v.pot > 0 ? { call: Math.round(amt * 10) / 10, pot: Math.round(v.pot * 10) / 10, need: Math.round((amt / (v.pot + amt)) * 100) } : null; })(),
    grid: sp.grid || null, acts: sp.res.actions.map((x) => x.k), labels: sp.res.actions.map((x, i) => lab(i)), heroCls: sp.cls,
    lab: g.lab, loss: g.loss, unit, mix: sp.res.actions.map((a, i) => [lab(i), Math.round(strat[i] * 100)]), facing: sp.path.filter((e) => e.k !== 'fold').length };
  if (window.__drLog) window.__drLog.push({ ...hand, tier, stage: run.stages[run.si], d: sp.diff, acts: sp.res.actions.map((x) => x.k).join('/'), facing: sp.path.filter((e) => e.k !== 'fold').map((e) => e.k).join('-') || 'first in' });
  const r = { j, pts, rel, ok, tier, lab: g.lab, strat: strat.slice(), stage: run.stages[run.si], before, after: run.stack, hand };
  run.total += pts; run.results.push(r);
  if (typeof drTrack === 'function') drTrack(r, run);
  if (run.stack <= 0 && !run.noBust) run.bust = true;
  if (run.noBust) run.stack = Math.max(1, run.stack);
  arSfx(tier === 'bad' ? 'bad' : tier); arBuzz(tier);
  run.state = 'answered'; drRender();
  if (matchMedia('(max-width: 760px)').matches) { const vv = document.querySelector('#dream .drverdict'); if (vv) vv.scrollIntoView({ behavior: 'smooth', block: 'center' }); }
}
// accuracy for an event, Chess.com style: the average decision score
const drAccuracy = (results) => (results.length ? Math.round(results.reduce((a, r) => a + (DR_LABELS[r.lab] ? DR_LABELS[r.lab].score : r.ok ? 80 : 10), 0) / results.length) : 0);
function drNext() {
  const run = DR.run; if (!run || run.state !== 'answered') return;
  if (run.bust) { drFinish(); return; }
  const per = drPer(run);
  if (++run.h < per) { drHand(); return; }
  run.h = 0;
  if (run.si + 1 < run.stages.length) {
    // between levels, the cards can still beat you
    if (run.ev.kind !== 'cash' && !run.noBust && !run.ev.drill && drCoolerRoll(run)) { run.bust = true; run.cooler = DR_COOLERS[(run.rng() * DR_COOLERS.length) | 0]; run.state = 'cooler'; drRender(); return; }
    run.si++; run.tip++; drTable();
  } else { run.si++; drFinish(); }
}
// cash sessions: your stack decides what you take home (each step of the meter is real money)
function drCashNet(run, luck) { return Math.round(run.ev.buyin * Math.max(-1, Math.min(2.5, (run.stack - 100) / 40 + (luck || 0)))); }
// Slick's one-line lesson after a mistake or a borderline play
function drTip(r) {
  const h = r.hand; if (!h || r.tier === 'perfect' || r.tier === 'good') return '';
  const icm = ['bubble', 'satbubble', 'deep', 'final'].includes(r.stage);
  const loose = h.mineK !== 'fold' && h.bestK === 'fold', tight = h.mineK === 'fold' && h.bestK !== 'fold';
  const c = h.cat;
  if (!loose && !tight) return `Right idea, wrong size. The solver prefers ${h.best} here.`;
  if (c === 'Facing a jam') return loose ? `Calling a jam takes a much stronger hand than shoving one.${icm ? ' Near the money, stronger still: busting costs more than chips.' : ''}` : `This hand is ahead of their shoving range often enough. Folding gives up too much.${icm ? ' Even with ICM, this one is a call.' : ''}`;
  if (c === 'First in / vs limps') return loose ? `Too loose from ${h.pos}. With players still to act behind you, open fewer hands.` : `This is a profitable open from ${h.pos}. Folding lets the blinds walk.`;
  if (c === 'Facing a raise') return loose ? 'Facing a raise, play tighter than when you open. Their range is already strong.' : `Good enough to continue from ${h.pos}. Folding here is too tight.`;
  return loose ? 'Against a 3-bet or more, only the top of your range keeps going.' : "You're folding too much to aggression. This hand wants to continue.";
}
function drRestore() {
  window.__pfHold = false;
  if (DR.saved) { applySetup(DR.saved); DR.saved = null; }
  TR.random = false; window.__trRand = false;
}
function drQuit() {
  const run = DR.run; if (!run) return;
  // walking away: a tournament counts as busting where you are; a cash session cashes out what's left
  if (run.ev.kind !== 'cash') run.bust = true;
  drFinish(true);
}
function drLeave() { if (DR && DR.run) drQuit(); }

// where you finish: busting decides it early; surviving to the end, your decisions (and a little luck) do
function drPlace(run, ev) {
  if (ev.kind === 'sat') {
    const P = ev.seats, between = (a, b) => Math.max(1, Math.min(ev.field, a + ((run.rng() * Math.max(1, b - a + 1)) | 0)));
    if (!run.bust) return between(1, P);
    return run.stages[Math.min(run.si, run.stages.length - 1)] === 'early' ? between(P + 6, ev.field) : run.h === 0 ? P + 1 : between(P + 1, P + 5);
  }
  const N = ev.field, P = drPaid(ev), rng = run.rng, stage = run.stages[Math.min(run.si, run.stages.length - 1)];
  const n = run.results.length, pct = run.total / (100 * Math.max(1, n)), between = (a, b) => Math.max(1, Math.min(N, a + ((rng() * Math.max(1, b - a + 1)) | 0)));
  if (run.bust) {
    if (stage === 'early') return between(Math.max(P + 2, Math.round(N * 0.45)), N);
    if (stage === 'middle') return between(P + 2, Math.max(P + 2, Math.round(N * 0.45)));
    if (stage === 'bubble') return run.h === 0 && !run.cooler ? P + 1 : between(Math.min(P, 10), P);
    if (stage === 'deep') return between(10, Math.max(10, Math.min(P, Math.round(P * 0.3))));
    const ranges = [[7, 9], [4, 6], [2, 3]], [a, b] = ranges[Math.min(2, Math.floor((run.h * 3) / drPer(run)))];
    return between(Math.min(a, N), Math.min(b, N));
  }
  // you survived the whole thing: your decisions (and a little luck) decide the top spots
  const winT = 0.6 + 0.04 * Math.log10(N) + 0.01 * Math.min(6, ev.ch), luck = (rng() - 0.5) * 0.12, score = pct + luck + (run.stack - 100) / 1000;
  run.luck = luck;
  if (score >= winT) return 1;
  if (score >= winT - 0.12) return 2;
  if (score >= winT - 0.22) return 3;
  if (score >= winT - 0.36) return between(4, Math.min(9, N));
  // you survived on fumes: poor decisions late in the day cost you the deep run
  if (score >= winT - 0.55) return between(Math.min(10, P), Math.max(10, P));
  return between(P + 1, Math.max(P + 2, Math.round(P * 1.3)));
}

function drFinish(quit) {
  const run = DR.run, s = DR.st, ev = run.ev; if (!run) return;
  DR.run = null; drRestore();
  if (ev.kind === 'sng') { drSngFinish(run, quit); return; }
  const n = run.results.length, pct = n ? run.total / (100 * n) : 0;
  const counts = { perfect: 0, good: 0, close: 0, bad: 0 }; run.results.forEach((r) => counts[r.tier]++);
  s.dec += n; s.perfect += counts.perfect; s.good += counts.good; s.close += counts.close; s.mistakes += counts.bad; s.played++;
  const rec = s.events[ev.id] || (s.events[ev.id] = { played: 0, cashes: 0, wins: 0, best: null, cash: 0, net: 0 });
  rec.played++;
  const keyHands = run.results.filter((r) => r.hand).map((r) => r).sort((a, b) => a.pts - b.pts);
  const res = { ev, n, pct, counts, all: run.results.map((x) => ({ lab: x.lab, tier: x.tier, stage: x.stage, hand: x.hand })), acc: drAccuracy(run.results), bestCombo: run.bestCombo || 0, before0: 0, quit: !!quit, keys: [...keyHands.filter((r) => r.tier === 'bad').slice(0, 2), ...keyHands.filter((r) => r.tier === 'perfect' && r.hand.bestF < 80).slice(-1)].slice(0, 3), boss: run.boss, before: s.br + (ev.free ? 0 : ev.buyin), titles: [], beat: null, met: null, prize: 0, net: 0 };
  if (ev.kind === 'sat') {
    const place = drPlace(run, ev); res.place = place; res.paid = ev.seats;
    rec.best = rec.best ? Math.min(rec.best, place) : place;
    if (place <= ev.seats) { drMoment(s, '🎟', `Won a seat into the ${drEvent(ev.target).name}`); s.tickets = s.tickets || {}; s.tickets[ev.target] = (s.tickets[ev.target] || 0) + 1; res.ticket = ev.target; rec.wins++; s.flags.satWin = true; }
    res.net = -(DR.usedTicket ? 0 : ev.buyin);
  } else if (ev.kind === 'cash') {
    const net = run.bust ? -ev.buyin : drCashNet(run, quit ? 0 : (run.rng() - 0.5) * 0.3);
    drAdd(s, ev.buyin + net, `Cashed out: ${ev.name}`);
    res.net = net; res.prize = ev.buyin + net; rec.net += net;
    if (net > 0) { s.flags.cashWin = true; res.repaid = drRepay(s, net); }
  } else {
    const P = drPaid(ev), place = drPlace(run, ev), pays = typicalPayouts(ev.field, ev.prizeBase || ev.buyin, 0).amounts;
    const prize = place <= P ? Math.round(pays[place - 1]) : 0;
    res.place = place; res.paid = P; res.prize = prize; res.net = prize - (ev.free ? 0 : ev.buyin);
    rec.best = rec.best ? Math.min(rec.best, place) : place;
    const poy = drPoyPoints(ev, place); if (poy) { s.poy = s.poy || {}; s.poy[drMonth()] = (s.poy[drMonth()] || 0) + poy; res.poy = poy; }
    if (prize > 0 && s.cashes === 0) drMoment(s, '💵', `First cash: ${drOrd(place)} in the ${ev.name}`);
    if (prize > 0) { drAdd(s, prize, `${drOrd(place)} in ${ev.name}`); s.cashes++; rec.cashes++; rec.cash = Math.max(rec.cash, prize); if (ev.ch === 2) s.flags.circuitCash = true; res.repaid = drRepay(s, res.net); }
    rec.net += res.net;
    const reachedFinal = !run.bust || run.stages[run.si] === 'final';
    res.cooler = run.cooler || null; res.stage = run.stages[Math.min(run.si, run.stages.length - 1)];
    if (reachedFinal && run.si >= run.stages.length - 1) { s.ft++; if (ev.id === 'main') s.flags.meft = true; if (run.boss) { s.met[run.boss] = true; res.met = run.boss; } }
    if (place === 1) {
      rec.wins++;
      if (ev.title) { s.titles[ev.title]++; res.titles.push(ev.title); s.trophies.unshift({ t: ev.title, ev: ev.name, prize, d: Date.now() }); drMoment(s, DR_TITLES[ev.title].icon, `Won the ${ev.name} for $${drMoney(prize)}`); }
      else if (!ev.daily) drMoment(s, '🏅', `Won the ${ev.name}`);
      if (run.boss && DR_LEGENDS.some((l) => l.id === run.boss)) { if (!s.legends[run.boss]) drMoment(s, '🂡', `Beat ${drLegend(run.boss).name} heads-up`); s.legends[run.boss] = true; res.beat = run.boss; }
      if (run.boss === 'ghost') drMoment(s, '🕶️', 'Beat Bio The Ghost for the world title');
      if (run.boss === 'trent') { s.flags.trentW = (s.flags.trentW || 0) + 1; drMoment(s, '😏', `Beat Mustich heads-up (${s.flags.trentW} and counting)`); }
      if (drTitleCount(s) === 1 && !s.story.firstwin) s.queue.push('firstwin');
      const once = (id) => { if (!s.story[id] && !s.queue.includes(id)) s.queue.push(id); };
      if (ev.title === 'room' && run.boss === 'trent') once('trentbeat');
      if (ev.title === 'ring' && s.titles.ring === 1) once('ring1');
      if (ev.title === 'bracelet' && s.titles.bracelet === 1) once('bracelet1');
      if ((ev.title === 'tour' || ev.title === 'glo') && s.titles.tour + s.titles.glo === 1) once('tour1');
      if (ev.title === 'titan' && s.titles.titan === 1) once('titan1');
      if (ev.id === 'main' && !s.story.champ) s.queue.push('champ');
    }
  }
  if (ev.daily) {
    s.daily = s.daily || {}; const today = drToday(), y = new Date(); y.setDate(y.getDate() - 1);
    s.streak = s.daily[drToday(y)] ? (s.streak || 0) + 1 : 1; s.bestStreak = Math.max(s.bestStreak || 0, s.streak);
    s.daily[today] = { place: res.place, prize: res.prize }; res.streak = s.streak;
    const keys = Object.keys(s.daily).sort(); if (keys.length > 120) keys.slice(0, keys.length - 120).forEach((k) => delete s.daily[k]);
  }
  if (ev.id === 'local' && !s.story.p_after) s.queue.push('p_after');
  if (run.cooler && s.ch >= 1 && !s.story.badbeat && !s.queue.includes('badbeat')) s.queue.push('badbeat');
  if (s.peak >= 100000 && !s.story.derek && !s.queue.includes('derek')) s.queue.push('derek');
  res.after = s.br;
  res.unlocked = drUnlocks(s);
  drProgress(s);
  drEventDone(res, run);
  drSave();
  DR.last = res; DR.view = 'results'; drRender(); scrollTo(0, 0);
  arSfx(res.titles.length ? 'win' : res.prize > 0 || res.net > 0 ? 'good' : 'loss');
  if (res.unlocked.length) setTimeout(() => arSfx('unlock'), 900);
}
