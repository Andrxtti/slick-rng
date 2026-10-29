# The Dream — Beta 1.0 script

Written fresh for Beta 1.0 (2026-10-29). Same premise, same people, new scenes and words.
Edit freely: `python beta1/tools/script_build.py` turns this file into `Assets/Dream/Resources/Film/<scene>.json`, then
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

Films have no choices: the player chooses in the live game, not inside a video.

---

## p_office — Floor 34
place: office · time: Monday, 6:47 PM · cast: lou, you
The beat: the job takes everything, and he finally notices.
{sunset through the glass, cleaners' vacuum far off, the air handler hum}

1. CARD [card] (ext): Brickell, Miami. Floor 34. 6:47 PM.
2. NATSO [lou] (ots lou; point): The Henderson deck. All of it again. On my desk by eight.
3. YOU [you] (cu you; talk): You signed off on it Friday.
4. NATSO [lou] (cu lou; lean): And tonight I'm un-signing. Partners liked the model, by the way. I told them it was my idea.
5. NATSO [lou] (two; nod > idle): Team player. That's what I love about you.
{Lou leaves frame; the motion-sensor lights click off down the floor, one bank at a time}
6. YOU [you] (push you; think): Nine years old, I watched a man push every chip he had into the middle with nothing. He never showed. The whole room went quiet.
7. YOU [you] (ecu you; think): Somewhere along the way I traded that for a lanyard.

## p_diner — The Lucky Seven
place: diner · time: 2:13 AM · cast: slick, you
The beat: a stranger who has been where you are hands you a door. Slick says almost nothing, and every word lands.
{rain on the window, the neon buzz, the pie case hum; a booth radio low}

1. CARD [card] (ext): Lucky Seven Diner. 2:13 AM.
2. SLICK [slick] (two; idle): You're staring at that pie like it owes you money.
3. YOU [you] (cu you; shrug): Long day.
4. SLICK [slick] (cu slick; talk): Long year, by the look of you. Spreadsheets?
5. YOU [you] (cu you; talk): For a guy who signs his name on them.
6. SLICK [slick] (ots slick; lean): And what did you want? Before the lanyard.
7. YOU [you] (cu you; think): Poker. For real. I know how that sounds at two in the morning.
8. SLICK [slick] (cu slick; talk): At two in the morning, it's the only true thing anybody says in here.
{he slides a red envelope along the counter; the chrome catches the neon}
9. SLICK [slick] (insert envelope; idle): Biscayne Card Room. Saturday, noon. The seat's paid.
10. YOU [you] (cu you; talk): Why would you do that for a stranger?
11. SLICK [slick] (push slick; talk): Because I sat on that stool once. Somebody bought my pie.
12. SLICK [slick] (cu slick; point > nod): Three things. Fold the junk. Lean on the scared ones. And near the money, play the money, not the chips.
13. YOU [you] (ots slick; talk): Who are you?
14. SLICK [slick] (ecu slick; idle): People call me Slick.
{he leaves cash for both checks; the bell over the door rings; you look down at the envelope}
15. CARD [card] (insert note): Inside, one line in pen: "People insist on calling it luck."

## p_seat — The first seat
place: cardroom · time: Saturday, 11:58 AM · cast: gnomey, tommy, you, dealer
The beat: walking into the room where you don't belong yet.
{the clatter of chips like rain on a tin roof, a floor call far off, a cash game laughing}

1. CARD [card] (ext): Biscayne Card Room. Saturday, noon.
2. GNOMEY [gnomey] (two; point): Seat card. Table nine, seat four. Phones off the felt. Welcome to Biscayne.
3. TOMMY [tommy] (cu tommy; lean): New face. You're the kid with the free seat. Word gets around a small room, kid.
4. TOMMY [tommy] (ots tommy; talk): Thirty-five years at these tables. Here's free advice: this room eats dreamers for breakfast.
5. YOU [you] (cu you; talk): Then I'll try to be lunch.
6. DEALER [dealer] (insert cards; idle): Shuffle up and deal.

## p_after — Monday again
place: office · time: Monday, 9:04 AM · cast: lou, you
The beat: the job looks smaller now.

1. NATSO [lou] (ots lou; point): Henderson numbers. Where are they?
2. YOU [you] (cu you; think): I played eleven hours on Saturday. I made decisions that mattered. And now this.
3. NATSO [lou] (cu lou; lean): Hello? Weekend's over, champ.
4. YOU [you] (push you; idle): Weekend's over.

## c1 — The Grind
place: cardroom · time: Tuesday, 7:02 PM · cast: gnomey, tommy, mustich, you
The beat: the room, the people, the goal.
{sodium light, the list board squeaks, the palms outside going black}

1. CARD [card] (ext): Chapter 1. The Grind.
2. GNOMEY [gnomey] (two; point): You came back. Most don't. One-two has a seat. Midnight Rush starts at twelve.
3. TOMMY [tommy] (cu tommy; talk): Told you, kid. Mayor of this room. I see everything.
4. MUSTICH [mustich] (cu mustich; laugh): Ah, the free-seat lad. Lovely hoodie. Did it come with the seat?
5. MUSTICH [mustich] (ots mustich; lean): I've had my name on the Classic list for a year. Don't get attached to it.
6. YOU [you] (push you; idle): Ten thousand. Or the Classic. Whichever comes first.
7. CARD [card] (wide): Goal: win the Biscayne Classic, or build a $10,000 bankroll.

## lesson1 — Fold the junk
place: diner · time: 1:40 AM · cast: slick, you
Plays after your first bust in Chapter 1.

1. CARD [card] (ext): Lucky Seven. 1:40 AM.
2. SLICK [slick] (two; idle): Tell me the hand.
3. YOU [you] (cu you; sad): King-jack, early position. Raised, got three-bet, called. Flopped a jack. He had kings.
4. SLICK [slick] (push slick; talk): Then the flop didn't beat you. The first decision did. Everything after that was a story you told yourself.
5. SLICK [slick] (cu slick; point > nod): Fold the junk. It isn't advice. It's arithmetic.

## needle — Mustich
place: cardroom · time: night · cast: mustich, you
Plays when Mustich wins a pot from you in Chapter 1 (once).

1. MUSTICH [mustich] (cu mustich; laugh): Oh, that's brilliant. Thank you. Genuinely. Dad'll want to hear about that one.
2. YOU [you] (cu you; idle): Enjoy it.
3. MUSTICH [mustich] (ots mustich; lean): I always do.

## classic — The Biscayne Classic
place: cardroom · time: Saturday, 1:00 PM · cast: gnomey, mustich, tommy, you
Plays when you sit down in the Classic.

1. CARD [card] (ext): The Biscayne Classic. 212 players. One trophy.
2. GNOMEY [gnomey] (wide; point): Two hundred and twelve runners. Final table by the cage. Good luck, all.
3. MUSTICH [mustich] (cu mustich; laugh): Your table or mine, I'll see you at the end of it.
4. TOMMY [tommy] (ots tommy; talk): Thirty years I've chased that trophy. Don't let a kid with a solver take it home.

## classic_win — Gold at Biscayne
place: cardroom · time: Saturday, 11:47 PM · cast: mustich, tommy, gnomey, you
Plays when you win the Classic.

1. CARD [card] (wide): The last card hits the felt.
2. MUSTICH [mustich] (cu mustich; shrug): Well. Dad says the Classic's a rounding error anyway.
3. TOMMY [tommy] (cu tommy; clap): Don't listen to him, kid. You earned it. Every chip.
4. GNOMEY [gnomey] (two; point): Your name goes on the board by the cage. Permanent marker.
5. YOU [you] (push you; fist): Permanent.

## classic_lose — Out of the Classic
place: cardroom · time: night · cast: tommy, you
Plays when you bust the Classic.

1. TOMMY [tommy] (two; lean): Happens to all of us, kid. Thirty years it's happened to me.
2. YOU [you] (cu you; sad): Doesn't make it feel better.
3. TOMMY [tommy] (cu tommy; talk): No. But the list is up again next month.

## goal — Ten thousand
place: diner · time: 2:13 AM · cast: slick, you
Plays when Chapter 1's goal is reached, before the end card.

1. SLICK [slick] (two; idle): You're not staring at the pie anymore.
2. YOU [you] (cu you; talk): I did it. Biscayne's done.
3. SLICK [slick] (cu slick; talk): Biscayne was the classroom. The Circuit is the exam. Bigger rooms. Better players. Rings.
4. SLICK [slick] (push slick; nod): People will call it luck. Let them.

## end — End card
1. CARD [card] (ext): Beta 1.0. Chapter 2: The Circuit, coming soon.

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
