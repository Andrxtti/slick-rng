# Confirmed Fire — guide for Claude Code

Owner: Steven (GitHub Andrxtti). Two products live here:
- **Confirmed Fire Poker** — the solver/trainer web app (`app/`, `docs/`, `engine/`, `spots/`, `tools/pf*`). Live site = `main`.
- **The Dream** — a story-career poker game. Web build in `dream/` (the content source of truth), Unity 6 port in `unity/`.

**Current mission: The Dream — Launch Beta 1.0. Read `unity/docs/BETA1.md` in full before doing anything.**

## Hard rules
- Work on branch `beta1` (made from `unity`). Commit and push after every task. Never push to `main` (it is the live site)
  and never write into `docs/` (GitHub Pages).
- The solver is off-limits: don't touch `tools/pf`, `tools/pfdream`, `app/`, `docs/`, `engine/`, `spots/`, and never
  regenerate `unity/Assets/Dream/Resources/Dream/spots.json`. Never commit `gw/` or any course data.
- Real people: a face is recreated only for people who agreed (the list is in BETA1.md, "Cast and consent"). Every legend
  is an original face. Never download or save photos of real public figures. Never put anyone's photo in this repo (it is
  public); reference photos live outside it in `C:\Dev\CastRefs`.
- Coaching is credited to "Confirmed Fire" only (no course or coach names).
- Licences: no GPL/AGPL code shipped inside the game (offline tools like Blender are fine). Art and assets CC0 or CC-BY
  only; record every asset's source, author and licence in `unity/docs/ASSETS.md`.
- Install only free/open-source tools from official sources (winget, extensions.blender.org, OpenUPM, the Unity
  registry), log each install in the night report, and change no system settings.
- The Unity editor is normally open on the project: drive it with the remote commands (`Logs/dream_cmd.txt`, see
  BETA1.md) instead of killing it. Never close Unity with unsaved scene changes; batch-mode builds only when it is closed.
- Plans and to-do lists: every task is tagged with the model and effort it needs ("Opus · high", "Sonnet · medium",
  "Haiku · low"), ordered, worked in batches, then a fresh list.
- Check your own work before calling it done: compile clean, look at rendered frames (ffmpeg contact sheets), compare
  with the "before" stills, and say plainly what is not good yet.

## How to work (Karpathy guidelines, MIT, github.com/forrestchang/andrej-karpathy-skills)
1. **Think before coding.** State assumptions. If several readings exist, pick the one BETA1.md supports and write it
   down in the night report; don't silently guess. Push back on approaches that won't reach the goal.
2. **Simplicity first.** The minimum code that solves the problem. No speculative features or abstractions.
3. **Surgical changes.** Touch only what the task needs; match the existing style; don't reformat or "improve" adjacent
   code; remove only what your own change orphaned.
4. **Goal-driven execution.** Turn each task into a check you can run (a compile, a render you inspect, a size you
   measure), then loop until it passes.

## Map
- `dream/` — web game (data.js, story.js, engine.js, script.md). Content changes start here, then
  `python unity/tools/export_dream.py` re-exports Unity's data (never spots.json).
- `unity/Assets/Dream` — the Unity code and resources (repo copy is the source; copy changes into the project).
- `unity/docs/` — ART_DIRECTION.md, STATUS.md, BETA1.md (mission), characters/ (cast briefs), TOOLS.md (open-source
  tool survey), ASSETS.md (asset licences).
- `tools/daz/` — headless Daz Studio scripts (build_char.dsa + chars/*.json specs).
- `tools/` other folders — PC worker jobs used by the cloud session; leave them alone.
