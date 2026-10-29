# The Dream — moments at the table (Test Beta 1.0)

Steven, 2026-10-29 12:13: "I love the visual updates especially to texts, however we lost the all in screen during
certain hands you play and any fun things like that during tourney game play. Add some scenes / the all in screen
feature back too."

**What this covers:** the moments around the hands during a tournament.
- Sections 1, 2, 7, 8, 9 and 10 are Steven's old game, coming back. They're marked **(old game)**, with the source
  file to port from.
- Sections 3 to 6 are new, written for Chapter 1's cast. They're marked **(new)**. Steven reviews them in the scene
  tree.

Look: the new brand type (Gloock, Inter, IBM Plex Mono), the same polish as the phone texts. Phone-first, light on the
GPU, skippable with a tap.

## 1. The all-in screen (old game) · Opus · high
Source: `unity/Assets/Dream/Scripts/Game/Showdown.cs` and `UI/Run.cs` (`ShowdownView`). Port the behavior exactly.

**When it plays:**
- Your decision puts your stack in the middle: you shove, or you call a shove.
- Always at the bubble and the final table. About half the time otherwise.
- Never two hands running. Never when "Skip all-in animations" is on in Settings (old flag `sd_skip`).

**What it shows:**
- "ALL IN" big in Gloock, with a neon-lime glow (old color #b8ff2e).
- The kicker: "Final table · all in vs Mustich" when the opponent is someone from the story.
- Your hand and theirs. Their cards flip at 0.8 s with a sting.
- An equity bar ("62% to win") appears when their cards turn over.

**The board runs out:**
- Flop at about 1.6 s, turn at 3.0 s, then "The river..." and a long breath, then the river at 4.6 s.
- A tick on each street and a hush before the river.

**How it ends:**
- "You hold!" in green, "Chopped. Split pot." in cream, or "Mustich holds." / "They hold." in red.
- "Pocket kings beats ace-queen."
- "Stack 38 → 81 bb".
- Then "See the verdict".

**Rules:**
- The cards are dealt to fit the result the hand already produced, so the drama never contradicts the grade (old
  `Showdown.Make`).
- If they fold to your shove: "Mustich thinks it over. Then the cards go in the muck. They fold. The pot is yours."
- Tap to skip straight to the result.

## 2. The level card with Slick's tip (old game) · Sonnet · low
Source: `UI/Run.cs` (the level intro) and `DR_TIPS` in `dream/data.js`.

**What it shows:**
- Before each level: the venue (first level) or "Next level", then the stage name ("The bubble", "Final table").
- The hands at this level, plus "turbo blinds: stacks get short fast" when it's a turbo.
- Slick's tip for the stage, with his portrait. Use the `DR_TIPS` lines word for word. For example, at the bubble:
  "Bubble. The short stacks are terrified. The big stacks are licking their lips. Which one are you?"
- At the final table, a rival bar: portrait, "At the final table", their name and their line. On the last hand it
  says "Final hand".

## 3. The bubble: hand for hand (new) · Opus · medium
**When:** once per event, when the next bust is the last one before the money. In the Classic that's 33 left with
32 paid.

**Overlay:**
- Card: "THE BUBBLE". Under it: "33 left · 32 paid · min-cash $X".
- GNOMEY (over the floor mic, a caption with a speaker icon): "We are hand for hand! Dealers, stand up when your hand
  is done. Nobody leaves the table."
- TOMMY (speech bubble from his seat, if he's at your table): "Thirty years, kid, and the bubble still makes my hands
  sweat."

**Then a phone text** (the new phone, small and over the table):
- SLICK: "Near the money, think about what the money's worth. Not the chips." These are Steven's words, from the
  diner.

## 4. In the money (new) · Sonnet · low
**When:** the bubble bursts and you're still in.

- A gold burst: "IN THE MONEY".
- GNOMEY: "That's the bubble! Everybody left is in the money."
- DAVID (text): "ARE YOU IN THE MONEY?? SCREENSHOT IT"

## 5. Heads-up (new) · Sonnet · medium
**When:** two left in any event you can win.

- A splash: "HEADS-UP" with both portraits and both stacks, for example "You 61 bb · Mustich 47 bb".
- GNOMEY: "Ladies and gentlemen, we are heads-up for [event]!"
- Then the opponent's final-table line. Mustich: "Final table. Dad flew in for this. Don't embarrass yourself."
  Tommy: "Thirty years, kid, and I still get butterflies at a final table." These are Steven's `PreLine`s.

## 6. Table talk (new, using Steven's lines) · Sonnet · medium
**What:** a speech bubble from an opponent's seat for about 3 s, at most once every few hands, after a notable
moment. Take the lines from Steven's `Witness` pool in the old `Story.cs`.

**Mustich:**
- When you win a big pot: "Enjoy it. Variance is a loan."
- When he busts you: "Out already? Dad's going to love this." / "Tough. Not really." / "Maybe stick to the
  spreadsheets."

**Tommy:**
- After you double up: "Kid. Don't let it go to your head. Let it go to your bankroll."
- After a cooler: "Brutal. Thirty years and that one still stings." / "That wasn't poker. That was weather."

**Online events (CF Tilt):** the line arrives as a chat message in the table's corner instead, from a screen name.

## 7. The cooler screen (old game) · Sonnet · medium
Source: `UI/Run.cs` (`Cooler`), `DR_COOLERS` in `dream/engine.js`, and `DR_COOLER_LINES` in `dream/ui.js`.

- "Cooler" / "Nothing you could do".
- One of the cooler stories ("Kings into aces. All the chips go in, and you never had a chance.").
- Slick's line, with his portrait: "You played it right. That's all you control. People insist on calling it luck."
  From the second cooler on, it counts: "That's cooler number 2. ...".
- Play it as the all-in screen with the bad result, then this card.

## 8. The walk away (old game) · Sonnet · medium
Source: `Story.cs` in the old game (`after_win`, `after_cash`, `after_bust`, `after_cooler`, the variant texts and
`Witness`). Claude Code left these out this morning. Bring them back.

After each event, play three short beats over a still of the room, before the results:
- The narrator line (one of three variants).
- Your line (one of three).
- The witness line.
  - Live, the witness is whoever is at the venue: Tommy, Gnomey or Mustich.
  - Online, it's a phone text from Slick or David, using the new phone.

## 9. The result's big moments (old game) · Haiku · low
Source: `UI/Run.cs` (Results, "big moments").

Banners: "A title for the trophy room", "Beat Mustich heads-up, for the title", "Final table with Tommy: they'll
remember your face". Use the brand type, not emoji.

## 10. Settings
"Skip all-in animations" (on/off), saved. The all-in screen still shows its result card when it's on.

## 11. Live or online: every moment knows where it is · Opus · high
Steven, 12:16: "make sure all scenes are logical, no more 'the dealer stands up' during an online tournament or in
person convo to legends during online tournaments, stuff like that".

**The rule:** tag every film, Yarn line and table moment with where it can happen, and make Flow refuse anything that
doesn't fit the event just played or being played. The tags are `live`, `online`, `any` and `story` (a fixed story
beat). Online events are the CF Tilt ones, for example the CF $55 Global Rush.

**Online, never:**
- a dealer, the floor, a mic, a cage, a trophy or a room;
- anyone talking to you in person, whether story cast or legend;
- the final-table films (`ft_*`), the Classic films (`classic_*`, `trentbeat`) or `p_seat`.

**Online, instead:** a CF Tilt banner on the table, the table's chat box, screen names, the phone for texts, and your
apartment at night with the laptop (the set and the laptop camera already exist).
- Story people only reach you online by text. Nobody in the story plays you online in Chapter 1.

**Known offenders to fix:**
- `after_win`: the old variant "Phones up. Dealer standing. Floor staff grinning." is live only.
- `after_cash`: "a walk to the cage" and "The cashier counts it twice" are live only.
- `firstwin`: "Saw the photo. Trophy... Left something in your locker" assumes a live title. After an online first
  title, use the online lines below.
- The final-table rival bar and the heads-up splash: online, the opponent is a screen name with no portrait or in-person
  line.

**Online versions (new):**
- **after_win** (narrator, one of):
  - "The final table window flashes gold. First of 1,104. Your name at the top of the CF Tilt lobby."
  - "Three in the morning, alone in the apartment, and you just beat a thousand people. You say it out loud to nobody."
  - "The chat fills with gg and one ship it. The laptop fan is the only applause."
- **after_cash:**
  - "A cash lands in your CF Tilt balance. You refresh it twice to be sure."
  - "Out before the end, but paid. The lobby already has the next one starting."
  - "Min-cash. You close the laptop gently, like it might change its mind."
- **after_bust:**
  - "You finished 214th. The table window closes by itself."
  - "Busted. The tab is gone before you can argue with it."
  - "The lobby keeps scrolling. Nobody noticed you leave."
- **after_cooler:**
  - "Aces into kings, in a window the size of a postcard. It still hurts full size."
  - "The hand history says you did everything right. The balance says otherwise."
  - "You watch the replay twice. It doesn't change."
- **Your line and the witness:** your lines are the same. The witness is always a text, from Slick or David.
- **firstwin**, if your first title is online:
  - SLICK (text): "Saw the screenshot. Your name at the top of the lobby. Proud of you, kid."
  - SLICK (text): "Left something for you at the Lucky Seven. Every champion needs a pair."
  - Then the narrator line, as Steven wrote it.
- **The bubble:**
  - Banner: "HAND-FOR-HAND · every table waits for every other table".
  - Chat: "fold fold fold", "pls no bubble", "someone just shove already".
  - Slick's text, as in section 3.
- **In the money:**
  - Banner: "You're in the money".
  - Chat: "gg bubble boy", "ty dealer".
  - David's text.
- **Heads-up:**
  - Banner: "HEADS-UP vs ripriver_88", with the stacks.
  - No Gnomey, no film.
- **Table talk:** chat lines from screen names: "nh", "sick", "lol ok", "ship it", "how", "gl". Mustich's and Tommy's
  lines are live only.
- **Level card:** "CF Tilt · Level 7 · blinds 400/800 · next level 5:00". Slick's tip arrives as a text.

**The same check for time and place:**
- A 2:13 AM diner film plays that night, not straight after a noon event. Hold it until the evening slot or the night.
- Lou appears only at the office or by text.
- Gnomey appears only at Biscayne.
- A home scene plays only when you're home.

**The check:** run the autoplay driver through two runs. One plays only online events, the other only live events.
Log every film and Yarn line that plays, with its tag. Fail on any mismatch, and keep the log in `Logs/`.

## Order
1, 7, 8, 11, then 3–6, then 2, 9, 10. Then `build-web`, a DAY_REPORT entry, and the stills in `C:\Dev\Builds\day2\`:
- an all-in that holds;
- one that doesn't;
- the bubble;
- heads-up;
- a table-talk bubble.

This comes before more hair or skin work.
