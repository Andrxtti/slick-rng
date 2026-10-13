//! Concurrent storage for regrets and average strategies.
//!
//! Info sets are allocated lazily the first time a traversal reaches them.
//! Values live in a segmented arena of relaxed atomics so many threads can
//! update the same tables at once (lock-free "Hogwild" style updates).

use std::collections::HashMap;
use std::hash::{BuildHasherDefault, Hasher};
use std::sync::atomic::{AtomicU32, AtomicU64, Ordering::Relaxed};
use std::sync::{Mutex, OnceLock};

#[derive(Default)]
pub struct FxHasher(u64);
impl Hasher for FxHasher {
    #[inline]
    fn write(&mut self, bytes: &[u8]) {
        for &b in bytes {
            self.write_u64(b as u64);
        }
    }
    #[inline]
    fn write_u64(&mut self, x: u64) {
        self.0 = (self.0.rotate_left(5) ^ x).wrapping_mul(0x51_7c_c1_b7_27_22_0a_95);
    }
    #[inline]
    fn finish(&self) -> u64 {
        self.0
    }
}
pub type FxBuild = BuildHasherDefault<FxHasher>;
pub type FxMap<K, V> = HashMap<K, V, FxBuild>;

const SEG_BITS: u32 = 22;
const SEG: u64 = 1 << SEG_BITS;
const MAX_SEGS: usize = 1 << 14; // up to 256 GB of addressable floats
const SHARDS: usize = 256;

pub struct Table {
    shards: Vec<Mutex<FxMap<u64, u64>>>,
    segs: Vec<OnceLock<Box<[AtomicU32]>>>,
    next: AtomicU64,
    pub limit_floats: u64,
}

impl Table {
    pub fn new(limit_bytes: u64) -> Table {
        Table {
            shards: (0..SHARDS).map(|_| Mutex::new(FxMap::default())).collect(),
            segs: (0..MAX_SEGS).map(|_| OnceLock::new()).collect(),
            next: AtomicU64::new(0),
            limit_floats: limit_bytes / 4,
        }
    }

    pub fn bytes_used(&self) -> u64 {
        self.next.load(Relaxed) * 4
    }

    pub fn infosets(&self) -> usize {
        self.shards.iter().map(|s| s.lock().unwrap().len()).sum()
    }

    fn alloc(&self, len: u64) -> Option<u64> {
        loop {
            let cur = self.next.load(Relaxed);
            let off = cur & (SEG - 1);
            let start = if off + len > SEG { (cur | (SEG - 1)) + 1 } else { cur };
            if start + len > self.limit_floats {
                return None;
            }
            if self
                .next
                .compare_exchange(cur, start + len, Relaxed, Relaxed)
                .is_ok()
            {
                let seg = (start >> SEG_BITS) as usize;
                self.segs[seg].get_or_init(|| (0..SEG).map(|_| AtomicU32::new(0)).collect());
                return Some(start);
            }
        }
    }

    /// Find (or create) the slot for an info set with `n` actions.
    /// Layout: n regrets followed by n strategy sums.
    #[inline]
    pub fn get(&self, key: u64, n: usize, create: bool) -> Option<u64> {
        let h = key.wrapping_mul(0x9E37_79B9_7F4A_7C15) >> 56;
        let mut shard = self.shards[h as usize].lock().unwrap();
        if let Some(&o) = shard.get(&key) {
            return Some(o);
        }
        if !create {
            return None;
        }
        let o = self.alloc(2 * n as u64)?;
        shard.insert(key, o);
        Some(o)
    }

    #[inline]
    fn cell(&self, i: u64) -> &AtomicU32 {
        let seg = self.segs[(i >> SEG_BITS) as usize].get().unwrap();
        &seg[(i & (SEG - 1)) as usize]
    }

    #[inline]
    pub fn load(&self, i: u64) -> f32 {
        f32::from_bits(self.cell(i).load(Relaxed))
    }

    #[inline]
    pub fn store(&self, i: u64, v: f32) {
        self.cell(i).store(v.to_bits(), Relaxed)
    }

    #[inline]
    pub fn add(&self, i: u64, v: f32) {
        let c = self.cell(i);
        c.store((f32::from_bits(c.load(Relaxed)) + v).to_bits(), Relaxed)
    }

    /// current regret-matching strategy into `out`
    #[inline]
    pub fn current_strategy(&self, off: u64, n: usize, out: &mut [f64]) {
        let mut sum = 0.0;
        for a in 0..n {
            let r = self.load(off + a as u64).max(0.0) as f64;
            out[a] = r;
            sum += r;
        }
        if sum > 0.0 {
            for x in out.iter_mut().take(n) {
                *x /= sum;
            }
        } else {
            for x in out.iter_mut().take(n) {
                *x = 1.0 / n as f64;
            }
        }
    }

    /// average strategy into `out`; falls back to uniform when never updated
    pub fn average_strategy(&self, off: u64, n: usize, out: &mut [f64]) {
        let mut sum = 0.0;
        for a in 0..n {
            let s = self.load(off + (n + a) as u64) as f64;
            out[a] = s;
            sum += s;
        }
        if sum > 0.0 {
            for x in out.iter_mut().take(n) {
                *x /= sum;
            }
        } else {
            self.current_strategy(off, n, out);
        }
    }
}
