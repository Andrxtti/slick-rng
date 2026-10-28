# The Dream — overnight work brief (Claude Code, PC)

Written 2026-10-28 by the solver session. This is the hand-off for an unattended Claude Code session working on the
game. Read all of it before touching anything. Work through the tasks in order; commit after each one.

## Setup (once)

- Clone the repo fresh: `git clone -b unity https://github.com/Andrxtti/slick-rng C:\Dev\slick-rng` and work there.
  Do NOT work inside `C:\Dev\cfp-worker` (that is the solve worker's folder; it is running solver jobs all night) and
  do not touch `tools/pf`, `tools/pfdream`, `app/`, `docs/` or anything solver-related.
- Create the branch `dream-night` from `unity` and commit there; push after every task (`git push -u origin dream-night`).
- The Unity 6 project is `C:\Dev\Confirmed Fire Dream`. Its `Assets/Dream` is a copy of the repo's `unity/Assets/Dream`:
  edit in the repo, then copy the changed files into the project before building. Never edit only the project copy.
- Compile/build check (headless, ~19 min; the PC is busy with solves, so at most 3 builds a night, and only after a
  batch of changes):
  `"C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Dev\Confirmed Fire Dream" -executeMethod CFDream.EditorTools.DreamBuild.BuildWeb -logFile C:\Dev\Builds\night-build.log`
  Find the installed version under `C:\Program Files\Unity\Hub\Editor`. A build that fails to compile is a failed task:
  fix it or revert the change; never leave the branch red.
- Game data source of truth is the web build under `dream/` (data.js, story.js, spots.js); Unity's
  `Resources/Dream/dream_data.json` and `spots.json` are exported from it with `python3 unity/tools/export_dream.py`.
  Do not regenerate `spots.json` tonight: the hand library is being re-solved on the worker and the solver session
  will export it in the morning.

## Context

- `unity/README.md` (project layout, build menu), `unity/docs/STATUS.md` (state after Beta 1.82, to-do list, notes),
  `dream/README.md` (game design: story, grading, progression), `dream/script.md` (the script).
- Scripts: `unity/Assets/Dream/Scripts/{Core,Data,Game,UI}`; the flow controller is `Game/Game.cs`, events and
  grading `Game/Play.cs`, saves `Game/State.cs`. Direction/shot lists: `unity/tools/direction.py` -> direction.json.
- Coaching is credited to "Confirmed Fire" (no course name, no coach name). Chip-EV grading comes from the solved
  library; never hand-write strategy.

## Tasks (in order)

1. **Steven's test notes.** Fix list from his Beta 1.70/1.82 testing; items known so far from STATUS.md: opening
   order of the first scenes, the diner scene, joystick feel at home, body clips on UMA characters (odd poses, sliding
   feet in Talking / Breathing Idle / Victory / Defeated / Fist Pump), voice colour of narrator / Slick / Tommy /
   Bio, load time (data file 70 MB: find what is in it and what can be lazy-loaded or compressed).
   Steven adds his notes below under "Steven's notes" before the session starts; treat those as the real list.
   (Sonnet-level work; review anything touching Calendar, Play.Start or SceneEnd twice.)
2. **Load time.** Measure the web build's payload (`C:\Dev\Builds\TheDream-Web`), list the biggest assets, and cut
   the first-load size without losing content: compress textures/audio, strip unused Mixamo clips from the acting
   set, lazy-load later chapters. Target: first screen in under 10 s on a phone over LTE. Report before/after sizes.
3. **Versus / player type (range adjust).** Keep it simple, per Steven: an opponent type is a percentage expansion
   or contraction of the solver's range for that seat, nothing more. Add to the data a small table of types
   (e.g. Tight-passive: open −30%, defend −20%, 3-bet −50%; Loose-passive: open +40%, defend +40%, 3-bet −40%;
   Maniac: open +60%, 3-bet +80%, jam +50%; Nit: open −50%, 3-bet −60%) and a `versus` mode in the trainer where
   the hero's grading uses the EV against the adjusted villain range when the library spot carries per-hand EVs.
   Where the spot cannot be re-valued (no EVs), fall back to the plain solver grading and say so in the UI.
   Build the UI (pick a type before a session, show it on the table) and the data; the EV re-valuation can be
   approximate: scale the villain's continue frequency and recompute fold-equity for the hero's raise/jam.
   Document the method in `dream/README.md`.
4. **Set dressing for chapters 3–7** (props and lights per set, like the diner pass) only if 1–3 are done and a
   build has passed. Take screenshots into `C:\Dev\Builds\night-shots` for Steven to judge.

Not tonight: anything in the solver, the hand library export, satellites, PLO, online play, store/accounts.

## Finish

Write `unity/docs/NIGHT_REPORT.md`: what was done (per task), what was built and whether it compiled, open questions
for Steven, and the exact commit hashes. Keep it short and factual. Push it.

## Steven's notes

(paste testing notes here)
