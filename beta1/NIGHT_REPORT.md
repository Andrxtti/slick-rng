# The Dream — Beta 1.0 night report (2026-10-29)

Written by Claude Code (Opus 5.5) on Steven's PC, working from `unity/docs/BETA1.md` (the 01:25 "built fresh" version
on `origin/unity`, commit 1aceafa, merged into `beta1`).

## Calls I made without you (read these first)
1. **Which brief.** The local checkout had the earlier BETA1.md (port into the old project). The newer one on
   `origin/unity` says: new project at `beta1/`, new script and voices. Your instructions to me matched the newer one,
   so I merged `origin/unity` into `beta1` and followed it.
2. **MakeHuman asset pack.** MPFB 2 was installed but had no skins, eyes, brows, hair or clothes. I downloaded the
   official CC0 "makehuman_system_assets" pack (files.makehumancommunity.org, 268 MB) into MPFB's user data folder.
   Logged in ASSETS.md. It is an asset download, not a tool install.
3. **Cutscene authoring.** §8 suggests Timeline + Cinemachine. I drive cutscenes from data instead: `story/SCRIPT.md`
   → a build step → one JSON per scene (lines, voice files, shots, blocking) → a small runtime `Film` player that cuts
   between cameras and plays acting clips and lip-sync. Why: everything tonight is authored headless, and data means
   you can edit a line in SCRIPT.md, re-voice and re-render with no editor work. Timeline can be layered on later for
   hand-tuned hero shots.

## To-do (model · effort), in order
Batch 0 — fresh project · Opus · medium
- [ ] 0. Project at `beta1/` (URP, Cinemachine, Timeline, Recorder, Input System), `.gitignore`, remote bridge,
  Recorder render; an empty lit test scene rendered to mp4 and inspected.

Batch 1 — script and voices
- [ ] 1. `story/SCRIPT.md` · Opus · high
- [ ] 2. Voices (Piper) · Opus · medium

Batch 2 — prove realism on the diner
- [ ] 3. Diner set · Opus · high
- [ ] 4. Slick and the player (MPFB → Rigify → FBX → Humanoid) · Opus · high
- [ ] 5. Diner scene acted and rendered · Opus · high

Batch 3 — the rest of the opening
- [ ] 6. Floor 34 office set and scene · Opus · medium
- [ ] 7. Consenting cast: Natso, David, Mustich, Tommy, Gnomey, Twogloves, Bio · Opus · medium each
- [ ] 8. Card room set and Chapter 1 scenes · Opus · medium

Batch 4 — highlight reel
- [ ] 9. Reel sets · Opus · medium
- [ ] 10. Render and cut the reel · Opus · medium

Batch 5 — playable Chapter 1
- [ ] 11. Port hand library and grading, same-grade check · Opus · high
- [ ] 12. Game flow · Sonnet · medium
- [ ] 13. Table design · Opus · medium

Batch 6 — ship
- [ ] 14. Web build · Haiku · low
- [ ] 15. This report · Opus · low

## Installs and downloads
| What | From | Licence | Where |
|---|---|---|---|
| Unity packages: URP 17.6.0, Cinemachine 3.1.7, Timeline 1.8.12, Recorder 5.1.7, Input System 1.20.1, uGUI 2.6.0 | Unity registry | Unity Companion | `beta1/Packages/manifest.json` |
| MakeHuman system assets (CC0) | files.makehumancommunity.org | CC0 | MPFB user data folder |

## Per task
(filled in as each task lands)
