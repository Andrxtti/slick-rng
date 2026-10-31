// Heads-up postflop vector CFR core (compiled to WebAssembly). Mirrors VecSolver in JS exactly:
// full traversal, exact chance enumeration, exact card removal at showdown, DCFR, alternating updates.
// The JS side builds the tree and writes it into this module's memory; see vcfr-glue in the engine.
typedef unsigned int u32; typedef int i32; typedef unsigned char u8;
extern unsigned char __heap_base;
static u32 heap_top = 0;
__attribute__((export_name("alloc"))) void* walloc(u32 bytes) {
  if (!heap_top) heap_top = (u32)&__heap_base;
  heap_top = (heap_top + 15) & ~15u;
  u32 p = heap_top; heap_top += bytes;
  u32 need = (heap_top + 65535) / 65536, have = __builtin_wasm_memory_size(0);
  if (need > have) { if (__builtin_wasm_memory_grow(0, need - have) == (u32)-1) return 0; }
  return (void*)p;
}
static void zero(void* p, u32 n) { u8* b = (u8*)p; for (u32 i = 0; i < n; i++) b[i] = 0; }
__attribute__((export_name("heap_used"))) u32 heap_used(void) { return heap_top; }

// ---- evaluator (same ranking as the engine's evaluate)
static inline int topBits(int mask, int n) { int out = 0, m = mask, k = 0; while (k < n && m) { int r = 31 - __builtin_clz(m); out = (out << 4) | r; m &= ~(1 << r); k++; } return out << (4 * (5 - n)); }
static inline int straightHigh(int mask) { int m = (mask << 1) | ((mask >> 12) & 1); int s = m & (m >> 1) & (m >> 2) & (m >> 3) & (m >> 4); return s ? (31 - __builtin_clz(s)) + 3 : -1; }
static int eval7(const u8* c) {
  int cnt[13] = {0}, sm[4] = {0}, all = 0;
  for (int i = 0; i < 7; i++) { int r = c[i] >> 2; sm[c[i] & 3] |= 1 << r; cnt[r]++; all |= 1 << r; }
  for (int s = 0; s < 4; s++) if (__builtin_popcount(sm[s]) >= 5) { int h = straightHigh(sm[s]); return h >= 0 ? (8 << 20) | (h << 16) : (5 << 20) | topBits(sm[s], 5); }
  int quads = 0, trips = 0, pairs = 0, np = 0;
  for (int r = 0; r < 13; r++) { if (cnt[r] == 4) quads |= 1 << r; else if (cnt[r] == 3) trips |= 1 << r; else if (cnt[r] == 2) { pairs |= 1 << r; np++; } }
  if (quads) { int q = 31 - __builtin_clz(quads); return (7 << 20) | (q << 16) | (topBits(all & ~(1 << q), 1) >> 12); }
  if (trips) { int t = 31 - __builtin_clz(trips), rp = (pairs | trips) & ~(1 << t); if (rp) return (6 << 20) | (t << 16) | ((31 - __builtin_clz(rp)) << 12); }
  int sh = straightHigh(all); if (sh >= 0) return (4 << 20) | (sh << 16);
  if (trips) { int t = 31 - __builtin_clz(trips); return (3 << 20) | (t << 16) | (topBits(all & ~(1 << t), 2) >> 8); }
  if (np >= 2) { int p1 = 31 - __builtin_clz(pairs), p2 = 31 - __builtin_clz(pairs & ~(1 << p1)); return (2 << 20) | (p1 << 16) | (p2 << 12) | (topBits(all & ~(1 << p1) & ~(1 << p2), 1) >> 8); }
  if (pairs) { int p = 31 - __builtin_clz(pairs); return (1 << 20) | (p << 16) | (topBits(all & ~(1 << p), 3) >> 4); }
  return topBits(all, 5);
}

// ---- problem data (written by JS)
#define MAXD 64
#define KA 0
#define KC 1
#define KF 2
#define KS 3
static i32 NN, NB0, START;            // nodes, starting board size, starting street
static i32 *kind, *player, *nact, *coff, *kids, *street; // node arrays; kids[coff[i] + a]
static float *uF, *uW, *uL, *uT;      // [2*node + p]
static i32 nh[2], NMAX, AMAX;
static u8 *ca[2], *cb[2]; static float* init[2]; static i32* same[2];
static i32 *bycard[2], *bycardOff[2]; // hands holding each card: bycard[p][bycardOff[p][c] .. bycardOff[p][c+1])
static u8 board[5];
static i32* slotTab;                  // per node: offsets table by board instance (lazily created), -1 = none
static float* store; static u32 storeUsed, storeCap; // regrets and strategy sums
static i32 *sdTab;                     // per 5-card board instance: offset into sd memory (-1 none)
static i32 iters; static float alpha = 1.5f, beta = 0.5f, gamma_ = 2.f;
static float posD, negD, wS;           // per-iteration discounts
// scratch per depth
static float *Dout[MAXD], *Dkids[MAXD], *Dsig[MAXD], *Dm[MAXD], *Do[MAXD];
static float cardS[52], wCard[52], tCard[52];
static float *RA[2], *RO[2], *RT[2];   // runout scratch (two levels: flop all-in)

__attribute__((export_name("setup"))) i32 setup(i32 nn, i32 nb0, i32 start, i32 n0, i32 n1, i32 amax) {
  NN = nn; NB0 = nb0; START = start; nh[0] = n0; nh[1] = n1; NMAX = n0 > n1 ? n0 : n1; AMAX = amax < 2 ? 2 : amax;
  kind = walloc(4 * nn); player = walloc(4 * nn); nact = walloc(4 * nn); coff = walloc(4 * nn); street = walloc(4 * nn);
  uF = walloc(8 * nn); uW = walloc(8 * nn); uL = walloc(8 * nn); uT = walloc(8 * nn);
  for (int p = 0; p < 2; p++) { ca[p] = walloc(nh[p]); cb[p] = walloc(nh[p]); init[p] = walloc(4 * nh[p]); same[p] = walloc(4 * nh[p]); bycardOff[p] = walloc(4 * 53); bycard[p] = walloc(4 * 2 * nh[p]); }
  slotTab = walloc(4 * nn); zero(slotTab, 4 * nn);
  sdTab = walloc(4 * 52 * 52); for (int i = 0; i < 52 * 52; i++) sdTab[i] = -1;
  for (int d = 0; d < MAXD; d++) { Dout[d] = walloc(4 * NMAX); Dkids[d] = walloc(4 * NMAX * AMAX); Dsig[d] = walloc(4 * NMAX * AMAX); Dm[d] = walloc(4 * NMAX); Do[d] = walloc(4 * NMAX); }
  for (int l = 0; l < 2; l++) { RA[l] = walloc(4 * NMAX); RO[l] = walloc(4 * NMAX); RT[l] = walloc(4 * NMAX); }
  iters = 0; storeUsed = 0; storeCap = 0; store = 0;
  return 1;
}
// pointers for JS to fill
__attribute__((export_name("ptr"))) void* ptr(i32 which, i32 p) {
  switch (which) { case 0: return kind; case 1: return player; case 2: return nact; case 3: return coff; case 4: return street;
    case 5: return uF; case 6: return uW; case 7: return uL; case 8: return uT; case 9: return ca[p]; case 10: return cb[p]; case 11: return init[p];
    case 12: return same[p]; case 13: return bycardOff[p]; case 14: return bycard[p]; case 15: return board; }
  return 0;
}
__attribute__((export_name("set_kids"))) void* set_kids(i32 total) { kids = walloc(4 * total); return kids; }
__attribute__((export_name("set_params"))) void set_params(float a, float b, float g) { alpha = a; beta = b; gamma_ = g; }

// instances per street: start street 1; later streets index by the cards dealt (52 per card)
static inline i32 ninst(i32 st) { i32 k = st - START, r = 1; while (k-- > 0) r *= 52; return r; }
static float* slot(i32 id, i32 inst, i32 p) {
  i32* tab = (i32*)slotTab[id];
  if (!tab) { i32 n = ninst(street[id]); tab = walloc(4 * n); for (i32 i = 0; i < n; i++) tab[i] = -1; slotTab[id] = (i32)tab; }
  if (tab[inst] < 0) {
    u32 len = 2u * nh[p] * nact[id];
    float* m = walloc(4 * len); if (!m) return 0; zero(m, 4 * len);
    tab[inst] = (i32)m; storeUsed += 4 * len;
  }
  return (float*)tab[inst];
}
__attribute__((export_name("store_bytes"))) u32 store_bytes(void) { return storeUsed; }

// showdown data for the current 5-card board: per player str[n] and order[n] (only hands not on the board)
static i32* showdown(const u8* b, i32 nb, i32 idx) {
  if (idx < 0) idx = 0;
  if (sdTab[idx] >= 0) return (i32*)sdTab[idx];
  // layout: [cnt0, cnt1, str0[n0], ord0[n0], str1[n1], ord1[n1]]
  i32* d = walloc(4 * (2 + 2 * nh[0] + 2 * nh[1]));
  u8 c7[7]; for (int i = 0; i < 5; i++) c7[i] = b[i];
  i32* w = d + 2;
  for (int p = 0; p < 2; p++) {
    i32 n = nh[p], *str = w, *ord = w + n, k = 0;
    for (i32 i = 0; i < n; i++) {
      u8 a = ca[p][i], c = cb[p][i]; int bad = 0; for (int j = 0; j < 5; j++) if (b[j] == a || b[j] == c) bad = 1;
      if (bad) { str[i] = -1; continue; } c7[5] = a; c7[6] = c; str[i] = eval7(c7); ord[k++] = i;
    }
    // insertion sort by strength (n <= ~1300; done once per board)
    for (i32 x = 1; x < k; x++) { i32 v = ord[x], sv = str[v], y = x - 1; while (y >= 0 && str[ord[y]] > sv) { ord[y + 1] = ord[y]; y--; } ord[y + 1] = v; }
    d[p] = k; w += 2 * n;
  }
  sdTab[idx] = (i32)d; return d;
}
static void terminal(i32 id, i32 p, const float* oreach, u8* b, i32 nb, i32 sdIdx, float* out);
// all-in before the river: average the showdown over every card still to come
static void runout(i32 id, i32 p, const float* oreach, u8* b, i32 nb, float* out) {
  i32 lv = nb - 3; if (lv < 0) lv = 0; if (lv > 1) lv = 1;
  float *acc = RA[lv], *o2 = RO[lv], *tmp = RT[lv]; i32 o = 1 - p, np = nh[p], no = nh[o];
  u8 onB[52] = {0}; for (i32 x = 0; x < nb; x++) onB[b[x]] = 1;
  float inv = 1.f / (float)(52 - nb - 4);
  for (i32 i = 0; i < np; i++) acc[i] = 0;
  for (i32 c = 0; c < 52; c++) {
    if (onB[c]) continue;
    for (i32 j = 0; j < no; j++) o2[j] = oreach[j];
    for (i32 x = bycardOff[o][c]; x < bycardOff[o][c + 1]; x++) o2[bycard[o][x]] = 0;
    b[nb] = (u8)c;
    i32 sdi = nb + 1 == 5 ? (NB0 == 3 ? (b[3] < b[4] ? b[3] * 52 + b[4] : b[4] * 52 + b[3]) : NB0 == 4 ? b[4] : 0) : -1;
    terminal(id, p, o2, b, nb + 1, sdi, tmp);
    for (i32 i = 0; i < np; i++) acc[i] += tmp[i];
    for (i32 x = bycardOff[p][c]; x < bycardOff[p][c + 1]; x++) acc[bycard[p][x]] -= tmp[bycard[p][x]];
  }
  for (i32 i = 0; i < np; i++) out[i] = acc[i] * inv;
}
static void terminal(i32 id, i32 p, const float* oreach, u8* b, i32 nb, i32 sdIdx, float* out) {
  i32 o = 1 - p, np = nh[p], no = nh[o]; const u8 *pa = ca[p], *pb = cb[p], *oa = ca[o], *ob = cb[o]; const i32* sm = same[p];
  for (int c = 0; c < 52; c++) cardS[c] = 0; float tot = 0;
  for (i32 j = 0; j < no; j++) { float r = oreach[j]; if (r == 0) continue; tot += r; cardS[oa[j]] += r; cardS[ob[j]] += r; }
  if (kind[id] == KF) {
    float u = uF[2 * id + p];
    for (i32 i = 0; i < np; i++) { float s = sm[i] >= 0 ? oreach[sm[i]] : 0; out[i] = u * (tot - cardS[pa[i]] - cardS[pb[i]] + s); }
    return;
  }
  if (nb < 5) { runout(id, p, oreach, b, nb, out); return; }
  float w_ = uW[2 * id + p], l_ = uL[2 * id + p], t_ = uT[2 * id + p];
  i32* d = showdown(b, nb, sdIdx); i32 kp = d[p], ko = d[o];
  i32 *s0 = d + 2, *str[2], *ord[2]; str[0] = s0; ord[0] = s0 + nh[0]; str[1] = s0 + 2 * nh[0]; ord[1] = str[1] + nh[1];
  i32 *po = ord[p], *oo = ord[o], *pstr = str[p], *ostr = str[o];
  for (int c = 0; c < 52; c++) wCard[c] = 0;
  for (i32 i = 0; i < np; i++) out[i] = 0;
  float wTot = 0; i32 j = 0, i = 0;
  while (i < kp) {
    i32 s = pstr[po[i]];
    while (j < ko && ostr[oo[j]] < s) { i32 jj = oo[j]; float r = oreach[jj]; if (r != 0) { wTot += r; wCard[oa[jj]] += r; wCard[ob[jj]] += r; } j++; }
    float tTot = 0; i32 j2 = j;
    while (j2 < ko && ostr[oo[j2]] == s) { i32 jj = oo[j2]; float r = oreach[jj]; if (r != 0) { tTot += r; tCard[oa[jj]] += r; tCard[ob[jj]] += r; } j2++; }
    i32 i2 = i;
    while (i2 < kp && pstr[po[i2]] == s) {
      i32 ii = po[i2]; u8 a = pa[ii], c = pb[ii]; float smv = sm[ii] >= 0 ? oreach[sm[ii]] : 0;
      float compat = tot - cardS[a] - cardS[c] + smv, win = wTot - wCard[a] - wCard[c], tie = tTot - tCard[a] - tCard[c] + smv;
      out[ii] = win * w_ + tie * t_ + (compat - win - tie) * l_; i2++;
    }
    for (i32 jx = j; jx < j2; jx++) { i32 jj = oo[jx]; tCard[oa[jj]] = 0; tCard[ob[jj]] = 0; }
    i = i2;
  }
}
static void strat(const float* X, i32 n, i32 na, float* sig) {
  for (i32 i = 0; i < n; i++) {
    float s = 0; for (i32 a = 0; a < na; a++) { float x = X[a * n + i]; if (x > 0) s += x; }
    if (s > 0) { float inv = 1.f / s; for (i32 a = 0; a < na; a++) { float x = X[a * n + i]; sig[a * n + i] = x > 0 ? x * inv : 0; } }
    else { float u = 1.f / na; for (i32 a = 0; a < na; a++) sig[a * n + i] = u; }
  }
}
// mode: 0 train, 1 average strategy, 2 best response
static float* walk(i32 id, i32 p, const float* my, const float* oreach, u8* b, i32 nb, i32 inst, i32 sdIdx, i32 depth, i32 mode) {
  float* out = Dout[depth]; i32 np = nh[p];
  i32 k = kind[id];
  if (k == KF || k == KS) { terminal(id, p, oreach, b, nb, sdIdx, out); return out; }
  if (k == KC) {
    u8 onB[52] = {0}; for (i32 x = 0; x < nb; x++) onB[b[x]] = 1;
    float live = (float)(52 - nb - 4), *acc = Dkids[depth], *m2 = Dm[depth], *o2 = Do[depth]; i32 o = 1 - p, no = nh[o];
    for (i32 i = 0; i < np; i++) acc[i] = 0;
    for (i32 c = 0; c < 52; c++) {
      if (onB[c]) continue;
      for (i32 j = 0; j < no; j++) o2[j] = oreach[j];
      for (i32 x = bycardOff[o][c]; x < bycardOff[o][c + 1]; x++) o2[bycard[o][x]] = 0;
      i32 any = 0; for (i32 j = 0; j < no; j++) if (o2[j] != 0) { any = 1; break; }
      if (!any) continue;
      for (i32 i = 0; i < np; i++) m2[i] = my[i];
      for (i32 x = bycardOff[p][c]; x < bycardOff[p][c + 1]; x++) m2[bycard[p][x]] = 0;
      b[nb] = (u8)c;
      i32 sdi = nb + 1 == 5 ? (NB0 == 3 ? (b[3] < b[4] ? b[3] * 52 + b[4] : b[4] * 52 + b[3]) : b[4]) : sdIdx;
      float* v = walk(kids[coff[id]], p, m2, o2, b, nb + 1, inst * 52 + c, sdi, depth + 1, mode);
      for (i32 i = 0; i < np; i++) acc[i] += v[i];
      for (i32 x = bycardOff[p][c]; x < bycardOff[p][c + 1]; x++) acc[bycard[p][x]] -= v[bycard[p][x]];
    }
    float inv = 1.f / live; for (i32 i = 0; i < np; i++) out[i] = acc[i] * inv;
    return out;
  }
  i32 q = player[id], na = nact[id];
  if (q == p) {
    float* st = slot(id, inst, p); i32 n = np; float *R = st, *S = st + n * na, *sig = Dsig[depth], *kd = Dkids[depth], *m2 = Dm[depth];
    strat(mode == 0 ? R : S, n, na, sig);
    for (i32 a = 0; a < na; a++) {
      for (i32 i = 0; i < n; i++) m2[i] = my[i] * sig[a * n + i];
      float* v = walk(kids[coff[id] + a], p, m2, oreach, b, nb, inst, sdIdx, depth + 1, mode);
      for (i32 i = 0; i < n; i++) kd[a * n + i] = v[i];
    }
    if (mode == 2) { for (i32 i = 0; i < n; i++) { float m = kd[i]; for (i32 a = 1; a < na; a++) if (kd[a * n + i] > m) m = kd[a * n + i]; out[i] = m; } return out; }
    for (i32 i = 0; i < n; i++) { float v = 0; for (i32 a = 0; a < na; a++) v += sig[a * n + i] * kd[a * n + i]; out[i] = v; }
    if (mode == 0) {
      for (i32 a = 0; a < na; a++) { float* Ra = R + a * n; float* Sa = S + a * n; const float* sa = sig + a * n; const float* ka = kd + a * n;
        for (i32 i = 0; i < n; i++) { float r = Ra[i]; r = r > 0 ? r * posD : r * negD; Ra[i] = r + ka[i] - out[i]; Sa[i] = Sa[i] * wS + my[i] * sa[i]; } }
    }
    return out;
  }
  i32 o = q, no = nh[o]; float* st = slot(id, inst, o); float *sig = Dsig[depth], *o2 = Do[depth], *acc = Dkids[depth];
  strat(mode == 0 ? st : st + no * na, no, na, sig);
  for (i32 i = 0; i < np; i++) acc[i] = 0;
  for (i32 a = 0; a < na; a++) {
    i32 any = 0; for (i32 j = 0; j < no; j++) { float x = oreach[j] * sig[a * no + j]; o2[j] = x; if (x != 0) any = 1; }
    if (!any) continue;
    float* v = walk(kids[coff[id] + a], p, my, o2, b, nb, inst, sdIdx, depth + 1, mode);
    for (i32 i = 0; i < np; i++) acc[i] += v[i];
  }
  for (i32 i = 0; i < np; i++) out[i] = acc[i];
  return out;
}
// one DCFR iteration; the discounts for this iteration come from JS (no libm in this module)
__attribute__((export_name("iterate"))) i32 iterate(float pos, float neg, float ws) {
  u8 b[5]; for (int i = 0; i < 5; i++) b[i] = board[i];
  posD = pos; negD = neg; wS = ws;
  for (i32 p = 0; p < 2; p++) walk(0, p, init[p], init[1 - p], b, NB0, 0, NB0 == 5 ? 0 : -1, 0, 0);
  return ++iters;
}
// value vector for player p at the root; returns pointer to Dout[0]
__attribute__((export_name("value"))) float* value(i32 p, i32 mode) {
  u8 b[5]; for (int i = 0; i < 5; i++) b[i] = board[i];
  return walk(0, p, init[p], init[1 - p], b, NB0, 0, NB0 == 5 ? 0 : -1, 0, mode);
}
// average strategy of player p at node id / instance: na x n floats (normalised) into out
__attribute__((export_name("avg"))) i32 avg(i32 id, i32 inst, float* out) {
  i32* tab = (i32*)slotTab[id]; i32 p = player[id], n = nh[p], na = nact[id];
  if (!tab || tab[inst] < 0) { for (i32 i = 0; i < n * na; i++) out[i] = 1.f / na; return 0; }
  strat(((float*)tab[inst]) + n * na, n, na, out); return 1;
}
__attribute__((export_name("iterations"))) i32 iterations(void) { return iters; }
