// ================================================================ THE DREAM: the calendar
// The career runs on a calendar, Sunday to Saturday. Every day has its games: the card room every night,
// Sundays online, and the majors (Circuit stops, the WCS Summer Series, the Royal Tour, the GLO Cup, the
// Titan Series) on their dates. Quit your job and you can play 3 games a day; keep it and you get a
// paycheck every Friday but only 3 games a week. Rent is due on the 1st. Go to sleep to start a new day.
// Live events follow the real-world calendar (holidays, specials) and give gear you can't get anywhere else.

const DR_CAL0 = Date.UTC(2026, 0, 5); // day 0: Monday, January 5 of Year 1 (the prologue)
const DR_DOW = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const DR_DOW_L = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const DR_MON = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const DR_MON_L = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
function drDate(d) {
  const t = new Date(DR_CAL0 + d * 864e5), y = t.getUTCFullYear(), m = t.getUTCMonth();
  return { y: y - 2025, Y: y, m: m + 1, dd: t.getUTCDate(), dow: t.getUTCDay(), last: new Date(Date.UTC(y, m + 1, 0)).getUTCDate() };
}
const drDayOf = (Y, m, dd) => Math.round((Date.UTC(Y, m - 1, dd) - DR_CAL0) / 864e5);
const drWeekOf = (d) => Math.floor((d + 1) / 7); // weeks run Sunday to Saturday (day 0 is a Monday)
const drDateStr = (d, long) => { const D = drDate(d); return long ? `${DR_DOW_L[D.dow]}, ${DR_MON_L[D.m - 1]} ${D.dd}` : `${DR_DOW[D.dow]} ${DR_MON[D.m - 1]} ${D.dd}`; };

// ---------------------------------------------------------------- money: paychecks, rent, travel
const DR_SALARY = 2000; // every Friday while you keep the day job
const DR_GIFT_QUIT = 5000; // Slick backs you when you quit
const DR_GAMES_CAP = 3; // per day full-time, per week with the day job
const DR_UPKEEP = {
  home: { studio: 650, apt: 1600, condo: 4200, penthouse: 15000, mansion: 42000, island: 120000 },
  ride: { bus: 90, hatch: 320, coupe: 750, sedan: 1600, super: 6500, jet: 45000 },
  living: 700,
};
const drBills = (s) => (DR_UPKEEP.home[s.eq.home] || 0) + (DR_UPKEEP.ride[s.eq.ride] || 0) + DR_UPKEEP.living;
// hotel and travel, once per day you play away from home
const DR_TRAVEL = { vegas: 250, main: 250, tv: 350, titan: 1000 };
// below this your roll is in danger: freerolls open up and Slick starts showing up
const DR_LOW = [0, 400, 2000, 8000, 30000, 100000, 100000, 100000];
const drLow = (s) => s.br < DR_LOW[Math.min(7, s.ch)];
const drBroke = (s) => s.br < 60;

// ---------------------------------------------------------------- the schedule
const DR_EVERY_DAY = ['c12', 'nightly', 'tilt11', 'c25'];
const DR_WEEKDAY = { 0: ['deep', 'tilt55', 'tilt215', 'tilt2100'], 2: ['tilt530'] };
// majors: month, start day, length, and what runs on which day of the stop ('daily' = every day of it)
const DR_SERIES = [
  { id: 'rtpi', n: 'Royal Tour Paradise Islands', venue: 'tv', icon: '👑', m: 1, from: 10, days: 7, ev: [[1, 'rpt3500'], ['all', 'c1025']] },
  { id: 'glow', n: 'GLO Cup Winter', venue: 'tv', icon: '🥇', m: 1, from: 21, days: 6, ev: [[1, 'glo10k'], [4, 'glo10k']] },
  { id: 'cfeb', n: 'Circuit: Silver Strike', venue: 'casino', icon: '💍', m: 2, from: 10, days: 10, ev: [['0-6', 'sat1700'], [1, 'ring400'], [3, 'ring600'], [5, 'ring400'], [7, 'ring1700']] },
  { id: 'rtla', n: 'Royal Tour LA Classic', venue: 'tv', icon: '👑', m: 3, from: 14, days: 7, ev: [[2, 'rpt3500'], ['all', 'c1025']] },
  { id: 'capr', n: 'Circuit: Silver Strike', venue: 'casino', icon: '💍', m: 4, from: 2, days: 10, ev: [['0-6', 'sat1700'], [1, 'ring400'], [3, 'ring600'], [5, 'ring400'], [7, 'ring1700']] },
  { id: 'tspr', n: 'Titan Series Spring', venue: 'titan', icon: '🔱', m: 4, from: 18, days: 10, ev: [['0-4', 'sat25k'], [2, 't25k'], [5, 't25k'], [7, 't100k'], ['all', 'private']] },
  { id: 'wcs', n: 'WCS Summer Series', venue: 'vegas', icon: '📿', m: 5, from: 30, days: 48, wcs: true },
  { id: 'caug', n: 'Circuit: Silver Strike', venue: 'casino', icon: '💍', m: 8, from: 4, days: 10, ev: [['0-6', 'sat1700'], [1, 'ring400'], [3, 'ring600'], [5, 'ring400'], [7, 'ring1700']] },
  { id: 'rtfl', n: 'Royal Tour Florida', venue: 'tv', icon: '👑', m: 8, from: 22, days: 7, ev: [[2, 'rpt3500'], ['all', 'c1025']] },
  { id: 'tfal', n: 'Titan Series Fall', venue: 'titan', icon: '🔱', m: 9, from: 12, days: 10, ev: [['0-4', 'sat25k'], [2, 't25k'], [5, 't25k'], [7, 't100k'], [8, 't250k'], ['all', 'private']] },
  { id: 'glof', n: 'GLO Cup Fall', venue: 'tv', icon: '🥇', m: 10, from: 20, days: 6, ev: [[1, 'glo10k'], [4, 'glo10k']] },
  { id: 'cnov', n: 'Circuit: Silver Strike', venue: 'casino', icon: '💍', m: 11, from: 3, days: 10, ev: [['0-6', 'sat1700'], [1, 'ring400'], [3, 'ring600'], [5, 'ring400'], [7, 'ring1700']] },
  { id: 'rt5d', n: 'Royal Tour Five Diamond', venue: 'tv', icon: '👑', m: 12, from: 5, days: 7, ev: [[1, 'rpt3500'], [4, 'rpt10k'], ['all', 'c1025']] },
  { id: 'hall', n: 'Hall of Legends Invitational', venue: 'hall', icon: '🏛️', m: 12, from: 18, days: 1, ev: [[0, 'legends']] },
];
// which series is running on a day, and which day of it
function drSeriesOn(d) {
  const D = drDate(d), out = [];
  for (const sr of DR_SERIES) for (const Y of [D.Y - 1, D.Y]) { const s0 = drDayOf(Y, sr.m, sr.from), o = d - s0; if (o >= 0 && o < sr.days) out.push({ sr, o, s0 }); }
  return out;
}
// the WCS Summer Series: bracelets twice a week each, the $10K Championship, the Main in July
function drWcsDay(d, o, s0) {
  const D = drDate(d), out = [['c510']];
  if (o >= 2 && o <= 19) out.push(['sat10k']);
  // numbered bracelet events
  let n = 0; for (let k = 0; k <= o; k++) { const w = drDate(s0 + k).dow; if (w === 1 || w === 4 || w === 3 || w === 6) n++; }
  if (D.dow === 1 || D.dow === 4) out.push(['b1500', `WCS Event #${n}: $1,500 No-Limit`]);
  if (D.dow === 3 || D.dow === 6) out.push(['b3000', `WCS Event #${n}: $3,000 Six-Max`]);
  if (D.m === 6 && D.dd === 18) out.push(['b10k']);
  if ((D.m === 6 && D.dd >= 20) || (D.m === 7 && D.dd <= 4)) out.push(['satmain']);
  if (D.m === 7 && D.dd === 5) out.push(['main']);
  return out;
}
// real-world live events (holidays, specials): they appear on whatever in-game day you are on
const DR_LIVE = [
  { id: 'haunt', from: '10-20', to: '11-02', icon: '🎃', n: 'Haunted Riverside', item: { slot: 'patch', id: 'haunt', name: '“Haunted Riverside” patch' },
    ev: { id: 'live_haunt', ch: 1, kind: 'mtt', name: 'Haunted Riverside Costume Freeroll', venue: 'cardroom', buyin: 0, free: true, field: 160, prizeBase: 40, live: 'haunt', blurb: 'Costumes required, buy-in waived. Play it once and the Haunted Riverside patch is yours. Real-world event: October 20 to November 2.' } },
];
const DR_LIVE_ITEMS = DR_LIVE.map((x) => ({ ...x.item, live: x.id, req: `${x.n} live event (${x.from.replace('-', '/')} to ${x.to.replace('-', '/')})` }));
DR_PATCH.haunt = { t: 'HAUNTED', c: '#ff7a1a', bg: '#140b16' };
DR_META['patch:haunt'] = ['rare', 'league', 'Live event exclusive: Haunted Riverside. Only during the real Halloween run.'];
function drLiveNow(dt = new Date()) { const k = `${String(dt.getMonth() + 1).padStart(2, '0')}-${String(dt.getDate()).padStart(2, '0')}`; return DR_LIVE.filter((x) => (x.from <= x.to ? k >= x.from && k <= x.to : k >= x.from || k <= x.to)); }
const drLivePlayed = (s, id) => !!((s.live || {})[id] || {})[drToday()];

// ---------------------------------------------------------------- what runs on a day
// returns [{ ev, why }] in display order. ev may be a copy of a DR_EVENTS entry with a day-specific name.
function drDayEvents(s, d) {
  const out = [], push = (id, name, why) => { const t = drEvent(id); if (t && !out.some((x) => x.ev.id === id)) out.push({ ev: name ? { ...t, name } : t, why }); };
  if (s.ch === 0) { if (d === 5) push('local', null, 'Your free seat'); return out; }
  const c = drCal(s), D = drDate(d);
  if (c.inv && c.inv.d === d) out.push({ ev: c.inv.ev, why: 'invite' });
  if (d === c.d && drLow(s)) push('freeroll', null, 'freeroll');
  for (const { sr, o, s0 } of drSeriesOn(d)) {
    if (sr.wcs) { for (const [id, name] of drWcsDay(d, o, s0)) push(id, name, sr.n); continue; }
    for (const [when, id] of sr.ev) { const ok = when === 'all' || (typeof when === 'number' ? when === o : (() => { const [a, b] = when.split('-').map(Number); return o >= a && o <= b; })()); if (ok) push(id, null, sr.n); }
  }
  if (D.dow === 6 && D.dd + 7 > D.last) push('classic', null, 'Last Saturday of the month');
  for (const id of DR_WEEKDAY[D.dow] || []) push(id, null, DR_DOW_L[D.dow]);
  for (const id of DR_EVERY_DAY) push(id, null, 'Every day');
  return out.filter((x) => !x.ev.req || s.ch >= x.ev.req);
}
const drTodayEvents = (s) => drDayEvents(s, drCal(s).d);
function drOnToday(s, id) { return drTodayEvents(s).find((x) => x.ev.id === id); }

// ---------------------------------------------------------------- state
function drCal(s) {
  if (!s.cal) {
    // older careers: drop them into the calendar at a point that fits their chapter
    const start = [0, 7, 49, 140, 210, 250, 166 + 365, 330][Math.min(7, s.ch || 0)];
    s.cal = { d: s.made ? start : 0, used: {}, wk: {}, trav: {}, re: {}, inv: null, meet: null, seed: Math.random().toString(36).slice(2), paid: '' };
    if (s.made && s.ch >= 1 && !s.job) s.job = 'quit';
  }
  return s.cal;
}
function drSlots(s) {
  const c = drCal(s);
  if (s.ch === 0) return { left: 1, cap: 1, used: 0, scope: 'day' };
  if (s.job === 'nights') { const u = c.wk[drWeekOf(c.d)] || 0; return { left: Math.max(0, DR_GAMES_CAP - u), cap: DR_GAMES_CAP, used: u, scope: 'week' }; }
  const u = c.used[c.d] || 0; return { left: Math.max(0, DR_GAMES_CAP - u), cap: DR_GAMES_CAP, used: u, scope: 'day' };
}
function drUseSlot(s) { const c = drCal(s); c.used[c.d] = (c.used[c.d] || 0) + 1; const w = drWeekOf(c.d); c.wk[w] = (c.wk[w] || 0) + 1; drCalPrune(c); }
function drCalPrune(c) { for (const k of Object.keys(c.used)) if (+k < c.d - 14) delete c.used[k]; for (const k of Object.keys(c.wk)) if (+k < drWeekOf(c.d) - 2) delete c.wk[k]; for (const k of Object.keys(c.trav)) if (+k < c.d - 3) delete c.trav[k]; for (const k of Object.keys(c.re)) if (+k.split('|')[0] < c.d - 3) delete c.re[k]; }
function drTravelFor(s, ev) { const c = drCal(s), cost = DR_TRAVEL[ev.venue] || 0; if (!cost || ev.online) return 0; return (c.trav[c.d] || []).includes(ev.venue) ? 0 : cost; }

// ---------------------------------------------------------------- a new day
const drRngFor = (s, d, salt) => drSeeded(`${drCal(s).seed}|${d}|${salt}`);
function drSleep(s, days = 1) {
  const c = drCal(s), news = [];
  for (let i = 0; i < days; i++) { c.d++; drNewDay(s, news); }
  DR.morning = news.length ? news : [['☀️', 'A quiet night. Nothing new this morning.']];
  drCalPrune(c); drProgress(s); drSave();
}
function drNewDay(s, news) {
  const c = drCal(s), D = drDate(c.d);
  if (c.inv && c.inv.d < c.d) { news.push(['📭', `The ${c.inv.ev.name} invite expired.`]); c.inv = null; }
  if (c.meet && c.meet.d < c.d) c.meet = null;
  if (s.job === 'nights' && D.dow === 5) { drAdd(s, DR_SALARY, 'Paycheck'); news.push(['💵', `Payday. +$${drMoney(DR_SALARY)} from the day job.`]); }
  const mk = `${D.Y}-${D.m}`;
  if (D.dd === 1 && c.paid !== mk) { c.paid = mk; drPayBills(s, news); }
  const seriesStart = drSeriesOn(c.d).find((x) => x.o === 0); if (seriesStart) news.push([seriesStart.sr.icon, `${seriesStart.sr.n} starts today.`]);
  if (s.ch >= 1) drMaybeInvite(s, news);
  if (s.ch >= 1) drMaybeMeet(s, news);
}
function drPayBills(s, news) {
  const due = drBills(s);
  if (s.br >= due) { drAdd(s, -due, 'Rent and bills'); news.push(['🏠', `Rent and bills paid: −$${drMoney(due)}.`]); return; }
  // can't cover it: pay what you can, move back down, sell the car
  const paid = s.br; drAdd(s, -paid, 'Rent and bills (short)');
  const moved = s.eq.home !== 'studio' || s.eq.ride !== 'bus'; s.eq.home = 'studio'; s.eq.ride = 'bus';
  news.push(['📦', `Couldn't cover $${drMoney(due)} in bills.${moved ? ' You moved back to the studio and sold the car.' : ''}`]);
  drMoment(s, '📦', moved ? 'Short on rent. Moved back to the studio over the laundromat.' : 'Short on rent this month.');
}

// ---------------------------------------------------------------- private game invites: rare, and a real decision
const DR_STAKES = [[5, 10], [10, 25], [25, 50], [50, 100], [100, 200], [200, 400], [500, 1000], [1000, 2000]];
const DR_HOSTS = [
  { id: 'nose', n: 'Tommy "The Nose" Maio', at: 'the back room at Riverside', line: 'Friends only, cash only, no questions. Half the table has been drinking since noon.', soft: 0.18, tier: [0, 2] },
  { id: 'athlete', n: 'a retired NBA star', at: 'his place in the hills', line: 'He plays poker the way he drove the lane: straight at you, every time.', soft: 0.3, tier: [1, 4] },
  { id: 'tech', n: 'a tech founder', at: 'his rooftop', line: 'Just sold his company. Plays every hand and calls it "learning".', soft: 0.35, tier: [1, 5] },
  { id: 'producer', n: 'a movie producer', at: 'a celebrity home game', line: 'Two actors, one rapper, one pro. Guess which seat is yours.', soft: 0.28, tier: [2, 6] },
  { id: 'hedge', n: 'a hedge fund manager', at: 'a private club', line: 'Thinks poker is "just math." About to find out what that means.', soft: 0.22, tier: [3, 7] },
  { id: 'mdad', n: "Mustich's dad", at: 'the yacht club', line: 'Old money, older ego, and he wants to see what beat his son.', soft: 0.15, tier: [3, 7] },
  { id: 'pros', n: 'three Titan regulars', at: 'a penthouse suite', line: '"Pros only. Bring your A-game." No fish, just pride and a lot of money.', soft: -0.15, tier: [4, 7] },
  { id: 'whale', n: 'a visiting billionaire', at: 'his yacht', line: 'Flew in for one night. Loses millions for fun and remembers who was kind about it.', soft: 0.42, tier: [5, 7] },
];
function drMaybeInvite(s, news) {
  const c = drCal(s); if (c.inv) return;
  const r = drRngFor(s, c.d, 'inv'); if (r() > 0.045) return; // about once a month
  // the stakes are sized to your roll, and sometimes they are too big
  const roll = Math.max(500, s.br), u = r(), ratio = u < 0.45 ? 0.05 + r() * 0.15 : u < 0.8 ? 0.2 + r() * 0.4 : 0.6 + r() * 0.9;
  let k = 0; for (let i = 0; i < DR_STAKES.length; i++) if (DR_STAKES[i][1] * 100 <= roll * ratio) k = i;
  const st = DR_STAKES[k], buyin = st[1] * 100, hosts = DR_HOSTS.filter((h) => k >= h.tier[0] && k <= h.tier[1]), h = hosts[(r() * hosts.length) | 0] || DR_HOSTS[0];
  const when = r() < 0.55 ? c.d : c.d + 1, name = `$${drMoney(st[0])}/$${drMoney(st[1])} Private Game`;
  const ev = { id: 'inv', ch: Math.min(7, Math.max(1, k)), kind: 'cash', name, venue: k >= 4 ? 'titan' : 'cardroom', buyin, invite: true, soft: h.soft, host: h.n,
    blurb: `Hosted by ${h.n} at ${h.at}. ${h.line}` };
  c.inv = { ev, d: when, made: c.d };
  news.push(['📨', `A private game invite: ${name}, ${when === c.d ? 'tonight' : 'tomorrow night'}.`]);
}
// ---------------------------------------------------------------- Slick shows up when you're in trouble
function drMaybeMeet(s, news) {
  const c = drCal(s); if (c.meet || !drLow(s)) return;
  const r = drRngFor(s, c.d, 'meet'); if (r() > (drBroke(s) ? 0.35 : 0.12)) return;
  const k = r(), seatEv = s.ch >= 3 ? 'tilt215' : 'deep';
  c.meet = k < 0.45 ? { kind: 'stake', amt: drStakeSize(s), d: c.d } : k < 0.8 ? { kind: 'seat', id: seatEv, d: c.d } : { kind: 'cash', amt: Math.round(drStakeSize(s) / 5 / 10) * 10, d: c.d };
  news.push(['🕶️', 'Slick texted: "Diner. 2 AM. Bring an appetite."']);
}
function drMeetHTML(s) {
  const m = drCal(s).meet; if (!m) return '';
  const off = m.kind === 'stake' ? [`He slides an envelope across the counter: $${drMoney(m.amt)}.`, 'Half of your profits go back to him until it is paid off.', `Take the stake (+$${drMoney(m.amt)})`]
    : m.kind === 'seat' ? [`"${drEvent(m.id).name}. Your seat's paid. Don't make me watch you punt it."`, 'A free seat. No strings, no makeup.', 'Take the seat']
    : [`"Eat something. Then study bubble spots for an hour." He leaves $${drMoney(m.amt)} under the plate.`, 'A gift. Rent money, not a buy-in.', `Take it (+$${drMoney(m.amt)})`];
  return `<article class="drcard drmeet"><div class="drsay">${drSpeaker({ sp: 'slick' }).av}<div><div class="drkicker">Lucky Seven Diner · 2:13 AM</div><b>Slick</b><p>${esc(off[0])}</p><small>${esc(off[1])}</small></div></div>
    <div class="drrow">${drBtn('gold sm', 'data-dr="meetok"', esc(off[2]))}${drBtn('ghost sm', 'data-dr="meetno"', 'Not tonight')}</div></article>`;
}
function drMeetTake(s) {
  const c = drCal(s), m = c.meet; if (!m) return;
  if (m.kind === 'stake') { drAdd(s, m.amt, "Slick's stake"); s.debt += m.amt; s.flags.stakes = (s.flags.stakes || 0) + 1; drMoment(s, '🥧', `Slick staked you $${drMoney(m.amt)}`); }
  else if (m.kind === 'seat') { s.tickets = s.tickets || {}; s.tickets[m.id] = (s.tickets[m.id] || 0) + 1; drMoment(s, '🎟', `Slick paid your seat into the ${drEvent(m.id).name}`); }
  else drAdd(s, m.amt, 'From Slick, for groceries');
  c.meet = null;
}

// ---------------------------------------------------------------- re-entry: big events only, while registration is open
const drReentryOk = (ev) => !!ev.reentry;
function drCanReenter(s, r) {
  const ev = r.ev, c = drCal(s); if (!drReentryOk(ev) || !r.bust || r.ticketUsed) return false;
  if ((r.stageI || 0) > 1) return false; // late registration closes after the middle levels
  return (c.re[`${c.d}|${ev.id}`] || 0) < 2 && s.br >= ev.buyin;
}

// ---------------------------------------------------------------- the calendar screen
function drSeriesAhead(s, n = 5) {
  const c = drCal(s), seen = new Set(), out = [];
  for (let d = c.d; d < c.d + 400 && out.length < n; d++) for (const x of drSeriesOn(d)) { const key = x.sr.id + x.s0; if (seen.has(key)) continue; seen.add(key); out.push({ ...x, start: x.s0, in: x.s0 - c.d }); }
  return out;
}
function drDayIcons(s, d) { const icons = new Set(drSeriesOn(d).map((x) => x.sr.icon)); const D = drDate(d); if (D.dow === 6 && D.dd + 7 > D.last) icons.add('🏆'); const c = drCal(s); if (c.inv && c.inv.d === d) icons.add('📨'); return [...icons].join(''); }
function drCalendarHTML() {
  const s = DR.st, c = drCal(s), sl = drSlots(s), D = drDate(c.d), wk = (DR.calWk || 0), sel = DR.calSel != null && DR.calSel >= c.d ? DR.calSel : c.d;
  const sun = c.d - ((c.d + 1) % 7) + wk * 7, days = Array.from({ length: 7 }, (_, i) => sun + i);
  const job = s.job === 'nights', bills = drBills(s), toFirst = drDate(c.d).last - D.dd + 1, runway = bills ? s.br / bills : 99;
  const status = s.ch === 0 ? 'Prologue' : job ? `Day job · ${sl.left} of ${sl.cap} sessions left this week · payday Friday` : `Full-time · ${sl.left} of ${sl.cap} games left today`;
  const head = `<section class="drcal-head"><div><div class="drkicker">Year ${D.y} · ${esc(DR_MON_L[D.m - 1])}</div><h3 class="drh">${esc(drDateStr(c.d, true))}</h3><small>${esc(status)}</small></div>
    ${s.ch >= 1 ? drBtn(sl.left ? 'ghost sm' : 'gold sm', 'data-dr="sleep"', job && !sl.left ? 'Sleep until Sunday ☾' : 'Go to sleep ☾') : ''}</section>`;
  const strip = `<div class="drweek"><button class="drwk-nav" data-dr="calwk" data-v="-1" ${wk <= 0 ? 'disabled' : ''} aria-label="Previous week">‹</button>${days.map((d) => { const X = drDate(d), past = d < c.d;
    return `<button class="drday ${d === c.d ? 'today' : ''} ${d === sel ? 'sel' : ''} ${past ? 'past' : ''}" data-dr="calday" data-d="${d}" ${past ? 'disabled' : ''}><small>${DR_DOW[X.dow]}</small><b>${X.dd}</b><i>${drDayIcons(s, d)}</i></button>`; }).join('')}<button class="drwk-nav" data-dr="calwk" data-v="1" aria-label="Next week">›</button></div>`;
  const morning = DR.morning ? `<article class="drcard drmorning"><div class="drkicker">This morning</div>${DR.morning.map(([i, t]) => `<div><span>${i}</span>${esc(t)}</div>`).join('')}</article>` : '';
  const money = s.ch >= 1 ? `<div class="drbills"><span>🏠 Rent and bills <b class="mono">$${drMoney(bills)}</b> due in ${toFirst} day${toFirst === 1 ? '' : 's'}</span><span class="${runway < 2 ? 'warn' : ''}">Your roll covers <b>${runway >= 24 ? 'years' : runway.toFixed(1) + ' months'}</b></span>${s.debt > 0 ? `<span class="warn">Owe Slick <b class="mono">$${drMoney(s.debt)}</b></span>` : ''}</div>` : '';
  const list = drDayEvents(s, sel), future = sel > c.d;
  const cards = list.map((x) => drEventCard(x.ev, { why: x.why, future, d: sel })).join('') || '<p class="drhint">Nothing scheduled.</p>';
  const daily = s.ch >= 1 && !future ? drDailyCard(s) : '', live = s.ch >= 1 && !future ? drLiveNow().map((L) => drEventCard(L.ev, { why: 'live', live: L })).join('') : '';
  const ahead = drSeriesAhead(s).map((x) => `<div class="drser"><span>${x.sr.icon}</span><div><b>${esc(x.sr.n)}</b><small>${esc(drDateStr(x.start))} – ${esc(drDateStr(x.start + x.sr.days - 1))}${x.in > 0 ? ` · in ${x.in} day${x.in === 1 ? '' : 's'}` : ' · on now'}</small></div>${x.in > 0 ? drBtn('ghost sm', `data-dr="sleepto" data-d="${x.start}"`, 'Skip to it') : ''}</div>`).join('');
  const jobRow = s.ch < 1 ? '' : job ? `<div class="drjob"><small>Working days for Corporate Lou, playing nights and weekends. Quit any time to play ${DR_GAMES_CAP} games a day, with no paycheck.</small>${drBtn('ghost sm', 'data-dr="quitjob"', 'Quit the job')}</div>`
    : drLow(s) ? `<div class="drjob warn"><small>Running low. Corporate Lou might take you back: $${drMoney(DR_SALARY)} every Friday, ${DR_GAMES_CAP} sessions a week.</small>${drBtn('ghost sm', 'data-dr="rehire"', 'Ask for the job back')}</div>` : '';
  return `${head}${strip}${morning}${drMeetHTML(s)}${money}
    <div class="drkicker drday-h">${future ? `${esc(drDateStr(sel, true))} · in ${sel - c.d} day${sel - c.d === 1 ? '' : 's'}` : 'Tonight'}</div>
    ${future ? `<div class="drrow">${drBtn('gold sm', `data-dr="sleepto" data-d="${sel}"`, `Sleep until ${esc(DR_DOW_L[drDate(sel).dow])} ☾`)}<small class="drhint">Paychecks, rent and invites happen along the way.</small></div>` : ''}
    ${live}${daily}<div class="drevs">${cards}</div>
    ${s.ch >= 1 ? `<div class="drkicker drday-h">Majors ahead</div><div class="drsers">${ahead}</div>` : ''}${jobRow}`;
}
