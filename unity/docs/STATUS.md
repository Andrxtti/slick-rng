# Hand-off 2026-10-28 (evening)
- Solver: Beta 4.7 ships tonight with a re-raise model fitted to GTO Wizard's own 3-bet-pot EVs (100bb agreement
  76.5 -> 78.2%). The Dream's hand library is being re-solved on that model on the PC worker overnight (150 jobs,
  C:\Dev\cfp-worker\out\dream-*.json); the solver session exports spots.json from it in the morning.
- Overnight game work runs in Claude Code on the PC from `unity/docs/OVERNIGHT.md` (branch dream-night).

# The Dream: status after Beta 1.69 (2026-10-24)

## Beta 1.82 (2026-10-26): the verified hand library
Every one of the 50 library tables (a table = one tournament situation: players, stacks, bubble/ITM/satellite/ring) was
solved three separate times from scratch under the fitted realization model (implied odds 0.8, offsuit penalty 2x,
multiway flop model on), and a hand is dealt only where the solves agree on its best play (tools/pf/consensus.py; a
"not sure" from any solve counts as disagreement). Solves run to an iteration target, not a clock, so a slow machine
cannot weaken a pass. Of 36,437 candidate hands, 21,915 ship in 927 spots: bubble 279 spots / 5,763 hands, shove 445 /
10,646, ring 174 / 4,694, satellite 29 / 812 (3 of 4 satellite tables; the fourth has no stable answers: the solves
land on different jam-or-fold strategies, which is satellite play being satellite play, so it ships nothing).
Versus the Beta 1.81 library, 63 of 11,249 shared hands changed their best answer (0.6%), nearly all marginal flats
facing a raise (A5s, A3s, 33, 44) that are now folds, the direction the GTO Wizard fit pointed.
Multiway spots that reach a flop are back in (41 of them), with two hand families never quizzed there, per the course:
offsuit junk flats from the big blind behind a raise and a caller (32o, J4o, K6o...: folds in MTTs), and TT+/AQ+ flats
at 15bb or less with a jam on the menu (short-stack play jams them). Saved misses reset once (they index spot positions).
Still out, unchanged: short flats from a seat that doesn't close the action, SB flats to 30bb, value jams when a small
re-raise is offered at 35bb or less, 3-bet flats at 35bb or less, blind re-jams over 25bb with a small 3-bet available.

## Live
Beta 1.69 at https://andrxtti.github.io/slick-rng/beta/app/ — Miami story, cinema scenes (coverage, DOF, establishing shots),
third-person home, Study drill, Mixamo acting in scenes, re-voiced cast.

## To test (Steven)
- Body clips on UMA characters: Talking / Breathing Idle / Victory / Defeated / Fist Pump. Look for odd poses, sliding feet.
- New voice colour: narrator, Slick, Tommy "The Shark", Bio "The Ghost".
- Load time (data file now 70 MB).

## To-do list (model · effort)
Sonnet · medium (do next, small, safe)
1. Phone prop in text beats. First check whether the 3D scene shows during text beats; skip if not.
2. Set dressing for chapters 3-7 (props and lights per set, like the diner pass). Needs screenshots to judge.
3. Sit/stand/walk clips at home, once there is sit furniture (couch, card table) with a use point.
4. Fix list from Steven's Beta 1.70 testing (opening order, diner, joystick feel).
Done 2026-10-24 (Fable/Opus extra): 5. shot list for every scene (unity/tools/direction.py -> direction.json; edit and
re-run, no code), five Slick lessons (first bust of chapters 1-5), fuller chapter 4-7 openers; 6. aftermath scenes
directed with a witness line per venue and outcome, six new stings; 7. skin (less orange scatter, matte, pore detail)
and hair sheen. Also fixed: Mixamo clips sank bodies into the floor (now feet-anchored), camera re-aims after a clip settles.
Opus · low (Fable is out for now)
8. Review of any Sonnet change that touches the opening flow or saves (Calendar, Play.Start, SceneEnd).
Haiku / script
9. Builds and publishing (a code-change build takes ~19 min on this PC when it is busy), script-edit pulls, re-voicing after script edits.
Later (after the storyline is complete)
10. Online play phase (multiplayer). No walkable city: travel stays cutscenes.
Steven's side: test Beta 1.70; edit the script doc; music tracks; Apple developer account later.

## Notes
- Mixamo clips live in Assets/Dream/Anim/Mixamo (27 files, FBX for Unity, no skin, 30 fps). "Dream > Build acting set"
  (remote command `acting-build`) rebuilds Resources/Dream/Acting.controller after adding clips.
- Shaking Hands is "Shaking Hands 2" (Mixamo has no plain "Shaking Hands").
- Leftover *.tmp files in Downloads from the Mixamo run are safe to delete.
