//! N-player (2..=9) postflop betting tree.
//!
//! The tree is public-information only: it is built once from the spot's
//! betting configuration and never depends on private or board cards.
//! Chance nodes mark street changes; cards are supplied by the solver.

#[derive(Clone, Debug)]
pub enum Size {
    /// fraction of the pot (0.33 = 33% pot)
    Pot(f64),
    /// multiple of the bet being faced (raises only)
    Mult(f64),
    AllIn,
}

impl Size {
    pub fn parse_list(s: &str, is_raise: bool) -> Result<Vec<Size>, String> {
        let mut v = Vec::new();
        for t in s.split(',') {
            let t = t.trim().to_ascii_lowercase();
            if t.is_empty() {
                continue;
            }
            if t == "a" || t == "allin" || t == "all-in" {
                v.push(Size::AllIn);
            } else if let Some(p) = t.strip_suffix('%') {
                let x: f64 = p.parse().map_err(|_| format!("bad size '{t}'"))?;
                v.push(Size::Pot(x / 100.0));
            } else if let Some(m) = t.strip_suffix('x') {
                if !is_raise {
                    return Err(format!("'{t}': x-sizes only apply to raises"));
                }
                let x: f64 = m.parse().map_err(|_| format!("bad size '{t}'"))?;
                v.push(Size::Mult(x));
            } else {
                return Err(format!("bad size '{t}' (use 50%, 2.5x or a)"));
            }
        }
        Ok(v)
    }
}

#[derive(Clone, Debug)]
pub struct StreetSizes {
    pub bets: Vec<Size>,
    pub raises: Vec<Size>,
}

#[derive(Clone, Debug)]
pub struct TreeConfig {
    /// players in postflop action order (first to act first)
    pub num_players: usize,
    /// chips behind for each player at the start of the spot
    pub stacks: Vec<f64>,
    /// pot at the start of the spot (all prior money, including blinds/antes)
    pub pot: f64,
    /// 0 = flop, 1 = turn, 2 = river
    pub start_street: u8,
    /// sizes per street (index 0 flop, 1 turn, 2 river)
    pub sizes: [StreetSizes; 3],
    /// max raises per street (after the first bet)
    pub raise_cap: u8,
    /// add an all-in option when the largest bet is within this fraction of the stack
    pub allin_threshold: f64,
    /// sizes within this relative distance are merged
    pub merge_threshold: f64,
}

#[derive(Clone, Copy, Debug, PartialEq)]
pub enum Action {
    Fold,
    Check,
    Call,
    /// bet or raise to this total amount on the current street
    BetTo(f64),
    AllIn(f64),
}

impl Action {
    pub fn label(&self) -> String {
        match self {
            Action::Fold => "Fold".into(),
            Action::Check => "Check".into(),
            Action::Call => "Call".into(),
            Action::BetTo(x) => format!("Bet {}", fmt_chips(*x)),
            Action::AllIn(x) => format!("All-in {}", fmt_chips(*x)),
        }
    }
}

pub fn fmt_chips(x: f64) -> String {
    if (x - x.round()).abs() < 1e-6 {
        format!("{}", x.round() as i64)
    } else {
        format!("{:.1}", x)
    }
}

#[derive(Clone, Debug)]
pub struct State {
    pub street: u8,
    pub pot: f64, // includes all bets made so far (all streets)
    pub street_bet: Vec<f64>, // amount put in on the current street
    pub invested: Vec<f64>, // total put in during the spot (all streets)
    pub folded: Vec<bool>,
    pub allin: Vec<bool>,
    pub to_act: usize,
    pub needs_to_act: Vec<bool>,
    pub raises: u8,
    pub last_raise_size: f64,
}

#[derive(Clone, Debug)]
pub enum NodeKind {
    Action { player: usize, actions: Vec<Action>, children: Vec<u32> },
    /// next street card is dealt; child continues the betting
    Chance { street: u8, child: u32 },
    /// one player left
    Fold,
    /// showdown; if `runout` the board is completed with no more betting
    Showdown,
}

#[derive(Clone, Debug)]
pub struct Node {
    pub kind: NodeKind,
    pub street: u8,
    pub invested: Vec<f64>,
    pub folded: Vec<bool>,
    pub pot: f64,
}

pub struct Tree {
    pub cfg: TreeConfig,
    pub nodes: Vec<Node>,
}

impl Tree {
    pub fn build(cfg: TreeConfig) -> Result<Tree, String> {
        let n = cfg.num_players;
        if !(2..=9).contains(&n) {
            return Err("players must be 2..9".into());
        }
        if cfg.stacks.len() != n {
            return Err("one stack per player required".into());
        }
        let st = State {
            street: cfg.start_street,
            pot: cfg.pot,
            street_bet: vec![0.0; n],
            invested: vec![0.0; n],
            folded: vec![false; n],
            allin: cfg.stacks.iter().map(|&s| s <= 0.0).collect(),
            to_act: 0,
            needs_to_act: vec![true; n],
            raises: 0,
            last_raise_size: 0.0,
        };
        let mut t = Tree { cfg, nodes: Vec::new() };
        let mut st = st;
        st.to_act = t.next_actor(&st, n - 1).unwrap_or(0);
        t.build_rec(st)?;
        if t.nodes.len() > 20_000_000 {
            return Err("tree too large; reduce bet sizes".into());
        }
        Ok(t)
    }

    fn push(&mut self, st: &State, kind: NodeKind) -> u32 {
        self.nodes.push(Node {
            kind,
            street: st.street,
            invested: st.invested.clone(),
            folded: st.folded.clone(),
            pot: st.pot,
        });
        (self.nodes.len() - 1) as u32
    }

    fn active_count(&self, st: &State) -> usize {
        st.folded.iter().filter(|f| !**f).count()
    }

    fn can_act(&self, st: &State, p: usize) -> bool {
        !st.folded[p] && !st.allin[p]
    }

    /// next player after `p` (cyclic) who still needs to act
    fn next_actor(&self, st: &State, p: usize) -> Option<usize> {
        let n = st.folded.len();
        for k in 1..=n {
            let q = (p + k) % n;
            if self.can_act(st, q) && st.needs_to_act[q] {
                return Some(q);
            }
        }
        None
    }

    fn remaining(&self, st: &State, p: usize) -> f64 {
        self.cfg.stacks[p] - st.invested[p]
    }

    fn build_rec(&mut self, st: State) -> Result<u32, String> {
        if self.active_count(&st) == 1 {
            return Ok(self.push(&st, NodeKind::Fold));
        }
        let p = st.to_act;
        let actions = self.gen_actions(&st, p);
        let id = self.push(&st, NodeKind::Action { player: p, actions: actions.clone(), children: vec![] });
        let mut children = Vec::with_capacity(actions.len());
        for a in &actions {
            let next = self.apply(&st, p, *a);
            children.push(self.after_action(next)?);
        }
        if let NodeKind::Action { children: c, .. } = &mut self.nodes[id as usize].kind {
            *c = children;
        }
        Ok(id)
    }

    /// decide what follows once an action has been applied
    fn after_action(&mut self, st: State) -> Result<u32, String> {
        if self.active_count(&st) == 1 {
            return Ok(self.push(&st, NodeKind::Fold));
        }
        if let Some(q) = self.next_actor(&st, st.to_act) {
            let mut s = st;
            s.to_act = q;
            return self.build_rec(s);
        }
        // betting round closed
        let can_bet = (0..st.folded.len()).filter(|&q| self.can_act(&st, q)).count();
        if st.street == 2 || can_bet <= 1 {
            return Ok(self.push(&st, NodeKind::Showdown));
        }
        // next street
        let n = st.folded.len();
        let mut s = st;
        s.street += 1;
        s.street_bet = vec![0.0; n];
        s.raises = 0;
        s.last_raise_size = 0.0;
        for q in 0..n {
            s.needs_to_act[q] = !s.folded[q] && !s.allin[q];
        }
        s.to_act = self.next_actor(&s, n - 1).unwrap();
        let chance = self.push(&s, NodeKind::Chance { street: s.street, child: 0 });
        let child = self.build_rec(s)?;
        if let NodeKind::Chance { child: c, .. } = &mut self.nodes[chance as usize].kind {
            *c = child;
        }
        Ok(chance)
    }

    fn gen_actions(&self, st: &State, p: usize) -> Vec<Action> {
        let max_bet = st.street_bet.iter().cloned().fold(0.0, f64::max);
        let to_call = max_bet - st.street_bet[p];
        let rem = self.remaining(st, p);
        let allin_to = st.street_bet[p] + rem; // street total if all-in
        let sizes = &self.cfg.sizes[(st.street) as usize];
        let mut acts = Vec::new();
        // can anyone else still respond to a bet?
        let others_can_act = (0..st.folded.len()).any(|q| q != p && self.can_act(st, q));

        if to_call > 1e-9 {
            acts.push(Action::Fold);
            if rem <= to_call + 1e-9 {
                acts.push(Action::AllIn(allin_to));
                return acts;
            }
            acts.push(Action::Call);
            if !others_can_act || st.raises >= self.cfg.raise_cap {
                return acts;
            }
            let pot_after_call = st.pot + to_call;
            let min_raise_to = max_bet + st.last_raise_size.max(to_call);
            let mut targets: Vec<f64> = Vec::new();
            for s in &sizes.raises {
                let t = match s {
                    Size::Pot(f) => max_bet + f * pot_after_call,
                    Size::Mult(m) => max_bet * m,
                    Size::AllIn => allin_to,
                };
                targets.push(t.max(min_raise_to));
            }
            self.push_sized(&mut acts, targets, allin_to, st.pot + to_call);
        } else {
            acts.push(Action::Check);
            if !others_can_act {
                return acts;
            }
            let mut targets = Vec::new();
            for s in &sizes.bets {
                let t = match s {
                    Size::Pot(f) => f * st.pot,
                    Size::Mult(_) => continue,
                    Size::AllIn => allin_to,
                };
                targets.push(t.max(1.0_f64.min(allin_to)));
            }
            self.push_sized(&mut acts, targets, allin_to, st.pot);
        }
        acts
    }

    fn push_sized(&self, acts: &mut Vec<Action>, mut targets: Vec<f64>, allin_to: f64, pot: f64) {
        targets.sort_by(|a, b| a.partial_cmp(b).unwrap());
        let mut chosen: Vec<f64> = Vec::new();
        let mut has_allin = false;
        for t in targets {
            let t = (t * 100.0).round() / 100.0;
            // close to all-in -> all-in
            if t >= allin_to * (1.0 - 1e-9) {
                has_allin = true;
                continue;
            }
            if let Some(&last) = chosen.last() {
                // merge sizes that are nearly identical relative to the pot
                if (t - last) / pot.max(1e-9) < self.cfg.merge_threshold {
                    continue;
                }
            }
            chosen.push(t);
        }
        // add all-in automatically when the biggest bet leaves little behind
        if let Some(&last) = chosen.last() {
            if (allin_to - last) <= self.cfg.allin_threshold * (pot + 2.0 * last) {
                has_allin = true;
            }
        }
        for t in chosen {
            acts.push(Action::BetTo(t));
        }
        if has_allin {
            acts.push(Action::AllIn(allin_to));
        }
    }

    fn apply(&self, st: &State, p: usize, a: Action) -> State {
        let mut s = st.clone();
        let max_bet = st.street_bet.iter().cloned().fold(0.0, f64::max);
        s.needs_to_act[p] = false;
        let put_to = |s: &mut State, total: f64| {
            let add = total - s.street_bet[p];
            s.street_bet[p] = total;
            s.invested[p] += add;
            s.pot += add;
            if self.cfg.stacks[p] - s.invested[p] <= 1e-9 {
                s.allin[p] = true;
            }
        };
        match a {
            Action::Fold => s.folded[p] = true,
            Action::Check => {}
            Action::Call => put_to(&mut s, max_bet),
            Action::BetTo(t) | Action::AllIn(t) => {
                put_to(&mut s, t);
                if t > max_bet + 1e-9 {
                    // a full raise reopens the action; a short all-in only
                    // reopens it for players who haven't acted yet.
                    let raise = t - max_bet;
                    let full = raise >= st.last_raise_size.max(1e-9) - 1e-9;
                    if max_bet > 0.0 {
                        s.raises += 1;
                    }
                    if full || max_bet == 0.0 {
                        s.last_raise_size = raise;
                    }
                    for q in 0..s.folded.len() {
                        if q != p && !s.folded[q] && !s.allin[q] {
                            s.needs_to_act[q] = true;
                        }
                    }
                }
            }
        }
        s.to_act = p;
        s
    }

    /// human-readable path of actions from root to node (debug/output)
    pub fn action_node_count(&self) -> usize {
        self.nodes.iter().filter(|n| matches!(n.kind, NodeKind::Action { .. })).count()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn cfg(n: usize, street: u8, bets: &str, raises: &str) -> TreeConfig {
        let s = StreetSizes {
            bets: Size::parse_list(bets, false).unwrap(),
            raises: Size::parse_list(raises, true).unwrap(),
        };
        TreeConfig {
            num_players: n,
            stacks: vec![100.0; n],
            pot: 10.0,
            start_street: street,
            sizes: [s.clone(), s.clone(), s],
            raise_cap: 2,
            allin_threshold: 0.0,
            merge_threshold: 0.1,
        }
    }

    #[test]
    fn heads_up_river_check_check() {
        let t = Tree::build(cfg(2, 2, "50%", "")).unwrap();
        // root: OOP check/bet
        match &t.nodes[0].kind {
            NodeKind::Action { player, actions, .. } => {
                assert_eq!(*player, 0);
                assert_eq!(actions, &vec![Action::Check, Action::BetTo(5.0)]);
            }
            _ => panic!(),
        }
    }

    #[test]
    fn three_way_builds_and_terminates() {
        let t = Tree::build(cfg(3, 0, "33%,75%", "3x")).unwrap();
        assert!(t.nodes.len() > 100);
        // every action node's children exist
        for n in &t.nodes {
            if let NodeKind::Action { children, actions, .. } = &n.kind {
                assert_eq!(children.len(), actions.len());
                for &c in children {
                    assert!((c as usize) < t.nodes.len());
                }
            }
        }
        // money is conserved: pot == start pot + invested
        for n in &t.nodes {
            let inv: f64 = n.invested.iter().sum();
            assert!((n.pot - 10.0 - inv).abs() < 1e-6);
            for (p, &x) in n.invested.iter().enumerate() {
                assert!(x <= 100.0 + 1e-6, "player {p} invested {x}");
            }
        }
    }
}
