# Art made in code for The Dream

Made by the cloud Claude session on 2026-10-29 for Confirmed Fire, with no third-party art: our own work, dedicated
to the public domain (CC0 1.0). Text is drawn with the game's OFL fonts (Gloock, Inter; the suit symbols are from
DejaVu Sans, whose licence is free). Add each file to `beta1/ASSETS.md` when it goes into the game.

- `table_kit/`, made by `unity/tools/art/table.py` and `kit.py`:
  - Table plates: `table_live.png` and `table_online.png` (2400×1200) and their `_portrait` versions (1200×2000).
    Each is a top-down racetrack table (felt, walnut racetrack, stitched leather rail, a pendant-light pool) in RGBA
    with a soft shadow. Live is Biscayne green. Online is CF Tilt teal.
  - `cards/`: 52 four-colour faces (360×504, "As", "Td", "7h"…) and two backs, `back_live` and `back_online`.
  - `chips/`: six denominations seen from the top and from the side (stack them about 9 px apart), plus the dealer
    button.
  - `ui/`: white 9-slice sprites for the game to tint. `kit.json` gives each one's slice borders. Set the Image type
    to Sliced; that is what stops pills turning into stretched ellipses.
- `grades/`, made by `luts.py`: one film grade per set (`diner_night`, `office_dusk`, `cardroom_sodium`,
  `apartment_night`, `title_miami`).
  - The `.cube` file is for ffmpeg on the film renders: `-vf lut3d=diner_night.cube`.
  - The `_strip32.png` is a URP Color Lookup strip for the live game. Check `neutral_strip32.png` first: applying it
    must change nothing.
  - `preview/` has before/after frames.
