//! Multiway Monte Carlo CFR (external sampling, linear weighting).

use crate::abstraction::Abstraction;
use crate::cards::*;
use crate::icm::{icm_equity, Payoff};
use crate::table::{FxMap, Table};
use crate::tree::{NodeKind, Tree};
use std::sync::atomic::{AtomicBool, AtomicU64, Ordering::Relaxed};
use std::time::Instant;

pub struct Spot {
    pub names: Vec<String>,
    pub tree: Tree,
    pub board: Vec<Card>,
    pub ranges: Vec<Range>,
    pub payoff: Payoff,
    /// 0 = exact (one info set per combo and runout)
    pub turn_buckets: usize,
    pub river_buckets: usize,
}

pub struct Rng(u64);
impl Rng {
    pub fn new(seed: u64) -> Rng {
        Rng(seed.wrapping_mul(0x9E37_79B9_7F4A_7C15) | 1)
    }
    #[inline]
    pub fn next(&mut self) -> u64 {
        let mut x = self.0;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        self.0 = x;
        x.wrapping_mul(0x2545_F491_4F6C_DD1D)
    }
    #[inline]
    pub fn unit(&mut self) -> f64 {
        (self.next() >> 11) as f64 / (1u64 << 53) as f64
    }
    #[inline]
    pub fn below(&mut self, n: usize) -> usize {
        (self.unit() * n as f64) as usize
    }
}

/// One sampled deal: private hands, full runout and per-street info keys.
pub struct Deal {
    pub combo: Vec<usize>,
    pub board: [Card; 5],
    pub strength: Vec<u32>,
    /// hand key per player per street (index = street 0..3)
    pub key: Vec<[u32; 3]>,
}

struct PlayerRange {
    combos: Vec<usize>,
    cum: Vec<f64>,
}

pub struct Solver {
    pub spot: Spot,
    pub table: Table,
    abs: Option<Abstraction>,
    pranges: Vec<PlayerRange>,
    pub iterations: AtomicU64,
    stop: AtomicBool,
}

const MAX_ACTIONS: usize = 16;
static mut RM_PLUS: bool = true;
static mut AVG_POW: f32 = 2.0;
pub fn set_variant(rm_plus: bool, avg_pow: f32) {
    unsafe {
        RM_PLUS = rm_plus;
        AVG_POW = avg_pow;
    }
}

impl Solver {
    pub fn new(spot: Spot, memory_bytes: u64) -> Result<Solver, String> {
        let n = spot.tree.cfg.num_players;
        if spot.ranges.len() != n {
            return Err("one range per player required".into());
        }
        let mut pranges = Vec::new();
        for (p, r) in spot.ranges.iter().enumerate() {
            let mut combos = Vec::new();
            let mut cum = Vec::new();
            let mut acc = 0.0;
            for c in 0..NUM_COMBOS {
                let (a, b) = combo_cards()[c];
                if r.weights[c] > 0.0 && !spot.board.contains(&a) && !spot.board.contains(&b) {
                    acc += r.weights[c] as f64;
                    combos.push(c);
                    cum.push(acc);
                }
            }
            if combos.is_empty() {
                return Err(format!("{}'s range is empty on this board", spot.names[p]));
            }
            pranges.push(PlayerRange { combos, cum });
        }
        for n in &spot.tree.nodes {
            if let NodeKind::Action { actions, .. } = &n.kind {
                if actions.len() > MAX_ACTIONS {
                    return Err("too many actions at one node".into());
                }
            }
        }
        let abs = if spot.turn_buckets > 0 || spot.river_buckets > 0 {
            Some(Abstraction::build(&spot.board, spot.turn_buckets, spot.river_buckets))
        } else {
            None
        };
        Ok(Solver {
            spot,
            table: Table::new(memory_bytes),
            abs,
            pranges,
            iterations: AtomicU64::new(0),
            stop: AtomicBool::new(false),
        })
    }

    fn start_street(&self) -> u8 {
        self.spot.tree.cfg.start_street
    }

    pub fn sample_deal(&self, rng: &mut Rng) -> Deal {
        let n = self.pranges.len();
        let mut combo = vec![0usize; n];
        let mut used = [false; 52];
        'retry: loop {
            used.iter_mut().for_each(|u| *u = false);
            for &c in &self.spot.board {
                used[c as usize] = true;
            }
            for p in 0..n {
                let pr = &self.pranges[p];
                let total = *pr.cum.last().unwrap();
                let x = rng.unit() * total;
                let i = pr.cum.partition_point(|&v| v <= x).min(pr.combos.len() - 1);
                let c = pr.combos[i];
                let (a, b) = combo_cards()[c];
                if used[a as usize] || used[b as usize] {
                    continue 'retry;
                }
                used[a as usize] = true;
                used[b as usize] = true;
                combo[p] = c;
            }
            break;
        }
        let mut board = [NO_CARD; 5];
        board[..self.spot.board.len()].copy_from_slice(&self.spot.board);
        for slot in self.spot.board.len()..5 {
            loop {
                let c = rng.below(52) as Card;
                if !used[c as usize] {
                    used[c as usize] = true;
                    board[slot] = c;
                    break;
                }
            }
        }
        let mut strength = vec![0u32; n];
        let mut cards = [0u8; 7];
        cards[..5].copy_from_slice(&board);
        for p in 0..n {
            let (a, b) = combo_cards()[combo[p]];
            cards[5] = a;
            cards[6] = b;
            strength[p] = evaluate(&cards);
        }
        let mut key = vec![[0u32; 3]; n];
        let start = self.start_street();
        for p in 0..n {
            for street in start..3 {
                key[p][street as usize] = self.hand_key(combo[p], street, &board);
            }
        }
        Deal { combo, board, strength, key }
    }

    /// info-set key for a combo on a street given the runout
    pub fn hand_key(&self, combo: usize, street: u8, board: &[Card; 5]) -> u32 {
        let start = self.start_street();
        if street == start {
            return combo as u32;
        }
        let flop_start = start == 0;
        if street == 1 {
            // turn (only reachable from a flop start)
            if let Some(abs) = &self.abs {
                if abs.turn_buckets > 0 {
                    return abs.turn_bucket(board[3], combo);
                }
            }
            return combo as u32 + NUM_COMBOS as u32 * (board[3] as u32 + 1);
        }
        // river
        let extra: &[Card] = if flop_start { &board[3..5] } else { &board[4..5] };
        if let Some(abs) = &self.abs {
            if abs.river_buckets > 0 {
                return abs.river_bucket(extra, combo);
            }
        }
        let path = if flop_start {
            53 + board[3] as u32 * 52 + board[4] as u32
        } else {
            board[4] as u32 + 1
        };
        combo as u32 + NUM_COMBOS as u32 * path
    }

    #[inline]
    fn infoset_key(node: u32, hand_key: u32) -> u64 {
        ((node as u64) << 32) | hand_key as u64
    }

    /// chips each player ends the spot with
    pub fn final_stacks(&self, node: usize, deal: &Deal) -> Vec<f64> {
        let tree = &self.spot.tree;
        let nd = &tree.nodes[node];
        let n = nd.invested.len();
        let stacks = &tree.cfg.stacks;
        let mut fin: Vec<f64> = (0..n).map(|i| stacks[i] - nd.invested[i]).collect();
        let alive: Vec<usize> = (0..n).filter(|&i| !nd.folded[i]).collect();
        if alive.len() == 1 {
            fin[alive[0]] += nd.pot;
            return fin;
        }
        // side pots by investment level
        let mut levels: Vec<f64> = alive.iter().map(|&i| nd.invested[i]).collect();
        levels.sort_by(|a, b| a.partial_cmp(b).unwrap());
        levels.dedup_by(|a, b| (*a - *b).abs() < 1e-9);
        let mut prev = 0.0;
        let mut distributed = 0.0;
        for (li, &lv) in levels.iter().enumerate() {
            let mut layer: f64 = (0..n)
                .map(|i| nd.invested[i].min(lv) - nd.invested[i].min(prev))
                .sum();
            if li == 0 {
                layer += tree.cfg.pot;
            }
            if li == levels.len() - 1 {
                // anything above the top level (folded overbets) goes here too
                layer = nd.pot - distributed;
            }
            let elig: Vec<usize> = alive.iter().cloned().filter(|&i| nd.invested[i] >= lv - 1e-9).collect();
            let best = elig.iter().map(|&i| deal.strength[i]).max().unwrap();
            let winners: Vec<usize> = elig.into_iter().filter(|&i| deal.strength[i] == best).collect();
            let share = layer / winners.len() as f64;
            for w in winners {
                fin[w] += share;
            }
            distributed += layer;
            prev = lv;
        }
        fin
    }

    fn utilities(&self, node: usize, deal: &Deal, cache: &mut FxMap<u64, Vec<f64>>) -> Vec<f64> {
        let fin = self.final_stacks(node, deal);
        match &self.spot.payoff {
            Payoff::Chips => fin
                .iter()
                .zip(&self.spot.tree.cfg.stacks)
                .map(|(f, s)| f - s)
                .collect(),
            Payoff::Icm { payouts, others } => {
                let mut h: u64 = 0xcbf29ce484222325;
                for x in &fin {
                    h = (h ^ ((x * 1000.0).round() as i64 as u64)).wrapping_mul(0x100000001b3);
                }
                if let Some(v) = cache.get(&h) {
                    return v.clone();
                }
                let n = fin.len();
                let mut all = fin.clone();
                all.extend_from_slice(others);
                let mut start: Vec<f64> = self.spot.tree.cfg.stacks.clone();
                start.extend_from_slice(others);
                let eq = icm_equity(&all, &start, payouts);
                let v = eq[..n].to_vec();
                if cache.len() < 200_000 {
                    cache.insert(h, v.clone());
                }
                v
            }
        }
    }

    fn traverse(&self, node: u32, p: usize, deal: &Deal, w: f32, rng: &mut Rng, cache: &mut FxMap<u64, Vec<f64>>) -> f64 {
        let nd = &self.spot.tree.nodes[node as usize];
        match &nd.kind {
            NodeKind::Fold | NodeKind::Showdown => self.utilities(node as usize, deal, cache)[p],
            NodeKind::Chance { child, .. } => self.traverse(*child, p, deal, w, rng, cache),
            NodeKind::Action { player, actions, children } => {
                let q = *player;
                let na = actions.len();
                let key = Self::infoset_key(node, deal.key[q][nd.street as usize]);
                let off = match self.table.get(key, na, true) {
                    Some(o) => o,
                    None => {
                        self.stop.store(true, Relaxed);
                        return 0.0;
                    }
                };
                let mut sigma = [0f64; MAX_ACTIONS];
                self.table.current_strategy(off, na, &mut sigma);
                if q == p {
                    let mut vals = [0f64; MAX_ACTIONS];
                    let mut v = 0.0;
                    for a in 0..na {
                        vals[a] = self.traverse(children[a], p, deal, w, rng, cache);
                        v += sigma[a] * vals[a];
                    }
                    for a in 0..na {
                        let i = off + a as u64;
                        let r = self.table.load(i) + w * (vals[a] - v) as f32;
                        self.table.store(i, if unsafe { RM_PLUS } { r.max(0.0) } else { r });
                    }
                    v
                } else {
                    let wa = w.powf(unsafe { AVG_POW });
                    for a in 0..na {
                        self.table.add(off + (na + a) as u64, wa * sigma[a] as f32);
                    }
                    let mut x = rng.unit();
                    let mut pick = na - 1;
                    for a in 0..na {
                        x -= sigma[a];
                        if x < 0.0 {
                            pick = a;
                            break;
                        }
                    }
                    self.traverse(children[pick], p, deal, w, rng, cache)
                }
            }
        }
    }

    /// Run MCCFR until `max_iters` deals, `seconds` elapse or memory runs out.
    /// `progress` is called roughly once a second from the main thread.
    pub fn solve(&self, max_iters: u64, seconds: f64, threads: usize, mut progress: impl FnMut(u64, f64, u64)) {
        let n = self.spot.tree.cfg.num_players;
        let t0 = Instant::now();
        let start_iter = self.iterations.load(Relaxed);
        self.stop.store(false, Relaxed);
        std::thread::scope(|s| {
            for tid in 0..threads.max(1) {
                let me = &*self;
                s.spawn(move || {
                    let mut rng = Rng::new(0x5EED + tid as u64 * 7919 + start_iter);
                    let mut cache = FxMap::default();
                    loop {
                        if me.stop.load(Relaxed) {
                            break;
                        }
                        let t = me.iterations.fetch_add(1, Relaxed);
                        if t >= max_iters {
                            me.stop.store(true, Relaxed);
                            break;
                        }
                        let w = (t / 1000 + 1) as f32; // linear weighting in blocks
                        let deal = me.sample_deal(&mut rng);
                        for p in 0..n {
                            me.traverse(0, p, &deal, w, &mut rng, &mut cache);
                        }
                    }
                });
            }
            loop {
                std::thread::sleep(std::time::Duration::from_millis(250));
                let el = t0.elapsed().as_secs_f64();
                let it = self.iterations.load(Relaxed).min(max_iters);
                progress(it, el, self.table.bytes_used());
                if self.stop.load(Relaxed) {
                    break;
                }
                if el >= seconds {
                    self.stop.store(true, Relaxed);
                    break;
                }
            }
        });
        let it = self.iterations.load(Relaxed);
        if it > max_iters {
            self.iterations.store(max_iters, Relaxed);
        }
    }

    pub fn out_of_memory(&self) -> bool {
        self.table.bytes_used() + 64 >= self.table.limit_floats * 4
    }

    /// average strategy for an info set (uniform if never visited)
    pub fn avg_strategy(&self, node: u32, hand_key: u32, na: usize) -> Vec<f64> {
        let mut out = vec![1.0 / na as f64; na];
        if let Some(off) = self.table.get(Self::infoset_key(node, hand_key), na, false) {
            self.table.average_strategy(off, na, &mut out);
        }
        out
    }

    /// Monte Carlo estimate of every player's EV under the average strategy.
    /// Returns (mean, standard error) per player.
    pub fn estimate_ev(&self, samples: usize, seed: u64) -> Vec<(f64, f64)> {
        let n = self.spot.tree.cfg.num_players;
        let mut rng = Rng::new(seed);
        let mut cache = FxMap::default();
        let mut sum = vec![0f64; n];
        let mut sq = vec![0f64; n];
        for _ in 0..samples {
            let deal = self.sample_deal(&mut rng);
            let mut node = 0u32;
            loop {
                let nd = &self.spot.tree.nodes[node as usize];
                match &nd.kind {
                    NodeKind::Fold | NodeKind::Showdown => {
                        let u = self.utilities(node as usize, &deal, &mut cache);
                        for p in 0..n {
                            sum[p] += u[p];
                            sq[p] += u[p] * u[p];
                        }
                        break;
                    }
                    NodeKind::Chance { child, .. } => node = *child,
                    NodeKind::Action { player, actions, children } => {
                        let s = self.avg_strategy(node, deal.key[*player][nd.street as usize], actions.len());
                        let mut x = rng.unit();
                        let mut pick = actions.len() - 1;
                        for (a, pa) in s.iter().enumerate() {
                            x -= pa;
                            if x < 0.0 {
                                pick = a;
                                break;
                            }
                        }
                        node = children[pick];
                    }
                }
            }
        }
        (0..n)
            .map(|p| {
                let m = sum[p] / samples as f64;
                let var = (sq[p] / samples as f64 - m * m).max(0.0);
                (m, (var / samples as f64).sqrt())
            })
            .collect()
    }

    /// Strategy report for the node reached by `path` (action indices from the
    /// root, staying on the starting street). Hands are weighted by how often
    /// the acting player's own earlier actions on the path reach this node.
    pub fn node_report(&self, path: &[usize]) -> Result<NodeReport, String> {
        let tree = &self.spot.tree;
        let mut node = 0u32;
        // (node, player, action index) for each step
        let mut steps = Vec::new();
        for &a in path {
            match &tree.nodes[node as usize].kind {
                NodeKind::Action { player, children, .. } => {
                    if a >= children.len() {
                        return Err(format!("action {a} not available"));
                    }
                    steps.push((node, *player, a));
                    node = children[a];
                }
                _ => return Err("path leaves the starting street".into()),
            }
        }
        let nd = &tree.nodes[node as usize];
        let (player, actions) = match &nd.kind {
            NodeKind::Action { player, actions, .. } => (*player, actions.clone()),
            _ => return Err("path ends at a terminal or street change".into()),
        };
        let na = actions.len();
        let pr = &self.pranges[player];
        let mut combos = Vec::new();
        for &c in &pr.combos {
            let mut reach = self.spot.ranges[player].weights[c] as f64;
            for &(sn, sp, sa) in &steps {
                if sp == player {
                    if let NodeKind::Action { actions, .. } = &tree.nodes[sn as usize].kind {
                        reach *= self.avg_strategy(sn, c as u32, actions.len())[sa];
                    }
                }
            }
            let strat = self.avg_strategy(node, c as u32, na);
            combos.push(ComboStrategy { combo: c, reach, strategy: strat });
        }
        let total: f64 = combos.iter().map(|c| c.reach).sum();
        let mut overall = vec![0f64; na];
        if total > 0.0 {
            for c in &combos {
                for a in 0..na {
                    overall[a] += c.reach * c.strategy[a] / total;
                }
            }
        }
        Ok(NodeReport {
            player,
            actions: actions.iter().map(|a| a.label()).collect(),
            overall,
            combos,
            pot: nd.pot,
        })
    }
}

pub struct ComboStrategy {
    pub combo: usize,
    pub reach: f64,
    pub strategy: Vec<f64>,
}

pub struct NodeReport {
    pub player: usize,
    pub actions: Vec<String>,
    pub overall: Vec<f64>,
    pub combos: Vec<ComboStrategy>,
    pub pot: f64,
}
