// ================================================================ THE DREAM: progression
// One career level across every mode (XP comes from decisions, not money), a level road of table
// cosmetics, daily quests, tiered achievements and skill mastery with drills.
// Reward rules: clothes and lifestyle come only from the Storyline; seasonal gear only from Leagues;
// ranked tiers earn ranked cosmetics; the level road and achievements give table cosmetics and titles.
// Nothing is ever for sale and there is no currency.

// ---------------------------------------------------------------- table cosmetics
const DR_BACKS = {
  classic: { n: 'House red', bg: 'repeating-linear-gradient(45deg,#b3262c 0 4px,#8e1b20 4px 8px)', bd: '#f3ecdc' },
  navy: { n: 'Navy diamonds', bg: 'repeating-linear-gradient(45deg,#1d3b6e 0 4px,#16305a 4px 8px),#1d3b6e', bd: '#cfd8ea' },
  emerald: { n: 'Emerald', bg: 'repeating-linear-gradient(-45deg,#126b47 0 3px,#0c5236 3px 7px)', bd: '#e8f5ec' },
  carbon: { n: 'Carbon fibre', bg: 'repeating-linear-gradient(45deg,#222 0 2px,#111 2px 4px),repeating-linear-gradient(-45deg,#1a1a1a 0 2px,#0c0c0c 2px 4px)', bd: '#5a5a5a' },
  crimson: { n: 'Crimson and gold', bg: 'radial-gradient(circle at 50% 50%,#e3b94f 0 18%,transparent 19%),repeating-linear-gradient(90deg,#7a0f1c 0 5px,#5f0b16 5px 10px)', bd: '#e3b94f' },
  neon: { n: 'Neon nights', bg: 'linear-gradient(135deg,#ff3cac,#784ba0 50%,#2b86c5)', bd: '#fff' },
  royal: { n: 'Royal purple', bg: 'radial-gradient(circle,#f4dfa6 0 12%,transparent 13%) 0 0/8px 8px,#4b1f7a', bd: '#f4dfa6' },
  obsidian: { n: 'Obsidian gold', bg: 'linear-gradient(135deg,#0b0b0d 40%,#3a2a0e 50%,#0b0b0d 60%)', bd: '#d9b36c' },
  diamond: { n: 'Diamond', bg: 'linear-gradient(135deg,#dfe9f3,#9fb4c7 30%,#f8fbff 50%,#9fb4c7 70%,#dfe9f3)', bd: '#fff' },
  rgold: { n: 'Ranked: Gold', bg: 'repeating-linear-gradient(60deg,#c99a2e 0 4px,#a37a1f 4px 8px)', bd: '#fff3c4' },
  rplat: { n: 'Ranked: Platinum', bg: 'repeating-linear-gradient(60deg,#7fb3c9 0 4px,#5f93a9 4px 8px)', bd: '#eaf6fb' },
};
const DR_FELTS = {
  green: { n: 'Classic green', c: '#0e4d39' }, blue: { n: 'Tournament blue', c: '#123f6b' }, burgundy: { n: 'Burgundy', c: '#5a1424' },
  black: { n: 'Black label', c: '#18181b' }, purple: { n: 'Velvet purple', c: '#3d1d5c' }, teal: { n: 'Deep teal', c: '#0b4f55' },
  goldtrim: { n: 'Gold trim', c: '#0e3b2d', trim: '#d9b36c' }, midnight: { n: 'Midnight', c: '#0a1022', trim: '#7fd0ff' },
  rdiamond: { n: 'Ranked: Diamond', c: '#0f2f4a', trim: '#b9f2ff' },
};
const DR_FRAMES = {
  none: { n: 'No frame', css: 'var(--line2)' }, bronze: { n: 'Bronze', css: 'linear-gradient(135deg,#a0643a,#e0a36f,#7a4524)' },
  silver: { n: 'Silver', css: 'linear-gradient(135deg,#8e9aa8,#f1f4f8,#6b7684)' }, gold: { n: 'Gold', css: 'linear-gradient(135deg,#b88a2e,#ffe9a8,#9a6f1c)' },
  flame: { n: 'Flame', css: 'conic-gradient(#ff5e3a,#ffb347,#ff2d55,#ff5e3a)' }, legend: { n: 'Legend', css: 'conic-gradient(#7fd0ff,#c79bff,#ffb37f,#8bd17c,#7fd0ff)' },
  rlegend: { n: 'Ranked: Legend', css: 'conic-gradient(#f4dfa6,#fff,#d9b36c,#fff,#f4dfa6)' },
};
const DR_TITLES_COS = {
  none: 'No title', grinder: 'Grinder', reg: 'The Reg', shark: 'Shark', highroller: 'High Roller', crusher: 'Crusher', livinglegend: 'Living Legend',
  professor: 'The Professor', ironwill: 'Iron Will', herocaller: 'Hero Caller', icm: 'ICM Scientist', heater: 'Heater', giantkiller: 'Giant Killer',
  closer: 'The Closer', robot: 'Robot', neverdead: 'Never Dead', taskmaster: 'Taskmaster', satshark: 'Satellite Shark', thickskin: 'Thick Skin', bounceback: 'Bounce Back',
};
const DR_COS_KINDS = [['back', 'Card backs', DR_BACKS, 'classic'], ['felt', 'Table felt', DR_FELTS, 'green'], ['frame', 'Avatar frame', DR_FRAMES, 'none'], ['title', 'Title', DR_TITLES_COS, 'none']];

// ---------------------------------------------------------------- the level road
const drXpNeed = (lvl) => 120 + 30 * (lvl - 1);
function drLevelOf(xp) { let l = 1, left = xp; while (left >= drXpNeed(l) && l < 99) { left -= drXpNeed(l); l++; } return { lvl: l, into: left, need: drXpNeed(l) }; }
const DR_ROAD = {
  2: ['title', 'grinder'], 3: ['back', 'navy'], 4: ['felt', 'blue'], 5: ['frame', 'bronze'], 6: ['back', 'emerald'], 7: ['title', 'reg'], 8: ['felt', 'burgundy'],
  10: ['back', 'carbon'], 11: ['frame', 'silver'], 12: ['felt', 'black'], 14: ['title', 'shark'], 15: ['back', 'crimson'], 18: ['felt', 'purple'], 20: ['back', 'neon'],
  22: ['frame', 'gold'], 24: ['felt', 'teal'], 26: ['title', 'highroller'], 28: ['back', 'royal'], 30: ['frame', 'flame'], 32: ['felt', 'goldtrim'], 36: ['back', 'obsidian'],
  38: ['title', 'crusher'], 42: ['felt', 'midnight'], 45: ['frame', 'legend'], 50: ['back', 'diamond'], 51: ['title', 'livinglegend'],
};
const drCosName = (kind, id) => (kind === 'title' ? DR_TITLES_COS[id] : ({ back: DR_BACKS, felt: DR_FELTS, frame: DR_FRAMES }[kind][id] || {}).n) || id;
const DR_COS_KIND_N = { back: 'Card back', felt: 'Table felt', frame: 'Avatar frame', title: 'Title' };

// ---------------------------------------------------------------- daily quests
const DR_QUESTS = [
  { id: 'textbook', t: 'Make 8 Best-or-better decisions', n: 8, xp: 60 },
  { id: 'combo', t: 'Get 5 good decisions in a row', n: 5, xp: 60 },
  { id: 'jam', t: 'Get 4 "facing a jam" spots right', n: 4, xp: 70, ch: 0 },
  { id: 'story', t: 'Finish a Storyline event', n: 1, xp: 50 },
  { id: 'cash', t: 'Finish in the money', n: 1, xp: 70 },
  { id: 'sngwin', t: 'Win a sit & go', n: 1, xp: 90, ch: 1 },
  { id: 'league', t: 'Play 2 league games', n: 2, xp: 60, ch: 1 },
  { id: 'ranked', t: 'Play a ranked game', n: 1, xp: 50, ch: 1 },
  { id: 'acc', t: 'Finish a game with 85%+ accuracy', n: 1, xp: 80 },
  { id: 'find', t: 'Find a Brilliant or Great play', n: 1, xp: 70 },
  { id: 'icm', t: 'Make 5 good decisions on a bubble or final table', n: 5, xp: 60 },
  { id: 'grind', t: 'Play the Daily Grind', n: 1, xp: 40, ch: 1 },
  { id: 'drill', t: 'Finish a skill drill', n: 1, xp: 60, ch: 1 },
];
const DR_QUEST_BONUS = 100;

// ---------------------------------------------------------------- achievements
const DR_TIER_N = ['Bronze', 'Silver', 'Gold'];
const DR_ACH = [
  { id: 'textbook', n: 'Textbook', d: 'Best-or-better decisions', t: [25, 150, 600], f: (p) => p.st.textbook, gold: ['title', 'professor'] },
  { id: 'brilliant', n: 'Brilliancy', d: 'Brilliant or Great finds', t: [3, 20, 75], f: (p) => p.st.finds },
  { id: 'jamcall', n: 'Snap Caller', d: 'Correct calls facing a jam', t: [5, 30, 120], f: (p) => p.st.jamCall, gold: ['title', 'herocaller'] },
  { id: 'jamfold', n: 'Iron Discipline', d: 'Correct folds facing a jam', t: [5, 30, 120], f: (p) => p.st.jamFold, gold: ['title', 'ironwill'] },
  { id: 'bubble', n: 'Bubble Survivor', d: 'Made the money from the bubble', t: [3, 15, 50], f: (p) => p.st.bubble, gold: ['title', 'icm'] },
  { id: 'wins', n: 'Closer', d: 'Tournaments and sit & gos won', t: [1, 10, 40], f: (p) => p.st.wins, gold: ['title', 'closer'] },
  { id: 'streak', n: 'Hot Hand', d: 'Good decisions in a row', t: [8, 15, 25], f: (p) => p.st.bestCombo, gold: ['title', 'heater'] },
  { id: 'days', n: 'Grinder', d: 'Different days played', t: [3, 14, 45], f: (p) => p.days.length },
  { id: 'legends', n: 'Legend Slayer', d: 'Legends beaten heads-up', t: [1, 8, 25], f: (p, s) => Object.keys(s.legends || {}).length, gold: ['title', 'giantkiller'] },
  { id: 'flawless', n: 'Flawless', d: 'Games at 100% accuracy (6+ decisions)', t: [1, 5, 20], f: (p) => p.st.flawless, gold: ['title', 'robot'] },
  { id: 'comeback', n: 'Comeback Kid', d: 'Won after running on fumes', t: [1, 3, 10], f: (p) => p.st.comeback, gold: ['title', 'neverdead'] },
  { id: 'coolers', n: 'Thick Skin', d: 'Coolered while playing 85%+ accurate', t: [1, 5, 15], f: (p) => p.st.cleanBeats || 0, gold: ['title', 'thickskin'] },
  { id: 'bounce', n: 'Bounce Back', d: 'Won the very next event after a cooler', t: [1, 3, 10], f: (p) => p.st.bounce || 0, gold: ['title', 'bounceback'] },
  { id: 'seats', n: 'Satellite Shark', d: 'Satellite seats won', t: [1, 5, 15], f: (p) => p.st.seats, gold: ['title', 'satshark'] },
  { id: 'league', n: 'League Regular', d: 'League games played', t: [10, 50, 150], f: (p, s) => (s.m && s.m.league.played) || 0 },
  { id: 'ranked', n: 'Ladder Climber', d: 'Best ranked tier (Silver, Gold, Diamond)', t: [1, 2, 4], f: (p, s) => (s.rk ? DR_TIERS.indexOf(drTier(s.rk.peak || 1000)) : 0), fmt: (v) => (DR_TIERS[v] || DR_TIERS[0])[1] },
  { id: 'skills', n: 'Well Rounded', d: 'Skills at 2 stars or more', t: [2, 4, 6], f: (p) => DR_SKILLS.filter((k) => drSkillStars(p, k[0]) >= 2).length },
  { id: 'quests', n: 'Taskmaster', d: 'Daily quests completed', t: [10, 60, 200], f: (p) => p.st.quests, gold: ['title', 'taskmaster'] },
  { id: 'bracelets', n: 'Jewelry Box', d: 'Gold bracelets (Storyline)', t: [1, 5, 15], f: (p, s) => (s.titles && s.titles.bracelet) || 0 },
  { id: 'collector', n: 'Collector', d: 'Cosmetics and gear owned', t: [8, 20, 40], f: (p, s) => Object.keys(p.cos).length + Object.keys(s.owned || {}).length },
];
const DR_ACH_XP = [50, 150, 400];

// ---------------------------------------------------------------- skills
const DR_SKILLS = [['first', 'First in', 'Opening, or folding, when nobody has raised'], ['raise', 'Facing a raise', 'Calling, 3-betting or folding to an open'], ['threebet', 'Facing a 3-bet+', 'When the pot has been re-raised'], ['jam', 'Facing a jam', 'Call off or let it go'], ['icm', 'Bubble and final table', 'Decisions where ICM bends the math'], ['short', 'Short stack', '15 big blinds or fewer']];
function drSkillKeys(hand, stage) {
  const k = [hand.cat === 'Facing a jam' ? 'jam' : hand.cat === 'Facing a raise' ? 'raise' : hand.cat === 'Facing a 3-bet or more' ? 'threebet' : 'first'];
  if (['bubble', 'satbubble', 'deep', 'final'].includes(stage)) k.push('icm');
  if (hand.bb <= 15) k.push('short');
  return k;
}
function drSkillAcc(p, k) { const a = p.sk[k] || []; return a.length ? Math.round(a.reduce((x, y) => x + y, 0) / a.length) : null; }
function drSkillStars(p, k) { const a = p.sk[k] || [], acc = drSkillAcc(p, k); if (a.length < 8 || acc == null) return 0; return acc >= 90 ? 3 : acc >= 78 ? 2 : acc >= 62 ? 1 : 0; }
function drWeakest(p) { const c = DR_SKILLS.filter(([k]) => k !== 'short').map(([k]) => [k, drSkillAcc(p, k), (p.sk[k] || []).length]).filter((x) => x[2] >= 6).sort((a, b) => a[1] - b[1]); return c[0] ? c[0][0] : null; }

// ---------------------------------------------------------------- state
function drPg(s) {
  if (!s.pg) s.pg = { xp: 0, st: { textbook: 0, finds: 0, jamCall: 0, jamFold: 0, bubble: 0, wins: 0, bestCombo: 0, flawless: 0, comeback: 0, seats: 0, quests: 0 }, days: [], sk: {}, ach: {}, cos: { 'back:classic': 1, 'felt:green': 1, 'frame:none': 1, 'title:none': 1 }, eq: { back: 'classic', felt: 'green', frame: 'none', title: 'none' }, q: null, news: [] };
  const p = s.pg, today = drToday();
  if (!p.q || p.q.day !== today) {
    // three quests a day, the same for everyone that day, filtered to what you can play
    let h = 0; for (const ch of today) h = (h * 31 + ch.charCodeAt(0)) >>> 0;
    const pool = DR_QUESTS.filter((q) => (q.ch || 0) <= s.ch).map((q) => q.id), pick = [];
    while (pick.length < 3 && pool.length) { h = (h * 1103515245 + 12345) >>> 0; pick.push(pool.splice(h % pool.length, 1)[0]); }
    p.q = { day: today, list: pick.map((id) => ({ id, v: 0, done: false })), bonus: false };
  }
  return p;
}
function drGrant(s, kind, id, why) {
  const p = drPg(s), key = kind + ':' + id; if (p.cos[key]) return null;
  p.cos[key] = 1; p.news.push({ kind, id, why });
  // wear it straight away if you're still on the starter look
  const starter = { back: 'classic', felt: 'green', frame: 'none', title: 'none' };
  if (p.eq[kind] === starter[kind]) p.eq[kind] = id;
  return { kind, id, why };
}
function drGainXp(s, amt, out) {
  const p = drPg(s), before = drLevelOf(p.xp).lvl; p.xp += Math.max(0, Math.round(amt));
  const after = drLevelOf(p.xp).lvl;
  for (let l = before + 1; l <= after; l++) {
    out.levels.push(l);
    const r = DR_ROAD[l]; if (r) { const g = drGrant(s, r[0], r[1], `Level ${l}`); if (g) out.cos.push(g); }
    if (typeof drMoment === 'function') drMoment(s, '⬆️', `Reached career level ${l}`);
  }
}
function drQuestBump(s, id, by, out) {
  const p = drPg(s), q = p.q.list.find((x) => x.id === id); if (!q || q.done) return;
  const def = DR_QUESTS.find((x) => x.id === id); q.v = id === 'combo' ? Math.max(q.v, by) : q.v + by;
  if (q.v >= def.n) { q.done = true; q.v = def.n; p.st.quests++; out.quests.push(def); drGainXp(s, def.xp, out); }
  if (!p.q.bonus && p.q.list.every((x) => x.done)) { p.q.bonus = true; out.bonus = true; drGainXp(s, DR_QUEST_BONUS, out); }
}
function drCheckAch(s, out) {
  const p = drPg(s);
  for (const a of DR_ACH) {
    const v = a.f(p, s) || 0, have = p.ach[a.id] || 0;
    let tier = have; while (tier < 3 && v >= a.t[tier]) tier++;
    for (let t = have; t < tier; t++) {
      out.ach.push({ a, tier: t }); drGainXp(s, DR_ACH_XP[t], out);
      if (t === 2 && a.gold) { const g = drGrant(s, a.gold[0], a.gold[1], `${a.n} (Gold)`); if (g) out.cos.push(g); }
    }
    p.ach[a.id] = tier;
  }
}
// ranked tiers reached unlock ranked cosmetics
function drRankedCos(s, out) {
  if (!s.rk) return; const ti = DR_TIERS.indexOf(drTier(s.rk.peak || s.rk.r));
  if (ti >= 2) { const g = drGrant(s, 'back', 'rgold', 'Reached Gold in Ranked'); if (g) out.cos.push(g); }
  if (ti >= 3) { const g = drGrant(s, 'back', 'rplat', 'Reached Platinum in Ranked'); if (g) out.cos.push(g); }
  if (ti >= 4) { const g = drGrant(s, 'felt', 'rdiamond', 'Reached Diamond in Ranked'); if (g) out.cos.push(g); }
  if (ti >= 5) { const g = drGrant(s, 'frame', 'rlegend', 'Reached Legend in Ranked'); if (g) out.cos.push(g); }
}

// ---------------------------------------------------------------- hooks from the game
const drOut = () => ({ xp0: 0, xp: 0, levels: [], cos: [], quests: [], ach: [], bonus: false });
// after every decision
function drTrack(r, run) {
  const s = DR.st, p = drPg(s), h = r.hand, good = r.tier === 'perfect' || r.tier === 'good';
  if (!run.prog) { run.prog = drOut(); run.prog.xp0 = p.xp; }
  const out = run.prog;
  const sc = DR_LABELS[r.lab] ? DR_LABELS[r.lab].score : good ? 85 : 20;
  for (const k of drSkillKeys(h, r.stage)) { const a = (p.sk[k] = p.sk[k] || []); a.push(sc); if (a.length > 30) a.shift(); }
  // the mistake deck: save every Mistake or Blunder as the exact spot; fixing it in a review clears it
  const sp = run.spot, ref = sp && sp.ref;
  if (ref) {
    p.mis = p.mis || []; const key = (x) => `${x.g}|${x.t}|${x.n}|${x.c}`, i = p.mis.findIndex((x) => key(x) === key(ref));
    if (run.ev.review) { if (good && i >= 0) { p.mis.splice(i, 1); run.fixed = (run.fixed || 0) + 1; } }
    else if (r.tier === 'bad' && i < 0) { p.mis.push({ ...ref, lab: r.lab, d: Date.now() }); if (p.mis.length > 60) p.mis.shift(); run.saved = (run.saved || 0) + 1; }
  }
  if (r.tier === 'perfect') { p.st.textbook++; drQuestBump(s, 'textbook', 1, out); }
  if (r.lab === 'brilliant' || r.lab === 'great') { p.st.finds++; drQuestBump(s, 'find', 1, out); }
  if (h.cat === 'Facing a jam' && good) { if (h.mineK === 'fold') p.st.jamFold++; else p.st.jamCall++; drQuestBump(s, 'jam', 1, out); }
  if (good && ['bubble', 'satbubble', 'deep', 'final'].includes(r.stage)) drQuestBump(s, 'icm', 1, out);
  p.st.bestCombo = Math.max(p.st.bestCombo, run.combo || 0); drQuestBump(s, 'combo', run.combo || 0, out);
  if ((run.stack || 0) < 40) run.fumes = true;
  drGainXp(s, Math.round(sc / 10), out);
}
// after every finished event, any mode
function drEventDone(res, run) {
  const s = DR.st, p = drPg(s), ev = res.ev, today = drToday();
  if (!run.prog) { run.prog = drOut(); run.prog.xp0 = p.xp; }
  const out = run.prog;
  if (!p.days.includes(today)) { p.days.push(today); if (p.days.length > 400) p.days.shift(); }
  const kind = ev.kind, paid = kind === 'sng' ? ev.paid : kind === 'sat' ? ev.seats : kind === 'cash' ? 0 : drPaid(ev);
  const cashed = kind === 'cash' ? res.net > 0 : res.place && res.place <= paid, won = res.place === 1 && kind !== 'cash';
  let xp = 20 + (cashed ? 25 : 0) + (won ? 60 : 0) + (res.acc >= 85 ? 20 : 0);
  if (kind === 'sng' && ev.mode === 'casual') xp = Math.round(xp * 0.6);
  drGainXp(s, xp, out);
  if (won) { p.st.wins++; if (run.fumes) p.st.comeback++; }
  // coolers: part of the game. Count them, reward taking them well, reward the bounce back
  if (res.cooler) { p.st.coolers = (p.st.coolers || 0) + 1; if (res.acc >= 85) p.st.cleanBeats = (p.st.cleanBeats || 0) + 1; p.lastCool = true; }
  else if (!ev.drill && !ev.review && res.mode !== 'challenge') { if (won && p.lastCool) p.st.bounce = (p.st.bounce || 0) + 1; p.lastCool = false; }
  if (kind === 'sat' && res.ticket) p.st.seats++;
  if (cashed && run.stages.includes('bubble') && run.si >= run.stages.indexOf('bubble')) p.st.bubble++;
  if (res.acc === 100 && res.n >= 6) p.st.flawless++;
  if (kind !== 'sng') drQuestBump(s, 'story', 1, out);
  if (cashed && kind !== 'cash') drQuestBump(s, 'cash', 1, out);
  if (kind === 'sng' && won && !ev.drill) drQuestBump(s, 'sngwin', 1, out);
  if (kind === 'sng' && ev.mode === 'league') drQuestBump(s, 'league', 1, out);
  if (kind === 'sng' && ev.mode === 'ranked') drQuestBump(s, 'ranked', 1, out);
  if (ev.daily) drQuestBump(s, 'grind', 1, out);
  if (ev.drill || ev.review) drQuestBump(s, 'drill', 1, out);
  res.fixed = run.fixed || 0; res.saved = run.saved || 0; res.deck = (p.mis || []).length;
  if (res.acc >= 85 && res.n >= 5) drQuestBump(s, 'acc', 1, out);
  drRankedCos(s, out); drCheckAch(s, out);
  out.lvl = drLevelOf(p.xp); out.xp = p.xp - out.xp0;
  res.prog = out;
}

// ---------------------------------------------------------------- the streak: consecutive days you played anything
function drStreak(s) {
  const p = drPg(s), set = new Set(p.days), d = new Date(); let n = 0;
  if (!set.has(drToday(d))) d.setDate(d.getDate() - 1); // yesterday still counts until midnight
  while (set.has(drToday(d))) { n++; d.setDate(d.getDate() - 1); }
  return { n, today: set.has(drToday()) };
}

// ---------------------------------------------------------------- screens
function drFrameStyle(s) { const p = drPg(s), f = DR_FRAMES[p.eq.frame] || DR_FRAMES.none; return `--frame:${f.css}`; }
function drTitleBadge(s) { const p = drPg(s); return p.eq.title && p.eq.title !== 'none' ? `<span class="drtitlebadge">${esc(DR_TITLES_COS[p.eq.title])}</span>` : ''; }
function drLevelChip(s) { const L = drLevelOf(drPg(s).xp); return `<button class="drlvl" data-dr="profile" aria-label="Career level ${L.lvl}, open your profile"><b>${L.lvl}</b><span><i style="width:${((L.into / L.need) * 100).toFixed(1)}%"></i></span></button>`; }
function drQuestsHTML(s, compact) {
  const p = drPg(s), done = p.q.list.filter((q) => q.done).length;
  return `<section class="drquests ${compact ? 'compact' : ''}"><div class="drquests-h"><div class="drkicker">Daily quests · ${done}/3</div><small>New quests at midnight · all three: +${DR_QUEST_BONUS} XP</small></div>
    ${p.q.list.map((q) => { const d = DR_QUESTS.find((x) => x.id === q.id); return `<div class="drquest ${q.done ? 'done' : ''}"><span class="drquest-ck">${q.done ? '✓' : ''}</span><div><b>${esc(d.t)}</b><div class="drbar"><i style="width:${Math.min(100, (q.v / d.n) * 100)}%"></i></div></div><small class="mono">${q.done ? `+${d.xp}` : `${q.v}/${d.n}`}</small></div>`; }).join('')}</section>`;
}
function drCosSwatch(kind, id) {
  if (kind === 'back') { const b = DR_BACKS[id]; return `<span class="drcos-back" style="background:${b.bg};border-color:${b.bd}"></span>`; }
  if (kind === 'felt') { const f = DR_FELTS[id]; return `<span class="drcos-felt" style="--fc:${f.c};--ft:${f.trim || 'transparent'}"></span>`; }
  if (kind === 'frame') return `<span class="drcos-frame" style="--frame:${DR_FRAMES[id].css}"></span>`;
  return `<span class="drcos-title">${esc(DR_TITLES_COS[id])}</span>`;
}
function drCosSource(kind, id) {
  const lv = Object.entries(DR_ROAD).find(([, r]) => r[0] === kind && r[1] === id); if (lv) return `Career level ${lv[0]}`;
  const a = DR_ACH.find((x) => x.gold && x.gold[0] === kind && x.gold[1] === id); if (a) return `${a.n}: Gold`;
  return { rgold: 'Reach Gold in Ranked', rplat: 'Reach Platinum in Ranked', rdiamond: 'Reach Diamond in Ranked', rlegend: 'Reach Legend in Ranked' }[id] || 'Starter';
}
function drProfileHTML() {
  const s = DR.st, p = drPg(s), L = drLevelOf(p.xp), tab = DR.ptab || 'quests', weak = drWeakest(p);
  if (p.news.length) { p.news = []; drSave(); }
  const next = Object.entries(DR_ROAD).filter(([l]) => +l > L.lvl).slice(0, 4);
  const body = tab === 'skills' ? `${drDeckCard(s)}<p class="drhint">Accuracy over your last 30 decisions of each kind, from every mode. Drills deal only that kind of spot, so you can fix a leak fast.</p>
      <div class="drskills">${DR_SKILLS.map(([k, n, d]) => { const acc = drSkillAcc(p, k), cnt = (p.sk[k] || []).length, st = drSkillStars(p, k);
        return `<div class="drskill ${k === weak ? 'weak' : ''}"><div><b>${esc(n)}</b><small>${esc(d)}</small></div><div class="drstars">${[1, 2, 3].map((i) => `<i class="${i <= st ? 'on' : ''}">★</i>`).join('')}</div>
          <div class="drskill-acc"><b class="mono">${acc == null ? '–' : acc + '%'}</b><small>${cnt < 8 ? `${cnt}/8 to rate` : `${cnt} decisions`}</small></div>${k === 'short' ? '' : drBtn(k === weak ? 'gold sm' : 'ghost sm', `data-dr="drill" data-k="${k}" ${s.ch >= 1 ? '' : 'disabled'}`, k === weak ? 'Drill your leak' : 'Drill')}</div>`; }).join('')}</div>`
    : tab === 'ach' ? `<div class="drachs">${DR_ACH.map((a) => { const t = p.ach[a.id] || 0, v = a.f(p, s) || 0, goal = a.t[Math.min(2, t)];
        return `<div class="drach t${t}"><div class="drach-medal">${['🔒', '🥉', '🥈', '🥇'][t]}</div><div><b>${esc(a.n)}</b><small>${esc(a.d)}${a.gold ? ` · Gold: “${esc(drCosName(a.gold[0], a.gold[1]))}”` : ''}</small><div class="drbar"><i style="width:${t >= 3 ? 100 : Math.min(100, (v / goal) * 100)}%"></i></div></div><small class="mono">${t >= 3 ? 'Done' : `${a.fmt ? a.fmt(v) : drMoney(v)}/${a.fmt ? a.fmt(goal) : drMoney(goal)}`}</small></div>`; }).join('')}</div>`
    : tab === 'cos' ? DR_COS_KINDS.map(([kind, label, cat]) => `<div class="drkicker">${label}</div><div class="drcosgrid">${Object.keys(cat).map((id) => { const own = !!p.cos[kind + ':' + id], on = p.eq[kind] === id;
        return `<button class="drcos ${own ? '' : 'locked'} ${on ? 'on' : ''}" data-dr="coseq" data-kind="${kind}" data-id="${id}" ${own ? '' : 'disabled'}>${drCosSwatch(kind, id)}<b>${esc(drCosName(kind, id))}</b><small>${own ? (on ? 'Using' : 'Tap to use') : '🔒 ' + esc(drCosSource(kind, id))}</small></button>`; }).join('')}</div>`).join('')
      + `<p class="drhint">Clothes, jewelry, homes and rides are in the Storyline closet. Seasonal gear comes from Leagues.</p>`
    : tab === 'backup' ? `<p class="drhint">Your career lives on this device. Copy this code somewhere safe to move it to another phone or restore it later.</p>
      <textarea id="drSaveCode" class="drsavecode" readonly>${esc(drExportCode())}</textarea>
      <div class="drrow">${drBtn('gold sm', 'data-dr="savecopy"', 'Copy code')}</div>
      <div class="drkicker">Restore</div><textarea id="drLoadCode" class="drsavecode" placeholder="Paste a backup code"></textarea>
      <div class="drrow">${drBtn('ghost sm', 'data-dr="saveload"', 'Restore this career')}</div><small id="drSaveMsg" class="drhint"></small>`
    : drQuestsHTML(s) + `<div class="drkicker">Level road</div><div class="drroad">${next.map(([l, [kind, id]]) => `<div class="drroad-step"><span class="mono">Lv ${l}</span>${drCosSwatch(kind, id)}<small>${esc(DR_COS_KIND_N[kind])}<br><b>${esc(drCosName(kind, id))}</b></small></div>`).join('') || '<p class="drhint">You have unlocked the whole road.</p>'}</div>
      <p class="drhint">You earn XP from every decision in every mode (better decisions, more XP), plus finishing games, quests and achievements.</p>`;
  return `<section class="drprofile">
    <div class="drprofile-top"><div class="drprof-av" style="${drFrameStyle(s)}">${drAvatarSVG(s, { face: true, mouth: 'smug' })}</div>
      <div><b class="drprofile-name">${esc(s.name)}</b>${drTitleBadge(s)}<div class="drkicker">Career level ${L.lvl}</div><div class="drbar xp"><i style="width:${((L.into / L.need) * 100).toFixed(1)}%"></i></div><small class="mono">${drMoney(L.into)} / ${drMoney(L.need)} XP</small></div>
      ${drBtn('ghost sm', 'data-dr="hub"', 'Done')}</div>
    <nav class="drtabs" role="tablist">${[['quests', 'Quests'], ['skills', 'Skills'], ['ach', 'Achievements'], ['cos', 'Collection'], ['backup', 'Backup']].map(([k, n]) => `<button role="tab" data-dr="ptab" data-t="${k}" aria-selected="${tab === k}">${n}</button>`).join('')}</nav>
    <div class="drtabbody">${body}</div></section>`;
}
// what you earned this game: XP bar, level-ups, unlocks, quests, achievements
function drProgHTML(out) {
  if (!out) return '';
  const L = out.lvl || drLevelOf(drPg(DR.st).xp);
  return `<div class="drprog">
    <div class="drprog-xp"><span class="drlvl big"><b>${L.lvl}</b></span><div><div class="drkicker">Career XP · +${drMoney(out.xp)}</div><div class="drbar xp"><i style="width:${((L.into / L.need) * 100).toFixed(1)}%"></i></div><small class="mono">${drMoney(L.into)} / ${drMoney(L.need)} to level ${L.lvl + 1}</small></div></div>
    ${out.levels.length ? `<div class="drwin lvl"><div class="drwin-icon">⬆️</div><div><div class="drkicker">Level up</div><b>Career level ${out.levels[out.levels.length - 1]}</b></div></div>` : ''}
    ${out.cos.map((c) => `<div class="drunlock cos">${drCosSwatch(c.kind, c.id)}<div><b>${esc(drCosName(c.kind, c.id))}</b><small>${esc(DR_COS_KIND_N[c.kind])} · ${esc(c.why)}</small></div></div>`).join('')}
    ${out.quests.map((q) => `<div class="drstreak">✅ Quest done: ${esc(q.t)} · +${q.xp} XP</div>`).join('')}${out.bonus ? `<div class="drstreak">🌟 All three quests done today · +${DR_QUEST_BONUS} XP</div>` : ''}
    ${out.ach.map((x) => `<div class="drunlock ach"><span class="drach-medal">${['🥉', '🥈', '🥇'][x.tier]}</span><div><b>${esc(x.a.n)}: ${DR_TIER_N[x.tier]}</b><small>${esc(x.a.d)} · +${DR_ACH_XP[x.tier]} XP</small></div></div>`).join('')}
  </div>`;
}

// ---------------------------------------------------------------- drills: only the kind of spot you're working on
function drNodeKind(node) {
  const p = node.p, me = node.h; if (p.includes('allin')) return 'jam';
  const raises = p.filter((k) => k === 'raise' || k === 'raiseB').length;
  return raises === 0 ? 'first' : raises === 1 && !node.s2 ? 'raise' : 'threebet';
}
function drDrillStart(kind) {
  const s = DR.st; if (DR.run || s.ch < 1) return;
  const ev = drSngEvent('casual', 'sng9'); ev.drill = kind; ev.id = 'drill:' + kind;
  ev.name = `Drill: ${(DR_SKILLS.find((x) => x[0] === kind) || [0, kind])[1]}`;
  drModeState(s);
  const rng = arRng(`${ev.id}|${Date.now()}|${Math.random()}`);
  DR.saved = snapSetup(); TR.random = true; window.__trRand = true;
  if (S.variant !== 'nlh') setVariant('nlh');
  DR.run = { ev, stages: kind === 'icm' ? ['bubble', 'final', 'bubble'] : ['early', 'bubble', 'final'], si: 0, h: 0, stack: 100, results: [], total: 0, rng, state: 'loading', spot: null, boss: null, fails: 0, bust: false, combo: 0, t0: performance.now(), tip: 0, noBust: true };
  DR.view = 'run'; drRender(); drTable();
}

// ---------------------------------------------------------------- the mistake deck
const DR_GID_STAGE = { ring: 'early', bubble: 'bubble', shove: 'final', sat: 'satbubble', cash: 'cash' };
// mistakes recorded on spots that later failed the library quality gate are not your mistakes: drop them
function drMisClean(p) { if (!p.mis || typeof drLib !== 'function') return; p.mis = p.mis.filter((it) => { const t = drLib()[it.g] && drLib()[it.g][it.t], n = t && t.n[it.n]; return !(n && n.bad); }); }
function drReviewStart() {
  const s = DR.st, p = drPg(s); drMisClean(p); if (DR.run || !(p.mis || []).length) return;
  const queue = p.mis.slice(0, 9).filter((it) => DR_GID_STAGE[it.g]);
  if (!queue.length) return;
  drModeState(s);
  const ev = drSngEvent('casual', 'sng9'); ev.review = true; ev.id = 'review'; ev.name = 'Mistake review';
  DR.saved = snapSetup(); TR.random = true; window.__trRand = true;
  if (S.variant !== 'nlh') setVariant('nlh');
  DR.run = { ev, queue, stages: queue.map((it) => DR_GID_STAGE[it.g]), per: 1, si: 0, h: 0, stack: 100, results: [], total: 0, rng: arRng(`review|${Date.now()}`), state: 'loading', spot: null, boss: null, fails: 0, bust: false, combo: 0, t0: performance.now(), tip: 0, noBust: true };
  DR.view = 'run'; drRender(); drTable();
}
function drDeckCard(s) {
  drMisClean(drPg(s));
  const n = (drPg(s).mis || []).length;
  return `<section class="drdeck ${n ? '' : 'empty'}"><div><div class="drkicker">Your mistake deck</div><b>${n ? `${n} spot${n === 1 ? '' : 's'} to fix` : 'Nothing to fix right now'}</b><small>${n ? 'Every Mistake or Blunder is saved as the exact spot. Get it right in a review and it leaves the deck.' : 'Mistakes and Blunders from any mode land here for review.'}</small></div>${n ? drBtn('gold sm', 'data-dr="review"', `Review ${Math.min(9, n)} ▸`) : ''}</section>`;
}

// ---------------------------------------------------------------- backup codes
function drExportCode() { try { return 'CFD1.' + btoa(unescape(encodeURIComponent(JSON.stringify(DR.st)))); } catch { return ''; } }
function drImportCode(code) {
  try {
    const raw = String(code || '').trim(); if (!raw.startsWith('CFD1.')) return 'That is not a Dream backup code.';
    const v = JSON.parse(decodeURIComponent(escape(atob(raw.slice(5))))); if (!v || v.v !== 1 || !v.made) return 'That code looks damaged.';
    localStorage.setItem(DR_KEY, JSON.stringify(v)); DR.st = drLoad(); return '';
  } catch { return 'That code could not be read.'; }
}
