// ================================================================ THE DREAM: the story
// A scene is a list of beats. sp: who talks ('n' = narrator, 'you', a DR_PEOPLE id, or 'L:<legend id>').
// t: the line (a string or a function of state). choice: buttons; each can run act(s) and jump to a scene.
// end: what happens when the scene finishes ('hub', or an event id to play next).

const DR_SCENES = {
  // ------------------------------------------------------------ prologue
  p_office: { place: 'office', title: 'Prologue', sub: 'The 9-to-5', beats: [
    { sp: 'n', t: 'Monday. 6:47 PM. Floor 34.\nEveryone else went home an hour ago.' },
    { sp: 'derek', t: 'The Henderson deck. Redo it. All of it. On my desk by eight.' },
    { sp: 'you', t: 'I sent it Friday. You signed off on it.' },
    { sp: 'derek', t: "And now I'm un-signing off on it. Problem?" },
    { sp: 'you', t: 'No problem, Derek.', mood: 'quiet' },
    { sp: 'derek', t: 'Great. Oh, and I told the partners the new model was my idea. Team player, right?' },
    { sp: 'n', t: 'You were nine the first time you saw it on TV.\nA final table under hot lights. A man in dark glasses pushed every chip he had into the middle with nothing.' },
    { sp: 'n', t: 'The room went silent. His opponent folded. He never showed.\nHe made it look like he could see the future.' },
    { sp: 'you', t: 'Somewhere along the way I traded that for a lanyard.', mood: 'think' },
  ], end: 'p_diner' },

  p_diner: { place: 'diner', title: 'Prologue', sub: 'Lucky Seven Diner', beats: [
    { sp: 'n', t: '2:13 AM. The deck is done.\nYou are not.' },
    { sp: 'slick', t: "You're doing the thing where you stare at the pie like it owes you money.", who: 'Stranger' },
    { sp: 'you', t: 'Long day.' },
    { sp: 'slick', t: 'Long life, by the look of it. What do you do?', who: 'Stranger' },
    { sp: 'you', t: 'Spreadsheets. For a guy who takes credit for them.' },
    { sp: 'slick', t: 'And what did you want to do?', who: 'Stranger', choice: [
      { t: 'Play poker. For real.', act: (s) => { s.flags.honest = true; }, line: { sp: 'slick', who: 'Stranger', t: 'Ha. Everybody says that at 2 AM. Almost nobody means it.' } },
      { t: "Doesn't matter now.", act: (s) => { s.flags.honest = false; }, line: { sp: 'slick', who: 'Stranger', t: "It always matters. You just stopped saying it out loud." } },
    ] },
    { sp: 'n', t: 'He slides a red envelope across the counter.' },
    { sp: 'slick', t: "Riverside Local Championship. Saturday, noon. Two-fifty seat. It's paid.", who: 'Stranger' },
    { sp: 'you', t: "Why would you give that to a stranger?" },
    { sp: 'slick', t: 'Because I sat on that stool once. Somebody bought my pie too.', who: 'Stranger' },
    { sp: 'slick', t: "Three things. Fold the junk. Pressure the scared ones. And near the money, think about what the money's worth, not the chips.", who: 'Stranger' },
    { sp: 'you', t: 'Who are you?' },
    { sp: 'slick', t: 'People call me Slick.' },
    { sp: 'n', t: 'He drops cash for both checks and is gone before the bell over the door stops ringing.\nInside the envelope: a seat card, and one line in pen.' },
    { sp: 'n', t: '“People insist on calling it luck…”', big: true },
  ], end: 'local' },

  p_after: { place: 'office', title: 'Prologue', sub: 'Monday again', beats: [
    { sp: 'derek', t: 'You look different. Did you finish the Henderson numbers?' },
    { sp: 'n', t: (s) => s.events.local && s.events.local.best <= drPaid(drEvent('local'))
      ? `You finished ${drOrd(s.events.local.best)} on Saturday and took home $${drMoney(s.events.local.cash)}.\nYou've replayed every hand in your head since.`
      : `You busted Saturday. And you have never felt more alive.\nYou've replayed every hand in your head since.` },
    { sp: 'derek', t: "Hello? I'm talking to you. Weekend's over, champ." },
    { sp: 'n', t: 'Decision time.', choice: [
      { t: 'Quit. Today.', act: (s) => { s.job = 'quit'; drAdd(s, 1200, 'Savings, after quitting'); }, go: 'p_quit' },
      { t: 'Keep the job. Grind nights.', act: (s) => { s.job = 'nights'; drAdd(s, 2000, 'Savings, keeping the day job'); }, go: 'p_keep' },
    ] },
  ], end: 'hub' },
  p_quit: { place: 'office', beats: [
    { sp: 'you', t: 'Derek? Redo it yourself.' },
    { sp: 'n', t: 'The elevator ride down is the best thirty-four floors of your life.\nYou have $1,200 in savings and absolutely no plan B.' },
    { sp: 'slick', t: 'Heard about Saturday. Riverside runs games every night. Start small. Never play above your roll. Text me when you win something.', text: true },
    { sp: 'mom', t: 'YOU QUIT YOUR JOB?? CALL ME', text: true },
  ], end: 'c1' },
  p_keep: { place: 'office', beats: [
    { sp: 'n', t: 'You keep the lanyard.\nBut tonight you open a new spreadsheet and name it BANKROLL.' },
    { sp: 'n', t: '$2,000 in savings. Days for Derek. Nights for the dream.' },
    { sp: 'slick', t: 'Heard about Saturday. Riverside runs games every night. Start small. Never play above your roll. Text me when you win something.', text: true },
  ], end: 'c1' },

  // ------------------------------------------------------------ chapter openers
  c1: { place: 'cardroom', title: 'Chapter 1', sub: 'The Grind', beats: [
    { sp: 'n', t: 'Riverside Card Room. Carpet from 1994. Coffee from this morning, maybe.\nTwenty tables, every one of them a classroom.' },
    { sp: 'rosa', t: "New face. Sign the list. One-two has a seat in ten. Nightly turbo kicks off at seven." },
    { sp: 'lou', t: "Hey, you're the kid from Saturday. Don't get cocky. This room eats dreamers for breakfast." },
    { sp: 'trent', t: "Cute hoodie. Did it come with the free seat?" },
    { sp: 'n', t: "Trent Vale. Dad's money, a solver subscription, and a smile you want to wipe off.\nHe's been circling the Riverside Classic all year." },
    { sp: 'n', t: 'Goal: win the Riverside Classic, or build your bankroll to $10,000.', goal: true },
  ], end: 'hub' },
  c2: { place: 'casino', title: 'Chapter 2', sub: 'The Circuit', beats: [
    { sp: 'slick', t: "You've outgrown Riverside. The Circuit stops at Silver Strike this month. Every event gives a ring. Go get one." },
    { sp: 'n', t: 'Bigger rooms. Better players. A guy at the rail is wearing four rings on one hand.' },
    { sp: 'L:moneybaker', t: "You're the kid who quit the cubicle? I was an accountant. Spreadsheets and everything. Play your game and let them find out." },
    { sp: 'trent', t: "Look who followed me to the Circuit. Try not to bust on the bubble this time." },
    { sp: 'n', t: 'Goal: win two Circuit rings and build a $40,000 bankroll.', goal: true },
  ], end: 'hub' },
  c3: { place: 'vegas', title: 'Chapter 3', sub: 'The Summer', beats: [
    { sp: 'n', t: "Las Vegas in June. 112 degrees outside, 68 inside, and ten thousand people chasing the same gold." },
    { sp: 'slick', t: 'Every kid who ever loved this game dreams about a bracelet. This is the room where they hand them out.' },
    { sp: 'n', t: "At the feature table sits a man in dark glasses. Fourteen bracelets. Nobody has seen him smile.\nThe man from TV. The reason you're here." },
    { sp: 'ghost', t: 'Kid. Stop looking at me and look at your cards.' },
    { sp: 'L:hellbent', t: "Another internet kid. I'll see you at the final table. Probably not, though." },
    { sp: 'n', t: 'Goal: win a gold bracelet.', goal: true },
  ], end: 'hub' },
  c4: { place: 'tv', title: 'Chapter 4', sub: 'The Tour', beats: [
    { sp: 'host', t: "Welcome back to the feature table! Joining us: the cubicle kid with a bracelet, and people are calling it a fluke." },
    { sp: 'you', t: 'People insist on calling it luck.' },
    { sp: 'host', t: "Ooh. We have a quote. Producers, put that on a T-shirt." },
    { sp: 'L:kfoxglove', t: "Cameras make people do stupid things. Don't be people." },
    { sp: 'n', t: 'Goal: win two Tour titles (Royal Tour or GLO Cup) and build a $750,000 bankroll.', goal: true },
  ], end: 'hub' },
  c5: { place: 'titan', title: 'Chapter 5', sub: 'High Roller', beats: [
    { sp: 'n', t: 'A private terminal. A plane with your name on the manifest. A buy-in that used to be a house.' },
    { sp: 'trent', t: "Funny. Dad stopped staking me last month. Guess we're both self-made now." },
    { sp: 'L:loophole', t: 'I reviewed your last final table. Your bubble frequencies are… acceptable.' },
    { sp: 'slick', t: "In here nobody's scared. Everyone knows the math. So the edge is in the spots they don't study. Find them." },
    { sp: 'n', t: 'Goal: win two Titan titles and build a $5,000,000 bankroll.', goal: true },
  ], end: 'hub' },
  c6: { place: 'main', title: 'Chapter 6', sub: 'The Main Event', beats: [
    { sp: 'n', t: 'Eight thousand four hundred and two players. Ten thousand dollars each.\nOne of them will be on the cover of every poker magazine in the world.' },
    { sp: 'L:mizrockie', t: "Defending champ. If you want it, you'll have to take it from me." },
    { sp: 'mom', t: 'IS THIS THE ONE ON TV?? SIT UP STRAIGHT', text: true },
    { sp: 'slick', t: "Nine-year-old you watched this on a fuzzy TV. Go show him how it ends." },
    { sp: 'n', t: 'Goal: win the Main Event.', goal: true },
  ], end: 'hub' },
  c7: { place: 'hall', title: 'Legacy', sub: 'The Hall of Legends', beats: [
    { sp: 'n', t: "Your picture hangs in the Hall of Legends now. Next to men you used to watch on a fuzzy TV." },
    { sp: 'ghost', t: "Fourteen bracelets. That's the number. Come take it." },
    { sp: 'n', t: "Goal: beat every legend and pass The Ghost's fourteen bracelets.", goal: true },
  ], end: 'hub' },

  // ------------------------------------------------------------ beats along the way
  broke: { place: 'diner', title: 'Rock bottom', beats: [
    { sp: 'n', t: 'Lucky Seven Diner. 2:13 AM. Same stool. Same pie. Empty wallet.' },
    { sp: 'slick', t: 'Busted?' },
    { sp: 'you', t: "Every last dollar." },
    { sp: 'slick', t: "Good. Now you're a poker player. Everybody goes broke once. The good ones learn why." },
    { sp: 'slick', t: (s) => `Here's $${drMoney(drStakeSize(s))}. Not a gift. Half your profits come back to me until it's paid. And this time, respect your roll.` },
  ], end: 'hub' },
  firstwin: { place: 'diner', title: 'First title', beats: [
    { sp: 'slick', t: "Saw the photo. Trophy, big grin, terrible lighting. Proud of you, kid." },
    { sp: 'slick', t: "Left something in your locker. Every champion needs a pair." },
    { sp: 'n', t: "Inside: a pair of pixelated sunglasses and a note.\n“Deal with it.”" },
  ], end: 'hub' },
  champ: { place: 'main', title: 'World Champion', beats: [
    { sp: 'n', t: 'Confetti. Cameras. A bracelet the size of a doorknob.\nSomewhere, a nine-year-old with a fuzzy TV is losing his mind.' },
    { sp: 'ghost', t: 'Good game, kid.' },
    { sp: 'n', t: 'He takes off his glasses. For the first time anyone can remember, The Ghost smiles.' },
    { sp: 'ghost', t: "Now you'll find out the hard part. Everyone will be hunting you." },
    { sp: 'derek', t: 'Hey buddy! Long time! Saw you on TV. Listen, about that promotion…', text: true },
    { sp: 'n', t: 'You leave him on read.' },
    { sp: 'slick', t: 'People insist on calling it luck.' },
    { sp: 'you', t: 'Let them.' },
  ], end: 'c7' },
};

// the first time a legend sits at your final table they say their line; who you meet is remembered
function drMeetLine(id) {
  if (id === 'ghost') return { sp: 'ghost', t: "Heads-up for the world title. I've waited a long time for someone worth this." };
  if (id === 'trent') return { sp: 'trent', t: "Final table of the Classic. Just you, me and Dad's money. Let's go." };
  if (id === 'lou') return { sp: 'lou', t: "Final table, kid. Thirty years I've been trying to win this thing. Don't you dare." };
  const L = drLegend(id); return L ? { sp: 'L:' + id, t: L.line } : null;
}
