// Clean preflop tree for The Dream / Slick RNG (GTO-Wizard-style menu), on top of the app's PreflopSolver core.
//  first in (UTG..BTN): fold / raise / jam (jam only when it is a real play at this depth)
//  SB folded to:        fold / complete / raise / jam          BB vs SB complete: check / raise / jam
//  vs an open:          fold / call / 3-bet / jam              (at most two cold callers)
//  vs a 3-bet:          players already in: fold / call / 4-bet / jam;  cold players: fold / jam
//  vs a 4-bet:          players already in: fold / call / jam;  cold players: fold
//  vs an all-in:        fold / call
// No open-limps outside the SB, no "raise to all-but-1bb". Sizes scale with depth and position.
const E = require('../../app/engine.js');
// calibration: PLAYSET='{"sGap1":0.15}' overrides the playability family constants (see PLAY_DEFAULT in engine.js)
if (process.env.PLAYSET) E.PLAY169.set(E.buildPlay({ ...E.PLAY_DEFAULT, ...JSON.parse(process.env.PLAYSET) }));

const OPEN = (eff) => (eff >= 50 ? 2.3 : eff >= 35 ? 2.2 : eff >= 25 ? 2.1 : 2.0);
const SB_OPEN = (eff) => (eff >= 75 ? 4.0 : eff >= 28 ? 3.5 : eff >= 18 ? 3.0 : 2.5);   // solved BvB sizes (8-max ChipEV, 12.5% ante)
const ISO = (eff) => (eff >= 30 ? 3.5 : 3.0);           // BB raise over an SB complete
const JAM_CAP = { 1: 25, 2: 50, 3: 100 };
// 3-bet size as a multiple of the open, by effective stack: the course's short-stack anchors (2.75x in position / 3.0x
// out of position at 25bb and under), then a line through GTO Wizard's 40 / 60 / 100bb sizes
const oop3 = (e) => (e <= 25 ? 3.0 : e <= 40 ? 3.0 + ((e - 25) / 15) * 0.5 : e <= 60 ? 3.5 + ((e - 40) / 20) * 0.25 : e <= 100 ? 3.75 + ((e - 60) / 40) * 0.75 : 4.5);
const ip3 = (e) => oop3(e) - (e <= 25 ? 0.25 : e >= 50 ? 0.5 : 0.25 + ((e - 25) / 25) * 0.25);
const JAM_CAP_NONBLIND3 = +(process.env.JAMCAP3 || 25);   // non-blind 3-bettors jam only at 25bb or less (small 3-bets above that)                // deepest effective stack where a jam is on the menu, by raise level

// postflop realization. Aggregate EQR fitted to published solver reports (IP ~1.15, OOP ~0.8 in single-raised pots);
// the hand-strength curve (gammaK) and the low-SPR fade (s0/s1) fitted to Pairrd frames from the RYE Masterclass
// (30bb 8-max: UTG RFI 17.4%, HJ vs UTG call 9.3 / 3-bet 5.2, UTG vs 3-bet fold 6.8 / call 7.4 / jam 3.8; 40bb BB vs UTG
// defends ~67%). See tools/pf/out/ref30_F.txt, ref40_F.txt.
const env = (k, d) => (process.env[k] !== undefined ? +process.env[k] : d);
// kInit: an out-of-position preflop aggressor (a blind 3-bettor, a squeezer) gets back this share of the positional penalty
// io 0.8 / pNeg 2: fitted to GTO Wizard's chip-EV solutions on Oct 25 (86.6% hand agreement at 100bb, 89.7% at 25bb over
// the multiway and heads-up nodes in gwcmp.js); see MULTIWAY_NOTES.md
// gRe 1: the strength curve is twice as steep on a 3-bet-pot flop, three times in a 4-bet pot (Oct 26, with GTO Wizard's
// 3-bet / squeeze / 4-bet sizes): BTN vs SB 3-bet continues 47% (GTO Wizard 45) instead of 62%, opener vs squeeze 46%
// (31) instead of 67%, single-raised pots and 25bb unchanged (89.6%)
const MODEL = { io: env('IO', 0.8), pPos: env('PPOS', 1), pNeg: env('PNEG', 2), rLow: env('RLOW', 0.73), gammaK: env('GAMMAK', 0.15), s0: env('S0', 0.25), s1: env('S1', 1.75), kInit: env('KINIT', 0.5), kLimp: env('KLIMP', 0), gRamp: env('GRAMP', 0), gRe: env('GRE', 1), pA: env('PA', 0), pAk: env('PAK', 1), pM: env('PM', 1), pMw: env('PMW', 0.6) };
// Multiway flops (three or more players see the flop). The engine settles these with a random-board showdown and
// splits the realization leak evenly between everyone, so a hand with no equity still collects a share of the pot and
// suitedness/position-by-hand play no part. MW=1 uses the heads-up model generalized instead: each player's multiway
// equity is estimated from the exact heads-up class equities against the actual opponents (Bradley-Terry: exact when
// two players), then share = R * playability * equity^gamma, normalized. Playability and the strength curve can be
// made stronger multiway (MWPLAY, MWGAM) and the out-of-position leak larger (MWR), as the course teaches.
// pen: extra weight on the playability PENALTIES (offsuit aces, offsuit junk) with three or more in; leak: 0 shares each
// player's positional leak among everyone (plain normalization), 1 gives it to the players who act after him, in
// proportion to their strength (position means the players behind you get to exploit your checks).
// pos: shape of the position ladder with three or more in. 1 = evenly spaced (the heads-up rule extended); higher
// values pull the middle seats toward the first to act, since only the last to act really has position in a multiway pot.
// Defaults (Oct 25 evening): the plain generalization of the heads-up model, no extra multiway tuning; the implied-odds
// term and offsuit penalty above do the work. The afternoon's chart-fitted settings (play 1.5, gam 0.3, pen 2, pos 2)
// imitated the course's ICM-tightened BB in chip EV and are withdrawn. MW=0 restores the engine settlement.
// playk: extra playability weight per player beyond three (nut potential and implied odds matter more the more players
// see the flop; offsuit broadways suffer more too).
// init: how much of the preflop aggressor's initiative bonus (MODEL.kInit) survives with three or more in. A c-bet into two
// or more players gets through far less often, so 1 keeps the heads-up bonus and 0 removes it multiway.
const MW = { on: env('MW', 1), play: env('MWPLAY', 1), gam: env('MWGAM', 0), r: env('MWR', 1), pen: env('MWPEN', 1), leak: env('MWLEAK', 0), pos: env('MWPOS', 1), playk: env('MWPLAYK', 0), init: env('MWINIT', 1) };
const PLAY = (() => {
  const RK = '23456789TJQKA', t = new Float64Array(169);
  E.CLASS_NAMES.forEach((nm, i) => {
    const hi = RK.indexOf(nm[0]), lo = RK.indexOf(nm[1]), gap = hi - lo, suited = nm[2] === 's', pair = nm.length === 2;
    let k = 0;
    if (pair) k = hi <= RK.indexOf('9') ? 0.12 : hi <= RK.indexOf('J') ? 0.05 : 0;
    else if (suited) {
      if (hi === 12) k = lo <= RK.indexOf('5') ? 0.10 : 0.05;
      else if (gap === 1) k = 0.12; else if (gap === 2) k = 0.09; else if (gap === 3) k = 0.05; else k = 0.02;
      if (hi >= RK.indexOf('T') && lo >= RK.indexOf('T')) k = Math.max(k, 0.06);
    } else {
      if (hi >= RK.indexOf('T') && lo >= RK.indexOf('T')) k = 0;
      else if (hi === 12) k = lo >= RK.indexOf('T') ? 0 : -0.05;
      else if (gap === 1) k = -0.01; else if (gap === 2) k = -0.04; else k = -0.08;
    }
    t[i] = k;
  });
  return t;
})();
if (process.env.PLAYSET) PLAY.set(E.PLAY169);   // the multiway model keeps its own copy of the table: calibrate both
// per-class correction (169 numbers) to the multiway playability table only, fitted to GTO Wizard's multiway nodes
// (model_mw.json, frozen by build_all.sh). PLAYMW=<file.json> overrides it for calibration runs; PLAYMW=none turns it off.
{ const fs0 = require('fs'), pm = process.env.PLAYMW || (fs0.existsSync(__dirname + '/model_mw.json') ? __dirname + '/model_mw.json' : 'none');
  if (pm !== 'none') { const dd = JSON.parse(fs0.readFileSync(pm, 'utf8')); for (let i = 0; i < 169; i++) PLAY[i] += dd[i] || 0; } }
class PF2 extends E.PreflopSolver {
  constructor(cfg) {
    const ex = {}; for (const [k, e] of [['playSpan', 'PSPAN'], ['pairSpan', 'PAIRSPAN'], ['ioSpan', 'IOSPAN'], ['pRe', 'PRE']]) if (process.env[e] !== undefined) ex[k] = +process.env[e];
    cfg = { rLow: MODEL.rLow, gammaK: MODEL.gammaK, gRe: MODEL.gRe, playPos: MODEL.pPos, playNeg: MODEL.pNeg, implied: MODEL.io, ...ex, ...cfg }; super(cfg);
  }
  prepTerminal(nd) {
    super.prepTerminal(nd);
    if (nd.kind !== 'flop') return;
    const n = this.n, alive = nd.alive, pot = nd.pot;
    const order = (this.cfg.postOrder || alive).filter((i) => alive.includes(i)), k = order.length;
    const behind = alive.map((i) => this.cfg.stacks[i] - nd.put[i]).sort((a, b) => b - a)[1], spr = behind / Math.max(pot, 1e-9);
    const ramp = Math.min(1, Math.max(0, (spr - MODEL.s0) / MODEL.s1)), rLow = this.cfg.rLow;
    order.forEach((i, r) => {
      let r0 = k === 1 ? 1 : rLow + (1 - rLow) * Math.pow(r / (k - 1), k >= 3 && MW.on ? MW.pos : 1);
      if (i === nd.aggr && r < k - 1) r0 += (1 - r0) * MODEL.kInit * (k >= 3 && MW.on ? MW.init : 1);
      if (nd.aggr < 0 && r < k - 1) r0 += (1 - r0) * MODEL.kLimp;                   // limped pot: nobody has the initiative
      nd.R[i] = 1 - (1 - r0) * ramp;
    });
    // hand-strength curve; steeper in re-raised pots (gRe per raise level past the open: ranges are strong and tight there,
    // so a weak hand realizes even less of its equity), fades with the positional edge as stacks get short (gRamp)
    nd.gamma = 1 + this.cfg.gammaK * (1 + MODEL.gRe * Math.max(0, (nd.level || 1) - 1)) * Math.log(1 + spr) * (MODEL.gRamp ? ramp : 1);
    // stacks left behind still get played: at a low stack-to-pot ratio the rest usually goes in by the turn, which is
    // where value hands get paid (the reason to 3-bet small and let them jam). A share pAll of these flops is settled as
    // an all-in showdown for the remaining stacks; it fades from 1 at SPR 0.25 to 0 at SPR MODEL.pA.
    nd.pAll = MODEL.pA > 0.25 ? Math.pow(Math.min(1, Math.max(0, (MODEL.pA - spr) / (MODEL.pA - 0.25))), MODEL.pAk) : 0;
    if (nd.pAll > 0) {
      const tw = { kind: 'show', put: nd.put.slice(), folded: nd.folded.slice(), aggr: nd.aggr, twin: true };
      for (const i of alive) tw.put[i] = this.cfg.stacks[i];
      super.prepTerminal(tw); nd.twin = tw;
    } else nd.twin = null;
  }
  // heads-up: how often the rest goes in depends on the matchup. Close matchups get it in; a dominated hand mostly
  // gives up on the flop (losing only the pot), so the stack-off share scales with 2*min(eq, 1-eq). Zero-sum by symmetry.
  termVec2(nd, p, o, out) {
    super.termVec2(nd, p, o, out);
    if (nd.kind !== 'flop' || !nd.twin || !(nd.pAll > 0)) return;
    const tmp = this._twv2 || (this._twv2 = new Float64Array(this.NC)); super.termVec2(nd.twin, p, o, tmp);
    const oc = this.cls[o], NC = this.NC;
    for (let c = 0; c < NC; c++) { const eq = this.EQ[c * NC + oc], q = nd.pAll * Math.min(1, 2 * Math.min(eq, 1 - eq) * MODEL.pM); out[c] = (1 - q) * out[c] + q * tmp[c]; }
  }
  // multiway pots, and players who already folded (their ICM equity still moves with the others' stacks)
  // add each pot layer to fin by model shares among the players eligible for it (see MW above)
  mwShares(nd, cls, fin) {
    const NC = this.NC;
    if (!this._LO) { this._LO = new Float64Array(NC * NC); for (let i = 0; i < NC * NC; i++) { const a = Math.min(0.995, Math.max(0.005, this.EQ[i])); this._LO[i] = Math.log(a / (1 - a)); } }
    const LO = this._LO, s = this._mwS || (this._mwS = new Float64Array(this.n)), x = this._mwX || (this._mwX = new Float64Array(this.n));
    for (const L of nd.layers) {
      const el = L.elig, k = el.length;
      if (k === 1) { fin[el[0]] += L.amount; continue; }
      let ss = 0;
      for (const i of el) { let lg = 0; for (const j of el) if (j !== i) lg += LO[cls[i] * NC + cls[j]]; s[i] = Math.exp(lg / (2 * (k - 1))); ss += s[i]; }
      const g = nd.gamma + MW.gam * (k - 2), mr = k >= 3 ? MW.r : 1, mp = k >= 3 ? MW.play + MW.playk * (k - 3) : 1, pen = k >= 3 ? MW.pen : 1; let xs = 0, bs = 0;
      const b = this._mwB || (this._mwB = new Float64Array(this.n));
      for (const i of el) { const pt = E.playTerm(nd, cls[i], PLAY[cls[i]], MODEL.pPos, MODEL.pNeg); b[i] = (1 + (pt < 0 ? pt * pen : pt) * mp) * Math.pow(s[i] / ss, g); bs += b[i]; x[i] = (1 - (1 - nd.R[i]) * mr) * b[i]; xs += x[i]; }
      if (k >= 3 && MW.leak > 0) {
        // leak to position: each player keeps R of his base share, the rest goes to the players acting after him
        const rank = this._mwRank || (this._mwRank = (() => { const o = this.cfg.postOrder || [...Array(this.n).keys()], r = new Int32Array(this.n); o.forEach((p, q) => (r[p] = q)); return r; })());
        const ord = Array.from(el).sort((p, q) => rank[p] - rank[q]), y = this._mwY || (this._mwY = new Float64Array(this.n));
        for (const i of el) y[i] = 0;
        for (let q = 0; q < ord.length; q++) {
          const i = ord[q], bi = b[i] / bs, keep = bi * (1 - (1 - nd.R[i]) * mr); y[i] += keep;
          let w = 0; for (let t = q + 1; t < ord.length; t++) w += b[ord[t]];
          if (w > 0) for (let t = q + 1; t < ord.length; t++) y[ord[t]] += ((bi - keep) * b[ord[t]]) / w; else y[i] += bi - keep;
        }
        for (const i of el) y[i] = (1 - MW.leak) * (x[i] / xs) + MW.leak * y[i];
        this.mwImplied(nd, cls, el, y); for (const i of el) fin[i] += L.amount * y[i];
      } else { const y = this._mwY || (this._mwY = new Float64Array(this.n)); for (const i of el) y[i] = x[i] / xs; this.mwImplied(nd, cls, el, y); for (const i of el) fin[i] += L.amount * y[i]; }
    }
  }
  // implied odds multiway (MODEL.io): each player with nut potential gains io * PLAY+ * playK share points, paid by
  // everyone in proportion to their current share (zero-sum), as in the heads-up model
  mwImplied(nd, cls, el, y) {
    const io = MODEL.io; if (!(io > 0) || !(nd.playK > 0)) return;
    let T = 0; const t = this._mwT || (this._mwT = new Float64Array(this.n));
    for (const i of el) { t[i] = io * Math.max(0, PLAY[cls[i]] * MODEL.pPos) * E.ioTerm(nd, cls[i]); T += t[i]; }
    for (const i of el) y[i] = Math.max(0, y[i] + t[i] - T * y[i]);
    let s = 0; for (const i of el) s += y[i]; if (s > 0) for (const i of el) y[i] /= s;
  }
  mwTermVec(nd, p, out) {
    const n = this.n, st = this.cfg.stacks, cls = this.cls, save = cls[p], alive = !nd.folded[p];
    const icm = this.cfg.payoff && this.cfg.payoff.type === 'icm';
    const fin = this._mwFin || (this._mwFin = new Float64Array(n));
    const one = (c) => {
      cls[p] = c;
      for (let i = 0; i < n; i++) fin[i] = st[i] - nd.put[i];
      this.mwShares(nd, cls, fin);
      if (!icm) return fin[p] - st[p];
      for (let i = 0; i < n; i++) fin[i] = Math.round(fin[i] * 32) / 32;            // coarse enough for the ICM cache
      this._mwUse = true; const u = this.utility(nd)[p]; this._mwUse = false; return u;
    };
    if (!alive) { const u = one(save); for (let c = 0; c < this.NC; c++) out[c] = u * this.cw[c]; }
    else for (let c = 0; c < this.NC; c++) out[c] = this.cw[c] > 0 ? one(c) * this.cw[c] : 0;
    cls[p] = save;
  }
  finalStacks(nd) { if (this._mwUse) { this._lw = null; return Float64Array.from(this._mwFin); } return super.finalStacks(nd); }
  termVec(nd, p, out) {
    if (MW.on && nd.kind === 'flop' && nd.alive.length >= 3) this.mwTermVec(nd, p, out); else super.termVec(nd, p, out);
    if (nd.kind !== 'flop' || !nd.twin || !(nd.pAll > 0)) return;
    if (nd.alive.length === 2 && nd.alive.includes(p)) return;                     // handled per matchup in termVec2
    const tmp = this._twv || (this._twv = new Float64Array(this.NC)); super.termVec(nd.twin, p, tmp);
    const q = nd.pAll * MODEL.pMw; for (let c = 0; c < this.NC; c++) out[c] = (1 - q) * out[c] + q * tmp[c];
  }
  buildTree() {
    const cfg = this.cfg, n = this.n, st = cfg.stacks;
    const pre = cfg.postOrder || [...Array(n).keys()];
    const postRank = (p) => pre.indexOf(p);                 // later in postOrder = acts later postflop = in position
    const s0 = { put: cfg.posts.slice(), folded: new Array(n).fill(false), allin: cfg.posts.map((x, i) => x >= st[i] - 1e-9), needs: new Array(n).fill(true),
      bet: Math.max(...cfg.posts), level: 0, lastRaise: Math.max(1, Math.max(...cfg.posts)), cur: -1, acted: new Array(n).fill(false), vol: new Array(n).fill(false), opener: -1, openTo: 0, raiser: -1 };
    const clone = (s) => ({ ...s, put: s.put.slice(), folded: s.folded.slice(), allin: s.allin.slice(), needs: s.needs.slice(), acted: s.acted.slice(), vol: s.vol.slice() });
    const live = (s) => s.folded.reduce((a, f) => a + (f ? 0 : 1), 0);
    const next = (s) => {
      if (live(s) < 2) return -1;
      for (let k = 1; k <= n; k++) {
        const i = (s.cur + k + n) % n;
        if (!s.folded[i] && !s.allin[i] && s.needs[i]) {
          let others = 0; for (let j = 0; j < n; j++) if (j !== i && !s.folded[j] && !s.allin[j]) others++;
          if (!others && s.put[i] >= s.bet - 1e-9) return -1;
          return i;
        }
      }
      return -1;
    };
    const r1 = (x) => Math.round(x * 10) / 10;
    const effOf = (s, p) => { let m = 0; for (let j = 0; j < n; j++) if (j !== p && !s.folded[j]) m = Math.max(m, st[j]); return Math.min(st[p], m); };
    // is p in position against everyone still live (acts last postflop)?
    const inPos = (s, p) => { for (let j = 0; j < n; j++) if (j !== p && !s.folded[j] && postRank(j) > postRank(p)) return false; return true; };
    const opts = (s, p) => {
      const o = [], toCall = s.bet - s.put[p], left = st[p] - s.put[p], inVol = s.vol[p], name = cfg.names[p];
      let others = 0; for (let j = 0; j < n; j++) if (j !== p && !s.folded[j] && !s.allin[j]) others++;
      const callers = s.vol.filter((v, j) => v && !s.folded[j]).length - 1;
      const eff = effOf(s, p), L = s.level + 1;
      const jamOk = eff <= ((L === 2 && name !== 'SB' && name !== 'BB') ? JAM_CAP_NONBLIND3 : (JAM_CAP[L] || 1e9)) + 1e-9 || L >= 4;
      if (cfg.pushFold) { if (toCall > 1e-9) o.push({ k: 'fold' }); else o.push({ k: 'check' }); if (s.level === 0 && toCall > 1e-9) { o.push({ k: 'allin', to: st[p], L: 1 }); } else if (s.level >= 1) o.push({ k: 'call' }); return o; }
      if (toCall > 1e-9) {
        o.push({ k: 'fold' });
        if (left <= toCall + 1e-9) { o.push({ k: 'call' }); return o; }            // calling puts you all in
        // facing an all-in: fold / call, plus an isolating re-jam when someone else with chips is still in the hand
        if (s.raiser >= 0 && s.bet >= st[s.raiser] - 1e-9) {
          o.push({ k: 'call' });
          let live2 = 0; for (let j = 0; j < n; j++) if (j !== p && j !== s.raiser && !s.folded[j] && !s.allin[j]) live2++;
          if (live2 > 0 && s.level < 3) o.push({ k: 'allin', to: st[p], L: s.level + 1 });
          return o;
        }
        if (s.level === 0) { if (name === 'SB' && live(s) === 2) o.push({ k: 'call' }); }   // SB completes vs BB only
        else if (s.level === 1) { if (callers < 2 || name === 'BB') o.push({ k: 'call' }); }
        else if (inVol) o.push({ k: 'call' });
      } else o.push({ k: 'check' });
      if (!others) return o;
      if (s.level >= 3 && !inVol) return o;                                        // cold vs a 4-bet: fold only
      let to = 0;
      if (L === 1) to = name === 'SB' ? SB_OPEN(eff) : name === 'BB' ? ISO(eff) : OPEN(eff);
      else if (L === 2) {
        // 3-bet sizes by depth (RYE Masterclass anchors: 2.75x at 20bb vs a min-raise, ~3x at 30bb, 3-3.2x IP / ~3.5x OOP
        // deep), squeezes much bigger (+1.5 opens per caller: ~13bb at 100bb vs a 2.3x open and a call)
        // in position = acts after the opener (and any callers) postflop; players still to act behind don't count
        const ip = postRank(p) > postRank(s.raiser) && s.vol.every((v, j) => !v || j === p || s.folded[j] || postRank(p) > postRank(j));
        // deeper, GTO Wizard's MTT chip-EV sizes (read Oct 26): SB 3-bet vs BTN 3.5x at 40bb, 3.75x at 60bb, 4.5x at
        // 100bb; BTN 3-bet vs UTG 4.0x at 100bb; a squeeze adds 0.5x per caller (5.5x vs an open and two calls)
        let mult = ip ? ip3(eff) : oop3(eff);
        if (name === 'SB' && cfg.names[s.raiser] === 'BB') mult = 2.8;          // SB limp, BB raises: the re-raise is small
        to = s.bet * mult + Math.max(0, callers) * (eff <= 25 ? 0.75 : 0.5) * s.bet;
      }
      else if (L === 3) to = s.bet * (eff >= 75 ? 2.2 : 2.0);   // 4-bets: GTO Wizard 2.2x at 100bb, 2.0x at 40-60bb (where the jam does most of the work)
      const coldVs3 = s.level === 2 && !inVol;
      const sized = L <= 3 && !coldVs3 && to > 0 && to < st[p] * (L === 3 ? 0.5 : 0.4) && to >= s.bet + s.lastRaise - 1e-9;
      if (sized) o.push({ k: 'raise', to: r1(to), L });
      if (jamOk || !sized) o.push({ k: 'allin', to: st[p], L });
      return o;
    };
    const apply = (s, p, a) => {
      const t = clone(s); t.needs[p] = false; t.acted[p] = true; t.cur = p;
      if (a.k !== 'fold' && a.k !== 'check') t.vol[p] = true;
      if (a.k === 'fold') t.folded[p] = true;
      else if (a.k === 'call') { t.put[p] = Math.min(t.bet, st[p]); if (t.put[p] >= st[p] - 1e-9) t.allin[p] = true; }
      else if (a.k === 'raise' || a.k === 'allin') {
        const to = Math.min(a.to, st[p]), raise = to - t.bet; t.put[p] = to; if (to >= st[p] - 1e-9) t.allin[p] = true;
        if (raise > 1e-9) { t.level++; t.raiser = p; if (raise >= t.lastRaise - 1e-9) t.lastRaise = raise; t.bet = to; for (let j = 0; j < n; j++) if (j !== p && !t.folded[j] && !t.allin[j]) t.needs[j] = true; }
      }
      return t;
    };
    const build = (s) => {
      const p = next(s);
      if (p < 0) {
        const lv = live(s); let free = 0; for (let j = 0; j < n; j++) if (!s.folded[j] && !s.allin[j]) free++;
        const nd = { kind: lv === 1 ? 'fold' : free >= 2 ? 'flop' : 'show', put: s.put.slice(), folded: s.folded.slice(), aggr: s.raiser, level: s.level };
        this.prepTerminal(nd); this.nodes.push(nd); return this.nodes.length - 1;
      }
      const os = opts(s, p);
      const id = this.nodes.length; this.nodes.push({ kind: 'act', player: p, actions: os, children: [], level: s.level, bet: s.bet, put: s.put.slice() });
      if (this.nodes.length > (cfg.maxNodes || 400000)) throw new Error('Preflop tree too large');
      for (const a of os) this.nodes[id].children.push(build(apply(s, p, a)));
      return id;
    };
    build(s0);
  }
}

// label for an action at a node, from the node itself (the only source of truth for button text)
function label(nd, a, cfg) {
  const f = (x) => (Math.round(x * 10) / 10).toString();
  const name = cfg.names[nd.player];
  if (a.k === 'fold') return 'Fold';
  if (a.k === 'check') return 'Check';
  if (a.k === 'call') { if (nd.level === 0) return 'Complete'; return 'Call ' + f(Math.min(nd.bet, cfg.stacks[nd.player])); }
  if (a.k === 'allin') return 'Jam ' + f(a.to);
  if (a.k === 'raise') { const L = nd.level + 1; return (L === 1 ? (name === 'BB' ? 'Raise ' : 'Raise ') : L === 2 ? '3-Bet ' : L === 3 ? '4-Bet ' : 'Raise ') + f(a.to); }
  return a.k;
}

module.exports = { MODEL, MW, PF2, label, E, OPEN, SB_OPEN, ISO, JAM_CAP };
