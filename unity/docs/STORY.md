# The Dream — the story, Chapter 2 to Legacy

Written 2026-10-29 by the cloud Claude session, picking up where Steven's Prologue and Chapter 1 end (`dream/script.md`
and the old `Story.cs` flow). Steven: "after chapter 1 that i wrote so far, you can take over and make an advanced
story with advanced conversation, movie scenes etc". Also: "the legends as almost bosses in the chapters, cover all of
them", and "pro tournaments like the WSOP clone and the Triton clone are all pros / characters of the story as the
players".

**Status: draft for Steven to read. Nothing gets voiced or rendered until he has.**

**Revised 2026-10-29 11:40 from Steven's answers in the scene tree:**
- Event names: Flashy Lights Circuit (rings), WCoC, the World Championship of Cards (the Summer bracelets and the Main
  Event), the Let's Go! Tour and the One Time! Cup (TV), and the Titan. Silver Strike stays as the casino's name.
- Slick's fold was ten years ago, not twenty ("he's not THAT old"). The player is 24 ("make my guy 24"), so he was
  fourteen when he watched it on TV.
- David's fiancée is Katie.
- Mustich stays a rival to the end. `c7_rival` is gone; `c7_rival` replaces it.
- The game still ends on the luck line: "People insist on calling it luck."
- No new side characters: the pro fields are legends and the current cast only.
- The last river of the Main Event is the player's call or fold, and the cards decide (`c6_final`, new `c6_fold`).
- Kept as written: Colt and the pie, Slick in Vegas for the Main, Lou's firm Halvorsen & Pike.
- Bio wears a mask, not glasses (Steven). Only Slick wears dark glasses.
- The twist, settled (Steven: "just adjust the verbiage to the storyline"): his Prologue says the man on TV wore dark
  glasses, and only Slick wears them. So Slick was the man on TV, and Bio was the one who folded. The spine below is
  rewritten that way.

Format, same as `dream/script.md`:
- Each scene is `## id — Chapter · title (place)`, then numbered lines `Name [speaker id]: line {shot}`.
- `(text)` marks a phone text. `- Choice:` is a button (`→` is where it leads; after `|` is the reply).
- Shots: ext, wide, two, ots, cu, ecu, push, low, high, hands, insert. After `:` comes the acting clip, after `@` the
  sting, after `~` the score.
- Speaker ids:

  | id | character |
  |---|---|
  | n | narrator |
  | you | the player |
  | slick | Slick |
  | derek | Natso "Corporate Lou" SweetLou |
  | lou | Tommy "The Shark" Maio |
  | mom | David Pec |
  | trent | Mustich |
  | rosa | Miles "The Snowplow" Gnomey |
  | host | Nick "Baby Goat" Twogloves |
  | ghost | Bio "The Ghost" |
  | L:\<id\> | a legend (ids from `dream/data.js` DR_LEGENDS) |
  | td | a tournament director |

  `td` is the only new id.

---

## The spine

**The question under the whole game:** who was the man in dark glasses on the fuzzy TV, what happened to him, and who
folded to him?

When you were fourteen, you watched the World Championship of Cards final on TV. A man in dark glasses moved all in on
the river with nothing. You could see his cards because the hole-card camera showed them to the audience. His opponent
thought for four minutes and folded the winning hand. The man never showed. In the interview afterwards he said one
line: *"People insist on calling it luck."*

- **The man in dark glasses was Slick.** He won the world title with seven-deuce, and the line in your envelope is his
  own, from that interview. Steven's Prologue already plants it: the stranger at the diner wears dark glasses, and the
  envelope carries the line.
- **What happened to him:** he spent the next year believing one perfect read made him bulletproof. He played above his
  roll every night until the game took back every dollar. He came home to Calle Ocho broke, and Colt the Cowboy bought
  his pie at the Lucky Seven (Chapter 2). He has spent ten years at 2:13 AM since, buying other people's pie. That is
  where "Never play above your roll" comes from.
- **The opponent who folded was Bio "The Ghost".** He has worn the mask since that night, so nobody can read his face
  again. Fourteen bracelets, the most of anyone alive, and never the one he lost to Slick: the Main Event (c5_score:
  "there's only one he still cares about every year").
- **Why Slick writes the line on every envelope:** it is the cockiest thing he ever said, and he wants someone to prove
  it right the long way. It wasn't luck. He read a person, and the person flinched. Everything he teaches is so you
  never flinch, and so you never think one great read makes you bulletproof.
- **How the player finds out:**
  - Chapter 2: Armadillo Fatts tells the story of the final and quotes the interview line. It's the line in your
    envelope. Then Colt says he bought pie for a broke kid in dark glasses on Calle Ocho.
  - Chapter 3: Bio, in the mask, at the feature table. "Tell Slick I said hello."
  - Chapter 4: Slick tells it himself, and names the man who folded.
- **The payoff:** at the Main Event final table Bio jams the river on you, in the seat Slick once had. He looks at you
  the way Slick looked at him. This time somebody doesn't flinch, whatever the cards do. In Legacy, Slick lets the Hall
  of Legends hang the champion's portrait he asked them to leave out.

**Every chapter turns a theme over:**
- Chapter 2, discipline: the money starts to be real.
- Chapter 3, cost: the Summer takes time you owe people.
- Chapter 4, exposure: the cameras, and Slick's past.
- Chapter 5, identity: when everyone has the same maths, who are you?
- Chapter 6, fear: the hand Slick couldn't play.
- Legacy, giving it back.

**The threads**, each with a beginning, a turn and an end:
- **David Pec**, the hype man in ALL CAPS.
  - The Summer costs you his engagement party.
  - His texts go lowercase. You win him back.
  - He flies to the Main Event rail and shouts the line he's been texting all game.
  - In Legacy you stand up at his wedding.
- **Mustich**, the rival.
  - He wins a bracelet before you do and never lets you forget it.
  - His father cuts him off (Chapter 5).
  - Bio busts him at the Main, and you find him in a hallway. He's still not your friend.
  - In Legacy he's still circling: back at the Hall of Legends Invitational, paying his own way, wanting your
    fifteenth bracelet.
- **Natso "Corporate Lou" SweetLou**, the comic thread.
  - He sees your ring on Instagram.
  - His firm sponsors the One Time! Cup, so he turns up in a VIP lanyard wanting a LinkedIn photo.
  - The promotion text after you win the Main.
  - In Legacy, a conference room named after you that you will never enter.
- **Tommy "The Shark" Maio.**
  - He hasn't left Biscayne for a game in thirty years, and drives you to your first Circuit stop.
  - He's on the Main Event rail.
  - In Legacy, he finally gets his name on the board by the cage.
- **Miles "The Snowplow" Gnomey.**
  - The running gag, "Seat open!"
  - He climbs from the Biscayne cage to Circuit floor, then the Summer floor, then the Main Event floor.
  - In Legacy he is the tournament director of the Hall of Legends Invitational.
- **Nick "Baby Goat" Twogloves**, the voice of the broadcast.
  - He names your "luck" line on air.
  - He calls the final hand of the Main.

---

## The legends: all 27, as near-bosses

Each legend gets an entrance, a signature line (their `line` in `data.js`, or one written to match it) and a table
moment. In the game they're the named "boss" at the final table of the event listed, with an `ft_intro` / `ft_won` /
`ft_lost` trio. The ✦ marks a scene below.

| # | Legend | Era | Chapter | Event (our names) | Scene | Their moment |
|---|---|---|---|---|---|---|
| 1 | Chris Schillings | Boom | 2 | Flashy Lights Circuit · $580 Ring Event | c2 ✦, ring1 ✦ | The accountant who did it: your mirror, and your first fan |
| 2 | Mouthy Michael | Boom | 2 | Circuit · $400 Turbo Ring | c2_mouth ✦ | Talks you into a call. Learn to stop listening |
| 3 | Sammy Smokes | Boom | 2 | Silver Strike's upstairs game | c2_smokes ✦ | The game you're not rolled for. Discipline |
| 4 | Armadillo Fatts | Old school | 2 | Silver Strike bar | c2_fatts ✦ | Tells the story of "The Fold": the first hint |
| 5 | Colt the Cowboy | Old school | 2 | Circuit charity event | c2_cowboy ✦ | Bought a skinny kid's pie on Calle Ocho once: the second hint |
| 6 | Phil Hellraiser | Boom | 3 | WCoC $1,500 Opener | c3 ✦, bracelet1 ✦ | Most bracelets alive, and he'll tell you |
| 7 | GG Negroni | Boom | 3 | WCoC $3,000 Six-Max | c3_gg ✦ | Says your cards out loud. Sometimes he's right |
| 8 | Gus Handsome | Boom | 3 | Players' dinner, WCoC $5,000 | c3_steak ✦ | Any two cards. The life you could have |
| 9 | Antonio Escandalo | Boom | 3 | Players' dinner | c3_steak ✦ | The card trick, and the lesson in misdirection |
| 10 | Stu Hungar | Old school | 3 | Hallway after the bubble | c3_bubble ✦ | The comeback kid, on comebacks |
| 11 | Chance Cage | Old school | 3 | WCoC $10,000 Heads-Up | c3_chance ✦ | Slow-plays you into the ground. Patience |
| 12 | Nicky Parmesan | Rising star | 3 | WCoC mixed games | c3_parm ✦ | Three bracelets this summer. "Pick a game." |
| 13 | Erik Seikick | Boom | 3 | Your bracelet final table | bracelet1 ✦ | Thirty years at the top, three words to you |
| 14 | Tom Duhhz | Online | 4 | Let's Go! Tour feature table | c4_duhhz ✦ | The six-figure bluff on stream |
| 15 | Pat Finny | Online | 4 | One Time! Cup $25,000 | c4_ice ✦ | Never blinks. You learn stillness |
| 16 | Kristen Foxglove | High roller | 4 | Let's Go! Tour | c4 ✦, tour1 ✦ | "Everyone has a file on you now." |
| 17 | Alex Foxglove | High roller | 4 | Player of the Year race | c4_race ✦ | The tour machine; the leaderboard is a person |
| 18 | Shane Dawb | High roller | 4 | Player of the Year race | c4_race ✦ | Another one for the cabinet |
| 19 | Mark Holdz | High roller | 4 | One Time! Cup turbo | c4_race ✦ | "Shove." Mr. Volume |
| 20 | Bryan Dartfrog | High roller | 5 | Titan · the backing meeting | c5_money ✦ | Buys a piece of you, or doesn't |
| 21 | Jay Cocoon | High roller | 5 | Titan hotel gym, 5 AM | c5_gym ✦ | Discipline beats talent |
| 22 | Fedor Holdum | High roller | 5 | Titan · $100,000 | c5_lab ✦ | "I studied this exact spot this morning." |
| 23 | Linus Loophole | High roller | 5 | Titan · $100,000 | c5 ✦, c5_lab ✦ | Your frequencies are acceptable |
| 24 | Isaac Maskedman | High roller | 5 | Titan · $50,000 Omaha | c5_prof ✦ | Four cards or two, he has solved it |
| 25 | Stephen Chives | High roller | 5 | Titan · $250,000 | c5_chives ✦ | "Good luck. Genuinely." The kindness at the top |
| 26 | Jesse Lonestar | Rising star | 6 | Main Event Day 1 | c6_day1 ✦ | GORILLA. MODE. |
| 27 | Michael Mightyrocky | Rising star | 6 | Main Event, the last two tables | c6 ✦, c6_champ ✦ | The defending champion |

All 27 come back in Legacy, in the Hall and as heads-up challenges (c7_gauntlet).

## The pro events and their fields (named people, not anonymous seats)

These are our own versions, with our names: the WCoC Summer Series is our WSOP, the Titan is our Triton, the Let's Go! Tour
and the One Time! Cup are the TV tour, and the WCoC Main Event is the world championship. Each event's final table has eight
named players besides you, drawn from these lists: legends and the current cast only, no new faces.

- **Flashy Lights Circuit** at Silver Strike Casino (Chapter 2: rings)
  - Chris Schillings, Mouthy Michael, Sammy Smokes, Mustich, Colt the Cowboy (the charity event).
  - Tommy "The Shark" Maio at the first stop; Miles "The Snowplow" Gnomey on the floor.
- **WCoC Summer Series, Las Vegas** (Chapter 3: bracelets)
  - $1,500 Opener: Phil Hellraiser, GG Negroni, Mustich, Stu Hungar.
  - $3,000 Six-Max: GG Negroni, Gus Handsome.
  - $5,000 Eight-Handed: Gus Handsome, Antonio Escandalo, Erik Seikick.
  - $10,000 Heads-Up: Chance Cage.
  - Mixed games: Nicky Parmesan.
  - Your bracelet final table: Erik Seikick, Phil Hellraiser, Mustich, GG Negroni, Gus Handsome, Stu Hungar, Antonio
    Escandalo, Nicky Parmesan.
- **Let's Go! Tour and One Time! Cup** (Chapter 4: TV titles)
  - Tom Duhhz, Pat Finny, Kristen and Alex Foxglove, Shane Dawb, Mark Holdz, Mustich.
  - Nick Twogloves in the booth.
- **Titan Super High Roller** (Chapter 5: invitation only)
  - Fedor Holdum, Bryan Dartfrog, Stephen Chives, Jay Cocoon, Linus Loophole, Isaac Maskedman, Mustich.
  - Bio is not here (c5_score).
- **WCoC Main Event** (Chapter 6)
  - Day 1: Jesse Lonestar and the ballroom.
  - Deep: Michael Mightyrocky, Mustich, Kristen Foxglove, Tom Duhhz.
  - The final table: Bio "The Ghost", Jesse Lonestar, Shane Dawb, Stephen Chives, Pat Finny, Kristen Foxglove, Tom
    Duhhz, Phil Hellraiser.

## Sets this needs (for the build list)

- **Already exist:** casino, vegas, tv, titan, main, hall, diner, home (the apartment), cardroom.
- **New:**
  - Tommy's Lincoln on the causeway at dawn.
  - The Silver Strike bar.
  - A Vegas steakhouse booth.
  - A convention-centre hallway, with fluorescent light, carpet and a bracelet poster.
  - The Titan hotel gym at 5 AM.
  - A Vegas 24-hour diner (the mirror of the Lucky Seven).
  - The Hall of Legends gallery.
  - A wedding reception.

---

# CHAPTER 2 — THE CIRCUIT

Goal: win two Circuit rings and build a $40,000 bankroll.

## c2_drive — Chapter 2 · The causeway (car)

1. Narrator [n]: 5:10 AM. The causeway is empty and the bay is the colour of a nickel. / Tommy "The Shark" Maio is driving a 1998 Lincoln like it owes him money.  {ext~grind}
2. Tommy "The Shark" Maio [lou]: Thirty years at Biscayne. You know how many times I drove north for a game?  {two:Talking}
3. You [you]: Once?  {cu:Shrugging}
4. Tommy "The Shark" Maio [lou]: Never. Not once. Rings are for guys who think they're going somewhere.  {ots:Shaking Head No}
5. You [you]: So why are you driving?  {cu:Talking}
6. Tommy "The Shark" Maio [lou]: Because you asked. And because the coffee at Silver Strike is free and mine is terrible.  {two:Shrugging@tick}
7. Narrator [n]: He doesn't say the other thing. He doesn't have to. A man doesn't iron his lucky shirt for free coffee.  {push^:Thinking@swell}

## c2 — Chapter 2 · The Circuit (casino)

1. Narrator [n]: Silver Strike Casino. Forty tables under a ceiling painted like a sunset, and every one of them is playing for a ring.  {ext~grind}
2. Narrator [n]: A man at the rail wears four on one hand. He keeps flexing it so you'll ask.  {wide^:Looking Around}
3. Miles "The Snowplow" Gnomey [rosa]: Look who it is. They've got me running the floor up here on weekends now. Table twelve, seat six. Seat open!  {two:Pointing@tick}
4. Chris Schillings [L:moneybaker]: You're the kid who quit the cubicle? I was an accountant. Spreadsheets and everything.  {two:Talking}
5. You [you]: How did you know?  {cu:Thinking}
6. Chris Schillings [L:moneybaker]: You hold your chips like they're receipts. Stop it. Play your game, and let them find out who you are.  {ots:Pointing>Head Nod Yes}
7. Mustich [trent]: Look who followed me north. New patch, see? Somebody pays me to wear their logo now. Try not to bust on the bubble.  {cu:Pointing@sting}
8. Narrator [n]: Goal: win two Circuit rings and build a $40,000 bankroll.  {wide^@swell}

## c2_fatts — Chapter 2 · Last call at the Strike (casino)

1. Narrator [n]: The casino bar at 1 AM. Keno on a television nobody watches. / An enormous man in a white hat is holding court with a plate of wings.  {wide^:Looking Around~night}
2. Armadillo Fatts [L:armadillo]: Sit down, partner. Everybody who sits gets a story, and nobody gets a refund.  {two:Talking}
3. Armadillo Fatts [L:armadillo]: Ten years back, give or take. World Championship of Cards, last two players. Kid in dark glasses shoves the river with nothing. Seven-deuce. The whole country can see it on the little camera.  {cu:Pointing}
4. Armadillo Fatts [L:armadillo]: Other fella has the winner. Thinks four minutes. Folds. Wears a mask to this day, so nobody can read him twice.  {push:Shaking Head No~tension}
5. You [you]: I watched that. I was fourteen.  {ecu:Thinking@hush}
6. Armadillo Fatts [L:armadillo]: Everybody watched it. Then the kid goes on TV and says one thing. "People insist on calling it luck." Cockiest sentence I ever heard.  {cu:Talking@sting}
7. Narrator [n]: The line in your envelope. Word for word. In pen.  {insert^:Thinking@hush}
8. You [you]: What happened to him?  {ots:Talking}
9. Armadillo Fatts [L:armadillo]: Same thing that happens to all of us. He went home. / Order the wings. They're better than the story.  {two:Shrugging@tick~none}

## c2_mouth — Chapter 2 · The Yapper (casino)

1. Narrator [n]: The $400 Turbo Ring, final table. Mouthy Michael has been talking for six straight hours and has no plans to stop.  {wide^:Looking Around~tension}
2. Mouthy Michael [L:mouth]: You're thinking. Don't think. Thinking is how they get you. You call this? You're not calling this. You're NOT calling this.  {cu:Standing Arguing}
3. Narrator [n]: Every word is built to put a voice in your head that isn't yours.  {push^:Thinking}
4. Slick [slick] (text): Is he talking?
5. You [you] (text): He hasn't stopped since noon.
6. Slick [slick] (text): Then he's scared. Comfortable people don't need noise. Play the cards, not the soundtrack.
7. Mouthy Michael [L:mouth]: ...You called. Of course you called. Nice hand. NICE HAND. I'm fine. I'm totally fine.  {cu:Angry Gesture@sting}

## lesson2 — A lesson · Pressure the scared ones (casino)

1. Narrator [n]: The Circuit floor at midnight. Chips being racked all around you. None of them are yours.  {wide^:Looking Around~night}
2. Slick [slick]: You check too much.  {ots:Talking}
3. You [you]: I didn't have anything.  {cu:Shrugging}
4. Slick [slick]: Neither did they. On a dry board the raiser bets small and often, and everybody without a pair goes away. You gave them a free card and a free look at you.  {push:Talking@sting}
5. You [you]: It felt like lying.  {cu:Thinking}
6. Slick [slick]: It's not lying. It's telling them a story they already believe. Pressure the scared ones. Small bets, lots of them. It's in your misses. Go run them.  {cu:Pointing>Head Nod Yes@swell}

## ring1 — Chapter 2 · First ring (casino)

1. Narrator [n] (win): The tournament director slides a heavy ring across the felt. It's a size too big. You put it on anyway.  {hands^:Fist Pump@chips~triumph}
2. Chris Schillings [L:moneybaker]: First ring. I still remember mine. Don't put it in a drawer. Wear it every day you play. Let it be heavy.  {two:Pointing>Head Nod Yes@swell}
3. Tommy "The Shark" Maio [lou]: Thirty years. First time I ever drove north, and the kid takes a ring. I'm telling everyone it was my coffee.  {two:Clapping@crowd}
4. David Pec [mom] (text): A RING??? LIKE A CHAMPIONSHIP RING?? LIKE THE NBA???
5. David Pec [mom] (text): PUTTING IT IN THE GROUP CHAT. RENAMING THE GROUP CHAT
6. Natso SweetLou [derek] (text): Saw your ring on Instagram!! Crazy! Quick q — do you still have the login for the Henderson drive?
7. Slick [slick] (text): One ring. Now go get another one. Don't celebrate in a place that sells chips.

## c2_smokes — Chapter 2 · Upstairs (casino)

1. Narrator [n]: A private elevator. A room above the casino with one table, eight chairs, and a man holding an unlit cigarette like a conductor's baton.  {ext~tension}
2. Sammy Smokes [L:fargo]: The ring kid. Sit, sit. Is small game tonight. Ten thousand minimum.  {two:Pointing}
3. Narrator [n]: Ten thousand is a quarter of everything you have. / The chairs are leather. The chips are clay. The room smells like money that was never worried.  {push^:Looking Around@hush}
4. Sammy Smokes [L:fargo]: Is a nice hand you have in you, I can tell. Is not my hand. But is nice.  {cu:Talking>Shrugging}
   - Choice: Sit down. | Narrator [n]: You buy in for ten thousand. Sammy smiles like a man who owns the building, because he does. → c2_smokes_sit
   - Choice: "Not tonight. I'm not rolled for it." | Sammy Smokes [L:fargo]: Ha! Is smartest thing anyone says in this room all year. Come back when it's small for you.
5. Slick [slick] (text): Who's upstairs?
6. You [you] (text): Sammy Smokes. 10K minimum.
7. Slick [slick] (text): The game you're not rolled for is always the best game in the building. That's how it gets you.

## c2_smokes_sit — Chapter 2 · Upstairs, later (casino)

1. Narrator [n]: Three hours. You play the best poker of your life and you lose six thousand dollars of it.  {high^:Sad Idle@hush~night}
2. Sammy Smokes [L:fargo]: You play good. The money doesn't know. Money never knows. This is why we keep it far from the heart.  {cu:Talking}
3. You [you]: Is that your lesson?  {cu:Shaking Head No}
4. Sammy Smokes [L:fargo]: My lesson? Is that I can afford this room. Go downstairs, ring kid. Downstairs is where you get rich.  {ots:Pointing@sting}

## c2_cowboy — Chapter 2 · The Cowboy (casino)

1. Narrator [n]: The Circuit's charity event. Celebrities, local news, a giant check. And at table one, in the same hat for fifty years, Colt the Cowboy.  {ext~grind}
2. Colt the Cowboy [L:brunswick]: Son, I was playing this game before your daddy could shuffle. Sit. Let's see if they taught you anything.  {two:Talking}
3. Narrator [n]: He raises with ten-deuce, shows it, and wins the pot. The table laughs. He doesn't.  {hands@chips}
4. Colt the Cowboy [L:brunswick]: Everybody laughs at the ten-deuce. That's why it works. You know who I taught that to? Skinny kid. Dark glasses. Calle Ocho.  {cu:Pointing}
5. You [you]: The Lucky Seven?  {ecu:Thinking@sting}
6. Colt the Cowboy [L:brunswick]: Bought him a slice of pie there once. Night he came home. A year after the big one, and every dollar of it gone. Soaked through, and he wouldn't take off those glasses.  {push:Thinking~tension}
7. Colt the Cowboy [L:brunswick]: He still buying people's pie?  {cu:Talking}
   - Choice: "Every night." | Colt the Cowboy [L:brunswick]: Good. Then I got my money's worth.
   - Choice: "I don't know who you mean." | Colt the Cowboy [L:brunswick]: Sure you don't. Neither does he.

## ring2 — Chapter 2 · The second ring (diner)

1. Narrator [n]: Lucky Seven Diner. 2:13 AM. Two rings on the counter next to the pie.  {ext~night}
2. Slick [slick]: Two rings and forty thousand dollars, none of it borrowed. That's a bankroll. You're not a tourist any more.  {two:Talking>Head Nod Yes}
3. You [you]: I met Colt the Cowboy.  {cu:Talking}
4. Slick [slick]: Everyone meets Colt eventually. He's like weather.  {cu:Shrugging}
5. You [you]: He said he bought a broke kid in dark glasses a slice of pie here. The night he came home.  {push:Thinking@hush~tension}
6. Narrator [n]: Slick turns his coffee cup a quarter turn. Then another. He doesn't look up.  {insert@tick}
7. Slick [slick]: Eat your pie. The Summer starts in June, and Vegas doesn't care who you met.  {ecu:Talking@sting}
8. Narrator [n]: Goal complete. Chapter 3: The Summer.  {wide^@swell}

---

# CHAPTER 3 — THE SUMMER

Goal: win a gold bracelet.

## c3 — Chapter 3 · The Summer (vegas)

1. Narrator [n]: Every June, the WCoC Summer Series takes over Las Vegas. 112 degrees outside and 68 inside, with ten thousand people chasing the same gold.  {ext~grind}
2. Miles "The Snowplow" Gnomey [rosa]: They hired me for the summer! Floor supervisor, rooms A through D. Six weeks, no sunlight. Seat open!  {two:Pointing@tick}
3. Slick [slick] (text): Every kid who ever loved this game dreams about a bracelet. This is the room where they hand them out. Don't look at the ceiling.
4. Narrator [n]: You look at the ceiling. Everyone does. Banners of every champion, back to the year your father was born.  {low^:Looking Around@swell}
5. Narrator [n]: At the feature table sits a man in a black mask. Fourteen bracelets, more than anyone alive. Nobody has seen him smile. Nobody has seen most of his face.  {cu^:Looking Around@hush~tension}
6. Bio "The Ghost" [ghost]: Kid. Stop looking at me and look at your cards.  {ecu:Talking@sting}
7. Phil Hellraiser [L:hellbent]: Another internet kid. I'll see you at the final table. Probably not, though. Statistically, not.  {ots:Shrugging}
8. Narrator [n]: Goal: win a gold bracelet.  {low^@swell~grind}

## c3_gg — Chapter 3 · Young Poker (vegas)

1. Narrator [n]: $3,000 Six-Max. On your left, GG Negroni has been narrating everyone's hand for an hour, out loud, and he's mostly right.  {wide^:Looking Around}
2. GG Negroni [L:negrono]: I put you on ace-queen. Am I close? I'm close. Your shoulders went up a quarter inch when the queen came.  {cu:Pointing}
3. You [you]: Maybe I have kings.  {cu:Shrugging}
4. GG Negroni [L:negrono]: If you had kings you'd be bored, and you're not bored. You're doing math. Kings don't do math.  {push:Talking@sting}
5. Narrator [n]: He folds. You had ace-queen.  {hands@tick}
6. GG Negroni [L:negrono]: See? Close. Hey, no hard feelings. Ninety percent of this game is listening. The other ten is pretending you weren't.  {two:Clapping}

## c3_steak — Chapter 3 · The players' dinner (vegas)

1. Narrator [n]: A steakhouse with no prices on the menu. A sponsor's table for twelve. Somebody ordered a bottle you've seen on TV.  {ext~night}
2. Gus Handsome [L:handsome]: Seven-four suited. That's how I got here. Nobody believes me. That's also how I got here.  {two:Talking}
3. Antonio Escandalo [L:escandalo]: Pick a card. Any card. No, don't show me. Show the table.  {cu:Pointing}
4. Narrator [n]: You pick the queen of hearts. He doesn't touch the deck. It's under your steak knife.  {insert@sting}
5. Antonio Escandalo [L:escandalo]: Poker is the same trick. Everyone watches the hand that moves. Nobody watches the hand that doesn't.  {cu:Talking}
6. Mustich [trent]: Speaking of tricks. Event nine. First bracelet. Thought you'd want to see it up close.  {ots:Fist Pump@sting~tension}
7. Narrator [n]: He's wearing it. Of course he's wearing it.  {push^:Thinking}
8. Gus Handsome [L:handsome]: Don't let him in your head. The bracelet's real, but the guy's a rental. Have the crab.  {two:Shrugging@tick}

## c3_bubble — Chapter 3 · Two hundred and twelfth (vegas)

1. Narrator [n]: Two hundred and eleven players left. Two hundred get paid. You were two hundred and twelve.  {high^:Sad Idle@hush~tension}
2. Narrator [n]: The hallway outside the ballroom. Fluorescent lights. A bracelet poster with somebody else's face on it.  {wide^:Looking Around}
3. Stu Hungar [L:hungar]: First bubble in the big room? Kid, I've seen this hallway from every angle. Lying down, mostly.  {two:Talking}
4. You [you]: You're Stu Hungar.  {cu:Thinking}
5. Stu Hungar [L:hungar]: Was. Then wasn't. Then was again. They call it a comeback. It's just showing up after everyone stopped expecting you.  {cu:Talking}
6. Stu Hungar [L:hungar]: Kid, I've seen this hand before. You haven't. Go learn why it hurts, then come back tomorrow.  {push:Pointing@swell}

## lesson3 — A lesson · What the money's worth (vegas)

1. Slick [slick] (text): You called a shove with ace-ten on the bubble.
2. You [you] (text): I was ahead of his range.
3. Slick [slick] (text): Ahead of his range in chips. Behind in money. Near the bubble, the chips you lose are worth more than the chips you win. That's the whole of ICM.
4. Slick [slick] (text): Think about what the money's worth, not the chips. The spot's on your Spacepad. Drill it till it stops hurting.
5. You [you] (text): You could've just said "fold".
6. Slick [slick] (text): Then you'd fold this one and call the next one. I'd rather you knew why.

## c3_chance — Chapter 3 · The Mirage (vegas)

1. Narrator [n]: $10,000 Heads-Up. Your opponent hasn't raised in forty minutes. He has a glass of water and all the time in the world.  {wide^:Looking Around~tension}
2. Chance Cage [L:chance]: Take your time. I always do.  {cu:Talking}
3. Narrator [n]: You bet. He calls. You bet. He calls. You bet everything.  {hands@chips}
4. Narrator [n]: He shows a full house he's been holding since the flop. / He hasn't raised in forty minutes because he hasn't needed to.  {push^:Shaking Head No@sting}
5. Chance Cage [L:chance]: The loudest thing at a poker table is a man who's waiting. You'll hear it next time.  {two:Head Nod Yes}

## c3_party — Chapter 3 · The engagement party (home)

1. David Pec [mom] (text): BRO. SATURDAY. YOU'RE COMING. KATIE SAID YES AND YOU ARE THE FIRST PERSON I TOLD AFTER MY MOM
2. Narrator [n]: Saturday is the $5,000 Eight-Handed. Your best event of the summer, on paper.  {insert^:Texting While Standing~night}
   - Choice: Fly home for the party. | Narrator [n]: You change your flight. The event will run without you. It does.
   - Choice: "I can't, D. It's the 5K. I'll make it up to you." | David Pec [mom] (text): its cool. go win.
3. David Pec [mom] (text): (if you fly home) YOU CAME??? YOU ACTUALLY CAME??? KATIE HE CAME
4. Narrator [n]: (if you stay) its cool. go win. All lowercase. David has never typed lowercase in his life.  {ecu^:Thinking@hush}

## c3_parm — Chapter 3 · Pick a game (vegas)

1. Narrator [n]: The mixed-game room. Eight games in rotation, and a kid in a backwards cap who has won three bracelets in five weeks.  {wide^:Looking Around}
2. Nicky Parmesan [L:glazer]: Pick a game. Any game. I'll still be the favourite, but it'll be fun for you.  {cu:Pointing}
3. You [you]: Hold'em.  {cu:Shrugging}
4. Nicky Parmesan [L:glazer]: Respect. Knowing your one game is a skill. Most people find out what it is the expensive way.  {two:Head Nod Yes@tick}

## bracelet1 — Chapter 3 · Gold (vegas)

1. Narrator [n]: Heads-up for the $1,500 Opener bracelet. Across from you: Erik Seikick. Thirty years at the top of this game, and he hasn't said a word to you all night.  {wide^:Looking Around~tension}
2. Erik Seikick [L:sidle]: ...  {ecu:Thinking}
3. Narrator [n] (win): The last card. They fasten the bracelet on your wrist, and the room is clapping before you realise it's for you. / Gold. Real gold. Your name in the book forever.  {low^:Victory@crowd~triumph}
4. Erik Seikick [L:sidle]: You played well.  {two:Shaking Hands 2>Shaking Hands 2@swell}
5. Narrator [n]: Three words. People have waited thirty years for three words from Erik Seikick.  {push^:Thinking}
6. Phil Hellraiser [L:hellbent]: Lucky. If I'd been at that final table, we'd be talking about my bracelet number eighteen.  {cu:Shrugging@tick}
7. David Pec [mom] (text): IS THAT REAL GOLD?? THE WHOLE GROUP CHAT SAW IT. KATIE CRIED. I CRIED. I'M STILL MAD BUT I CRIED
8. Slick [slick] (text): Fourteen-year-old you is screaming right now. Let him.

## c3_hello — Chapter 3 · The sportsbook, 4 AM (vegas)

1. Narrator [n]: 4 AM. The sportsbook is empty except for highlights of a game that ended hours ago.  {ext~night}
2. Narrator [n]: Someone sits two stools down. The mask hangs off one ear, and the face under it is older than you expected.  {wide^:Looking Around@hush}
3. Bio "The Ghost" [ghost]: Nice bracelet.  {cu:Talking}
4. You [you]: You've got fourteen.  {cu:Thinking}
5. Bio "The Ghost" [ghost]: I've got fourteen. You've got one that matters more. The first one is the only one you don't win for anybody else.  {push:Talking~tension}
6. Bio "The Ghost" [ghost]: You wait like somebody taught you to. I only know one man who teaches that.  {cu:Talking@hush}
7. Narrator [n]: He stands. He leaves a tip on the bar for a bartender who isn't there.  {hands@tick}
8. Bio "The Ghost" [ghost]: Tell Slick I said hello.  {ecu:Talking@sting}
9. Narrator [n]: He's gone before you can ask how he knows the name.  {wide^~none}

---

# CHAPTER 4 — THE TOUR

Goal: win two Tour titles (Let's Go! Tour or One Time! Cup) and build a $750,000 bankroll.

## c4_truth — Chapter 4 · The Fold (diner)

1. Narrator [n]: Lucky Seven Diner. 2:13 AM. You didn't text first. He was already there.  {ext~night}
2. You [you]: Bio says hello.  {two:Talking}
3. Narrator [n]: For a long time, nothing. The pie case hums. A car goes by with its windows down, playing something sad in Spanish.  {wide^:Thinking@hush}
4. Slick [slick]: World Championship of Cards. Last two players. I had seven-deuce and a river that missed everything, and I knew he had me beat. I could see it on him.  {cu:Talking~tension}
5. Slick [slick]: So I shoved. I didn't look at the board. I looked at him. And I watched him start to doubt he was allowed to win.  {push:Thinking}
6. Slick [slick]: Four minutes. Then the best player I ever sat with folded the best hand he ever held, and the whole world watched it on a little camera.  {ecu:Shaking Head No@sting}
7. You [you]: The man in the dark glasses was you. And the one who folded was Bio.  {cu:Thinking}
8. Slick [slick]: They asked me how. I said "people insist on calling it luck". Cockiest thing I ever said. Then I spent a year proving I could do it every night, above my roll, until the game took back every dollar.  {cu:Talking}
9. You [you]: And Colt bought your pie.  {ots:Talking@hush}
10. Slick [slick]: Colt bought my pie. / I write that line on the envelopes so somebody proves it right the long way. It wasn't luck. I read a person, and the person flinched. Everything I've taught you is so you never flinch, and so you never think one great read makes you bulletproof.  {push:Pointing@swell}
11. Slick [slick]: Now finish your pie. You've got cameras next week, and cameras love a flinch.  {two:Shrugging~night}

## c4 — Chapter 4 · The Tour (tv)

1. Narrator [n]: A soundstage in Hollywood, Florida. Three cameras, a hole-card reader, and forty thousand people watching you blink.  {ext@flash~grind}
2. Nick "Baby Goat" Twogloves [host]: Welcome back to the Let's Go! Tour feature table! Joining us tonight: the cubicle kid with a bracelet. The chat's calling it a fluke.  {two:Pointing}
3. You [you]: People insist on calling it luck.  {push:Talking@swell}
4. Nick "Baby Goat" Twogloves [host]: OH. We have a quote. Producers, T-shirt. Now.  {cu:Clapping@flash}
5. Narrator [n]: Under the lights everything shows. The rail can see your cards. The table can see your face.  {wide^:Looking Around}
6. Kristen Foxglove [L:kfoxglove]: Cameras make people do stupid things. Don't be people.  {cu:Shaking Head No}
7. David Pec [mom] (text): youre on the STREAM. my mom is watching. MY MOM. SIT UP STRAIGHT
8. Narrator [n]: Goal: win two Tour titles (Let's Go! Tour or One Time! Cup) and build a $750,000 bankroll.  {wide^@swell}

## c4_duhhz — Chapter 4 · Air (tv)

1. Narrator [n]: The river. Tom Duhhz pushes a stack worth more than your first year on Floor 34 into the middle, and smiles at the camera like it's an old friend.  {wide^:Looking Around~tension}
2. Tom Duhhz [L:swan]: I have air. Do you want to find out?  {cu:Talking}
3. Nick "Baby Goat" Twogloves [host]: The hole-card cam says... folks, he is not lying. He has ABSOLUTELY nothing. The question is whether our cubicle kid believes him.  {two:Pointing@flash}
4. Narrator [n]: Somewhere a fourteen-year-old is watching a river jam on a little camera. / This time you know which seat you're in.  {ecu^:Thinking@hush}
5. Tom Duhhz [L:swan]: Truth is the best bluff. Nobody believes it.  {cu:Shrugging@sting}

## lesson4 — A lesson · People, not boards (tv)

1. Narrator [n]: The stream is over. The chat has moved on. The hand hasn't.  {wide^:Looking Around~night}
2. Slick [slick]: You bluffed the one guy who never folds.  {cu:Talking}
3. You [you]: The board was perfect for it.  {cu:Shrugging}
4. Slick [slick]: Perfect for a bluff against a player who folds. He doesn't. A bluff is a bet against a person, not a board. Pick the person first.  {push:Pointing@sting}
5. You [you]: Is that what you did to Bio?  {cu:Thinking@hush}
6. Slick [slick]: I picked the person first. Then the card. Then the moment. In that order. Ranges, not hands. People, not boards. Drill the river spots.  {two:Pointing>Head Nod Yes@swell}

## c4_lou — Chapter 4 · The sponsor (tv)

1. Narrator [n]: The One Time! Cup VIP lounge. Free shrimp. A banner reading "Halvorsen & Pike: Proud Partner". / You know that logo. You used to wear it on a lanyard.  {ext~grind}
2. Natso SweetLou [derek]: Buddy!! There he is! I told the partners we go way back. Mentor-mentee kind of thing.  {two:Talking}
3. You [you]: You made me redo the Henderson deck at seven PM on a Monday, Lou.  {cu:Shrugging}
4. Natso SweetLou [derek]: And look at you now! Character building. Quick photo for LinkedIn? "Proud to have shaped this talent."  {cu:Pointing@tick}
   - Choice: Take the photo. | Narrator [n]: He posts it in four minutes. It gets eleven likes. Nine are from the partners.
   - Choice: "Redo it yourself, Lou." | Natso SweetLou [derek]: Ha! Ha. Great bit. We'll do it later.

## c4_ice — Chapter 4 · The Finnish Ice (tv)

1. Narrator [n]: One Time! Cup, $25,000. Pat Finny hasn't moved in nine minutes. Not his hands. Not his eyes. Possibly not his heart.  {wide^:Looking Around~tension}
2. Pat Finny [L:antionius]: Your move.  {ecu:Thinking}
3. Narrator [n]: You try it. Stillness. Breath through the nose. Hands flat. The camera finds nothing to love.  {push^:Thinking@hush}
4. Pat Finny [L:antionius]: Better. Next time, also stop blinking.  {cu:Head Nod Yes@tick}

## c4_race — Chapter 4 · Player of the Year (tv)

1. Nick "Baby Goat" Twogloves [host]: Let's look at the Player of the Year race! Up top, as always, Shane Dawb. Then the Tour Machine, Alex Foxglove. Mark Holdz in third, because he played every event on Earth.  {wide:Pointing@flash~grind}
2. Shane Dawb [L:martini]: Another one for the cabinet. The cabinet's getting crowded. I might need a second cabinet.  {cu:Talking}
3. Alex Foxglove [L:foxglove]: Leaderboard points are points. You want in on the race, you play the schedule. All of it.  {two:Talking>Head Nod Yes}
4. Mark Holdz [L:addemup]: Shove.  {cu:Pointing@sting}
5. Nick "Baby Goat" Twogloves [host]: That was the whole interview. Mark Holdz, ladies and gentlemen. And, new on the board in fourteenth, the cubicle kid.  {two:Clapping@flash}

## tour1 — Chapter 4 · The interview (tv)

1. Nick "Baby Goat" Twogloves [host]: Champion! Fifty thousand people on the stream. How does it feel?  {two:Pointing@flash~triumph}
   - Choice: "People insist on calling it luck." | Nick "Baby Goat" Twogloves [host]: THERE it is. The chat is going absolutely crazy.
   - Choice: "I just tried to make good decisions." | Nick "Baby Goat" Twogloves [host]: Humble AND dangerous. Worst combination for everyone else.
2. Kristen Foxglove [L:kfoxglove]: Nice result. Now everyone has a file on you. Welcome to the Tour.  {cu:Pointing>Head Nod Yes}

## tour2 — Chapter 4 · The envelope (tv)

1. Narrator [n] (win): Two Tour titles. Three quarters of a million dollars. A trophy shaped like a crown, which is a lot even for television.  {low^:Victory@crowd~triumph}
2. Nick "Baby Goat" Twogloves [host]: And somebody left this at the desk for you. Black envelope. Embossed. Very James Bond.  {cu:Pointing}
3. Narrator [n]: Inside: the Titan. Invitation only. Buy-ins from one hundred thousand dollars.  {insert@sting}
4. Slick [slick] (text): Those rooms don't have scared people in them. Bring the part of you that isn't scared either.

---

# CHAPTER 5 — HIGH ROLLER

Goal: win two Titan titles and build a $5,000,000 bankroll.

## c5 — Chapter 5 · High Roller (titan)

1. Narrator [n]: A private terminal. A plane with your name on the manifest. A buy-in that used to be a house.  {ext~grind}
2. Narrator [n]: Nobody in this room is scared. That's the problem with it.  {wide^:Looking Around~tension}
3. Mustich [trent]: Funny. Dad stopped staking me last month. Said I needed to "find out what it costs". So I guess we're both self-made now.  {two:Shrugging}
4. You [you]: Welcome to the club. The dues are terrible.  {cu:Talking}
5. Linus Loophole [L:loophole]: I reviewed your last final table. Your bubble frequencies are... acceptable. Your river frequency is off by four percent.  {ots:Thinking}
6. Slick [slick] (text): In there nobody's scared. Everyone knows the maths. So the edge is in the spots they don't study. Find them.
7. David Pec [mom] (text): a HOUSE. you bought in for a HOUSE. im not telling your mother
8. Narrator [n]: Goal: win two Titan titles and build a $5,000,000 bankroll.  {low^@swell~grind}

## c5_money — Chapter 5 · A piece of you (titan)

1. Narrator [n]: The hotel's top-floor bar. Bryan Dartfrog drinks sparkling water and talks about seven-figure buy-ins the way other people talk about parking.  {ext~night}
2. Bryan Dartfrog [L:kenny]: The $250K is next week. You've got the skill. You don't have the roll. I'll buy half your action. Fair price.  {two:Talking}
3. You [you]: And if I win?  {cu:Thinking}
4. Bryan Dartfrog [L:kenny]: Then I win half. And you win the other half, plus you got to play the biggest game on the planet like it was a one-two.  {cu:Shrugging}
   - Choice: Sell him half. | Bryan Dartfrog [L:kenny]: Smart. Nice stack. I'll take half.
   - Choice: "I'll play what I can afford." | Bryan Dartfrog [L:kenny]: Respect. Most people learn that after the house, not before.
5. Slick [slick] (text): Selling action isn't weakness. Playing above your roll is. Never confuse the two.

## c5_gym — Chapter 5 · 5 AM (titan)

1. Narrator [n]: 5:02 AM. The hotel gym. Someone has been on the treadmill since before you were awake.  {ext~grind}
2. Jay Cocoon [L:kahoon]: Five AM gym, six AM study, Titans by dinner. You up early, or up late?  {two:Talking}
3. You [you]: Up late.  {cu:Shrugging}
4. Jay Cocoon [L:kahoon]: That's the difference between us. Not talent. I've got talent too. Discipline beats talent. I've got both.  {push:Pointing@sting}
5. Jay Cocoon [L:kahoon]: Get on the bike. Ten minutes. Your decisions after hour eight will thank you.  {two:Head Nod Yes@tick}

## c5_lab — Chapter 5 · The lab (titan)

1. Narrator [n]: A conference room turned war room. Two laptops, a whiteboard of hand ranges, and the two most precise players alive arguing about a three percent frequency.  {wide^:Looking Around}
2. Fedor Holdum [L:holdz]: I studied this exact spot this morning. River, four-bet pot, ninety big blinds. You check more than you should.  {cu:Pointing}
3. Linus Loophole [L:loophole]: He checks four percent more than he should. Precision, Fedor.  {ots:Thinking@tick}
4. You [you]: How do you two ever lose?  {cu:Shrugging}
5. Fedor Holdum [L:holdz]: We play each other.  {two:Shrugging}
6. Narrator [n]: Everyone in this building has the same tools. Some of them have the same thoughts.  {push^:Thinking@hush}

## lesson5 — A lesson · The spots nobody studies (titan)

1. Narrator [n]: A room where everybody knows the maths. You lost anyway.  {wide^:Thinking~tension}
2. Slick [slick] (text): They all have the same solver. So where's the edge?
3. You [you] (text): There isn't one.
4. Slick [slick] (text): There's one in every spot they don't study. Odd stacks. Four-bet pots. Limped pots with antes. The book is thin there. Be the one who read it.
5. Slick [slick] (text): And one more spot nobody studies: the person across from you at hour eleven. The book is thinnest there.

## c5_prof — Chapter 5 · The Professor (titan)

1. Narrator [n]: The $50,000 Omaha. Four cards each, and every hand a tangle of possibilities. Isaac Maskedman is untangling them faster than you can see them.  {wide^:Looking Around~tension}
2. Isaac Maskedman [L:hackston]: Interesting sizing. Incorrect, but interesting.  {cu:Thinking}
3. You [you]: What would you have bet?  {cu:Talking}
4. Isaac Maskedman [L:hackston]: Two-thirds pot. But I'd have known why. You bet two-thirds of something you felt. Feel less. Count more.  {push:Pointing@sting}

## c5_chives — Chapter 5 · Genuinely (titan)

1. Narrator [n]: The $250,000. You lose the biggest pot of your life to a two-outer on the river. The room goes quiet in the polite way rich rooms go quiet.  {high^:Sad Idle@hush~tension}
2. Stephen Chives [L:chillwick]: That's brutal. I'm sorry. Genuinely.  {two:Talking}
3. Narrator [n]: He means it. In a room full of people trying to take everything you have, someone means it.  {push^:Thinking@swell}
4. Stephen Chives [L:chillwick]: Final table every week, they say about me. They don't mention the weeks I didn't. Good luck tomorrow. Genuinely.  {cu:Head Nod Yes}

## titan1 — Chapter 5 · The Titan trophy (titan)

1. Narrator [n] (win): A trident-shaped trophy, a wire transfer with too many zeros, and a room full of the best players alive nodding at you.  {low^:Fist Pump@crowd~triumph}
2. Mustich [trent]: Congrats. Honestly. Turns out you can't buy what you've got. Believe me, I asked.  {two:Shaking Hands 2>Shaking Hands 2@swell}
3. You [you]: Took you long enough to figure that out.  {cu:Shrugging}
4. Mustich [trent]: Ha. Rematch at the Main?  {cu:Pointing@tick}

## c5_score — Chapter 5 · Not here (titan)

1. Nick "Baby Goat" Twogloves [host]: Big question from the chat: where's Bio "The Ghost"? Biggest buy-ins in the world, and the GOAT is missing.  {two:Pointing@flash}
2. Kristen Foxglove [L:kfoxglove]: Bio doesn't play the Titan. Never has.  {cu:Shrugging}
3. Nick "Baby Goat" Twogloves [host]: Why not?  {cu:Talking}
4. Kristen Foxglove [L:kfoxglove]: He says money isn't the score. Bracelets are. And there's only one he still cares about every year.  {push:Talking@sting~tension}

## titan2 — Chapter 5 · The last flight (diner)

1. Narrator [n]: Five million dollars. Two tridents. And you're back on a stool at the Lucky Seven, because some things don't change when your bank balance does.  {ext~night}
2. Slick [slick]: The Main Event. July. Eight thousand players.  {two:Talking}
3. You [you]: He'll be there.  {cu:Thinking}
4. Slick [slick]: He's always there. / So am I, this year. I bought a ticket. First time in ten years.  {push:Talking@sting}
5. You [you]: You're coming to Vegas?  {cu:Thinking}
6. Slick [slick]: Somebody has to buy your pie.  {ecu:Head Nod Yes@swell~night}

---

# CHAPTER 6 — THE MAIN EVENT

Goal: win the Main Event.

## c6 — Chapter 6 · The Main Event (main)

1. Narrator [n]: Every July: eight thousand players, ten thousand dollars each. / One of them ends up on the cover of every poker magazine in the world.  {ext@crowd~grind}
2. Narrator [n]: Day 1. A ballroom the size of an airport. Two hundred tables, and somewhere in the noise, a seat with your name on it.  {wide^:Looking Around}
3. Miles "The Snowplow" Gnomey [rosa]: Main Event floor! They gave me the Main! Table one-eighty-eight, seat two. Seat open!  {two:Pointing@tick}
4. Michael Mightyrocky [L:mizrockie]: Defending champ. If you want it, you'll have to take it from me.  {cu:Pointing@sting}
5. Bio "The Ghost" [ghost]: Still here, kid. So am I.  {ecu:Talking~tension}
6. Narrator [n]: Goal: win the Main Event.  {low^@swell}

## c6_day1 — Chapter 6 · Gorilla mode (main)

1. Narrator [n]: Day 1, level four. The player in seat nine has won nine titles this year, and he has decided the entire table is his personal property.  {wide^:Looking Around~tension}
2. Jesse Lonestar [L:lonestar]: GORILLA. MODE.  {cu:Fist Pump@sting}
3. Narrator [n]: He raises eleven hands in a row. On the twelfth you re-raise. He thinks. He folds. He grins.  {hands@chips}
4. Jesse Lonestar [L:lonestar]: There it is! Somebody at this table has a pulse! Nine wins this year, and the ones I remember are the ones somebody fought me for.  {two:Clapping}

## c6_hall — Chapter 6 · The hallway (main)

1. Narrator [n]: Day 5. The service hallway behind the ballroom. Linen carts, a vending machine, and Mustich, sitting on the floor in a five-thousand-dollar jacket.  {wide^:Sad Idle~night}
2. Mustich [trent]: Bio. Of course it was Bio. He didn't even look at my cards. He looked at me like I was... furniture.  {cu:Shaking Head No}
3. You [you]: He does that.  {cu:Talking}
4. Mustich [trent]: Dad's watching on the stream. Probably already writing the email. "Find out what it costs."  {push:Thinking@hush}
5. Narrator [n]: You sit down on the floor next to him. You're not friends. You're not anything. You sit anyway.  {two^:Sad Idle@swell}
6. Mustich [trent]: Go win it. If anyone's taking him down, I want it to be someone who knows what this hallway feels like.  {cu:Pointing>Head Nod Yes}

## c6_champ — Chapter 6 · Take it from me (main)

1. Narrator [n]: Two tables left. The defending champion, three seats to your right, has been grinding since Day 1 without a single bad day.  {wide^:Looking Around~tension}
2. Michael Mightyrocky [L:mizrockie]: You know they call me The Grinder? It's not because I'm fancy. It's because I'm still here when the fancy people go home.  {cu:Talking}
3. Narrator [n]: The biggest pot of the tournament so far. His crown against your stack.  {hands@chips}
4. Michael Mightyrocky [L:mizrockie] (bust): Good. Good hand. I mean it. Take it the whole way, or I'll be mad I didn't.  {two:Shaking Hands 2@swell}

## c6_night — Chapter 6 · 2:13 AM, Las Vegas (diner)

1. Narrator [n]: The night before the final table. A 24-hour diner off Flamingo Road. Different city. Same hour.  {ext~night}
2. Narrator [n]: He's already on a stool. Dark glasses. Two slices of pie.  {wide^:Looking Around@hush}
3. Slick [slick]: Ten years since I set foot in this city. The pie's worse.  {two:Talking}
4. You [you]: If it comes down to him and me. If he shoves the river.  {cu:Thinking~tension}
5. Slick [slick]: Then it won't be about the cards. He'll look at you the way I looked at him. He's waited ten years to put somebody in the seat he was in.  {push:Talking}
6. You [you]: What did you see in him that night?  {cu:Talking}
7. Slick [slick]: Doubt. He didn't doubt his hand. He doubted he was allowed to win. / Don't give him that. Whatever you decide, decide it like you're allowed.  {ecu:Pointing@sting}
8. Narrator [n]: He pays for both slices. He doesn't leave. For the first time, he stays until you do.  {wide^@swell~night}

## c6_rail — Chapter 6 · The rail (main)

1. Narrator [n]: The final table. A stage, a wall of lights, and a rail packed three deep.  {ext@crowd~grind}
2. David Pec [mom]: SIT UP STRAIGHT!  {wide:Pointing@crowd}
3. Narrator [n]: Not a text. David, in person, with a sign that says "PEOPLE INSIST ON CALLING IT LUCK" in a handwriting you'd know anywhere.  {push^:Clapping@swell}
4. Tommy "The Shark" Maio [lou]: Thirty-one years. Second time I ever left Biscayne for a game. Somebody had to come make sure you don't get cocky.  {two:Head Nod Yes}
5. Narrator [n]: Meanwhile, at a sports bar in Brickell, a man in a Halvorsen & Pike quarter-zip tells the bartender he used to manage you.  {cu^:Talking@tick}
6. Nick "Baby Goat" Twogloves [host]: From the booth, live, worldwide: welcome to the final table of the WCoC Main Event.  {two:Pointing@flash}

## c6_final — Chapter 6 · The river (main)

1. Narrator [n]: Heads-up. Bio "The Ghost" and you. Every chip in the building is on one table.  {wide^:Looking Around~tension}
2. Nick "Baby Goat" Twogloves [host]: Folks, this is the spot. River. The Ghost moves all in. And if you're old enough... you've seen this movie before.  {two:Pointing@hush}
3. Narrator [n]: He's not looking at the board. He's looking at you, the way a man in dark glasses once looked at him.  {ecu^:Thinking}
4. Narrator [n]: On the rail, a man in dark glasses takes them off.  {cu^@sting}
5. Narrator [n]: You don't flinch. Whatever you do next, you decide it like you're allowed.  {push^:Thinking@swell}
   - Choice: Call. | Narrator [n]: You call. Both hands go face up. The cards decide. → champ / c6_lost
   - Choice: Fold. | Narrator [n]: You fold. Not fast. Not slow. Like someone who decided. → c6_fold

## c6_fold — Chapter 6 · Not this one (main)

1. Nick "Baby Goat" Twogloves [host]: The kid lets it go! And the Ghost... doesn't show. Of course he doesn't show.  {two:Pointing@flash~tension}
2. Bio "The Ghost" [ghost]: You thought about it. Good. Most people only think about it afterwards.  {cu:Talking}
3. Narrator [n]: On the rail, Slick puts his glasses back on. The match isn't over. Neither are you.  {push^:Thinking@swell}

## champ — World Champion (main)

1. Narrator [n] (win): Confetti. Cameras. A bracelet the size of a doorknob. / Somewhere, a fourteen-year-old with a fuzzy TV is losing his mind.  {wide^:Victory@crowd~triumph}
2. Bio "The Ghost" [ghost]: Good game, kid.  {two:Shaking Hands 2>Shaking Hands 2@hush~none}
3. Narrator [n]: He takes off the mask. For the first time anyone can remember, Bio "The Ghost" smiles.  {ecu@swell}
4. Narrator [n]: Then he looks past you, to the rail, and nods at a man he hasn't spoken to in ten years. / Slick nods back.  {push^:Head Nod Yes@swell}
5. Bio "The Ghost" [ghost]: Now you'll find out the hard part. Everyone will be hunting you.  {cu:Pointing~tension}
6. Natso SweetLou [derek] (text): Hey buddy! Long time! Saw you on TV. Listen, about that promotion...
7. Narrator [n]: You leave him on read.  {cu^:Texting While Standing@tick}
8. Slick [slick]: People insist on calling it luck.  {push:Talking@swell~triumph}
9. You [you]: Let them.  {low:Head Nod Yes@sting}

## c6_lost — Chapter 6 · Next year (main)

1. Narrator [n] (bust): Second in the world. Somebody else's confetti.  {high^:Sad Idle@hush~night}
2. Bio "The Ghost" [ghost]: You didn't flinch. That's rarer than winning. Come back next July.  {two:Shaking Hands 2}
3. Slick [slick]: One hand, kid. Over ten thousand, they pay whoever studied. You studied.  {push:Talking@swell}

---

# LEGACY — THE HALL OF LEGENDS

Goal: beat every legend and pass Bio "The Ghost"'s fourteen bracelets.

## c7 — Legacy · The Hall of Legends (hall)

1. Narrator [n]: Your picture hangs in the Hall of Legends now, next to people you used to watch on a fuzzy TV.  {ext@swell~triumph}
2. Narrator [n]: Stu. Colt. Chance. The Ghost. Twenty-seven legends on these walls, and a kid from Floor 34 who got told to redo the Henderson deck.  {wide^:Looking Around}
3. Bio "The Ghost" [ghost]: Fourteen bracelets. That's the number. Come take it.  {low:Pointing@sting~tension}
4. Slick [slick]: There's nobody left to teach you. So teach somebody. I sat on that stool once too, remember.  {two:Talking>Head Nod Yes@swell~night}
5. You [you]: I remember.  {cu:Head Nod Yes}
6. Narrator [n]: Goal: beat every legend and pass Bio "The Ghost"'s fourteen bracelets.  {push^@swell}

## c7_gauntlet — Legacy · The challenges (hall)

Each legend sends a heads-up challenge card, one line each. These play as the legend matches in the game.

1. Stu Hungar [L:hungar] (text): Kid, I've seen this hand before. Let's see if you have.
2. Colt the Cowboy [L:brunswick] (text): Bring your ten-deuce, son. I'll bring mine.
3. Chance Cage [L:chance] (text): Take your time. I'll be waiting.
4. Armadillo Fatts [L:armadillo] (text): I've got a new story. It's about you. Come hear the ending.
5. Phil Hellraiser [L:hellbent] (text): Bracelet eighteen is coming. You'll be a nice footnote.
6. GG Negroni [L:negrono] (text): I already know your cards. Want to make it official?
7. Erik Seikick [L:sidle] (text): ...
8. Tom Duhhz [L:swan] (text): I have air. Want to find out?
9. Pat Finny [L:antionius] (text): Your move.
10. Michael Mightyrocky [L:mizrockie] (text): Rematch. I'm still here.

The other seventeen use their `data.js` lines, unchanged.

## c7_rival — Legacy · Still circling (hall)

1. Narrator [n]: The Hall of Legends Invitational, final table. Seat five, in a jacket he paid for himself this time: Mustich.  {wide^:Looking Around~tension}
2. Mustich [trent]: Saw your portrait in the lobby. Very moving. They got your bad side.  {cu:Shrugging@tick}
3. Mustich [trent]: Dad sold the company. No more stake, no more rail. Turns out I still want to beat you. Annoying, isn't it?  {ots:Talking}
4. You [you]: For you? Very.  {cu:Shrugging}
5. Mustich [trent]: Fifteen is just a number, Florida. Somebody's going to take it off you. I'd like it to be someone you can't stand.  {push:Pointing@sting}
6. Narrator [n]: He grins. The same grin from the Biscayne card room, the first night. You'd miss it if it ever went away. You'll never tell him that.  {two^:Thinking@swell}

## c7_wedding — Legacy · The best man (home)

1. Narrator [n]: A reception hall in Hialeah. Fairy lights, a DJ who plays one song too many, and David Pec in a tuxedo he's been talking about since March.  {ext@crowd~triumph}
2. David Pec [mom]: My best man is a world champion. I told everyone. Everyone. The caterers know.  {two:Clapping}
3. You [you]: I missed your engagement party for a five-thousand-dollar event.  {cu:Talking}
4. David Pec [mom]: And you cashed for eleven grand and sent us to Cancún. Bro. I forgave you in the airport.  {cu:Shrugging@tick}
5. Narrator [n]: You stand up to give the speech. You had cards. You put them away.  {low^:Thinking@swell}
6. You [you]: The first person who ever believed I could do this wasn't a pro. He texted in all caps, and he was right.  {push:Talking@swell~triumph}

## c7_portrait — Legacy · The missing frame (hall)

1. Narrator [n]: The Hall's annual dedication. Twenty-seven frames, and one blank space on the wall that's been there for ten years. Nobody remembers why.  {wide^:Looking Around~tension}
2. Miles "The Snowplow" Gnomey [rosa]: Tournament director, Hall of Legends Invitational. Me! From the cage at Biscayne! Ladies and gentlemen, take your seats. Seats open!  {two:Pointing@crowd}
3. Narrator [n]: The curator lifts a cloth from the blank space. A young man in dark glasses, ten years ago, holding the world championship trophy.  {insert@sting}
4. Slick [slick]: I asked them to leave it out. I didn't want to be the champion who went broke a year later.  {cu:Thinking}
5. Bio "The Ghost" [ghost]: You were the only one who ever made me think for four minutes. Ten years I've worn this thing because of you.  {two:Talking@swell}
6. Narrator [n]: Two men shake hands under a portrait neither of them wanted to see. / The room doesn't know why it's clapping. It claps anyway.  {push^:Shaking Hands 2@crowd~triumph}

## c7_fifteen — Legacy · Fifteen (hall)

1. Narrator [n] (win): Fifteen bracelets. One more than the Ghost.  {low^:Victory@crowd~triumph}
2. Bio "The Ghost" [ghost]: Fifteen. I knew it'd be you the night you stopped looking at me in Vegas and looked at your cards.  {two:Shaking Hands 2>Shaking Hands 2@swell}
3. Tommy "The Shark" Maio [lou] (text): Kid! They put MY name on the board at Biscayne! Classic champion! Thirty-two years! Tell Slick he owes me a coffee.
4. Natso SweetLou [derek] (text): The partners want to name the new conference room after you!! Swing by for the ribbon cutting? Also do you have the Henderson login
5. Narrator [n]: You don't answer either text yet. There's somewhere you have to be.  {cu^:Texting While Standing@tick}

## c7_end — Legacy · 2:13 AM (diner)

1. Narrator [n]: Lucky Seven Diner. 2:13 AM. A kid at the counter is staring at the pie like it owes them money.  {ext~night}
2. You [you]: You're doing the thing where you stare at the pie like it owes you money.  {two:Talking}
3. Narrator [n]: You slide a red envelope across the counter. Inside: a seat card, and one line in pen.  {hands@tick~tension}
4. Narrator [n]: "People insist on calling it luck."  {ecu^:Thinking@hush}
5. Narrator [n]: Let them.  {push^@swell~triumph}
6. Narrator [n]: THE DREAM.  {wide^@sting~none}

---

## Notes for building it (later, after Steven has read it)

- **Lines that change with your choices:**
  - c3_party: David's later lines depend on the choice. Track `$davidParty` in Yarn. If you stayed, c7_wedding line 4
    stands. If you flew home, swap it for "You flew home for my party. In the middle of the Summer. Bro."
  - c4_lou: the choice sets `$louPhoto`. If set, Lou's Legacy text adds "the LinkedIn post is still doing numbers".
  - c5_money: selling action sets `$soldTitan`. If set, titan1 gets one extra line from Bryan Dartfrog: "Nice. Half
    of that is mine. Worth every cent."
- **The last river of the Main** (`c6_final`) is dealt live: the player calls or folds. Call: the cards decide
  (`champ` or `c6_lost`). Fold: `c6_fold`, and the heads-up match plays on to its end.
- **The quit/keep-the-job thread from Chapter 1 carries on:**
  - "Moonlighter" players get `derek_job` / `quit250` (from the old game) during Chapter 2.
  - By the Titan, everyone has quit.
- **Every legend scene uses the legend's own `line` from `data.js` at least once.** The text above follows that.
- **Shots and acting clips** follow ART_DIRECTION: ext, then wide, then two and cu/ots, then push, then ecu. Save one
  push-in for the turning line of each scene. The big turns are marked @sting or @swell.
- **New voices needed:** narrator, Slick, David, Mustich, Tommy, Gnomey, Lou, Nick and Bio already exist. Each legend
  needs one Piper voice from the legend pool.
