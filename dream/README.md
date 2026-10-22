# The Dream (beta build)

A story-career version of the Confirmed Fire Poker trainer. This build replaces the Punch-Out Arena
in the trainer slot so the test group can compare the two as separate apps.

**Story:** a post-grad stuck in a corporate job (Floor 34, Derek the boss) meets a stranger at the Lucky Seven
Diner at 2:13 AM. The stranger (Slick) gives him a free seat into the Riverside Local Championship. From there:
Card room grind → Circuit rings → Summer bracelets → Tour titles → Titan high rollers → the Main Event against
The Ghost (our own GOAT) → Legacy (beat every legend, pass 14 bracelets).

**Gameplay:** every decision is a real solver spot (same engine and grading as the Arena). Textbook/solid plays
build chips, mistakes cost a lot; 3–4 mistakes bust you. Big fields have more levels (3–5 stages), and coolers
can still knock you out between levels. Surviving to the end, your accuracy (plus a little luck) decides 1st/2nd/3rd.
Payouts use the app's typical payout structure. Cash sessions pay by the stack you finish with. Satellites win seats.

**Bankroll** is the score and the gate (risk labels: safe / taking a shot / betting the dream). Going broke brings
Slick back with a stake; half your profits repay him. **Unlockables** (outfits, eyewear, headwear, watches, chains,
homes, rides, sponsor patches) are milestone-based only — nothing is bought, no currency.

**Legends** are parody tributes (32 across five eras). Beat one heads-up for a title and their card turns gold.

## Files
- `data.js` — places, people, legends, chapters, titles, events, unlockables, coach tips (tune pacing here)
- `story.js` — every scene and milestone beat
- `avatar.js` — the drawn player/characters; `props.js` — scene backdrops
- `engine.js` — state, events, scoring (`DR_DELTA`), coolers, placement (`drPlace`), payouts, unlocks
- `ui.js` — screens; `dream.css` — styles
- `build-dream.py` — builds `app/slick-rng.html` from `base.html` (the trainer-work app) + these files
- `build-beta-pages.py` — writes `docs/beta/dream/` and `docs/beta/arena/` for GitHub Pages testing

## Tuning knobs
- Chapter goals: `DR_CHAPTERS` in data.js
- Chip swing per decision: `DR_DELTA` in engine.js
- Win threshold: `winT` in `drPlace` (rises with field size and chapter)
- Cooler odds: `drCoolerRoll`

## Overhaul (Oct 22)

### Hands: the pre-solved library (`spots.js`)
- Tables are solved offline for ~4 minutes each (headless Chromium, `gen.py` in the session scratchpad), then harvested:
  first decisions plus "second decisions" (you opened, now face a 3-bet or jam), with every hand class's strategy and action EVs.
- `pack.py` keeps real situations (frequent lines, clean re-raise spots), drops autopilot hands (trash nobody plays,
  premiums with nobody raising) and orders each situation's hands hardest-first by EV gap.
- `drLibDeal` deals from it: difficulty (`drDifficulty`: chapter, stage, boss, rating) picks the window, half the hands
  are right-to-fold and half right-to-play, so folding everything cannot win. Phones never solve (`window.__pfHold`).
- Live solving remains only as a fallback for stages with no library tables.

### Grading (`drLabelFor`)
Chess.com-style labels from EV loss (bb, or % of prize pool with ICM): Brilliant, Great, Best, Excellent, Good,
Inaccuracy, Mistake, Blunder. Mixed strategies are respected (a play the solver makes 25%+ is never worse than Good).
Event accuracy is the average decision score; the results screen has a full game review.

### Progression (`progression.js`)
- Career level from XP in every mode (decisions, finishes, quests, achievements); a level road of table cosmetics
  (card backs, felts, avatar frames, titles). Clothes stay Storyline-only; seasonal gear stays League-only;
  ranked tiers unlock ranked cosmetics.
- 3 daily quests (same for everyone that day), 18 tiered achievements, 6 tracked skills with stars and drills.
- Leagues: Duolingo-style weekly 30-player divisions by chip colour with promotion and demotion.
- Daily Challenge: 9 hands seeded by the date, one attempt, shareable emoji grid.
- Backup codes to move a career between devices.
