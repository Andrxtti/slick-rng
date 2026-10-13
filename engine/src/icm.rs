//! Tournament payoff models: chip EV, ICM (Malmuth-Harville) and PKO bounties.

/// Exact Malmuth-Harville ICM over all tournament players.
/// `stacks` – chips of every remaining player (0 = busted in this hand).
/// `start` – chips at the start of the hand, used to order players who bust
/// in the same hand (bigger starting stack finishes higher).
/// Returns each player's $ equity.
pub fn icm_equity(stacks: &[f64], start: &[f64], payouts: &[f64]) -> Vec<f64> {
    let n = stacks.len();
    assert!(n <= 20, "exact ICM supports up to 20 players");
    let mut eq = vec![0.0; n];
    let alive: Vec<usize> = (0..n).filter(|&i| stacks[i] > 1e-9).collect();
    let a = alive.len();

    // busted players take the places after all alive players
    let mut busted: Vec<usize> = (0..n).filter(|&i| stacks[i] <= 1e-9).collect();
    busted.sort_by(|&x, &y| start[y].partial_cmp(&start[x]).unwrap());
    for (k, &i) in busted.iter().enumerate() {
        let place = a + k;
        if place < payouts.len() {
            eq[i] = payouts[place];
        }
    }
    if a == 0 {
        return eq;
    }
    let paid = payouts.len().min(a);
    if paid == 0 {
        return eq;
    }
    let total: f64 = alive.iter().map(|&i| stacks[i]).sum();
    // P(set S of alive players occupies exactly the first |S| places)
    let size = 1usize << a;
    let mut prob = vec![0.0f64; size];
    let mut sum_s = vec![0.0f64; size];
    for m in 1..size {
        let low = m.trailing_zeros() as usize;
        sum_s[m] = sum_s[m & (m - 1)] + stacks[alive[low]];
    }
    prob[0] = 1.0;
    // iterate subsets in order of popcount (increasing mask works since
    // supersets are always numerically larger)
    for m in 0..size {
        let p = prob[m];
        if p == 0.0 {
            continue;
        }
        let k = m.count_ones() as usize;
        if k >= paid {
            continue;
        }
        let rem = total - sum_s[m];
        if rem <= 0.0 {
            continue;
        }
        for j in 0..a {
            if m & (1 << j) != 0 {
                continue;
            }
            let pj = p * stacks[alive[j]] / rem;
            eq[alive[j]] += pj * payouts[k];
            prob[m | (1 << j)] += pj;
        }
    }
    eq
}

#[derive(Clone, Debug)]
pub enum Payoff {
    /// plain chips
    Chips,
    /// ICM $ equity. `others` are the stacks of tournament players not in this hand.
    Icm { payouts: Vec<f64>, others: Vec<f64> },
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn icm_basics() {
        // equal stacks -> equal equity
        let e = icm_equity(&[100.0, 100.0, 100.0], &[100.0; 3], &[50.0, 30.0, 20.0]);
        for x in &e {
            assert!((x - 100.0 / 3.0).abs() < 1e-9);
        }
        // winner-take-all == chip proportion
        let e = icm_equity(&[300.0, 100.0], &[0.0; 2], &[1.0]);
        assert!((e[0] - 0.75).abs() < 1e-12);
        // total equity == total prize pool
        let p = [50.0, 30.0, 20.0];
        let e = icm_equity(&[500.0, 300.0, 150.0, 50.0], &[0.0; 4], &p);
        assert!((e.iter().sum::<f64>() - 100.0).abs() < 1e-9);
        // known value: stacks 50/30/20, payouts 50/30/20
        let e = icm_equity(&[50.0, 30.0, 20.0], &[0.0; 3], &[50.0, 30.0, 20.0]);
        // P1 first = .5; second = .3*(50/70) + .2*(50/80) = .214286+.125 = .339286
        let p1 = 0.5 * 50.0 + 0.339285714 * 30.0 + (1.0 - 0.5 - 0.339285714) * 20.0;
        assert!((e[0] - p1).abs() < 1e-6, "{} vs {}", e[0], p1);
        // bust ordering: two bust in same hand, bigger starter gets 2nd
        let e = icm_equity(&[1000.0, 0.0, 0.0], &[400.0, 300.0, 300.5], &[50.0, 30.0, 20.0]);
        assert_eq!(e, vec![50.0, 20.0, 30.0]);
    }
}
