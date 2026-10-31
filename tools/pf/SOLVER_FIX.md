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

## P1 — heads-up postflop ground truth at 10-40bb (running)
- `node tools/pf/p1_cfgs.js` -> 240 configs in `p1/cfgs` (pot types bb_btn, bb_utg, sb_bb_limp, sb_bb_open, bb3b_btn x
  10/15/20/25/30/40bb x the 12-flop set of the 100bb realization runs; ranges from the library at that depth; index with
  pot / SPR / combos in `p1/index.json`). `bash tools/pf/p1_run.sh`: realize/solve_full.js, 60 s/flop, resumable (~4 h).
- Skipped (the line isn't in the library or the ranges are under 20 combos): 10bb except the limped pot, BvB opens at 10-20bb,
  3-bet pots below 40bb. The 3-bet pots at 15-30bb need constructed ranges in a second pass (the library barely 3-bets there:
  that's the bug), and every range comes from the current library, so P4 iterates (solve -> ranges -> P1 -> P2).
- Pilot (25bb BTN vs BB, Ks7d2c): BB (OOP) realizes 0.70 of its equity, BTN (IP) 1.13.
- Validation against Steven's GTO Wizard spot-checks: waiting for him to read the spots (he reads GTO Wizard himself).

## P2 — replace the formula (tooling done, numbers after P1)
- `p2_table.js`: realization per pot type x depth x position x class (flop-averaged, >= 400 samples, else the side's mean).
- `solver2.js` PF2.realTab, behind `REALTAB=tools/pf/p2/real_table.json` (off by default = the shipped formula): heads-up flop
  leaves in chip EV take share = R_me[c]*eq / (R_me[c]*eq + R_opp[o]*(1-eq)), pot type from level/position/aggressor, R
  interpolated by SPR between the solved depths; falls back to the formula where the table has no row (ICM, 4-bet pots, 3+ way).
- `p2_check.js`: the leaf formula against P1's measured class results (flop-averaged). Meaningful once a spot has all 12 flops.

## P3 — tree completeness 10-40bb (in progress)
- Found in the code (solver2.js opts): open-limps UTG..BTN are impossible already (only the SB completes: Steven's rule keeps
  that only where his GTO Wizard check shows SB limping, per depth: pending him). One open size per depth (2.0 under 25bb,
  2.1 at 25-34, 2.2 at 35-49). Small 3-bets exist at every depth (sized while under 40% of the stack): 25bb BB vs BTN has the
  3-bet to 6.3. Small 4-bets exist under half the stack: 40bb yes (to 15.2), 25bb no (12.6 > 12.5: jam only).
- The "missing 25bb small-3-bet subtree" is an export cut, not the tree: solve_full.js dropped every line reached under
  MINP 1e-4, and the solver 3-bets there 0.07%, so the opener's reply was never written. Fixed: `RARE_DEPTH` (default 2)
  exports two decisions past a rare action (RARE_DEPTH=0 is the old cut). Strategies that deep in rare lines converge less.
- Still to decide with evidence (P1/P4): a min-raise next to the 2.x open, a second 3-bet size, the 25bb small 4-bet.
- Limp rule: `SBLIMP` env in solver2.js ('all' default = shipped tree, 'none', or depths '10,15' each +-2.5bb) gates the SB
  complete. Open-limps were already impossible. Limp-reshove (SB complete, BB raise, SB jam) follows automatically where
  the complete exists. Set from Steven's GTO Wizard read per depth before P4.

## P2 pilot 1 (Oct 31, 8-max 40/25bb, one pass each, 900 s): formula vs table v1
- 40bb BB vs BTN 2.2x: fold/call/3-bet/jam 10/74/5/11 -> 32/43/13/11. BB vs UTG 3-bet 0.03% -> 8.6%. 4 of the 5 40bb
  regression checks pass (the small 4-bet still never appears: BTN vs BB 3-bet 48 fold / 23 call / 29 jam).
- 25bb BB vs BTN 2.1x: 0/79/0/21 -> 41/33/3/23. But 72o still calls (-0.81 vs fold -1.00).
- Red flag: SB never folds BvB (40bb 17% -> 0%, 25bb 15% -> 0%). Cause: P1's ranges came from the library, so the hands it
  folds were never measured, and the table gave them the side's mean realization (SB limp 0.94; measured trash like K2o is
  0.15-0.18). Fix: P1 v2 with every class at a 1% floor weight and 400k samples/flop so each class gets ~400 samples
  (first set kept in p1/out_v1). Also: AA hits the fit cap (R 4) because a pot-share leaf can't pay more than the pot,
  while the solve stacks off; a structural limit of the leaf, small in practice (the pAll stack-off term is gone with REALTAB).
