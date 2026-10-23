// ================================================================ THE DREAM: data
// Story, places, legends (parody names), the event ladder and every unlockable.
// Bankroll is the story's score and decides which buy-ins you can afford. Nothing is bought:
// clothes, jewelry, homes and rides unlock with milestones and titles.

const DR_KEY = 'slickDream';
const DR_HANDS = 3;           // decisions per tournament stage
const DR_CASH_HANDS = 5;      // decisions per cash session

// ---------------------------------------------------------------- places (scene backdrops)
const DR_PLACES = {
  office:   { name: 'Floor 34', sub: 'Halvorsen & Pike Consulting' },
  diner:    { name: 'Lucky Seven Diner', sub: '2:13 AM' },
  cardroom: { name: 'Riverside Card Room', sub: 'Open 24 hours' },
  casino:   { name: 'Silver Strike Casino', sub: 'Circuit stop' },
  vegas:    { name: 'Las Vegas', sub: 'The Summer Series' },
  tv:       { name: 'Feature Table', sub: 'Royal Poker Tour · live on stream' },
  titan:    { name: 'Titan Super High Roller', sub: 'Invitation only' },
  main:     { name: 'The Main Event', sub: 'World Card Series' },
  hall:     { name: 'Hall of Legends', sub: 'Legacy' },
  online:   { name: 'Casual tables', sub: 'Just for fun' },
  cftilt:   { name: 'CF Tilt Poker', sub: 'Online poker room' },
  ranked:   { name: 'Ranked', sub: 'Monthly season' },
  league:   { name: 'League night', sub: 'Public league' },
};

// ---------------------------------------------------------------- people
// Legends are parody names, played as tributes to their public styles. The Ghost is our own
// creation: the GOAT everyone measures themselves against.
const DR_PEOPLE = {
  slick:  { name: 'Slick', role: 'The stranger from the diner', c: '#5aa9de', img: 'slick' },
  derek:  { name: 'Corporate Lou', role: 'Your manager', c: '#9aa6b2', look: { skin: 1, hair: 'slick', hairC: 2, facial: 'none' }, eq: { outfit: 'suit', eyes: 'none', head: 'none', chain: 'none', patch: 'none' }, mouth: 'smug' },
  rosa:   { name: 'Cheryl', role: 'Floor manager, Riverside', c: '#e8704f', look: { skin: 4, hair: 'long', hairC: 0, facial: 'none' }, eq: { outfit: 'polo', eyes: 'readers', head: 'none', chain: 'silver', patch: 'none' } },
  trent:  { name: 'Mustich', role: 'Trust-fund pro. Your rival.', c: '#c084fc', look: { skin: 0, hair: 'slick', hairC: 4, facial: 'none', eyeC: 3 }, eq: { outfit: 'blazer', eyes: 'aviator', head: 'none', chain: 'silver', patch: 'none' }, mouth: 'smug' },
  lou:    { name: 'Tommy "The Nose" Maio', role: 'Riverside regular since 1989', c: '#d9b36c', look: { skin: 2, hair: 'bald', hairC: 6, facial: 'beard' }, eq: { outfit: 'hoodie', eyes: 'none', head: 'cap', chain: 'none', patch: 'none' } },
  host:   { name: 'Miles Gnomey', role: 'Stream host', c: '#f878b8', look: { skin: 3, hair: 'quiff', hairC: 1, facial: 'stubble' }, eq: { outfit: 'designer', eyes: 'none', head: 'none', chain: 'silver', patch: 'none' }, mouth: 'smile' },
  ghost:  { name: 'Bio The Ghost', role: '14 bracelets. Never explains.', c: '#f3ecdc', look: { skin: 5, hair: 'buzz', hairC: 0, facial: 'goatee', eyeC: 1 }, eq: { outfit: 'tux', eyes: 'aviator', head: 'none', chain: 'none', patch: 'none' } },
  mom:    { name: 'David Pec', role: 'Your best friend. Texts in all caps', c: '#5cbf64', look: { skin: 2, hair: 'fade', hairC: 1, facial: 'stubble' }, eq: { outfit: 'hoodie', eyes: 'none', head: 'none', chain: 'none', patch: 'none' }, mouth: 'smile' },
};

// era: old school, boom, online high stakes, high roller/GTO, rising stars
const DR_LEGENDS = [
  { id: 'hungar', name: 'Stu Hungar', nick: 'The Comeback Kid', era: 'Old school', ch: 7, style: 'Reads you before you read your cards. Three-time world champ, a legend for a reason.', line: "Kid, I've seen this hand before. You haven't." },
  { id: 'brunswick', name: 'Colt the Cowboy', nick: 'The One', era: 'Old school', ch: 3, style: 'The godfather. Aggressive before aggressive was cool. Loves ten-deuce.', line: 'Son, I was playing this game before your daddy could shuffle.' },
  { id: 'chance', name: 'Chance Cage', nick: 'The Mirage', era: 'Old school', ch: 3, style: 'Slow-plays monsters and lets you hang yourself.', line: 'Take your time. I always do.' },
  { id: 'armadillo', name: 'Armadillo Fatts', nick: 'The Talker', era: 'Old school', ch: 1, style: 'Spins yarns between hands and steals your blinds during the punchline.', line: "I've got more stories than you've got chips, partner." },
  { id: 'moneybaker', name: 'Chris Schillings', nick: 'The Accountant', era: 'Boom', ch: 2, style: 'Won it all off an $86 satellite. Proof the dream is real.', line: 'I was you once. Spreadsheets and all. Play your game.' },
  { id: 'hellbent', name: 'Phil Hellraiser', nick: 'Whine it In', era: 'Boom', ch: 3, style: 'Most bracelets ever, and he will tell you. Tight, then explodes when he loses.', line: 'If there were no luck in poker, I would win every single one of these.' },
  { id: 'negrono', name: 'GG Negroni', nick: 'Young Poker', era: 'Boom', ch: 3, style: 'Talks through every hand and usually says your cards out loud.', line: "I put you on ace-queen. Am I close? I'm close." },
  { id: 'handsome', name: 'Gus Handsome', nick: 'The Great Dane', era: 'Boom', ch: 4, style: 'Plays any two cards, and makes it work just often enough.', line: 'Seven-four suited. My favourite hand. Today.' },
  { id: 'escandalo', name: 'Antonio Escandalo', nick: 'The Magician', era: 'Boom', ch: 4, style: 'Smooth, fearless, and the chips just seem to disappear.', line: 'Watch closely. Now your stack is gone.' },
  { id: 'mouth', name: 'Mouthy Michael', nick: 'The Yapper', era: 'Boom', ch: 2, style: 'Never stops talking. Never stops jamming.', line: "You call this? You're not calling this. You're NOT calling this." },
  { id: 'fargo', name: 'Sammy Smokes', nick: 'The Unlit Cigarette', era: 'Boom', ch: 2, style: 'Pure feel. Bets like he owns the casino.', line: 'Is a nice hand. Is not my hand, but is nice.' },
  { id: 'sidle', name: 'Erik Sidle', nick: 'The Quiet Assassin', era: 'Boom', ch: 4, style: 'Thirty years at the top without saying thirty words.', line: '…' },
  { id: 'swan', name: 'Tom "hrrrm" Swan', nick: 'The Online Kid', era: 'Online', ch: 4, style: 'Six-figure bluffs with nothing, on purpose.', line: 'I have air. Do you want to find out?' },
  { id: 'antionius', name: 'Patrik Antionius', nick: 'The Finnish Ice', era: 'Online', ch: 5, style: 'Never blinks. Never tilts. Never stops.', line: 'Your move.' },
  { id: 'holdz', name: 'Fedor Holdz', nick: 'Crown Up Guy', era: 'High roller', ch: 5, style: 'Retired at the top, came back, kept winning.', line: 'I studied this exact spot this morning.' },
  { id: 'bonanza', name: 'Justin Bonanza', nick: 'The Prodigy', era: 'High roller', ch: 5, style: 'Biggest single-year score in history. Calm, mathematical, relentless.', line: 'Equilibrium says call. So I call.' },
  { id: 'kenny', name: 'Bryn Kenny-Kash', nick: 'The All-Time Money King', era: 'High roller', ch: 5, style: 'Plays the biggest games on the planet like they are $1/$2.', line: 'Nice stack. I will take it.' },
  { id: 'chillwick', name: 'Stephen Chillwick', nick: 'Mr. Consistency', era: 'High roller', ch: 5, style: 'Final table every week. Never flustered.', line: 'Good luck. Genuinely.' },
  { id: 'kahoon', name: 'Jason Kahoon', nick: 'The Gym Rat', era: 'High roller', ch: 5, style: 'Works out at 5 AM, studies at 6, wins Titans by dinner.', line: 'Discipline beats talent. I have both.' },
  { id: 'loophole', name: 'Linus Loophole', nick: 'The Machine', era: 'High roller', ch: 5, style: 'Plays so close to perfect that the solver checks HIS work.', line: 'Your frequency is off by four percent.' },
  { id: 'hackston', name: 'Isaac Hackston', nick: 'The Professor', era: 'High roller', ch: 5, style: 'Four cards or two, he has solved it.', line: 'Interesting sizing. Incorrect, but interesting.' },
  { id: 'addemup', name: 'Michael Addemup', nick: 'The Titan Slayer', era: 'High roller', ch: 5, style: 'Wins the biggest buy-ins in the world, then disappears.', line: 'Shove.' },
  { id: 'foxglove', name: 'Alex Foxglove', nick: 'The Tour Machine', era: 'High roller', ch: 4, style: 'Top of the leaderboard so often they named the trophy room after him.', line: 'Leaderboard points are points.' },
  { id: 'kfoxglove', name: 'Kristen Foxglove', nick: 'The Queen of the Tour', era: 'High roller', ch: 4, style: 'Multiple player-of-the-year titles. Reads people for a living.', line: "You're nervous. Your left hand gives it away." },
  { id: 'martini', name: 'Artur Martini', nick: 'Player of the Year', era: 'High roller', ch: 5, style: 'Shaken, never stirred. Top of the Titan rankings.', line: 'Another one for the cabinet.' },
  { id: 'lonestar', name: 'Jesse Lonestar', nick: 'Gorilla Mode', era: 'Rising star', ch: 5, style: 'Nine wins in one year. When he goes gorilla mode, run.', line: 'GORILLA. MODE.' },
  { id: 'mocha', name: 'Kayhan Mocha', nick: 'The Norwegian Storm', era: 'Rising star', ch: 5, style: 'Topped the money list with a $7.7 million invitational win.', line: 'Cold out there. Colder in here.' },
  { id: 'daisies', name: 'Seth Daisies', nick: 'The Quarter-Million Man', era: 'Rising star', ch: 5, style: 'First bracelet was a $250K Super High Roller. Started at the top.', line: 'Go big or go home. I went big.' },
  { id: 'bindr', name: 'Bernhard Bindr', nick: 'The Breakout', era: 'Rising star', ch: 5, style: 'Came out of nowhere with a $10 million win. Like you.', line: 'Everyone was a nobody once.' },
  { id: 'punchline', name: 'Punnat Punchline', nick: 'The Bangkok Banker', era: 'Rising star', ch: 5, style: 'Three Titan titles in one year. All business.', line: 'This is my office.' },
  { id: 'glazer', name: 'Nicky Parmesan', nick: 'Blocked', era: 'Rising star', ch: 3, style: 'Three bracelets in one summer. Mixed-game wizard.', line: 'Pick a game. Any game.' },
  { id: 'mizrockie', name: 'Michael Mizrockie', nick: 'The Grinder', era: 'Rising star', ch: 6, style: 'Won the Main Event and the Players Championship in the same summer. Defending champ.', line: "Defending champ. You'll have to take it from me." },
];
const drLegend = (id) => DR_LEGENDS.find((l) => l.id === id);
// every legend gets a fixed look from their name: older eras go gray and suited, newer ones hoodies and headphones
function drLegendLook(L) {
  let h = 2166136261; for (const ch of L.id) { h ^= ch.charCodeAt(0); h = Math.imul(h, 16777619); }
  const r = (n) => { h = Math.imul(h ^ (h >>> 15), 2246822507) >>> 0; return h % n; };
  const old = L.era === 'Old school', young = L.era === 'Rising star' || L.era === 'Online';
  const hair = old ? ['bald', 'short', 'slick', 'buzz'][r(4)] : ['short', 'fade', 'wavy', 'curly', 'slick', 'buzz', 'long', 'bun', 'locs'][r(9)];
  return { look: { skin: r(8), hair, hairC: old ? [6, 7, 0][r(3)] : [0, 1, 2, 3, 4][r(5)], facial: old ? ['mustache', 'beard', 'none', 'goatee'][r(4)] : ['none', 'none', 'stubble', 'goatee', 'beard'][r(5)], eyeC: [0, 0, 1, 2, 3, 4, 5][r(7)] },
    eq: { outfit: old ? ['suit', 'blazer', 'polo'][r(3)] : young ? ['hoodie', 'bomber', 'polo', 'blazer'][r(4)] : ['suit', 'designer', 'blazer', 'bomber'][r(4)],
      eyes: ['none', 'none', 'tinted', 'aviator', 'readers'][r(5)], head: old ? ['none', 'cap'][r(2)] : young ? ['none', 'phones', 'cap', 'beanie'][r(4)] : 'none', chain: ['none', 'none', 'silver', 'cuban'][r(4)], patch: 'none' } };
}

// ---------------------------------------------------------------- chapters
const DR_CHAPTERS = [
  { n: 'Prologue', t: 'The 9-to-5', place: 'office', goal: 'Play the Riverside Local Championship', test: (s) => !!s.story.p_after },
  { n: 'Chapter 1', t: 'The Grind', place: 'cardroom', goal: 'Win the Riverside Classic or build a $10,000 bankroll', test: (s) => s.titles.room > 0 || s.br >= 10000,
    prog: (s) => [Math.min(1, s.br / 10000), `$${drMoney(s.br)} of $10,000`] },
  { n: 'Chapter 2', t: 'The Circuit', place: 'casino', goal: 'Win 2 Circuit rings and build a $40,000 bankroll', test: (s) => s.titles.ring >= 2 && s.br >= 40000,
    prog: (s) => [(Math.min(2, s.titles.ring) / 2 + Math.min(1, s.br / 40000)) / 2, `💍 ${Math.min(2, s.titles.ring)}/2 rings · $${drMoney(s.br)} of $40,000`] },
  { n: 'Chapter 3', t: 'The Summer', place: 'vegas', goal: 'Win a gold bracelet', test: (s) => s.titles.bracelet > 0, prog: (s) => [Math.min(1, s.titles.bracelet), s.titles.bracelet ? 'bracelet won' : 'no bracelet yet'] },
  { n: 'Chapter 4', t: 'The Tour', place: 'tv', goal: 'Win 2 Tour titles (Royal Tour or GLO Cup) and build a $750,000 bankroll', test: (s) => s.titles.tour + s.titles.glo >= 2 && s.br >= 750000,
    prog: (s) => [(Math.min(2, s.titles.tour + s.titles.glo) / 2 + Math.min(1, s.br / 750000)) / 2, `👑 ${Math.min(2, s.titles.tour + s.titles.glo)}/2 titles · $${drMoneyShort(s.br)} of $750K`] },
  { n: 'Chapter 5', t: 'High Roller', place: 'titan', goal: 'Win 2 Titan titles and build a $5,000,000 bankroll', test: (s) => s.titles.titan >= 2 && s.br >= 5e6,
    prog: (s) => [(Math.min(2, s.titles.titan) / 2 + Math.min(1, s.br / 5e6)) / 2, `🔱 ${Math.min(2, s.titles.titan)}/2 Titans · $${drMoneyShort(s.br)} of $5M`] },
  { n: 'Chapter 6', t: 'The Main Event', place: 'main', goal: 'Win the Main Event', test: (s) => s.titles.me > 0, prog: (s) => [s.titles.me ? 1 : 0, s.titles.me ? 'World Champion' : 'not yet'] },
  { n: 'Legacy', t: 'The Hall of Legends', place: 'hall', goal: "Beat every legend and pass Bio The Ghost's 14 bracelets", test: () => false,
    prog: (s) => [(Object.keys(s.legends).length / DR_LEGENDS.length + Math.min(1, s.titles.bracelet / 15)) / 2, `${Object.keys(s.legends).length}/${DR_LEGENDS.length} legends · ${s.titles.bracelet}/15 bracelets`] },
];

// ---------------------------------------------------------------- titles
const DR_TITLES = {
  local: { n: 'Local Champion', icon: '🏅', plural: 'Local titles' },
  room: { n: 'Riverside Classic', icon: '🏆', plural: 'Card-room trophies' },
  ring: { n: 'Circuit ring', icon: '💍', plural: 'Circuit rings' },
  bracelet: { n: 'Gold bracelet', icon: '📿', plural: 'Bracelets' },
  tour: { n: 'Royal Tour title', icon: '👑', plural: 'Tour titles' },
  glo: { n: 'GLO Cup', icon: '🏆', plural: 'GLO Cups' },
  titan: { n: 'Titan trophy', icon: '🔱', plural: 'Titan titles' },
  me: { n: 'Main Event', icon: '🌍', plural: 'Main Events' },
};

// ---------------------------------------------------------------- the event ladder
// kind: mtt (3 stages: early, bubble, final table) or cash (one session).
// legends: who can be waiting at your final table.
const DR_EVENTS = [
  { id: 'local', ch: 0, kind: 'mtt', name: 'Riverside Local Championship', venue: 'cardroom', buyin: 250, field: 118, free: true, title: 'local', boss: 'lou', blurb: 'Your free seat. Your first real tournament.' },
  { id: 'c12', ch: 1, kind: 'cash', name: '$1/$2 No-Limit', venue: 'cardroom', buyin: 200, blurb: 'The bread and butter. 100 big blinds.' },
  { id: 'nightly', ch: 1, kind: 'mtt', name: 'Nightly $60 Turbo', venue: 'cardroom', buyin: 60, field: 42, blurb: 'Fast blinds. Shove-or-fold by midnight.' },
  { id: 'deep', ch: 1, kind: 'mtt', name: 'Sunday $150 Deepstack', venue: 'cardroom', buyin: 150, field: 126, legends: ['armadillo'], blurb: 'The best structure in town.' },
  { id: 'tilt11', ch: 1, kind: 'mtt', online: true, name: 'CF Tilt $11 Nightly Turbo', venue: 'cftilt', buyin: 11, field: 980, blurb: 'Online side event. Everyone antes, nobody waits for the dealer.' },
  { id: 'classic', ch: 1, kind: 'mtt', name: 'Riverside Classic', venue: 'cardroom', buyin: 340, field: 212, title: 'room', boss: 'trent', blurb: 'The big one at Riverside. Mustich wants it too.' },
  { id: 'sat1700', ch: 2, kind: 'sat', name: 'Circuit Main Mega Satellite', venue: 'casino', buyin: 180, field: 164, seats: 16, target: 'ring1700', blurb: 'Win a seat into the $1,700 Circuit Main. The bubble is brutal: everyone left gets the same prize.' },
  { id: 'c25', ch: 2, kind: 'cash', name: '$2/$5 No-Limit', venue: 'casino', buyin: 500, blurb: 'Deeper stacks, bigger egos.' },
  { id: 'ring400', ch: 2, kind: 'mtt', name: 'Circuit $400 No-Limit', venue: 'casino', buyin: 400, field: 384, title: 'ring', legends: ['fargo', 'mouth'], blurb: 'Every Circuit event awards a ring.' },
  { id: 'ring600', ch: 2, kind: 'mtt', name: 'Circuit $600 Deepstack', venue: 'casino', buyin: 600, field: 522, title: 'ring', legends: ['mouth', 'moneybaker'], blurb: 'Deep stacks, deep field.' },
  { id: 'tilt55', ch: 2, kind: 'mtt', online: true, name: 'CF Tilt $55 Sunday Warm-Up', venue: 'cftilt', buyin: 55, field: 1650, blurb: 'Online side event. Grind it from the hotel room between Circuit days.' },
  { id: 'ring1700', ch: 2, kind: 'mtt', name: 'Circuit $1,700 Main Event', venue: 'casino', buyin: 1700, field: 1146, title: 'ring', legends: ['moneybaker', 'fargo', 'mouth', 'trent'], blurb: 'The ring everyone wants on this stop. Mustich is registered.' },
  { id: 'sat10k', ch: 3, kind: 'sat', name: '$10K Championship Satellite', venue: 'vegas', buyin: 1100, field: 212, seats: 21, target: 'b10k', blurb: 'One in ten wins a seat into the pros-only bracelet event.' },
  { id: 'c510', ch: 3, kind: 'cash', name: '$5/$10 No-Limit', venue: 'vegas', buyin: 1500, blurb: 'Summer in Vegas. Every table has a story.' },
  { id: 'b1500', ch: 3, kind: 'mtt', name: 'WCS $1,500 No-Limit', venue: 'vegas', buyin: 1500, field: 2410, title: 'bracelet', legends: ['negrono', 'glazer', 'chance'], blurb: 'Gold bracelet event. Huge field.' },
  { id: 'b3000', ch: 3, kind: 'mtt', name: 'WCS $3,000 Six-Max', venue: 'vegas', buyin: 3000, field: 1104, title: 'bracelet', legends: ['hellbent', 'negrono', 'glazer'], blurb: 'Gold bracelet event. Six-handed aggression.' },
  { id: 'tilt215', ch: 3, kind: 'mtt', online: true, name: 'CF Tilt $215 Sunday Major', venue: 'cftilt', buyin: 215, field: 2300, blurb: 'Online side event. The biggest Sunday on CF Tilt Poker.' },
  { id: 'b10k', ch: 3, kind: 'mtt', name: 'WCS $10,000 Championship', venue: 'vegas', buyin: 10000, field: 418, title: 'bracelet', legends: ['brunswick', 'hellbent', 'chance'], blurb: 'The pros-only bracelet. Legends everywhere.' },
  { id: 'c1025', ch: 4, kind: 'cash', name: '$10/$25 Feature Game', venue: 'tv', buyin: 5000, blurb: 'On stream, hole cards shown. No hiding.' },
  { id: 'rpt3500', ch: 4, kind: 'mtt', name: 'Royal Tour $3,500 Main', venue: 'tv', buyin: 3500, field: 862, title: 'tour', legends: ['handsome', 'escandalo', 'kfoxglove'], blurb: 'Tour title and a seat in the Champions Cup.' },
  { id: 'glo10k', ch: 4, kind: 'mtt', name: 'GLO Cup $10,000', venue: 'tv', buyin: 10000, field: 96, title: 'glo', legends: ['foxglove', 'kfoxglove', 'swan'], blurb: 'Small field. Every player is a killer.' },
  { id: 'tilt530', ch: 4, kind: 'mtt', online: true, name: 'CF Tilt $530 Super Tuesday', venue: 'cftilt', buyin: 530, field: 640, blurb: 'Online side event. Tour pros grind it from their phones.' },
  { id: 'rpt10k', ch: 4, kind: 'mtt', name: 'Royal Tour $10,000 Championship', venue: 'tv', buyin: 10000, field: 421, title: 'tour', legends: ['sidle', 'swan', 'escandalo', 'mizrockie'], blurb: 'The crown jewel of the Tour.' },
  { id: 'sat25k', ch: 5, kind: 'sat', name: 'Titan $25K Satellite', venue: 'titan', buyin: 2700, field: 104, seats: 10, target: 't25k', blurb: 'Ten seats into the Titan $25,000. Ninety-four people go home with nothing.' },
  { id: 'private', ch: 5, kind: 'cash', name: '$100/$200 Private Game', venue: 'titan', buyin: 40000, blurb: 'Penthouse game. Invitation from Mustich, of all people.' },
  { id: 't25k', ch: 5, kind: 'mtt', name: 'Titan $25,000', venue: 'titan', buyin: 25000, field: 142, title: 'titan', legends: ['chillwick', 'kahoon', 'lonestar', 'bindr'], blurb: 'The entry point to the biggest games on earth.' },
  { id: 't100k', ch: 5, kind: 'mtt', name: 'Titan $100,000', venue: 'titan', buyin: 100000, field: 68, title: 'titan', legends: ['holdz', 'bonanza', 'loophole', 'martini', 'punchline', 'mocha', 'trent'], blurb: 'Sixty-eight of the best players alive. And Mustich.' },
  { id: 'tilt2100', ch: 5, kind: 'mtt', online: true, name: 'CF Tilt $2,100 High Roller', venue: 'cftilt', buyin: 2100, field: 210, blurb: 'Online side event. Half the Titan field logs in for this one.' },
  { id: 't250k', ch: 5, kind: 'mtt', name: 'Titan $250,000 Invitational', venue: 'titan', buyin: 250000, field: 40, title: 'titan', legends: ['addemup', 'kenny', 'hackston', 'antionius', 'daisies'], blurb: 'The richest table in the world.' },
  { id: 'satmain', ch: 6, kind: 'sat', name: 'Main Event Mega Satellite', venue: 'main', buyin: 1100, field: 704, seats: 70, target: 'main', blurb: "The accountant's route: a cheap seat into the biggest tournament on earth." },
  { id: 'main', ch: 6, kind: 'mtt', name: 'WCS $10,000 Main Event', venue: 'main', buyin: 10000, field: 8402, title: 'me', boss: 'ghost', legends: ['mizrockie'], blurb: 'Eight thousand dreamers. One world champion.' },
  { id: 'legends', ch: 7, kind: 'mtt', name: 'Legends Invitational', venue: 'hall', buyin: 50000, field: 33, title: 'bracelet', legends: ['hungar', 'brunswick', 'chance', 'hellbent', 'mizrockie'], blurb: 'Every era. One table. A bracelet for the winner.' },
];
// the Daily Grind: one free sponsored freeroll a day, online on CF Tilt Poker
const DR_DAILY = { id: 'daily', ch: 1, kind: 'mtt', online: true, venue: 'cftilt', name: 'The Daily Grind', buyin: 0, field: 60, free: true, daily: true, blurb: 'One free sponsored freeroll a day. Keep the streak alive for exclusive gear.' };
const drEvent = (id) => (id === 'daily' ? DR_DAILY : DR_EVENTS.find((e) => e.id === id));

// ---------------------------------------------------------------- unlockables
// slot: what it changes. Each unlocks with a milestone: never bought.
const DR_ITEMS = [
  // outfits
  { slot: 'outfit', id: 'office', name: 'Clearance-rack office fit', req: 'Where it all started', test: () => true },
  { slot: 'outfit', id: 'tee', name: 'Plain black tee', req: 'Where it all started', test: () => true },
  { slot: 'outfit', id: 'cardigan', name: 'Study-group cardigan', req: 'Play 3 sessions', test: (s) => s.played >= 3 },
  { slot: 'outfit', id: 'jersey', name: 'Lucky number 7 jersey', req: 'Make 100 decisions', test: (s) => s.dec >= 100 },
  { slot: 'outfit', id: 'denim', name: 'Denim trucker jacket', req: 'Win a session at $1/$2', test: (s) => !!s.flags.cashWin },
  { slot: 'outfit', id: 'track', name: 'Retro track jacket', req: 'Daily Grind streak of 3 days', test: (s) => (s.bestStreak || 0) >= 3 },
  { slot: 'outfit', id: 'vest', name: 'Argyle sweater vest', req: 'Peak bankroll of $10,000', test: (s) => s.peak >= 10000 },
  { slot: 'outfit', id: 'varsity', name: 'Varsity letterman', req: 'Cash in a Circuit event', test: (s) => !!s.flags.circuitCash },
  { slot: 'outfit', id: 'leather', name: 'Worn leather jacket', req: 'Peak bankroll of $50,000', test: (s) => s.peak >= 50000 },
  { slot: 'outfit', id: 'fleece', name: 'Trail fleece', req: 'Reach the Summer', test: (s) => s.ch >= 3 },
  { slot: 'outfit', id: 'hoodie', name: 'The lucky hoodie', req: 'Play your first tournament', test: (s) => s.played >= 1 },
  { slot: 'outfit', id: 'polo', name: 'Card-room quarter-zip', req: 'Reach Chapter 1', test: (s) => s.ch >= 1 },
  { slot: 'outfit', id: 'bomber', name: 'Black bomber jacket', req: 'Win any tournament', test: (s) => drTitleCount(s) >= 1 },
  { slot: 'outfit', id: 'blazer', name: 'Circuit blazer', req: 'Reach the Circuit', test: (s) => s.ch >= 2 },
  { slot: 'outfit', id: 'suit', name: 'Tailored suit', req: 'Win a gold bracelet', test: (s) => s.titles.bracelet >= 1 },
  { slot: 'outfit', id: 'designer', name: 'Designer suit', req: 'Peak bankroll of $1,000,000', test: (s) => s.peak >= 1e6 },
  { slot: 'outfit', id: 'tux', name: 'Main Event tuxedo', req: 'Make the Main Event final table', test: (s) => !!s.flags.meft },
  // eyewear
  { slot: 'eyes', id: 'none', name: 'No eyewear', req: '', test: () => true },
  { slot: 'eyes', id: 'readers', name: 'Blue-light readers', req: 'Where it all started', test: () => true },
  { slot: 'eyes', id: 'round', name: 'Wire round frames', req: 'Make 50 decisions', test: (s) => s.dec >= 50 },
  { slot: 'eyes', id: 'thick', name: 'Thick black frames', req: 'Play 2 sessions', test: (s) => s.played >= 2 },
  { slot: 'eyes', id: 'sport', name: 'Wraparound sport shades', req: 'Peak bankroll of $25,000', test: (s) => s.peak >= 25000 },
  { slot: 'eyes', id: 'tinted', name: 'Tinted table glasses', req: 'Cash in a Circuit event', test: (s) => !!s.flags.circuitCash },
  { slot: 'eyes', id: 'aviator', name: 'Gold aviators', req: 'Win a Circuit ring', test: (s) => s.titles.ring >= 1 },
  { slot: 'eyes', id: 'shades', name: "Slick's 8-bit shades", req: 'Win any title', test: (s) => drTitleCount(s) >= 1 },
  // head
  { slot: 'head', id: 'none', name: 'Nothing', req: '', test: () => true },
  { slot: 'head', id: 'cap', name: 'Backwards ballcap', req: 'Play 5 sessions', test: (s) => s.played >= 5 },
  { slot: 'head', id: 'snap', name: 'Flat-brim snapback', req: 'Reach Chapter 1', test: (s) => s.ch >= 1 },
  { slot: 'head', id: 'bucket', name: 'Cream bucket hat', req: 'Play 10 sessions', test: (s) => s.played >= 10 },
  { slot: 'head', id: 'cowboy', name: 'Texas hat', req: 'Win a Circuit ring', test: (s) => s.titles.ring >= 1 },
  { slot: 'head', id: 'fedora', name: 'High-roller fedora', req: 'Peak bankroll of $500,000', test: (s) => s.peak >= 500000 },
  { slot: 'head', id: 'phones', name: 'Noise-cancelling cans', req: 'Make 100 decisions', test: (s) => s.dec >= 100 },
  { slot: 'head', id: 'beanie', name: 'Vegas pool beanie', req: 'Reach the Summer', test: (s) => s.ch >= 3 },
  { slot: 'head', id: 'visor', name: "Old-school dealer's visor", req: 'Daily Grind streak of 7 days', test: (s) => (s.bestStreak || 0) >= 7 },
  // earrings
  { slot: 'ear', id: 'none', name: 'None', req: '', test: () => true },
  { slot: 'ear', id: 'stud', name: 'Silver studs', req: 'Peak bankroll of $2,500', test: (s) => s.peak >= 2500 },
  { slot: 'ear', id: 'hoop', name: 'Gold hoops', req: 'Peak bankroll of $15,000', test: (s) => s.peak >= 15000 },
  { slot: 'ear', id: 'diamond', name: 'Diamond studs', req: 'Peak bankroll of $75,000', test: (s) => s.peak >= 75000 },
  // rings
  { slot: 'ring', id: 'none', name: 'None', req: '', test: () => true },
  { slot: 'ring', id: 'signet', name: 'Silver signet', req: 'Peak bankroll of $20,000', test: (s) => s.peak >= 20000 },
  { slot: 'ring', id: 'gold', name: 'Gold band', req: 'Win a gold bracelet', test: (s) => s.titles.bracelet >= 1 },
  { slot: 'ring', id: 'pinky', name: 'Diamond pinky ring', req: 'Peak bankroll of $500,000', test: (s) => s.peak >= 500000 },
  { slot: 'ring', id: 'champ', name: 'Champion ring', req: 'Win the Main Event', test: (s) => s.titles.me >= 1 },
  // bracelets
  { slot: 'wrist', id: 'none', name: 'None', req: '', test: () => true },
  { slot: 'wrist', id: 'beads', name: 'Bead bracelet', req: 'Play 4 sessions', test: (s) => s.played >= 4 },
  { slot: 'wrist', id: 'gold', name: 'Gold cuff', req: 'Peak bankroll of $40,000', test: (s) => s.peak >= 40000 },
  { slot: 'wrist', id: 'tennis', name: 'Diamond tennis bracelet', req: 'Peak bankroll of $400,000', test: (s) => s.peak >= 400000 },
  // sleeve patches
  { slot: 'sleeve', id: 'none', name: 'Bare sleeve', req: '', test: () => true },
  { slot: 'sleeve', id: 'grind', name: 'Daily Grind sleeve', req: 'Daily Grind streak of 5 days', test: (s) => (s.bestStreak || 0) >= 5 },
  { slot: 'sleeve', id: 'site', name: 'CF Tilt Poker sleeve', req: 'Cash in a Circuit event', test: (s) => !!s.flags.circuitCash },
  { slot: 'sleeve', id: 'energy', name: 'Energy drink sleeve', req: 'Win a gold bracelet', test: (s) => s.titles.bracelet >= 1 },
  { slot: 'sleeve', id: 'global', name: 'Global ambassador sleeve', req: 'Win the Main Event', test: (s) => s.titles.me >= 1 },
  // watches
  { slot: 'watch', id: 'digital', name: 'Gas-station digital', req: 'Where it all started', test: () => true },
  { slot: 'watch', id: 'steel', name: 'Steel diver', req: 'Peak bankroll of $25,000', test: (s) => s.peak >= 25000 },
  { slot: 'watch', id: 'gold', name: 'Gold president', req: 'Peak bankroll of $250,000', test: (s) => s.peak >= 250000 },
  { slot: 'watch', id: 'iced', name: 'Iced-out chrono', req: 'Peak bankroll of $2,000,000', test: (s) => s.peak >= 2e6 },
  { slot: 'watch', id: 'hyper', name: 'Hypercar tourbillon', req: 'Win the Main Event', test: (s) => s.titles.me >= 1 },
  // chains
  { slot: 'chain', id: 'none', name: 'No chain', req: '', test: () => true },
  { slot: 'chain', id: 'silver', name: 'Thin silver chain', req: 'Peak bankroll of $10,000', test: (s) => s.peak >= 10000 },
  { slot: 'chain', id: 'cuban', name: 'Gold Cuban link', req: 'Peak bankroll of $100,000', test: (s) => s.peak >= 100000 },
  { slot: 'chain', id: 'rope', name: 'Rope chain & pendant', req: 'Peak bankroll of $250,000', test: (s) => s.peak >= 250000 },
  { slot: 'chain', id: 'layered', name: 'Layered chains', req: 'Win a gold bracelet', test: (s) => s.titles.bracelet >= 1 },
  { slot: 'chain', id: 'iced', name: 'Iced Cuban link', req: 'Peak bankroll of $1,000,000', test: (s) => s.peak >= 1e6 },
  // homes
  { slot: 'home', id: 'studio', name: 'Studio over the laundromat', req: 'Where it all started', test: () => true },
  { slot: 'home', id: 'apt', name: 'Downtown one-bedroom', req: 'Peak bankroll of $25,000', test: (s) => s.peak >= 25000 },
  { slot: 'home', id: 'condo', name: 'High-rise condo', req: 'Peak bankroll of $150,000', test: (s) => s.peak >= 150000 },
  { slot: 'home', id: 'penthouse', name: 'Strip penthouse', req: 'Peak bankroll of $1,000,000', test: (s) => s.peak >= 1e6 },
  { slot: 'home', id: 'mansion', name: 'Hills mansion', req: 'Peak bankroll of $5,000,000', test: (s) => s.peak >= 5e6 },
  { slot: 'home', id: 'island', name: 'Private island villa', req: 'Win the Main Event', test: (s) => s.titles.me >= 1 },
  // rides
  { slot: 'ride', id: 'bus', name: 'Monthly bus pass', req: 'Where it all started', test: () => true },
  { slot: 'ride', id: 'hatch', name: "'09 hatchback", req: 'Peak bankroll of $5,000', test: (s) => s.peak >= 5000 },
  { slot: 'ride', id: 'coupe', name: 'Used sports coupe', req: 'Peak bankroll of $50,000', test: (s) => s.peak >= 50000 },
  { slot: 'ride', id: 'sedan', name: 'German super-sedan', req: 'Peak bankroll of $250,000', test: (s) => s.peak >= 250000 },
  { slot: 'ride', id: 'super', name: 'Italian supercar', req: 'Peak bankroll of $2,000,000', test: (s) => s.peak >= 2e6 },
  { slot: 'ride', id: 'jet', name: 'Private jet share', req: 'Win a Titan title', test: (s) => s.titles.titan >= 1 },
  // sponsor patches
  { slot: 'patch', id: 'none', name: 'Unsponsored', req: 'Where it all started', test: () => true },
  { slot: 'patch', id: 'grind', name: 'Daily Grind patch', req: 'Daily Grind streak of 3 days', test: (s) => (s.bestStreak || 0) >= 3 },
  { slot: 'patch', id: 'tavern', name: "Rusty's Tavern", req: 'Win a session at $1/$2', test: (s) => !!s.flags.cashWin },
  { slot: 'patch', id: 'site', name: 'CF Tilt Poker patch', req: 'Win a Circuit ring', test: (s) => s.titles.ring >= 1 },
  { slot: 'patch', id: 'energy', name: 'Energy drink sponsor', req: 'Win a gold bracelet', test: (s) => s.titles.bracelet >= 1 },
  { slot: 'patch', id: 'global', name: 'Global brand ambassador', req: 'Win the Main Event', test: (s) => s.titles.me >= 1 },
];
const DR_SLOTS = [['outfit', 'Outfit'], ['eyes', 'Eyewear'], ['head', 'Headwear'], ['ear', 'Earrings'], ['chain', 'Chain'], ['ring', 'Ring'], ['wrist', 'Bracelet'], ['watch', 'Watch'], ['patch', 'Chest patch'], ['sleeve', 'Sleeve patch'], ['home', 'Home'], ['ride', 'Ride']];
// the closet's category bar: picking one shows its items (outfits split by what they are)
const DR_OUTFIT_CAT = { office: 'shirts', tee: 'shirts', polo: 'shirts', flannel: 'shirts', hoodie: 'street', jersey: 'street', track: 'street', varsity: 'street', denim: 'street', fleece: 'street', ugly: 'street', bomber: 'jackets', leather: 'jackets', cardigan: 'jackets', blazer: 'jackets', vest: 'formal', suit: 'formal', designer: 'formal', tux: 'formal', vampire: 'formal' };
const DR_CATS = [['shirts', 'Shirts', 'outfit'], ['street', 'Streetwear', 'outfit'], ['jackets', 'Jackets', 'outfit'], ['formal', 'Formal', 'outfit'], ['head', 'Headwear', 'head'], ['eyes', 'Eyewear', 'eyes'], ['chain', 'Chains', 'chain'], ['ear', 'Earrings', 'ear'], ['ring', 'Rings', 'ring'], ['wrist', 'Bracelets', 'wrist'], ['watch', 'Watches', 'watch'], ['patch', 'Chest patch', 'patch'], ['sleeve', 'Sleeve patch', 'sleeve'], ['home', 'Home', 'home'], ['ride', 'Ride', 'ride']];
const DR_RIDE_ICON = { bus: '🚌', hatch: '🚗', coupe: '🏎️', sedan: '🚘', super: '🏎️', jet: '🛩️' };
const DR_HOME_ICON = { studio: '🧺', apt: '🏢', condo: '🌆', penthouse: '🌃', mansion: '🏛️', island: '🏝️' };
const DR_PATCH = { none: null, grind: { t: 'GRIND', c: '#5cbf64' }, tavern: { t: "RUSTY'S", c: '#b8432a' }, site: { t: 'CF TILT', c: '#1f5ae8' }, energy: { t: 'VOLT', c: '#b8f018' }, global: { t: 'APEX', c: '#f3ecdc' },
  boo: { t: 'BOO!', c: '#ff7a1a' }, harvest: { t: 'HARVEST', c: '#d9893a' }, frost: { t: 'FROST', c: '#7fd0ff' } };

// the line under your name, earned as you climb
const DR_REPUTATION = [
  [(s) => s.titles.me > 0 && s.titles.bracelet >= 15, 'The New GOAT'], [(s) => s.titles.me > 0, 'World Champion'],
  [(s) => s.titles.titan > 0, 'Titan'], [(s) => s.titles.tour + s.titles.glo > 0, 'Tour Champion'],
  [(s) => s.titles.bracelet > 0, 'Bracelet Winner'], [(s) => s.titles.ring > 0, 'Circuit Ring Winner'],
  [(s) => s.ch >= 2, 'Circuit Grinder'], [(s) => s.titles.room > 0, 'Riverside Champion'],
  [(s) => s.ch >= 1, (s) => (s.job === 'quit' ? 'Full-time Dreamer' : 'Moonlighter')], [() => true, 'Cubicle Dreamer'],
];

// ---------------------------------------------------------------- coach lines between stages
const DR_TIPS = {
  satbubble: [
    'Satellite bubble: once you have a seat locked up, folding aces can be right. Survival is the only prize.',
    "Short stacks have to gamble. Big stacks don't. Know which one you are.",
  ],
  middle: [
    "The short stacks are getting desperate. Let them shove into your good hands.",
    "Stack sizes changed. Your opening ranges should too.",
    "This is where tournaments are won quietly. Steal, don't spew.",
  ],
  deep: [
    "You're in the money. Ladder up, but don't play scared. Scared money never wins.",
    "Every pay jump matters now. Pick on the stacks that are clinging on.",
  ],
  early: [
    "Early levels: chips are cheap, mistakes aren't. Fold the junk and let them spew.",
    "Position is power. The button prints money; under the gun is a tax.",
    "You don't have to win a hand to win this level. Survive and steal.",
  ],
  bubble: [
    "Bubble. The short stacks are terrified. The big stacks are licking their lips. Which one are you?",
    "Near the money, a call costs more than chips. Tighten your calls, not your raises.",
    'Min-cash is not the goal. But busting on the bubble is a long ride home.',
  ],
  final: [
    "Final table. Every pay jump is real money. Every chip is a decision.",
    'Short stack? Your jam is your weapon. Raise-fold gives it away.',
    "Look around. Somebody here is scared. Make sure it isn't you.",
  ],
  cash: [
    "Cash game: no bubble, no pay jumps. Just chip EV. Play the math.",
    'Steal the blinds you can. Defend the ones you should.',
  ],
};
