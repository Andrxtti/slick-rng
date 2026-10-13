//! Hand-strength bucketing for streets after the first.
//!
//! On the starting street every combo is its own info set (exact).
//! On later streets hands can be grouped into buckets so that large
//! multiway trees fit in memory:
//!   * river: hand-strength percentile vs. a random hand
//!   * turn: expected river strength (E[HS]) x volatility (draws vs. made hands)

use crate::cards::*;
use crate::table::FxMap;

pub struct Abstraction {
    pub turn_buckets: usize,  // E[HS] levels; total turn buckets = this * VOL_LEVELS
    pub river_buckets: usize,
    river: FxMap<u32, Vec<u16>>, // key: sorted 5-card board bits -> bucket per combo
    turn: FxMap<u32, Vec<u16>>,  // key: turn card
}

pub const VOL_LEVELS: usize = 4;
const VOL_CUTS: [f64; 3] = [0.06, 0.13, 0.21];

fn board_key(extra: &[Card]) -> u32 {
    // the starting board is fixed, so only the extra cards identify a runout
    let mut k = 0u32;
    let mut v: Vec<u32> = extra.iter().map(|&c| c as u32).collect();
    v.sort();
    for c in v {
        k = k * 53 + c + 1;
    }
    k
}

/// percentile strength (0..1) of every combo on a complete 5-card board
fn river_percentiles(board: &[Card]) -> Vec<f32> {
    let combos = combo_cards();
    let mut strengths: Vec<(u32, usize)> = Vec::with_capacity(1081);
    let mut cards = [0u8; 7];
    cards[..5].copy_from_slice(board);
    for (i, &(a, b)) in combos.iter().enumerate() {
        if board.contains(&a) || board.contains(&b) {
            continue;
        }
        cards[5] = a;
        cards[6] = b;
        strengths.push((evaluate(&cards), i));
    }
    strengths.sort_unstable();
    let n = strengths.len() as f32;
    let mut out = vec![-1.0f32; NUM_COMBOS];
    let mut i = 0;
    while i < strengths.len() {
        let mut j = i;
        while j < strengths.len() && strengths[j].0 == strengths[i].0 {
            j += 1;
        }
        let pct = (i as f32 + (j - i) as f32 * 0.5) / n;
        for k in i..j {
            out[strengths[k].1] = pct;
        }
        i = j;
    }
    out
}

impl Abstraction {
    /// `board` is the starting board (3 or 4 cards).
    pub fn build(board: &[Card], turn_buckets: usize, river_buckets: usize) -> Abstraction {
        let mut abs = Abstraction {
            turn_buckets,
            river_buckets,
            river: FxMap::default(),
            turn: FxMap::default(),
        };
        let deck: Vec<Card> = (0..52).filter(|c| !board.contains(c)).collect();
        let need_river = river_buckets > 0 || (turn_buckets > 0 && board.len() == 3);
        if board.len() == 3 {
            // all turn+river runouts
            let mut river_pct: FxMap<u32, Vec<f32>> = FxMap::default();
            if need_river {
                for (i, &t) in deck.iter().enumerate() {
                    for &r in &deck[i + 1..] {
                        let full = [board[0], board[1], board[2], t, r];
                        river_pct.insert(board_key(&[t, r]), river_percentiles(&full));
                    }
                }
            }
            if river_buckets > 0 {
                for (k, pct) in &river_pct {
                    abs.river.insert(*k, quantize(pct, river_buckets));
                }
            }
            if turn_buckets > 0 {
                for &t in &deck {
                    let mut sum = vec![0f64; NUM_COMBOS];
                    let mut sq = vec![0f64; NUM_COMBOS];
                    let mut cnt = vec![0u32; NUM_COMBOS];
                    for &r in &deck {
                        if r == t {
                            continue;
                        }
                        let pct = &river_pct[&board_key(&[t, r])];
                        for c in 0..NUM_COMBOS {
                            let p = pct[c];
                            if p >= 0.0 {
                                sum[c] += p as f64;
                                sq[c] += (p * p) as f64;
                                cnt[c] += 1;
                            }
                        }
                    }
                    let mut b = vec![u16::MAX; NUM_COMBOS];
                    for c in 0..NUM_COMBOS {
                        if cnt[c] == 0 {
                            continue;
                        }
                        let (a, bb) = combo_cards()[c];
                        if a == t || bb == t {
                            continue;
                        }
                        let m = sum[c] / cnt[c] as f64;
                        let var = (sq[c] / cnt[c] as f64 - m * m).max(0.0);
                        let sd = var.sqrt();
                        let h = ((m * turn_buckets as f64) as usize).min(turn_buckets - 1);
                        let v = VOL_CUTS.iter().filter(|&&cut| sd > cut).count();
                        b[c] = (h * VOL_LEVELS + v) as u16;
                    }
                    abs.turn.insert(t as u32, b);
                }
            }
        } else if board.len() == 4 && river_buckets > 0 {
            for &r in &deck {
                let full = [board[0], board[1], board[2], board[3], r];
                abs.river.insert(board_key(&[r]), quantize(&river_percentiles(&full), river_buckets));
            }
        }
        abs
    }

    #[inline]
    pub fn river_bucket(&self, extra: &[Card], combo: usize) -> u32 {
        self.river[&board_key(extra)][combo] as u32
    }

    #[inline]
    pub fn turn_bucket(&self, turn: Card, combo: usize) -> u32 {
        self.turn[&(turn as u32)][combo] as u32
    }
}

fn quantize(pct: &[f32], buckets: usize) -> Vec<u16> {
    pct.iter()
        .map(|&p| {
            if p < 0.0 {
                u16::MAX
            } else {
                ((p * buckets as f32) as usize).min(buckets - 1) as u16
            }
        })
        .collect()
}
