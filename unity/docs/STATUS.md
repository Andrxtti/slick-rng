# The Dream: status after Beta 1.69 (2026-10-24)

## Live
Beta 1.69 at https://andrxtti.github.io/slick-rng/beta/app/ — Miami story, cinema scenes (coverage, DOF, establishing shots),
third-person home, Study drill, Mixamo acting in scenes, re-voiced cast.

## To test (Steven)
- Body clips on UMA characters: Talking / Breathing Idle / Victory / Defeated / Fist Pump. Look for odd poses, sliding feet.
- New voice colour: narrator, Slick, Tommy "The Shark", Bio "The Ghost".
- Load time (data file now 70 MB).

## Next, by model
- Sonnet: phone prop for text beats (check first that the 3D scene shows during text beats); ch3-7 set dressing;
  hook up Sit To Stand / Stand To Sit / Walking / Sitting clips to the home (sit at the card table, walk to the door);
  pick extra clips per mood (Shrugging, Pointing, Angry Gesture, Agreeing...) into Acting().
- Fable extra: look pass 2 (skin/hair shading); win/bust cutscene direction; finish the storyline (chapters 1-7 beats, shot lists).
- No walkable city. Travel is always a cutscene. Online play (multiplayer) is its own phase, started after the storyline is complete.
- Haiku/script: builds, publishing, script edits, re-voicing.

## Notes
- Mixamo clips live in Assets/Dream/Anim/Mixamo (27 files, FBX for Unity, no skin, 30 fps). "Dream > Build acting set"
  (remote command `acting-build`) rebuilds Resources/Dream/Acting.controller after adding clips.
- Shaking Hands is "Shaking Hands 2" (Mixamo has no plain "Shaking Hands").
- Leftover *.tmp files in Downloads from the Mixamo run are safe to delete.
