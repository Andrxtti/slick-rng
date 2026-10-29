# The Dream — Day 2 work list (Claude Code on Steven's PC, while Steven is at work)

Written 2026-10-29 10:25 ET by the cloud Claude session. Steven is at work and wants us to keep improving the
characters, the cutscenes and the graphics of Test Beta 1.0. The rules are the same as last night: `CLAUDE.md`, then
`unity/docs/BETA1.md` (cast and consent, tools, limits).

## Steven's priorities for the afternoon (10:22, his words first)
"I want more advanced script writing for the entire story, the best graphical settings and characters we can achieve.
The story Chapter 1 at least needs to follow our original script flow that leads into the Slick text in your apartment
to make your job decision etc. I want better fonts used for the text and UI, let's take the afternoon to refine and
make AAA quality."

Do these first, in this order. The batches further down are the detail for 3–5.

1. **Chapter 1 follows the original flow exactly** · Opus · high. The source is the old Unity game's
   `unity/Assets/Dream/Scripts/Game/Story.cs` (`ApplyOverrides`, which supersedes `dream/script.md` where they differ):
   - `p_office` (Monday 6:47 PM, Floor 34).
   - `p_diner` (2:13 AM, the envelope).
   - `p_seat` (Saturday 11:40 AM, Biscayne Card Room: Miles takes the envelope, "Table nine, seat four").
   - The Biscayne Championship (the free-seat tournament, played).
   - `p_after` (Monday at the office, no choice any more: "Seven more hours of spreadsheets...").
   - **`p_offer`: Monday night in your apartment.** The phone buzzes on the counter, and Slick texts his offer: quit Lou
     and he backs you, or keep the job and play on the side. "Decision time."
     - Choices: "Quit. Text Lou tonight." / "Keep the job. Grind nights and weekends."
   - `p_quit` (at home: you text Lou, Slick confirms the money, rent due on the first, David's "BRO. YOU QUIT YOUR JOB??")
     or `p_keep` (at home: the BANKROLL spreadsheet, the Friday paycheck, Slick's "Smart.").
   - `c1` at the card room, then Chapter 1's events and scenes:
     - Events: the Winn Nightly $110, the CF $55 Global Rush online, the Biscayne Classic Day 1 and Day 2.
     - Scenes: `classic_day1`, the three Classic endings, `ft_*`, `trentbeat`, `lesson1`, `firstwin`, `badbeat`, `broke`,
       and David's texts (`david1`...).

   This needs:
   - **The apartment** as a new set (a small Miami studio at night, the counter, the phone, city light through the
     blinds).
   - **A beautiful phone-text presentation**: Messages-style bubbles, typing dots, timestamps, the sender's name. It must
     look premium, not like a debug panel. The job decision is asked there.
   - Yarn Spinner runs the logic.
2. **The whole story, written properly** · Opus · high. Write `beta1/story/STORY.md`:
   - Every chapter from the Prologue to Legacy, as a prestige-drama script: scene list, beats, real dialogue with
     subtext, each character's voice, recurring threads (Lou, David, Mustich, Tommy, Gnomey, Nick, Bio, Slick's lessons
     and his past), and the turns.
   - Keep the bible (BETA1.md §3), the names, the chapter goals and the 2:13 AM diner.
   - Chapter 1 gets full shootable dialogue now. The later chapters get full scene-by-scene scripts that are ready to
     voice later.
   - The old `dream/script.md` and `Story.cs` are the floor, not the ceiling.
   - **Steven (10:23): the legends are near-bosses through the chapters, and every one of the 27 appears somewhere in the
     story.**
     - Give each legend an entrance scene, a signature line, a table moment and a beat with the player, matched to the era
       and the chapter: old school early as myths and cameos, the boom era on the Circuit and in the Summer, online and
       high-roller legends on the Tour and at the Titan, the rising stars at the Main Event.
     - Make a table in STORY.md mapping each legend to chapter, event and scene, so it's visible that all 27 are covered.
   - **The pro tournaments are our own versions**, using our names, never the real trademarks:
     - The WCS Summer Series is our WSOP.
     - The Titan high rollers are our Triton.
     - The Royal Tour and the GLO Cup are the TV tour.
     - The Main Event is the world championship.

     The players at those tables are the story's own people (legends, cast, recurring rivals), with names and lines, not
     anonymous seats. Put each pro event's field of named characters in STORY.md, and in the game data where events list
     opponents.
3. **Fonts and UI type** · Opus · medium. Use the brand fonts from the old project:
   - **Gloock** (display), **Inter** (body), **IBM Plex Mono** (numbers).
   - They're all OFL; copy the fonts and their licence files from `unity/Assets/Dream/Resources/Dream/Fonts` and
     `Fonts-Licenses`.
   - Set them up as crisp TextMeshPro/SDF assets with a real type scale (sizes, weights, tracking, line height) across
     the title, film captions, phone texts, hub, table, grades and end card. No default Arial anywhere.
4. **Best graphics we can achieve** · Opus · high. Two quality levels:
   - **Films** (offline, no limits): render at 1440p or 4K and downsample to 1080p (supersampling). Use high-res shadows,
     SSAO, contact shadows if available, bokeh depth of field, real lightmaps, 2K–4K textures on faces in close-ups, and
     restrained grain.
   - **Phones** (live): the FSR/post fix, sensible shadows, compressed textures, still 60 fps on a 2021 iPhone.
5. **Characters, then cutscenes**: Batches A and B below.

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
