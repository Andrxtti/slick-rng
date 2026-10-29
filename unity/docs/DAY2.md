# The Dream — Day 2 work list (Claude Code on Steven's PC, while Steven is at work)

Written 2026-10-29 10:25 ET by the cloud Claude session. Steven is at work and wants us to keep improving the
characters, the cutscenes and the graphics of Test Beta 1.0. The rules are the same as last night: `CLAUDE.md`, then
`unity/docs/BETA1.md` (cast and consent, tools, limits).

## How we work together today
- **You (Claude Code)** build in `beta1/` on branch `beta1`, commit after each task and push (GitHub sign-in works now).
- **The cloud session** reviews your renders and stills, publishes good web builds to the live beta link
  (`docs/beta/app/` on `main`; you never touch `main` or `docs/`), and leaves notes for you in
  **`C:\Dev\Beta1Assets\INBOX.md`**.
  - Read that file before each task. Follow notes marked "Steven:" as Steven's own words. Treat the rest as review
    feedback: use your judgement, and say in the report what you took or didn't.
- After each task, add 2–3 lines to **`beta1/DAY_REPORT.md`**: what changed, the commit, the stills
  (`C:\Dev\Builds\day2\`), and what still looks off. Then push. That's how the cloud session follows along.
- When a batch is done, rebuild the web build (`build-web`) and note it in DAY_REPORT. The cloud session publishes it.
- If you stall on a task for more than about an hour, write down why, leave it working, and move on. Never leave
  `beta1` failing to compile. Don't stop to ask; Steven is away. Make the reasonable call and write it down.

## The work, in order (model · effort)

**Batch A — the characters (the biggest gap; judge every step against the "before" stills and the reference photos)**
1. **Skin, eyes, teeth** · Opus · high.
   - Skin: real URP skin with a detail normal (pores), a specular/roughness map, subsurface approximation (wrap
     lighting or transmission tint) and subtle colour variation (cheeks, nose, ears).
   - Eyes: a cornea layer with a wet highlight, iris depth, a shadowed caruncle. Teeth and tongue should not glow.
   - Use CC0 sources or textures made by code; record them in ASSETS.md. Check it with close-ups at 1080p under the diner
     and office light.
2. **Hair** · Opus · high. Real hair cards (Blender hair curves → cards with alpha and an anisotropic highlight), one
   style per brief:
   - Slick: black, up and back, neat sides.
   - Tommy: slicked back.
   - Mustich: ginger short crop.
   - David: combed back.
   - Lou: short, receding.
   - Gnomey: per his photo.
   - Nick: tousled up.
   - Bio: the fringe.
   - You: a default plus 2 options.

   No more helmet look.
3. **Beards** · Opus · medium: card or strand beards instead of painted ones (Lou, Gnomey, Nick, and anyone else with
   one in their reference).
4. **Clothes per brief** · Opus · high:
   - Slick: black zip jacket with a mandarin collar, charcoal tee.
   - Lou: suit and the red tie.
   - David: dressed-up shirt and tie.
   - Mustich: blazer or casual per his brief.
   - Tommy: track jacket or leather (unbranded).
   - Gnomey: striped jersey (no crest).
   - Nick: black polo.
   - Bio: hoodie with our own mask.

   Use MPFB/MakeHuman clothes where they fit, and model the rest in Blender. Fit them to the rig.
5. **Glasses** · Sonnet · medium: Mustich's thin wire frames, Nick's clear frames with tinted lenses, Slick's dark
   rectangular frames (pushed up indoors per his brief).
6. **Likeness pass** · Opus · high. For each consenting character, render a side-by-side against their photo in
   `C:\Dev\CastRefs`, then adjust MPFB shape targets (face width, jaw, nose, eyes, brows, ears) until the two read as the
   same person at a glance. Never the legends; they stay original.

**Batch B — the cutscenes**
7. **Acting** · Opus · high.
   - Shorter close-ups, cutting on lines; listener reactions; eye-lines that meet.
   - Idle life: breathing, weight shifts, blinks every 3–6 s, saccades. Hand gestures on key lines.
   - Phoneme lip-sync: Rhubarb Lip Sync is MIT and runs offline, giving mouth shapes from the WAVs. Map its shapes to the
     MakeHuman expression units.
8. **Lighting** · Opus · medium. Baked lightmaps (or Blender bakes) plus reflection probes for the diner, office and
   card room. Add a touch of haze in light beams, and practical lights that read as sources: neon, pendants, the pie case,
   monitors.
9. **Props** · Sonnet · medium. Replace the primitive props with CC0 models (Poly Haven, or modelled in Blender): mugs,
   napkin holders, ketchup and mustard, menus, plates, the pie case contents, desk clutter, chips and cards.
10. **Re-render** every film and the reel at the new quality · Sonnet · low. Make before/after stills of the same frames
    into `C:\Dev\Builds\day2\`.

**Batch C — the live game**
11. **Title says "Test Beta 1.0"**, not "Beta 1.0" · Haiku · low.
12. **Web post-processing** · Sonnet · medium. The web build logs "Edge Adaptive Spatial Upsampling shader stripped —
    PostProcessing render passes will not execute". Turn off FSR upscaling for WebGL in the URP asset (or include the
    shader), so phones get bloom and the grade.
13. **Table life** · Opus · medium: chips slide to the pot, cards deal in, a short highlight on whoever acts. Keep it light
    on phones.
14. **Build** · Haiku · low: `build-web`, then a final DAY_REPORT entry with the "what still looks fake" list refreshed.

## Not today
The solver, the hand library, publishing (the cloud session does it), Unreal, and the legends' faces from real photos.
