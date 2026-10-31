# Multiway spots: how we build them, and where we follow the coach over the solver

Working notes for the solver and for the game's coaching section / description page. Everything here is in our own
words. The hand-by-hand chart transcription used to score the solver (`mw_charts.js`) is local reference data from a
paid course and is never committed or shipped.

## Standing decisions (Steven, Oct 2026)

- Chip EV must be GTO: the realization model is fitted to GTO Wizard's chip-EV solutions (read from Steven's own account,
  gw/, local only). ICM and multiway are where the product should be stronger, cheaper, sharper and more informative than
  GTO Wizard; the course's ICM-tightened guidance is the yardstick for our ICM-table output, not for chip-EV solves.
- Multiway spots follow the RYE Masterclass guidance, especially from the blinds, where it does not contradict chip-EV
  GTO: his charts are ICM-tightened (he says so), so a chip-EV BB defends wider than his chart by design.
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

## GTO Wizard check (Oct 25, MTT chip EV 8-max, exact percentages, 18 nodes at 100bb + 16 at 25bb)

- BTN overcalls, SB and HJ/MP: GTO Wizard and the course agree at 92-94% hand agreement; both are wider than every version
  of our model (our BTN/SB cold-calls are 4-8 points narrow: 22-44 and 54s-T9s fold).
- BB multiway: GTO Wizard chip EV is 15-30 points wider than the course charts (67 vs 44 vs UTG open + SB call, 65 vs 36 vs
  MP open + BTN call, 76 vs 66 vs BTN open + SB call). That gap is the ICM/"vs good regs" tightening his charts carry.
- Our first-cut multiway model (play 1, gam 0, pen 1, pos 1) agrees with GTO Wizard at 85.5% (the course charts: 86.3%);
  the afternoon refit toward his BB widths scores 81.8% and is withdrawn as the default. Engine default: 77.8%.
- At 25bb chip EV the BB defends 77-98% of hands against an open and a call (jams included); BTN overcalls 13-15%
  (half of it jams); SB 18-21%.
- Heads-up 100bb: GTO Wizard flats A8o/A9o from the SB vs a BTN open but folds A2o-A7o (A5o 3-bets or folds); BTN vs CO
  flats A9o+ only. Our old SB/BTN flats of A2o-A7o were wrong (heads-up model issue).

## Second refit, against GTO Wizard (Oct 25 evening)

What moved the model and what did not (all 100bb 8-max, full re-solves, scored on 19 GTO Wizard nodes):
- Multiway knobs (playability x1.5-3, flatter or steeper strength curve, position ladder, leak routing, per-player
  playability, smaller initiative bonus): 84-86% agreement whatever the setting; BTN overcalls stuck at 10-16% vs 18-21%.
- Scaling the whole playability table (bonuses 2-4x, penalties 1-3x): same. The multiway node did not move even at 4x.
- Diagnosis: equity-share settlement. 22 has ~20% equity 3-way, collects ~2.0bb for a 2.3bb call and folds; GTO Wizard's
  own EV view has 22 at +0.65bb, 44 +0.79, 54s +0.21, ATo/AJo 0 (fold). Set-mining and nut draws win big pots when they
  hit; a share-by-equity rule cannot express that.
- Fix: a zero-sum implied-odds transfer (MODEL.io, engine cfg.implied): pairs, suited connectors and suited aces gain
  io * PLAY+ * playK share points, paid by the others in proportion to their share; heads-up and multiway alike. With
  io 0.8 and the offsuit penalty x2: 86.6% agreement (the course charts score 86.3% against the same nodes), SB and BB
  within a few points, heads-up BTN vs CO / BTN vs UTG within 3-4 points. BTN overcalls still 11-15% vs 18-21%.
- Why the BTN overcall is still narrow at 100bb: the opener's response to a squeeze. After two callers and an SB squeeze,
  our UTG folds 29% of its range and 4-bets or jams 49%; GTO Wizard folds 62% and re-raises 17%. That heads-up
  over-aggression (jams of 100bb that are rarely right) makes calling behind an open expensive in our tree. It fades
  with stack depth: at 25bb our solves already agree with GTO Wizard at 88.7% with BTN/SB/HJ within 1-4 points.
  Logged as the heads-up calibration item (4-bet/jam structure at 60bb+), not a multiway problem.
- Per-hand EVs at rare multiway nodes are noisy in short solves (the BTN overcall node is reached in <1% of deals); the
  strategies are fine (every visit updates all 169 hands), the EV readout is not. One more reason for the two-solve gate.

## Refit result (Oct 25 afternoon, 100bb 8-max chip EV, 18 charts with his on-camera corrections applied; superseded)

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

## Heads-up 3-bet / 4-bet structure (Oct 26 evening, A-1)

GTO Wizard MTT chip-EV sizes read from the account (gw/SIZES.md): SB 3-bet vs BTN 3.5x at 40bb, 3.75x at 60bb, 4.5x
at 100bb; BTN 3-bet vs UTG 4.0x at 100bb; a squeeze adds 0.5x per caller; 4-bets 2.2x at 100bb, 2.0x at 40-60bb (where
the jam does most of the work: 3.7-6.2% jams, under 0.3% small 4-bets); 5-bets 2.1x or jam. Our tree now follows a
depth curve through those points above 25bb (the course's 2.75x / 3.0x anchors stay at 25bb and under).
Sizes alone left the opener and the 3-bettor continuing far too much (BTN vs SB 3-bet 62% of its range vs 45%, opener
vs squeeze 67% vs 31%): offsuit aces, JTo/QTo and suited junk flatting 3-bets in position. A global steeper strength
curve (gammaK 0.22-0.3) fixed that but folded the BB out of single-raised multiway pots (60 -> 51 vs GTO Wizard 67),
so the steepness now scales with the raise level at the flop only (MODEL.gRe = 1: x2 in 3-bet pots, x3 in 4-bet pots).
Result at 100bb 8-max: BTN vs SB 3-bet 47% (GW 45, agreement 82%), opener vs squeeze 46% (31, 73%), SB vs BTN 4-bet 67%
(58, 57%: still loose, the one open item), every single-raised node unchanged, 25bb unchanged at 89.6%.
Tested on a 3-handed BTN/SB/BB tree (hu.js), where these nodes are dense; in the 8-max tree the squeeze line is reached
in ~0.2% of deals and a 600 s solve leaves most hands there at the starting mix. The Dream library (solved Oct 26
morning, all tables 70bb and under, 3-bet pots mostly below 25bb) is not re-solved for this; the chart library is.

## Oct 28: what the remaining gap to GTO Wizard is made of

- Scoring now keeps jams apart from smaller raises wherever our tree offers both (gwlib.py JAMSEP, default on). The old
  metric counted a 100bb 4-bet jam as agreeing with GTO Wizard's 22bb 4-bet. Shipped library on the honest metric: 80.6%
  (82.1% on the old one), 41 nodes, offsuit junk excluded.
- The biggest systematic error is composition, not width. Facing an open (and in squeeze spots) GTO Wizard raises with
  suited hands (A5s-A8s, KJs-KTs, QJs, JTs, T9s, 76s, 65s) and flat-calls pairs 66-99 and offsuit broadways (AQo, AJo, KQo);
  our model does the reverse. About 6 of the 19 disagreement points are this swap; another ~2 are jams vs small raises,
  the rest are fold/call width (suited junk and weak offsuit aces called too often, offsuit broadways folded too often).
- Global knobs (playability fade by SPR, pair-only fade, extra playability in 3-bet pots, flatter strength curve in
  3-bet pots) all moved the score by less than run-to-run noise (about 1 point). The fix is per-class corrections by pot
  type, fitted the same way as the multiway correction: model_pot.json, re-raised pots and heads-up single-raised pots
  (fit_pot.py, every third node held out).
- 4-bet jams: the library solver allowed a 4-bet jam up to 100bb and jammed ~89% of its 4-bets at 100bb (GTO Wizard ~5%).
  The app engine already capped 4-bet jams at 70bb for exactly this reason (no stack-off play in the flop model); the
  library solver now uses the same cap. Settling a share of low-SPR flops as all-in (PA) only halved the jams and cost
  the 25bb tables, so it stays off.

## Oct 28 (afternoon): 100bb re-raise composition, with 13 new GTO Wizard nodes

- Reference set is now 53 nodes (35 at 100bb: every seat facing each open, the opener facing a 3-bet from BTN/BB,
  the 3-bettor facing a 4-bet, CO facing a BTN 3-bet; 16 at 25bb; one each at 40/60). gw/extract.js reads a spot
  straight off the GTO Wizard page (strategy bands + the hand's weight at the node; unreachable hands are skipped).
  Shipped 4.5 library: 77.9% over the 53, 75.6% at 100bb.
- Tree error found with probe_ev.js (EV of a hand's raise split by the next player's reply): cold players facing a
  3-bet could only fold or JAM, so at 100bb the blinds jammed AA/KK/AK and A5s 3-4% each over every 3-bet. A suited
  3-bet lost 9bb to each of those; hands that could call a jam didn't. Fixed: cold vs a 3-bet gets a sized 4-bet while
  the jam cap (70bb) is off, a jam when it's on (same as the app engine).
- Model error seen in the same probe: when the opener CALLS a 3-bet (SPR ~4.6), the in-position 3-bettor keeps a full
  27-point positional leak from the caller, so JJ collects 77% of the pot, 88 58%, AQo 60%. That makes 3-betting
  pairs and offsuit broadways far too good. Candidate fix: the positional edge should fade with SPR well above 2
  (S1 knob: ramp = (SPR-0.25)/S1), tested at S1 = 5 and 10 on the PC.

## Oct 28 (afternoon, 2) — calibration result, postflop check, real-postflop realization

- Calibration solves on the PC (8 min each, 100bb and 25bb): base (with the cold-jam tree fix) 75.4% / 81.9%,
  S1=5 75.4 / 81.0, S1=10 74.9 / 78.4, S1=5+PRE1 76.0 / 80.3, PSPAN3 76.4 / 81.3, S1=5+PSPAN3 76.4 / 81.5,
  S1=10+PSPAN3 76.3 / 78.8. The positional-fade idea does nothing; PSPAN3 is +1 point at 100bb (noise level).
  The composition error is unchanged: at every 100bb node the big misses are 54s 65s 76s 87s 77 88 99.
- Where it comes from (BTN vs SB 3-bet, 100bb): GW calls 10.4 with K8s-K4s, Q8s, J8s (100%) and folds A9o/A8o;
  we fold the suited kings (call EV -2.67 vs fold -2.3) and call the offsuit aces (-1.49). GW 4-bets AA only 59%,
  bluffs A6s/K5s/K4s/A9o/ATo; we 4-bet AQo 93%, KQo 79%, KJo 49%. In a 3-bet pot at SPR ~3.7 our model gives
  dominated offsuit hands too much and suited playable hands too little. The fitted PLAY_RE table (frequency fit,
  bounded ±0.3) did not fix it.
- GW postflop check (one flop per day on this plan): BTN opens 2.5, BB calls, Ks7d2c. GW: BTN bets 33% 75.6% /
  checks 24.4%; BB vs the bet raises 23% / calls 37% / folds 40%. Ours (sampled engine, 50/200 buckets, 7 min on
  the 2-core box): BTN 72.4 / 27.6, BB 25.0 / 38.8 / 36.2; per-hand top-action agreement 93.8% (BTN) and 82.8%
  (BB); mean |diff| 10.7 / 15.1 points. Our BB donks 12% (GW's tree has no small donk). gw/post_K72.txt, gwpost.js.
- ICM check: GW's ICM solutions (8-max, 1000 players, bubble, 152 left) need the Elite Tournament tier - locked on
  this account. Reference for ICM stays icmref.js (RYE frames) until another source is available.
- New approach for the re-raise mix: realization from REAL postflop solves. tools/pf/realize/solve_full.js solves a
  heads-up flop (GW ranges from gw100.txt, pot, stacks) with the sampled engine and records each class's mean
  utility (= pot share with the money behind) and all-in equity; 8 matchups x 12 flops on the PC (3-bet pots SB/BB
  vs BTN, BB vs UTG, CO vs BTN 3-bet; a 4-bet pot; single-raised BB vs BTN/CO/UTG). realize/fit.js fits per-class
  multipliers on the playability factor so the preflop model's share matches the real share, per raise level.

## Oct 28 (evening) — realization fitted to GTO Wizard's own EVs

- Our postflop solver vs GW on the caller side of 3-bet pots (12 flops, GW ranges): BTN vs BB 3-bet matches on
  average (mean diff +0.001 of pot) with hand noise ±0.1; UTG vs BB 3-bet is 0.04 low, and in both the sampled
  solver (50/200 buckets) undervalues suited connectors (76s 0.26 vs GW 0.40) and overvalues pairs (88 0.45 vs
  0.38). So the fit target is GW's own per-hand EVs (gw/ev100.txt, 13 nodes): for a pure call, share =
  (EV + cost) / pot; for a call/fold mix, share = cost / pot; for a fold, share < cost / pot (hinge).
  realize/gwshare.py builds the targets, realize/fitgw.js fits aggRe (last raiser's realization factor), and 16
  hand-category multipliers on the playability factor (REAL_MULT in engine.js, ramp-scaled like the positional edge).
- Level 2 (3-bet pots), 214 targets: rms 0.064 -> 0.015. aggRe 1.6 (the 3-bettor's range advantage), suited
  connectors x1.43 / x1.30 / x1.25 (one-, two-, three-gap), suited broadways x1.11, mid pairs (TT/JJ) x0.54, low
  pairs x0.90, suited aces x0.95, weak offsuit aces x0.68. Level 3 (one 4-bet node, 18 hands): aggRe 1.4 again;
  level 2's table is reused. Level 1 (BB vs opens): the existing model already fits (rms 0.037); left alone.
- First calibration (v1 table without the fold constraints, aggRe 1.43): 100bb 76.5 -> 77.4% (40 nodes, RFI nodes
  added), 25bb 81.9 -> 81.9. BB-vs-open nodes improved 3-7 points; the vs-3-bet nodes got worse because the
  caller now called suited junk GW folds (the fit had no fold constraints). v2 adds the hinge on folds -> calib3.
- Calibration rounds (100bb over 40 nodes / 25bb): base 76.5 / 81.9; v2 table (aggRe inactive: the tree's state
  clone dropped the last-raiser field - fixed) 77.7 / 81.7; v3 (fit on our own 3-bet ranges, aggRe 1.32 live)
  77.0 / 81.9; v4 (3%-of-pot fold margin) 77.1 / 81.1. The suited-junk calls vs 3-bets (K4s, Q6s, J7s) survive every
  fit: they sit within 0.3bb of indifference, and the solver's own 3-bet range shifts under them.
- Course rule 2 (course_rules.py): facing a 3-bet or bigger, suited junk (top card below an ace, gap >= 3, low card
  <= 8), offsuit junk and offsuit aces below ATo fold unless calling beats folding by 0.5bb. Chip-EV neutral by
  construction; v3 + rule: 78.2 / 81.9 (base + rule 77.1). Adopted: aggRe 1.32 is now the engine default, the v3
  tables are embedded (REAL_MULT), and the 20-table library is rebuilding on the PC (60 solves, lib-p{1,2,3}-*).
  The Dream library finished its PC passes on the previous model and will be re-solved on this one.

## Oct 28 (night) — aggRe was dead code in the solver; refit through the solver's own code; not adopted

- Found while checking before the rebuild: PF2 (solver2.js) builds its own tree and its prepTerminal rebuilds R after the
  engine's, so cfg.aggRe never reached a solve (the engine-side clone fix only touched the page's tree). Every "aggRe"
  calibration so far measured the REAL_MULT table alone. Fixed in PF2.prepTerminal (heads-up re-raised pots, the last
  raiser's R times aggRe, ramped); probe_model.js now shows each setting (AGGRE, PLAYREAL, PLAYPOT) moving the solver's
  own terminal valuation.
- realize/fitgw2.js refits through PF2.prepTerminal + PF2.termVec2 on a hand-made terminal; checked equal to a real
  m8-d100 tree terminal to 5 decimals. GW ranges, GW caller EVs (incl. fold hinges), 5 re-raised nodes, 268 targets:
  rms 0.043 -> 0.013, aggRe 1.4, suited connectors x1.2-1.35, TT/JJ x0.5. Leave-one-out: every held-out node improves
  (0.072->0.032, 0.044->0.025, 0.036->0.011, 0.038->0.012, 0.019->0.011).
- Calibration (8-min solves, two runs each, model stamped in every output and checked): base 76.4 / 75.4 (100bb),
  81.1 / 79.9 (25bb); fit 76.2 / 76.1, 79.1 / 80.3. Run-to-run noise ~1 point; the fit shows no gain at 100bb and
  slightly lower at 25bb. Not adopted: the shipped model stays (aggRe 1, no REAL_MULT table; solver2 now loads a table
  only from an explicit PLAYREAL). Better valuation of 3-bet pots did not move the strategies toward GW, so the
  remaining disagreement is not in that valuation. Course rule 2 (junk vs 3-bets, 0.5bb) +0.6 at 100bb, 0 at 25bb.
- The 4.7 library builds on the shipped model + the cold-jam tree fix + course rules 1-2, every pass run to the 4.5/4.6
  build's iteration counts (pc_build.py emits the jobs; solve_full.js MAXSECS lets the target, not the clock, end a
  solve; cloud_build.sh runs the cloud share). 40 solves on the PC, 20 on the cloud.

## Oct 28, late — 4.7 library: gate result and the one re-solve
- All 60 solves verified before merging (iters >= target, model stamp aggRe 1 / playReal none / realL2 d578ffe8 /
  playRe 30cdedeb / playHu 18ddafe5). Cloud ran six-max passes 1 (all) and 2 (d10, d15, d100); the other 7 six-max
  pass-2 jobs moved to the PC after md5-checking solver2.js, solve_full.js, engine.js and every m6 config on both.
- Gate run (consensus -> course rules -> build_gates): 19/20 tables <= 2.4% flagged; **m6-d80 5.71% (FAIL, limit 3%)**.
  GW agreement 78.9% over 57 nodes (shipped 4.6: 78.5%, same metric). Page test PASS. Candidate archived, app/lib
  restored to 4.6.
- Cause: m6-d80 pass 1 (cloud) is an outlier: 538 nodes vs 355/333, mean max-action difference 0.21 vs the other
  passes, which agree at 0.06 (last build's m6-d80 passes: 0.03-0.06, ~320 nodes). Openings agree to a point; pass 1
  diverges deep in BTN open / SB flat / BB squeeze lines with extra 4- and 5-bet branches. Not a machine effect:
  cloud pass 1 of m6-d10..d50 agrees with the PC passes as closely as PC passes agree with each other.
- Procedure fixed before the result: one fresh solve of m6-d80 (job 0lib3-p4-m6-d80, same model and target
  2,027,540) replaces pass 1 for that table only; whatever it gives is used. If m6-d80 still fails the gate, the
  table is investigated and nothing ships. The outlier stays archived (bfull_p1/m6-d80.json).
- Re-solve 0lib3-p4-m6-d80: 2,055,360 iters (target 2,027,540), model stamp OK, 370 nodes; mean max-action
  difference 0.10 / 0.09 to passes 2 / 3 (outlier was 0.23). Merged as bfull_p1b (= pass 1 with this table
  replaced). Gate: **PASS**, every table <= 2.36% flagged (m6-d80 1.85%). GW agreement 78.9% (4.6: 78.5%); by depth
  25bb 83.7 (84.0), 100bb 77.3 (76.7), 40bb 69.8 (64.8), 60bb 71.8 (73.2). Gains in 4- and 5-bet nodes (+4.5 to +7.9,
  the cold-player sized 4-bet); two 100bb nodes facing an early open lost 4-5 points (ours slightly narrower than GW,
  as in 4.6). Page test PASS (pt_docs.py updated for the 4.6 launch flow; it passes on 4.6 and 4.7).
- **Shipped as Beta 4.7** (main ca200ce, Oct 29 02:17 UTC).
