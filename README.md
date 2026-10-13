# Slick RNG

Multiway GTO poker solver.

## Engine (`engine/`)
- 2–9 players, any board (flop / turn / river start)
- Custom bet & raise sizes per street (% pot, x-raise, all-in), raise caps, size merging
- Unequal stacks, correct side pots
- Payoffs: chip EV or ICM (Malmuth-Harville, up to 20 remaining players)
- Monte Carlo CFR (external sampling, regret-matching+, weighted averaging), multithreaded
- Exact strategies per combo on the starting street; optional hand-strength buckets on later streets

## Usage
    slick solve spots/srp_3way_K84.json [--path 0,1] [--out result.json]

`--path` walks the tree by action index from the root (e.g. `0,1` = first player checks, second bets small).
