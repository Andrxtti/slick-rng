// ================================================================ THE DREAM: the other three ways to play
// Storyline is the career. Casual, Ranked and Leagues are short sit-and-gos (SNGs) on the same solver spots.
// Reward rules: story gear only comes from the story; league exclusives only come from leagues;
// ranked earns a rating and a season emblem; casual is just for fun. Nothing is ever for sale.
// Opponents on every board are simulated until online play (a server) exists.

const DR_MODES = [['story', 'Storyline', '📖'], ['casual', 'Casual', '☕'], ['ranked', 'Ranked', '🏅'], ['league', 'Leagues', '🏆']];
const DR_SNG = {
  hyper6: { name: '6-Max Hyper', field: 6, paid: 2, stages: ['early', 'final'], pts: [70, 40, 15, 8, 6, 6], blurb: 'Six players, two paid. About three minutes.' },
  sng9: { name: '9-Max Sit & Go', field: 9, paid: 3, stages: ['early', 'bubble', 'final'], pts: [100, 65, 40, 20, 12, 10, 6, 6, 6], blurb: 'The classic single table. Three paid, a real bubble.' },
  dbl18: { name: '18-Player Double Table', field: 18, paid: 4, stages: ['early', 'middle', 'bubble', 'final'], pts: [130, 85, 55, 35, 15, 12, 10, 8, 6, 6, 5, 5, 5, 5, 5, 5, 5, 5], blurb: 'Two tables down to one. The longest SNG.' },
};
const DR_MODE_FORMATS = { casual: ['hyper6', 'sng9', 'dbl18'], ranked: ['sng9'], league: ['sng9', 'hyper6'] };
const DR_HANDLES = ['RiverRat88', 'NutPeddler', 'ICMagician', 'SnapCallSally', 'TiltedTom', 'FoldEquity', 'GTOgranny', '3betBetty', 'DonkSlayer', 'MinCashMike',
  'QuadsQueen', 'ShipItSam', 'VarianceVic', 'OverbetOscar', 'CoolerKing', 'StackCrusher', 'PotOddsPete', 'NitNancy', 'BluffBarry', 'TurnTheNuts',
  'SuitedConnie', 'FlopZilla', 'AceRagAndy', 'BubbleBoyBen', 'HeroCallHal', 'JamOrFoldJo', 'CheckRaiseCass', 'LimpKing', 'BlindStealer', 'RunItTwice',
  'PocketRocketRoy', 'TableCaptain', 'SlowRollSue', 'MuckedMel', 'BigBlindBo', 'FeltFreddie', 'DealMeInDee', 'ChipLeaderLu', 'ShortStackStan', 'KickerKate'];

// ---------------------------------------------------------------- league seasons: one theme a month
// Each tier is an exclusive. Earn it in that month's league or it's gone (until the theme returns).
const DR_SEASONS = {
  10: { key: 'halloween', name: 'Haunted Felt League', icon: '🎃', accent: '#ff7a1a', blurb: 'October only. The felt is cursed, the prizes are not.' },
  11: { key: 'harvest', name: 'Harvest Cup', icon: '🦃', accent: '#d9893a', blurb: 'November only. Stuff the stack, carve the field.' },
  12: { key: 'frost', name: 'Frostbite Series', icon: '❄️', accent: '#7fd0ff', blurb: 'December only. Cold decks, warm sweaters.' },
};
const DR_SEASON_DEFAULT = { key: 'open', name: 'Open League', icon: '🏆', accent: '#d9b36c', blurb: 'Year-round league. Themed months bring limited-time gear.' };
const DR_LG_TIERS = [['play', 'Play 5 league games this month'], ['day', 'Finish #1 on a daily board'], ['week', 'Finish #1 on a weekly board'], ['top10', 'Finish top 10 on the monthly board'], ['month', 'Finish #1 on the monthly board']];
const DR_LG_ITEMS = [
  { slot: 'patch', id: 'boo', name: '“BOO!” patch', season: 'halloween', tier: 'play' },
  { slot: 'head', id: 'pumpkin', name: 'Jack-o’-lantern cap', season: 'halloween', tier: 'day' },
  { slot: 'chain', id: 'bones', name: 'Bone chain', season: 'halloween', tier: 'week' },
  { slot: 'eyes', id: 'masq', name: 'Masquerade mask', season: 'halloween', tier: 'top10' },
  { slot: 'outfit', id: 'vampire', name: 'Count’s cape', season: 'halloween', tier: 'month' },
  { slot: 'patch', id: 'harvest', name: 'Harvest Cup patch', season: 'harvest', tier: 'play' },
  { slot: 'head', id: 'pilgrim', name: 'Pilgrim hat', season: 'harvest', tier: 'day' },
  { slot: 'chain', id: 'wishbone', name: 'Gold wishbone', season: 'harvest', tier: 'week' },
  { slot: 'eyes', id: 'amber', name: 'Amber cider shades', season: 'harvest', tier: 'top10' },
  { slot: 'outfit', id: 'flannel', name: 'Harvest flannel', season: 'harvest', tier: 'month' },
  { slot: 'patch', id: 'frost', name: 'Frostbite patch', season: 'frost', tier: 'play' },
  { slot: 'head', id: 'santa', name: 'Santa hat', season: 'frost', tier: 'day' },
  { slot: 'chain', id: 'lights', name: 'Holiday lights', season: 'frost', tier: 'week' },
  { slot: 'eyes', id: 'snow', name: 'Snow goggles', season: 'frost', tier: 'top10' },
  { slot: 'outfit', id: 'ugly', name: 'The ugly sweater', season: 'frost', tier: 'month' },
].map((it) => ({ ...it, league: true }));
const drSeasonFor = (monthKey) => DR_SEASONS[+monthKey.slice(5, 7)] || DR_SEASON_DEFAULT;
const drSeasonItem = (season, tier) => DR_LG_ITEMS.find((x) => x.season === season.key && x.tier === tier);
const drLgReq = (it) => { const sz = Object.values(DR_SEASONS).find((x) => x.key === it.season); return `${sz ? sz.name : 'League'}: ${DR_LG_TIERS.find((t) => t[0] === it.tier)[1].toLowerCase()}`; };

// ---------------------------------------------------------------- ranked: one rating, monthly seasons
const DR_TIERS = [[0, 'Bronze', '🥉'], [1100, 'Silver', '🥈'], [1250, 'Gold', '🥇'], [1400, 'Platinum', '💠'], [1600, 'Diamond', '💎'], [1800, 'Legend', '🔱']];
const drTier = (r) => DR_TIERS.filter((t) => r >= t[0]).pop();
const drTierNext = (r) => DR_TIERS.find((t) => t[0] > r);
const DR_RANK_DELTA = [32, 20, 10, 2, -4, -8, -12, -16, -20];
const DR_PLACEMENTS = 3;

// ---------------------------------------------------------------- periods
function drWeekKey(d = new Date()) { const m = new Date(d); m.setHours(12, 0, 0, 0); m.setDate(m.getDate() - ((m.getDay() + 6) % 7)); return drToday(m); }
function drPeriodFrac(kind, d = new Date()) {
  const h = d.getHours() + d.getMinutes() / 60;
  if (kind === 'd') return h / 24;
  if (kind === 'w') return (((d.getDay() + 6) % 7) + h / 24) / 7;
  const days = new Date(d.getFullYear(), d.getMonth() + 1, 0).getDate(); return (d.getDate() - 1 + h / 24) / days;
}
const drPeriodKey = (kind, d = new Date()) => (kind === 'd' ? drToday(d) : kind === 'w' ? drWeekKey(d) : drMonth(d));
function drHash(str) { let h = 2166136261; for (const ch of str) { h ^= ch.charCodeAt(0); h = Math.imul(h, 16777619); } return h >>> 0; }
function drSeeded(str) { let x = drHash(str) || 1; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return ((x >>> 0) % 100000) / 100000; }; }

// ---------------------------------------------------------------- state
function drModeState(s) {
  s.m = s.m || { casual: { played: 0, wins: 0 }, ranked: { played: 0, wins: 0 }, league: { played: 0, wins: 0 }, dec: 0, ok: 0 };
  s.m.challenge = s.m.challenge || { played: 0, wins: 0, days: {} };
  s.rk = s.rk || { r: 1000, peak: 1000, played: 0, season: drMonth(), best: 1000, hist: [] };
  s.lg = s.lg || { pts: { d: {}, w: {}, m: {} }, games: {}, done: {}, hist: [], priv: [], news: [] };
  // a new ranked season each month: soft reset toward the start, keep your best emblem
  if (s.rk.season !== drMonth()) {
    if (s.rk.played) s.rk.hist.unshift({ season: s.rk.season, best: s.rk.best, tier: drTier(s.rk.best)[1] });
    s.rk.r = Math.max(1000, Math.round((s.rk.r + 1000) / 2)); s.rk.best = s.rk.r; s.rk.played = 0; s.rk.season = drMonth();
  }
  return s;
}

// ---------------------------------------------------------------- simulated boards
const DR_LG_TOP = { d: 260, w: 1400, m: 5200 };
// weekly divisions, Duolingo style: 30 players, the top move up a chip colour, the bottom five drop
const DR_DIVS = [['White chip', '#e9e5dc', 15], ['Red chip', '#d4433a', 12], ['Blue chip', '#3a7be0', 10], ['Green chip', '#2a9d58', 8], ['Black chip', '#2b2b2e', 7], ['Purple chip', '#8d4fd6', 7], ['Yellow chip', '#e3b94f', 6], ['Orange chip', '#e8702a', 5], ['Pink chip', '#f878b8', 5], ['Diamond chip', '#b9f2ff', 0]];
const DR_DEMOTE = 5;
function drDivFor(s, wk) { s.lg.divs = s.lg.divs || {}; if (s.lg.divs[wk] == null) s.lg.divs[wk] = s.lg.div || 0; return s.lg.divs[wk]; }
function drLgBoard(s, kind, key, frac) {
  const div = kind === 'w' ? drDivFor(s, key) : 0, size = kind === 'w' ? 29 : DR_HANDLES.length;
  const R = drSeeded(`lg|${kind}|${key}|${div}`), top = DR_LG_TOP[kind] * (kind === 'w' ? 0.55 + 0.2 * div : 1) * (0.88 + R() * 0.24), rows = [];
  const names = DR_HANDLES.slice().sort(() => R() - 0.5).slice(0, size);
  for (let i = 0; i < names.length; i++) {
    const full = top * Math.pow(1 - i / (names.length + 6), 1.7) * (0.86 + R() * 0.14);
    rows.push({ n: names[i], p: Math.round(full * Math.pow(Math.max(0, Math.min(1, frac)), 0.85)) });
  }
  rows.push({ n: s.name, p: (s.lg.pts[kind] || {})[key] || 0, me: true });
  return rows.sort((a, b) => b.p - a.p || (a.me ? -1 : 1));
}
function drRankBoard(s) {
  const R = drSeeded(`rk|${drMonth()}`), f = Math.pow(drPeriodFrac('m'), 0.6), rows = DR_HANDLES.slice(0, 30).map((n, i) => ({ n, p: Math.round(1000 + (880 - i * 29 + R() * 60) * f) }));
  rows.push({ n: s.name, p: s.rk.r, me: true });
  return rows.sort((a, b) => b.p - a.p);
}

// ---------------------------------------------------------------- grants: when a day, week or month closes
function drLgGrant(s, it, why) {
  const k = drItemKey(it); if (s.owned[k]) return null;
  s.owned[k] = true; s.newItems = [...(s.newItems || []), k]; s.lg.news.push({ item: k, why });
  drMoment(s, '🏆', `League exclusive: ${it.name} (${why})`); return it;
}
function drLgSettle(s) {
  drModeState(s); const now = { d: drToday(), w: drWeekKey(), m: drMonth() }, got = [];
  for (const kind of ['d', 'w', 'm']) {
    for (const key of Object.keys(s.lg.pts[kind])) {
      if (key >= now[kind] || s.lg.done[kind + ':' + key]) continue;
      s.lg.done[kind + ':' + key] = true;
      const b = drLgBoard(s, kind, key, 1), rank = b.findIndex((r) => r.me) + 1, sz = drSeasonFor(kind === 'w' ? key.slice(0, 7) : key);
      const lab = { d: 'daily', w: 'weekly', m: 'monthly' }[kind];
      s.lg.hist.unshift({ kind, key, rank, of: b.length, pts: s.lg.pts[kind][key], season: sz.key });
      if (rank === 1) { s.m.league.boards = (s.m.league.boards || 0) + 1; drMoment(s, sz.icon, `Won the ${lab} ${sz.name} board`); }
      if (kind === 'w') {
        // promotion and demotion for next week
        const div = drDivFor(s, key), up = DR_DIVS[div][2];
        if (up && rank <= up && div < DR_DIVS.length - 1) { s.lg.div = div + 1; s.lg.news.push({ div: s.lg.div, up: true, rank }); drMoment(s, '⬆️', `Promoted to the ${DR_DIVS[s.lg.div][0]} league`); if (typeof drGainXp === 'function') drGainXp(s, 150, drOut()); }
        else if (div > 0 && rank > b.length - DR_DEMOTE) { s.lg.div = div - 1; s.lg.news.push({ div: s.lg.div, up: false, rank }); }
        else s.lg.news.push({ div, stay: true, rank });
      }
      const tier = kind === 'd' && rank === 1 ? 'day' : kind === 'w' && rank === 1 ? 'week' : null;
      const grants = tier ? [tier] : kind === 'm' ? [...(rank <= 10 ? ['top10'] : []), ...(rank === 1 ? ['month'] : [])] : [];
      for (const t of grants) { const it = drSeasonItem(sz, t); if (it) { const g = drLgGrant(s, it, `#${rank} ${lab}`); if (g) got.push(g); } }
    }
  }
  // the play-5 patch, in case it was earned before this check existed
  for (const [m, n] of Object.entries(s.lg.games)) { const it = drSeasonItem(drSeasonFor(m), 'play'); if (it && n >= 5) { const g = drLgGrant(s, it, '5 league games'); if (g) got.push(g); } }
  if (s.lg.hist.length > 60) s.lg.hist.length = 60;
  if (got.length) drSave();
  return got;
}

// ---------------------------------------------------------------- playing an SNG
function drSngEvent(mode, fmt) {
  const f = DR_SNG[fmt]; if (!f) return null;
  const sz = drSeasonFor(drMonth());
  return { id: `sng:${mode}:${fmt}`, kind: 'sng', mode, fmt, name: mode === 'league' ? `${sz.name} · ${f.name}` : mode === 'ranked' ? `Ranked ${f.name}` : f.name,
    field: f.field, paid: f.paid, buyin: 0, free: true, ch: 0, venue: mode === 'league' ? 'league' : mode === 'ranked' ? 'ranked' : 'online', blurb: f.blurb, stagesFix: f.stages };
}
function drSngPlace(run, ev) {
  const N = ev.field, P = ev.paid, rng = run.rng, stage = run.stages[Math.min(run.si, run.stages.length - 1)];
  const between = (a, b) => Math.max(1, Math.min(N, a + ((rng() * Math.max(1, b - a + 1)) | 0)));
  if (run.bust) {
    if (stage === 'early') return between(Math.ceil(N * 0.6), N);
    if (stage === 'middle') return between(P + 2, Math.ceil(N * 0.6) - 1);
    if (stage === 'bubble') return run.h === 0 && !run.cooler ? P + 1 : P;
    return Math.max(2, P - run.h);
  }
  const n = run.results.length, pct = run.total / (100 * Math.max(1, n)), luck = (rng() - 0.5) * 0.12, score = pct + luck + (run.stack - 100) / 1000;
  run.luck = luck;
  return score >= 0.68 ? 1 : score >= 0.55 ? 2 : Math.min(3, P);
}
function drSngFinish(run, quit) {
  const s = drModeState(DR.st), ev = run.ev, mode = ev.mode, f = DR_SNG[ev.fmt];
  const n = run.results.length, pct = n ? run.total / (100 * n) : 0;
  const counts = { perfect: 0, good: 0, close: 0, bad: 0 }; run.results.forEach((r) => counts[r.tier]++);
  s.m.dec += n; s.m.ok += counts.perfect + counts.good;
  const place = drSngPlace(run, ev), st = s.m[mode] || (s.m[mode] = { played: 0, wins: 0 }); st.played++; if (place === 1) st.wins++;
  const keyHands = run.results.filter((r) => r.hand).sort((a, b) => a.pts - b.pts);
  const res = { sng: true, ev, mode, n, pct, counts, place, all: run.results.map((x) => ({ lab: x.lab, tier: x.tier, stage: x.stage, hand: x.hand })), acc: drAccuracy(run.results), bestCombo: run.bestCombo || 0, quit: !!quit, cooler: run.cooler || null, luck: run.luck || 0, unlocked: [],
    keys: [...keyHands.filter((r) => r.tier === 'bad').slice(0, 2), ...keyHands.filter((r) => r.tier === 'perfect' && r.hand.bestF < 80).slice(-1)].slice(0, 3) };
  if (mode === 'ranked') {
    const r0 = s.rk.r, K = s.rk.played < DR_PLACEMENTS ? 2 : 1, base = DR_RANK_DELTA[Math.min(8, place - 1)] * K;
    let d = base - Math.round((r0 - 1000) / 120); if (place === 1) d = Math.max(8, d); if (place <= ev.paid && d < 0) d = 0;
    s.rk.r = Math.max(0, r0 + d); s.rk.played++; s.rk.best = Math.max(s.rk.best, s.rk.r); s.rk.peak = Math.max(s.rk.peak, s.rk.r);
    res.r0 = r0; res.r1 = s.rk.r; res.delta = d; res.placing = s.rk.played <= DR_PLACEMENTS ? s.rk.played : 0;
    if (drTier(s.rk.r)[0] > drTier(r0)[0]) { res.tierUp = drTier(s.rk.r); drMoment(s, res.tierUp[2], `Ranked up to ${res.tierUp[1]}`); }
  }
  if (mode === 'league') {
    const pts = (f.pts[place - 1] || 5) + (quit ? 0 : Math.round(15 * Math.max(0, pct))), m = drMonth();
    for (const kind of ['d', 'w', 'm']) { const k = drPeriodKey(kind); s.lg.pts[kind][k] = (s.lg.pts[kind][k] || 0) + pts; }
    s.lg.games[m] = (s.lg.games[m] || 0) + 1;
    res.pts = pts; res.games = s.lg.games[m];
    res.ranks = ['d', 'w', 'm'].map((kind) => { const b = drLgBoard(s, kind, drPeriodKey(kind), drPeriodFrac(kind)); return b.findIndex((r) => r.me) + 1; });
    const sz = drSeasonFor(m), it = drSeasonItem(sz, 'play');
    if (it && s.lg.games[m] >= 5) { const g = drLgGrant(s, it, '5 league games'); if (g) res.unlocked.push(g); }
  }
  if (mode === 'challenge') { const grid = run.results.map((x) => ({ perfect: '🟩', good: '🟩', close: '🟨', bad: '🟥' }[x.tier])).join(''); s.m.challenge.days[drToday()] = { acc: res.acc, grid, place }; res.grid = grid;
    const keys = Object.keys(s.m.challenge.days).sort(); if (keys.length > 120) delete s.m.challenge.days[keys[0]]; }
  drEventDone(res, run);
  drSave();
  DR.last = res; DR.view = 'results'; drRender(); scrollTo(0, 0);
  arSfx(place === 1 ? 'win' : place <= ev.paid ? 'good' : 'loss');
  if (res.unlocked.length) setTimeout(() => arSfx('unlock'), 900);
}
// the Daily Challenge: the same tables and hands for everyone today (seeded by the date), one attempt
function drChallengeStart() {
  const s = DR.st; if (DR.run || s.ch < 1) return; drModeState(s);
  if (s.m.challenge.days[drToday()]) return;
  const ev = drSngEvent('casual', 'sng9'); ev.mode = 'challenge'; ev.id = 'challenge'; ev.name = `Daily Challenge · ${new Date().toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}`;
  const rng = arRng(`challenge|${drToday()}`);
  DR.saved = snapSetup(); TR.random = true; window.__trRand = true;
  if (S.variant !== 'nlh') setVariant('nlh');
  DR.run = { ev, stages: ['early', 'bubble', 'final'], si: 0, h: 0, stack: 100, results: [], total: 0, rng, state: 'loading', spot: null, boss: null, fails: 0, bust: false, combo: 0, t0: performance.now(), tip: 0, noBust: true };
  DR.view = 'run'; drRender(); drTable();
}
function drChallengeCard(s) {
  drModeState(s); const today = s.m.challenge.days[drToday()];
  const days = Object.keys(s.m.challenge.days).sort(); let streak = 0; for (let d = new Date(); s.m.challenge.days[drToday(d)]; d.setDate(d.getDate() - 1)) streak++;
  return `<section class="drchal"><div><div class="drkicker">Daily Challenge · same 9 hands for everyone today</div>
    ${today ? `<b class="drchal-score">${today.acc}% accuracy</b><div class="drchal-grid">${today.grid}</div><small>Come back tomorrow for a new set${streak > 1 ? ` · 🔥 ${streak}-day streak` : ''}</small>` : `<b class="drchal-score">One attempt. No do-overs.</b><small>Play it, then share your score with your crew.${streak ? ` · 🔥 ${streak}-day streak` : ''}</small>`}</div>
    ${today ? drBtn('ghost sm', 'data-dr="chshare"', 'Share') : drBtn('gold', 'data-dr="challenge"', 'Play today ▸')}</section>`;
}
function drChallengeText(s) { const t = s.m.challenge.days[drToday()]; if (!t) return ''; return `Confirmed Fire · The Dream\nDaily Challenge ${new Date().toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}\n${t.acc}% accuracy\n${t.grid}`; }
function drSngStart(mode, fmt) {
  const s = DR.st; if (DR.run || s.ch < 1) return;
  const ev = drSngEvent(mode, fmt); if (!ev) return;
  drModeState(s);
  const rng = arRng(`${ev.id}|${Date.now()}|${Math.random()}`);
  DR.saved = snapSetup(); TR.random = true; window.__trRand = true;
  if (S.variant !== 'nlh') setVariant('nlh');
  DR.run = { ev, stages: ev.stagesFix.slice(), si: 0, h: 0, stack: 100, results: [], total: 0, rng,
    state: 'loading', spot: null, boss: null, fails: 0, bust: false, combo: 0, t0: performance.now(), tip: 0 };
  DR.view = 'run'; drRender(); drTable();
}

// ---------------------------------------------------------------- screens
function drModeBar() {
  const s = DR.st, open = s.ch >= 1, cur = DR.mode || 'story';
  return `<nav class="drmodes" role="tablist" aria-label="Game mode">${DR_MODES.map(([k, n, i]) => `<button role="tab" data-dr="mode" data-m="${k}" aria-selected="${cur === k}" ${k !== 'story' && !open ? 'disabled title="Finish the prologue first"' : ''}><span>${i}</span>${n}${k !== 'story' && !open ? ' 🔒' : ''}</button>`).join('')}</nav>`;
}
function drProfileStrip(s) {
  const t = drTier(s.rk.r), sz = drSeasonFor(drMonth()), mp = s.lg.pts.m[drMonth()] || 0;
  return `<section class="drprof"><div class="drprof-av" style="${drFrameStyle(s)}">${drAvatarSVG(s, { face: true })}</div>
    <div><b>${esc(s.name)}</b>${drTitleBadge(s)}<small>${t[2]} ${t[1]} · <span class="mono">${s.rk.r}</span> · ${sz.icon} <span class="mono">${drMoney(mp)}</span> league pts</small></div>
    </section>${drTodayBar(s)}`;
}
function drFmtCard(mode, fmt, cta) {
  const f = DR_SNG[fmt], st = DR.st.m[mode];
  return `<article class="drfmt"><div><h3>${esc(f.name)}</h3><p>${esc(f.blurb)}</p>
    <small class="mono">${f.field} players · ${f.paid} paid · ${f.stages.length * DR_HANDS} hands</small></div>${drBtn('gold', `data-dr="sng" data-m="${mode}" data-f="${fmt}"`, cta)}</article>`;
}
function drBoardHTML(rows, unit, max = 8) {
  const me = rows.findIndex((r) => r.me), top = rows.slice(0, max), show = me >= max ? [...top, rows[me]] : top;
  return `<div class="drpoy-list">${show.map((r) => `<div class="${r.me ? 'me' : ''}"><span class="mono">${rows.indexOf(r) + 1}</span><b>${esc(r.n)}</b><small class="mono">${drMoney(r.p)}${unit}</small></div>`).join('')}</div>`;
}
function drDivBoardHTML(s, rows) {
  const div = drDivFor(s, drWeekKey()), [name, col, up] = DR_DIVS[div], me = rows.findIndex((r) => r.me), n = rows.length;
  const show = rows.map((r, i) => ({ r, i })).filter(({ i }) => i < 10 || Math.abs(i - me) <= 2 || i >= n - DR_DEMOTE);
  let last = -1, html = '';
  for (const { r, i } of show) {
    if (i - last > 1) html += '<div class="drdiv-gap">⋯</div>';
    if (i === up && up) html += '<div class="drdiv-line up">▲ Promotion zone</div>';
    if (i === n - DR_DEMOTE && div > 0) html += '<div class="drdiv-line down">▼ Demotion zone</div>';
    html += `<div class="${r.me ? 'me' : ''} ${i < up ? 'zup' : div > 0 && i >= n - DR_DEMOTE ? 'zdown' : ''}"><span class="mono">${i + 1}</span><b>${esc(r.n)}</b><small class="mono">${drMoney(r.p)} pts</small></div>`; last = i;
  }
  return `<div class="drdiv-h" style="--dc:${col}"><span class="drdiv-chip"></span><div><b>${esc(name)} league</b><small>${up ? `Top ${up} move up` : 'The top division'}${div > 0 ? ` · bottom ${DR_DEMOTE} drop` : ''} · resets Monday</small></div></div><div class="drpoy-list">${html}</div>`;
}
function drCasualHTML(s) {
  return `${drChallengeCard(s)}${drDeckCard(s)}<section class="drmodehead"><div class="drkicker">Casual</div><h2 class="drh">No pressure. Just poker.</h2>
    <p>Quick sit-and-gos on real solver spots. Nothing is on the line: no bankroll, no rating, no rewards. Warm up, try a new line, learn.</p>
    <small class="mono">${s.m.casual.played} played · ${s.m.casual.wins} won</small></section>
    ${(() => { const p = drPg(s), w = drWeakest(p), sk = DR_SKILLS.filter(([k]) => k !== 'short');
      return `<section class="drdrills"><div class="drkicker">Skill drills · 9 hands of one kind of spot</div><div class="drdrill-row">${sk.map(([k, n]) => `<button class="drdrill ${k === w ? 'weak' : ''}" data-dr="drill" data-k="${k}"><b>${esc(n)}</b><small>${drSkillAcc(p, k) == null ? 'Not rated yet' : drSkillAcc(p, k) + '% · ' + '★'.repeat(drSkillStars(p, k)) + '☆'.repeat(3 - drSkillStars(p, k))}${k === w ? ' · your leak' : ''}</small></button>`).join('')}</div></section>`; })()}
    <div class="drfmts">${DR_MODE_FORMATS.casual.map((f) => drFmtCard('casual', f, 'Play')).join('')}</div>`;
}
function drRankedHTML(s) {
  const t = drTier(s.rk.r), nx = drTierNext(s.rk.r), lo = t[0], prog = nx ? (s.rk.r - lo) / (nx[0] - lo) : 1;
  const days = new Date(new Date().getFullYear(), new Date().getMonth() + 1, 0).getDate() - new Date().getDate() + 1;
  return `<section class="drrank"><div class="drrank-emb">${t[2]}</div><div><div class="drkicker">Ranked season · ${new Date().toLocaleDateString(undefined, { month: 'long' })}</div>
      <b class="drrank-t">${t[1]}</b><span class="mono drrank-r">${s.rk.r}</span>
      <div class="drbar"><i style="width:${(prog * 100).toFixed(1)}%"></i></div>
      <small>${s.rk.played < DR_PLACEMENTS ? `Placement games: ${s.rk.played} of ${DR_PLACEMENTS} (double rating swings)` : nx ? `${nx[0] - s.rk.r} to ${nx[2]} ${nx[1]}` : 'Top tier. Hold it.'} · season ends in ${days} day${days === 1 ? '' : 's'}</small></div></section>
    <p class="drhint">Finish in the money to protect your rating, win to climb. Your highest tier this season becomes your season emblem. Each month starts a new season with a soft reset.</p>
    <div class="drfmts">${DR_MODE_FORMATS.ranked.map((f) => drFmtCard('ranked', f, 'Find a table')).join('')}</div>
    <div class="drpoy"><div class="drkicker">Ranked ladder</div>${drBoardHTML(drRankBoard(s), '', 10)}<small class="drhint">Ladder opponents are simulated until online play arrives.</small></div>
    ${s.rk.hist.length ? `<div class="drkicker">Past seasons</div><div class="drembs">${s.rk.hist.slice(0, 12).map((h) => `<span>${drTier(h.best)[2]} ${esc(h.tier)}<small>${esc(h.season)}</small></span>`).join('')}</div>` : ''}`;
}
function drLeagueHTML(s) {
  const m = drMonth(), sz = drSeasonFor(m), kind = DR.lgb || 'w', fr = drPeriodFrac(kind), b = drLgBoard(s, kind, drPeriodKey(kind), fr);
  const days = new Date(new Date().getFullYear(), new Date().getMonth() + 1, 0).getDate() - new Date().getDate() + 1, games = s.lg.games[m] || 0;
  const items = DR_LG_ITEMS.filter((x) => x.season === sz.key);
  const ends = kind === 'd' ? 'resets at midnight' : kind === 'w' ? 'resets Monday' : `ends in ${days} day${days === 1 ? '' : 's'}`;
  const news = s.lg.news.splice(0); if (news.length) drSave();
  const priv = s.lg.priv || [];
  return `${news.length ? `<section class="drlgnews">${news.map((x) => { if (x.div != null) return `<div class="${x.up ? 'up' : x.stay ? '' : 'down'}"><b>${x.up ? '⬆️ Promoted' : x.stay ? '➡️ Held your spot' : '⬇️ Relegated'}: ${esc(DR_DIVS[x.div][0])} league</b><small>You finished #${x.rank} last week${x.up ? ' (+150 XP)' : ''}.</small></div>`;
      const it = DR_LG_ITEMS.find((i) => drItemKey(i) === x.item); return it ? `<div><b>🏆 ${esc(it.name)}</b><small>Earned: ${esc(x.why)}. It's in your closet.</small></div>` : ''; }).join('')}</section>` : ''}
    <section class="drseason" style="--acc:${sz.accent}"><div class="drseason-icon">${sz.icon}</div><div><div class="drkicker">Public league · ${new Date().toLocaleDateString(undefined, { month: 'long' })}</div>
      <h2 class="drh">${esc(sz.name)}</h2><p>${esc(sz.blurb)}</p><small>Ends in ${days} day${days === 1 ? '' : 's'} · ${games} game${games === 1 ? '' : 's'} played this month</small></div></section>
    <div class="drfmts">${DR_MODE_FORMATS.league.map((f) => drFmtCard('league', f, 'Play for points')).join('')}</div>
    <div class="drpoy"><div class="drboardtabs">${[['w', 'My division'], ['d', 'Today'], ['m', 'Season']].map(([k, n]) => `<button data-dr="lgb" data-k="${k}" aria-pressed="${kind === k}">${n}</button>`).join('')}</div>
      ${kind === 'w' ? drDivBoardHTML(s, b) : drBoardHTML(b, ' pts', 10)}<small class="drhint">Board ${ends}. Points: finish place plus a bonus for solver-approved decisions. Rivals are simulated until online play arrives.</small></div>
    ${items.length ? `<div class="drkicker">${sz.icon} ${esc(sz.name)} exclusives · only this month</div><div class="drlgitems">${items.map((it) => {
      const own = drOwns(s, it.slot, it.id), tier = DR_LG_TIERS.find((t) => t[0] === it.tier)[1];
      return `<div class="drlgitem ${own ? 'own' : ''}"><div class="drlgitem-av">${drAvatarSVG({ ...s, eq: { ...s.eq, [it.slot]: it.id } }, { face: it.slot === 'head' || it.slot === 'eyes' })}</div><b>${esc(it.name)}</b><small>${own ? '✓ Earned' : '🔒 ' + esc(tier)}${it.tier === 'play' && !own ? ` (${Math.min(5, games)}/5)` : ''}</small></div>`; }).join('')}</div>` : '<p class="drhint">No themed gear this month. Seasonal leagues run October, November and December.</p>'}
    <div class="drkicker">Private leagues</div>
    <section class="drpriv">
      ${priv.length ? priv.map((p) => `<div class="drprivcard"><div><b>${esc(p.name)}</b><small>Code <span class="mono">${esc(p.code)}</span> · ${p.own ? 'you run it' : 'joined'} · ${esc(DR_SNG[p.fmt] ? DR_SNG[p.fmt].name : '')}</small></div><small class="drhint">Waiting for online play: your crew's results will sync here.</small></div>`).join('') : '<p class="drhint">Start one for your crew, or join with a friend\'s code. Same spots, one leaderboard, your own monthly winner.</p>'}
      <div class="drprivform"><input id="drLgName" maxlength="24" placeholder="League name" aria-label="League name">${drBtn('gold sm', 'data-dr="lgnew"', 'Create')}</div>
      <div class="drprivform"><input id="drLgCode" maxlength="9" placeholder="Join code" aria-label="Join code">${drBtn('ghost sm', 'data-dr="lgjoin"', 'Join')}</div>
      <small class="drhint">Private leagues go live with online play. Codes you make or join now are saved.</small>
    </section>`;
}
function drModeHubHTML() {
  const s = drModeState(DR.st), mode = DR.mode;
  drLgSettle(s);
  return `${drModeBar()}${drProfileStrip(s)}${mode === 'casual' ? drCasualHTML(s) : mode === 'ranked' ? drRankedHTML(s) : drLeagueHTML(s)}`;
}
function drSngResultsHTML() {
  const r = DR.last, ev = r.ev, s = DR.st, f = DR_SNG[ev.fmt];
  const headline = ev.review ? `Mistake review · ${r.fixed || 0} of ${r.n} fixed` : r.mode === 'challenge' ? `Daily Challenge · ${r.acc}%` : ev.drill ? `Drill complete · ${r.acc}%` : r.place === 1 ? 'You won the table' : r.place <= ev.paid ? `${drOrd(r.place)} place, in the money` : r.place === ev.paid + 1 ? 'Bubble' : `Out in ${drOrd(r.place)}`;
  const acc = r.n ? Math.round(((r.counts.perfect + r.counts.good) / r.n) * 100) : 0;
  let card = '';
  if (ev.review) card = `<p class="drres-sub">${r.deck ? `${r.deck} spot${r.deck === 1 ? '' : 's'} left in your deck. The ones you missed stay for next time.` : 'Deck cleared. Every leak you saved is fixed.'}</p>`;
  else if (r.mode === 'challenge') card = `<div class="drchal-grid big">${r.grid || ''}</div><div class="drrow">${drBtn('gold sm', 'data-dr="chshare"', 'Share your score')}</div><small id="drShareMsg" class="drhint"></small>`;
  else if (ev.drill) card = `<p class="drres-sub">${r.acc >= 85 ? 'That leak is closing. Keep drilling it until the stars fill in.' : 'This is exactly the spot to keep working on.'}</p>`;
  else if (r.mode === 'casual') card = `<div class="drmoney"><div><small>Casual</small><b>Just for fun</b></div><div><small>Casual record</small><b class="mono">${s.m.casual.wins} W · ${s.m.casual.played} played</b></div></div>`;
  if (r.mode === 'ranked') { const t = drTier(r.r1); card = `${r.tierUp ? `<div class="drwin"><div class="drwin-icon">${r.tierUp[2]}</div><div><div class="drkicker">Ranked up</div><b>${esc(r.tierUp[1])}</b><small>Season emblem upgraded</small></div></div>` : ''}
    <div class="drmoney"><div><small>Rating</small><b class="mono">${r.r0} → ${r.r1}</b></div><div><small>Change</small><b class="mono ${r.delta >= 0 ? 'up' : 'down'}">${r.delta >= 0 ? '+' : '−'}${Math.abs(r.delta)}</b></div><div><small>Tier</small><b>${t[2]} ${esc(t[1])}</b></div></div>
    ${r.placing ? `<p class="drhint">Placement game ${r.placing} of ${DR_PLACEMENTS}.</p>` : ''}`; }
  if (r.mode === 'league') card = `<div class="drmoney"><div><small>League points</small><b class="mono up">+${r.pts}</b></div><div><small>Today</small><b class="mono">#${r.ranks[0]}</b></div><div><small>This week</small><b class="mono">#${r.ranks[1]}</b></div><div><small>This month</small><b class="mono">#${r.ranks[2]}</b></div></div>`;
  return `<section class="drres pl-${ev.venue} ${r.place === 1 ? 'champ' : ''}">
    <div class="drkicker">${esc(ev.name)}</div>
    <h1 class="drres-h">${esc(headline)}</h1><p class="drres-sub">${esc(`of ${f.field} · ${f.paid} paid${r.cooler ? ' · coolered' : ''}${r.place === 1 && r.luck > 0.03 ? ' · ran hot, earned it' : ''}`)}</p>
    ${card}
    ${drProgHTML(r.prog)}
    ${r.saved && !ev.review ? `<div class="drstreak">📌 ${r.saved} mistake${r.saved === 1 ? '' : 's'} saved to your deck for review</div>` : ''}
    ${drReviewHTML(r)}
    ${r.unlocked.length ? `<div class="drunlocks"><div class="drkicker">League exclusive unlocked</div>${r.unlocked.map((it) => `<div class="drunlock"><b>${esc(it.name)}</b><small>${esc(drLgReq(it))}</small></div>`).join('')}</div>` : ''}
    <div class="drrow">${drBtn('gold big', 'data-dr="hub"', 'Continue ▸')}${r.mode === 'challenge' ? '' : ev.review ? (r.deck ? drBtn('ghost', 'data-dr="review"', 'Review more') : '') : ev.drill ? drBtn('ghost', `data-dr="drill" data-k="${ev.drill}"`, 'Drill again') : drBtn('ghost', `data-dr="sng" data-m="${r.mode}" data-f="${ev.fmt}"`, 'Play again')}</div>
  </section>`;
}
