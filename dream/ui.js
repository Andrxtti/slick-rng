// ================================================================ THE DREAM: screens
// title → creator → story scenes → life hub (schedule, closet, trophies, legends, story) → event → results

const drBtn = (cls, attrs, label) => `<button class="drbtn ${cls}" ${attrs}>${label}</button>`;
const drPortraitImg = () => { const i = document.querySelector('.brandmark img'); return i ? i.src : ''; };

function drRender() {
  if (!DR || S.mode !== 'trainer') return;
  const box = $('dream'); if (!box) return;
  const s = DR.st;
  if (DR.view === 'hub' && s.queue.length && !DR.run) { drPlayScene(s.queue.shift()); return; }
  box.className = 'dream v-' + DR.view;
  box.innerHTML = DR.view === 'title' ? drTitleHTML() : DR.view === 'create' ? drCreateHTML() : DR.view === 'scene' ? drSceneHTML()
    : DR.view === 'run' ? drRunHTML() : DR.view === 'results' ? drResultsHTML() : drHubHTML();
  if (DR.view === 'scene') drType();
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
  return `<section class="drscene pl-${place}">
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
  const m = /^c(\d)$/.exec(id); if (m) s.ch = Math.max(s.ch, +m[1]);
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
function drHubHTML() {
  const s = DR.st, chd = DR_CHAPTERS[Math.min(7, s.ch)], pr = chd.prog ? chd.prog(s) : [s.story.p_after ? 1 : 0, ''];
  const home = DR_ITEMS.find((x) => x.slot === 'home' && x.id === s.eq.home), ride = DR_ITEMS.find((x) => x.slot === 'ride' && x.id === s.eq.ride);
  const tab = DR.tab || 'events';
  const fresh = (s.newItems || []).length;
  return `<section class="drlife hm-${s.eq.home}">
      <div class="drlife-av">${drAvatarSVG(s)}</div>
      <div class="drlife-info">
        <div class="drkicker">${esc(chd.n)} · ${esc(chd.t)}</div>
        <div class="drlife-name">${esc(s.name)}</div>
        <div class="drlife-rep">${esc(drRep(s))}</div>
        <div class="drbank"><small>Bankroll</small><b class="mono">$${drMoney(s.br)}</b>${s.debt > 0 ? `<span class="drdebt">Owes Slick $${drMoney(s.debt)}</span>` : ''}</div>
        <div class="drlifestyle"><span>${DR_HOME_ICON[s.eq.home] || '🏠'} ${esc(home ? home.name : '')}</span><span>${DR_RIDE_ICON[s.eq.ride] || '🚗'} ${esc(ride ? ride.name : '')}</span></div>
      </div>
    </section>
    ${drTitleStrip(s)}
    <section class="drgoal">
      <div><div class="drkicker">${esc(chd.n)} goal</div><b>${esc(chd.goal)}</b><div class="drbar"><i style="width:${(pr[0] * 100).toFixed(1)}%"></i></div><small>${esc(pr[1])}</small></div>
      ${drBtn('ghost sm', `data-dr="replay" data-id="${s.ch === 0 ? 'p_diner' : 'c' + Math.min(7, s.ch)}"`, '▶ Story')}
    </section>
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
    <div class="drev-top"><span class="drev-kind">${ev.kind === 'cash' ? 'Cash game' : ev.title ? `${DR_TITLES[ev.title].icon} ${DR_TITLES[ev.title].n} event` : 'Tournament'}</span><span class="drrisk r-${rk}">${esc(rl)}</span></div>
    <h3>${esc(ev.name)}</h3>
    <p>${esc(ev.blurb)}</p>
    <div class="drev-facts">
      <span><small>Buy-in</small><b class="mono">${ev.free ? 'FREE' : '$' + drMoney(ev.buyin)}</b></span>
      ${ev.kind === 'mtt' ? `<span><small>Field</small><b class="mono">${drMoney(ev.field)}</b></span><span><small>1st place</small><b class="mono">$${drMoneyShort(first)}</b></span>` : `<span><small>Venue</small><b>${esc(pl.name)}</b></span>`}
    </div>
    ${boss || legends.length ? `<div class="drev-legends">${boss ? `<span>Final boss: <b>${esc(boss)}</b></span>` : ''}${legends.length ? `<span>Legends in the field: ${legends.map((l) => `<b class="${s.legends[l.id] ? 'beat' : ''}">${esc(l.name)}</b>`).join(', ')}</span>` : ''}</div>` : ''}
    <div class="drev-bot"><small>${rec ? `Played ${rec.played}${ev.kind === 'mtt' ? ` · best ${rec.best ? drOrd(rec.best) : '—'}${rec.wins ? ` · ${rec.wins} win${rec.wins > 1 ? 's' : ''}` : ''}` : ` · net ${rec.net >= 0 ? '+' : '−'}$${drMoney(Math.abs(rec.net))}`}` : 'Never played'}</small>
      ${drBtn(rk === 'no' ? 'ghost' : 'gold', `data-dr="play" data-id="${ev.id}" ${rk === 'no' ? 'disabled' : ''}`, rk === 'no' ? 'Can\'t afford it' : ev.kind === 'cash' ? 'Sit down' : 'Register')}</div>
  </article>`;
}
function drEventsHTML() {
  const s = DR.st, list = drAvailable(s), now = list.filter((e) => e.ch === s.ch || s.ch === 0), older = list.filter((e) => !now.includes(e));
  return `${s.ch === 0 ? '' : `<p class="drhint">Your bankroll decides what you can enter. Big shots pay big, and busting hurts. ${s.debt > 0 ? 'Slick takes half your profits until his stake is paid back.' : ''}</p>`}
    <div class="drevs">${now.map(drEventCard).join('')}</div>
    ${older.length ? `<details class="drolder"><summary>Earlier stops (${older.length})</summary><div class="drevs">${older.map(drEventCard).join('')}</div></details>` : ''}`;
}
function drClosetHTML() {
  const s = DR.st, fresh = new Set(s.newItems || []);
  const html = DR_SLOTS.map(([slot, label]) => {
    const items = DR_ITEMS.filter((x) => x.slot === slot);
    return `<div class="drslot"><div class="drkicker">${label}</div><div class="dritems">${items.map((it) => {
      const own = drOwns(s, slot, it.id), on = s.eq[slot] === it.id, k = drItemKey(it);
      return `<button class="dritem ${own ? '' : 'locked'} ${on ? 'on' : ''}" data-dr="equip" data-s="${slot}" data-v="${it.id}" ${own ? '' : 'disabled'}>${fresh.has(k) ? '<i class="drnew">New</i>' : ''}<b>${esc(it.name)}</b><small>${own ? (on ? 'Wearing' : 'Tap to use') : '🔒 ' + esc(it.req)}</small></button>`;
    }).join('')}</div></div>`;
  }).join('');
  if (fresh.size) { s.newItems = []; drSave(); }
  return `<div class="drcloset"><div class="drcloset-prev hm-${s.eq.home}">${drAvatarSVG(s)}</div><div class="drslots">${html}</div></div>`;
}
function drTrophiesHTML() {
  const s = DR.st, acc = s.dec ? Math.round(((s.perfect + s.good) / s.dec) * 100) : 0;
  return `<div class="drstats">
      <div><small>Events played</small><b class="mono">${s.played}</b></div><div><small>Cashes</small><b class="mono">${s.cashes}</b></div>
      <div><small>Final tables</small><b class="mono">${s.ft}</b></div><div><small>Peak bankroll</small><b class="mono">$${drMoneyShort(s.peak)}</b></div>
      <div><small>Decisions</small><b class="mono">${s.dec}</b></div><div><small>Solver-approved</small><b class="mono">${acc}%</b></div>
    </div>
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
  return `<div class="drstorylist">${list.map((id) => { const d = DR_SCENES[id]; return drBtn('ghost', `data-dr="replay" data-id="${id}"`, `▶ ${esc(d.title || '')} · ${esc(d.sub || '')}`); }).join('')}</div>
    <div class="drkicker">Money</div>
    <div class="drledger">${s.ledger.slice(-12).reverse().map((l) => `<div><span>${esc(l.why)}</span><b class="mono ${l.amt >= 0 ? 'up' : 'down'}">${l.amt >= 0 ? '+' : '−'}$${drMoney(Math.abs(l.amt))}</b></div>`).join('')}</div>
    <div class="drrow">${drBtn('ghost sm', 'data-dr="title"', 'Title screen')}</div>`;
}

// ---------------------------------------------------------------- an event in progress
function drHudHTML(run) {
  const ev = run.ev, stage = run.stages[Math.min(run.si, run.stages.length - 1)];
  const N = ev.field, P = ev.kind === 'mtt' ? drPaid(ev) : 0;
  const left = ev.kind === 'cash' ? null : stage === 'early' ? Math.max(P * 3, Math.round(N * (1 - 0.15 * (run.h + 1)))) : stage === 'middle' ? Math.max(P + 4, Math.round(N * (0.45 - 0.08 * run.h)))
    : stage === 'bubble' ? P + 1 - (run.h > 0 ? run.h : 0) : stage === 'deep' ? Math.max(10, Math.round(P * (0.3 - 0.08 * run.h))) : Math.max(2, 9 - run.h * 3);
  const bb = Math.round(run.stack / 4 * 10) / 10;
  return `<section class="drhud pl-${ev.venue}">
    <div class="drhud-top"><div><div class="drkicker">${esc(DR_PLACES[ev.venue].name)}</div><b>${esc(ev.name)}</b></div>${drBtn('ghost sm', 'data-dr="quit"', ev.kind === 'cash' ? 'Cash out' : 'Walk away')}</div>
    ${ev.kind === 'mtt' ? `<div class="drstages">${run.stages.map((st, k) => `<span class="${k < run.si ? 'done' : k === run.si ? 'now' : ''}">${DR_STAGE[st].label}</span>`).join('')}</div>` : ''}
    <div class="drhud-row">
      <div class="drstack"><small>${ev.kind === 'cash' ? 'Your stack' : 'Chips'}</small><div class="drbar stack ${run.stack < 40 ? 'low' : run.stack > 140 ? 'big' : ''}"><i style="width:${Math.min(100, run.stack / 2)}%"></i></div><b class="mono">${ev.kind === 'cash' ? `${bb * 4} bb` : `${bb} bb`}</b></div>
      ${left ? `<div class="drleft"><small>Players left</small><b class="mono">${drMoney(left)}</b></div>` : ''}
      ${ev.kind === 'mtt' ? `<div class="drleft"><small>Paid</small><b class="mono">${drMoney(P)}</b></div>` : ''}
    </div>
  </section>`;
}
function drRunHTML() {
  const run = DR.run, ev = run.ev, stage = run.stages[Math.min(run.si, run.stages.length - 1)], head = drHudHTML(run);
  if (run.state === 'loading' || run.state === 'ready') {
    const tips = DR_TIPS[stage], tip = tips[(run.tip + run.results.length) % tips.length];
    let meet = stage === 'final' && run.boss ? drMeetLine(run.boss) : null;
    // someone famous at your table earlier in the day
    const field = (ev.legends || []).filter((id) => id !== run.boss);
    if (!meet && field.length && stage !== 'early' && stage !== 'final' && run.tip % 2 === 1) { const L = drLegend(field[run.tip % field.length]); meet = { sp: 'L:' + L.id, t: `${L.style}` }; }
    const who = meet ? drSpeaker(meet) : drSpeaker({ sp: 'slick' });
    const intro = stage === 'early' ? (ev.kind === 'cash' ? 'Grab a rack. Find a seat.' : `${drMoney(ev.field)} players. Shuffle up and deal.`)
      : stage === 'middle' ? `Day two. About ${drMoney(Math.round(ev.field * 0.45))} left. The fish are gone.`
      : stage === 'deep' ? `In the money, ${drMoney(Math.max(10, Math.round(drPaid(ev) * 0.3)))} left. Every pay jump is real.`
      : stage === 'bubble' ? `${drMoney(drPaid(ev) + 1)} left. ${drMoney(drPaid(ev))} get paid. Somebody is going home with nothing.`
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
  let html = `${head}<section class="drspot">
    <div class="drspot-head"><span class="drkicker">Hand ${run.results.length + (run.state === 'answered' ? 0 : 1)}</span><span class="drhint">${esc(trCategory(v))}</span></div>
    <div class="drseats">${v.seats.map((x, k) => `<div class="drseat ${k === sp.hero ? 'me' : ''} ${x.folded ? 'fold' : ''}"><b>${x.pos}</b><small class="mono">${fmt(x.stack - x.put)}bb</small><small>${esc(x.acts.join(' · ') || (k === sp.hero ? 'you' : ''))}</small></div>`).join('')}</div>
    <div class="drhero"><div class="drcards">${trCardHTML(sp.hands[sp.hero][0])}${trCardHTML(sp.hands[sp.hero][1])}</div><div><b>${hero.pos} · your move</b><small class="mono">pot ${fmt(v.pot)} bb</small></div></div>`;
  if (run.state === 'play') html += `<div class="dropts">${sp.res.actions.map((a, k) => drBtn('opt', `data-dr="ans" data-k="${a.k}"`, `<span class="kbd">${k + 1}</span>${esc(label(a))}`)).join('')}</div>`;
  else {
    const r = run.results[run.results.length - 1], per = ev.kind === 'cash' ? DR_CASH_HANDS : DR_HANDS;
    const lastHand = run.h === per - 1 && run.si === run.stages.length - 1;
    const big = { perfect: 'Textbook.', good: 'Solid.', close: 'Playable.', bad: 'Mistake.' }[r.tier], sub = { perfect: "The solver's favourite play.", good: 'A play the solver makes too.', close: 'The solver mixes it in, but prefers something else.', bad: 'The solver (almost) never does this.' }[r.tier];
    const d = r.after - r.before;
    html += `<div class="drverdict t-${r.tier}"><div><b>${big}</b><small>${sub}</small></div><span class="mono drdelta ${d >= 0 ? 'up' : 'down'}">${d >= 0 ? '+' : '−'}${Math.abs(d / 4)} bb</span></div>
      <div class="drmix">${sp.res.actions.map((a, k) => `<div class="${k === r.j ? 'mine' : ''}"><span>${esc(label(a))}${k === r.j ? ' · you' : ''}</span><i style="--w:${(r.strat[k] * 100).toFixed(1)}%"></i><b class="mono">${Math.round(r.strat[k] * 100)}%</b></div>`).join('')}</div>
      ${run.bust ? `<div class="drbust">${ev.kind === 'cash' ? 'Felted. That session is over.' : 'Busted. Your tournament is over.'}</div>` : ''}
      <div class="drrow">${drBtn('gold big', 'data-dr="next"', run.bust || lastHand ? (ev.kind === 'cash' ? 'Cash out ▸' : 'See where you finished ▸') : run.h === per - 1 ? 'Next level ▸' : 'Next hand ▸')}</div>`;
  }
  return html + '</section>';
}

// ---------------------------------------------------------------- results
function drResultsHTML() {
  const r = DR.last, s = DR.st, ev = r.ev;
  const titleHTML = r.titles.map((t) => `<div class="drwin t-${t}"><div class="drwin-icon">${DR_TITLES[t].icon}</div><div><div class="drkicker">Champion</div><b>${esc(DR_TITLES[t].n)}</b><small>${esc(ev.name)}</small></div></div>`).join('');
  const beat = r.beat ? drLegend(r.beat) : null;
  let headline, sub;
  if (ev.kind === 'cash') { headline = r.net > 0 ? `Up $${drMoney(r.net)}` : r.net < 0 ? `Down $${drMoney(-r.net)}` : 'Broke even'; sub = r.net > 0 ? 'Rack it up. That table is still talking about you.' : 'Some nights the cards win. Your decisions are what you keep.'; }
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
    <div class="drmoney"><div><small>${ev.kind === 'cash' ? 'Cashed out' : 'Prize'}</small><b class="mono">$${drMoney(r.prize)}</b></div><div><small>Bankroll</small><b class="mono">$${drMoney(r.before)} → $${drMoney(r.after)}</b></div>${r.repaid ? `<div><small>Paid back to Slick</small><b class="mono">$${drMoney(r.repaid)}</b></div>` : ''}</div>
    <div class="drdec"><div class="drkicker">Your decisions · ${acc}% solver-approved</div>
      <div class="drdec-row"><span class="t-perfect">${r.counts.perfect} textbook</span><span class="t-good">${r.counts.good} solid</span><span class="t-close">${r.counts.close} playable</span><span class="t-bad">${r.counts.bad} mistake${r.counts.bad === 1 ? '' : 's'}</span></div></div>
    ${r.keys && r.keys.length ? `<div class="drkeys"><div class="drkicker">Key hands</div>${r.keys.map((k) => { const h = k.hand; return `<div class="drkey t-${k.tier}"><div class="drkey-cards">${h.cards.map((c) => `<span class="mono s-${SUITS[c & 3]}">${RANKS[c >> 2]}${SYM[SUITS[c & 3]]}</span>`).join('')}</div><div><b>${esc(h.pos)} · ${esc(h.bb)}bb · ${esc(h.cat)}</b><small>${k.tier === 'bad' ? `You: ${esc(h.mine)} (${h.myF}%). Solver: ${esc(h.best)} (${h.bestF}%).` : `You found ${esc(h.mine)}, a close spot the solver plays ${h.bestF}% of the time.`}</small></div></div>`; }).join('')}</div>` : ''}
    ${r.unlocked.length ? `<div class="drunlocks"><div class="drkicker">Unlocked</div>${r.unlocked.map((it) => `<div class="drunlock"><b>${esc(it.name)}</b><small>${esc(DR_SLOTS.find((x) => x[0] === it.slot)[1])} · ${esc(it.req)}</small></div>`).join('')}</div>` : ''}
    <div class="drrow">${drBtn('gold big', 'data-dr="hub"', 'Continue ▸')}${drBtn('ghost', `data-dr="play" data-id="${ev.id}" ${drAfford(s, ev) && ev.id !== 'local' ? '' : 'disabled'}`, 'Play it again')}</div>
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
  if (a === 'look') { const f = b.dataset.f; DR.draft.look[f] = f === 'skin' || f === 'hairC' ? +b.dataset.v : b.dataset.v; drRender(); return; }
  if (a === 'begin') {
    const n = (($('drName') || {}).value || '').trim();
    const st = drDefault(); st.made = true; st.name = n || 'Rookie'; st.look = { ...DR.draft.look };
    DR.st = st; drUnlocks(st); st.newItems = []; drSave(); drPlayScene('p_office'); return;
  }
  if (a === 'tab') { DR.tab = b.dataset.t; drRender(); return; }
  if (a === 'replay') { const id = b.dataset.id; DR.sc = { id, i: 0, extra: [], replay: true }; DR.view = 'scene'; drRender(); scrollTo(0, 0); return; }
  if (a === 'equip') { s.eq[b.dataset.s] = b.dataset.v; drSave(); drRender(); return; }
  if (a === 'play') { drStart(b.dataset.id); return; }
  if (a === 'deal') { drHand(); return; }
  if (a === 'ans') { drAnswer(b.dataset.k); return; }
  if (a === 'next') { drNext(); return; }
  if (a === 'quit') { drQuit(); return; }
  if (a === 'coolerdone') { drFinish(); return; }
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
