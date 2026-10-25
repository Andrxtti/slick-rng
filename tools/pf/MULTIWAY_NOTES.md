# Multiway spots: how we build them, and where we follow the coach over the solver

Working notes for the solver and for the game's coaching section / description page. Everything here is in our own
words. The hand-by-hand chart transcription used to score the solver (`mw_charts.js`) is local reference data from a
paid course and is never committed or shipped.

## Standing decisions (Steven, Oct 2026)

- Multiway spots follow the RYE Masterclass guidance, especially from the blinds. Where our solver and the coach
  disagree, agreement with the coach wins. GTO Wizard is the other yardstick.
- Offsuit junk calls in the BB in a multiway pot (53o, 64o, Q5o, J7o and the like) are folds in MTTs. No model
  setting that keeps them as calls ships.
- Where the solver and the coach don't completely agree on a hand, the game shows the other action as a possible
  choice at a lower percentage. The percentages are always our solver's own mix (the EV-gap gate already treats any
  option within the gap as acceptable); we never blend chart cells into the game data.
- Multiway and ICM spots that are not fully GTO get no special label in the solver. A description page explains how
  these spots are built, and the differences below become explanations in the coaching section.
- Nothing visible may be wrong. A spot we can't stand behind is held back (export_spots.py) rather than shipped.

## Why multiway is different (the coach's reasoning, our words)

- Pot odds look great in the BB closing the action 3-way (about 17% to call), but raw equity isn't realized: out of
  position against two ranges, weak offsuit hands fold the flop too often and get bet off the pot. Chip-EV sims defend
  far wider than tournament play should.
- His 100bb charts are ICM-tightened. He says in Part 1 that chip EV squeezes KQs/AJs/ATs and defends the BB much
  wider; ICM "starts early" and makes ranges tighter but more aggressive. Our 100bb comparison is chip EV, so we fit the
  realization model to his widths on purpose (Steven: follow his guidance), accepting that this bakes some tournament
  tightness into the multiway model.
- Hands that go UP multiway: small pairs (sets), suited connectors and suited aces (flushes, straights, nut draws),
  low offsuit connectors like 65o/54o/76o in the BB (they make straights that don't collide with the openers'
  broadways). Hands that go DOWN: offsuit broadways and offsuit aces (dominated, no nut potential, many ways to lose a
  big pot). Card removal: speculative calls should be low cards that don't interact with the openers' ranges.
- Squeezing: value with AA–JJ, AK, AQs (TT/99/88 as the opener moves later or when the SB is also in); bluffs are
  suited aces (A5s above all), suited broadways with blockers (KTs, QTs, JTs) and suited connectors. Offsuit bluffs are
  AQo vs EP and small offsuit aces in the BB. Only KK/AA go broke after squeezing at 100bb. Squeeze sizes about 11bb
  from the field, about 13bb from the blinds, about 7bb non-all-in at 20bb.
- BTN overcalls are tighter than chip EV because the blinds should squeeze, which costs the BTN its 2bb; behind a tight
  EP opener you are protected and can call more speculative hands.

## Specific guidance he gives on camera (goes into the coaching text)

- BB vs CO open + BTN flat: T9o/98o/97o are problem hands; 97o is mostly a fold and 98o folds against good players;
  65o/54o/76o play better. 88 is call-or-squeeze. AQo squeezes and folds to a 4-bet.
- BB vs MP open + late-position flat (no SB): KTo, QTo and JTo are reasonable folds; T9o and 98o fold.
- BB vs MP open + SB flat: value-squeeze AQ+ and 99+, 88 either way; calls widen a little (T8o, 97o) because the SB in
  the pot means more checks and a chance to buy position.
- BB and SB vs EP opens: few or no bluff squeezes; AQo is a fold in the ICM sims. With the SB in vs EP: call TT, 99,
  AQo; squeeze JJ+ and AK.
- SB vs EP open + EP flat: KTs and QJs are pure folds in the sims; JTs is better folded too.
- BTN vs EP open + MP flat: AQo, AQs and TT flat (even JJ can flat). BTN vs HJ open + CO flat: squeeze very wide (the
  CO is often dead money).
- His own chart corrections: fewer bluff squeezes than shown in BB vs EP open + SB flat; add more suited aces to BTN vs
  HJ open + CO flat. (40bb: KQs should be call or jam, not squeeze.)
- Short stacks: at 20bb squeeze to ~7bb or jam; SB vs open + call mostly jams; BB vs EP open + SB flat at 20bb always
  jams AJ+, KQs, 77+; MP overcall at 20bb vs EP folds 99.

## What the solver comparison found (100bb, 8-max, chip EV, 18 charts)

- Engine default (random-board showdown, leak split evenly): BB defends 90–100% everywhere against 35–66% on the
  charts. 71% of hands agree.
- Generalized flop model (MW=1 in solver2.js: Bradley-Terry multiway equity, position, playability): 84% agree, BB
  within ~5 points of the charts when the flat came from CO/BTN or EP/MP, still too wide when the SB is the flatter
  (73/44, 82/53, 97/66). Squeeze shares already match the charts within a point in both models; the problem is only
  the calling ranges.
- Missing on the BTN and SB: 22–44 and 54s–T9s, exactly the hands he says go up multiway.
- Best-response probes: more playability widens BTN/SB but blows up HJ/MP; a steeper strength curve fixes the SB-flat
  BB spots but under-defends the other BB spots; a bigger out-of-position leak makes everything worse because the leak
  flows to whoever is in position. The refit therefore targets the SB's own flat range and the BB's offsuit junk
  directly rather than one global knob.

## Refit result (Oct 25, 100bb 8-max chip EV, 18 charts with his on-camera corrections applied)

Settings scanned by full re-solves (450 s each, two per round), scored by hand-weighted agreement with the charts:

| model | agree | mean width error | BTN | LP | SB | BB |
|---|---|---|---|---|---|---|
| engine default (random-board showdown) | 71% | 21 pts | 16/17 | 7/8 | 19/15 | 99/47 |
| generalized flop model, first cut | 84% | 8 pts | 14/17 | 7/8 | 13/15 | 62/47 |
| **chosen: play 1.5, gamma +0.3 per extra player, offsuit penalty x2, position ladder 2** | **87%** | **4.6 pts** | 12/17 | 8/8 | 13/15 | 41/47 |

(width = % of hands continuing, ours/chart, mean per group.) Squeeze shares match the charts within a point under every
model. What the chosen model gets right: every BB spot within 10 points of the chart and the "SB flatted" spots within 5;
HJ/MP/EP+2 within 2; SB cold-calls within 4. What it still misses, all on the conservative side (we fold where he calls):
BTN overcalls run 4-7 points narrow (22-44 and 54s-T9s fold), BB vs EP open + EP/MP flat is 20 vs 34, BB vs CO open +
BTN flat 45 vs 55. Nothing is looser than his chart except the BB vs BTN open + SB flat squeeze share (15 vs 12).

Knobs tried and rejected: a bigger out-of-position leak (MWR 1.3) makes everything worse; routing the leak to the players
acting after you (MWLEAK) is the more principled rule but scored slightly lower, so it stays off; playability x2 across the
board blows up early-position overcalls.

Checks: heads-up benches at 20 and 30bb with the model off vs on differ only on near-ties and low-sample nodes (within the
run-to-run noise of the bench itself); the model only touches flops with three or more players in. The library gate
(solve_lib.js) now adds two multiway-robustness passes (harsher and kinder multiway model) so a multiway hand is dealt
only when its answer survives both.

Known heads-up issue found on the way (not multiway): at 100bb our SB and BTN flat weak offsuit aces (A2o-A7o) and weak
offsuit kings against late opens. Those are 3-bet-or-fold hands. It is the heads-up realization model (offsuit aces are
under-penalized), it feeds the BB's multiway defense, and it belongs to the heads-up calibration item.

## Library re-solve under the new model (Oct 25, all 50 tables, full gate)

- sanity.js: 0 problems on 23,903 dealt hands. Shipped library untouched (out_mw/ is local, ignored).
- Reproducibility (libdiff.py, spots matched by seat + path, hands by class): the same table solved twice with the same
  settings reproduces 100% of the dealt answers they share (0 of 200 at 240 s, 0 of 216 at 480 s). 480 s vs 240 s differs
  on 2 of 206, both near-ties facing a jam (A7s, ATo; EV margins under 0.1). A first comparison that matched by list
  position reported 43% "flips"; that was the comparison's bug, not the solver's.
- New model vs the shipped library: 60 of 10,627 dealt hands shared by both change their best action (0.6%), all with a
  gate-size margin; mostly fold -> call in the BB and the blinds vs a raise. 2 of the 60 are in held-back multiway spots.
- Next gate step (credibility): ship only hands where two independent solves agree on the best action. That removes the
  near-tie flips above at the cost of a second 3-hour run per library build.

## Still open

- The shipped library is untouched. Turning the model on means re-solving all 56 tables through the gate (about 3 hours
  on this machine), then reviewing the 22 held-back multiway spots one by one (mwreview.js) against his 20-40bb
  guidance before any of them is dealt again.
