# The Dream — Test Beta 1.0 script

**The Prologue and Chapter 1 are Steven's script, in his words** (the old game's `dream/story.js` scenes with the Unity
game's `Story.cs` overrides, which win where they differ). This file stages them as film: shots, blocking, acting,
pacing. The dialogue is his; the only changes are listed in `beta1/DAY_REPORT.md` ("Rhythm fixes"). Chapter 2 onward is
being written by the cloud session (`unity/docs/STORY.md`).
Edit freely: `python beta1/tools/script_build.py` turns this file into one JSON per film, then
`python beta1/tools/voice.py` voices any changed line. Re-render the scene to see it.

## How a line is written
`N. NAME [id] (shot; acting): words`
- `id` is the voice: slick, you, lou (Natso SweetLou), david, mustich, tommy, gnomey, nick, bio, dealer, card.
  `card` is a title card on black, no voice.
- `shot` is the camera: `ext`, `wide`, `two`, `cu <who>`, `ecu <who>`, `ots <who>` (over the other's shoulder onto
  `<who>`), `insert <thing>`, `push <who>` (slow push-in for the turning line). Lens follows ART_DIRECTION §2.
- `acting` is the body: a clip name (`talk`, `nod`, `shrug`, `point`, `lean`, `think`, `laugh`, `idle`, `fist`, `clap`,
  `sad`, `stand`, `sit`) and optionally `> <next clip>`.
- A line in `{ }` under a scene is a stage direction for the render (sound, light, movement); it is not spoken.
- `CARD` lines are the narrator: words on screen, never voiced. With a shot they sit over the picture; ` / ` breaks the
  line. The game draws them (and subtitles for every spoken line) over the video in the brand fonts, so they stay sharp
  on phones. A `{key}` in a card is filled in by the game when the film plays (your result on Saturday, who you met at
  the final table). A third cue part like `2.5s` holds the shot that long; a card of just `-` shows no words.

Films have no choices: the player chooses in the live game, not inside a video.

**Story logic is in Yarn, not here.** This file is the readable script of the acted films (each scene below is one
short linear film). Which film plays when, the choices (the job decision), the story variables ($job, what has been
seen), the conditions on later lines, and the phone texts live in `Assets/Dream/Resources/Story/*.yarn`
(Prologue.yarn, Chapter1.yarn). Edit lines of dialogue in a film here; edit what happens and when in the .yarn files.

---


## p_office — Floor 34
place: office · time: Monday, 6:47 PM · cast: lou, you
The beat: the job takes everything, and he finally notices.
{sunset through the glass, cleaners' vacuum far off, the air handler hum}

1. CARD [card] (ext): Monday. 6:47 PM. Floor 34, Brickell. / Everyone else went home an hour ago. Out the glass, the bay is going pink.
2. NATSO [lou] (ots lou; point): The Henderson deck. Redo it. All of it. On my desk by eight.
3. YOU [you] (cu you; talk): I sent it Friday. You signed off on it.
4. NATSO [lou] (cu lou; lean): And now I'm un-signing off on it. Problem?
5. YOU [you] (cu you; idle): No problem, Lou.
6. NATSO [lou] (two; nod > idle): Great. Oh, and I told the partners the new model was my idea. Team player, right?
7. CARD [card] (push you; think): You were nine the first time you saw it on TV. / A final table under hot lights. A man in dark glasses pushed every chip he had into the middle with nothing.
8. CARD [card] (ecu you; think): The room went silent. His opponent folded. He never showed. / He made it look like he could see the future.
9. YOU [you] (ecu you; think): Somewhere along the way I traded that for a lanyard.

## p_diner — The Lucky Seven
place: diner · time: 2:13 AM · cast: slick, you
The beat: a stranger at the counter, and the question nobody has asked him in years. Ends on the question; the game
asks it ("Play poker. For real." / "Doesn't matter now."), then plays p_diner_yes or p_diner_no, then p_diner2.
{rain on the window, the neon buzz, the pie case hum}

1. CARD [card] (ext): 2:13 AM. The deck is done. / You are not.
2. STRANGER [slick] (two; idle): You're doing the thing where you stare at the pie like it owes you money.
3. YOU [you] (cu you; shrug): Long day.
4. STRANGER [slick] (cu slick; talk): Long life, by the look of it. What do you do?
5. YOU [you] (cu you; talk): Spreadsheets. For a guy who takes credit for them.
6. STRANGER [slick] (push slick; lean): And what did you want to do?

## p_diner_yes — The honest answer
place: diner · time: 2:13 AM · cast: slick, you

1. YOU [you] (cu you; think): Play poker. For real.
2. STRANGER [slick] (cu slick; laugh): Ha. Everybody says that at 2 AM. Almost nobody means it.

## p_diner_no — The other answer
place: diner · time: 2:13 AM · cast: slick, you

1. YOU [you] (cu you; shrug): Doesn't matter now.
2. STRANGER [slick] (push slick; talk): It always matters. You just stopped saying it out loud.

## p_diner2 — The envelope
place: diner · time: 2:14 AM · cast: slick, you
The beat: a stranger who has been where you are hands you a door. Slick says almost nothing, and every word lands.
{he slides a red envelope along the counter; the chrome catches the neon}

1. CARD [card] (insert envelope; idle): He slides a red envelope across the counter.
2. STRANGER [slick] (cu slick; talk): The Biscayne Championship. Saturday, noon. Two-fifty seat. It's paid.
3. YOU [you] (cu you; talk): Why would you give that to a stranger?
4. STRANGER [slick] (push slick; talk): Because I sat on that stool once. Somebody bought my pie too.
5. STRANGER [slick] (cu slick; point > nod): Three things. Fold the junk. Pressure the scared ones. And near the money, think about what the money's worth, not the chips.
6. YOU [you] (ots slick; talk): Who are you?
7. SLICK [slick] (ecu slick; idle): People call me Slick.
{the bell over the door rings; his stool is empty}
8. CARD [card] (wide; idle): He drops cash for both checks and is gone before the bell over the door stops ringing. / Inside the envelope: a seat card, and one line in pen.
9. CARD [card] (ecu you; think): “People insist on calling it luck…”

## p_seat — The first seat
place: cardroom · time: Saturday, 11:40 AM · cast: gnomey, you
The beat: walking into the room where you don't belong yet. Miles takes the envelope at the cage.
{the clatter of chips like rain on a tin roof, a floor call far off}

1. CARD [card] (ext): Saturday. 11:40 AM. Biscayne Card Room, off the causeway. / Carpet from 1994. Coffee from this morning, maybe.
2. MILES [gnomey] (two; idle): Help you?
3. YOU [you] (cu you; talk): Somebody bought me a seat. The Biscayne Championship.
4. MILES [gnomey] (cu gnomey; point): Free seat? Lucky you. Table nine, seat four. Cards in the air at noon.
5. CARD [card] (wide; idle): A hundred and eighteen players. Two hundred and fifty dollars a seat. You paid nothing, and your hands are shaking anyway.

## ft_intro_tommy — Final table (the Biscayne Championship)
place: cardroom · time: Saturday night · cast: tommy, you
Plays when you reach the final table of an event Tommy is in.

1. CARD [card] (wide; idle): {ft_intro}
2. TOMMY [tommy] (cu tommy; lean): Thirty years, kid, and I still get butterflies at a final table.
3. YOU [you] (cu you; nod): Let's play.

## ft_won_tommy — Final table, won
place: cardroom · time: Saturday night · cast: tommy, you

1. CARD [card] (wide; idle): {ft_won}
2. TOMMY [tommy] (cu tommy; laugh): Ha! The kid got me. Thirty years and the kid got me.
3. YOU [you] (cu you; nod): Good game.

## ft_lost_tommy — Final table, lost
place: cardroom · time: Saturday night · cast: tommy, you

1. CARD [card] (wide; idle): {ft_lost}
2. TOMMY [tommy] (cu tommy; shrug): Not today, kid. Thirty years of practice.
3. YOU [you] (cu you; nod): Good game. I'll see you again.

## ft_intro_mustich — Final table (the Biscayne Classic)
place: cardroom · time: Sunday · cast: mustich, you

1. CARD [card] (wide; idle): {ft_intro}
2. MUSTICH [mustich] (cu mustich; lean): Final table. Dad flew in for this. Don't embarrass yourself.
3. YOU [you] (cu you; nod): Let's play.

## p_after — Monday again
place: office · time: Monday, 9:04 AM · cast: lou, you
The beat: the job looks smaller now. No choice here any more: that comes tonight, at home.

1. NATSO [lou] (ots lou; point): You look different. Did you finish the Henderson numbers?
2. CARD [card] (cu you; think): {saturday}
3. NATSO [lou] (cu lou; lean): Hello? I'm talking to you. Weekend's over, champ.
4. CARD [card] (push you; idle): Seven more hours of spreadsheets. Your mind never leaves table nine.

## p_offer — Monday night
place: home · time: Monday, 11:20 PM · cast: you
The beat: the apartment at night, the city through the blinds, the phone on the counter. Slick's texts and the choice
come next, on the phone, in the game.
{the phone buzzes on the counter}

1. CARD [card] (wide; idle): Monday night. Your phone buzzes on the counter.
2. CARD [card] (cam phone; idle; 3s): -
3. CARD [card] (push you; think; 2.5s): -

## p_quit — Sent
place: home · time: Monday, 11:24 PM · cast: you
Plays after you text Lou.

1. CARD [card] (push you; idle): Sent. No paycheck. No plan B. / You have never felt lighter.

## p_keep — BANKROLL
place: home · time: Monday, 11:24 PM · cast: you
Plays when you keep the job.

1. CARD [card] (cam laptop; idle): You keep the lanyard. / But tonight you open a new spreadsheet and name it BANKROLL.
2. CARD [card] (cu you; think): $2,000 lands every Friday. Days for Natso SweetLou. Three sessions a week for the dream.

## c1 — The Grind
place: cardroom · time: Tuesday, 7:02 PM · cast: gnomey, tommy, mustich, you
The beat: the room, the people, the goal.
{sodium light, the list board squeaks, the palms outside going black}

1. CARD [card] (ext; idle): Biscayne Card Room, off the causeway. Carpet from 1994, palms out front, coffee from this morning, maybe. / Twenty tables, every one of them a classroom.
2. MILES [gnomey] (two; point): Back again? Sign the list. One-two has a seat in ten. Nightly turbo kicks off at seven.
3. TOMMY [tommy] (cu tommy; point): Hey, you're the kid from Saturday. Don't get cocky. This room eats dreamers for breakfast.
4. MUSTICH [mustich] (cu mustich; laugh): Cute hoodie. Did it come with the free seat?
5. CARD [card] (ecu mustich; idle): Mustich. Dad's money, a solver subscription, and a smile you want to wipe off. / He's been circling the Biscayne Classic all year.
6. CARD [card] (wide; idle): Goal: win the Biscayne Classic, or build your bankroll to $10,000.

## classic_day1 — Bags and tags
place: cardroom · time: Saturday, 11:50 PM · cast: gnomey, mustich, you
Plays when you survive Day 1 of the Biscayne Classic. Slick's text follows on the phone.

1. CARD [card] (wide; idle): Bags and tags. Two hundred and twelve started this morning. Forty come back tomorrow, and you're one of them.
2. MILES [gnomey] (two; point): Bag your chips, write your name on it. Day 2 is noon tomorrow. Don't be late.
3. MUSTICH [mustich] (cu mustich; lean): Look who survived. Don't get used to it.

## trentbeat — The Biscayne Classic
place: cardroom · time: Sunday night · cast: mustich, tommy, gnomey, you
Plays when you win the Classic.

1. CARD [card] (wide; idle): Final hand. Heads-up for the Biscayne Classic, and the whole card room is standing on chairs.
2. CARD [card] (push you; fist): The last card hits the felt. The Biscayne Classic is yours.
3. MUSTICH [mustich] (cu mustich; shrug): Enjoy it. Dad says the Classic is a rounding error.
4. TOMMY [tommy] (cu tommy; clap): Don't listen to him, kid. Thirty years I've been chasing that trophy. You earned it.
5. MILES [gnomey] (two; point): Your name goes on the board by the cage. Permanent marker. Congratulations.

## classic_mustich — The Biscayne Classic, Day 2
place: cardroom · time: Sunday night · cast: mustich, tommy, you
Plays when you're out of Day 2 and Mustich wins it.

1. CARD [card] (ecu you; sad): {day2}
2. CARD [card] (wide; idle): Final hand. Mustich shoves with his dad watching from the rail. Called. An ace on the river.
3. CARD [card] (cu mustich; fist): The room explodes. Mustich holds the trophy over his head like he bought it.
4. MUSTICH [mustich] (cu mustich; laugh): Write it down, Florida. Some people are born for this.
5. CARD [card] (push you; idle): {day2_you}
6. TOMMY [tommy] (two; talk): Next month, kid. That trophy isn't going anywhere.

## classic_shark — The Biscayne Classic, Day 2
place: cardroom · time: Sunday night · cast: tommy, gnomey, you
Plays when you're out of Day 2 and Tommy wins it.

1. CARD [card] (ecu you; sad): {day2}
2. CARD [card] (wide; idle): Final hand. Tommy “The Shark” Maio turns over two kings and the whole rail stops breathing.
3. CARD [card] (cu tommy; idle): The river bricks. Thirty years at this card room, and the old man finally has his trophy.
4. TOMMY [tommy] (cu tommy; fist): Thirty years! You hear me? Thirty years!
5. CARD [card] (push you; idle): {day2_you}
6. MILES [gnomey] (two; point): His name goes on the board by the cage. Next month, it could be yours.

## classic_other — The Biscayne Classic, Day 2
place: cardroom · time: Sunday night · cast: slick, gnomey, you
Plays when you're out of Day 2 and somebody else wins it. (In the old game a legend could win it; the legends aren't
built yet, so it's Slick, who is in that list too.)

1. CARD [card] (ecu you; sad): {day2}
2. CARD [card] (wide; idle): Final hand. All in, called, and the board runs clean.
3. CARD [card] (wide; idle): The room comes to its feet. The Biscayne Classic has a new name on the board.
4. SLICK [slick] (cu slick; idle): Somebody had to keep you humble, kid.
5. CARD [card] (push you; idle): {day2_you}
6. MILES [gnomey] (two; point): The name goes on the board by the cage. Next month, it could be yours.

## lesson1 — A lesson: fold the junk
place: diner · time: 1:40 AM · cast: slick, you
Plays the first time you bust out of the money in Chapter 1 (not a cooler).

1. CARD [card] (ext; idle): Lucky Seven. 1:40 AM. The pie is the same. You are a little poorer.
2. SLICK [slick] (two; idle): Tell me the hand that got you.
3. YOU [you] (cu you; sad): King-jack, early position. I raised. Got three-bet. Called. Flopped a jack. Lost everything to kings.
4. SLICK [slick] (push slick; talk): Then the hand didn't get you. The first decision did. From early, king-jack is a fold. Everything after was a story you told yourself.
5. SLICK [slick] (cu slick; point > nod): Fold the junk. That's not advice, it's arithmetic. Your misses are on the Spacepad. Drill them until the fold is boring.

## firstwin — First title
place: diner · time: 2:13 AM · cast: slick, you
Plays after your first tournament win.

1. SLICK [slick] (cu slick; talk): Saw the photo. Trophy, big grin, terrible lighting. Proud of you, kid.
2. SLICK [slick] (two; point): Left something in your locker. Every champion needs a pair.
3. CARD [card] (cu you; laugh): Inside: a pair of pixelated sunglasses and a note. / “Deal with it.”

## badbeat — Bad beat
place: diner · time: 2:13 AM · cast: slick, you
Plays after your first cooler. Mustich's text comes next on the phone, then badbeat2.

1. CARD [card] (ext; idle): Lucky Seven Diner. Same stool, same pie. The feeling is not the same.
2. YOU [you] (cu you; sad): I did everything right. Every decision. And I'm out.
3. SLICK [slick] (cu slick; talk): Then you had a good day. You just did not get paid for it.
4. SLICK [slick] (push slick; talk): Over one hand, the cards pay whoever they like. Over ten thousand, they pay whoever studied. Play for the ten thousand.

## badbeat2 — Bad beat (after Mustich's text)
place: diner · time: 2:15 AM · cast: slick, you

1. YOU [you] (cu you; idle): He's right about one thing. It isn't for everyone.
2. SLICK [slick] (push slick; nod): Good. Make sure it is for you. I keep count of the beats, by the way. The ones who make it all have a long list.

## broke — Rock bottom
place: diner · time: 2:13 AM · cast: slick, you
Plays when the bankroll can't buy into anything. Then broke_q (full-time) or broke_n (day job), then the choice.

1. CARD [card] (ext; idle): Lucky Seven Diner, Calle Ocho. 2:13 AM. Same stool. Same pie. Empty wallet.
2. SLICK [slick] (two; idle): Busted?
3. YOU [you] (cu you; sad): Every last dollar.
4. SLICK [slick] (cu slick; talk): Good. Now you're a poker player. Everybody goes broke once. The good ones learn why.

## broke_q — Rock bottom (full-time)
place: diner · time: 2:14 AM · cast: slick, you

1. SLICK [slick] (push slick; point): Here's how you climb out. Freerolls on CF Tilt every day. A real job while you rebuild. Or my money, and half your profits until it's back.

## broke_n — Rock bottom (day job)
place: diner · time: 2:14 AM · cast: slick, you

1. SLICK [slick] (push slick; point): You've still got Lou's paycheck. Freerolls on CF Tilt while it builds back. Or my money, and half your profits until it's back.

## broke_stake — Slick's stake
place: diner · time: 2:15 AM · cast: slick, you

1. SLICK [slick] (cu slick; nod): Half your profits come back to me until it's paid. Respect the roll this time.

## broke_free — The freerolls
place: diner · time: 2:15 AM · cast: slick, you

1. SLICK [slick] (cu slick; nod): Free money is the best money. Show me you can build it back from zero.

## end — End card
1. CARD [card] (ext): Test Beta 1.0. Chapter 2: The Circuit, coming soon.

---

## Stings (short lines for the reel and the table)
- SLICK [slick]: People call me Slick.
- SLICK [slick]: Fold the junk. It isn't advice. It's arithmetic.
- SLICK [slick]: Near the money, play the money.
- BIO [bio]: Kid. Stop looking at me. Look at your cards.
- BIO [bio]: Fourteen bracelets. Come take them.
- NICK [nick]: Welcome back to the feature table. Forty thousand people are watching you blink.
- MUSTICH [mustich]: Your table or mine, I'll see you at the end of it.
- TOMMY [tommy]: This room eats dreamers for breakfast.
- DAVID [david]: BRO. YOU'RE ON THE STREAM.
- YOU [you]: People insist on calling it luck.

## The reel (first launch, 75 s, tap to skip)
Cut on the music. Title cards in white on black, a beat each. Every shot below is a render from a set; "voice" is a
sting from the list above laid over it.

| t (s) | shot | voice / card |
|---|---|---|
| 0–4 | black; a lighter-click of chips | card: "Everybody has a dream." |
| 4–9 | office, wide: Floor 34 at sunset, you alone at the glass | — |
| 9–14 | diner, ext then two: the neon, Slick turning on his stool | SLICK: "People call me Slick." |
| 14–17 | insert: the red envelope sliding along the counter | — |
| 17–20 | card | "Some people get a seat." |
| 20–26 | card room, wide: sodium light, twenty tables | TOMMY: "This room eats dreamers for breakfast." |
| 26–30 | card room, cu Mustich laughing | MUSTICH: "Your table or mine…" |
| 30–33 | card | "Some people earn one." |
| 33–38 | casino floor: a Circuit ring on a pillow under gold light | — |
| 38–43 | Vegas room: the bracelet case, white heat | SLICK: "Near the money, play the money." |
| 43–48 | TV table: key lights in a black room, cameras | NICK: "Forty thousand people are watching you blink." |
| 48–52 | Titan lounge: cold marble, blue glass, the trophy | — |
| 52–60 | Main Event stage: the wall of lights; Bio's eyes over the mask | BIO: "Kid. Stop looking at me. Look at your cards." |
| 60–66 | Hall of Legends: gold on black, a row of portraits, one empty frame | BIO: "Fourteen bracelets. Come take them." |
| 66–70 | diner, ecu: Slick's half smile | YOU: "People insist on calling it luck." |
| 70–75 | card | "THE DREAM" |
