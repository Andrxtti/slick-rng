# The Dream — graphics track (Test Beta 1.0, Day 2 afternoon)

Steven, 12:39: "continue to actively work on our graphical capabilities and improvements".

Do this after the table moments in `TABLE.md`. Work the list in order. Before calling a task done, compare its
before and after stills in `C:\Dev\Builds\day2\`.

**From the cloud session (already on origin/unity):**
- `unity/art/table_kit/`: table plates, four-colour cards, chips, the dealer button, 9-slice sprites.
- `unity/art/grades/`: film grades per set.

They're our own art, made in code, CC0. See `unity/art/README.md` and add each file to `beta1/ASSETS.md` when you use
it.

## G1. Fix the pills everywhere · Opus · high (first: it's on the job decision)
The job decision's buttons, the table's action buttons and the Text Message field draw as stretched, blurry ellipses.
- **Cause:** a round sprite stretched as Simple.
- **Fix:** use `ui/pill.png` (borders 63/63/63/63), `rounded_24.png` and `rounded_12.png` as Sliced Images, with
  Pixels Per Unit Multiplier set so the corner radius stays fixed (about 22 px at 1080 wide).
- Let text-holding buttons grow in height with their text, with padding.
- **Check:** the job decision at 430×932 and at 1920×1080, and the table's Fold, Raise and Jam.

## G2. The table, rebuilt with the kit · Opus · high
**The plate:**
- `table_live.png` at live events and `table_online.png` at CF Tilt events (see TABLE.md §11). Use the `_portrait`
  plates in portrait.
- It replaces the drawn ellipse and its dark rim.
- Seats sit on the rail, inside the screen. SB and LJ are clipped at the edges today.

**Cards:**
- The four-colour faces (`cards/As.png`…) with `ui/card_shadow.png` under each.
- Backs: `back_live` and `back_online`.
- Your hole cards are large and slightly fanned. The board sits in a row on the felt.

**Chips and the button:**
- Bets as small stacks of side-view chips in front of each seat; denominations from the bet size, about 9 px apart.
- The pot as a stack in the middle with its number.
- The dealer button sprite.

**Seats:**
- `ui/seat_plate.png`, tinted dark glass, with the name and the stack in IBM Plex Mono.
- Live: the cast's rendered portraits, as you're already doing with `Resources/UI/*.jpg`.
- Online: a screen name and a two-letter avatar, no faces.
- Whoever is acting gets `ui/glow.png` around their plate, the same seat glow you added.

**Rules and check:**
- Keep seat order clockwise and the deal-in, bet and seat-glow motion.
- Check at 430×932 (portrait) and 1920×1080. Stills go in `day2/table_new_*.png`, next to `keep_hub.png`.

## G3. Hair that reads as hair · Opus · high
From the 12:10 review:
- The cards must cut out strands: a real alpha strand texture, alpha clip with a dither fade, darker roots, smaller
  and more cards at the hairline.
- For the films, render with MSAA 8× and alpha-to-coverage on the hair material. Films are offline, so cost doesn't
  matter.
- If an ECU still reads as blocks, go back to a shaped hair mesh with the Kajiya-Kay shader for today's films.
- Slick's fringe: reshape the shell so the cards can go up and back; don't cut it.
- Show Slick's and Mustich's ECU before and after.

## G4. Light the faces · Opus · medium
**The card room is too dark.** In `day2/check2.jpg` the faces sit in murk.
- Give every close-up and OTS a soft key on the face: a large area light or a spot with a big radius, warm, from the
  pendant's side. Add a faint cool rim from the room.
- Raise the ambient with Adaptive Probe Volumes: URP 17 has them. Bake per set; that's better indirect light than flat
  ambient.
- Aim for faces at 45–60% luminance in the grade.

**Other sets:**
- Apartment: a warm practical (a small lamp) for the faces against the blue window.
- Diner: the neon is good. Add a soft fill so the shadow side of a face isn't black.

## G5. Film grades · Sonnet · low
- Films: add `-vf lut3d=unity/art/grades/<set>.cube` to the ffmpeg step that downsamples your 1440p renders, before
  the encode. Sets: diner, office, cardroom, apartment, and title for the reel.
- If a grade is too strong, mix it with the ungraded frame (ffmpeg `blend`, or a strength of 60%).
- Live game: URP Color Lookup with the `_strip32.png` of the current set. Check the import with
  `neutral_strip32.png` first: it must change nothing.
- Previews are in `unity/art/grades/preview/`.

## G6. IllusionRP, as an experiment on a copy · Opus · high
IllusionRP (github.com/AkiKurisu/IllusionRP) adds to URP:
- skin subsurface, hair and fabric shading models;
- PCSS, contact and screen-space shadows;
- SSGI, SSR, GTAO;
- volumetric fog and auto exposure.

**Before starting:**
- **Licence:** read `LICENSE` in the repo. MIT, Apache or BSD is fine. Stop if it's GPL or has no licence.
- **Compatibility:** it targets Unity 6.3+ with URP 17.3, Forward and Forward+ only, on Windows DX12 and Android
  Vulkan.

**The experiment:**
- Our films are rendered offline on Windows, so it could serve the films only, while the web build stays on plain URP.
- Copy `beta1` to `C:\Dev\Beta1-IllusionTest`. Not in the repo, and don't touch beta1.
- Add the package with the editor on DX12. Turn on skin SSS on the faces, the hair model, PCSS, contact shadows and
  GTAO.
- Render the diner ECU of Slick and the card room two-shot. Compare them side by side with today's.

**Decide:**
- Adopt it for films only if it compiles on our URP and is clearly better. Write the decision and the stills in
  DAY_REPORT.
- Give up after an hour if it won't compile.

## G7. Eyes and teeth · Sonnet · medium
- Eyes: a cornea layer with one sharp wet highlight from the key, iris depth, and a shadowed upper lid and caruncle.
- Teeth: warmer, less separated, gums darker, ambient occlusion inside the mouth.
- Check with ECUs in the diner and office light.

## G8. Phones: post-processing on the web · Haiku · low
From the 12:10 review: the web build still logs the stripped FSR shader, so post-processing is off.
- Make the WebGL quality level use URP_Mobile, with Linear upscaling and render scale 1.
- Check the browser console after `build-web`.

After each task: commit, a DAY_REPORT entry with stills, and `build-web` when a batch is done. The cloud session
publishes.
