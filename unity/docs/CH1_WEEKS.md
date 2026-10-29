# Chapter 1 — the month, week by week (Test Beta 1.1 content)

Written 2026-10-29 14:50 by the cloud Claude session, for Claude Code to port. Steven: Chapter 1 should be about a month
of in-game calendar ("at least a month") and not over at once; approved 13:36 as Test Beta 1.1. The calendar is in
`Flow.cs`: Chapter 1 starts Mon 12 Jan 2026 (day 7), the Biscayne Classic is Sat 14 Feb (day 40), Day 2 Sun 15 Feb.

Format is the same as `dream/script.md` and `STORY.md`. Nothing here is voiced yet; Steven reads it first. No new
characters: Slick, Gnomey, Tommy, Mustich, Lou, David only.

## The four weeks

| Week | Dates | Beat | Scene ids |
|---|---|---|---|
| 1 | Mon 12 – Sun 18 Jan | Learn the room: Gnomey, Tommy, Mustich's first jab, Slick's first lesson | `c1`, `lesson1` (as now) |
| 2 | Mon 19 – Sun 25 Jan | Mustich is entered in the Classic: the rival bar starts | `c1_w2` |
| 3 | Mon 26 Jan – Sun 1 Feb | Slick's debt and the job pressure (a Friday text, then the diner) | `c1_w3_text`, `c1_w3_diner` |
| 4 | Mon 2 – Fri 13 Feb | Classic week: the poster, the night before | `c1_w4_diner`, `c1_w4_eve` |
| End | Sat 14 – Sun 15 Feb | The Classic | `classic_*`, `trentbeat`, `firstwin` (as now) |

(Week 4 is the Classic week proper: Mon 9 – Fri 13 Feb. The two scenes key off the day.)

## When each one fires

- A beat fires **once**, the first time its condition is met, never again, never during an event.
- `c1_w2`: the **first live game** you start on or after Mon 19 Jan. Live only (it is set in the room).
- `c1_w3_text`: **Friday 30 Jan, with the pay and rent**, on whichever path you are on. Any (a text reads anywhere, at the
  table or online). `c1_w3_diner`: the first night after it when you have played a live event. Story (night, the diner).
- `c1_w4_diner`: the first night on or after Mon 9 Feb. Story. `c1_w4_eve`: **Friday 13 Feb**, the evening before. Story.
- Never inside an online event (CF Tilt): the text scene is the only one that may appear there.
- Save keys to add: `wk2`, `wk3`, `wk4a`, `wk4b` (booleans), so each beat plays once.

## Keys the lines use (the game fills them in, like `{lastEv}` today)

`{br}` your bankroll · `{debt}` what you still owe Slick (0 once repaid) · `{stake}` the Classic stake goal ($5,000) ·
`{days}` days to the Classic · `{rent}` the weekly bill ($600) · `{pay}` the weekly pay (keep path: $1,250).
Lines marked (debt) play only while `{debt}` > 0, (clear) only once it is 0; (keep) / (quit) pick the job path.

---

## c1_w2 — Chapter 1 · Week two (cardroom)

1. Narrator [n]: Second Monday. The room knows your name now, mostly. The coffee is the same coffee.  {ext~grind}
2. Miles "The Snowplow" Gnomey [rosa]: Poster's up by the cage. Biscayne Classic, Saturday, February fourteenth. Two hundred and twelve seats, three-forty to get in. Sign-up sheet's on the clipboard.  {two:Pointing@tick}
3. Narrator [n]: Eleven names on it already. The twelfth, in careful handwriting, is Mustich.  {push:Looking Around}
4. Mustich [trent]: Don't look so surprised. I've been studying this room for a year. You've been studying it for a week.  {cu:Shrugging@sting}
5. Mustich [trent]: I ran your bubble hands through my solver last night. Do you want to know what it said?  {ots:Pointing}
    - Choice: "Does it fold for you too?" | Mustich [trent]: It folds for me when it's right. You fold when you're scared. Different thing.  {cu:Shrugging}
    - Choice: Say nothing. | Mustich [trent]: Nothing. Good. That's the first correct play you've made all week.  {cu:Head Nod Yes}
6. Tommy "The Shark" Maio [lou]: Kid. Come here. That boy's father owns three car dealerships and none of them has ever lost a dime to a cooler.  {two:Talking}
7. Tommy "The Shark" Maio [lou]: He'll play the Classic like a man who has never been hungry. Hungry beats scared. Scared beats nothing. Remember the order.  {ots:Pointing>Head Nod Yes@swell}
8. Narrator [n]: Goal: the Biscayne Classic, Saturday, February 14. Build your bankroll to {stake} before it.  {wide^@swell}

## c1_w3_text — Chapter 1 · Friday (any)

1. Narrator [n]: Friday. The pay lands. {rent} leaves the same morning for the apartment.  {insert}
2. Slick [slick] (text): Rent's in. Good. Look at what's left, not at what you won.
3. Slick [slick] (text): (debt) You still owe me {debt}. Half of every profit comes back to me until it's gone. That's not interest. That's the lesson.
4. Slick [slick] (text): (clear) You owe me nothing now. Which means every dollar you lose from here is yours. Notice how that feels.
5. Natso SweetLou [derek] (text): (keep) Henderson deck, Monday, 9. Nobody gets the weekend off for "a hobby."
6. David Pec [mom] (text): (quit) rent day!!! how are we feeling. be honest. honest = fine. dishonest = I bring soup
7. David Pec [mom] (text): (keep) you look tired in that photo. quit the deck. or dont. but sleep

## c1_w3_diner — Chapter 1 · Lucky Seven, late (diner)

1. Narrator [n]: Lucky Seven. Past midnight. The pie is the same. You are not as poor as last week.  {high^:Looking Around@hush~night}
2. Slick [slick]: Tell me one hand you folded this week that you wanted to play.  {two:Talking}
3. You [you]: Ace-queen, under the gun, deep stacks. Somebody three-bet me and I folded. It was probably good.  {cu:Thinking}
4. Slick [slick]: Probably? Okay. Hold on to the word. In a week you'll be sitting there with a stack you can feel, and "probably" will cost you the thing you built.  {push:Pointing@sting}
5. Slick [slick]: The roll isn't your score. It's your air. Every time you play above it, you're holding your breath. Nobody wins holding their breath.  {cu:Talking}
6. Slick [slick]: (keep) And you're doing it on four hours of sleep, with a boss who thinks a bonus is a hug. That's allowed. It just has a price. Write it down.  {two:Shrugging}
7. Slick [slick]: (quit) And you quit the one thing that paid on time. That's allowed. It just means you're not allowed to be sloppy. Write that down too.  {two:Shrugging}
8. Narrator [n]: Goal: build your bankroll to {stake} before the Classic. {days} days.  {wide^@swell}

## c1_w4_diner — Chapter 1 · Classic week (diner)

1. Narrator [n]: Lucky Seven, the Sunday before the Classic. Slick has a legal pad. He doesn't take notes. He draws one line down the middle.  {high^:Looking Around@hush~night}
2. Slick [slick]: Two days. Day one is for staying alive. Day two is for the ones who stayed alive, and they all sit near each other.  {two:Talking}
3. Slick [slick]: There'll be two hundred and twelve people on Saturday, and about thirty of them know what they're doing. You are going to meet all thirty. Don't be the one who flinches first.  {push:Pointing@sting}
4. You [you]: What do I do if I get scared?  {cu:Thinking}
5. Slick [slick]: Be scared. Then fold the right hand. They're two different jobs. People mix them up.  {cu:Talking}
6. Slick [slick]: Eat something. Chips don't count calories, but you do.  {ots:Shrugging~night}

## c1_w4_eve — Chapter 1 · The night before (home)

1. Narrator [n]: Friday night. {days} days became one. The apartment is quiet. The bay is a black stripe between two buildings.  {ext~night}
2. Slick [slick] (text): Early night. No new hands tonight, only the ones you've already missed. Review them. Then sleep.
3. David Pec [mom] (text): TOMORROW!!! I'll be on my couch screaming at a stream of nothing. Fold the junk. I'm so proud I could scream. I'm screaming.
4. Natso SweetLou [derek] (text): (keep) Henderson numbers, Monday, 9. Good luck with your "event."
5. Narrator [n]: You have {br}. The Classic is $340. You've never been more afraid, and it feels like the first honest feeling you've had in a year.  {push^:Thinking@swell~night}
6. Narrator [n]: Saturday. Biscayne. The big one.  {wide^@sting}

---

## Notes for the port

- Where a line is tagged (debt)/(clear)/(keep)/(quit), the Yarn version is an `<<if>>` around it; delete the tag from the
  spoken text. A text with no active variant is skipped.
- Live or online: `c1_w2` is live only. `c1_w3_text` is `any`. The three diner/home scenes are `story` (night): Flow plays
  them between events, never inside a CF Tilt event, never at a table.
- The Slick texts are tagged "Slick" and keep the contact name from `q_contact`.
- The "Now you're not playing scared" text that fires on crossing the $5,000 stake already exists in 1.1; leave it.
- Re-using a line later: `c4_truth` keeps "Now finish your pie. You've got cameras next week", so it is NOT used here.
- If a scene's condition can't be met (a player who only plays online never walks into Biscayne before the 19th), `c1_w2`
  fires the first time they are in the room (the Classic is live, so they will be).
