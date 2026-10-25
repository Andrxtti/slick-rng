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

## Still open

- Whether multiway spots return to the library at all depends on the refit landing within the charts' widths on every
  group, then passing the HU benches and sanity checks unchanged.
