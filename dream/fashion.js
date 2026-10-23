// ================================================================ THE DREAM: fashion
// Every piece has a grade, a maker, a line about what it says, a record of where you earned it and of what you
// did while wearing it. Wear a piece while you play well and it breaks in: new colourways and metal finishes,
// then your monogram, then a finish that catches the light. Nothing is bought. Everything is earned at the table.

const DR_GRADE = {
  standard: ['Standard', '#a3a9b2', 1], rare: ['Rare', '#5b9cf5', 3], elite: ['Elite', '#b582f7', 6],
  legendary: ['Legendary', '#f0c25a', 10], iconic: ['Iconic', '#ff5a6e', 15],
};
// fictional makers, real-world feel
const DR_BRANDS = {
  valuemart: 'ValueMart', campus: 'Campus Co-op', drift: 'Drift Supply', feltkings: 'Felt Kings', northline: 'Northline',
  riverside: 'Riverside Card Room', castellane: 'Castellane', vellard: 'Maison Vellard', noir: 'Atelier Noir',
  lumen: 'Lumen Optical', rayden: 'Rayden', ridgeline: 'Ridgeline', slick: "Slick's", lonestar: 'Lone Star Hatters', sonder: 'Sonder Audio',
  quikset: 'Quikset', ostrander: 'Ostrander', valcour: 'Valcour', kade: 'Kade & Co.', marlowe: 'Marlowe Diamond', wcs: 'WCS Official',
  grind: 'The Daily Grind', rusty: "Rusty's Tavern", cftilt: 'CF Tilt Poker', volt: 'VOLT Energy', apex: 'APEX',
  metro: 'Metro Transit', kestrel: 'Kestrel', sabre: 'Sabre', adler: 'Adler', veloce: 'Veloce', altair: 'Altair Jets',
  league: 'League exclusive', home: '',
};
// slot:id → [grade, maker, what it says, effect it always has]
const DR_META = {
  'outfit:office': ['standard', 'valuemart', 'Clearance rack, week one at the firm. Everyone starts somewhere.'],
  'outfit:tee': ['standard', 'valuemart', 'Bought in a three-pack. The uniform of people with somewhere better to be.'],
  'outfit:cardigan': ['standard', 'campus', 'Study-group chic. You read the books before you played the hands.'],
  'outfit:hoodie': ['standard', 'drift', 'You wore it to your first tournament. You will never throw it away.'],
  'outfit:jersey': ['rare', 'feltkings', 'Lucky number 7. Earned with your first hundred decisions.'],
  'outfit:polo': ['rare', 'riverside', 'Riverside house merch. They only hand it to the regulars.'],
  'outfit:denim': ['rare', 'drift', 'Earned the night you first beat the $1/$2 game.'],
  'outfit:track': ['rare', 'drift', 'Three Daily Grinds in a row. Grinders recognize grinders.'],
  'outfit:vest': ['rare', 'castellane', 'Your first $10,000 bankroll. Dress like you plan to keep it.'],
  'outfit:fleece': ['rare', 'northline', 'For a desert summer that runs on casino air conditioning.'],
  'outfit:varsity': ['elite', 'feltkings', 'Circuit casher. The letter on the chest stands for Cashed.'],
  'outfit:leather': ['elite', 'vellard', '$50,000 bankroll. Broken in by a hundred 3 a.m. sessions.'],
  'outfit:bomber': ['elite', 'drift', 'Your first trophy. The jacket they photographed you in.'],
  'outfit:blazer': ['elite', 'castellane', 'Circuit regulars dress for the cameras.'],
  'outfit:suit': ['legendary', 'castellane', 'Tailored the week you won your first gold bracelet.'],
  'outfit:designer': ['legendary', 'vellard', 'Seven figures. The label nobody needs to read.'],
  'outfit:tux': ['iconic', 'noir', 'Fitted only for players who make the Main Event final table.', 'sheen'],
  'outfit:vampire': ['elite', 'league', 'Haunted Felt League champion. It only comes back in October.'],
  'outfit:flannel': ['elite', 'league', 'Harvest Cup champion. One month a year, one winner a month.'],
  'outfit:ugly': ['elite', 'league', 'Frostbite Series champion. Ugly on purpose. Rare on purpose.'],
  'eyes:readers': ['standard', 'lumen', 'Blue-light readers for twelve hours of spreadsheets a day.'],
  'eyes:round': ['standard', 'rayden', 'Wire frames. You look like you know the math. You do.'],
  'eyes:thick': ['standard', 'rayden', 'Thick black frames. Serious study, serious face.'],
  'eyes:sport': ['rare', 'ridgeline', 'Wraparounds for long days under casino lights.'],
  'eyes:tinted': ['rare', 'rayden', 'Tinted so nobody reads your eyes at a Circuit final table.'],
  'eyes:aviator': ['elite', 'rayden', 'Gold aviators. Circuit ring winners only.', 'sheen'],
  'eyes:shades': ['legendary', 'slick', 'Slick hands his 8-bit shades to people who win. Nobody else.'],
  'eyes:masq': ['rare', 'league', 'Haunted Felt League top 10.'], 'eyes:amber': ['rare', 'league', 'Harvest Cup top 10.'], 'eyes:snow': ['rare', 'league', 'Frostbite Series top 10.'],
  'head:cap': ['standard', 'drift', 'Backwards, like you have been here before.'],
  'head:bucket': ['standard', 'drift', 'Ten sessions in. The hat of someone who stays.'],
  'head:snap': ['rare', 'riverside', 'Riverside snapback. The card room knows your name now.'],
  'head:phones': ['rare', 'sonder', 'Noise-cancelling. The table talks, you do not hear it.'],
  'head:beanie': ['rare', 'northline', 'Vegas pool beanie. Summer Series veterans only.'],
  'head:visor': ['rare', 'riverside', 'An old dealer gave it to you after seven straight Daily Grinds.'],
  'head:cowboy': ['elite', 'lonestar', 'A Circuit ring and a Texas hat. Old-school respect.'],
  'head:fedora': ['legendary', 'vellard', 'High-roller fedora. $500,000 bankroll and the nerve to wear it.'],
  'head:pumpkin': ['rare', 'league', 'Haunted Felt League daily winner.'], 'head:pilgrim': ['rare', 'league', 'Harvest Cup daily winner.'], 'head:santa': ['rare', 'league', 'Frostbite Series daily winner.'],
  'ear:stud': ['standard', 'kade', 'Your first jewelry. Small, but it is yours.'],
  'ear:hoop': ['rare', 'kade', 'Gold hoops. $15,000 bankroll and some confidence.'],
  'ear:diamond': ['elite', 'marlowe', 'Diamond studs. They catch every light in the room.', 'glint'],
  'ring:signet': ['rare', 'kade', 'Silver signet. The first thing you bought with poker money.'],
  'ring:gold': ['elite', 'kade', 'Gold band, bought the night of your first bracelet.'],
  'ring:pinky': ['legendary', 'marlowe', 'Diamond pinky ring. Half a million and it shows.', 'glint'],
  'ring:champ': ['iconic', 'wcs', 'World champion ring. One is made each year.', 'glint'],
  'wrist:beads': ['standard', 'drift', 'Bead bracelet. Four sessions in and it already feels lucky.'],
  'wrist:gold': ['elite', 'kade', 'Gold cuff. Heavy enough that you notice it on every bet.', 'sheen'],
  'wrist:tennis': ['legendary', 'marlowe', 'Diamond tennis bracelet. A line of light on your wrist.', 'glint'],
  'watch:digital': ['standard', 'quikset', 'Gas-station digital. It always says 8:00, start of the shift.'],
  'watch:steel': ['rare', 'ostrander', 'Steel diver. The first real watch you ever owned.'],
  'watch:gold': ['legendary', 'ostrander', 'Gold Presidential. A quarter of a million bankroll on your wrist.', 'sheen'],
  'watch:iced': ['legendary', 'valcour', 'Iced-out chrono. Two million and every diamond is real.', 'glint'],
  'watch:hyper': ['iconic', 'valcour', 'Hypercar tourbillon. Made for world champions, a few a year.', 'glint'],
  'chain:silver': ['standard', 'kade', 'Thin silver chain. Quiet. For now.'],
  'chain:cuban': ['elite', 'kade', 'Gold Cuban link. Six figures and the whole room knows.', 'sheen'],
  'chain:rope': ['elite', 'kade', 'Rope chain with the CF pendant, for the people who backed you.', 'sheen'],
  'chain:layered': ['legendary', 'kade', 'Layered gold, for a gold bracelet winner.', 'sheen'],
  'chain:iced': ['legendary', 'marlowe', 'Iced Cuban link. A million dollars of light.', 'glint'],
  'chain:bones': ['rare', 'league', 'Haunted Felt League weekly winner.'], 'chain:wishbone': ['elite', 'league', 'Harvest Cup weekly winner.', 'sheen'], 'chain:lights': ['rare', 'league', 'Frostbite Series weekly winner.'],
  'patch:grind': ['rare', 'grind', 'Three-day Daily Grind streak. Sponsored by showing up.'],
  'patch:tavern': ['standard', 'rusty', "Rusty's backs you after your first $1/$2 win. Free wings."],
  'patch:site': ['rare', 'cftilt', 'CF Tilt Poker signs Circuit ring winners.'],
  'patch:energy': ['legendary', 'volt', 'VOLT sponsors gold bracelet winners only.'],
  'patch:global': ['iconic', 'apex', 'Global brand ambassador. Signed the week you won the Main Event.', 'holo'],
  'patch:boo': ['rare', 'league', 'Played the Haunted Felt League.'], 'patch:harvest': ['rare', 'league', 'Played the Harvest Cup.'], 'patch:frost': ['rare', 'league', 'Played the Frostbite Series.'],
  'sleeve:grind': ['rare', 'grind', 'Five-day Daily Grind streak.'],
  'sleeve:site': ['rare', 'cftilt', 'CF Tilt Poker sleeve for Circuit cashers.'],
  'sleeve:energy': ['legendary', 'volt', 'VOLT sleeve. Bracelet winners only.'],
  'sleeve:global': ['iconic', 'apex', 'APEX ambassador sleeve. World champions only.', 'holo'],
  'home:studio': ['standard', 'home', 'Above the laundromat. The dryers hum all night.'],
  'home:apt': ['rare', 'home', 'Downtown one-bedroom. Your own front door.'],
  'home:condo': ['elite', 'home', 'High-rise condo. The city looks small from up here.'],
  'home:penthouse': ['legendary', 'home', 'Strip penthouse. You can see the casino where it started.'],
  'home:mansion': ['legendary', 'home', 'Hills mansion. Five million and a gate.'],
  'home:island': ['iconic', 'home', 'A private island. Bought with a Main Event title.'],
  'ride:bus': ['standard', 'metro', 'Monthly pass. Route 34 to the office and back.'],
  'ride:hatch': ['standard', 'kestrel', "'09 Kestrel hatchback. It starts most mornings."],
  'ride:coupe': ['rare', 'sabre', 'Used Sabre coupe. Loud, fast, a little late on payments.'],
  'ride:sedan': ['elite', 'adler', 'Adler super-sedan. German, quiet, very quick.'],
  'ride:super': ['legendary', 'veloce', 'Veloce supercar. Italian, red, two million.'],
  'ride:jet': ['iconic', 'altair', 'A share of an Altair jet. Titans do not fly commercial.'],
};
const DR_WEARABLE = ['outfit', 'eyes', 'head', 'ear', 'chain', 'ring', 'wrist', 'watch', 'patch', 'sleeve'];
const drMeta = (slot, id) => { const m = DR_META[slot + ':' + id]; return m ? { g: m[0], brand: DR_BRANDS[m[1]] || '', story: m[2], fx: m[3] || null } : { g: 'standard', brand: '', story: '', fx: null }; };
const drGradeOf = (slot, id) => drMeta(slot, id).g;

// ---------------------------------------------------------------- wear: what you did in it, and how broken in it is
const DR_WEAR_AT = [0, 25, 100, 300]; // good decisions made while wearing it
const DR_WEAR_N = ['New', 'Broken in', 'Signature', 'Mastered'];
const DR_WEAR_GIVES = ['', 'New colourways and metal finishes', 'Your monogram, stitched or engraved', 'A finish that catches the light'];
const drWearKey = (slot, id) => slot + ':' + id;
function drWearRec(s, key) { s.wear = s.wear || {}; return s.wear[key] || (s.wear[key] = { g: 0, e: 0, c: 0, f: 0, w: 0, t: [] }); }
function drWearLv(s, key) { const g = ((s.wear || {})[key] || {}).g || 0; let l = 0; while (l < 3 && g >= DR_WEAR_AT[l + 1]) l++; return l; }
const drWorn = (s) => DR_WEARABLE.map((slot) => [slot, s.eq[slot]]).filter(([, id]) => id && id !== 'none');
// after every graded decision
// the gear you sat down in counts for the whole event, even if the results unlock and equip something new
function drWearDecision(s, good, out, run) {
  if (run && !run.worn) run.worn = drWorn(s);
  if (!good) return;
  for (const [slot, id] of (run && run.worn) || drWorn(s)) {
    const k = drWearKey(slot, id), r = drWearRec(s, k), before = drWearLv(s, k); r.g++;
    const after = drWearLv(s, k);
    if (after > before && out) { out.wear = out.wear || []; out.wear.push({ slot, id, lv: after }); }
  }
}
// after every finished event
function drWearEvent(s, res, run, flags) {
  for (const [slot, id] of run.worn || drWorn(s)) {
    const r = drWearRec(s, drWearKey(slot, id)); r.e++;
    if (flags.cashed) r.c++; if (flags.ft) r.f++;
    if (flags.won) { r.w++; if (res.ev.title || res.ev.kind === 'mtt') { r.t.unshift(res.ev.name); if (r.t.length > 5) r.t.pop(); } }
  }
}
// where and when you earned it
function drGot(s, key, at) { s.got = s.got || {}; if (!s.got[key]) s.got[key] = { d: Date.now(), at: at || '' }; }

// ---------------------------------------------------------------- colourways and metal finishes
// extra colourways added to the dye list (older saves keep their indices)
DR_DYES.push('#efe3c8', '#b4874e', '#24402f', '#5e1b2d', '#46505e', '#0f2a44');
const DR_DYE_BASIC = 13; // indices below this are the original twelve (plus "own colour")
const DR_FINISH = [['Yellow gold', '#e3b94f', 0.2], ['White gold', '#e4e7ec', 0.16], ['Rose gold', '#e7a487', 0.2], ['Black chrome', '#2e2e34', 0.24]];
const DR_METAL_SLOTS = { chain: 1, ring: 1, wrist: 1, watch: 1, ear: 1 };
const DR_NO_FINISH = { 'wrist:beads': 1, 'watch:digital': 1, 'chain:bones': 1, 'chain:lights': 1 };
const DR_BASE_METAL = { 'chain:silver': '#d2d6de', 'ring:signet': '#d2d6de', 'ring:pinky': '#d2d6de', 'ear:stud': '#d2d6de', 'ear:diamond': '#d2d6de', 'wrist:tennis': '#d2d6de', 'watch:steel': '#b9bec7', 'watch:iced': '#e9edf2', 'watch:hyper': '#26262a' };
const drHasFinish = (slot, id) => !!DR_METAL_SLOTS[slot] && id !== 'none' && !DR_NO_FINISH[slot + ':' + id];
// the metal colour to draw a piece in, or null for its own
function drFinishHex(st, slot) { const id = (st.eq || {})[slot], f = (st.fin || {})[slot + ':' + id]; return f != null && DR_FINISH[f] ? DR_FINISH[f] : null; }
const drIsLeague = (slot, id) => DR_LG_ITEMS.some((x) => x.slot === slot && x.id === id);
// which colours this piece can wear right now
function drDyeOpts(s, slot, id) {
  if ((slot !== 'outfit' && slot !== 'head') || id === 'none' || drIsLeague(slot, id)) return null;
  const lv = drWearLv(s, drWearKey(slot, id)), basic = !!DR_DYEABLE[id];
  return DR_DYES.map((c, i) => ({ i, c, ok: i === 0 || (i < DR_DYE_BASIC ? basic || lv >= 1 : lv >= 1), prem: i >= DR_DYE_BASIC }));
}
const drInitials = (n) => (n || '').trim().split(/\s+/).filter(Boolean).slice(0, 2).map((w) => w[0]).join('').toUpperCase() || 'CF';
// what the renderer needs to know about the player's gear (finishes, monogram, light-catching effects)
function drGearLook(st) {
  const lv = (slot) => { const id = (st.eq || {})[slot]; return id && id !== 'none' && st.wear ? drWearLv(st, drWearKey(slot, id)) : 0; };
  const fx = {};
  for (const slot of DR_WEARABLE) { const id = (st.eq || {})[slot]; if (!id || id === 'none') continue; const m = drMeta(slot, id), l = lv(slot); const f = l >= 3 ? (m.fx === 'glint' ? 'glint' : slot === 'outfit' || slot === 'head' ? 'thread' : m.fx || 'sheen') : m.fx; if (f) fx[slot] = f; }
  return { mono: lv('outfit') >= 2 ? drInitials(st.name) : '', monoChain: lv('chain') >= 2 ? drInitials(st.name) : '', fx };
}

// ---------------------------------------------------------------- fit rating and maker sets
const DR_FIT_N = [[92, 'Iconic'], [80, 'Red carpet'], [65, 'Dripping'], [45, 'Sharp'], [25, 'Clean'], [0, 'Off the rack']];
function drFitScore(s) {
  let pts = 0; const makers = {};
  for (const [slot, id] of drWorn(s)) { const m = drMeta(slot, id), l = drWearLv(s, drWearKey(slot, id)); pts += DR_GRADE[m.g][2] + [0, 1, 2, 4][l]; const mk = (DR_META[slot + ':' + id] || [])[1]; if (mk && mk !== 'league') makers[mk] = (makers[mk] || 0) + 1; }
  const sets = Object.entries(makers).filter(([, n]) => n >= 3).map(([mk, n]) => ({ mk, n }));
  pts += sets.reduce((a, x) => a + 3 * x.n, 0);
  const score = Math.max(1, Math.min(99, Math.round((pts * 99) / 140)));
  return { score, name: DR_FIT_N.find(([t]) => score >= t)[1], sets };
}

// ---------------------------------------------------------------- saved fits
const DR_FIT_SLOTS = 4;
function drFitSave(s, i) { s.fits = s.fits || []; const eq = {}; for (const k of DR_WEARABLE) eq[k] = s.eq[k]; s.fits[i] = { eq, dye: { ...s.dye }, fin: { ...(s.fin || {}) }, d: Date.now() }; }
function drFitWear(s, i) { const f = (s.fits || [])[i]; if (!f) return; for (const k of DR_WEARABLE) if (f.eq[k] && (f.eq[k] === 'none' || drOwns(s, k, f.eq[k]))) s.eq[k] = f.eq[k]; s.dye = { ...s.dye, ...f.dye }; s.fin = { ...(s.fin || {}), ...f.fin }; }

// ---------------------------------------------------------------- the closet
const drDate = (d) => new Date(d).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
function drPips(l) { return `<span class="drpips" aria-label="${DR_WEAR_N[l]}">${[1, 2, 3].map((i) => `<i class="${i <= l ? 'on' : ''}"></i>`).join('')}</span>`; }
function drInspectHTML(s, slot, id) {
  if (!id || id === 'none') return '';
  const it = [...DR_ITEMS, ...DR_LG_ITEMS].find((x) => x.slot === slot && x.id === id); if (!it) return '';
  const k = drWearKey(slot, id), m = drMeta(slot, id), G = DR_GRADE[m.g], worn = DR_WEARABLE.includes(slot), r = (s.wear || {})[k] || { g: 0, e: 0, c: 0, f: 0, w: 0, t: [] }, l = drWearLv(s, k), got = (s.got || {})[k];
  const next = l < 3 ? DR_WEAR_AT[l + 1] : null, base = DR_WEAR_AT[l];
  const lvl = worn ? `<div class="drlv">${drPips(l)}<b>${DR_WEAR_N[l]}</b>${next ? `<small>${r.g - base}/${next - base} good decisions in it until ${DR_WEAR_N[l + 1]}: ${DR_WEAR_GIVES[l + 1].toLowerCase()}</small><div class="drbar"><i style="width:${Math.min(100, ((r.g - base) / (next - base)) * 100).toFixed(1)}%"></i></div>` : '<small>Fully broken in. It is part of you now.</small>'}</div>` : '';
  const tags = [r.t.length ? `<span class="drtag gold">Title-winning ×${r.w}</span>` : r.w ? `<span class="drtag gold">Winner ×${r.w}</span>` : '', r.f ? `<span class="drtag">Final table ×${r.f}</span>` : '', r.c ? `<span class="drtag">Cashed ×${r.c}</span>` : ''].join('');
  const rec = worn && r.e ? `<small class="drinsp-rec">Worn for ${r.e} event${r.e === 1 ? '' : 's'}${r.t.length ? ` · won the ${r.t.slice(0, 2).map(esc).join(' and the ')} in it` : ''}</small>` : worn ? '<small class="drinsp-rec">Not worn in an event yet.</small>' : '';
  const prov = got ? `Earned ${drDate(got.d)}${got.at ? ' · ' + esc(got.at) : ''}` : it.req && it.req !== 'Where it all started' ? `Earned: ${esc(it.req)}` : 'With you from day one';
  const dyes = drDyeOpts(s, slot, id), fin = drHasFinish(slot, id);
  const sw = dyes ? `<div class="drkicker">Colourway${l < 1 && dyes.some((x) => !x.ok) ? ' · more unlock when it is Broken in' : ''}</div><div class="drsws">${dyes.map((x) => `<button class="drsw ${x.c ? '' : 'orig'} ${x.prem ? 'prem' : ''}" style="--c:${x.c || '#888'}" data-dr="dye" data-v="${x.i}" aria-pressed="${(s.dye[id] || 0) === x.i}" aria-label="${x.c ? 'Colourway ' + x.i : 'Original colour'}" ${x.ok ? '' : 'disabled'}></button>`).join('')}</div>` : '';
  const fs = fin ? `<div class="drkicker">Metal finish${l < 1 ? ' · unlocks when it is Broken in' : ''}</div><div class="drsws">${[null, ...DR_FINISH].map((f, i) => `<button class="drsw metal" style="--c:${f ? f[1] : DR_BASE_METAL[k] || '#e3b94f'}" data-dr="fin" data-v="${i - 1}" aria-pressed="${((s.fin || {})[k] ?? -1) === i - 1}" aria-label="${f ? f[0] : 'Original metal'}" title="${f ? f[0] : 'Original'}" ${l >= 1 || !f ? '' : 'disabled'}></button>`).join('')}</div>` : '';
  return `<div class="drinsp g-${m.g}" style="--gc:${G[1]}">
    <div class="drinsp-top"><span class="drgrade">${G[0]}</span>${m.brand ? `<span class="drbrand">${esc(m.brand)}</span>` : ''}${m.fx ? '<span class="drtag fx">✦ Catches the light</span>' : ''}</div>
    <b class="drinsp-name">${esc(it.name)}</b><p class="drinsp-story">${esc(m.story)}</p>
    ${lvl}${tags ? `<div class="drtags">${tags}</div>` : ''}${rec}<small class="drinsp-prov">${prov}</small>${sw}${fs}</div>`;
}
function drFitsHTML(s) {
  const f = drFitScore(s), fits = s.fits || [];
  return `<div class="drfitcard"><div class="drfit"><b class="mono">${f.score}</b><div><small>Fit rating</small><span>${f.name}</span></div></div>
    ${f.sets.length ? `<small class="drfit-set">${f.sets.map((x) => `Full ${esc(DR_BRANDS[x.mk])} fit · ${x.n} pieces`).join(' · ')}</small>` : '<small class="drfit-set">Wear 3 pieces from one maker for a set bonus.</small>'}
    <div class="drfits">${Array.from({ length: DR_FIT_SLOTS }, (_, i) => fits[i] ? `<div class="drfitslot"><button class="drfitbtn" data-dr="fitwear" data-i="${i}"><b>Fit ${i + 1}</b><small>Wear</small></button><button class="drfitre" data-dr="fitsave" data-i="${i}" aria-label="Save what you are wearing as Fit ${i + 1}" title="Save over">↻</button></div>` : `<button class="drfitbtn empty" data-dr="fitsave" data-i="${i}"><b>+</b><small>Save fit</small></button>`).join('')}</div></div>`;
}
function drClosetHTML() {
  const s = DR.st, fresh = new Set(s.newItems || []);
  const cat = DR_CATS.find((c) => c[0] === DR.ccat) || DR_CATS[0], slot = cat[2], cur = drSeasonFor(drMonth()).key;
  const inCat = (x) => slot !== 'outfit' || (DR_OUTFIT_CAT[x.id] || 'formal') === cat[0];
  const items = [...DR_ITEMS.filter((x) => x.slot === slot), ...DR_LG_ITEMS.filter((x) => x.slot === slot && (drOwns(s, slot, x.id) || x.season === cur))].filter(inCat);
  const newIn = (c) => [...DR_ITEMS, ...DR_LG_ITEMS].some((x) => x.slot === c[2] && (c[2] !== 'outfit' || (DR_OUTFIT_CAT[x.id] || 'formal') === c[0]) && fresh.has(drItemKey(x)));
  const bar = `<div class="drcats" role="tablist">${DR_CATS.map((c) => `<button class="drcat" role="tab" data-dr="ccat" data-v="${c[0]}" aria-pressed="${c[0] === cat[0]}">${c[1]}${newIn(c) ? '<i class="drnew">•</i>' : ''}</button>`).join('')}</div>`;
  const grid = items.map((it) => {
    const own = drOwns(s, slot, it.id), on = s.eq[slot] === it.id, k = drItemKey(it), m = drMeta(slot, it.id), none = it.id === 'none';
    const l = own && DR_WEARABLE.includes(slot) && !none ? drWearLv(s, k) : 0, r = (s.wear || {})[k], gl = '';
    const cert = r && r.w ? '<i class="drcert" title="Won in this">🏆</i>' : r && r.f ? '<i class="drcert" title="Final-table worn">◆</i>' : '';
    return `<button class="dritem ${own ? '' : 'locked'} ${on ? 'on' : ''} ${it.league ? 'lg' : ''} ${none ? '' : 'g-' + m.g}" style="--gc:${DR_GRADE[m.g][1]}" data-dr="equip" data-s="${slot}" data-v="${it.id}" ${own ? '' : 'disabled'}>${fresh.has(k) ? '<i class="drnew">New</i>' : ''}<b>${esc(it.name)}</b>${!none ? `<small class="drmk"><span>${DR_GRADE[m.g][0]}</span>${m.brand && m.brand !== DR_BRANDS.league ? ' · ' + esc(m.brand) : ''}</small>` : ''}<small>${own ? (on ? 'Wearing' : 'Tap to wear') : '🔒 ' + esc(it.league ? drLgReq(it) : it.req)}</small>${it.league ? '<i class="drlgtag">League</i>' : gl}${own && l ? drPips(l) : ''}${cert}</button>`;
  }).join('');
  const seen = items.map(drItemKey).filter((k) => fresh.has(k));
  if (seen.length) { s.newItems = (s.newItems || []).filter((k) => !seen.includes(k)); drSave(); }
  const all = [...DR_ITEMS.filter((x) => x.id !== 'none'), ...DR_LG_ITEMS], have = all.filter((x) => drOwns(s, x.slot, x.id)).length;
  return `<div class="drcloset"><div class="drcloset-top"><div class="drcloset-prev hm-${s.eq.home}"><div class="drcloset-av">${dr3LiveOK() ? dr3LiveHTML(s, s.name) : drAvatarSVG(s)}</div><div class="drcloset-side">${drFitsHTML(s)}<button class="drlink drrestyle" data-dr="restyle">Change your look ›</button><div class="drcoll"><small>Collection</small><b class="mono">${have}/${all.length}</b><div class="drbar"><i style="width:${((have / all.length) * 100).toFixed(1)}%"></i></div></div></div></div>${bar}</div><div class="drslot">${drInspectHTML(s, slot, s.eq[slot])}<div class="dritems">${grid}</div></div></div>`;
}
// level-ups shown on the results screen
function drWearUpsHTML(list) {
  if (!list || !list.length) return '';
  return list.map((x) => { const it = [...DR_ITEMS, ...DR_LG_ITEMS].find((i) => i.slot === x.slot && i.id === x.id); return `<div class="drunlock wearup g-${drGradeOf(x.slot, x.id)}" style="--gc:${DR_GRADE[drGradeOf(x.slot, x.id)][1]}">${drPips(x.lv)}<div><b>${esc(it ? it.name : x.id)}: ${DR_WEAR_N[x.lv]}</b><small>${esc(DR_WEAR_GIVES[x.lv])}</small></div></div>`; }).join('');
}

// ---------------------------------------------------------------- face sculpting (creator)
const DR_SCULPT = [
  ['Face', [['fw', 'Face width'], ['jw', 'Jaw'], ['ch', 'Chin'], ['cb', 'Cheekbones'], ['cf', 'Cheeks']]],
  ['Nose', [['nw', 'Width'], ['nl', 'Length'], ['nb', 'Bridge']]],
  ['Mouth', [['lp', 'Lips'], ['mw', 'Width']]],
  ['Eyes', [['es', 'Size'], ['ep', 'Spacing'], ['et', 'Tilt'], ['bh', 'Brow height']]],
  ['Ears', [['er', 'Size']]],
];
function drSculptHTML(look) {
  const sc = look.sc || {}, v = (k) => Math.round((+sc[k] || 0) * 10);
  return `<details class="drsculpt" ${DR.sculptOpen ? 'open' : ''}><summary><span>Sculpt your face</span><small>${Object.values(sc).some((x) => +x) ? 'Customized' : '15 sliders'}</small></summary>
    ${DR_SCULPT.map(([g, list]) => `<div class="drsc-g"><div class="drkicker">${g}</div>${list.map(([k, n]) => `<label class="drsc"><span>${n}</span><input type="range" min="-10" max="10" step="1" value="${v(k)}" data-sc="${k}" aria-label="${g} ${n}"><output>${v(k) > 0 ? '+' : ''}${v(k)}</output></label>`).join('')}</div>`).join('')}
    <div class="drrow">${drBtn('ghost sm', 'data-dr="sculptrnd"', 'Shuffle')}${drBtn('ghost sm', 'data-dr="sculpt0"', 'Reset')}</div></details>`;
}
// sliders reshape the live 3D preview without re-rendering the form (so dragging never stops)
function drSculptBind() {
  const det = document.querySelector('#dream .drsculpt'); if (det) det.ontoggle = () => { DR.sculptOpen = det.open; const sec = det.closest('.drcreate'); if (sec) sec.classList.toggle('sculpting', det.open); };
  let pend = 0, last = 0;
  const redraw = () => { pend = 0; last = performance.now(); if ($('dr3live')) { try { dr3Mount(); } catch (e) { /* keep the old frame */ } } else { const av = $('drCreateAv'), d = DR.draft; if (av) av.innerHTML = drAvatarSVG(d.restyle ? { ...DR.st, look: d.look } : { name: d.name || 'You', look: d.look, eq: drDefault().eq, titles: {}, br: 0 }); } };
  document.querySelectorAll('#dream input[data-sc]').forEach((el) => {
    el.oninput = () => {
      const k = el.dataset.sc, val = +el.value; DR.draft.look.sc = { ...(DR.draft.look.sc || {}), [k]: val / 10 };
      const o = el.parentNode.querySelector('output'); if (o) o.textContent = (val > 0 ? '+' : '') + val;
      if (!pend) pend = setTimeout(redraw, Math.max(0, 140 - (performance.now() - last)));
    };
  });
}
