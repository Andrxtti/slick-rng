//! Cards, hand evaluation and range parsing.
//!
//! Card encoding: `rank * 4 + suit`, rank 0 = deuce .. 12 = ace, suit 0..3 = c,d,h,s.

pub type Card = u8;
pub const NO_CARD: Card = 255;

const RANKS: &[u8] = b"23456789TJQKA";
const SUITS: &[u8] = b"cdhs";

#[inline]
pub fn rank(c: Card) -> u8 {
    c >> 2
}
#[inline]
pub fn suit(c: Card) -> u8 {
    c & 3
}

pub fn parse_rank(ch: u8) -> Option<u8> {
    RANKS.iter().position(|&r| r == ch.to_ascii_uppercase()).map(|p| p as u8)
}

pub fn parse_card(s: &str) -> Result<Card, String> {
    let b = s.trim().as_bytes();
    if b.len() != 2 {
        return Err(format!("bad card '{s}'"));
    }
    let r = parse_rank(b[0]).ok_or_else(|| format!("bad rank in '{s}'"))?;
    let su = SUITS
        .iter()
        .position(|&x| x == b[1].to_ascii_lowercase())
        .ok_or_else(|| format!("bad suit in '{s}'"))? as u8;
    Ok(r * 4 + su)
}

/// Parse a run of cards like "Td9d6h" or "Td 9d 6h".
pub fn parse_cards(s: &str) -> Result<Vec<Card>, String> {
    let clean: String = s.chars().filter(|c| !c.is_whitespace() && *c != ',').collect();
    if clean.len() % 2 != 0 {
        return Err(format!("bad card list '{s}'"));
    }
    let mut out = Vec::new();
    for i in (0..clean.len()).step_by(2) {
        let c = parse_card(&clean[i..i + 2])?;
        if out.contains(&c) {
            return Err(format!("duplicate card in '{s}'"));
        }
        out.push(c);
    }
    Ok(out)
}

pub fn card_str(c: Card) -> String {
    format!("{}{}", RANKS[rank(c) as usize] as char, SUITS[suit(c) as usize] as char)
}

// ---------------------------------------------------------------------------
// Combos: the 1326 two-card hands, indexed densely.
// ---------------------------------------------------------------------------

pub const NUM_COMBOS: usize = 1326;

/// combo index -> (high card, low card)
pub fn combo_cards() -> &'static [(Card, Card); NUM_COMBOS] {
    use std::sync::OnceLock;
    static T: OnceLock<[(Card, Card); NUM_COMBOS]> = OnceLock::new();
    T.get_or_init(|| {
        let mut t = [(0u8, 0u8); NUM_COMBOS];
        let mut i = 0;
        for a in 0..52u8 {
            for b in 0..a {
                t[i] = (a, b);
                i += 1;
            }
        }
        t
    })
}

pub fn combo_index(a: Card, b: Card) -> usize {
    let (hi, lo) = if a > b { (a, b) } else { (b, a) };
    (hi as usize) * (hi as usize - 1) / 2 + lo as usize
}

pub fn combo_str(i: usize) -> String {
    let (a, b) = combo_cards()[i];
    format!("{}{}", card_str(a), card_str(b))
}

/// The 169-class label ("AKs", "QQ", "T9o") for a combo.
pub fn combo_class(i: usize) -> String {
    let (a, b) = combo_cards()[i];
    let (ra, rb) = (rank(a).max(rank(b)), rank(a).min(rank(b)));
    let ca = RANKS[ra as usize] as char;
    let cb = RANKS[rb as usize] as char;
    if ra == rb {
        format!("{ca}{cb}")
    } else if suit(a) == suit(b) {
        format!("{ca}{cb}s")
    } else {
        format!("{ca}{cb}o")
    }
}

// ---------------------------------------------------------------------------
// 7-card evaluator. Returns a strength where higher is better.
// Layout: category << 20 | five 4-bit kickers.
// ---------------------------------------------------------------------------

#[inline]
fn top_bits(mask: u32, n: usize) -> u32 {
    // pack the n highest set bits' ranks into 4-bit kicker fields
    let mut out = 0u32;
    let mut m = mask;
    let mut k = 0;
    while k < n && m != 0 {
        let r = 31 - m.leading_zeros();
        out = (out << 4) | r;
        m &= !(1 << r);
        k += 1;
    }
    out << (4 * (5 - n) as u32)
}

#[inline]
fn straight_high(mask: u32) -> Option<u32> {
    // add the wheel ace as rank -1
    let m = (mask << 1) | ((mask >> 12) & 1);
    let s = m & (m >> 1) & (m >> 2) & (m >> 3) & (m >> 4);
    if s == 0 {
        None
    } else {
        // bit position in shifted mask; highest card of the straight = pos + 4 - 1
        let pos = 31 - s.leading_zeros();
        Some(pos + 3)
    }
}

pub fn evaluate(cards: &[Card]) -> u32 {
    let mut suit_mask = [0u32; 4];
    let mut counts = [0u8; 13];
    let mut all = 0u32;
    for &c in cards {
        let r = rank(c) as usize;
        suit_mask[suit(c) as usize] |= 1 << r;
        counts[r] += 1;
        all |= 1 << r;
    }
    // flush / straight flush
    for &sm in &suit_mask {
        if sm.count_ones() >= 5 {
            if let Some(h) = straight_high(sm) {
                return (8 << 20) | (h << 16);
            }
            return (5 << 20) | top_bits(sm, 5);
        }
    }
    let mut quads = 0u32;
    let mut trips = 0u32;
    let mut pairs = 0u32;
    for r in 0..13 {
        match counts[r] {
            4 => quads |= 1 << r,
            3 => trips |= 1 << r,
            2 => pairs |= 1 << r,
            _ => {}
        }
    }
    if quads != 0 {
        let q = 31 - quads.leading_zeros();
        let kick = top_bits(all & !(1 << q), 1) >> 12;
        return (7 << 20) | (q << 16) | kick;
    }
    if trips != 0 {
        let t = 31 - trips.leading_zeros();
        let rest_pairs = (pairs | trips) & !(1 << t);
        if rest_pairs != 0 {
            let p = 31 - rest_pairs.leading_zeros();
            return (6 << 20) | (t << 16) | (p << 12);
        }
    }
    if let Some(h) = straight_high(all) {
        return (4 << 20) | (h << 16);
    }
    if trips != 0 {
        let t = 31 - trips.leading_zeros();
        let k = top_bits(all & !(1 << t), 2) >> 8;
        return (3 << 20) | (t << 16) | k;
    }
    if pairs.count_ones() >= 2 {
        let p1 = 31 - pairs.leading_zeros();
        let p2 = 31 - (pairs & !(1 << p1)).leading_zeros();
        let k = top_bits(all & !(1 << p1) & !(1 << p2), 1) >> 8;
        return (2 << 20) | (p1 << 16) | (p2 << 12) | k;
    }
    if pairs != 0 {
        let p = 31 - pairs.leading_zeros();
        let k = top_bits(all & !(1 << p), 3) >> 4;
        return (1 << 20) | (p << 16) | k;
    }
    top_bits(all, 5)
}

// ---------------------------------------------------------------------------
// Ranges: "AA,KK:0.5,AKs,A5s-A2s,22+,T9s+,AhKh,KQo:0.25"
// ---------------------------------------------------------------------------

#[derive(Clone)]
pub struct Range {
    pub weights: Vec<f32>, // NUM_COMBOS
}

impl Range {
    pub fn parse(s: &str) -> Result<Range, String> {
        let mut w = vec![0f32; NUM_COMBOS];
        for raw in s.split(',') {
            let tok = raw.trim();
            if tok.is_empty() {
                continue;
            }
            let (hand, weight) = match tok.split_once(':') {
                Some((h, wt)) => (
                    h.trim(),
                    wt.trim().parse::<f32>().map_err(|_| format!("bad weight in '{tok}'"))?,
                ),
                None => (tok, 1.0),
            };
            if !(0.0..=1.0).contains(&weight) {
                return Err(format!("weight out of range in '{tok}'"));
            }
            for c in expand_token(hand)? {
                w[c] = weight;
            }
        }
        Ok(Range { weights: w })
    }

    pub fn total(&self) -> f32 {
        self.weights.iter().sum()
    }
}

fn class_combos(hi: u8, lo: u8, kind: char) -> Vec<usize> {
    let mut v = Vec::new();
    for s1 in 0..4u8 {
        for s2 in 0..4u8 {
            let a = hi * 4 + s1;
            let b = lo * 4 + s2;
            if a == b {
                continue;
            }
            let ok = match kind {
                'p' => hi == lo && s1 < s2,
                's' => hi != lo && s1 == s2,
                'o' => hi != lo && s1 != s2,
                'a' => hi != lo || s1 < s2,
                _ => false,
            };
            if ok {
                v.push(combo_index(a, b));
            }
        }
    }
    v
}

/// Parse a class token like "AKs", "QQ", "T9", returning (hi, lo, kind).
fn parse_class(t: &str) -> Result<(u8, u8, char), String> {
    let b = t.as_bytes();
    if b.len() < 2 || b.len() > 3 {
        return Err(format!("bad hand '{t}'"));
    }
    let r1 = parse_rank(b[0]).ok_or_else(|| format!("bad hand '{t}'"))?;
    let r2 = parse_rank(b[1]).ok_or_else(|| format!("bad hand '{t}'"))?;
    let (hi, lo) = (r1.max(r2), r1.min(r2));
    let kind = if hi == lo {
        if b.len() == 3 {
            return Err(format!("bad pair '{t}'"));
        }
        'p'
    } else if b.len() == 3 {
        match b[2].to_ascii_lowercase() {
            b's' => 's',
            b'o' => 'o',
            _ => return Err(format!("bad hand '{t}'")),
        }
    } else {
        'a'
    };
    Ok((hi, lo, kind))
}

fn expand_token(t: &str) -> Result<Vec<usize>, String> {
    // explicit combo "AhKh"
    if t.len() == 4 {
        if let (Ok(a), Ok(b)) = (parse_card(&t[0..2]), parse_card(&t[2..4])) {
            if a == b {
                return Err(format!("bad combo '{t}'"));
            }
            return Ok(vec![combo_index(a, b)]);
        }
    }
    let mut out = Vec::new();
    if let Some(base) = t.strip_suffix('+') {
        let (hi, lo, kind) = parse_class(base)?;
        if kind == 'p' {
            for r in hi..13 {
                out.extend(class_combos(r, r, 'p'));
            }
        } else {
            for r in lo..hi {
                out.extend(class_combos(hi, r, kind));
            }
        }
    } else if let Some((a, b)) = t.split_once('-') {
        let (h1, l1, k1) = parse_class(a)?;
        let (h2, l2, k2) = parse_class(b)?;
        if k1 != k2 {
            return Err(format!("bad span '{t}'"));
        }
        if k1 == 'p' {
            for r in h1.min(h2)..=h1.max(h2) {
                out.extend(class_combos(r, r, 'p'));
            }
        } else {
            if h1 != h2 {
                return Err(format!("span must keep top card: '{t}'"));
            }
            for r in l1.min(l2)..=l1.max(l2) {
                out.extend(class_combos(h1, r, k1));
            }
        }
    } else {
        let (hi, lo, kind) = parse_class(t)?;
        out.extend(class_combos(hi, lo, kind));
    }
    Ok(out)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn ev(s: &str) -> u32 {
        evaluate(&parse_cards(s).unwrap())
    }

    #[test]
    fn hand_order() {
        let order = [
            "2c3d5h7s9cJdKh", // high card
            "2c2d5h7s9cJdKh", // pair
            "2c2d5h5s9cJdKh", // two pair
            "2c2d2h7s9cJdKh", // trips
            "Ac2d3h4s5cJdKh", // wheel
            "6c2d3h4s5cJdKh", // 6-high straight
            "2c4c6c8cTcJdKh", // flush
            "2c2d2h5s5cJdKh", // full house
            "2c2d2h2s9cJdKh", // quads
            "Ac2c3c4c5cJdKh", // steel wheel
            "TcJcQcKcAc2d3h", // royal
        ];
        for w in order.windows(2) {
            assert!(ev(w[0]) < ev(w[1]), "{} !< {}", w[0], w[1]);
        }
        // kickers
        assert!(ev("AcAdKh7s5c3d2h") > ev("AcAdQh7s5c3d2h"));
        assert!(ev("AcAdKhKs5c3d2h") < ev("AcAdKhKsQc3d2h"));
        assert_eq!(ev("AcAdKhKs5c3d2h"), ev("AhAsKcKd5s3c2d"));
        // two trips -> full house uses higher trips
        assert!(ev("KcKdKh2s2c2dAh") > ev("QcQdQhAsAc2d3h"));
        // three pairs: best two + kicker
        assert_eq!(ev("AcAdKhKsQcQd2h"), ev("AcAdKhKsQc3d2h"));
    }

    #[test]
    fn ranges() {
        assert_eq!(Range::parse("AA").unwrap().total(), 6.0);
        assert_eq!(Range::parse("AKs").unwrap().total(), 4.0);
        assert_eq!(Range::parse("AKo").unwrap().total(), 12.0);
        assert_eq!(Range::parse("AK").unwrap().total(), 16.0);
        assert_eq!(Range::parse("22+").unwrap().total(), 78.0);
        assert_eq!(Range::parse("A2s+").unwrap().total(), 48.0);
        assert_eq!(Range::parse("A5s-A2s").unwrap().total(), 16.0);
        assert_eq!(Range::parse("QQ-TT:0.5").unwrap().total(), 9.0);
        assert_eq!(Range::parse("AhKh").unwrap().total(), 1.0);
        let all: usize = (0..NUM_COMBOS).map(|i| combo_class(i).len()).count();
        assert_eq!(all, 1326);
    }
}
