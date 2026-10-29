# The Dream — Launch Beta 1.0 (overnight brief for Claude Code on Steven's PC)

Written 2026-10-29 01:15 ET by the cloud Claude session, from tonight's conversation with Steven. Read all of this, then
`CLAUDE.md` (rules), `unity/docs/ART_DIRECTION.md`, `unity/docs/STATUS.md`, `dream/README.md` and
`unity/docs/characters/*.md`. Then work the task list at the bottom, in order, committing after each task.

## 1. What Beta 1.0 is

Steven wants a demo good enough to show people: friends who are lending their faces, poker pros he will ask for
permission, partners, and short videos for social media. In Steven's words, "a AAA level game, with stunningly acted and
scenic cutscenes". The bar is realistic people and places, film-quality cutscenes, and a polished first chapter.

When the app launches:
1. **Highlight reel** (60–90 s): a movie-trailer preview of the whole journey, made to hook. Floor 34 at 6:47 PM, the
   2:13 AM diner meeting with Slick, the card-room grind, a Circuit ring, the Vegas bracelet, the TV final table, the Titan
   high roller, the Main Event against Bio "The Ghost", the Hall of Legends. Quick cuts on music, a few spoken lines as
   stings (existing voice files), title cards, ending on "THE DREAM". It is rendered offline and played as a video. Tap to
   skip.
2. **Prologue as film**: `p_office` and `p_diner` (then `p_seat`, `p_after`) play as rendered cutscenes with the new sets
   and characters.
3. **Playable Chapter 1** ("The Grind", the Biscayne Card Room), through its goal: win the Biscayne Classic or build a
   $10,000 bankroll. Includes `c1`, `trentbeat`, `lesson1`, the `classic_*` scenes, `ft_*` and aftermath scenes.
4. **End card**: "Beta 1.0 — Chapter 2: The Circuit, coming soon" with a way to send feedback.

Delivery: cutscenes and the reel are rendered on this PC and played back as video, so phones only play video. The live
parts (home, the table) run in URP on phones and must stay light. The web build (iPhone Safari) is the beta channel.

## 2. Honest limits (plan around them, don't fight them)
- **The PC**: Intel i7-1355U with Iris Xe integrated graphics, 16 GB RAM, no NVIDIA GPU. Rendering and lightmap bakes are
  slow but fine offline. Anything that needs CUDA (most AI face/mocap research code) won't run here.
- **Unreal/MetaHuman is blocked** for now (Epic account entitlement bug, ticket open). Characters come from Blender.
- **The face videos haven't arrived yet.** Eric and the rest of the cast are sending ~15 s head-turn videos. Until then,
  build each consenting character from the reference photo in `C:\Dev\CastRefs` as closely as one photo allows. Keep the
  head swappable, so a scanned head can replace it later without re-rigging the body.
- **Priority is depth over breadth.** One diner scene that looks real beats ten scenes that look like the old ones.
  Prove each step on the diner first.

## 3. Cast and consent
Faces are **recreations** only for people who agreed to it. Steven has their messages (2026-10-29). Reference photos
are in `C:\Dev\CastRefs` (outside the repo; never commit them):

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
  `unity/docs/characters/legends.md` (the inspiration sheet: era, build, clothes, props, manner) when it lands on the
  `unity` branch. Never download or use photos of the real pros.
- **You (the player)**: the closet's face sculpting stays. Give the default player a realistic MPFB base.
- **Extras** (dealers, rail, staff): three shared MPFB bodies with swappable heads, hair and outfits.

## 4. Tools on this PC (installed tonight)
- **Unity 6000.6.7f1**: project `C:\Dev\Confirmed Fire Dream` (URP 17.6). Editor:
  `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`. The project's `Assets/Dream` is a copy of
  `unity/Assets/Dream`: edit in the repo, copy changed files into the project, then send `refresh`. Never edit only the
  project copy.
- **Remote commands** (the editor is open): write one command per line into
  `C:\Dev\Confirmed Fire Dream\Logs\dream_cmd.txt`. The editor runs it within a second or two and appends the result to
  `Logs\dream_cmd_out.txt`. Useful commands:
  - `refresh`: recompile and import.
  - `render <sceneId>`: full-frame 1920×1080 film, UI hidden, to `C:\Dev\Builds\Cutscenes\<id>.mp4`.
  - `render-ui <id>`: the phone screen with its UI, as played.
  - `render-all`, `play`, `stop`, `scene <id>`, `snap <name> [bust|face|full|room]`, `build-web`, `pkg-add <id[@ver]>`,
    `resolve`, `status`, `acting-build`, `cinema-profile`.
  - Full list: `Editor/DreamRemote.cs`. Compile errors land in `Logs\Editor.log` (grep `error CS`).
- **Render pipeline**: `Editor/DreamRender.cs` (Unity Recorder 5.1.3) plus film mode in `CharacterStage`
  (`CharacterStage.Film`). The post look is in the `Resources/Dream/Cinema` volume (`Editor/DreamCinema.cs`).
  - "Before" stills are in `C:\Dev\Builds\Cutscenes\before\`, the first film render in `...\Cutscenes\film\`.
  - Known issue: the last shot of `p_diner` frames the back of a head.
- **Blender 5.2.3**: `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`.
  - Add-ons: **MPFB 2** (MakeHuman: realistic bodies, skins, clothes, rigs; generated characters are CC0), **Rigify**,
    **Node Wrangler**. FBX and glTF exporters are built in.
  - Headless runs: `blender -b -P script.py -- args`. Add `--online-mode` to install more extensions:
    `blender --online-mode -c extension install -e <id>`.
- **Daz Studio 4.24** + free Genesis 9 content: `C:\Program Files\DAZ 3D\DAZStudio4\DAZStudio.exe`. The headless builder
  is `tools/daz/build_char.dsa`. The free content has no East Asian male face, little men's hair, no stubble and no
  trousers. Use Blender/MPFB as the main route; Daz is optional.
- **Yarn Spinner 3.2.8** (MIT, from OpenUPM) is in the project. Use it only for new branching dialogue if it helps. The
  seven chapters already run on our own script system (`dream/story.js` → `Story.cs`); don't migrate them.
- **ffmpeg**, **Python 3.12** and **Git**: installed tonight via winget. Open a new terminal so they're on PATH.
- **Free asset sources** (CC0): Poly Haven (models, textures, HDRIs; it has an API), ambientCG (PBR textures). Record
  everything in `unity/docs/ASSETS.md`.
- **Open-source tool survey**: `unity/docs/TOOLS.md` will land on the `unity` branch during the night (characters and
  faces, animation and mocap, environments and trailer). Merge `origin/unity` into `beta1` when it appears and use its
  shortlist.

## 5. How the game is built (what you'll change)
- **Sets** are built at runtime by `Scripts/Character/HomeRoom.cs` from `Resources/Dream/Homes/set_<place>.json`: box
  walls, simple props from `Props/*.bytes`, point lights. The realistic route is a set made in Blender (or assembled in
  Unity from CC0 assets) and saved as a prefab under `Resources/Dream/Sets/<place>`.
  - `CharacterStage.EnterScene` should load that prefab when it exists and fall back to the JSON set otherwise.
  - Keep the `you`/`them` marks and the camera shot kinds (`Shot()`: ext, wide, two, cu, ots, push, ecu, hands...)
    working. `direction.json` (from `unity/tools/direction.py`) holds each scene's shot list.
- **Characters** are UMA bodies today (`Scripts/Character/UMABody.cs`, created through `Bodies.Create`, interface
  `ICharacterBody`). The realistic route is an `ICharacterBody` that loads a Blender-made FBX (MPFB body, Rigify →
  Unity Humanoid, face shape keys), set up as Humanoid so the Mixamo acting clips in `Anim/Mixamo` still play.
  - Mouth and expressions come from shape keys: lip-sync from the existing voice files. uLipSync (MIT) is the planned
    route; check TOOLS.md.
  - Skin, eyes and hair need proper URP materials: subsurface approximation, wet eye highlight, anisotropic hair.
  - The UMA path stays as the fallback until the new bodies cover the cast.
- **The cinema look** already exists: depth of field, bloom, ACES and a grade. Lighting per set follows ART_DIRECTION §1
  (Miami at night: tungsten inside, cool neon outside, one accent colour per set).
- **Flow**: `Scripts/Game/Game.cs` (flow), `Story.cs` (scenes), `Play.cs` (events, grading), `State.cs` (saves),
  `Calendar.cs`. Story text comes from `dream/story.js` / `dream/script.md` via the export.

## 6. Tasks (in order; commit and push to `beta1` after each)

**Batch 0 — setup** · Opus · low
0. `git clone -b unity https://github.com/Andrxtti/slick-rng C:\Dev\slick-rng`, then `git checkout -b beta1`.
   - Check that `Assets/Dream` in the Unity project matches the repo (hash a few files), and that `blender`, `ffmpeg`,
     `python` and `git` run.
   - Send `refresh`, wait for the compile, then `render p_diner`, and look at a contact sheet of the result. This is the
     baseline.

**Batch 1 — prove realism on the diner**
1. **Diner set, realistic** · Opus · high. Build the Lucky Seven at 2:13 AM, per ART_DIRECTION and `dream/script.md`
   p_diner:
   - Chrome-edged counter and stools, red vinyl booths, a pie case with light inside, pendant lights, the red neon
     "LUCKY SEVEN" sign, a wet street outside the window, a bell over the door.
   - Use Poly Haven/ambientCG PBR materials, then bake lighting with one or two real-time lights for faces.
   - Wire it in through the prefab path described in §5.
   - Done when: `render p_diner` stills look like a real place next to `before/` (put the pair in
     `C:\Dev\Builds\night-shots\`).
2. **First realistic character: Slick placeholder** · Opus · high. MPFB 2 adult male per `slick.md`:
   - Lean, angular, East Asian, black hair styled up, the glasses, the black zip jacket.
   - Rigify → Unity Humanoid, face shape keys (visemes plus blink, smile, brow), proper skin, eye and hair materials.
   - Wire it in as the `ICharacterBody` for Slick in scenes; the UMA fallback stays for everyone else.
   - Done when: diner close-ups hold up at 1080p. Eric's scanned head replaces this face later.
3. **Lip-sync and acting** · Opus · medium. Voice lines drive the mouth, with blinks every 3–6 s and eye saccades.
   - Re-render p_diner. Fix the back-of-head shot, and any shot whose framing misses (eyes on the upper third).

**Batch 2 — the rest of the opening**
4. **Floor 34 office set** (6:47 PM, sunset over Brickell through glass, monitor glow) · Opus · medium. Re-render
   `p_office`.
5. **The consenting cast** from `C:\Dev\CastRefs` (§3) · Opus · medium each, in this order: Natso (Lou, the office),
   David, Mustich, Tommy, Gnomey, Twogloves, Bio. Same pipeline as Slick, with swappable heads.
6. **Player default body** (realistic MPFB) and the three extras bodies · Opus · medium.

**Batch 3 — the highlight reel**
7. **Script the reel** · Opus · high. Write a 60–90 s cut to `unity/docs/REEL.md`: shots, lines, music beats, title
   cards. Draw on the chapter openers (`c1`–`c7`), `bracelet1`, `tour1`, `titan1`, `champ`, the legends' lines and Bio.
8. **Minimum sets for the reel's shots** · Opus · medium each: card room, casino floor, Vegas room, TV table, Main Event
   stage, Hall of Legends. Dress for the camera: only what each shot sees.
9. **Render and cut the reel** · Opus · medium. Render each shot, cut with ffmpeg (titles, music, fades), export H.264
   1080p plus a 720p mobile version. Music must be CC0/CC-BY (record it in ASSETS.md) or silence plus the existing
   stings.

**Batch 4 — Beta 1.0 flow and Chapter 1**
10. **Launch flow** · Sonnet · medium. On a new career: reel (VideoPlayer, skippable) → prologue films → Chapter 1 → end
    card. Returning players skip the reel (with a replay option in Story).
11. **Card room and table polish for playable Chapter 1** · Opus · medium. The card room set for the live parts must
    stay light enough for phones.
12. **Build** · Haiku · low. Run `build-web` and measure the payload; the first screen should appear in under 10 s on LTE.
    Stage the build in `C:\Dev\Builds\TheDream-Web`. Do not publish: Steven decides.
13. **Night report** · Opus · low. Write `unity/docs/NIGHT_REPORT.md`:
    - What was done per task, with commit hashes.
    - Before/after stills, saved in `C:\Dev\Builds\night-shots`.
    - What still looks fake and why, and what you need from Steven (videos, decisions, music).

    Push it.

If a task stalls for more than about an hour, write down why in the report, leave it in a working state, and move on to
the next one. Never leave `beta1` failing to compile.

## 7. Not tonight
The solver, the hand library, `spots.json`, online play, store/accounts, publishing to `main`/`docs/`, Unreal.
