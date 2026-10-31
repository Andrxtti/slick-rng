# solver-fix: progress log (SOLVER_PLAN.md, Steven 30 Oct)
Branch `solver-fix` (worktree C:\Dev\slick-solver), from main e03bbe7. Shipped library tagged `lib-shipped-2026-10-30`.
Nothing here goes into docs/ until Steven approves the report.

## P0 — baseline + test harness (done)
- `node tools/pf/p0_grid.js [libdir] [tag]`: format x depth x node type (open, vs open, vs limp, BvB open/vs open/vs limp,
  squeeze, vs jam, vs 3-bet, vs 3-bet jam, cold vs 3-bet, vs 4-bet, cold vs 4-bet), hero's mix (fold/call/raise/jam) weighted
  by reach x combos x node probability, and local regret (best action EV minus the mixed EV, bb per hand).
  Local regret is a convergence proxy, NOT full-tree exploitability (there is no best-response tool for the 8-player tree).
- Baseline of the shipped library: `tools/pf/p0/grid_shipped.{json,md}` (208 squares), `spots_shipped.json` (the audit spots
  with 16 key hands' mix and EVs). It reproduces the audit: 40bb BB vs BTN fold 8.4 / call 74.4 / 3-bet 5.1 / jam 12.0.
- `node tools/pf/p0_compare.js <before> <after>`: grid deltas + 11 directional regression checks from the audit (exit 1 on
  a FAIL). Shipped vs itself: 1 of 11 pass (only "100bb does not regress"), as it should.
- Also seen: at 40bb the SB completes (limps) 50% when folded to; by Steven's 19:59 rule SB limps stay only where his GTO
  Wizard check shows them at that depth.
- Blockers noted for P1/S1: the reference nodes (`tools/pf/gw/*.txt`) are not on this PC (gitignored); `gwlib.py` and
  `fit_mw.py` read `cls.json` from a Linux /tmp path. The plan's answer key is our own exact solves, so P1 doesn't need them.
