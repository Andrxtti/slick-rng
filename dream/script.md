# The Dream — script

Edit the words after the colon. Keep the numbers and the speaker ids in brackets as they are; a line that starts with `Choice:` is a button the player taps (the arrow says where it leads; after `|` is the reply it gets).
`[n]` is the narrator, `[you]` is the player. `/` in a line is a line break on screen. Texts on the phone are marked (text).
The braces after a line are the shot list (unity/tools/direction.py: shot, who moves how, the sting, the score); leave them or delete them, the words are what matter.

## p_office — Prologue · The 9-to-5 (office)

1. Narrator [n]: Monday. 6:47 PM. Floor 34, Brickell. / Everyone else went home an hour ago. Out the glass, the bay is going pink.  {ext~night}
2. Natso SweetLou [derek]: The Henderson deck. Redo it. All of it. On my desk by eight.  {ots:Pointing}
3. You [you]: I sent it Friday. You signed off on it.  {cu:Talking}
4. Natso SweetLou [derek]: And now I'm un-signing off on it. Problem?  {cu:Standing Arguing}
5. You [you] (quiet): No problem, Lou.  {ecu:Head Nod Yes}
6. Natso SweetLou [derek]: Great. Oh, and I told the partners the new model was my idea. Team player, right?  {ots:Pointing}
7. Narrator [n]: You were nine the first time you saw it on TV. / A final table under hot lights. A man in dark glasses pushed every chip he had into the middle with nothing.  {wide^:Thinking@swell}
8. Narrator [n]: The room went silent. His opponent folded. He never showed. / He made it look like he could see the future.  {push^:Thinking}
9. You [you] (think): Somewhere along the way I traded that for a lanyard.  {ecu:Thinking@sting}

## p_diner — Prologue · Lucky Seven Diner (diner)

1. Narrator [n]: 2:13 AM. The deck is done. / You are not.  {ext~night}
2. Stranger [slick]: You're doing the thing where you stare at the pie like it owes you money.  {two:Talking}
3. You [you]: Long day.  {cu:Shrugging}
4. Stranger [slick]: Long life, by the look of it. What do you do?  {ots:Talking}
5. You [you]: Spreadsheets. For a guy who takes credit for them.  {cu:Talking}
6. Stranger [slick]: And what did you want to do?  {push:Talking~none}
    - Choice: Play poker. For real. | Stranger [slick]: Ha. Everybody says that at 2 AM. Almost nobody means it.
    - Choice: Doesn't matter now. | Stranger [slick]: It always matters. You just stopped saying it out loud.
7. Narrator [n]: He slides a red envelope across the counter.  {hands@tick~tension}
8. Stranger [slick]: The Biscayne Championship. Saturday, noon. Two-fifty seat. It's paid.  {cu:Pointing}
9. You [you]: Why would you give that to a stranger?  {cu:Thinking}
10. Stranger [slick]: Because I sat on that stool once. Somebody bought my pie too.  {push:Talking@swell}
11. Stranger [slick]: Three things. Fold the junk. Pressure the scared ones. And near the money, think about what the money's worth, not the chips.  {cu:Pointing>Head Nod Yes}
12. You [you]: Who are you?  {ots:Talking}
13. Slick [slick]: People call me Slick.  {ecu:Talking@sting}
14. Narrator [n]: He drops cash for both checks and is gone before the bell over the door stops ringing. / Inside the envelope: a seat card, and one line in pen.  {wide^:Looking Around@bell}
15. Narrator [n]: “People insist on calling it luck…”  {ecu^:Thinking@swell~night}

## p_after — Prologue · Monday again (office)

1. Natso SweetLou [derek]: You look different. Did you finish the Henderson numbers?  {ots:Pointing}
2. Narrator [n]: (built at runtime)  {cu^:Thinking}
3. Natso SweetLou [derek]: Hello? I'm talking to you. Weekend's over, champ.  {cu:Angry Gesture@tick}
4. Narrator [n]: Nobody says you have to decide forever. Keep the paycheck and play around it, or walk now and bet on yourself. You can always quit later.  {two}
5. Narrator [n]: Decision time.  {push^:Thinking@sting~none}
    - Choice: Quit. Today. → p_quit
    - Choice: Keep the job. Grind nights and weekends. → p_keep

## p_quit —  (office)

1. You [you]: Lou? Redo it yourself.  {low:Pointing@sting~grind}
2. Narrator [n]: The elevator ride down is the best thirty-four floors of your life. / No paycheck. No plan B.  {ext@swell~grind}
3. Slick [slick] (text): Heard you walked out. I put $5,000 in your account. Not a loan. Don't make me regret it.
4. Slick [slick] (text): Rent is due on the first. Play three games a day, max. Never play above your roll.
5. David Pec [mom] (text): BRO. YOU QUIT YOUR JOB?? CALL ME

## p_keep —  (office)

1. Narrator [n]: You keep the lanyard. / But tonight you open a new spreadsheet and name it BANKROLL.  {high^:Thinking~night}
2. Narrator [n]: $2,000 lands every Friday. Days for Natso SweetLou. Three sessions a week for the dream.  {cu^:Texting While Standing}
3. Slick [slick] (text): Heard about Saturday. Smart. A paycheck buys you time to learn. Never play above your roll. Text me when you win something.

## c1 — Chapter 1 · The Grind (cardroom)

1. Narrator [n]: Biscayne Card Room, off the causeway. Carpet from 1994, palms out front, coffee from this morning, maybe. / Twenty tables, every one of them a classroom.  {ext~grind}
2. Miles "The Snowplow" Gnomey [rosa]: New face. Sign the list. One-two has a seat in ten. Nightly turbo kicks off at seven.  {two:Pointing}
3. Tommy "The Shark" Maio [lou]: Hey, you're the kid from Saturday. Don't get cocky. This room eats dreamers for breakfast.  {ots:Talking>Head Nod Yes}
4. Mustich [trent]: Cute hoodie. Did it come with the free seat?  {cu:Shrugging@tick}
5. Narrator [n]: Mustich. Dad's money, a solver subscription, and a smile you want to wipe off. / He's been circling the Biscayne Classic all year.  {push:Looking Around}
6. Narrator [n]: Goal: win the Biscayne Classic, or build your bankroll to $10,000.  {wide^@swell}

## lesson1 — A lesson · Fold the junk (diner)

1. Narrator [n]: Lucky Seven. 1:40 AM. The pie is the same. You are a little poorer.  {high^:Sad Idle@hush~night}
2. Slick [slick]: Tell me the hand that got you.  {two:Talking}
3. You [you]: King-jack, early position. I raised. Got three-bet. Called. Flopped a jack. Lost everything to kings.  {cu:Shaking Head No}
4. Slick [slick]: Then the hand didn't get you. The first decision did. From early, king-jack is a fold. Everything after was a story you told yourself.  {push:Pointing@sting}
5. Slick [slick]: Fold the junk. That's not advice, it's arithmetic. Your misses are on the Spacepad. Drill them until the fold is boring.  {cu:Pointing>Head Nod Yes@swell}

## trentbeat — Chapter 1 · The Biscayne Classic (cardroom)

1. Narrator [n] (win): The last card hits the felt. The Biscayne Classic is yours.  {wide^:Victory@crowd~triumph}
2. Mustich [trent]: Enjoy it. Dad says the Classic is a rounding error.  {cu:Shrugging>Fist Pump}
3. Tommy "The Shark" Maio [lou]: Don't listen to him, kid. Thirty years I've been chasing that trophy. You earned it.  {ots:Head Nod Yes@swell}
4. Miles "The Snowplow" Gnomey [rosa]: Your name goes on the board by the cage. Permanent marker. Congratulations.  {two:Clapping>Victory@crowd}

## firstwin — First title (diner)

1. Slick [slick]: Saw the photo. Trophy, big grin, terrible lighting. Proud of you, kid.  {two:Clapping>Victory@crowd~triumph}
2. Slick [slick]: Left something in your locker. Every champion needs a pair.  {cu:Pointing}
3. Narrator [n]: Inside: a pair of pixelated sunglasses and a note. / “Deal with it.”  {hands^@swell}

## c2 — Chapter 2 · The Circuit (casino)

1. Slick [slick]: You've outgrown Biscayne. The Circuit stops at Silver Strike every couple of months. Every event gives a ring. Check the calendar.  {ext~grind}
2. Narrator [n]: Bigger rooms. Better players. A guy at the rail is wearing four rings on one hand.  {wide^:Looking Around}
3. Chris Schillings [L:moneybaker]: You're the kid who quit the cubicle? I was an accountant. Spreadsheets and everything. Play your game and let them find out.  {two:Talking>Head Nod Yes}
4. Mustich [trent]: Look who followed me to the Circuit. Try not to bust on the bubble this time.  {cu:Pointing@tick}
5. Narrator [n]: Goal: win two Circuit rings and build a $40,000 bankroll.  {wide^@swell}

## lesson2 — A lesson · Pressure the scared ones (casino)

1. Narrator [n]: The Circuit floor at midnight. Chips being racked all around you. None of them yours.  {wide^:Looking Around~night}
2. Slick [slick]: You check too much.  {ots:Talking}
3. You [you]: I didn't have anything.  {cu:Shrugging}
4. Slick [slick]: Neither did they. On a dry board the raiser bets small and often, and everybody without a pair goes away. You gave them a free card and a free look at you.  {push:Talking@sting}
5. Slick [slick]: Pressure the scared ones. Small bets, lots of them. It's in your misses. Go run them.  {cu:Pointing>Head Nod Yes@swell}

## ring1 — Chapter 2 · First ring (casino)

1. Narrator [n]: A tournament director slides a heavy ring across the table. It barely fits. You put it on anyway.  {hands^:Fist Pump@chips~triumph}
2. Chris Schillings [L:moneybaker]: First ring? I still remember mine. Don't put it in a drawer. Wear it every day you play.  {two:Pointing>Head Nod Yes@swell}
3. Slick [slick] (text): One ring. Now go get another one.

## c3 — Chapter 3 · The Summer (vegas)

1. Narrator [n]: Every June, Las Vegas hosts the WCS Summer Series. 112 degrees outside, 68 inside, ten thousand people chasing the same gold. It's on your calendar now.  {ext~grind}
2. Slick [slick]: Every kid who ever loved this game dreams about a bracelet. This is the room where they hand them out.  {two:Pointing}
3. Narrator [n]: At the feature table sits a man in dark glasses. Fourteen bracelets. Nobody has seen him smile. / The man from TV. The reason you're here.  {cu^:Looking Around@hush~tension}
4. Bio The Ghost [ghost]: Kid. Stop looking at me and look at your cards.  {ecu:Talking@sting}
5. Phil Hellraiser [L:hellbent]: Another internet kid. I'll see you at the final table. Probably not, though.  {ots:Shrugging}
6. Narrator [n]: Goal: win a gold bracelet.  {low^@swell~grind}

## lesson3 — A lesson · What the money's worth (vegas)

1. Narrator [n]: Two hundred and eleven players left. Two hundred get paid. You were two hundred and twelve.  {high^:Sad Idle@hush~tension}
2. Slick [slick]: You called a shove with ace-ten on the bubble.  {ots:Talking}
3. You [you]: I was ahead of his range.  {cu:Standing Arguing}
4. Slick [slick]: Ahead of his range in chips. Behind in money. Near the bubble, the chips you lose are worth more than the chips you win. That is the whole of ICM.  {push:Pointing@sting~night}
5. Slick [slick]: Think about what the money's worth. Not the chips. The spot's on your Spacepad. Drill it till it stops hurting.  {two:Talking>Head Nod Yes@swell}

## bracelet1 — Chapter 3 · Gold (vegas)

1. Narrator [n] (win): They fasten the bracelet around your wrist and the room starts clapping before you realize it is for you. / Gold. Real gold. Your name in the record book forever.  {low^:Victory@crowd~triumph}
2. Phil Hellraiser [L:hellbent]: Lucky. If I had been at that final table, we'd be talking about my bracelet number eighteen.  {cu:Shrugging@tick}
3. David Pec [mom] (text): IS THAT REAL GOLD?? THE WHOLE GROUP CHAT SAW IT
4. Slick [slick] (text): Nine-year-old you is screaming right now. Let him.

## c4 — Chapter 4 · The Tour (tv)

1. Narrator [n]: A soundstage in Hollywood, Florida. Three cameras, a hole-card reader, and forty thousand people watching you blink.  {ext@flash~grind}
2. Dex Marlow [host]: Welcome back to the feature table! Joining us: the cubicle kid with a bracelet, and people are calling it a fluke.  {two:Pointing}
3. You [you]: People insist on calling it luck.  {push:Talking@swell}
4. Dex Marlow [host]: Ooh. We have a quote. Producers, put that on a T-shirt.  {cu:Clapping@flash}
5. Narrator [n]: Under the lights everything shows. The rail can see your hole cards. The table can see your face.  {wide^:Looking Around}
6. Slick [slick]: Cameras change people. Don't let them change your ranges. Play the hand you'd play in an empty room.  {ots:Pointing>Head Nod Yes}
7. Kristen Foxglove [L:kfoxglove]: Cameras make people do stupid things. Don't be people.  {cu:Shaking Head No}
8. David Pec [mom] (text): youre on the STREAM. my mom is watching. MY MOM
9. Narrator [n]: Goal: win two Tour titles (Royal Tour or GLO Cup) and build a $750,000 bankroll.  {wide^@swell}

## lesson4 — A lesson · People, not boards (tv)

1. Narrator [n]: The stream is over. The chat has moved on. The hand has not.  {wide^:Looking Around~night}
2. Slick [slick]: You bluffed the one guy who never folds.  {cu:Talking}
3. You [you]: The board was perfect for it.  {cu:Shrugging}
4. Slick [slick]: Perfect for a bluff against a player who folds. He doesn't. A bluff is a bet against a person, not a board. Pick the person first.  {push:Pointing@sting}
5. Slick [slick]: Ranges, not hands. People, not boards. Drill the river spots. They're waiting for you.  {two:Pointing>Head Nod Yes@swell}

## tour1 — Chapter 4 · The interview (tv)

1. Dex Marlow [host]: Champion! Fifty thousand people are watching on stream. How does it feel?  {two:Pointing@flash~triumph}
    - Choice: "People insist on calling it luck." | Dex Marlow [host]: THERE it is. The chat is going absolutely crazy.
    - Choice: "I just tried to make good decisions." | Dex Marlow [host]: Humble AND dangerous. The worst combination for everyone else.
2. Kristen Foxglove [L:kfoxglove]: Nice result. Now everyone has a file on you. Welcome to the Tour.  {cu:Pointing>Head Nod Yes}

## c5 — Chapter 5 · High Roller (titan)

1. Narrator [n]: A private terminal. A plane with your name on the manifest. A buy-in that used to be a house.  {ext~grind}
2. Narrator [n]: Nobody in this room is scared. That's the problem with it.  {wide^:Looking Around~tension}
3. Mustich [trent]: Funny. Dad stopped staking me last month. Guess we're both self-made now.  {two:Shrugging}
4. You [you]: Welcome to the club.  {cu:Talking}
5. Linus Loophole [L:loophole]: I reviewed your last final table. Your bubble frequencies are… acceptable.  {ots:Thinking}
6. Slick [slick]: In here nobody's scared. Everyone knows the math. So the edge is in the spots they don't study. Find them.  {push:Pointing>Head Nod Yes@swell}
7. David Pec [mom] (text): a HOUSE. you bought in for a HOUSE. im not telling your mother
8. Narrator [n]: Goal: win two Titan titles and build a $5,000,000 bankroll.  {low^@swell~grind}

## lesson5 — A lesson · The spots nobody studies (titan)

1. Narrator [n]: A room where everybody knows the math. You lost anyway.  {wide^:Thinking~tension}
2. Slick [slick]: They all have the same solver. So where's the edge?  {ots:Shrugging}
3. You [you]: There isn't one.  {cu:Shaking Head No}
4. Slick [slick]: There's one in every spot they don't study. Odd stacks. Four-bet pots. Limped pots with antes. The book is thin there. Be the one who read it.  {push:Pointing@sting~night}
5. Slick [slick]: Your misses in the strange spots. That's the list now. Drill those.  {two:Talking>Head Nod Yes@swell}

## titan1 — Chapter 5 · The Titan trophy (titan)

1. Narrator [n] (win): A trident-shaped trophy, a wire transfer with too many zeros, and a room full of the best players alive nodding at you.  {low^:Fist Pump@crowd~triumph}
2. Mustich [trent]: Congrats. Honestly. Turns out you can't buy what you've got.  {two:Shaking Hands 2>Shaking Hands 2@swell}
3. You [you]: Took you long enough to figure that out.  {cu:Shrugging}
4. Mustich [trent]: Ha. Rematch at the Main?  {cu:Pointing@tick}

## c6 — Chapter 6 · The Main Event (main)

1. Narrator [n]: Every July: eight thousand players, ten thousand dollars each. / One of them ends up on the cover of every poker magazine in the world.  {ext@crowd~grind}
2. Narrator [n]: Day one. A ballroom the size of an airport. Two hundred tables, and somewhere in the noise, a seat with your name on it.  {wide^:Looking Around}
3. Michael Mizrockie [L:mizrockie]: Defending champ. If you want it, you'll have to take it from me.  {cu:Pointing@sting}
4. Bio The Ghost [ghost]: Still here, kid. So am I.  {ecu:Talking~tension}
5. David Pec [mom] (text): BRO YOU ARE ON TV?? SIT UP STRAIGHT
6. Slick [slick]: Nine-year-old you watched this on a fuzzy TV. Go show him how it ends.  {push:Talking@swell~grind}
7. Narrator [n]: Goal: win the Main Event.  {low^@swell}

## champ — World Champion (main)

1. Narrator [n] (win): Confetti. Cameras. A bracelet the size of a doorknob. / Somewhere, a nine-year-old with a fuzzy TV is losing his mind.  {wide^:Victory@crowd~triumph}
2. Bio The Ghost [ghost]: Good game, kid.  {two:Shaking Hands 2>Shaking Hands 2@hush~none}
3. Narrator [n]: He takes off his glasses. For the first time anyone can remember, Bio The Ghost smiles.  {ecu@swell}
4. Bio The Ghost [ghost]: Now you'll find out the hard part. Everyone will be hunting you.  {cu:Pointing~tension}
5. Natso SweetLou [derek] (text): Hey buddy! Long time! Saw you on TV. Listen, about that promotion…
6. Narrator [n]: You leave him on read.  {cu^:Texting While Standing@tick}
7. Slick [slick]: People insist on calling it luck.  {push:Talking@swell~triumph}
8. You [you]: Let them.  {low:Head Nod Yes@sting}

## c7 — Legacy · The Hall of Legends (hall)

1. Narrator [n]: Your picture hangs in the Hall of Legends now. Next to men you used to watch on a fuzzy TV.  {ext@swell~triumph}
2. Narrator [n]: Stu. Colt. Chance. The Ghost. And a kid from Floor 34 who got told to redo the Henderson deck.  {wide^:Looking Around}
3. Bio The Ghost [ghost]: Fourteen bracelets. That's the number. Come take it.  {low:Pointing@sting~tension}
4. Slick [slick]: There's nobody left to teach you. So teach somebody. I sat on that stool once too, remember.  {two:Talking>Head Nod Yes@swell~night}
5. You [you]: I remember.  {cu:Head Nod Yes}
6. Narrator [n]: Goal: beat every legend and pass Bio The Ghost's fourteen bracelets.  {push^@swell}

## badbeat — Bad beat · The cards do not care (diner)

1. Narrator [n]: Lucky Seven Diner. Same stool, same pie. The feeling is not the same.  {high^:Sad Idle@hush~tension}
2. You [you]: I did everything right. Every decision. And I'm out.  {cu:Standing Arguing}
3. Slick [slick]: Then you had a good day. You just did not get paid for it.  {ots:Talking>Shaking Head No}
4. Slick [slick]: Over one hand, the cards pay whoever they like. Over ten thousand, they pay whoever studied. Play for the ten thousand.  {push:Pointing@swell~night}
5. Mustich [trent] (text): Heard you got coolered. Brutal. Maybe this game just isn’t for everyone.
6. You [you]: He's right about one thing. It isn't for everyone.  {cu:Shaking Head No}
7. Slick [slick]: Good. Make sure it is for you. I keep count of the beats, by the way. The ones who make it all have a long list.  {two:Head Nod Yes>Head Nod Yes@swell}

## broke — Rock bottom (diner)

1. Narrator [n]: Lucky Seven Diner, Calle Ocho. 2:13 AM. Same stool. Same pie. Empty wallet.  {high^:Sad Idle@hush~tension}
2. Slick [slick]: Busted?  {ots:Talking}
3. You [you]: Every last dollar.  {cu:Shaking Head No}
4. Slick [slick]: Good. Now you're a poker player. Everybody goes broke once. The good ones learn why.  {push:Talking@swell~night}
5. Slick [slick]: Here's how you climb out. Freerolls on CF Tilt every day. A real job while you rebuild. Or my money, and half your profits until it's back.  {two:Pointing~none}
    - Choice: Take Slick's stake | Slick [slick]: Half your profits come back to me until it's paid. Respect the roll this time.
    - Choice: Grind the freerolls | Slick [slick]: Free money is the best money. Show me you can build it back from zero.
    - Choice: Ask Lou for the job back | Narrator [n]: Natso SweetLou smirks for a full ten seconds. Then he slides a badge across the desk.

## after_bust — After (cardroom)

1. Narrator [n] (bust): (built at runtime)  {high@hush}
2. You [you] (bust): (built at runtime)  {cu:Defeated}

## after_cash — After (cardroom)

1. Narrator [n] (cash): (built at runtime)  {wide@chips}
2. You [you] (cash): (built at runtime)  {cu:Fist Pump}

## after_cooler — After (cardroom)

1. Narrator [n] (cooler): (built at runtime)  {high@hush~tension}
2. You [you] (cooler): (built at runtime)  {ecu:Shaking Head No}

## after_win — After (cardroom)

1. Narrator [n] (win): (built at runtime)  {wide@crowd}
2. You [you] (win): (built at runtime)  {low:Victory}

## david1 — A text (home)

1. David Pec [mom] (text): TEN THOUSAND?? FROM POKER?? AND YOU STILL WENT TO WORK TODAY???
2. David Pec [mom] (text): im just saying. lou doesnt deserve you. the felt does

## david2 — A text (home)

1. David Pec [mom] (text): 25K. bro. thats a YEAR of lou's bonuses in like two months
2. David Pec [mom] (text): when do we call it. seriously. when

## david3 — A text (home)

1. David Pec [mom] (text): 50 THOUSAND. I TOLD EVERYONE AT THE BAR. EVERYONE
2. David Pec [mom] (text): you know what slick would say. you know what I say. the dream doesnt wait

## david4 — A text (home)

1. David Pec [mom] (text): 100K and youre still doing the henderson deck???? I cant
2. David Pec [mom] (text): quit. move. go full time. ill drive you to the cardroom myself

## derek — Meanwhile · Floor 34 (office)

1. Natso SweetLou [derek] (text): Hey buddy!! Long time. Saw your name on a poker site. Crazy! We're hiring a Senior Associate. Coffee?
    - Choice: Leave him on read. | Narrator [n]: You put the phone face down and go back to studying bubble spots.
    - Choice: "Redo it yourself, Lou." | Narrator [n]: Three dots appear. Then disappear. Then nothing, forever.

## derek_job — Meanwhile · Floor 34 (office)

1. Natso SweetLou [derek] (text): Saw your name on a poker site. Is THAT why the Henderson numbers were late?
    - Choice: Quit. Go full-time. → quit2
    - Choice: "They'll be on your desk Monday." | Narrator [n]: You put the phone face down. The bankroll spreadsheet is still open in the other tab.

## quit2 — Two weeks' notice (office)

1. You [you]: Lou? I'm done. Full-time.  {low:Pointing@sting~grind}
2. Natso SweetLou [derek]: You'll be back.  {cu:Shrugging}
3. Narrator [n]: No paycheck now. Three games a day, and rent is still due on the first.  {wide^@swell}

## quit250 — Decision made · Floor 34 (office)

1. David Pec [mom] (text): 250K. a QUARTER MILLION. if you walk into that office tomorrow I will personally come get you
2. Natso SweetLou [derek]: The partners saw the poker site. A quarter million, they said. So which is it: the deck, or the cards?  {ots:Angry Gesture~tension}
3. You [you]: The cards, Lou. It was always the cards.  {push:Talking@sting}
4. Narrator [n]: You leave the badge on the desk. No paycheck. Every day is yours now: three games, or a live one and the rest online.  {wide^:Looking Around@swell~triumph}
5. Slick [slick] (text): About time. Rent is still due on the first. Never play above your roll.

## rehire — Back on Floor 34 (office)

1. Natso SweetLou [derek]: Look who came crawling back. Same desk. Same Henderson deck. Smaller title.  {ots:Pointing@sting~tension}
2. Narrator [n]: $2,000 every Friday again. Three sessions a week for the dream.  {high^:Sad Idle}

