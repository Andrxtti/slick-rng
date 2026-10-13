//! Spot files: JSON description of a hand to solve.
//!
//! ```json
//! {
//!   "players": [
//!     {"name": "BB",  "stack": 97.5, "range": "22+,A2s+,..."},
//!     {"name": "CO",  "stack": 97.5, "range": "..."},
//!     {"name": "BTN", "stack": 97.5, "range": "..."}
//!   ],
//!   "pot": 8.5,
//!   "board": "Td9d6h",
//!   "sizes": {
//!     "flop":  {"bet": "33%,75%", "raise": "3x"},
//!     "turn":  {"bet": "50%,100%", "raise": "3x"},
//!     "river": {"bet": "50%,100%,a", "raise": "3x"}
//!   },
//!   "raise_cap": 2,
//!   "allin_threshold": 0.15,
//!   "merge_threshold": 0.1,
//!   "abstraction": {"turn_buckets": 30, "river_buckets": 100},
//!   "payoff": {"type": "icm", "payouts": [50,30,20], "others": [40, 25]},
//!   "solve": {"iterations": 2000000, "seconds": 120, "threads": 0, "memory_gb": 8}
//! }
//! ```
//! Players are listed in postflop action order (first to act first).

use crate::cards::*;
use crate::icm::Payoff;
use crate::json::Json;
use crate::solver::Spot;
use crate::tree::{Size, StreetSizes, Tree, TreeConfig};

pub struct SolveSettings {
    pub iterations: u64,
    pub seconds: f64,
    pub threads: usize,
    pub memory_bytes: u64,
}

fn sizes(j: Option<&Json>, default_bet: &str, default_raise: &str) -> Result<StreetSizes, String> {
    let bet = j.and_then(|j| j.get("bet")).and_then(|v| v.str()).unwrap_or(default_bet);
    let raise = j.and_then(|j| j.get("raise")).and_then(|v| v.str()).unwrap_or(default_raise);
    Ok(StreetSizes { bets: Size::parse_list(bet, false)?, raises: Size::parse_list(raise, true)? })
}

pub fn parse_spot(text: &str) -> Result<(Spot, SolveSettings), String> {
    let j = Json::parse(text)?;
    let players = j.get("players").and_then(|v| v.arr()).ok_or("missing 'players'")?;
    let mut names = Vec::new();
    let mut stacks = Vec::new();
    let mut ranges = Vec::new();
    for (i, p) in players.iter().enumerate() {
        names.push(p.get("name").and_then(|v| v.str()).map(String::from).unwrap_or(format!("P{}", i + 1)));
        stacks.push(p.get("stack").and_then(|v| v.num()).ok_or(format!("player {} missing 'stack'", i + 1))?);
        let r = p.get("range").and_then(|v| v.str()).ok_or(format!("player {} missing 'range'", i + 1))?;
        ranges.push(Range::parse(r).map_err(|e| format!("{}: {e}", names[i]))?);
    }
    let board = parse_cards(j.get("board").and_then(|v| v.str()).ok_or("missing 'board'")?)?;
    if !(3..=5).contains(&board.len()) {
        return Err("board must have 3-5 cards".into());
    }
    let start_street = (board.len() - 3) as u8;
    let pot = j.get("pot").and_then(|v| v.num()).ok_or("missing 'pot'")?;
    let sz = j.get("sizes");
    let cfg = TreeConfig {
        num_players: names.len(),
        stacks,
        pot,
        start_street,
        sizes: [
            sizes(sz.and_then(|s| s.get("flop")), "33%,75%", "3x")?,
            sizes(sz.and_then(|s| s.get("turn")), "50%,100%", "3x")?,
            sizes(sz.and_then(|s| s.get("river")), "50%,100%", "3x")?,
        ],
        raise_cap: j.get("raise_cap").and_then(|v| v.num()).unwrap_or(2.0) as u8,
        allin_threshold: j.get("allin_threshold").and_then(|v| v.num()).unwrap_or(0.15),
        merge_threshold: j.get("merge_threshold").and_then(|v| v.num()).unwrap_or(0.1),
    };
    let tree = Tree::build(cfg)?;

    let payoff = match j.get("payoff") {
        None => Payoff::Chips,
        Some(p) => match p.get("type").and_then(|v| v.str()).unwrap_or("chips") {
            "chips" => Payoff::Chips,
            "icm" => {
                let payouts: Vec<f64> = p
                    .get("payouts")
                    .and_then(|v| v.arr())
                    .ok_or("icm payoff needs 'payouts'")?
                    .iter()
                    .filter_map(|x| x.num())
                    .collect();
                let others: Vec<f64> = p
                    .get("others")
                    .and_then(|v| v.arr())
                    .map(|a| a.iter().filter_map(|x| x.num()).collect())
                    .unwrap_or_default();
                if names.len() + others.len() > 20 {
                    return Err("ICM supports up to 20 remaining players".into());
                }
                Payoff::Icm { payouts, others }
            }
            t => return Err(format!("unknown payoff type '{t}'")),
        },
    };

    // default abstraction: exact for river/turn starts, buckets for flop multiway
    let abs = j.get("abstraction");
    let default_tb = if start_street == 0 && names.len() > 2 { 30 } else { 0 };
    let default_rb = if start_street == 0 && names.len() > 2 { 100 } else { 0 };
    let turn_buckets = abs.and_then(|a| a.get("turn_buckets")).and_then(|v| v.num()).map(|x| x as usize).unwrap_or(default_tb);
    let river_buckets = abs.and_then(|a| a.get("river_buckets")).and_then(|v| v.num()).map(|x| x as usize).unwrap_or(default_rb);

    let s = j.get("solve");
    let settings = SolveSettings {
        iterations: s.and_then(|s| s.get("iterations")).and_then(|v| v.num()).unwrap_or(1e7) as u64,
        seconds: s.and_then(|s| s.get("seconds")).and_then(|v| v.num()).unwrap_or(60.0),
        threads: {
            let t = s.and_then(|s| s.get("threads")).and_then(|v| v.num()).unwrap_or(0.0) as usize;
            if t == 0 {
                std::thread::available_parallelism().map(|x| x.get()).unwrap_or(4)
            } else {
                t
            }
        },
        memory_bytes: (s.and_then(|s| s.get("memory_gb")).and_then(|v| v.num()).unwrap_or(6.0) * 1e9) as u64,
    };

    Ok((Spot { names, tree, board, ranges, payoff, turn_buckets, river_buckets }, settings))
}
