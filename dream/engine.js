// ================================================================ THE DREAM: engine
// State, money, chapters, unlocks, and playing an event: solver-graded decisions decide your
// stack, your finish and your payout.

var DR = null;
const drMoney = (n) => Math.round(n).toLocaleString('en-US');
const drMoneyShort = (n) => (n >= 1e6 ? `${+(n / 1e6).toFixed(n >= 1e7 ? 1 : 2)}M` : n >= 1e4 ? `${Math.round(n / 1000)}K` : drMoney(n));
const drOrd = (n) => { const v = n % 100; return n + (v >= 11 && v <= 13 ? 'th' : ['th', 'st', 'nd', 'rd'][n % 10] || 'th'); };
const drPaid = (ev) => typicalPayouts(ev.field, ev.buyin, 0).paid;
const drTitleCount = (s) => Object.values(s.titles).reduce((a, b) => a + b, 0);
const drStakeSize = (s) => [300, 300, 1500, 6000, 25000, 120000, 60000, 150000][Math.min(7, s.ch)];

function drDefault() {
  return { v: 1, made: false, name: '', look: { ...DR_LOOK_DEFAULT }, br: 0, peak: 0, ch: 0, story: {}, queue: [], job: null,
    titles: { local: 0, room: 0, ring: 0, bracelet: 0, tour: 0, glo: 0, titan: 0, me: 0 }, legends: {}, met: {}, events: {},
    dec: 0, perfect: 0, good: 0, close: 0, mistakes: 0, played: 0, cashes: 0, ft: 0, flags: {},
    eq: { outfit: 'office', eyes: 'readers', head: 'none', watch: 'digital', chain: 'none', home: 'studio', ride: 'bus', patch: 'none' },
    owned: {}, newItems: [], debt: 0, ledger: [], trophies: [], lastEvent: null };
}
function drLoad() {
  try {
    const v = JSON.parse(localStorage.getItem(DR_KEY) || 'null');
    if (!v || v.v !== 1) return drDefault();
    const d = drDefault();
    return { ...d, ...v, titles: { ...d.titles, ...(v.titles || {}) }, eq: { ...d.eq, ...(v.eq || {}) }, look: { ...d.look, ...(v.look || {}) }, flags: v.flags || {} };
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

// ---------------------------------------------------------------- unlocks
function drItemKey(it) { return it.slot + ':' + it.id; }
function drUnlocks(s) {
  const fresh = [];
  for (const it of DR_ITEMS) {
    const k = drItemKey(it);
    if (s.owned[k] || !it.test(s)) continue;
    s.owned[k] = true;
    if (it.req && it.req !== 'Where it all started' && it.id !== 'none') fresh.push(it);
    // wear the newest thing you earn: later in the list is the upgrade
    const cur = DR_ITEMS.findIndex((x) => x.slot === it.slot && x.id === s.eq[it.slot]), mine = DR_ITEMS.indexOf(it);
    if (it.id !== 'none' && mine > cur && it.slot !== 'head') s.eq[it.slot] = it.id;
  }
  s.newItems = [...(s.newItems || []), ...fresh.map(drItemKey)];
  return fresh;
}
const drOwns = (s, slot, id) => !!s.owned[slot + ':' + id];
function drRep(s) { for (const [t, label] of DR_REPUTATION) if (t(s)) return typeof label === 'function' ? label(s) : label; return 'Dreamer'; }

// ---------------------------------------------------------------- chapters
function drAvailable(s) {
  if (s.ch === 0) return DR_EVENTS.filter((e) => e.id === 'local');
  return DR_EVENTS.filter((e) => e.ch >= 1 && e.ch <= s.ch);
}
function drAfford(s, ev) { return ev.free || s.br >= ev.buyin; }
function drRisk(s, ev) {
  if (ev.free) return ['free', 'Free seat'];
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
      drAdd(s, stake, "Slick's stake"); s.debt += stake; s.flags.stakes = (s.flags.stakes || 0) + 1; s.queue.push('broke');
    }
  }
}

// ---------------------------------------------------------------- an event
const DR_STAGE = { early: { gid: 'ring', label: 'Early', long: 'Early levels' }, middle: { gid: 'ring', label: 'Middle', long: 'Middle levels' }, bubble: { gid: 'bubble', label: 'Bubble', long: 'The bubble' },
  deep: { gid: 'shove', label: 'Deep', long: 'Deep run' }, final: { gid: 'shove', label: 'Final', long: 'Final table' }, cash: { gid: 'cash', label: 'Cash', long: 'Cash session' } };
// bigger fields mean longer days: more levels to survive before the final table
function drStagesFor(ev) {
  if (ev.kind === 'cash') return ['cash'];
  return ev.field < 150 ? ['early', 'bubble', 'final'] : ev.field < 1500 ? ['early', 'middle', 'bubble', 'final'] : ['early', 'middle', 'bubble', 'deep', 'final'];
}
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
const DR_DELTA = { perfect: 10, good: 6, close: 1, bad: -36 };

function drPickBoss(s, ev, rng) {
  if (ev.boss) return ev.boss;
  if (!ev.legends) return null;
  const fresh = ev.legends.filter((id) => !s.legends[id]), pool = fresh.length ? fresh : ev.legends;
  return pool[(rng() * pool.length) | 0];
}
function drStart(id) {
  const s = DR.st, ev = drEvent(id);
  if (DR.run || !ev || !drAfford(s, ev)) return;
  if (!ev.free) drAdd(s, -ev.buyin, `Buy-in: ${ev.name}`);
  s.lastEvent = id; drSave();
  const seed = `${id}|${Date.now()}|${Math.random()}`, rng = arRng(seed);
  DR.saved = snapSetup(); TR.random = true; window.__trRand = true;
  if (S.variant !== 'nlh') setVariant('nlh');
  DR.run = { ev, stages: drStagesFor(ev), si: 0, h: 0, stack: 100, results: [], total: 0, rng,
    state: 'loading', spot: null, boss: drPickBoss(s, ev, rng), fails: 0, bust: false, combo: 0, t0: performance.now(), tip: 0 };
  DR.view = 'run'; drRender(); drTable();
}
async function drTable() {
  const run = DR.run; if (!run) return;
  run.state = 'loading'; run.spot = null; drRender();
  const stage = run.stages[run.si];
  for (let attempt = 0; attempt < 3; attempt++) {
    applySetup(arSetup(DR_STAGE[stage].gid, run.rng)); run.t0 = performance.now();
    while (performance.now() - run.t0 < AR_SOLVE_MS) {
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
  const sp = await drDeal(run.rng); if (DR.run !== run) return;
  if (!sp) { if (++run.fails > 3) { drFinish(); return; } drTable(); return; }
  run.spot = sp; run.state = 'play'; drRender();
}
// a good teaching spot: the solver has a clear opinion, and the action before you is something real
// people do (not five players all-in in front of you because a half-solved tree said so)
async function drDeal(rng) {
  let last = null;
  for (let t = 0; t < 10; t++) {
    const sp = await arDeal(rng); if (!sp) return last;
    last = sp;
    const strat = sp.res.strat[sp.cls], k = strat.length, hi = Math.max(...strat), lo = Math.min(...strat);
    const flat = k >= 2 && hi - lo < 0.22;
    let jams = 0, callsAfterJam = 0; for (const e of sp.path) { if (e.k === 'allin') jams++; else if (jams && e.k === 'call') callsAfterJam++; }
    const chaos = jams + callsAfterJam >= 3 || jams >= 2;
    if (!flat && !chaos) return sp;
  }
  return last;
}
function drAnswer(k) {
  const run = DR.run; if (!run || run.state !== 'play') return;
  const sp = run.spot, j = sp.res.actions.findIndex((x) => x.k === k); if (j < 0) return;
  const strat = sp.res.strat[sp.cls], fmax = Math.max(...strat), rel = fmax > 0 ? strat[j] / fmax : 0, ok = strat[j] >= 0.05;
  const tier = !ok ? 'bad' : rel >= 0.9 ? 'perfect' : rel >= 0.5 ? 'good' : 'close';
  const pts = ok ? Math.round(100 * rel) : -Math.round(100 * (1 - rel));
  const before = run.stack; run.stack = Math.max(0, Math.min(200, run.stack + DR_DELTA[tier]));
  run.combo = ok ? run.combo + 1 : 0;
  run.total += pts; run.results.push({ j, pts, rel, ok, tier, strat: strat.slice(), stage: run.stages[run.si], before, after: run.stack });
  if (run.stack <= 0) run.bust = true;
  arSfx(tier === 'bad' ? 'bad' : tier); arBuzz(tier);
  run.state = 'answered'; drRender();
  if (matchMedia('(max-width: 760px)').matches) { const v = document.querySelector('#dream .drverdict'); if (v) v.scrollIntoView({ behavior: 'smooth', block: 'center' }); }
}
function drNext() {
  const run = DR.run; if (!run || run.state !== 'answered') return;
  if (run.bust) { drFinish(); return; }
  const per = run.ev.kind === 'cash' ? DR_CASH_HANDS : DR_HANDS;
  if (++run.h < per) { drHand(); return; }
  run.h = 0;
  if (run.si + 1 < run.stages.length) {
    // between levels, the cards can still beat you
    if (run.ev.kind === 'mtt' && drCoolerRoll(run)) { run.bust = true; run.cooler = DR_COOLERS[(run.rng() * DR_COOLERS.length) | 0]; run.state = 'cooler'; drRender(); return; }
    run.si++; run.tip++; drTable();
  } else { run.si++; drFinish(); }
}
function drRestore() {
  if (DR.saved) { applySetup(DR.saved); DR.saved = null; }
  TR.random = false; window.__trRand = false;
}
function drQuit() {
  const run = DR.run; if (!run) return;
  // walking away: a tournament counts as busting where you are; a cash session cashes out what's left
  if (run.ev.kind === 'mtt') run.bust = true;
  drFinish(true);
}
function drLeave() { if (DR && DR.run) drQuit(); }

// where you finish: busting decides it early; surviving to the end, your decisions (and a little luck) do
function drPlace(run, ev) {
  const N = ev.field, P = drPaid(ev), rng = run.rng, stage = run.stages[Math.min(run.si, run.stages.length - 1)];
  const n = run.results.length, pct = run.total / (100 * Math.max(1, n)), between = (a, b) => Math.max(1, Math.min(N, a + ((rng() * Math.max(1, b - a + 1)) | 0)));
  if (run.bust) {
    if (stage === 'early') return between(Math.max(P + 2, Math.round(N * 0.45)), N);
    if (stage === 'middle') return between(P + 2, Math.max(P + 2, Math.round(N * 0.45)));
    if (stage === 'bubble') return run.h === 0 && !run.cooler ? P + 1 : between(Math.min(P, 10), P);
    if (stage === 'deep') return between(10, Math.max(10, Math.min(P, Math.round(P * 0.3))));
    const ranges = [[7, 9], [4, 6], [2, 3]], [a, b] = ranges[Math.min(2, run.h)];
    return between(Math.min(a, N), Math.min(b, N));
  }
  // you survived the whole thing: your decisions (and a little luck) decide the top spots
  const winT = 0.6 + 0.04 * Math.log10(N) + 0.01 * Math.min(6, ev.ch), luck = (rng() - 0.5) * 0.12, score = pct + luck + (run.stack - 100) / 1000;
  run.luck = luck;
  return score >= winT ? 1 : score >= winT - 0.12 ? 2 : score >= winT - 0.22 ? 3 : between(4, Math.min(9, N));
}

function drFinish(quit) {
  const run = DR.run, s = DR.st, ev = run.ev; if (!run) return;
  DR.run = null; drRestore();
  const n = run.results.length, pct = n ? run.total / (100 * n) : 0;
  const counts = { perfect: 0, good: 0, close: 0, bad: 0 }; run.results.forEach((r) => counts[r.tier]++);
  s.dec += n; s.perfect += counts.perfect; s.good += counts.good; s.close += counts.close; s.mistakes += counts.bad; s.played++;
  const rec = s.events[ev.id] || (s.events[ev.id] = { played: 0, cashes: 0, wins: 0, best: null, cash: 0, net: 0 });
  rec.played++;
  const res = { ev, n, pct, counts, quit: !!quit, boss: run.boss, before: s.br + (ev.free ? 0 : ev.buyin), titles: [], beat: null, met: null, prize: 0, net: 0 };
  if (ev.kind === 'cash') {
    let net;
    if (run.bust) net = -ev.buyin;
    else {
      const edge = Math.max(-1, Math.min(2.4, (pct - 0.45) * 4)), luck = (run.rng() - 0.5) * 0.5;
      net = Math.round(ev.buyin * Math.max(-1, edge + luck));
      if (quit) net = Math.min(net, Math.round(ev.buyin * ((run.stack - 100) / 100)));
    }
    drAdd(s, ev.buyin + net, `Cashed out: ${ev.name}`);
    res.net = net; res.prize = ev.buyin + net; rec.net += net;
    if (net > 0) { s.flags.cashWin = true; res.repaid = drRepay(s, net); }
  } else {
    const P = drPaid(ev), place = drPlace(run, ev), pays = typicalPayouts(ev.field, ev.buyin, 0).amounts;
    const prize = place <= P ? Math.round(pays[place - 1]) : 0;
    res.place = place; res.paid = P; res.prize = prize; res.net = prize - (ev.free ? 0 : ev.buyin);
    rec.best = rec.best ? Math.min(rec.best, place) : place;
    if (prize > 0) { drAdd(s, prize, `${drOrd(place)} in ${ev.name}`); s.cashes++; rec.cashes++; rec.cash = Math.max(rec.cash, prize); if (ev.ch === 2) s.flags.circuitCash = true; res.repaid = drRepay(s, res.net); }
    rec.net += res.net;
    const reachedFinal = !run.bust || run.stages[run.si] === 'final';
    res.cooler = run.cooler || null; res.stage = run.stages[Math.min(run.si, run.stages.length - 1)];
    if (reachedFinal && run.si >= run.stages.length - 1) { s.ft++; if (ev.id === 'main') s.flags.meft = true; if (run.boss) { s.met[run.boss] = true; res.met = run.boss; } }
    if (place === 1) {
      rec.wins++;
      if (ev.title) { s.titles[ev.title]++; res.titles.push(ev.title); s.trophies.unshift({ t: ev.title, ev: ev.name, prize, d: Date.now() }); }
      if (run.boss && DR_LEGENDS.some((l) => l.id === run.boss)) { s.legends[run.boss] = true; res.beat = run.boss; }
      if (drTitleCount(s) === 1 && !s.story.firstwin) s.queue.push('firstwin');
      if (ev.id === 'main' && !s.story.champ) s.queue.push('champ');
    }
  }
  if (ev.id === 'local' && !s.story.p_after) s.queue.push('p_after');
  res.after = s.br;
  res.unlocked = drUnlocks(s);
  drProgress(s);
  drSave();
  DR.last = res; DR.view = 'results'; drRender(); scrollTo(0, 0);
  arSfx(res.titles.length ? 'win' : res.prize > 0 || res.net > 0 ? 'good' : 'loss');
  if (res.unlocked.length) setTimeout(() => arSfx('unlock'), 900);
}
