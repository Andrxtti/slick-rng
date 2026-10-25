// Clean preflop tree for The Dream / Slick RNG (GTO-Wizard-style menu), on top of the app's PreflopSolver core.
//  first in (UTG..BTN): fold / raise / jam (jam only when it is a real play at this depth)
//  SB folded to:        fold / complete / raise / jam          BB vs SB complete: check / raise / jam
//  vs an open:          fold / call / 3-bet / jam              (at most two cold callers)
//  vs a 3-bet:          players already in: fold / call / 4-bet / jam;  cold players: fold / jam
//  vs a 4-bet:          players already in: fold / call / jam;  cold players: fold
//  vs an all-in:        fold / call
// No open-limps outside the SB, no "raise to all-but-1bb". Sizes scale with depth and position.
const E = require('../../app/engine.js');

const OPEN = (eff) => (eff >= 50 ? 2.3 : eff >= 35 ? 2.2 : eff >= 25 ? 2.1 : 2.0);
const SB_OPEN = (eff) => (eff >= 40 ? 3.0 : eff >= 20 ? 2.5 : 2.0);
const ISO = (eff) => (eff >= 30 ? 3.5 : 3.0);           // BB raise over an SB complete
const JAM_CAP = { 1: 25, 2: 50, 3: 100 };                // deepest effective stack where a jam is on the menu, by raise level

// postflop realization, fitted to published solver equity-realization reports (IP ~1.15, OOP ~0.8 in
// single-raised pots at 20-100bb): the positional penalty holds until stacks are nearly all in
const MODEL = { rLow: 0.70, gammaK: 0.0, s0: 1.0, s1: 2.0 };
class PF2 extends E.PreflopSolver {
  constructor(cfg) { cfg = { rLow: MODEL.rLow, gammaK: MODEL.gammaK, ...cfg }; super(cfg); }
  prepTerminal(nd) {
    super.prepTerminal(nd);
    if (nd.kind !== 'flop') return;
    const n = this.n, alive = nd.alive, pot = nd.pot;
    const order = (this.cfg.postOrder || alive).filter((i) => alive.includes(i)), k = order.length;
    const behind = alive.map((i) => this.cfg.stacks[i] - nd.put[i]).sort((a, b) => b - a)[1], spr = behind / Math.max(pot, 1e-9);
    const ramp = Math.min(1, Math.max(0, (spr - MODEL.s0) / MODEL.s1)), rLow = this.cfg.rLow;
    order.forEach((i, r) => { const r0 = k === 1 ? 1 : rLow + (1 - rLow) * (r / (k - 1)); nd.R[i] = 1 - (1 - r0) * ramp; });
    nd.gamma = 1 + this.cfg.gammaK * Math.log(1 + spr);
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
      const jamOk = eff <= (JAM_CAP[L] || 1e9) + 1e-9 || L >= 4;
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
      else if (L === 2) { const ip = inPos(s, p), mult = ip ? 3.0 : (name === 'SB' || name === 'BB') ? 3.8 : 3.4; to = s.bet * mult + Math.max(0, callers) * s.bet; }
      else if (L === 3) to = s.bet * (inPos(s, p) ? 2.2 : 2.4);
      const coldVs3 = s.level === 2 && !inVol;
      const sized = L <= 3 && !coldVs3 && to > 0 && to < st[p] * 0.4 && to >= s.bet + s.lastRaise - 1e-9;
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
        const nd = { kind: lv === 1 ? 'fold' : free >= 2 ? 'flop' : 'show', put: s.put.slice(), folded: s.folded.slice() };
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

module.exports = { MODEL, PF2, label, E, OPEN, SB_OPEN, ISO, JAM_CAP };
