# Slick RNG

Multiway GTO poker solver.

## Engine (`engine/`)
- 2–9 players, any board (flop / turn / river start)
- Custom bet & raise sizes per street (% pot, x-raise, all-in), raise caps, size merging
- Unequal stacks, correct side pots
- Payoffs: chip EV or ICM (Malmuth-Harville, up to 20 remaining players)
- Monte Carlo CFR (external sampling, regret-matching+, weighted averaging), multithreaded
- Exact strategies per combo on the starting street; optional hand-strength buckets on later streets

## PLO (4-card pot-limit Omaha)
- Preflop solver in the app (cash and MTT, ICM): pot-limit tree, Omaha showdowns (2 hole + 3 board)
- 270,725 hands in 16,432 suit-isomorphic classes, grouped into 256 strategy buckets by strength and play style
- Tables are built offline: `node tools/plo_tables.js` writes `app/plo_tables.json`; the engine embeds them

## Usage
    slick solve spots/srp_3way_K84.json [--path 0,1] [--out result.json]

`--path` walks the tree by action index from the root (e.g. `0,1` = first player checks, second bets small).
