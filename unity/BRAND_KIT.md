# The Dream: Brand Kit

How a clothing brand (ours, a partner, or a merch drop) gets its pieces into the game. A brand is one folder you drop into the project. No code changes and no app rebuild of game logic are needed, just a new build with the folder in it.

```
unity/Assets/Dream/Resources/Brands/<brand-key>/
    brand.json          the brand, its items, how each is worn and unlocked, redeem codes
    <art>               models, textures and UMA recipes for the items (see "Art" below)
```

When the game starts it reads every `brand.json` under `Resources/Brands`. Each item appears in the Closet with its grade, the brand's name, its story and how to unlock it. Mistakes such as a missing id or an id that's already taken are written to the Unity Console as `[Dream] Brand pack: ...` warnings, and that item is skipped.

## brand.json

```json
{
  "key": "drift",
  "name": "Drift Supply",
  "about": "Card-room streetwear from Riverside.",
  "site": "https://example.com",
  "partner": true,
  "items": [
    {
      "slot": "outfit",
      "id": "drift_coach",
      "name": "Drift coach jacket",
      "grade": "rare",
      "category": "jackets",
      "story": "Made for the 2 a.m. walk to the car after a deep run.",
      "unlock": "level:8",
      "wear": { "kind": "uma", "asset": "Drift_CoachJacket_Recipe", "umaSlot": "Chest", "dyeable": true }
    },
    {
      "slot": "chain",
      "id": "drift_tag",
      "name": "Drift dog tag",
      "grade": "elite",
      "story": "Stamped with your seat number.",
      "unlock": "redeem",
      "wear": { "kind": "prefab", "asset": "Brands/drift/DogTag", "bone": "Neck", "offset": [0, -0.05, 0.08], "euler": [0, 0, 0], "scale": 1 }
    }
  ],
  "codes": {
    "<sha256 of the normalised code>": "chain:drift_tag"
  }
}
```

### Brand fields

| Field | What it is |
|---|---|
| `key` | Short id, lower case, no spaces. Also the folder name. |
| `name` | Shown on every item ("Drift Supply · Rare"). |
| `about`, `site` | Shown on the brand's page in the Closet. |
| `partner` | `true` for an outside brand. It's shown with a partner badge. |
| `items` | The pieces (below). |
| `codes` | Redeem codes, stored as hashes (below). |

### Item fields

| Field | What it is |
|---|---|
| `slot` | Where it's worn: `outfit`, `eyes`, `head`, `ear`, `chain`, `ring`, `wrist`, `watch`, `patch`, `sleeve` (also `home`, `ride`). |
| `id` | Unique in its slot. Prefix it with the brand key (`drift_coach`) so it can never clash with ours. |
| `name` | The item's name in the Closet. |
| `grade` | `standard`, `rare`, `elite`, `legendary`, `iconic`. This sets its colour and how much it adds to the Fit rating. |
| `category` | Outfits only: `shirts`, `street`, `jackets` or `formal`. This is the Closet tab it appears under. |
| `story` | One or two lines shown on the item. |
| `req` | Optional text that says how to unlock it. It's filled in from `unlock` when left out. |
| `unlock` | How it's earned (below). Items are never sold. |
| `fx` | Optional effect id for a special finish. |
| `wear` | How it looks in 3D (below). |

### Unlock rules

| Rule | Unlocks when |
|---|---|
| `""` | Owned from the start. |
| `level:N` | The player reaches career level N. |
| `played:N`, `dec:N` | N events played, or N decisions made. |
| `peak:N` | The bankroll has ever reached $N. |
| `streak:N` | A Daily Grind streak of N days. |
| `ch:N` | The player reaches story chapter N. |
| `event:ID`, `won:ID` | The player has played, or won, a given event (ids are in `dream_data.json`). |
| `title:ID`, `titles:N` | A given title, or N titles in total. |
| `redeem` | Only a redeem code unlocks it. This is the virtual twin of a real-world piece. |

### How a piece is worn (`wear`)

| `kind` | Use it for | Fields |
|---|---|---|
| `uma` | Clothes that must bend and fit every body (tops, jackets, trousers, shoes, hair, hats that sit on hair). | `asset`: the UMA Wardrobe Recipe name. `umaSlot`: Chest, Legs, Feet, Hair, Helmet, etc. `with`: extra recipes worn together. `dyeable`: whether players can recolour it. |
| `prefab` | Rigid pieces: jewellery, watches, glasses, pendants, accessories that don't bend. | `asset`: prefab path inside `Resources` (no extension). `bone`: a Unity humanoid bone name (Head, Neck, UpperChest, LeftHand, RightHand, LeftLowerArm, ...). `offset` (metres), `euler` (degrees), `scale`. `dyeable` tints `_BaseColor`. |
| `proc` | Re-use one of our built-in generated pieces in your colours. | `asset`: the built-in id (e.g. `cuban`, `aviator`, `cap`). Optional `offset`, `euler`, `scale`. |

Patches (`patch`, `sleeve`) are texture overlays on the outfit. Supply them as a UMA Overlay with a transparent PNG.

## Art

**Bodies.** Every player shares one skeleton: Unity's humanoid rig, as used by UMA. Clothes are made once and fit every face and body shape the player can make.

**Clothes (`uma`).**
- Model them on our base body (ask us for the FBX). Skin them to the same skeleton, then turn them into a UMA Slot, an Overlay and a Wardrobe Recipe with UMA's Slot Builder.
- Budget per piece: up to about 8,000 triangles, one 2048×2048 texture set (albedo, normal, metallic/smoothness).
- Leave the albedo neutral where you want players to be able to dye it (mark it `dyeable`).

**Rigid pieces (`prefab`).**
- Units are metres, +Y is up and +Z is the character's forward. Put the pivot where the piece attaches, such as the clasp of a pendant or the centre of a watch face.
- Budget: up to about 3,000 triangles and one 1024×1024 texture set.
- Use URP/Lit materials.

**Phones and the web.** The game also runs in Safari on iPhone, so keep within the budgets above. Pieces over budget still work, but they're the first thing to slow down older phones.

## Redeem codes (the virtual twin of real merch)

A code printed on a hang tag or a receipt unlocks the matching piece in the game. The game never stores the codes themselves, only their SHA-256 hashes, so nobody can read the codes out of the app.

Codes ignore case, spaces and dashes: `DRIFT-7KQ2-M9XA`, `drift 7kq2 m9xa` and `DRIFT7KQ2M9XA` are the same code.

To make codes for a drop:

```
python unity/tools/brand_code.py --brand drift --item chain:drift_tag --count 500 --out drift_tag_codes.csv
```

This writes the codes to the CSV, for the printer, and prints the `"codes"` block to paste into `brand.json`.

**Before launch:** this offline check lets any copy of a code work on any phone. For a real drop, check codes on a server so each one works once. The game calls `Wardrobe.Redeem`, so that's the one place to change.

## Checklist for a new brand

1. Make the folder `Resources/Brands/<key>/` with a `brand.json`.
2. Add the art: UMA recipes, or prefabs under `Resources/Brands/<key>/`.
3. Make codes if any items use `"unlock": "redeem"`.
4. Press Play in Unity. Check the Console for `[Dream] Brand packs: <name> (<n>)` and fix any warnings.
5. Check every piece in the Closet on a few body shapes, then build.
