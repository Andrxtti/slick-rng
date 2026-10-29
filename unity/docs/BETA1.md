# The Dream — Launch Beta 1.0, built fresh (overnight brief for Claude Code on Steven's PC)

Written 2026-10-29 01:25 ET by the cloud Claude session, from tonight's conversation with Steven. Read all of this, then
`CLAUDE.md` (rules), `unity/docs/ART_DIRECTION.md`, `dream/README.md`, `dream/script.md` and `unity/docs/characters/*.md`.
Then work the task list at the bottom, in order, committing after each task. Steven is asleep: don't stop to ask. Make
the most reasonable call, write it in the night report, and keep going until every batch is done or truly blocked.

## 1. The ask, in Steven's words, and what it means

"Use the new tools and write new script, build new characters, all as if it's from scratch but with our background
info / direction", "a new Unity project too, but without overwriting the other in case we go back", and "a AAA level
game, with stunningly acted and scenic cutscenes".

So:
- **A brand-new Unity project.** It lives at `C:\Dev\slick-rng\beta1` (in the repo, on branch `beta1`).
  - The old project is the fallback and stays untouched: `C:\Dev\Confirmed Fire Dream`, plus `unity/` and `dream/` in the
    repo. Read them for reference. Copy over only what is proven and listed in §5. Never modify, move or rebuild the old
    project.
- **A new script**, written from scratch for Beta 1.0. Keep the story's direction (§3) and the cast. New scenes, new
  dialogue, new beats, new voices.
- **New characters, sets and cutscenes**, built with Blender (MPFB 2, Rigify), CC0 assets and Unity's film tools.
- **Kept:** the story bible, the cast and consent rules, the art direction, and the solver-graded hands. The hand
  library and its grading are the product's core. Steven will later combine this game with the solver, so the hands must
  stay exactly as solved.

Audience: friends who are lending their faces, poker pros Steven will ask for permission, partners, and short social
videos. The bar is realistic people and places, film-quality acted cutscenes, and a polished first chapter.

## 2. What Beta 1.0 plays like
1. **Highlight reel** (60–90 s, plays on first launch, tap to skip): a movie-trailer preview of the whole journey, made to
   hook. It covers:
   - Floor 34 at 6:47 PM, and the 2:13 AM diner meeting with Slick.
   - The card-room grind, a Circuit ring, the Vegas bracelet, the TV final table, the Titan high roller.
   - The Main Event against Bio "The Ghost", and the Hall of Legends.

   Quick cuts on music, spoken lines as stings, title cards, ending on "THE DREAM". Rendered offline, played as video.
2. **Prologue as film**: the office and the diner, then the first tournament seat. These are rendered, acted cutscenes
   with the new script, voices, sets and characters.
3. **Playable Chapter 1** ("The Grind", the Biscayne Card Room) through its goal: win the Biscayne Classic or build a
   $10,000 bankroll. Every decision is a solver-graded spot from the existing hand library. Chapter 1 has its own story
   scenes (lessons from Slick, Mustich's needling, Tommy "The Shark", the Classic).
4. **End card**: "Beta 1.0 — Chapter 2: The Circuit, coming soon" with a way to send feedback.

Delivery: cutscenes and the reel are rendered on this PC and played back as video, so phones only play video. The live
parts (home, the table) run in URP on phones and must stay light. The web build (iPhone Safari) is the beta channel.
Keep URP. HDRP doesn't run on phones or the web.

## 3. Story direction (keep) — details in `dream/README.md`, `dream/script.md`
A post-grad stuck in a corporate job (Floor 34, Brickell, Miami; boss Natso SweetLou, "Corporate Lou") meets a stranger
at the Lucky Seven Diner at 2:13 AM. The stranger is Slick. Slick gives him a free seat into the local championship.
- **The arc:** card room grind → Circuit rings → summer bracelets → Tour titles → Titan high rollers → the Main Event
  against Bio "The Ghost" → the Hall of Legends.
- **Supporting cast:** David Pec (best friend, texts in ALL CAPS), Mustich (smug rival), Tommy "The Shark" Maio (the card
  room's self-appointed mayor), Miles "The Snowplow" Gnomey (floor manager), Nick "Baby Goat" Twogloves (stream host),
  and 27 legends (parody tributes).
- **Tone:** grounded, Miami at night, a little funny, never cartoonish. Slick is quiet menace and economy. Coaching is
  credited to "Confirmed Fire" only.
- **Bankroll** is the score and the gate. Unlockables are earned, never bought.

You may restructure scenes, cut, add and rewrite. Keep the premise, the names, the 2:13 AM diner meeting and the
chapter goals.

## 4. Honest limits (plan around them)
- **The PC**: Intel i7-1355U, Iris Xe integrated graphics, 16 GB RAM, no NVIDIA GPU. Rendering and bakes are slow but
  fine offline. Anything that needs CUDA won't run.
- **Memory**: don't run two Unity editors at once. Steven closes the old project before you start.
- **Unreal/MetaHuman is blocked** (an Epic account bug). Characters come from Blender.
- **Face videos haven't arrived.** Build each consenting character from the reference photo in `C:\Dev\CastRefs` as
  closely as one photo allows, with a swappable head so a scanned head can replace it later.
- **Depth over breadth.** One diner scene that looks and plays real beats ten average ones. Prove each step there
  first.
- **Repo size**: the repo is public on GitHub.
  - Commit code, scenes, prefabs, materials and small textures.
  - Keep `.blend` sources, big textures and raw renders out of git, in `C:\Dev\Beta1Assets`, with a manifest of where each
    came from (CC0 sources can be downloaded again).
  - Use a proper Unity `.gitignore` (Library, Temp, Obj, Logs, UserSettings, Builds).

## 5. What to bring over from the old project (port deliberately, read the code first)
- **The hand library**: `unity/Assets/Dream/Resources/Dream/spots.json` (copy as-is, never regenerate). Also the code
  that loads and grades it: `Scripts/Data/DB.cs` (spots part) and `Scripts/Game/Play.cs` (events, grading, chip swings,
  coolers, placement, payouts).
  - Keep the exact grading behaviour. Write a quick check that the same spot and answer grade the same in both projects.
- **Event and progression data** for Chapter 1: events, payouts and the Chapter 1 goal, from `dream_data.json` (exported
  from `dream/data.js`). Take only what Chapter 1 needs.
- **Voice approach**: Piper (offline neural TTS) per `unity/tools/voice.py`, with the per-character voice settings
  there. Install Piper for Windows from its official GitHub releases, and re-voice the new script.
- **Proven tricks** worth re-using: the remote-command bridge (`unity/Assets/Dream/Editor/DreamRemote.cs`, which lets
  you drive the editor while it's open), offline renders with Unity Recorder (`Editor/DreamRender.cs`), and the
  cinema volume (`Editor/DreamCinema.cs`).

Everything else is new. Write it fresh, simply, in the style CLAUDE.md asks for.

## 6. Cast and consent
Faces are **recreations** only for people who agreed (Steven has their messages, 2026-10-29). Reference photos are in
`C:\Dev\CastRefs` (outside the repo; never commit them):

| Character | Reference | Brief |
|---|---|---|
| Slick (Eric Chang "SlickRic") | Eric's video is coming. Until then, the brief only (East Asian, lean, angular) | characters/slick.md |
| Mustich | `mustich.jpg` (only the man with glasses; never the child) | characters/mustich.md |
| David Pec | `david.png` | characters/david.md |
| Natso SweetLou ("Corporate Lou") | `natso.jpg` | characters/natso.md |
| Tommy "The Shark" Maio | `tommy.webp` (the face only; the film's character, costume and branding stay out) | characters/tommy.md |
| Bio "The Ghost" | `bio.png` (our own mask print) | characters/bio.md |
| Miles "The Snowplow" Gnomey | `gnomey.jpg` (no club crest or sponsor marks) | characters/gnomey.md |
| Nick "Baby Goat" Twogloves | `twogloves.jpg` (none of the real player's record or biography) | characters/twogloves.md |

- **Legends (27, `dream/data.js` DR_LEGENDS)**: parody tributes, so every face is original. Use
  `unity/docs/characters/legends.md` (the inspiration sheet) when it lands on the `unity` branch. Never download or use
  photos of the real pros.
- **The player**: a realistic default with face sculpting.
- **Extras** (dealers, rail, staff): a few shared bodies with swappable heads, hair and outfits.

## 7. Tools on this PC
- **Unity 6000.6.7f1**: `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`.
  - Create the project headless: `Unity.exe -batchmode -createProject C:\Dev\slick-rng\beta1 -quit`. Then set up URP
    (package plus pipeline asset) by script, or open it once in the editor.
  - Free Unity packages worth using: Universal RP, Cinemachine, Timeline, Recorder, Input System. Optional: Yarn Spinner
    (MIT, via OpenUPM).
  - Recorder 5.1.3 resolved in the old project; it came from a local `file:` package, so check what installs cleanly.
  - Compile errors: run with `-logFile`, then grep `error CS`.
- **Blender 5.2.3**: `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`.
  - Add-ons: **MPFB 2** (realistic bodies, skins, clothes, rigs; output is CC0), **Rigify**, **Node Wrangler**. FBX and
    glTF exporters are built in.
  - Headless runs: `blender -b -P script.py -- args`. Install more extensions with
    `blender --online-mode -c extension install -e <id>`.
- **Daz Studio 4.24** (`C:\Program Files\DAZ 3D\DAZStudio4\DAZStudio.exe`) with free Genesis 9: optional. The free
  content is thin. `tools/daz/` has a headless builder.
- **ffmpeg, Python 3.12, Git**: installed tonight. Use a new terminal so they're on PATH.
- **Free asset sources** (CC0): Poly Haven (models, textures, HDRIs; it has an API), ambientCG. Record every asset in
  `beta1/ASSETS.md`.
- **Open-source tool survey**: `unity/docs/TOOLS.md` lands on the `unity` branch during the night. Merge `origin/unity`
  into `beta1` when it appears and use its shortlist (face scanning, mocap, lip-sync, hair, sets).
- **References from the old project**:
  - "Before" stills: `C:\Dev\Builds\Cutscenes\before\`.
  - Old film render: `C:\Dev\Builds\Cutscenes\film\`.
  - Old live web beta: https://andrxtti.github.io/slick-rng/beta/app/.

## 8. Suggested architecture (change it if you find better, and say why)
- **Cutscenes**: Timeline + Cinemachine. Authored shots per scene (camera, lens, focus, blocking), acting from Mixamo or
  mocap clips plus additive face animation, lip-sync from the voice files, rendered with Recorder to 1080p H.264, and
  played in-game with VideoPlayer. The script lives in a plain text format (Markdown or Yarn) that a build step turns
  into scene data, so Steven can edit lines.
- **Characters**: one pipeline script per character. MPFB in Blender → Rigify (game rig) → FBX with face shape keys
  (visemes, blink, brows, smile) → Unity Humanoid. URP materials for skin (subsurface approximation), eyes and hair.
- **Sets**: built in Blender or assembled in Unity from CC0 assets, with baked lighting and one or two real-time lights
  for faces. ART_DIRECTION §1 sets the palette: Miami at night.
- **Game**: a small, clean flow. Title → reel → prologue → Chapter 1 hub (events) → table (solver spots) → results →
  end card. Saves in PlayerPrefs. UI Toolkit or uGUI, your call. It must work at phone portrait size.

## 9. Tasks (in order; commit after each; push when you can)

**Batch 0 — fresh project** · Opus · medium
0. `git pull`, `git checkout -b beta1`.
   - Create the Unity project at `beta1/` (URP, the packages in §7) with a Unity `.gitignore`.
   - Add a remote-command bridge and a Recorder render command.
   - Done when an empty lit test scene renders to an mp4 you have inspected.

**Batch 1 — the new script and voices**
1. **Write the Beta 1.0 script** · Opus · high. Write `beta1/story/SCRIPT.md`:
   - The reel, the prologue scenes, and Chapter 1's scenes and stings.
   - Every scene gets its place, time, cast, beats, lines and a shot plan.
   - Make it dramatic, specific and short enough to render tonight.
   - Keep §3. Give Slick the best lines.
2. **Voices** · Opus · medium. Use Piper on Windows, with one consistent voice per character (start from voice.py's
   settings). Generate every line in the script.

**Batch 2 — prove realism on the diner**
3. **Diner set** · Opus · high. The Lucky Seven at 2:13 AM:
   - Chrome-edged counter and stools, red vinyl booths, a lit pie case, pendant lights, the red neon sign, a wet street
     outside, a bell over the door.
   - Use CC0 PBR materials and baked light.
4. **Slick and the player** · Opus · high. Use the full character pipeline (§8) and the briefs.
5. **The diner scene, acted and rendered** · Opus · high. Blocking, camera, acting, lip-sync, rendered with Recorder.
   - Done when: a contact sheet and a few full-res stills hold up next to the old "before" stills. Put the pair in
     `C:\Dev\Builds\night-shots\`.

**Batch 3 — the rest of the opening**
6. **Floor 34 office set** (6:47 PM, sunset over Brickell, monitor glow) and the office scene · Opus · medium.
7. **The consenting cast** from `C:\Dev\CastRefs` (§6) · Opus · medium each, in this order: Natso, David, Mustich, Tommy,
   Gnomey, Twogloves, Bio.
8. **The card room set** (live play must stay phone-light) and Chapter 1's scenes · Opus · medium.

**Batch 4 — the highlight reel**
9. **Reel sets, dressed only for what each shot sees** · Opus · medium each: casino floor, Vegas room, TV table, Titan
   lounge, Main Event stage, Hall of Legends.
10. **Render and cut the reel** with ffmpeg (titles, music, fades) · Opus · medium. Export 1080p plus a 720p mobile
    version. Music must be CC0/CC-BY (record it in ASSETS.md), or use silence plus stings.

**Batch 5 — playable Chapter 1**
11. **Port the hand library and grading** (§5), with the same-grade check · Opus · high.
12. **The game flow** · Sonnet · medium: title, reel, prologue, Chapter 1 hub and events, table, results, Chapter 1
    goal, end card.
13. **Table design**: felt, chips, cards, type, motion · Opus · medium.

**Batch 6 — ship the beta build**
14. **Build** · Haiku · low. Web build into `C:\Dev\Builds\TheDream-Beta1-Web`. Measure the payload; the first screen
    should appear in under 10 s on LTE. Do not publish: Steven decides.
15. **Night report** · Opus · low. Write `beta1/NIGHT_REPORT.md`:
    - What was done per task, with commit hashes.
    - Before/after stills, saved in `C:\Dev\Builds\night-shots`.
    - What still looks fake and why.
    - What you need from Steven: videos, decisions, music.

If a task stalls for more than about an hour, write down why in the report, leave it in a working state, and move on.
Never leave `beta1` failing to compile.

## 10. Not tonight
The solver itself, regenerating the hand library, online play, store/accounts, publishing to `main`/`docs/`, Unreal,
and any change to the old Unity project.
