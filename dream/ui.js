// ================================================================ THE DREAM: screens
// title → creator → story scenes → life hub (schedule, closet, trophies, legends, story) → event → results

const drBtn = (cls, attrs, label) => `<button class="drbtn ${cls}" ${attrs}>${label}</button>`;
const drPortraitImg = () => { const i = document.querySelector('.brandmark img'); return i ? i.src : ''; };

function drRender() {
  if (!DR || S.mode !== 'trainer') return;
  const box = $('dream'); if (!box) return;
  const s = DR.st;
  if (DR.view === 'hub' && s.queue.length && !DR.run) { drPlayScene(s.queue.shift()); drSave(); return; }
  // picked up mid-prologue: start the story where it was left
  if (DR.view === 'hub' && s.made && s.ch === 0 && !s.story.p_diner && !DR.run) { drPlayScene(s.story.p_office ? 'p_diner' : 'p_office'); return; }
  box.className = 'dream v-' + DR.view;
  box.innerHTML = DR.view === 'title' ? drTitleHTML() : DR.view === 'create' ? drCreateHTML() : DR.view === 'scene' ? drSceneHTML()
    : DR.view === 'profile' ? drProfileHTML() : DR.view === 'run' ? drRunHTML() : DR.view === 'results' ? (DR.last && DR.last.sng ? drSngResultsHTML() : drResultsHTML()) : DR.mode && DR.mode !== 'story' ? drModeHubHTML() : drHubHTML();
  if (DR.view === 'scene') drType();
  if (DR.view === 'results') drCountUp();
  const nm = $('drName'); if (nm) nm.oninput = () => { DR.draft.name = nm.value.slice(0, 18); const p = $('drPrevName'); if (p) p.textContent = nm.value || 'You'; };
}

// ---------------------------------------------------------------- title + creator
function drTitleHTML() {
  const s = DR.st;
  return `<section class="drtitle pl-diner">
    <div class="drtitle-in">
      <div class="drkicker">Confirmed Fire Poker presents</div>
      <h1 class="drlogo">The <em>Dream</em></h1>
      <p class="drtag">From a cubicle on floor 34 to the Hall of Legends.<br>Every decision is graded by the solver. Every title is earned.</p>
      <div class="drtitle-btns">
        ${s.made ? drBtn('gold big', 'data-dr="continue"', `Continue · ${esc(s.name)}`) : ''}
        ${drBtn(s.made ? 'ghost' : 'gold big', 'data-dr="new"', s.made ? 'Start a new career' : 'Start your story')}
      </div>
      ${s.made ? `<div class="drconfirm" id="drNewConfirm" hidden><span>Start over? Your current career is erased.</span>${drBtn('danger', 'data-dr="newyes"', 'Erase and start over')}${drBtn('ghost', 'data-dr="newno"', 'Keep it')}</div>` : ''}
      <div class="drquote">“People insist on calling it luck…”</div>
    </div>
  </section>`;
}
function drCreateHTML() {
  const d = DR.draft, look = d.look, prev = { name: d.name || 'You', look, eq: drDefault().eq, titles: {}, br: 0 };
  const sw = (field, list, cur, kind) => list.map((c, i) => `<button class="drsw ${kind}" style="--c:${c}" data-dr="look" data-f="${field}" data-v="${i}" aria-pressed="${cur === i}" aria-label="${field} ${i + 1}"></button>`).join('');
  const opts = (field, list, cur) => list.map(([v, n]) => `<button class="drchip" data-dr="look" data-f="${field}" data-v="${v}" aria-pressed="${cur === v}">${n}</button>`).join('');
  return `<section class="drcreate">
    <div class="drcreate-prev pl-office"><div class="drcard-av">${drAvatarSVG(prev)}</div><b id="drPrevName">${esc(d.name || 'You')}</b><small>Associate Analyst · Floor 34</small></div>
    <div class="drcreate-form">
      <h2 class="drh">Who are you?</h2>
      <label class="drfield"><span>Your name</span><input id="drName" maxlength="18" value="${esc(d.name)}" placeholder="Your name" autocomplete="off"></label>
      <div class="drfield"><span>Skin tone</span><div class="drsws">${sw('skin', DR_SKIN, look.skin, 'skin')}</div></div>
      <div class="drfield"><span>Hair</span><div class="drchips">${opts('hair', DR_HAIR, look.hair)}</div></div>
      <div class="drfield"><span>Hair color</span><div class="drsws">${sw('hairC', DR_HAIRC, look.hairC, 'hair')}</div></div>
      <div class="drfield"><span>Eye color</span><div class="drsws">${sw('eyeC', DR_EYEC, look.eyeC || 0, 'eye')}</div></div>
      <div class="drfield"><span>Facial hair</span><div class="drchips">${opts('facial', DR_FACIAL, look.facial)}</div></div>
      <div class="drrow">${drBtn('ghost', 'data-dr="title"', 'Back')}${drBtn('gold big', 'data-dr="begin"', 'Clock in →')}</div>
      <p class="drhint">Clothes, jewelry, your home and your ride come later. You have to earn those.</p>
    </div>
  </section>`;
}

// ---------------------------------------------------------------- story scenes
function drPlayScene(id) {
  const sc = DR_SCENES[id]; if (!sc) { DR.view = 'hub'; drRender(); return; }
  DR.sc = { id, i: 0, extra: [] }; DR.view = 'scene'; drRender(); scrollTo(0, 0);
}
function drBeat() {
  const sc = DR.sc; if (!sc) return null;
  return sc.extra.length ? sc.extra[0] : DR_SCENES[sc.id].beats[sc.i] || null;
}
function drSpeaker(b) {
  const s = DR.st;
  if (b.sp === 'n') return null;
  if (b.sp === 'you') return { name: s.name || 'You', role: drRep(s), av: `<div class="drface you">${drAvatarSVG(s, { face: 1 })}</div>` };
  if (b.sp.startsWith('L:')) {
    const L = drLegend(b.sp.slice(2));
    return { name: L.name, role: `“${L.nick}” · ${L.era}`, av: `<div class="drface legend">${drFaceOf(drLegendLook(L))}</div>` };
  }
  const p = DR_PEOPLE[b.sp];
  if (b.sp === 'slick') { const img = drPortraitImg(); return { name: b.who || p.name, role: b.who ? '' : p.role, av: `<div class="drface slick">${img ? `<img src="${img}" alt="">` : '🕶️'}</div>` }; }
  return { name: p.name, role: p.role, av: `<div class="drface npc" style="--c:${p.c}">${drFaceOf(p, p.mouth)}</div>` };
}
function drFaceOf(p, mouth) { return drAvatarSVG({ name: p.name || '', look: p.look, eq: { ...drDefault().eq, ...p.eq }, titles: {}, br: 0 }, { face: 1, mouth }); }
function drLegendFace(L) { return `<div class="drlegend-mono face">${drFaceOf(drLegendLook(L))}</div>`; }
function drSceneHTML() {
  const sc = DR.sc, def = DR_SCENES[sc.id], b = drBeat(); if (!b) return '';
  const place = b.place || def.place, pl = DR_PLACES[place] || {};
  const who = drSpeaker(b), text = typeof b.t === 'function' ? b.t(DR.st) : b.t;
  DR.tw = { full: text, n: 0 };
  const total = def.beats.length, at = Math.min(total, sc.i + 1);
  const choice = !sc.extra.length && b.choice;
  const card = sc.i === 0 && !sc.extra.length && !sc.replay && /^(Prologue|Chapter|Legacy)/.test(def.title || '') && !sc.carded;
  if (card) sc.carded = true;
  return `<section class="drscene pl-${place}">${card ? `<div class="drchapter" aria-hidden="true"><small>${esc(def.title)}</small><b>${esc(def.sub || '')}</b></div>` : ''}
    <div class="drscene-top"><span class="drkicker">${esc(def.title || '')}${def.sub ? ` · ${esc(def.sub)}` : ''}</span><span class="drplace">${esc(pl.name || '')}${pl.sub ? ` <small>${esc(pl.sub)}</small>` : ''}</span>${drBtn('ghost sm', 'data-dr="skip"', 'Skip ›')}</div>
    <div class="drscene-stage"><div class="drprop" aria-hidden="true">${drPropSVG(place)}</div>${who && !b.text ? `<div class="drscene-who">${who.av}</div>` : ''}${b.text ? `<div class="drphone"><div class="drphone-h">${esc(who ? who.name : '')}</div><div class="drphone-b" id="drText"></div></div>` : ''}</div>
    ${b.text ? '' : `<div class="drbox ${b.sp === 'n' ? 'narr' : ''} ${b.big ? 'big' : ''} ${b.goal ? 'goal' : ''}" data-dr="adv">
      ${who ? `<div class="drbox-name">${esc(who.name)}${who.role ? `<small>${esc(who.role)}</small>` : ''}</div>` : ''}
      <div class="drbox-text" id="drText"></div>
      ${choice ? `<div class="drchoices">${b.choice.map((c, k) => drBtn('choice', `data-dr="choose" data-k="${k}"`, esc(c.t))).join('')}</div>` : `<div class="drbox-more">${at < total || sc.extra.length ? 'Tap to continue ▸' : 'Tap to continue ▸'}</div>`}
    </div>`}
    ${b.text ? `<div class="drbox narr slim" data-dr="adv"><div class="drbox-more">Tap to continue ▸</div></div>` : ''}
    <div class="drdots">${def.beats.map((_, k) => `<i class="${k < at ? 'on' : ''}"></i>`).join('')}</div>
  </section>`;
}
function drType() {
  const el = $('drText'); if (!el || !DR.tw) return;
  clearInterval(DR.twTimer);
  const full = DR.tw.full, reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (reduce) { el.textContent = full; DR.tw.n = full.length; return; }
  DR.tw.n = 0; el.textContent = '';
  DR.twTimer = setInterval(() => {
    if (!DR.tw || !$('drText')) { clearInterval(DR.twTimer); return; }
    DR.tw.n = Math.min(full.length, DR.tw.n + 2); $('drText').textContent = full.slice(0, DR.tw.n);
    if (DR.tw.n >= full.length) clearInterval(DR.twTimer);
  }, 22);
}
function drAdvance() {
  const sc = DR.sc; if (!sc) return;
  if (DR.tw && DR.tw.n < DR.tw.full.length) { clearInterval(DR.twTimer); DR.tw.n = DR.tw.full.length; const el = $('drText'); if (el) el.textContent = DR.tw.full; return; }
  const b = drBeat(); if (!sc.extra.length && b && b.choice) return; // pick an option first
  if (sc.extra.length) sc.extra.shift(); else sc.i++;
  if (!sc.extra.length && sc.i >= DR_SCENES[sc.id].beats.length) { drSceneEnd(); return; }
  drRender();
}
function drChoose(k) {
  const sc = DR.sc, b = drBeat(); if (!b || !b.choice) return;
  const c = b.choice[k]; if (!c) return;
  if (c.act && !sc.replay) c.act(DR.st);
  drSave();
  if (c.go) { if (sc.replay) { DR.sc = { id: c.go, i: 0, extra: [], replay: true }; drRender(); return; } DR.st.story[sc.id] = true; drSave(); drPlayScene(c.go); return; }
  sc.i++;
  if (c.line) sc.extra.push(c.line);
  if (!sc.extra.length && sc.i >= DR_SCENES[sc.id].beats.length) { drSceneEnd(); return; }
  drRender();
}
function drSceneEnd() {
  const s = DR.st, id = DR.sc.id, def = DR_SCENES[id]; clearInterval(DR.twTimer);
  if (DR.sc.replay) { DR.sc = null; DR.view = 'hub'; DR.tab = 'story'; drRender(); scrollTo(0, 0); return; }
  s.story[id] = true;
  const m = /^c(\d)$/.exec(id); if (m) { if (+m[1] > s.ch) drMoment(s, '📖', `${DR_CHAPTERS[+m[1]].n}: ${DR_CHAPTERS[+m[1]].t}`); s.ch = Math.max(s.ch, +m[1]); }
  if (id === 'p_quit') drMoment(s, '🚪', 'Quit the 9-to-5'); if (id === 'p_keep') drMoment(s, '🌙', 'Kept the day job. Nights are for the dream.');
  drUnlocks(s); drProgress(s); drSave();
  DR.sc = null;
  const end = def.end || 'hub';
  if (DR_SCENES[end]) { drPlayScene(end); return; }
  if (drEvent(end)) { DR.view = 'hub'; drStart(end); return; }
  DR.view = 'hub'; drRender(); scrollTo(0, 0);
}
function drSkip() {
  // skip to the end of this scene, but never past a choice you haven't made
  const sc = DR.sc; if (!sc) return;
  const beats = DR_SCENES[sc.id].beats; sc.extra = [];
  const k = beats.findIndex((b, i) => i >= sc.i && b.choice);
  if (k >= 0) { sc.i = k; drRender(); return; }
  drSceneEnd();
}

// ---------------------------------------------------------------- the life hub
function drTitleStrip(s) {
  const k = ['bracelet', 'ring', 'tour', 'glo', 'titan', 'me', 'room', 'local'];
  return `<div class="drtitles">${k.map((t) => `<div class="drtl ${s.titles[t] ? 'on' : ''}" title="${DR_TITLES[t].plural}"><b>${DR_TITLES[t].icon}</b><span class="mono">${s.titles[t]}</span><small>${DR_TITLES[t].plural}</small></div>`).join('')}
    <div class="drtl ${Object.keys(s.legends).length ? 'on' : ''}"><b>🂡</b><span class="mono">${Object.keys(s.legends).length}</span><small>Legends beaten</small></div></div>`;
}
// one tap to your profile: level, XP and today's quests
function drTodayBar(s) {
  const p = drPg(s), L = drLevelOf(p.xp), done = p.q.list.filter((q) => q.done).length;
  return `<button class="drtoday" data-dr="profile"><span class="drlvl"><b>${L.lvl}</b><span><i style="width:${((L.into / L.need) * 100).toFixed(1)}%"></i></span></span>
    <span class="drtoday-t"><b>Career level ${L.lvl}</b><small>${done}/3 daily quests${p.news.length ? ` · ${p.news.length} new unlock${p.news.length > 1 ? 's' : ''}` : ''}</small></span><span class="drtoday-q">${p.q.list.map((q) => `<i class="${q.done ? 'on' : ''}"></i>`).join('')}</span><span aria-hidden="true">›</span></button>`;
}
function drHubHTML() {
  const s = DR.st, chd = DR_CHAPTERS[Math.min(7, s.ch)], pr = chd.prog ? chd.prog(s) : [s.story.p_after ? 1 : 0, ''];
  const home = DR_ITEMS.find((x) => x.slot === 'home' && x.id === s.eq.home), ride = DR_ITEMS.find((x) => x.slot === 'ride' && x.id === s.eq.ride);
  const tab = DR.tab || 'events';
  const fresh = (s.newItems || []).length;
  return `${drModeBar()}<section class="drlife hm-${s.eq.home}">
      <div class="drlife-av">${drAvatarSVG(s)}</div>
      <div class="drlife-info">
        <div class="drkicker">${esc(chd.n)} · ${esc(chd.t)}</div>
        <div class="drlife-name">${esc(s.name)}</div>
        <div class="drlife-rep">${esc(drRep(s))}${drTitleBadge(s)}</div>
        <div class="drbank"><small>Bankroll</small><b class="mono">$${drMoney(s.br)}</b>${s.debt > 0 ? `<span class="drdebt">Owes Slick $${drMoney(s.debt)}</span>` : ''}</div>
        <div class="drlifestyle"><span>${DR_HOME_ICON[s.eq.home] || '🏠'} ${esc(home ? home.name : '')}</span><span>${DR_RIDE_ICON[s.eq.ride] || '🚗'} ${esc(ride ? ride.name : '')}</span></div>
      </div>
    </section>
    ${!s.flags.howto && s.ch >= 1 ? `<section class="drhowto"><div class="drsay">${drSpeaker({ sp: 'slick' }).av}<div><b>How this works</b>
      <p>Every hand is a real decision from a long solver run, graded like a chess engine grades moves: <b>Best</b>, <b>Inaccuracy</b>, <b>Mistake</b>, <b>Blunder</b>, and the rare <b>Brilliant</b>. Good decisions grow your <b>tournament life</b>; a couple of blunders send you home. Your <b>bankroll</b> decides what you can enter. Titles and a bigger roll unlock clothes, jewelry, a better place and a better ride. Every decision in every mode also earns <b>career XP</b> toward table cosmetics. It gets harder as you climb.</p></div></div>
      ${drBtn('gold sm', 'data-dr="howto"', 'Got it')}</section>` : ''}
    ${drTitleStrip(s)}
    <section class="drgoal">
      <div><div class="drkicker">${esc(chd.n)} goal</div><b>${esc(chd.goal)}</b><div class="drbar"><i style="width:${(pr[0] * 100).toFixed(1)}%"></i></div><small>${esc(pr[1])}</small></div>
      <div class="drgoal-btns">${(() => { const id = s.ch >= 1 ? drPick(s) : null, ev = id && drEvent(id); return ev && drAfford(s, ev) ? drBtn('gold sm', `data-dr="play" data-id="${ev.id}"`, `Play ${esc(ev.name.replace(/^(WCS|Circuit|Royal Tour) /, ''))} ▸`) : ''; })()}${drBtn('ghost sm', `data-dr="replay" data-id="${s.ch === 0 ? 'p_diner' : 'c' + Math.min(7, s.ch)}"`, '▶ Story')}</div>
    </section>
    ${drTodayBar(s)}
    <nav class="drtabs" role="tablist">${[['events', 'Schedule'], ['closet', `Closet${fresh ? ` <i class="drnew">${fresh}</i>` : ''}`], ['trophies', 'Trophies'], ['legends', 'Legends'], ['story', 'Story']].map(([k, n]) => `<button role="tab" data-dr="tab" data-t="${k}" aria-selected="${tab === k}">${n}</button>`).join('')}</nav>
    <div class="drtabbody">${tab === 'closet' ? drClosetHTML() : tab === 'trophies' ? drTrophiesHTML() : tab === 'legends' ? drLegendsHTML() : tab === 'story' ? drStoryHTML() : drEventsHTML()}</div>`;
}
// Slick's pick: the biggest event in this chapter that your roll can take without betting the dream
function drPick(s) {
  const list = drAvailable(s).filter((e) => e.ch === s.ch || s.ch === 0);
  const ok = list.filter((e) => drRisk(s, e)[0] === 'safe' || drRisk(s, e)[0] === 'free' || drRisk(s, e)[0] === 'shot');
  const mtt = ok.filter((e) => e.kind === 'mtt').sort((a, b) => b.buyin - a.buyin);
  const safe = mtt.filter((e) => drRisk(s, e)[0] !== 'shot');
  return (safe[0] || mtt[mtt.length - 1] || ok[0] || {}).id;
}
function drEventCard(ev) {
  const s = DR.st, [rk, rl] = drRisk(s, ev), rec = s.events[ev.id], pl = DR_PLACES[ev.venue], pick = drPick(s) === ev.id;
  const first = ev.kind === 'mtt' ? Math.round(typicalPayouts(ev.field, ev.buyin, 0).amounts[0]) : 0;
  const boss = ev.boss ? (DR_PEOPLE[ev.boss] || {}).name : null, legends = (ev.legends || []).map(drLegend).filter(Boolean);
  return `<article class="drev pl-${ev.venue} ${rk === 'no' ? 'locked' : ''} ${pick ? 'pick' : ''}">${pick ? `<div class="drpick"><span class="drpick-face">${drPortraitImg() ? `<img src="${drPortraitImg()}" alt="">` : '🕶️'}</span>Slick's pick</div>` : ''}
    <div class="drev-top"><span class="drev-kind">${ev.kind === 'cash' ? 'Cash game' : ev.kind === 'sat' ? '🎟 Satellite' : ev.title ? `${DR_TITLES[ev.title].icon} ${DR_TITLES[ev.title].n} event` : 'Tournament'}</span><span class="drrisk r-${rk}">${esc(rl)}</span></div>
    <h3>${esc(ev.name)}</h3>
    <p>${esc(ev.blurb)}</p>
    <div class="drev-facts">
      <span><small>Buy-in</small><b class="mono">${ev.free ? 'FREE' : drTicket(s, ev) ? '🎟 Seat' : '$' + drMoney(ev.buyin)}</b></span>
      ${ev.kind === 'mtt' ? `<span><small>Field</small><b class="mono">${drMoney(ev.field)}</b></span><span><small>1st place</small><b class="mono">$${drMoneyShort(first)}</b></span>` : ev.kind === 'sat' ? `<span><small>Field</small><b class="mono">${drMoney(ev.field)}</b></span><span><small>Seats</small><b class="mono">${ev.seats}</b></span><span><small>Prize</small><b>${esc(drEvent(ev.target).name.replace(/^WCS |^Circuit /, ''))} seat</b></span>` : `<span><small>Venue</small><b>${esc(pl.name)}</b></span>`}
    </div>
    ${boss || legends.length ? `<div class="drev-legends">${boss ? `<span>Final boss: <b>${esc(boss)}</b></span>` : ''}${legends.length ? `<span>Legends in the field: ${legends.map((l) => `<b class="${s.legends[l.id] ? 'beat' : ''}">${esc(l.name)}</b>`).join(', ')}</span>` : ''}</div>` : ''}
    <div class="drev-bot"><small>${rec ? `Played ${rec.played}${ev.kind === 'sat' ? ` · ${rec.wins} seat${rec.wins === 1 ? '' : 's'} won` : ev.kind === 'mtt' ? ` · best ${rec.best ? drOrd(rec.best) : '—'}${rec.wins ? ` · ${rec.wins} win${rec.wins > 1 ? 's' : ''}` : ''}` : ` · net ${rec.net >= 0 ? '+' : '−'}$${drMoney(Math.abs(rec.net))}`}` : 'Never played'}</small>
      ${drBtn(rk === 'no' ? 'ghost' : 'gold', `data-dr="play" data-id="${ev.id}" ${rk === 'no' ? 'disabled' : ''}`, rk === 'no' ? 'Can\'t afford it' : ev.kind === 'cash' ? 'Sit down' : drTicket(s, ev) ? 'Use your seat' : 'Register')}</div>
  </article>`;
}
function drEventsHTML() {
  const s = DR.st, list = drAvailable(s), now = list.filter((e) => e.ch === s.ch || s.ch === 0), older = list.filter((e) => !now.includes(e));
  const ev = drDailyEvent(s), done = drDailyDone(s), today = done ? s.daily[drToday()] : null;
  const y = new Date(); y.setDate(y.getDate() - 1);
  const alive = done || (s.daily && s.daily[drToday(y)]);
  const daily = s.ch === 0 ? '' : `<article class="drdaily pl-${ev.venue}"><div><div class="drkicker">Today · free</div><h3>The Daily Grind</h3>
      <p>${done ? `Played today: ${today.place ? drOrd(today.place) : '—'}${today.prize ? `, +$${drMoney(today.prize)}` : ''}. Back tomorrow.` : 'One free sponsored freeroll a day, about five minutes. New spots every day.'}</p>
      <small>🔥 Streak ${alive ? s.streak || 0 : 0} day${(alive ? s.streak : 0) === 1 ? '' : 's'} · best ${s.bestStreak || 0} · 3 days: Grind patch · 7 days: dealer's visor</small></div>
      ${drBtn(done ? 'ghost' : 'gold', `data-dr="play" data-id="daily" ${done ? 'disabled' : ''}`, done ? 'Done ✓' : 'Play today')}</article>`;
  return `${daily}${s.ch === 0 ? '' : `<p class="drhint">Your bankroll decides what you can enter. Big shots pay big, and busting hurts. ${s.debt > 0 ? 'Slick takes half your profits until his stake is paid back.' : ''}</p>`}
    <div class="drevs">${now.map(drEventCard).join('')}</div>
    ${older.length ? `<details class="drolder"><summary>Earlier stops (${older.length})</summary><div class="drevs">${older.map(drEventCard).join('')}</div></details>` : ''}
    <div class="drsoon"><div class="drkicker">Coming next</div><div class="drsoon-row">
      <div><b>Online play</b><small>Real opponents in Ranked and Leagues, and private leagues for your crew.</small></div>
      <div><b>Story-only gear</b><small>Everything in this closet comes from your career. League exclusives live in Leagues.</small></div>
      <div><b>Story chapters 7+</b><small>Rivalries, sponsorship deals and the Hall of Legends ceremony.</small></div></div></div>`;
}
// rarity reads straight off what it takes to earn an item
function drRarity(it) {
  if (it.league) return 'limited';
  const src = String(it.test);
  if (/titles\.me|meft|island|hyper/.test(src) || /me >= 1/.test(src)) return 'legendary';
  if (/bracelet|titan|tour|1e6|2e6|5e6/.test(src)) return 'epic';
  if (/ring|circuit|peak >= (2|5|1)\d{4,5}|ch >= [23]|streak|bestStreak/.test(src)) return 'rare';
  return 'common';
}
function drClosetHTML() {
  const s = DR.st, fresh = new Set(s.newItems || []);
  const html = DR_SLOTS.map(([slot, label]) => {
    const cur = drSeasonFor(drMonth()).key;
    const items = [...DR_ITEMS.filter((x) => x.slot === slot), ...DR_LG_ITEMS.filter((x) => x.slot === slot && (drOwns(s, slot, x.id) || x.season === cur))];
    return `<div class="drslot"><div class="drkicker">${label}</div><div class="dritems">${items.map((it) => {
      const own = drOwns(s, slot, it.id), on = s.eq[slot] === it.id, k = drItemKey(it);
      const rar = drRarity(it);
      return `<button class="dritem ${own ? '' : 'locked'} ${on ? 'on' : ''} ${it.league ? 'lg' : ''} r-${rar}" data-dr="equip" data-s="${slot}" data-v="${it.id}" ${own ? '' : 'disabled'}>${fresh.has(k) ? '<i class="drnew">New</i>' : ''}<b>${esc(it.name)}</b><small>${own ? (on ? 'Wearing' : 'Tap to use') : '🔒 ' + esc(it.league ? drLgReq(it) : it.req)}</small>${it.league ? '<i class="drlgtag">League</i>' : `<i class="drrar">${rar}</i>`}</button>`;
    }).join('')}</div></div>`;
  }).join('');
  if (fresh.size) { s.newItems = []; drSave(); }
  const all = [...DR_ITEMS.filter((x) => x.id !== 'none'), ...DR_LG_ITEMS], have = all.filter((x) => drOwns(s, x.slot, x.id)).length;
  return `<div class="drcloset"><div class="drcloset-prev hm-${s.eq.home}">${drAvatarSVG(s)}<div class="drcoll"><small>Collection</small><b class="mono">${have}/${all.length}</b><div class="drbar"><i style="width:${((have / all.length) * 100).toFixed(1)}%"></i></div></div></div><div class="drslots">${html}</div></div>`;
}
function drTrophiesHTML() {
  const s = DR.st, acc = s.dec ? Math.round(((s.perfect + s.good) / s.dec) * 100) : 0;
  return `<div class="drstats">
      <div><small>Events played</small><b class="mono">${s.played}</b></div><div><small>Cashes</small><b class="mono">${s.cashes}</b></div>
      <div><small>Final tables</small><b class="mono">${s.ft}</b></div><div><small>Peak bankroll</small><b class="mono">$${drMoneyShort(s.peak)}</b></div>
      <div><small>Decisions</small><b class="mono">${s.dec}</b></div><div><small>Solver-approved</small><b class="mono">${acc}%</b></div>
    </div>
    ${s.ch >= 1 ? (() => { const b = drPoyBoard(s), me = b.findIndex((r) => r.me), top = b.slice(0, 8), show = me >= 8 ? [...top, b[me]] : top;
      return `<div class="drpoy"><div class="drkicker">Player of the Year race · ${new Date().toLocaleDateString(undefined, { month: 'long' })}</div>
        <div class="drpoy-list">${show.map((r) => `<div class="${r.me ? 'me' : ''}"><span class="mono">${b.indexOf(r) + 1}</span><b>${esc(r.n)}</b><small class="mono">${drMoney(r.p)} pts</small></div>`).join('')}</div>
        <small class="drhint">Cash and win tournaments to climb. The race resets every month. Rivals are simulated until online play arrives.</small></div>`; })() : ''}
    <div class="drcase">${s.trophies.length ? s.trophies.map((t) => `<div class="drtrophy t-${t.t}"><b>${DR_TITLES[t.t].icon}</b><span>${esc(DR_TITLES[t.t].n)}</span><small>${esc(t.ev)}</small><small class="mono">$${drMoney(t.prize)}</small></div>`).join('') : '<p class="drhint">Your trophy case is empty. For now.</p>'}</div>`;
}
function drLegendsHTML() {
  const s = DR.st, eras = ['Old school', 'Boom', 'Online', 'High roller', 'Rising star'];
  const card = (L) => {
    const beat = s.legends[L.id], met = s.met[L.id];
    return `<div class="drlegend ${beat ? 'beat' : met ? 'met' : 'unk'}">${beat || met ? drLegendFace(L) : `<div class="drlegend-mono">?</div>`}
      <b>${beat || met ? esc(L.name) : '???'}</b><small>“${esc(L.nick)}”${beat || met ? '' : ` · ${esc(DR_CHAPTERS[Math.min(7, L.ch)].t)}`}</small>${beat || met ? `<p>${esc(L.style)}</p>` : ''}<span class="drlegend-st">${beat ? 'Beaten' : met ? 'Met at a final table' : 'Not met yet'}</span></div>`;
  };
  return `<p class="drhint">Every legend is a tribute. Beat one heads-up for the title and their card turns gold.</p>
    <div class="drlegend ghost ${s.titles.me ? 'beat' : ''}"><div class="drlegend-mono face">${drFaceOf(DR_PEOPLE.ghost)}</div><b>The Ghost</b><small>“The GOAT”</small><p>Fourteen bracelets. Never explains. The man from the fuzzy TV.</p><span class="drlegend-st">${s.titles.me ? 'Beaten for the world title' : 'Final boss of the Main Event'}</span></div>
    ${eras.map((e) => `<div class="drkicker drera">${e}</div><div class="drlegends">${DR_LEGENDS.filter((L) => L.era === e).map(card).join('')}</div>`).join('')}`;
}
function drStoryHTML() {
  const s = DR.st, list = ['p_office', 'p_diner', 'p_after', 'firstwin', 'c1', 'trentbeat', 'c2', 'ring1', 'derek', 'c3', 'bracelet1', 'c4', 'tour1', 'c5', 'titan1', 'c6', 'champ', 'c7', 'broke'].filter((id) => s.story[id]);
  const tl = (s.timeline || []).slice().reverse();
  return `${tl.length ? `<div class="drkicker">Your story so far</div><div class="drtimeline">${tl.slice(0, 40).map((m) => `<div><span>${m.icon}</span><div><b>${esc(m.text)}</b><small>${new Date(m.d).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}</small></div></div>`).join('')}</div>` : ''}
    <div class="drkicker">Replay a scene</div><div class="drstorylist">${list.map((id) => { const d = DR_SCENES[id]; return drBtn('ghost', `data-dr="replay" data-id="${id}"`, `▶ ${esc(d.title || '')} · ${esc(d.sub || '')}`); }).join('')}</div>
    <div class="drkicker">Money</div>
    <div class="drledger">${s.ledger.slice(-12).reverse().map((l) => `<div><span>${esc(l.why)}</span><b class="mono ${l.amt >= 0 ? 'up' : 'down'}">${l.amt >= 0 ? '+' : '−'}$${drMoney(Math.abs(l.amt))}</b></div>`).join('')}</div>
    <div class="drrow">${drBtn('ghost sm', 'data-dr="title"', 'Title screen')}${drBtn('ghost sm', 'data-dr="sound"', AR && AR.st.sound === false ? '🔇 Sound off' : '🔊 Sound on')}</div>`;
}

// ---------------------------------------------------------------- an event in progress
function drHudHTML(run) {
  const ev = run.ev, stage = run.stages[Math.min(run.si, run.stages.length - 1)];
  const N = ev.field, P = ev.kind !== 'cash' ? drPaid(ev) : 0;
  const left = ev.kind === 'cash' ? null : stage === 'early' ? Math.max(P * 3, Math.round(N * (1 - 0.15 * (run.h + 1)))) : stage === 'middle' ? Math.max(P + 4, Math.round(N * (0.45 - 0.08 * run.h)))
    : stage === 'bubble' || stage === 'satbubble' ? P + 1 + (stage === 'satbubble' ? 2 - run.h : 0) - (stage === 'bubble' && run.h > 0 ? run.h : 0) : stage === 'deep' ? Math.max(10, Math.round(P * (0.3 - 0.08 * run.h))) : Math.max(2, 9 - run.h * 3);
  const leftN = left == null ? null : Math.min(N, left);
  const status = run.stack >= 150 ? 'Dominating' : run.stack >= 110 ? 'Strong' : run.stack >= 75 ? 'Healthy' : run.stack >= 40 ? 'Hanging on' : 'On fumes. Careful.';
  const cashUp = drCashNet(run, 0);
  return `<section class="drhud pl-${ev.venue}">
    <div class="drhud-top"><div><div class="drkicker">${esc(DR_PLACES[ev.venue].name)}</div><b>${esc(ev.name)}</b></div>${drBtn(`ghost sm ${run.confirmQuit ? 'danger' : ''}`, 'data-dr="quit"', run.confirmQuit ? (ev.kind === 'cash' ? 'Tap again to cash out' : 'Tap again: you forfeit') : ev.kind === 'cash' ? 'Cash out' : 'Walk away')}</div>
    ${ev.kind !== 'cash' ? `<div class="drstages">${run.stages.map((st, k) => `<span class="${k < run.si ? 'done' : k === run.si ? 'now' : ''}">${DR_STAGE[st].label}</span>`).join('')}</div>` : ''}
    <div class="drhud-row">
      <div class="drstack"><small>${ev.kind === 'cash' ? 'Session' : 'Tournament life'}</small><div class="drbar stack ${run.stack < 40 ? 'low' : run.stack > 140 ? 'big' : ''}"><i style="width:${Math.min(100, run.stack / 2)}%"></i></div><b class="mono">${ev.kind === 'cash' ? `${cashUp >= 0 ? '+' : '−'}$${drMoney(Math.abs(cashUp))}` : status}</b></div>
      ${leftN ? `<div class="drleft"><small>Players left</small><b class="mono">${drMoney(leftN)}</b></div>` : ''}
      ${ev.kind !== 'cash' ? `<div class="drleft"><small>${ev.kind === 'sat' ? 'Seats' : 'Paid'}</small><b class="mono">${drMoney(P)}</b></div>` : ''}
    </div>
  </section>`;
}
function drRunHTML() {
  const run = DR.run, ev = run.ev, stage = run.stages[Math.min(run.si, run.stages.length - 1)], head = drHudHTML(run);
  if (run.state === 'loading' || run.state === 'ready') {
    const tips = DR_TIPS[stage] || DR_TIPS.early, tip = tips[(run.tip + run.results.length) % tips.length];
    let meet = stage === 'final' && run.boss ? drMeetLine(run.boss) : null;
    // someone famous at your table earlier in the day
    const field = (ev.legends || []).filter((id) => id !== run.boss);
    if (!meet && field.length && stage !== 'early' && stage !== 'final' && run.tip % 2 === 1) { const L = drLegend(field[run.tip % field.length]); meet = { sp: 'L:' + L.id, t: `${L.style}` }; }
    const who = meet ? drSpeaker(meet) : drSpeaker({ sp: 'slick' });
    const intro = stage === 'early' ? (ev.kind === 'cash' ? 'Grab a rack. Find a seat.' : `${drMoney(ev.field)} players. Shuffle up and deal.`)
      : stage === 'satbubble' ? `${drMoney(drPaid(ev) + 3)} left for ${drMoney(drPaid(ev))} seats. A seat is a seat: chips above it are worth nothing.`
      : stage === 'middle' && ev.kind === 'sng' ? 'Two tables are about to become one. Short stacks are getting itchy.'
      : stage === 'middle' ? `Day two. About ${drMoney(Math.round(ev.field * 0.45))} left. The fish are gone.`
      : stage === 'deep' ? `In the money, ${drMoney(Math.max(10, Math.round(drPaid(ev) * 0.3)))} left. Every pay jump is real.`
      : stage === 'bubble' ? `${drMoney(drPaid(ev) + 1)} left. ${drMoney(drPaid(ev))} get paid. Somebody is going home with nothing.`
        : stage === 'final' && ev.kind === 'sng' ? `Down to the last few. ${ev.paid} get paid, only one wins.`
        : stage === 'final' ? (run.boss === 'ghost' ? 'Heads-up for the world title.' : 'The final table. Lights, cameras, pay jumps.') : 'Grab a rack. Find a seat.';
    return `${head}<section class="drstagecard pl-${ev.venue}">
      <div class="drkicker">${esc(DR_STAGE[stage].long)}</div><h2 class="drh">${esc(intro)}</h2>
      <div class="drsay">${who.av}<div><b>${meet && stage !== 'final' ? `At your table: ${esc(who.name)}` : esc(who.name)}</b><p>${esc(meet ? meet.t : tip)}</p></div></div>
      ${run.state === 'loading' ? `<div class="drbar load"><i id="drLoadBar"></i></div><small class="drhint">Setting up the table…</small>` : drBtn('gold big', 'data-dr="deal"', run.results.length ? 'Deal me in ▸' : "Shuffle up and deal ▸")}
    </section>`;
  }
  if (run.state === 'cooler') return `${head}<section class="drstagecard cooler pl-${ev.venue}"><div class="drkicker">Cooler</div><h2 class="drh">${esc(run.cooler)}</h2>
      <div class="drsay">${drSpeaker({ sp: 'slick' }).av}<div><b>Slick</b><p>You played it right. That's all you control. People insist on calling it luck.</p></div></div>
      ${drBtn('gold big', 'data-dr="coolerdone"', 'See where you finished ▸')}</section>`;
  if (run.state === 'dealing') return `${head}<section class="drspot dim"><div class="drdealing"><span></span><span></span><span></span></div><small class="drhint">Dealing…</small></section>`;
  const sp = run.spot, keep = TR.spot; TR.spot = sp; const v = trSpotView(); TR.spot = keep;
  const hero = v.seats[sp.hero], label = (a) => { const o = v.opts.find((x) => x.k === a.k); return o ? o.t : a.k; };
  const pgx = drPg(DR.st), felt = DR_FELTS[pgx.eq.felt] || DR_FELTS.green, back = DR_BACKS[pgx.eq.back] || DR_BACKS.classic;
  let html = `${head}<section class="drspot st-${run.state}" style="--felt:${felt.c};--trim:${felt.trim || 'transparent'};--back:${back.bg};--backbd:${back.bd}">
    <div class="drspot-head"><span class="drkicker">Hand ${run.results.length + (run.state === 'answered' ? 0 : 1)}</span><span class="drhint">${esc(trCategory(v))}</span></div>
    <div class="drseats">${(() => {
      // at the final table, the boss sits in the biggest stack that isn't you
      let bossSeat = -1, bossName = '';
      if (stage === 'final' && run.boss) { bossName = run.boss === 'ghost' ? 'The Ghost' : (DR_PEOPLE[run.boss] || drLegend(run.boss) || {}).name || ''; let big = -1; v.seats.forEach((x, k) => { if (k !== sp.hero && x.stack > big) { big = x.stack; bossSeat = k; } }); }
      return v.seats.map((x, k) => `<div class="drseat ${k === sp.hero ? 'me' : ''} ${x.folded ? 'fold' : ''} ${k === bossSeat ? 'boss' : ''}">${k !== sp.hero && !x.folded ? '<span class="drseat-cards"><i></i><i></i></span>' : ''}<b>${x.pos}</b>${k === bossSeat ? `<small class="drbossname">${esc(bossName)}</small>` : ''}<small class="mono">${fmt(x.stack - x.put)}bb</small><small>${esc(x.acts.join(' · ') || (k === sp.hero ? 'you' : ''))}</small></div>`).join('');
    })()}</div>
    <div class="drhero"><div class="drcards">${trCardHTML(sp.hands[sp.hero][0])}${trCardHTML(sp.hands[sp.hero][1])}</div><div><b>${hero.pos} · your move</b><small class="mono">pot ${fmt(v.pot)} bb</small></div></div>`;
  if (run.state === 'play') html += `<div class="dropts">${sp.res.actions.map((a, k) => drBtn('opt', `data-dr="ans" data-k="${a.k}"`, `<span class="kbd">${k + 1}</span>${esc(label(a))}`)).join('')}</div>`;
  else {
    const r = run.results[run.results.length - 1], per = run.per || (ev.kind === 'cash' ? DR_CASH_HANDS : DR_HANDS);
    const lastHand = run.h === per - 1 && run.si === run.stages.length - 1;
    const L = DR_LABELS[r.lab] || DR_LABELS.good, d = r.after - r.before, h = r.hand;
    const lossTxt = h.loss == null ? '' : h.loss <= 1e-9 ? 'No value lost' : `−${h.unit === 'icm' ? `${h.loss.toFixed(h.loss < 0.1 ? 3 : 2)}% of the prize pool` : h.unit === 'bbicm' ? `${h.loss.toFixed(2)} bb of tournament equity` : `${h.loss.toFixed(2)} bb`}`;
    const mood = r.tier === 'perfect' ? 'smile' : r.tier === 'bad' ? 'frown' : 'flat';
    html += `<div class="drverdict t-${r.tier} l-${r.lab}" style="--lc:${L.c}"><div class="drverdict-face">${drAvatarSVG(DR.st, { face: true, mouth: mood })}</div><div><span class="drlab"><i>${L.icon}</i>${esc(L.n)}</span><small>${esc(L.line)}${lossTxt ? ` · <span class="mono">${lossTxt}</span>` : ''}</small></div><span class="mono drdelta ${d >= 0 ? 'up' : 'down'}">${ev.kind === 'cash' ? `${d >= 0 ? '+' : '−'}$${drMoney(Math.abs(drCashNet({ ev, stack: r.after }, 0) - drCashNet({ ev, stack: r.before }, 0)))}` : d > 0 ? 'Life ▲' : d < -20 ? 'Life ▼▼' : d < 0 ? 'Life ▼' : 'Life ='}</span></div>
      ${run.combo >= 3 && r.tier !== 'bad' ? `<div class="drcombo">🔥 ${run.combo} in a row</div>` : ''}
      <div class="drmix">${sp.res.actions.map((a, k) => `<div class="${k === r.j ? 'mine' : ''}"><span>${esc(label(a))}${k === r.j ? ' · you' : ''}</span><i style="--w:${(r.strat[k] * 100).toFixed(1)}%"></i><b class="mono">${Math.round(r.strat[k] * 100)}%</b></div>`).join('')}</div>
      ${drTip(r) ? `<div class="drtip">${drSpeaker({ sp: 'slick' }).av}<p>${esc(drTip(r))}</p></div>` : ''}
      ${run.bust ? `<div class="drbust">${ev.kind === 'cash' ? 'Felted. That session is over.' : 'Busted. Your tournament is over.'}</div>` : ''}
      <div class="drrow">${drBtn('gold big', 'data-dr="next"', run.bust || lastHand ? (ev.kind === 'cash' ? 'Cash out ▸' : 'See where you finished ▸') : run.h === per - 1 ? 'Next level ▸' : 'Next hand ▸')}</div>`;
  }
  return html + '</section>';
}

// ---------------------------------------------------------------- results
// money on the results screen rolls up from where you started
function drCountUp() {
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  document.querySelectorAll('#dream [data-count]').forEach((el) => {
    const [a, b] = el.dataset.count.split(':').map(Number); if (reduce || a === b) { el.textContent = '$' + drMoney(b); return; }
    const t0 = performance.now(), dur = 1400;
    const step = (t) => { const k = Math.min(1, (t - t0) / dur), e = 1 - Math.pow(1 - k, 3); el.textContent = '$' + drMoney(a + (b - a) * e); if (k < 1 && el.isConnected) requestAnimationFrame(step); };
    requestAnimationFrame(step);
  });
}
// the post-game review: accuracy, label counts, then every decision you made
function drReviewHTML(r) {
  const all = r.all || [], acc = r.acc != null ? r.acc : drAccuracy(all), n = all.length;
  const order = ['brilliant', 'great', 'best', 'excellent', 'good', 'inaccuracy', 'mistake', 'blunder'];
  const cnt = {}; all.forEach((x) => { cnt[x.lab] = (cnt[x.lab] || 0) + 1; });
  const ring = `<svg viewBox="0 0 120 120" class="dracc-ring" aria-hidden="true"><circle cx="60" cy="60" r="50" stroke="rgba(255,255,255,.1)" stroke-width="12" fill="none"/><circle cx="60" cy="60" r="50" stroke="${acc >= 85 ? '#8bd17c' : acc >= 65 ? '#f2c14e' : '#e5484d'}" stroke-width="12" fill="none" stroke-linecap="round" stroke-dasharray="${(acc / 100) * 314.2} 314.2" transform="rotate(-90 60 60)"/></svg>`;
  const verdict = acc >= 95 ? 'Solver-level poker.' : acc >= 85 ? 'Strong, disciplined play.' : acc >= 70 ? 'Solid, with a few leaks.' : acc >= 50 ? 'Some costly spots. Review them.' : 'Rough one. The review is where you get better.';
  const row = (x, i) => { const h = x.hand, L = DR_LABELS[x.lab] || DR_LABELS.good;
    const loss = h.loss == null || h.loss <= 1e-9 ? '' : `−${h.unit === 'icm' ? h.loss.toFixed(2) + '%' : h.loss.toFixed(2) + 'bb'}`; // bbicm shows as bb
    return `<details class="drrev-row" ${x.tier === 'bad' && i < 40 ? '' : ''}><summary><i class="drrev-ic" style="--lc:${L.c}">${L.icon}</i><span class="drkey-cards">${h.cards.map((c) => `<span class="mono s-${SUITS[c & 3]}">${RANKS[c >> 2]}${SYM[SUITS[c & 3]]}</span>`).join('')}</span><span class="drrev-what"><b>${esc(h.pos)} · ${esc(h.bb)}bb</b><small>${esc(h.cat)}</small></span><span class="drrev-play"><small>You</small><b>${esc(h.mine)}</b></span>${loss ? `<span class="mono drrev-loss">${loss}</span>` : ''}</summary>
      <div class="drrev-body"><div class="drmix">${(h.mix || []).map(([t, f]) => `<div class="${t === h.mine ? 'mine' : ''}"><span>${esc(t)}${t === h.mine ? ' · you' : ''}</span><i style="--w:${f}%"></i><b class="mono">${f}%</b></div>`).join('')}</div>${drTip({ tier: x.tier, stage: x.stage, hand: h }) ? `<p class="drhint">${esc(drTip({ tier: x.tier, stage: x.stage, hand: h }))}</p>` : ''}</div></details>`; };
  return `<div class="drreview"><div class="drreview-top"><div class="dracc">${ring}<b class="mono">${acc}</b><small>Accuracy</small></div><div><div class="drkicker">Game review</div><b class="drreview-v">${verdict}</b>
      <div class="drlabs">${order.filter((k) => cnt[k]).map((k) => `<span style="--lc:${DR_LABELS[k].c}"><i>${DR_LABELS[k].icon}</i>${cnt[k]} ${esc(DR_LABELS[k].n)}</span>`).join('')}</div>${r.bestCombo >= 3 ? `<small class="drhint">Best run: ${r.bestCombo} good decisions in a row</small>` : ''}</div></div>
    ${n ? `<div class="drrev-list">${all.map(row).join('')}</div><small class="drhint">Tap any hand to see the solver's full mix.</small>` : ''}</div>`;
}
function drKeysHTML(keys) {
  const r = { keys };
  return r.keys && r.keys.length ? `<div class="drkeys"><div class="drkicker">Key hands</div>${r.keys.map((k) => { const h = k.hand; return `<div class="drkey t-${k.tier}"><div class="drkey-cards">${h.cards.map((c) => `<span class="mono s-${SUITS[c & 3]}">${RANKS[c >> 2]}${SYM[SUITS[c & 3]]}</span>`).join('')}</div><div><b>${esc(h.pos)} · ${esc(h.bb)}bb · ${esc(h.cat)}</b><small>${k.tier === 'bad' ? `You: ${esc(h.mine)} (${h.myF}%). Solver: ${esc(h.best)} (${h.bestF}%).` : `You found ${esc(h.mine)}, a close spot the solver plays ${h.bestF}% of the time.`}</small></div></div>`; }).join('')}</div>` : '';
}
function drResultsHTML() {
  const r = DR.last, s = DR.st, ev = r.ev;
  const titleHTML = r.titles.map((t) => `<div class="drwin t-${t}"><div class="drwin-icon">${DR_TITLES[t].icon}</div><div><div class="drkicker">Champion</div><b>${esc(DR_TITLES[t].n)}</b><small>${esc(ev.name)}</small></div></div>`).join('');
  const beat = r.beat ? drLegend(r.beat) : null;
  let headline, sub;
  if (ev.kind === 'sat') { headline = r.ticket ? 'You won a seat' : r.place === r.paid + 1 ? 'Bubble boy' : `${drOrd(r.place)} place`; sub = r.ticket ? `Your seat in the ${drEvent(r.ticket).name} is waiting on the schedule.` : r.place === r.paid + 1 ? 'One spot from a seat. In a satellite, that hurts twice.' : `of ${drMoney(ev.field)} · ${ev.seats} seats`; }
  else if (ev.kind === 'cash') { headline = r.net > 0 ? `Up $${drMoney(r.net)}` : r.net < 0 ? `Down $${drMoney(-r.net)}` : 'Broke even'; sub = r.net > 0 ? 'Rack it up. That table is still talking about you.' : 'Some nights the cards win. Your decisions are what you keep.'; }
  else if (r.place === 1) { headline = 'You won it all'; sub = `1st of ${drMoney(ev.field)}${r.luck > 0.03 ? ' · you ran hot, and you earned it' : ''}`; }
  else if (r.prize > 0) { headline = `${drOrd(r.place)} place`; sub = `of ${drMoney(ev.field)} · ${r.place === r.paid ? 'a min-cash, but a cash' : r.place <= 9 ? 'a final table finish' : 'in the money'}`; }
  else { headline = r.place === r.paid + 1 ? 'Bubble boy' : `${drOrd(r.place)} place`; sub = r.place === r.paid + 1 ? 'One spot from the money. It happens to everyone once.' : `of ${drMoney(ev.field)} · out before the money`; }
  if (r.cooler) sub = `${sub} · coolered`;
  const acc = r.n ? Math.round(((r.counts.perfect + r.counts.good) / r.n) * 100) : 0;
  const confetti = r.titles.length ? `<div class="drconfetti" aria-hidden="true">${Array.from({ length: 36 }, (_, i) => `<i style="--x:${(i * 37) % 100}%;--d:${(i % 7) * 0.35}s;--c:${['#f4dfa6', '#d9b36c', '#e8705a', '#5aa9de', '#f3ecdc'][i % 5]};--r:${(i * 53) % 360}deg"></i>`).join('')}</div>` : '';
  return `<section class="drres pl-${ev.venue} ${r.titles.length ? 'champ' : ''}">${confetti}
    <div class="drkicker">${esc(ev.name)}</div>
    <h1 class="drres-h">${esc(headline)}</h1><p class="drres-sub">${esc(sub)}</p>
    ${titleHTML}
    ${r.titles.includes('me') && r.boss === 'ghost' ? `<div class="drbeat"><div class="drlegend-mono face">${drFaceOf(DR_PEOPLE.ghost)}</div><div><div class="drkicker">Heads-up for the world title</div><b>You beat The Ghost</b><small>Fourteen bracelets, and he finally smiled.</small></div></div>` : ''}
    ${beat ? `<div class="drbeat">${drLegendFace(beat)}<div><div class="drkicker">Legend beaten</div><b>${esc(beat.name)}</b><small>“${esc(beat.nick)}” · card added to your Legends</small></div></div>` : r.met && r.met !== 'ghost' && drLegend(r.met) ? `<div class="drbeat met">${drLegendFace(drLegend(r.met))}<div><div class="drkicker">You shared a final table with</div><b>${esc(drLegend(r.met).name)}</b><small>Beat them heads-up to win their card</small></div></div>` : ''}
    ${r.ticket ? `<div class="drwin"><div class="drwin-icon">🎟</div><div><div class="drkicker">Seat won</div><b>${esc(drEvent(r.ticket).name)}</b><small>Buy-in $${drMoney(drEvent(r.ticket).buyin)}, paid for.</small></div></div>` : ''}
    <div class="drmoney">${ev.kind === 'sat' ? '' : `<div><small>${ev.kind === 'cash' ? 'Cashed out' : 'Prize'}</small><b class="mono">$${drMoney(r.prize)}</b></div>`}<div><small>Bankroll</small><b class="mono">$${drMoney(r.before)} → <span data-count="${r.before}:${r.after}">$${drMoney(r.after)}</span></b></div>${r.repaid ? `<div><small>Paid back to Slick</small><b class="mono">$${drMoney(r.repaid)}</b></div>` : ''}</div>
    ${r.streak ? `<div class="drstreak">🔥 Daily Grind streak: <b>${r.streak} day${r.streak === 1 ? '' : 's'}</b></div>` : ''}
    ${drProgHTML(r.prog)}
    ${r.saved ? `<div class="drstreak">📌 ${r.saved} mistake${r.saved === 1 ? '' : 's'} saved to your deck for review</div>` : ''}
    ${drReviewHTML(r)}
    ${r.unlocked.length ? `<div class="drunlocks"><div class="drkicker">Unlocked</div>${r.unlocked.map((it) => `<div class="drunlock"><b>${esc(it.name)}</b><small>${esc(DR_SLOTS.find((x) => x[0] === it.slot)[1])} · ${esc(it.req)}</small></div>`).join('')}</div>` : ''}
    <div class="drrow">${drBtn('gold big', 'data-dr="hub"', 'Continue ▸')}${ev.daily ? '' : drBtn('ghost', `data-dr="play" data-id="${ev.id}" ${drAfford(s, ev) && ev.id !== 'local' ? '' : 'disabled'}`, 'Play it again')}</div>
  </section>`;
}

// ---------------------------------------------------------------- input
document.addEventListener('click', (e) => {
  const b = e.target.closest('#dream [data-dr]'); if (!b || !DR || b.disabled) return;
  const a = b.dataset.dr, s = DR.st;
  if (a === 'adv') { if (e.target.closest('[data-dr="choose"]')) return; drAdvance(); return; }
  if (a === 'choose') { e.stopPropagation(); drChoose(+b.dataset.k); return; }
  if (a === 'skip') { drSkip(); return; }
  if (a === 'continue') { DR.view = 'hub'; drRender(); return; }
  if (a === 'new') { const c = $('drNewConfirm'); if (c) { c.hidden = false; return; } DR.draft = { name: '', look: { ...DR_LOOK_DEFAULT } }; DR.view = 'create'; drRender(); return; }
  if (a === 'newyes') { DR.st = drDefault(); drSave(); DR.draft = { name: '', look: { ...DR_LOOK_DEFAULT } }; DR.view = 'create'; drRender(); return; }
  if (a === 'newno') { $('drNewConfirm').hidden = true; return; }
  if (a === 'title') { DR.view = 'title'; drRender(); return; }
  if (a === 'look') { const f = b.dataset.f; DR.draft.look[f] = f === 'skin' || f === 'hairC' || f === 'eyeC' ? +b.dataset.v : b.dataset.v; drRender(); return; }
  if (a === 'begin') {
    const n = (($('drName') || {}).value || '').trim();
    const st = drDefault(); st.made = true; st.name = n || 'Rookie'; st.look = { ...DR.draft.look };
    DR.st = st; drUnlocks(st); st.newItems = []; drSave(); drPlayScene('p_office'); return;
  }
  if (a === 'tab') { DR.tab = b.dataset.t; drRender(); return; }
  if (a === 'profile') { DR.view = 'profile'; DR.ptab = DR.ptab || 'quests'; drRender(); scrollTo(0, 0); return; }
  if (a === 'ptab') { DR.ptab = b.dataset.t; drRender(); return; }
  if (a === 'coseq') { const p = drPg(s); p.eq[b.dataset.kind] = b.dataset.id; drSave(); drRender(); return; }
  if (a === 'drill') { drDrillStart(b.dataset.k); return; }
  if (a === 'challenge') { drChallengeStart(); return; }
  if (a === 'review') { drReviewStart(); return; }
  if (a === 'chshare') { const t = drChallengeText(s); const done = (m) => { const el = $('drShareMsg'); if (el) el.textContent = m; }; if (navigator.share) navigator.share({ text: t }).catch(() => {}); else { try { navigator.clipboard.writeText(t); done('Copied. Paste it to your group.'); } catch { done(t); } } return; }
  if (a === 'savecopy') { const t = $('drSaveCode'); if (t) { t.select(); try { navigator.clipboard.writeText(t.value); } catch { document.execCommand('copy'); } const m = $('drSaveMsg'); if (m) m.textContent = 'Copied.'; } return; }
  if (a === 'saveload') { const t = $('drLoadCode'), err = drImportCode(t && t.value), m = $('drSaveMsg'); if (err) { if (m) m.textContent = err; return; } DR.view = 'hub'; DR.mode = 'story'; drRender(); scrollTo(0, 0); return; }
  if (a === 'mode') { DR.mode = b.dataset.m; if (DR.mode === 'story') DR.tab = 'events'; drRender(); return; }
  if (a === 'closet') { DR.mode = 'story'; DR.tab = 'closet'; drRender(); const t = document.querySelector('#dream .drtabs'); if (t) t.scrollIntoView({ block: 'start' }); return; }
  if (a === 'sng') { drSngStart(b.dataset.m, b.dataset.f); return; }
  if (a === 'lgb') { DR.lgb = b.dataset.k; drRender(); return; }
  if (a === 'lgnew' || a === 'lgjoin') {
    const inp = $(a === 'lgnew' ? 'drLgName' : 'drLgCode'), v = ((inp || {}).value || '').trim(); if (!v) { if (inp) inp.focus(); return; }
    drModeState(s);
    if (a === 'lgnew') { const code = 'CF-' + Array.from({ length: 4 }, () => 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'[(Math.random() * 32) | 0]).join(''); s.lg.priv.push({ name: v.slice(0, 24), code, own: true, fmt: 'sng9', d: Date.now() }); }
    else { const code = v.toUpperCase().replace(/[^A-Z0-9-]/g, '').slice(0, 9); if (!s.lg.priv.some((p) => p.code === code)) s.lg.priv.push({ name: `League ${code}`, code, own: false, fmt: 'sng9', d: Date.now() }); }
    drSave(); drRender(); return;
  }
  if (a === 'replay') { const id = b.dataset.id; DR.sc = { id, i: 0, extra: [], replay: true }; DR.view = 'scene'; drRender(); scrollTo(0, 0); return; }
  if (a === 'equip') { s.eq[b.dataset.s] = b.dataset.v; drSave(); drRender(); return; }
  if (a === 'play') { drStart(b.dataset.id); return; }
  if (a === 'deal') { drHand(); return; }
  if (a === 'ans') { drAnswer(b.dataset.k); return; }
  if (a === 'next') { drNext(); return; }
  if (a === 'quit') {
    const run = DR.run; if (!run) return;
    if (!run.confirmQuit) { run.confirmQuit = true; drRender(); setTimeout(() => { if (DR.run === run && run.confirmQuit) { run.confirmQuit = false; drRender(); } }, 3500); return; }
    drQuit(); return;
  }
  if (a === 'coolerdone') { drFinish(); return; }
  if (a === 'sound') { if (AR) { AR.st.sound = AR.st.sound === false; arSave(); } drRender(); return; }
  if (a === 'howto') { s.flags.howto = true; drSave(); drRender(); return; }
  if (a === 'hub') { DR.view = 'hub'; DR.tab = 'events'; drRender(); scrollTo(0, 0); return; }
});
document.addEventListener('keydown', (e) => {
  if (!DR || S.mode !== 'trainer' || e.target.closest('input,select,textarea')) return;
  if (DR.view === 'scene' && (e.key === ' ' || e.key === 'Enter')) { e.preventDefault(); drAdvance(); return; }
  const run = DR.run; if (DR.view !== 'run' || !run) return;
  if (run.state === 'play' && /^[1-9]$/.test(e.key)) { const a = run.spot.res.actions[+e.key - 1]; if (a) drAnswer(a.k); }
  else if (e.key === 'Enter' && run.state === 'answered') drNext();
  else if (e.key === 'Enter' && run.state === 'ready') drHand();
});
