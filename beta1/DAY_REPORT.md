# The Dream — Test Beta 1.0 day report (2026-10-29, afternoon)

Claude Code on Steven's PC, working `unity/docs/DAY2.md` while Steven is at work. The cloud session reviews stills,
writes Chapter 2 on (`unity/docs/STORY.md`) and publishes web builds; notes come in through `C:\Dev\Beta1Assets\INBOX.md`.
Stills for this report: `C:\Dev\Builds\day2\`.

## To-do (ordered; model · effort)
**Priority 1: Steven's Prologue and Chapter 1, in his words**
1. Script: stage Steven's Prologue and Chapter 1 as film in `story/SCRIPT.md` (old `dream/story.js` and the Unity
   game's `Story.cs` overrides) · Opus · high · done
2. The apartment set (`tools/blender/home.py`): Miami studio at night, the counter, the phone, city through the blinds · Opus · high · done
3. Phone texts: Messages-style screen (bubbles, typing dots, timestamps, sender, reply chips for the job decision) · Opus · high · done
4. Yarn: the diner question, Monday, the offer at home, quit/keep, the Classic's two days, final tables, firstwin,
   badbeat, lesson1, broke with its choices, David, Slick's Circuit text · Opus · high · done
5. Chapter 1's events and calendar as Steven's game ran them (Winn Nightly $110, CF $55 Global Rush online, Classic
   Day 1/Day 2, Comeback Freeroll; live and online slots) · Sonnet · medium · done
6. Voice and render every film; captions drawn by the game; contact sheets reviewed · Sonnet · low
7. Play it through both branches with the test driver; `build-web` · Haiku · low

**Priority 2: brand fonts** · Opus · medium
8. TextMeshPro with Gloock / Inter / IBM Plex Mono, a type scale, rounded cards and pills · started with priority 1
9. The table and grade panel in the new type; check every screen at phone size

**Priority 3: best graphics** · Opus · high
10. Films: supersampled renders (1440p → 1080p), SSAO, high-res shadows, bokeh, restrained grain
11. Phones: the FSR/post fix (Batch C 12), sensible shadows, compressed textures

**Priority 4 and Batches A–C** (characters, cutscenes, live game): fresh list after priority 3.

## Calls I made (Steven is away)
- **Films never carry words now.** Narrator cards and subtitles are drawn by the game over the video (Gloock for the
  narrator, Inter for subtitles), from a small `<film>.json` written at render time. Why: Steven's narration has
  numbers that depend on your game ("You finished 14th on Saturday and took home $612"), the brand fonts stay sharp at
  any size, and held upright the words sit in the black under the picture instead of over faces.
- **The diner question is asked.** Steven's p_diner has a choice ("Play poker. For real." / "Doesn't matter now."). The
  film stops on Slick's question, the game asks it, then plays the matching answer and the rest of the scene
  (`p_diner`, `p_diner_yes`/`p_diner_no`, `p_diner2`). The answer is saved as `$honest`, as in the old game.
- **Who wins the Classic when you don't:** the old game drew from Mustich, Tommy, Slick and the legends. The legends
  aren't built, so it's Mustich (always, if he beat you at the final table; else one time in five), Tommy (about one in
  eight) or Slick (the rest, with his own line from Steven's list).
- **Lesson 1** uses Steven's written lines (the old game's fallback), not the hand-that-cost-you version: a film can't
  voice a hand that changes every game.
- **Left out, as not listed in DAY2:** the after-event "walk away" scenes, `derek_job`, `broke_told`/`slick_back`. The
  results screen covers the walk away for now.
- **Removed from the films:** `needle`, `classic`, `classic_win`, `classic_lose`, `goal`. They were my lines from last
  night; Steven's `trentbeat` and the three Classic endings replace them, and the chapter now ends on Slick's text from
  Steven's Chapter 2 opener ("You've outgrown Biscayne...") before the end card.

## Rhythm fixes to Steven's lines
None so far. Steven's words are as written, including "Spacepad" in lesson 1 (the drill screen isn't in this beta) and
"Nightly turbo kicks off at seven" in c1 (the Winn Nightly is no longer a turbo, as in his Unity game).

## Log
### Priority 1 done: Steven's Prologue and Chapter 1 (11:35)
- **Flow, played by the test driver on both branches** (`Logs/auto_keep.txt`, `auto_quit.txt`): reel → p_office →
  p_diner → the question ("Play poker. For real.") → p_diner_yes → p_diner2 → p_seat → the Biscayne Championship (Tommy's
  final-table films) → p_after → **p_offer at home: the phone buzzes, Slick's three texts, "Decision time." on the
  phone** → quit (you text Lou "Lou. Redo it yourself.", p_quit, Slick ×2, David) or keep (p_keep's BANKROLL sheet,
  Slick's "Smart.") → c1 → hub → events → Mustich's text after a cooler, firstwin, David's TEN THOUSAND, Slick's Circuit
  text → end card. Stills: `day2/home_sheet.jpg`, `offer_phone.png`, `phone_keep.png`, `phone_quit.png`, `keep_hub.png`.
- **Calendar and events** as his Unity game: Winn Nightly $110 (live), CF $55 Global Rush (online), the Classic on the
  last Saturday of the month (Day 1 bags 40; Day 2 on Sunday pays the 212-player pool), the Comeback Freeroll under $400.
- **Web build** rebuilt: 14.9 MB engine payload (+1.1 MB for fonts and TextMeshPro), 0 errors, 29 films + captions
  (40 MB streamed). It now builds with a phone pipeline (`URP_Mobile`: Linear upscaling instead of FSR, no MSAA, small
  shadows) and switches back to the film pipeline after.
- **Not good yet:** the phone body in p_offer is a plain slab; insert shots on the diner counter blow out under the key
  light; a perfect player who wins the free Local ($7,000) and quits ($5,000) is past the $10,000 goal after one more
  event (Steven's economy from the old game; I left it). Characters are still last night's faces (priority 4).
### Steven's 11:27 notes (from INBOX, 11:45)
- **Character creator** is now the first menu after the reel: "Create your player", your face, your name in Gloock
  (18 characters, empty means "Rookie" as in the old game) and the dice. Saved with the career, passed to Yarn as
  `$name`, shown on the hub and the end card; Steven's lines still say "kid". The dice rolls a name, not a look: the films
  are rendered with one player, so look options would need every film rendered per look. Still: `day2/creator.png`.
- **Money:** Slick's backing is $2,000 (his texts in p_offer and p_quit, the hint and the bankroll); the paycheck is $1,250
  every Friday (calendar, the keep hint, p_keep's caption). Keep-path savings stay $2,000.
- **Classic winners mixed up:** Mustich 40%, Tommy 35%, Slick 25% (always Mustich if he beat you at the final table).
- **Apartment:** it's already a small studio with water and city through the blinds (Poly Haven "modern_buildings_night").
  Calling it Brickell with a glimpse of the bay; a closer bay HDRI would be better (on the list).
- Both branches re-played by the test driver; web build rebuilt: **15.0 MB, 0 errors**. Ready to publish.
### Priority 2 (fonts) and 3 (graphics), Batch A1–A2 and C11–13 (12:00)
- **Type:** every screen uses TextMeshPro in the brand fonts (OFL, `Resources/Fonts`, licences in `Fonts-Licenses`):
  Gloock for titles, the narrator, the grade name and the name field; Inter for everything you read (caps labels get
  +8 tracking, small text more line height); IBM Plex Mono for money, stacks, pot and hand lists. DejaVu covers the
  symbols (★ ✓); the 👍 grade icon (no font on the web) shows ✓✓. Buttons and cards have rounded corners. No Arial
  anywhere. Title reads "Test Beta 1.0" (C11). Stills: `day2/creator.png`, `phone_quit.png`, `keep_end.png`.
- **Films (offline):** renders are 2560×1440, scaled down with lanczos (supersampling); SSAO on the film renderer;
  4K soft shadows were already on; grain down to 0.16; insert key light halved (the counter blow-out).
- **Phones (live, C12):** the web build switches to `URP_Mobile` (Linear upscaling instead of FSR, whose stripped EASU
  shader turned post-processing off; no MSAA; 1K shadows, 2 cascades; its own renderer without SSAO), then back.
- **Skin (A1):** new `Dream/Skin` shader: URP PBR + wrap-lit warm scatter at the terminator from every light + a tiled
  pore normal map made in code (`FilmData/Tex/pores_n.png`, ours) + slightly rougher where the skin is flushed. Teeth and
  tongue darkened so they never glow.
- **Hair (A2):** `tools/blender/hair.py` grows ~12–16k card segments per head over the MakeHuman hair, following each
  brief: back (Slick, Tommy, David), crop (Mustich, Lou, Gnomey, dealer), tousled (Nick), fringe (Bio), side part (you).
  Our own strand texture; new `Dream/Hair` shader with two Kajiya-Kay highlights. Still: `day2/diner_hair_check.jpg`,
  `slick_hair_pair.png` (Blender).
  **Not good yet:** Slick still reads as a fringe from the front, because the MakeHuman shell underneath hangs over his
  forehead. Next: trim the shell's front so his cards can go up and back.
- **Table life (C13):** hole cards deal in from the pot, bets slide out from each seat, your seat glows softly while it's
  your move. Coroutines on UI only, so it stays light on phones.
- **Lip-sync (B7) is blocked on a permission:** I downloaded Rhubarb Lip Sync 1.13.0 (MIT, the official GitHub release)
  to `C:\Dev\Beta1Assets\tools\rhubarb`, but this session wasn't allowed to run the downloaded .exe. Lip-sync stays
  loudness-driven. If Steven allows running it, it's one script to write the mouth shapes for every line.
### TABLE.md: the moments at the table, and live vs online (13:05) · Opus · high
Steven's 12:13 and 12:16 asks, done in TABLE.md's order. Code: `Scripts/Game/Moments.cs` (new), hooks in `Table.cs` and
`Flow.cs`, the online first title in `Chapter1.yarn`.
- **§1 All-in screen**, ported from the old `Showdown.cs`/`Run.cs` with the dealing logic verbatim (cards dealt to fit
  the grade). Neon ALL IN in Gloock, "Final table · all in vs Tommy", their cards flip at 0.8 s, the equity bar, the
  flop at 1.6 s, the turn at 3.0 s, "The river...", the river at 4.6 s, "You hold!" / "Tommy holds." / "Chopped", the
  hands, "Stack 170 → 180 bb", "See the verdict". It always plays at the bubble and the final table, about half the time
  otherwise, never two hands running; tap skips; their fold to your shove has its own card. One fix of mine: when both
  players have the same made hand on the board, it says "Three twos each. The kicker plays." instead of "Three twos
  beats three twos".
- **§7 Cooler:** the all-in that goes wrong, then "Nothing you could do", one of the old cooler stories, and Slick's line
  with his portrait ("That's cooler number 2. ..." from the second on).
- **§8 Walk away:** before the results, three beats over the room (Biscayne, or your apartment at night when online):
  the narrator (Steven's three variants each, the new online ones for CF Tilt), your line, and the witness (Tommy, Gnomey
  or Mustich in person live; a text from Slick or David online).
- **§2 Level card** before every level: the room or "Next level" (online: "CF Tilt · Level 7 · blinds ..."), the stage,
  the hands, Slick's DR_TIPS line with his portrait (online: as a text), and at the final table the rival bar
  (Tommy or Mustich, their PreLine).
- **§3–5 Bubble, in the money, heads-up**, live with Gnomey on the floor mic and Tommy's/Mustich's lines, online with the
  CF Tilt banner and the chat box; Slick's and David's texts on both.
- **§6 Table talk:** Tommy or Mustich after a big swing (live); chat lines from screen names (online).
- **§9 Results banners** ("Beat Tommy heads-up, for the title", "A title for the trophy room", "Top of the CF Tilt
  lobby"); **§10** "All-in animations: on/off" on the title screen, saved.
- **§11 Live or online:** every film is tagged live / story / any (`Moments.FilmTag`), every table moment notes where it
  is, and Flow refuses a live film after an online event. firstwin after an online first title is Slick's two texts
  (TABLE.md's lines) and Steven's narrator line, no photo, no locker.
  **Proof:** `day2/auto_keep_online.txt` (online events only) and `auto_quit_live.txt` (live only): **PASS, no
  mismatch**. The first live run caught a real bug: the last online event leaked into a new career and refused
  `p_seat`; fixed.
  Time and place: every Chapter 1 event ends at night (the Local's films run before Monday), so the diner films always
  play at night; Lou only appears at the office or by text; Gnomey only at Biscayne.
- Stills: `day2/allin_holds.png`, `allin_doesnt_cooler.png`, `bubble_live.png`, `bubble_online.png`, `headsup.png`,
  `walkaway_live.png`, `walkaway_online.png`, `moments_live.jpg`, `moments_sheet.jpg`.
- **Web post-processing (review item 1):** the web build now always includes URP's FSR (EASU) shader, which URP needs
  to exist before it runs any post pass. Please confirm the console line is gone. Also found and fixed: a build had left
  ProjectSettings with no render pipeline. The build now puts back `URP.asset` by name (and there's a `film-pipeline`
  bridge command).
- **Steven's 11:55 notes:** "You were fourteen the first time you saw it on TV" (a narrator card, so nothing to
  re-voice; the caption file is updated). Title music "Retroracing Nightlife" by Bogart VGM (CC-BY 4.0) loops under the
  title and the creator with a 2.5 s fade-in, stops for the reel and the story, credited on the end card and in
  `beta1/ASSETS.md`. There is no `unity/docs/ASSETS.md`, and `unity/` is the fallback I don't touch, so the beta's ledger
  holds it. It's streamed from `StreamingAssets/Music` (1.9 MB at 96 kbps), not in the first download.
- **Web build:** 15.3 MB, 0 errors. Ready to publish.

### Rhythm fixes to Steven's lines (updated)
- p_office: "You were nine" → "You were fourteen" (Steven's own change, 11:55: the player is 24).
