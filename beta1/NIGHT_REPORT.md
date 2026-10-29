# The Dream — Beta 1.0 night report (2026-10-29, 01:15–03:45 ET)

Written by Claude Code (Opus 5.5) on Steven's PC from `unity/docs/BETA1.md` (the 01:25 "built fresh" brief on
`origin/unity`, merged into `beta1`). Everything is committed and pushed to `origin/beta1`.

## Morning change (08:30–09:00): the job decision is back
Steven asked for the quit-or-keep-the-job decision in Beta 1.0. Done, matching the old Unity game (`Calendar.cs`,
`Story.cs`, `Career.cs`):
- **Script:** `p_after` ends on "Decision time." New `p_quit` (you tell Lou to redo it himself; Slick's $5,000; David's
  all-caps text) and `p_keep` (you keep the lanyard; Saturdays are yours; Slick's "a paycheck buys you time to learn").
  Voiced with Piper and rendered as films. Stills: `night-shots/p_after_sheet.jpg`, `p_quit_sheet.jpg`, `p_keep_sheet.jpg`.
- **The choice:** after the `p_after` film, a full-screen card ("Quit. Today." / "Keep the job. Grind nights.") plays the
  chosen film, then `c1`. The choice is saved (closing the app on the card brings the card back). Still:
  `night-shots/job_keep_choice.png`.
- **Chapter 1 rules:**
  - **Quit:** Slick's `GiftQuit` $5,000; one live game a day, any day; title "Full-time Dreamer".
  - **Keep:** $2,000 savings; live card-room games on weekends only, one per week (weeks run Sunday to Saturday, as in the
    old game); +$2,000 every Friday; title "Moonlighter". The hub shows the date, why a game is locked, and a Sleep button
    that skips to the next playable day (old `SkipTo`).
  - **Both:** "Biscayne Champion" replaces either title once the Classic is won. The bankroll now starts at $0 before the
    free Local, as in the old game (my $1,000 starting savings is gone).
- **Lines that change with the job, carried over:** going broke (Slick's way out differs: old `broke` vs `broke_n`), and
  David's "TEN THOUSAND?? … AND YOU STILL WENT TO WORK TODAY???" at $10,000 for Moonlighters (old `david1`). The stake is
  now really repaid from half your profits, as the old message promised. The Chapter 2 lines that change with the job
  (Chris Schillings's intro, quit2, quit250, derek_job) are past the beta and not built.
- **Checked by playing:** a test driver (`auto quit|keep [lose|broke]` on the bridge) played the game from the title to
  the end card six times. Logs are in `beta1/Logs/auto_*.txt`; screenshots are `night-shots/job_*`.
  - Quit: one game a day, Monday through Sunday; the stake taken when broke and repaid in full.
  - Keep: Monday locked, Saturday with the Friday paycheck, then one game per weekend; David's text before the goal.
  - Not reached in play: the Moonlighter's broke text. Fridays kept refilling the roll first, which is the day job working.
- **Noticed while testing:** with the day job, after a Sunday game the next one is the *following* Sunday, never the
  Saturday between them (one live game per Sunday-to-Saturday week). That's the old game's rule; Steven
  confirmed (2026-10-29) to keep it for now. Changing it later is one line in `Flow.CantPlay`.
- **Also fixed:** close-ups blocked by Lou's back (wider actor clearance), and the first shot of a film aiming before the
  seated pose had settled. Web build rebuilt (13.1 MB, 0 errors; 15 films).

## Watch these first
- `C:\Dev\Builds\Beta1\Reel\the_dream_reel_1080p.mp4` (73 s; 720p version next to it). This is the highlight reel.
- `C:\Dev\Builds\night-shots\p_diner_beta1.mp4`: the diner, acted. `diner_before_after_slick.jpg` puts old and new side by side.
- `C:\Dev\Builds\night-shots\cast_lineup.jpg`: Slick, you, Lou, David, Mustich, Tommy, Gnomey, Nick, Bio.
- `C:\Dev\Builds\Beta1\Film\*.mp4` and `*_sheet.jpg`: every film and a one-still-per-line contact sheet.
- The web build: `C:\Dev\Builds\TheDream-Beta1-Web` (staged, not published). Serve the folder (e.g. `python -m http.server`
  in it) and open it on a phone on the same Wi-Fi.

## Story logic in Yarn Spinner (09:00–09:30)
Steven asked for Yarn Spinner (MIT, OpenUPM `dev.yarnspinner.unity` 3.2.8, as in the old project) to run the story.
- **Where things live:** `story/SCRIPT.md` stays the readable script of the *acted films*. The *story logic* moved into
  `Assets/Dream/Resources/Story/` (`Prologue.yarn`, `Chapter1.yarn`, compiled by `Dream.yarnproject`). It covers which
  film plays, the choices, the story variables, the conditions on later lines, and the phone texts. SCRIPT.md says so at
  the top. I didn't move the films' dialogue into Yarn: films are rendered from SCRIPT.md, and Yarn only decides which
  one plays.
- **Nodes:**
  - `Decision` plays `p_after`, asks quit/keep on the choice card, sets `$job`, pays $5,000 or $2,000, then plays
    `p_quit`/`p_keep` and `c1`.
  - `BeforeEvent` plays the Classic's opener the first time.
  - `AfterEvent` handles the job decision after the Local; the broke texts by job via `<<detour Broke>>`; `classic_win`,
    `classic_lose`, `needle` and `lesson1` on their conditions; then `CheckGoal`.
  - `AfterSleep` lets a paycheck end the chapter.
  - `ChapterEnd` sends David's texts for Moonlighters, then the goal and end films.
- **Commands:** `<<film id>>` (waits for the film), `<<money n "why">>`, `<<stake n>>`.
- **Line tags:** `#text` (phone texts), `#card` and `#hint` (the choice card).
- **Variables:**
  - The game passes in `$bankroll`, `$peak`, `$chapter`, `$room_title`, `$event`, `$won`, `$bust` and `$mistake`
    before each run.
  - Yarn owns `$job` and the seen-flags; they're saved with the career.
  - The calendar rules (one game a day, weekends with the job, Friday paychecks, titles) stay in C#: they're rules, not
    story.
- **Code:**
  - `Scripts/Game/Story.cs` is the runner bridge plus a presenter that draws Yarn's lines and choices in the game's UI.
  - `Editor/StorySetup.cs` creates the Yarn project through Yarn Spinner's own utility.
  - The C# copies of the broke texts, David's text, the decision and the film picking were removed from `Flow.cs`.
- **Checked by playing** (driver runs in `Logs/auto_*.txt`):
  - Keep: Yarn played `p_keep`, `needle`, `lesson1`, the Classic opener and `classic_lose`, then ended the chapter
    after a paycheck with David's texts.
  - Quit: one game a day; Yarn's quit-version broke texts and the stake, repaid in full.
  - The Moonlighter's broke texts, by running the `AfterEvent` node directly with `$job` = nights and a $10 bankroll
    (`story` bridge command; screenshot `night-shots/job_keep_broke.png`). Play couldn't get there reliably: even the
    worst answers cashed at $70.
- **Bug found:** `InMemoryVariableStorage.TryGetValue<bool>` throws on a string variable instead of returning false,
  which froze the game on the choice card until I read variables by type.
- **Web build:** 13.8 MB (+0.7 MB for Yarn), 0 errors.

## Table seat order (Steven's bug, 09:50)
- **Seat order:** seats went counter-clockwise. They now run clockwise, so the next player in order (UTG, UTG+1, UTG+2,
  LJ, HJ, CO, BTN, SB, BB) sits to your left (`Table.cs`, `a = -PI/2 - k*2*PI/n`).
- **Checked on four real library spots** (test command `spot <pos>`; screenshots `night-shots/table_seats_*.png`):
  - you in CO: BTN bottom-left, SB left, BB upper-left;
  - you in BB: UTG to your left;
  - you in BTN: SB to your left;
  - you in UTG: UTG+1 to your left, BB bottom-right.
- **Dealer button:** a white "D" puck sits beside the BTN seat.
- **Bet labels:**
  - an action that carries its size shows once ("Jam 7.9", not "Jam 7.9 7.9");
  - a fold shows "Fold" (not "Fold 0.5");
  - blinds show their amount;
  - a player with nothing behind shows "all in".
- **Web build:** rebuilt with this and the Yarn work: 13.8 MB, 0 errors.

## Calls I made without you
1. **Which brief.** The local checkout had the earlier BETA1.md (port into the old project). Your instructions matched
   the newer "fresh project at `beta1/`" brief on `origin/unity`, so I merged that and followed it.
2. **Cutscenes are data, not Timeline.** `story/SCRIPT.md` → `tools/script_build.py` → one JSON per scene → a small
   `Film/Player` that cuts between computed shots (lens per ART_DIRECTION, eyes on the upper third, handheld drift,
   push-ins, camera collision) and plays voices, Mixamo acting, blinks, look-ats and lip-sync. Why: tonight was authored
   headless. Data means you edit a line in SCRIPT.md, then run `voice.py` and `render <scene>`, with no editor work.
   Timeline can still be layered on for hand-tuned hero shots.
3. **Sets and characters are generated and kept out of git.** `tools/blender/*.py` builds every set and character from
   CC0 sources (`tools/fetch_assets.py` re-downloads them), into `Assets/Dream/FilmSets` and `FilmCast` (gitignored).
   The repo stays small and public-safe, and every look is reproducible. Film-only data lives outside `Resources`, so
   none of it enters the phone build.
4. **Rig.** MPFB's `mixamo_unity` rig instead of Rigify: it maps straight to Unity Humanoid, so the Mixamo acting clips
   play unchanged. Faces use the 21 MakeHuman expression units (blink, brows, smile, open/round/spread/close for
   lip-sync); the viseme and ARKit packs aren't installed and weren't needed.
5. **Beards and Bio's mask are painted/generated in code** (no beard assets exist in the free pack). The mask print is
   our own, made by code.
6. **Music.** No CC0/CC-BY track was fetched. The reel's score (drone, impacts, riser, boom) and the room tones are
   synthesized with ffmpeg here, so they're ours. Swap in a real track when you pick one (`tools/reel_cut.py`).
7. **Films have no choices.** The game asks them between films (the job decision is a choice card after `p_after`;
   see the morning change above). Going broke brings a $1,000 stake from Slick, repaid from half your profits.
8. **Chapter 1 events:** the four live card-room events (1/2 cash, Midnight Rush, Sunday Deepstack, the Classic). The
   CF Tilt online events and the freeroll were left out to keep the beta to the card room.
9. **Feedback link** on the end card opens a GitHub issue on the repo. Change `Flow.FeedbackUrl` if you'd rather email.
10. **Git identity:** set for this repo only (Andrxtti / your email); no global settings changed.

## Per task (commit)
| # | task | commit | result |
|---|---|---|---|
| 0 | Fresh project at `beta1/` (URP 17.6, Cinemachine 3.1.7, Timeline, Recorder 5.1.7, Input System, glTFast), bridge, Recorder render | d7c3ca0 | Test scene rendered to 1080p30 H.264 and inspected |
| 1 | New script `story/SCRIPT.md` | 0192d60 | 12 scenes, 64 voiced lines, 10 stings, the reel plan |
| 2 | Voices (Piper) | 804976a | Every line voiced, levelled to −18 LUFS-ish, one speaker per character |
| 3 | Diner set | b8a711f | Lucky Seven: chrome counter, 8 stools, 3 red booths, pie case, pendants, LUCKY SEVEN neon, wet street, bell |
| 4–5 | Slick, the player, the diner acted | e0e90a8 | 15-shot film with lip-sync, blinks, look-ats, film lighting |
| 6 | Floor 34 office and scene (+ Lou) | 654e252 | Sunset through the glass, the Henderson deck on screen |
| 7 | The consenting cast | becdbd6 | David, Mustich, Tommy, Gnomey, Twogloves, Bio (and Lou above) |
| 8 | Card room and Chapter 1 scenes | 0ea8656 | 10 tables, sodium light, list board, the champions board; c1, p_seat, needle, classic, classic_win/lose |
| 9 | Reel sets | c38ffa3 | Casino ring, Vegas bracelet, TV table (Nick), Titan trophy, Main Event (Bio), Hall of Legends |
| 10 | Reel cut | (this commit) | 73.3 s, 19 shots, title cards, stings; 1080p 20 MB, 720p 4.6 MB |
| 11 | Hand library and grading | 0ea8656 | spots.json byte copy (sha256 checked). **Same-grade check: 26,609 hands × every answer = 67,896 grades, 0 differences** against the old LabelFor (copied as text by `tools/grade_check_gen.py`) |
| 12 | Game flow | 18a6bcc | Title → reel (first launch, skippable, replay in hub/title) → prologue films → Biscayne Championship → p_after + c1 → hub → events → results → story films → goal → end card |
| 13 | Table design | 18a6bcc | Generated felt and rail, seats with stacks and actions, four-colour cards with drawn suits, chess-style grade panel |
| 14 | Web build | (this commit) | Succeeded, 0 errors. See "Build" below |
| 15 | This report | (this commit) | |

A bug I found and fixed along the way, worth knowing for the old project too: Mixamo's **"Talking" clip is a seated
clip** (head at 1.10 m) and **"Pointing" half-crouches** (1.28 m), against about 1.52 m for true standing clips. Standing
characters using them sink. The old project maps standing talkers to them.

## Build (task 14)
`C:\Dev\Builds\TheDream-Beta1-Web`, WebGL with gzip and the decompression fallback (works on any static host).
- Engine payload (Build/): **13.1 MB**: wasm 8.5 MB, data 4.4 MB (most of it the hand library), loader and framework 0.1 MB.
- Films (StreamingAssets/Films, 720p): 19 MB in all, streamed only when each plays. The reel is 4.6 MB.
- First screen on LTE, by arithmetic only (not tested on a phone): about 5 s to download at 20 Mbit/s plus wasm
  startup, so **borderline under 10 s**. At 10 Mbit/s it's ~10.5 s and misses. The easy wins: Brotli on a host that
  serves it, and a smaller wasm (trim Cinemachine/Timeline/glTFast/Recorder out of the player, which uses none of them).
- Not checked tonight: iOS Safari's video autoplay rules for the reel (the first tap may be needed to start sound).
- A Windows Firewall prompt for `adb` (Unity's Android tools) is waiting on screen. I left it alone (system setting).

## Before / after (C:\Dev\Builds\night-shots)
`diner_set_before_after.jpg`, `diner_before_after_slick.jpg`, `diner_after_*.png`, `office_before_sheet.jpg` vs
`office_after_*`, `reel_sheet.jpg`, `cast_lineup.jpg`, `game_table.png`, `game_grade.png`.

## What still looks fake, and why
- **Faces.** MPFB/MakeHuman bodies with one diffuse skin map: no pores, no specular detail, slightly plastic, with
  helmet-like hair cards. Likeness from one photo is approximate (Lou and Gnomey are closest, thanks to their beards).
  The head-turn videos are the real fix. The heads are the body mesh today; a scanned head can replace the face
  without re-rigging.
- **Beards** are painted on the skin, so they read at film distance but not in extreme close-ups. The cheek edge
  shows UV stepping.
- **Hair** is MakeHuman's few short styles. No ginger crop, no bowl cut, no slick-back (Tommy, Slick). Bio's fringe is close.
- **Clothes** are stand-ins: Slick's zip jacket is a tinted casual jacket. There are no hoodies, polos, track jackets
  or Lou's red tie. Mustich's and Nick's glasses aren't made yet.
- **Acting** is canned Mixamo clips plus procedural face/head motion. It's believable in two-shots and stiff in long
  close-ups. Lip-sync is loudness-driven, not phoneme-accurate.
- **Sets:** the diner and office hold up best. The card room is dark and generic beyond the hero table. The reel
  vignettes are graphic rather than real (the Hall's portraits are deliberately abstract, since legends must be
  original faces). There are no baked lightmaps; everything is realtime.
- **Films are rendered at 1080p, shipped at 720p.** The `end` film is just a card.
- **The table** is clean but plain: no chip animations or card dealing motion yet.

## What I need from you
1. **Face videos** (Eric first): ~15 s head turns, neutral and smiling. Then I'll build the heads properly.
2. **Music:** pick a CC0/CC-BY trailer track, or keep the synthesized score.
3. **Decide:** the feedback route (GitHub issue vs email).
4. **Publish** the web build when you're happy (I didn't).

## Installs and downloads (all free, official sources)
| What | From | Licence | Where |
|---|---|---|---|
| Unity packages: URP 17.6.0, Cinemachine 3.1.7, Timeline 1.8.12, Recorder 5.1.7, Input System 1.20.1, uGUI 2.6.0, glTFast 6.20.0 | Unity registry | Unity Companion / Apache-2.0 | `beta1/Packages/manifest.json` |
| Yarn Spinner for Unity 3.2.8 | OpenUPM (dev.yarnspinner.unity) | MIT | `beta1/Packages/manifest.json` |
| MakeHuman system assets | files.makehumancommunity.org | CC0 | MPFB user data folder |
| Piper 2023.11.14-2 (Windows) + en_US-libritts-high voice | github.com/rhasspy/piper releases, huggingface rhasspy/piper-voices | MIT / CC-BY 4.0 data | `C:\Dev\Beta1Assets\tools\piper` |
| Poly Haven textures, models, HDRIs (listed in ASSETS.md) | api.polyhaven.com | CC0 | `C:\Dev\Beta1Assets\polyhaven` |

## How to re-make anything
- Script → data → voices: `python beta1/tools/script_build.py && python beta1/tools/voice.py`
- Sets: `blender -b -P beta1/tools/blender/<diner|office|cardroom>.py` and `reelsets.py -- <casino|vegas|tv|titan|main|hall>`
- Characters: `blender -b -P beta1/tools/blender/character.py -- beta1/tools/blender/chars/<name>.json [--preview]`
- Films (editor open on beta1): write `render p_diner c1 ...` into `beta1/Logs/dream_cmd.txt`; stills: `python beta1/tools/stills.py <id>`
- Reel: `python beta1/tools/reel_build.py` (scenes), render `reel_*`, then `python beta1/tools/reel_cut.py`
- Game films: `python beta1/tools/films_publish.py`; web build: remote `build-web`
- Grade check: remote `grade-check` → `beta1/Logs/grade_check.txt`

## Next list (model · effort)
1. Faces from the videos, Eric first · Opus · high
2. Hair and clothes made for the cast (Blender cards, jacket/hoodie/polo/track jacket, glasses props) · Opus · high
3. Card room depth: light pass, extras at the other tables, chip/card motion at the live table · Opus · medium
4. Real music in the reel, re-cut · Sonnet · low
5. Phone pass on the web build (load time on LTE, video autoplay rules on iOS Safari) · Sonnet · medium
