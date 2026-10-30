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
### GRAPHICS.md, G1–G8 (Steven 12:39: "continue to actively work on our graphical capabilities") · 13:30
- **G1 pills:** every button and field uses a 9-slice rounded sprite with a fixed corner radius (34 px buttons, 40 px
  field; circles only where it's square, like the avatar and send button), so nothing stretches into an ellipse. The
  phone's choice chips have padding and room for two lines.
- **G2 table:** rebuilt with the cloud session's kit (`Resources/Kit`, CC0): the racetrack plate (live green / CF Tilt
  teal, the tall plate when the space is taller than wide) fitted to the screen, seats on the rail as dark-glass plates
  with the stack in Plex Mono (online: screen names and two-letter avatars, no faces), your seat gold with the kit glow,
  bets as stacks of side chips in front of each seat, the pot as a stack, the kit's dealer button, and the kit's
  four-colour cards with shadows (your two fanned). The all-in screen uses the same cards. Import limits keep it light:
  cards 256 px, chips 128, plates 2048, compressed (`Editor/KitImport.cs`).
- **G3 hair:** alpha-to-coverage on the hair cards with MSAA 8× for the films, darker roots, highlights tamed. Slick and
  Tommy moved to MakeHuman's slicked-back shell (`short04`; compared against short01/03 in `work/slick_shells.png`), so
  Slick's cards go up and back off a clean hairline. I didn't cut the shell: a trim test gave a jagged hairline and was
  reverted.
- **G4 faces:** every close-up, OTS and two-shot gets a wider, softer key (60° spot, soft edge), a stronger fill (the
  shadow side never goes black) and a new cool rim light from behind. The card room's key, ambient and exposure are up;
  the apartment's warm key is up against the blue window. Adaptive Probe Volumes not done (needs a bake per set).
- **G5 grades:** `films_publish.py` applies each set's `.cube` (diner_night, office_dusk, cardroom_sodium,
  apartment_night) at 60% while scaling to 720p. Test frame: `work/grade_pair.png`. Not yet: the URP Color Lookup strip
  in the live game (the live game is UI only, no 3D camera to grade).
- **G6 IllusionRP: not attempted.** It installs from a GitHub repository, which isn't on CLAUDE.md's list of allowed
  sources, and it needs a second editor on a copy of the project (one editor at a time on this PC) switched to DX12. If
  Steven wants it, it's a session of its own, with the licence read first.
- **G7 eyes/teeth:** teeth warmer, rougher and set back. The eyes keep their wet 0.95 smoothness; no separate cornea
  mesh yet.
- **G8:** the WebGL build uses `URP_Mobile` (Linear upscaling, render scale 1) and always includes the EASU shader.
  Needs the cloud session's console check.
### The S block (Steven 13:22, "do all Sonnet and then let me know when to change to Opus") · 14:40
This session ran as Opus all afternoon (the model is Steven's switch), and part of the Opus block (G1 pills, G2 table,
G3 hair, G4 faces) was already in when the request arrived; it's listed above. The S block, done:
- **S1 web post-processing:** found why it kept failing. URP builds every post pass for a renderer that has post data,
  and the FSR (EASU) shader isn't supported on WebGL2, so it disables them all with that warning no matter what
  upscaling the asset selects. The live game is UI only (overlay canvases, which post-processing never touches), so
  the phone renderer (`URP_Renderer_Mobile`) now has no post data at all: no post passes, no FSR, no warning. The
  player also logs its ground truth at startup: `[Dream] pipeline URP_Mobile · quality … · upscaling Linear · render
  scale 1`. Please confirm in the browser console. What this means: the live game has no bloom or grade, because
  it never had a 3D camera to put them on; the films carry their grade (S4).
- **S2 online stills retaken during a CF Tilt event:** `day2/online_moments.jpg` (THE BUBBLE: "167 left · 166 paid",
  hand-for-hand, the chat box; HEADS-UP vs nitbox). The test driver now names stills `_online`/`_live`. The earlier
  "online" stills were taken during the live Prologue event: a naming bug in the driver, not live content online. The
  two proof runs had already shown no mismatch.
- **S3 polish:** the moment overlays are fully opaque (no seat numbers bleed through, and the verdict's Next button
  is covered); speaker, Slick and rival cards put the words right after the portrait (a small component keeps them 24
  px after it at any screen shape); the all-in names kickers: "Three twos, ace kicker, beats three twos, queen kicker."
- **S4 grades:** every film re-published through its set's `.cube` at 60%. The live game's Color Lookup isn't
  possible (see S1: no 3D camera).
- **S5 eyes and teeth:** teeth warmer, rougher, set back. No new cornea layer; the eyes keep their wet 0.95
  smoothness. Not done.
- **S6 props:** not done beyond the apartment (the phone's lock screen, the laptop's BANKROLL sheet). The phone
  body is still a plain slab.
- **S7 Rhubarb: still blocked.** Even with Steven's yes relayed, this session's permission check refuses to run the
  downloaded `rhubarb.exe` ("Code from External"). I didn't work around it. **Steven: to allow it, add a Bash permission
  rule for `C:\Dev\Beta1Assets\tools\rhubarb\Rhubarb-Lip-Sync-1.13.0-Windows\rhubarb.exe` in Claude Code's settings.**
- **S9 a month of Chapter 1 (Test Beta 1.1):**
  - **New numbers:**
    - The free Championship pays $2,493 for first (prizeBase 89; was $7,000).
    - Chapter 1 ends only when the Biscayne Classic is over: out on Day 1, Day 2 played, or the day gone by. The
      Classic is **Saturday 14 February 2026 (day 40)**, Day 2 Sunday the 15th, and there's only one Day 1.
    - The goal bar is now the **$5,000 Classic stake** with "N days to go"; crossing it sends Slick's one text, "Now
      you're not playing scared."
    - **Rent and living, $600 every Friday** on both paths, shown with the pay.
    - Title and end card say **Test Beta 1.1**; the build goes to `C:\Dev\Builds\TheDream-Beta1.1-Web`.
    - c1's narrator Goal line now reads "Goal: the Biscayne Classic, Saturday, February 14. Build your bankroll to
      $5,000 before it." (the rule changed, so the line had to).
  - **Check (keep, perfect bot):** the chapter ends on **day 41** (won Day 2: Biscayne Champion). Bankroll by week:
    Mon 12 Jan $4,493 · Sat 17 Jan $13,166 · Sun 25 Jan $39,899 · Sun 1 Feb $58,554 · Sun 8 Feb $85,397 ·
    Sat 14 Feb $110,336 · end $124,514. A bug the run found: Day 1 could be re-entered all Saturday (fixed, one Day 1).
  - **Not good yet:** a perfect player wins almost every event. The old game's placement formula (kept verbatim) puts
    100%-accuracy play first of 1,104, and the CF $55 Global Rush pays about $8,100 for that. So a perfect bot reaches
    six figures; a real player won't. If the cloud session wants it flatter even for perfect play, the lever is the
    online first-place payout or how often a perfect run finishes first, not the calendar.
  - **Four weeks, four beats: TODO for the cloud session** (no existing scenes, so nothing invented): W2 "Mustich shows
    up and the rival bar starts" (only c1 and the Classic films have him), W3 "Slick's debt / half-your-profits
    reminder and the job pressure", W4 "the Classic week (cameras, Slick's pie line)". W1 is c1 (Gnomey, Tommy).
- **Quit check (perfect bot):** ends on **day 41** too (Biscayne Champion). By week: Mon 12 Jan $4,493 · Mon 19 Jan
  $73,382 · Mon 26 Jan $125,895 · Sun 1 Feb $182,953 · Sat 7 Feb $240,011 · Sat 14 Feb $300,890 · end $323,201. Same
  perfect-bot caveat; both month runs still PASS on live vs online (`day2/auto_keep_month.txt`, `auto_quit_month.txt`).
- **S8 build:** `C:\Dev\Builds\TheDream-Beta1.1-Web`, **17.9 MB, 0 errors** (+2.6 MB for the table kit). I tried to
  read the console with headless Edge: the page loads, but the engine never logs there (no WebGL headless), so the
  console check is still the cloud session's. Films: 45 MB streamed, graded.

### Evening (16:20 on), the 16:25 list
**1. S10, film taps and sound (as revised 15:50)** · `1d90af4`
- **One film screen and one VideoPlayer for the whole session** (`Films.cs`). It's made on the first film, hidden
  between films, and reused by changing the url, so the page sees one `<video>` element all session, the one the first
  tap unlocked.
- **No full-screen tap-to-skip and no "tap to skip" label.** The page owns taps during films. `Skip()` stays for the
  drivers.
- **Two watchdogs:** a film that never starts (missing file) or whose clock stops is treated as over, so the story can't
  hang on black. One fired in testing: `goal.mp4` no longer exists and the old code waited forever.
- **Check** (editor, `C:\Dev\Builds\day2ilms_audio.txt`): `p_quit` → `p_keep` → `lesson1` with no taps between,
  1 VideoPlayer the whole time, each film unmuted at volume 1.00, clock advancing, handing over by itself at each end.
  The page-side check (headless Chromium with autoplay blocked) is for the cloud session on the published build.
- **Found on the way:** driven from the bridge, the editor is unfocused and stops ticking the game loop (frame count
  frozen), which looked like films stalling. The `main` test command now sets `runInBackground` at run time (player
  settings untouched).

**2. N1, US spelling** · `c8e11f3`
- The only British spelling left in player-facing text was "favourite" (the grade line, `ch1.json`).
- `export_ch1.py` now applies a US-spelling map to the old game's data, so a re-export keeps it. The diff is that one
  word; `spots.json` is byte-identical.
- The other hits (colour, grey, metres) are code comments.

**3. N2 narrator and N3 every unspoken line** (16:05 rules: no audition gate, texts voiced too)
- **The narrator** is LibriTTS speaker **498**, pace 1.12, −3 semitones, warmth 0.6, compression, a short plate
  (`aecho 0.8:0.5:40:0.2`).
  - How it was picked: I rendered "Everybody has a dream. Biscayne Card Room, off the causeway." in 151 speakers
    (every 6th id) and measured each one's median pitch.
  - The six deepest were 498 (79 Hz), 186, 342, 600, 174, 42. After the recipe, 498 sits at 65 Hz, the deepest by a
    clear margin, and stays cleanly voiced.
  - Audition, in that order: `C:\Dev\Builds\day2
arrator_audition.mp3`.
- **Voiced now:**
  - all 31 narrator cards in the films;
  - the reel's four title cards ("Everybody has a dream.", "Some people get a seat.", "Some people earn one.",
    "THE DREAM");
  - all 16 phone texts, in the sender's voice at 0.8 volume, after a soft two-note message tone (synthesized here,
    ours), with the bubble held until the voice ends;
  - Yarn's plain narration lines (the narrator).
  - Every in-person character line in the films already had a voice.
- **Left silent, by the rules:**
  - the 11 {key} cards: `{day2}`, `{day2_you}` ×3 (classic_mustich/other/shark), `{ft_intro}` ×2, `{ft_lost}`,
    `{ft_won}`, `{saturday}` (p_after);
  - one Yarn line with a {value} in it;
  - the choice card's #card/#hint lines (UI heading and option hints, not captions).
- **How:**
  - film cards get voice ids in `script_build.py`, and the player holds each card for its voice;
  - lines outside films go to `Resources/Voice/Lines/<key>.wav`, key = FNV-1a of "who|text", the same in Python and
    C# (checked: both give `bad533de` for "slick|Hi");
  - all 29 films re-rendered (card timing changes with the voice, so muxing audio alone would have overlapped lines),
    republished, and the reel re-cut with the narrator over its cards.


**4. Test Beta 1.1.3 built** (17:38): `C:\Dev\Builds\TheDream-Beta1.1-Web`, Build/ 19.7 MB (+1.8 MB for the voice lines), 0 errors, 29 films + reel; `BUILD_READY.txt` written.

### Task 5: balance by the month (evening, Opus · high)
Two real month-long bot runs on the game (driver, `auto <keep|quit> acc 0.6`), then 150–200 simulated months per case on the
real engine with the game's calendar (`Editor/SimCal.cs`, `simcal <keep|quit> <q> <runs>`: day-job schedule of 3 online + 1
weekend live a week and $1,250 on Fridays, or full-time 1 live + 1 online a day; $600 rent every Friday; Slick's $1,000
loan at rock bottom, half of profits back). Raw lines: `C:\Dev\Builds\day2\balance_calendar.txt`.

- Bot, keep, career accuracy 70%: bankroll $2,000 (Jan 12) → $2,485 (Jan 17) → $2,750 (Jan 25) → $3,125 (Feb 1) → $3,500
  (Feb 8); **$5,000 on day 34** (Feb 8), $6,489 at the end card, no loan. Every week beat fired (c1_w2 → c1_w4_eve2).
- Bot, quit: stalled on Jan 19 at Mustich's new choice (the driver didn't know it; taught), then on an unfocused editor;
  replaced by the calendar sim, which is many months instead of one.
- Placement with the online offset at 0.08 (commit 2b4b612): full-time 70% reached $5,000 in only 38% of months and needed
  Slick's loan in 61%. Moved the online offset to **0.02** (the suggested lever, online prizes for good players, via the
  field difficulty rather than the payout table):

| player | reaches $5,000 | median day (25–75%) | Slick's loan | median bankroll day 13 / 20 / 27 / 34 / 39 |
|---|---|---|---|---|
| keep, 70% | 69% | 25 (18–32) | 0% | $2,514 / $3,198 / $4,445 / $5,369 / $6,019 |
| quit, 70% | 56% | 18 (13–28) | 48% | $1,558 / $2,021 / $2,621 / $3,455 / $3,764 |
| keep, 55% | 31% | 32 (27–39) | 0% | $2,278 / $2,808 / $3,279 / $3,823 / $4,473 |
| quit, 55% | 0% | — | 99% | $646 / $805 / $617 / $672 / $613 |

Not good yet, said plainly: (1) "never before day 20" can't hold with real variance — a full-time 70% player who wins one
1,104-player online event is over $5,000 in week two (a quarter of the quit months reach it by day 13). Capping it would
need a schedule rule, not a balance number; Steven's call. (2) The keeping 55% player reaches $5,000 in 31% of months
almost on the pay cheque alone ($650 a week after rent). (3) The quitting 55% player never gets there and leans on the loan
all month, which matches "saved by Slick's loan", but more than once. Grading untouched.

### N2b: the narrator, again (evening, Opus · medium; INBOX 19:05, Steven 19:01)
- Cause, as the cloud said: the -3 semitone `asetrate` shift on a flat LibriTTS reader is the disguised-witness sound.
- Installed `kokoro-onnx` 0.6.1 (PyPI, MIT) and the Kokoro-82M v1.0 ONNX weights + voices (Apache-2.0; from the kokoro-onnx
  GitHub release "model-files-v1.0", outside the repo in `C:\Dev\Beta1Assets\dl\kokoro`). Logged in ASSETS.md.
- `tools/narrator.py`: Kokoro, no pitch shift, slowed only by Kokoro's speed; EQ only (low shelf +4 dB at 120 Hz, -2.5 dB at
  300 Hz, +2 dB at 3 kHz), 3:1 compression, a short warm room (two early echoes, no plate); phrases split at "/" and full
  stops with a 0.45 s breath.
- Audition `C:\Dev\Builds\day2\narrator_audition2.mp3`: am_onyx 0.9, am_fenrir 0.88, bm_george 0.9, am_michael 0.88. I can't
  listen, so I measured: am_onyx is naturally the deepest (median 86 Hz; fenrir 129, george 141, michael 118, lewis 100) with
  the steadiest line. **am_onyx at 0.9** voices every narrator line (cards, Yarn narration, the reel's title cards).
- The cast: no voice shifts more than 1 semitone now (Slick -1.5 -> -1, Tommy -2 -> -1, Bio -2.5 -> -1), still Piper.
- For tomorrow: Kokoro sounds far more natural than Piper, so it's worth trying for the cast, but its American male voices
  are few (onyx, michael, fenrir, puck, adam, echo, eric, liam) for nine men. Not changed tonight.

### Task 6: the month's weekly beats (CH1_WEEKS) — DRAFT
Nine films from `unity/docs/CH1_WEEKS.md` (c1_w2 + Mustich's choice a/b + c1_w2b, c1_w3_diner_keep/quit, c1_w4_diner,
c1_w4_eve + eve2) and the Friday/eve texts in Yarn (`Chapter1.yarn`: BeforeEvent, AfterEvent, AfterSleep; flags $wk2..$wk4b
saved). They fire on the calendar: c1_w2 before the first live game from Mon 19 Jan; the week-3 texts on Fri 30 Jan; the
week-3 diner after the next live game; Classic week from Mon 9 Feb; the eve on Fri 13 Feb. Checked in a real month-long bot
run (keep): all fired once, in order (`C:\Dev\Builds\day2\balance_keep_acc70.txt`, films list). **Steven hasn't read these
yet; they are a draft**, rendered with tonight's look and narrator.

### Task 7: the film look (Opus · high) — stills in `C:\Dev\Builds\day2\look\`
- Diner: the outside red neon 9 -> 5, the warm back light toward neutral, and per-set fill x1.6 and rim x1.7 (Sets.Look
  `fill`/`rim`): faces now have a cool shadow side and an edge instead of one deep red (`diner_before.png` / `diner_after.png`).
- Waxy skin: skin smoothness 0.46 -> 0.36, the wrap (terminator) 0.5 -> 0.62 and scatter 0.8 -> 1.0, so the falloff is softer
  and the highlight smaller. Hair shell smoothness 0.38 -> 0.22.
- Hair: Slick's cards longer, lifted 9 mm and denser (2,400), with more jitter, colour lifted a touch so it isn't a black
  helmet; Mustich's crop has 3,200 cards with strong jitter. **Not good yet:** Slick reads shaggy rather than slicked back,
  and Mustich's curls still read as chunky clay up close (`rhubarb_mustich.png`); real curls need strand-level curl in
  hair.py (tomorrow, Opus · high).
- The envelope insert: post exposure -1.5 stops on inserts, bloom 0.9 -> 0.2 on inserts (the blob was the chrome counter edge
  blooming), framed on the envelope (it had slid off the counter edge out of frame) (`insert_before.png` / `insert_after.png`).
  Still not perfect: the white mug in the corner glows.

### Task 9: IllusionRP (G6) — not adopted
- Licence: the Unity Companion License (not MIT/Apache/BSD, but not GPL and allowed for Unity projects; it would only touch the
  offline film renders). Cloned to `C:\Dev\Beta1Assets\dl\illusion`, project copied to `C:\Dev\Beta1-IllusionTest` (not in the
  repo; beta1 untouched).
- It doesn't compile on our Unity 6000.6 / URP 17.6: 12 errors. Its InternalBridge reaches into URP internals that 17.6
  renamed (`PostProcessPassRenderGraph`, `PostProcessMaterialLibrary`), and its UnityRHI dependency uses an editor API that is
  now an error (`CreateAssetWithContent`). It targets Unity 6.3 / URP 17.3 exactly. The rhi error is a one-line fix; the URP
  bridge is a port into URP's private code, and that would break again with every URP update. Downgrading means a second
  editor (6.3) and a separate film project. Decision: **stop here** (well inside the hour); we keep our own Skin/Hair shaders.
  Log: `C:\Dev\Beta1Assets\illusion_compile.log`.

### Task 10: Rhubarb lip sync
- The permission rule worked: `rhubarb.exe` (official 1.13.0 release, MIT, in `C:\Dev\Beta1Assets\tools\rhubarb`) ran from
  `python beta1/tools/lipsync.py` with each line's text as the dialog hint: 98 spoken film lines, 0 errors, cues in
  `FilmData/Voice/<id>.mouth.txt`.
- `Face.cs` plays Rhubarb's shapes (A closed, B teeth, C open, D wide, E round, F pucker, G f/v, H l, X rest) as open/spread/round
  on the MakeHuman mouth units; lines without cues fall back to the old loudness envelope. Mouth shapes now change per sound,
  with real closures on m/b/p (`rhubarb_mustich.png`).

## Milestone A (INBOX 20:35): Chapter 1 complete -> Test Beta 1.1.3
Install: Playwright for Python 1.x (PyPI, Apache-2.0) + its Chromium, to drive the web build at phone size (offline test tool).

### A1. Task 8 + B1 + B2 + B3
- **B1, the all-in screen (Sonnet · medium).** Cause: `CardImg` put an AspectRatioFitter (FitInParent) on a box anchored to its slot;
  FitInParent resets anchors to the WHOLE parent, so your two cards became one full-screen card and the opponent's two stacked
  into one. Each card now fits inside its own slot. The 3c2a2c0 layout is back (both hands side by side under YOU / opponent,
  the equity bar, the board in a row, the result line), kit faces at normal size; landscape gets its own rows in a centred
  column. Stills: `C:\Dev\Builds\day2\allin_fix_portrait.png` (430x932), `allin_fix_landscape.png` (932x430).
- **B2, table bigger / buttons smaller.** Portrait: the table area 45% -> 61% of the height, buttons 10% -> 6.5% tall, "Your move"
  gone, your cards between. Landscape: table 70% -> 75%, buttons 11% -> 8.5%. Seat text up a third (it was ~10 px at phone
  size), the pot under the logo in portrait too, bets placed so their labels clear the pot's. Stills: `day2/table_phone_*.png`.
- **B3, freezing after switching away (Opus · high).** Reproduced in Chromium at 430x932 (the build, the reel playing, the page
  marked hidden + `visibilitychange`, the page frozen over CDP for 15 s, Unity's own <video> paused as iOS does): on return the
  film's watchdog saw a clock stopped long ago and ended the film (or, with the loop still running while hidden, after 4 s);
  Unity also believes a browser-paused video is still playing, so nothing restarted it. Fixes:
  - `Plugins/WebGL/Resume.jslib` + `Game/Resume.cs`: the page tells the game when it is hidden/shown (visibilitychange,
    pageshow, focus). Hidden: save. Shown: unpause audio, and if the page was hidden, `Films.Wake()`.
  - `Films.Wake()`: pause + play the <video> (Unity's isPlaying can't be trusted after the browser paused it), and the watchdogs
    count from now. While hidden the watchdogs don't judge the film; a frame more than a second after the last one (the loop
    was asleep, the event may come later) also wakes it. A film that still won't play is ended by the watchdog 4 s later, so
    no screen ever waits on a video.
  - A lost WebGL context (iOS under memory pressure in the background) can't come back in Unity: the page reloads, and the
    game resumes from its save (it saves on every change).
  - Checked: after a 15 s hidden + frozen page the reel resumed at 5.4 s and played on; a forced context loss reloaded and
    resumed, 3 runs of 3. Not checked: a real iPhone, and Steven's index.html page guard (my test build uses Unity's default
    page, so I couldn't see whether his guard blocks taps).

### A2. B5: the apartment is home, with the Spacepad (Opus · high)
- **Home replaces the card-room hub.** After the Prologue the game lives in your Brickell studio at night: a Cycles still
  (`tools/blender/home.py --still`: AgX, 160 samples + denoise, the night HDRI through the blinds at 0.45, the lamp, the cabinet
  strip, the laptop's glow, pendant off). 3.5 min per frame on this laptop. Shipped as JPEG at phone size in
  `StreamingAssets/Home/` (portrait 596x1290 = 65 KB, landscape 1290x597 = 61 KB), loaded when the screen opens, not in
  the first download. On it: the date, the bankroll, the rep, last night's news, and Open the Spacepad / Closet / Sleep.
- **The Spacepad** (the planner): the bankroll, the stake bar and days to the Classic, the next Friday (rent and living, pay
  with the day job, what's owed to Slick), this week Monday to Sunday (today lit; LIVE / ONLINE by your job's rules, RENT on
  Friday, CLASSIC on the 14th), and tonight's games. Online games are played from it; a live game is a drive first.
- **The drive** (a card for now, as allowed): "Biscayne Card Room, off the causeway" with a line per ride (the bus, the '09
  hatchback, ...), 3 s or a tap, then the event.
- **The closet**, ported from `dream/data.js` DR_ITEMS: outfit (12), glasses (7), hat (7), chain (4), ride (4), home (3), with the
  old milestone tests mapped to this save (sessions = events, decisions, peak, titles, chapter). Items past Chapter 1 show
  locked with their requirement. What you wear is saved (`Save.eq`). "Studio over the laundromat" is renamed "Brickell studio"
  to match the set.
- Stills, phone size: `C:\Dev\Builds\day2\home_{hub,spacepad,closet,drive}_{430x932,932x430}.png`; the Cycles frames
  `home_still_cycles_*.png`.
- **Not good yet, plainly:** the closet has no picture of you yet. The pre-rendered layers (body + outfit + glasses + chain +
  hat, H2) are the next step, so tonight it's a list. The car is a text card, not a film. The still reads as a clean CG room:
  the blinds are a little flat and the sofa side is dark. The old game's planner rules (mark Online/Live days every Sunday),
  rent per home tier and the Daily Grind streak aren't ported; Chapter 1's own calendar rules stand.

### A3. B4 + task 6 + storyboards + the test menu
- **B4, goal fast-forward (Sonnet · low).** With the $5,000 stake made before Friday 13 Feb, home offers "Skip to the Biscayne
  Classic · Sat, Feb 14": every Friday on the way pays and charges as usual, the weekly beats you passed are marked played,
  the night before still plays, then the Classic day. Checked: day 20 -> 39, $5,200 -> $7,150 (three Fridays with the job),
  `c1_w4_eve` and the eve texts played.
- **Task 6, the weekly beats** (committed earlier tonight): fire in order in a full bot month; Steven hasn't read them (DRAFT).
- **Storyboards (THE BASE).** `tools/storyboard_build.py` reads `unity/docs/SCRIPT_ALL.md` from origin/unity (copy in
  `beta1/story/SCRIPT_ALL.md`) into `Resources/Story/scenes.json` (95 KB): 99 scenes, 81 without a film -> storyboards. A scene
  plays as a storyboard when it has no film, or when it's EDITED and its lines differ from its film's captions; those films
  are printed as "needs re-render" (today: none; no scene is EDITED yet). `Game/Storyboard.cs` plays it on the film screen:
  the place's still (`StreamingAssets/Sets`: office, diner, card room, home, from the films and the Cycles still), the
  speaker's portrait (or initials for anyone without one), the name, the words, the voice when a clip exists; tap or wait.
  Storyboard voices ship only for scenes this build reaches (c2_drive, c2: 13 lines; Chris Schillings has no voice yet).
  Mapping the scene tree's old speaker keys: derek = Lou, rosa = Gnomey, trent = Mustich, lou = Tommy, mom = David, n = narrator.
- **The test menu**: 5 taps on TEST BETA on the title. Bankroll -$1,000 / +$1,000 / $50 / $5,000; date -1 / +1 / +7 / Fri 13 Feb /
  Classic day / Day 2; job keep/quit; Go home; and every scene (film or storyboard), 12 a page.

### A4. The Chapter 1 ending -> Chapter 2's opening
- The ending as built: the Classic's Day 1 (`classic_day1` when you bag chips, Slick's "Day 2. Go to sleep." text), Day 2 (won:
  `trentbeat`; out: `classic_mustich` / `classic_shark` / `classic_other` by who beat you), the first title, then Slick's beat
  ("You've outgrown Biscayne. The Circuit stops at Silver Strike ...").
- New: right after it, once, Chapter 2's opening as a storyboard, `c2_drive` (Tommy drives you north) then `c2` (Silver Strike),
  then the end card "Chapter 2 · The Circuit — coming in the next build" (Send feedback / Keep playing Chapter 1 / Title).
  I included `c2_drive` because it's the scene tree's lead-in to `c2`; say if you want only `c2`.
- Keep run (perfect-play driver, `C:\Dev\Builds\day2\a5_auto_keep.txt`): reached the end card on day 41, Biscayne Champion;
  films in order: reel, p_office, p_diner, p_diner_yes, p_diner2, p_seat, ft_intro_tommy, ft_won_tommy, p_after, p_offer,
  p_keep, c1, badbeat, badbeat2, lesson1, c1_w2, c1_w2_a, c1_w2b, c1_w3_diner_keep, c1_w4_diner, c1_w4_eve, c1_w4_eve2,
  classic_day1, ft_intro_mustich, trentbeat, **c2_drive, c2**.
- Quit run (`C:\Dev\Builds\day2\a5_auto_quit.txt`): end card on day 41; films: reel, p_office, p_diner, p_diner_yes, p_diner2,
  p_seat, ft_intro_tommy, ft_won_tommy, p_after, p_offer, p_quit, c1, firstwin, badbeat, badbeat2, lesson1, c1_w2, c1_w2_a,
  c1_w2b, c1_w3_diner_quit, c1_w4_diner, c1_w4_eve, c1_w4_eve2, classic_day1, ft_intro_mustich, trentbeat, **c2_drive, c2**;
  "PASS: no live/online mismatch". Its first try stopped on a blank home screen: the apartment-still cache kept a texture the
  editor had destroyed between play sessions (editor-only; a build plays one session); the cache now reloads a dead entry.
- Both runs use the perfect-play driver, so they test the path, not the balance (their bankrolls balloon).

### A5. Rhubarb + autoplay + build
- Rhubarb lip sync on all 98 spoken Chapter 1 film lines (task 10, committed earlier); the films were re-rendered with it.
- Autoplay keep and quit both reach the end card (above).
- **Build: Test Beta 1.1.3**, C:\Dev\Builds\TheDream-Beta1.1-Web, Build/ 22.5 MB (first download, under 25 MB), 0 errors;
  smoke-tested in Chromium at 430x932 (boots, the reel plays, hidden 15 s and back: "film reel.mp4 resumed at 5.3s").
  `C:\Dev\Beta1Assets\BUILD_READY.txt` written; BUILD_PARTIAL_1.1.3.txt removed (superseded).

## Milestone B (INBOX 05:00): Chapter 2 playable + track H -> Test Beta 1.1.4
Plan, in order (each committed): Batch 1, Chapter 2: 1 data (Sonnet · medium), 2 engine: rings, the satellite seat,
Chapter 2 difficulty (Opus · medium), 3 calendar + flow (Opus · high), 4 Chapter2.yarn on storyboards with Steven's choices
(Opus · high), 5 voices (Sonnet · low), 6 autoplay keep/quit to Chapter 3's start + build 1.1.4 (Sonnet · medium).
Batch 2, track H: H1 the test (Opus · high), then H2 where it wins.

### Batch 1: Chapter 2, the Circuit
- **Data**: the old game's six Chapter 2 events (Mega Satellite $180 -> 16 seats in the Main, $2/$5 cash, $400 Opener, $600
  Double Stack, $1,700 Main, CF Tilt $55 Sunday Kickoff) with payouts for every field, and the three legends.
- **Calendar** (my call, written in Chapter2.cs): the Circuit stops at Silver Strike every 8 weeks from Thursday 12 March,
  11 days a stop (satellite, Opener x2, Double Stack x2, cash days, satellites, the Main on the second Saturday, the charity day
  last). Between stops you grind from home (the Winn Nightly, CF Tilt). Sunday Kickoff every Sunday. With the day job, a stop
  allows both weekend days (otherwise the Main could never be played after a Sunday ring event).
- **Rules**: rings count (Rep: Circuit Grinder -> Ring Winner -> Two-time Ring Winner); a satellite seat pays the Main's buy-in
  ("Your seat"); the hand engine runs at Chapter 2. Goal: 2 rings and $40,000 -> ring2 -> Chapter 3's opening (c3) -> end card
  "Chapter 3 · The Summer — coming in the next build". A 1.1.3 save that finished Chapter 1 goes on into Chapter 2.
- **Story** (`Chapter2.yarn`, all storyboards until filmed; Steven's words from SCRIPT_ALL.md): c2_drive + c2 right after
  Chapter 1's end; c2_fatts the first night at the Strike; c2_mouth your first Circuit final table; ring1 your first ring;
  lesson2 your first ring-event bust; c2_smokes after ring1 with $10,000+ during a stop (Sit down: c2_smokes_sit, -$6,000);
  c2_cowboy on the second stop's charity day; ring2 at the goal. The two choices' options come from the script, so his edits
  show up. Storyboards can play part of a scene (`c2_smokes@a` before the choice, `@1` the chosen answer, `@b` after).
- **Voices**: every Chapter 2 storyboard line and choice answer; five legends on Piper speakers nobody else uses (Armadillo
  Fatts, Mouthy Michael, Sammy Smokes, Chris Schillings, Colt the Cowboy), **picked without listening: placeholders.**
  "hellbent" in c3 has no voice.
- **Keep run** (perfect play, `C:\Dev\Builds\day2\b_auto_keep.txt`): end card on day 125, Two-time Ring Winner. Chapter 2's
  films/storyboards in order: c2_drive, c2, c2_fatts, c2_mouth, ring1, c2_smokes@a, c2_smokes@1, c2_smokes_sit, c2_smokes@b,
  ring2, c3. Not seen (a perfect player): lesson2 (no ring bust) and c2_cowboy (goal met before the second stop's charity day).

### Batch 2, track H: H1, the test (Opus · high)
`tools/blender/h1_test.py`: the diner (diner.py) + Slick's exported character, his materials rebuilt from the manifest, skin
as Principled BSDF with random-walk subsurface, varied roughness and a pore bump; warm pendants, the reduced neon, key, cool
fill and rim; AgX; 85 mm at f/2.8 focused on the eyes. **`C:\Dev\Builds\day2\real_slick_ecu.png`**: (c) Unity real time,
(a) Eevee 62 s/frame, (b) Cycles 64 + OIDN 321 s/frame on this laptop.
- Honest read: Cycles gives the skin a softer, truer falloff than Eevee or Unity, and depth of field is real. But all three
  read as the same CG face. The limit is the asset, not the renderer: the MakeHuman base mesh and its flat skin texture, the
  card hair (it renders light grey in Blender; the tint path needs work), eyes without a modelled cornea. At 5 min a frame,
  Cycles films would be ~12 hours per minute of film on this laptop.
- **Not done in H1** (it's the actual next step): real strand hair (MPFB hair to curves + Principled Hair BSDF), a refractive
  cornea, a skin texture with real detail. H2 should start with the head, not the renderer. Suggestion for Steven: for the
  cast who agreed, a head built from their reference photos (kept outside the repo) is what moves faces toward real;
  legends stay original.
- The apartment test is the Cycles home still already in the game (3.5 min/frame, A2), against the Unity home film frame.
- **Quit run** (`C:\Dev\Builds\day2\b_auto_quit.txt`): end card on day 124; this time lesson2 fired too (a ring-event bust).
- **Chapter 2 balance** (`sim`, 300–400 entries per event at the event's own chapter): at first a ~70% player won only
  0.7–1.3% of ring entries (two rings in dozens of stops) and lost $112 a $2/$5 session. Now: a ring-event placement offset of
  0.20 (`Hands.PlaceRing`) and the $2/$5 table soft (0.6, tourists). Per entry, 70% / 55% / perfect: Opener ring 12.5% / 2.5% /
  ~65%, Double Stack 8.3% / 1.5%, Main 8.8% / 1.8%; $2/$5 -$8 / -$221 / +$775 a session. So a 70% player full-time (about five
  ring entries a stop) expects two rings in about five stops (~9 months of game time); with the day job (three a stop) longer.
  Side effect said plainly: ring events are slightly +EV even at 55%. Chapter 1 is untouched (the offset only touches rings).

### Batch 1, step 6: Test Beta 1.1.4
- First build: Build/ 31.2 MB, over the 25 MB budget. Chapter 2's voices (in Resources, quality 1) were 14 MB of sound.
  Fixes: the voice lines now stream (masters in `beta1/Voice/Lines`, 112 MP3s at 48 kbps mono in `StreamingAssets/Voice`,
  3.8 MB, preloaded in the background from the title; a line whose clip hasn't arrived reads silently); an import rule
  for what stays in Resources/Voice (quality 0.35, mono); the four table plates crunch-compressed (no visible banding).
  **Build/ 17.2 MB** (1.1.3 was 22.5), 0 errors. Chromium smoke test at 430x932: boots, "voices preloaded: 112 of 112",
  hidden 15 s and back, the reel resumes.

### INBOX 12:10: hotfix, no cash, Chapter 2's goal -> Test Beta 1.1.4 (rebuilt)
- **Hotfix (Opus · high)** — Steven on iPhone: opening the Spacepad on the quit path crashed to the title and the save was gone.
  - Not reproducible in Chromium (430x932, quit path, Spacepad twice: no exception, ~59 MB JS heap). What would turn a small
    bug into this on a phone: the web build had **exceptions "explicitly thrown only"**, so any null reference aborts the
    whole game (in the editor it's just a log line); and memory could grow to 2 GB where iPhone Safari kills a tab far sooner.
  - Fixes: the web build catches exceptions (full, no stack traces; Build/ still 17.3 MB) and caps memory at 1 GB; home,
    the Spacepad and the closet catch their own errors (logged, kept in `LastError` for the test menu, the player lands home);
    the save is **mirrored to localStorage on every write** (synchronous, unlike IndexedDB's sync) and `Save.Load` takes the
    newest valid copy by `rev`; only "New career" erases a career (and keeps the old one aside in localStorage).
  - Proved in Chromium on the build: the quit-path Spacepad opens; reload -> Continue (rev 2, from PlayerPrefs) -> home;
    IndexedDB deleted + reload -> Continue (rev 2, **from the mirror**). Stills: scratch `sp/`, copied to `day2/hotfix_*.png`.
  - For the cloud's index.html (I don't touch docs/): it renders at devicePixelRatio up to 2.5 (an iPhone canvas of ~1075x2330
    plus URP's buffers) and primes 32 <video> elements at the start tap; both cost phone memory. Suggest dpr <= 2 (or 1.5 on
    iPhones) and fewer primed videos. Not tested on a real iPhone.
- **No cash games** (Steven 10:40): the $2/$5 game is out of Chapter 2 (data, calendar, driver; the cash code stays, never
  offered; Chapter 1 had none). Its stop days became Mega Satellite days; the charity day has no live event. Gnomey's c1 line
  ("One-two has a seat in ten") is Steven's new one; re-voiced, lip-synced, c1 re-rendered and published.
- **Chapter 2's goal** (Steven 12:09): a ring OR $15,000, counted once you've been to the Strike (otherwise a player who left
  Chapter 1 above $15,000 would skip the chapter). ring2 (the Lucky Seven, about Colt) plays only if you met the Cowboy;
  then c3. The c2 narrator Goal line and ring2's "Two rings and forty thousand" line are overridden in
  `tools/storyboard_build.py` until the scene tree has Steven's wording (listed in the INBOX for the cloud).
- **Balance for the new goal** (`simcal2`, 200 runs from a typical Chapter 1 ending): the ring offset tuned for two rings
  (0.20) ended the chapter in ~6–10 days at 70%. Now 0.07: 70% keep median 51 days (25–75%: 10–67), full-time 15 (7–63),
  a ring decides it 13–19% of the time; 55% keep ~6 months. (`C:\Dev\Builds\day2\b_balance_ch2.txt`)
- Keep autoplay on this code: end card on day 68 (the perfect driver left Chapter 1 with $91K, so Chapter 2 ended after its
  first night at the Strike, as the rule says).
- **Build: Test Beta 1.1.4**, Build/ 17.3 MB, 0 errors.

### Track H, H2 step 1: real strand hair (Opus · high)
`tools/blender/strands.py`: strands grown over the hair shell along hair.py's flow (Slick: back), as Blender hair curves with
Principled Hair BSDF (melanin 0.9); the cards hidden, the shell matte and dark underneath. `h1_test.py -- <engine> --strands`.
`C:\Dev\Builds\day2\real_slick_strands.png`: Cycles with cards (H1) | Eevee 20k strands, 50 s | Cycles 60k strands, 179 s.
- Reads as hair now: dark, strand-level, the rim light catching individual hairs at the sides; the cards read as a grey
  helmet. Cycles with strands is faster than with cards (179 s vs 321 s: the alpha-cut cards were the expensive part).
- Not good yet: the top of the head is out of frame in this ECU (check a wider shot next); the left ear blows out under the
  key in Cycles; the face is still the MakeHuman face (skin texture and eyes are the next H2 steps).

### Track H, H2 step 2: a wider look (`h1_test.py -- cycles --strands --mcu`, 124 s/frame)
`C:\Dev\Builds\day2\real_slick_mcu_strands.png`, with an eye light (catchlights) and a more frontal key. Plainly, from the MCU:
- the strands give a true slicked-back silhouette, but lie thin and flat on top (needs more density/volume on the crown);
- the face is still MakeHuman's smooth CG face, the ear still pale; the body is in the import's A-pose (a still needs a pose);
  the jacket's texture shows its pattern oddly at this size.
- Conclusion for track H: strands are worth adopting for Cycles stills and inserts now; photoreal faces need a better head
  (a detailed skin texture, modelled eyes, ideally the consenting cast from their reference photos), which is Steven's call
  on tools and licences (see the 1.1.4 notes). Until then the next H steps are posing the stills and the hair's crown.
