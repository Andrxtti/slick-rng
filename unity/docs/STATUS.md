# The Dream: status after Beta 1.69 (2026-10-24)

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
