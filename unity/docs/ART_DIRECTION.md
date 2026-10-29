# The Dream — art direction and build plan (v1, 2026-10-28)

The look we are after: human, cinematic, restrained. République / Life is Strange on the character side, Sky on the
lighting side, Marvel Snap on the table. Mobile at all times. Free tools only; the budget is time and taste.

## 1. One palette: Miami at night

Every set shares one light. Warm tungsten inside, cool bay/neon outside, wet reflections, one accent colour per set.
- Night sky: deep blue-violet (#1a1f3a → #2a3350). Street/neon: magenta-pink (#ff4fa3) and aqua (#2fd3c6), used small.
- Interiors: tungsten amber (3200K) key, a cold rim from a window or a screen. Skin reads warm against a cold background.
- Felt: a desaturated green (#1f5a3a) with a worn centre; chips in cream/red/blue/black/purple with real edge spots.
- Blacks are never black: a dark blue-brown floor of shadow (#15130f). Highlights roll off (ACES), never clip.
- Per-set accent: office = fluorescent cyan on the glass; diner = red neon and pie-case light; card room = sodium yellow;
  casino = gold and purple; Vegas = white heat and signage; TV table = key-lit faces in a black room; Titan = cold
  white marble and blue glass; Main Event = a wall of lights; Hall of Legends = gold on black; home = whatever hour it is.

## 2. Camera language (already written per line in unity/tools/direction.py)

ext → wide → two → cu/ots → push → ecu. Close framing on faces does most of the work: a face that fills the screen
with a shallow depth of field looks expensive on any phone. Lenses: 35mm for wide/two, 65mm for cu/ots, 85mm for ecu.
Handheld drift on every shot (tiny, slow), a push-in on the turning line of each scene, one static wide per scene.

## 3. Characters: the standard

- Stylized-realistic: real proportions, real skin, slightly simplified features. Never photoreal (uncanny on phones),
  never cartoon. Five hero characters get everything: You, Slick, Lou (Natso SweetLou), Tommy "The Shark", Bio "The Ghost".
  Everyone else shares three body types and gets a head, a hairstyle and an outfit.
- Pipeline (free): Daz Studio + Genesis 9 (base figures, expressions, morph dials) → Daz to Unity bridge → URP lit
  shader with subsurface approximation (skin), a hair shader with anisotropic highlight, eye shader with a wet
  highlight. Lip-sync: uLipSync (open source) from the existing voice files driving the Genesis 9 mouth shapes.
  Body: Mixamo clips (already in the acting set). Idle life: breathing, blinks every 3–6 s, eye saccades, weight shifts.
- Each character has a brief (section 6) and a Daz dial sheet (tools/daz/<name>.md) written from Steven's description or
  reference image: age, build, face shape, skin, hair, eyes, three outfits (closet grades), posture and tells.

## 4. Sets

Ten sets. Surfaces, HDRIs and props from Poly Haven and ambientCG (both CC0) and free Asset Store packs (Megascans
stopped being free after 2024); custom pieces in Blender. Lighting is baked (lightmaps + light probes) with one or two real-time lights per set for the faces.
Each set is dressed once, lit once, then only cameras change.

| id | set | hero light | accent | props that sell it |
|---|---|---|---|---|
| office | Floor 34, Brickell, 6:47 PM | fluorescent panels off, monitor glow, bay sunset through glass | cyan/pink window | lanyard, the Henderson deck on screen, a cold coffee |
| diner | Lucky Seven, 2:13 AM | pie-case and counter pendants | red neon sign | pie, red envelope, stools, the bell over the door |
| cardroom | Biscayne Card Room | sodium ceiling, table lamps | yellow | 1994 carpet, list board, palms outside |
| casino | Circuit casino floor | chandeliers, slot glow | gold/purple | ring on a pillow, roped area |
| vegas | Summer, the big room | white top light, heat haze outside | white | bracelet case, a sea of tables |
| tv | Tour TV table | key lights in a black room | none | cameras, boom, the hole-card lens |
| titan | High-roller lounge | cold white, blue glass | blue | marble, a view, the trophy |
| main | Main Event | wall of lights, the stage | white/gold | the bracelet, the rail |
| hall | Hall of Legends | gold on black | gold | 27 portraits, 14 bracelets |
| home | the player's place | time of day | warm | whatever they have unlocked |

## 5. Delivery: video cutscenes, real-time home and table

Cutscenes are rendered offline in Unity (Unity Recorder, URP, 1080p, DOF, bloom, grain, ACES, 8x MSAA) to H.265
and streamed per chapter, so every phone shows the same picture and the app stays small. Home and table render live
in URP, tuned for a 2021 phone at 60 fps. The shot list (direction.json) is the render script.

Proof shot (first milestone): Slick in the diner, p_diner frames 2–13, rendered to video with lip-sync. Everything is
judged on that clip before another character is built.

## 6. Cast briefs (fill in from Steven's descriptions or reference images)

Format per character:
```
Name · role · age · build
Face: shape, notable features, skin, eyes, hair
Wardrobe: three looks (closet grades Standard / Signature / Iconic)
Movement: posture, tells, how they talk with their hands
Reference: image file or one-line inspiration
```
- **You** — the player; face sculpted in the closet (15 sliders), default look from the brief.
Briefs live in `unity/docs/characters/<name>.md` (reference images are used for type only, never likeness):
slick, natso, tommy, bio, mustich, gnomey, twogloves, david — all eight written 2026-10-28.
- **Legends** (27, dream/data.js DR_LEGENDS) — parody tributes to real pros, so every face is original: era, build, one
  signature prop and a colour, drawn from the style line. Built on the three shared bodies.
- **Extras** — dealers (one per venue uniform), rail and crowd, Floor 34 staff, table opponents: the three shared
  bodies with heads, hair and outfits from the kit.

## 7. Build order (model · effort)

1. Render pipeline: Unity Recorder in the project, a remote command `render <scene>` that plays a story scene in the
   editor and writes `C:\Dev\Builds\Cutscenes\<scene>.mp4` — Opus · high (overnight task; baseline with current characters)
2. Slick: Daz brief → Genesis 9 → Unity, shaders, lip-sync; the diner lit; proof shot — Fable · extra (with Steven in Daz)
3. Table UI design system (felt, chips, cards, type, motion) — Opus · high
4. Sets 2–10, one per batch, from the set table; screenshots for review — Opus · medium
5. Remaining cast from briefs — Opus · medium per character
6. Video streaming per chapter, app-size pass — Sonnet · medium
