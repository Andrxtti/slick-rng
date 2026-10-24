# Paints the closet's clothes onto UMA's garment meshes. Each closet item is a recipe here: a base garment (one of
# UMA's slot meshes, whose UV layout + body positions were exported to uv/<slot>_slot.json by
# Dream > 3D characters > Export clothing UVs), a fabric, and layers painted in BODY space (x right, y front, z up,
# metres), so "a chest print" is the same instruction on a tee, a hoodie or a jacket. Output: <id>.png (diffuse,
# 1024px) per item plus clothes.json (item -> base slot, base recipe) for the editor to turn into UMA overlays and
# wardrobe recipes (Dream > 3D characters > Build closet clothes).
#   python3 clothes.py <out dir> [item,item]
import json, os, sys, math, numpy as np, cv2
HERE = os.path.dirname(os.path.abspath(__file__))
S = 1024

def C4(h): return tuple(int(round(float(v) * 255)) for v in hexrgb(h)) + (255,)
def hexrgb(h): h = h.lstrip('#'); return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255

# ------------------------------------------------------------------ bases: UMA slot meshes we paint on
BASES = {
    # id: (slot asset name, wardrobe recipe it came from, UMA wardrobe slot, normal/spec textures to reuse)
    "tee":        ("male_tshirt_white", "male_tshirt_white_Recipe", "Chest", "tshirt_white"),
    "hoodie":     ("male_hoodie_grey", "male_hoodie_grey_Recipe", "Chest", "hoodie_grey"),
    "jacket":     ("male_jacket_hive", "male_jacket_hive_Recipe", "Chest", "jacket_hive"),
    "sweats":     ("male_sweatpants_black", "male_sweatpants_black_Recipe", "Legs", "sweatpants_black"),
    "joggers":    ("male_sportpants_grey", "male_sportpants_grey_Recipe", "Legs", "sportpants_grey"),
    "slacks":     ("male_sportpants_alt_black", "male_sportpants_alt_black_Recipe", "Legs", "sportpants_alt_black"),
    "shorts":     ("male_shorts_black_cotton", "male_shorts_black_cotton_Recipe", "Legs", "shorts_black_cotton"),
    "lows":       ("male_shoe_low_white.001", "male_shoe_low_white.001_Recipe", "Feet", "shoe_low_white"),
    "highs":      ("male_shoes_tall", "male_shoes_tall_Recipe", "Feet", "shoes_tall_white"),
}

_maps = {}
def body_map(base):
    """per texel of the base's texture: body position (x,y,z), a facing hint (front +1 / back -1 / side 0), mask"""
    if base in _maps: return _maps[base]
    slot = BASES[base][0]
    d = json.load(open(os.path.join(HERE, 'uv', slot + '_slot.json')))
    uv, pos, tris = np.array(d['uv']), np.array(d['pos']), np.array(d['tris']).reshape(-1, 3)
    P = np.zeros((S, S, 3), np.float32); M = np.zeros((S, S), np.uint8); N = np.zeros((S, S, 3), np.float32)
    # per-vertex normals (body space) so we know which way a texel faces
    vn = np.zeros_like(pos)
    for t in tris:
        a, b, c = pos[t]; n = np.cross(b - a, c - a); vn[t] += n
    vn /= np.maximum(np.linalg.norm(vn, axis=1, keepdims=True), 1e-9)
    for t in tris:
        p = uv[t]; pts = np.array([[p[i, 0] * S, (1 - p[i, 1]) * S] for i in range(3)], np.float32)
        x0, y0 = np.floor(pts.min(0)).astype(int); x1, y1 = np.ceil(pts.max(0)).astype(int)
        x0, y0 = max(0, x0), max(0, y0); x1, y1 = min(S - 1, x1), min(S - 1, y1)
        if x1 < x0 or y1 < y0: continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        (ax, ay), (bx, by), (cx, cy) = pts
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-9: continue
        w0 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den
        w1 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den
        w2 = 1 - w0 - w1
        eps = -0.02   # a hair of bleed so seams don't show
        inside = (w0 >= eps) & (w1 >= eps) & (w2 >= eps)
        if not inside.any(): continue
        q = w0[..., None] * pos[t[0]] + w1[..., None] * pos[t[1]] + w2[..., None] * pos[t[2]]
        nn = w0[..., None] * vn[t[0]] + w1[..., None] * vn[t[1]] + w2[..., None] * vn[t[2]]
        sub = P[y0:y1 + 1, x0:x1 + 1]; sub[inside] = q[inside]
        subn = N[y0:y1 + 1, x0:x1 + 1]; subn[inside] = nn[inside]
        M[y0:y1 + 1, x0:x1 + 1][inside] = 1
    # grow the painted area a few texels past the island edge (texture filtering samples outside)
    for _ in range(6):
        dil = cv2.dilate(M, np.ones((3, 3), np.uint8)); ring = (dil > 0) & (M == 0)
        if not ring.any(): break
        for arr in (P, N):
            blur = cv2.blur(arr * M[..., None], (3, 3)); cnt = cv2.blur(M.astype(np.float32), (3, 3))
            arr[ring] = (blur / np.maximum(cnt[..., None], 1e-6))[ring]
        M = dil
    _maps[base] = (P, N, M.astype(bool)); return _maps[base]

# ------------------------------------------------------------------ fabric
def noise(shape, scale, seed=0):
    rng = np.random.default_rng(seed); small = rng.random((max(2, shape[0] // scale), max(2, shape[1] // scale))).astype(np.float32)
    return cv2.resize(small, (shape[1], shape[0]), interpolation=cv2.INTER_CUBIC)
def fabric(kind, seed=1):
    """a luminance layer around 1.0 with the weave of the cloth"""
    h = np.ones((S, S), np.float32)
    if kind == "jersey":   # fine knit
        h += (noise((S, S), 2, seed) - 0.5) * 0.10 + (noise((S, S), 9, seed + 1) - 0.5) * 0.06
    elif kind == "fleece":
        h += (noise((S, S), 3, seed) - 0.5) * 0.16 + (noise((S, S), 24, seed + 1) - 0.5) * 0.08
    elif kind == "denim":
        yy, xx = np.mgrid[0:S, 0:S]; tw = np.sin((xx + yy) * 0.9) * 0.5 + 0.5
        h += (tw - 0.5) * 0.14 + (noise((S, S), 2, seed) - 0.5) * 0.10
    elif kind == "wool":
        h += (noise((S, S), 4, seed) - 0.5) * 0.12 + (noise((S, S), 40, seed + 1) - 0.5) * 0.10
    elif kind == "leather":
        h += (noise((S, S), 6, seed) - 0.5) * 0.18 + (noise((S, S), 60, seed + 1) - 0.5) * 0.12
    elif kind == "nylon":
        h += (noise((S, S), 2, seed) - 0.5) * 0.05
    elif kind == "suit":
        h += (noise((S, S), 2, seed) - 0.5) * 0.05 + (noise((S, S), 30, seed + 1) - 0.5) * 0.04
    elif kind == "canvas":
        yy, xx = np.mgrid[0:S, 0:S]; h += (np.sin(xx * 1.7) * np.sin(yy * 1.7)) * 0.07 + (noise((S, S), 3, seed) - 0.5) * 0.08
    else:
        h += (noise((S, S), 3, seed) - 0.5) * 0.08
    return h

# ------------------------------------------------------------------ painting in body space
class Painter:
    def __init__(self, base, color, weave="jersey", seed=1):
        self.base = base; self.P, self.N, self.M = body_map(base)
        self.img = np.zeros((S, S, 3), np.float32); self.img[:] = hexrgb(color)
        self.weave = fabric(weave, seed)
        X, Y, Z = self.P[..., 0], self.P[..., 1], self.P[..., 2]
        self.x, self.y, self.z = X, Y, Z
        self.front = self.N[..., 1] < -0.25; self.back = self.N[..., 1] > 0.25   # UMA's slots: -y is the front
        zs = Z[self.M]; self.z0, self.z1 = float(zs.min()), float(zs.max())
    def h(self, t): return self.z0 + (self.z1 - self.z0) * t    # height as a fraction of the garment
    def where(self, m, color, soft=0.0):
        """paint colour where the body-space mask m holds (softened by `soft` texels)"""
        a = m.astype(np.float32)
        if soft > 0: a = cv2.GaussianBlur(a, (0, 0), soft)
        self.img = self.img * (1 - a[..., None]) + hexrgb(color) * a[..., None]
    def band(self, z_lo, z_hi, color, sides="all", soft=1.0):
        m = (self.z >= z_lo) & (self.z <= z_hi)
        if sides == "front": m &= self.front
        if sides == "back": m &= self.back
        self.where(m, color, soft)
    def stripe_x(self, x0, x1, color, z_lo=None, z_hi=None, sides="all", soft=1.0):
        m = (self.x >= x0) & (self.x <= x1)
        if z_lo is not None: m &= self.z >= z_lo
        if z_hi is not None: m &= self.z <= z_hi
        if sides == "front": m &= self.front
        if sides == "back": m &= self.back
        self.where(m, color, soft)
    def sleeves(self, color, from_x=0.24, soft=1.0): self.where(np.abs(self.x) >= from_x, color, soft)
    def legs_side_stripe(self, color, width=0.012, soft=0.8):
        """down the outside seam of each leg: the texels at the outer angle around that leg's axis"""
        m = np.zeros((S, S), bool)
        K = 128; zf = np.clip((self.z - self.z0) / (self.z1 - self.z0) * (K - 1), 0, K - 1); zq = zf.astype(int)
        for sgn in (-1, 1):
            leg = self.M & (np.sign(self.x) == sgn)
            cx = np.full(K, np.nan, np.float32); cy = np.full(K, np.nan, np.float32); rad = np.full(K, np.nan, np.float32)
            for k in range(K):
                sel = leg & (zq == k)
                if sel.sum() > 12 and self.z0 + (self.z1 - self.z0) * k / K < 0.72:   # below the crotch the slice is one leg
                    cx[k] = self.x[sel].mean(); cy[k] = self.y[sel].mean(); rad[k] = np.abs(self.x[sel] - cx[k]).max()
            ks = np.arange(K)
            for arr in (cx, cy, rad):
                ok = ~np.isnan(arr)
                if ok.sum() > 2: arr[:] = np.interp(ks, ks[ok], arr[ok]); arr[:] = np.convolve(np.pad(arr, 3, mode='edge'), np.ones(7) / 7, mode='valid')
                else: arr[:] = 0.08 if arr is rad else 0
            cxi = np.interp(zf, ks, cx); cyi = np.interp(zf, ks, cy); ri = np.interp(zf, ks, rad)
            ang = np.arctan2(self.y - cyi, (self.x - cxi) * sgn)   # 0 = facing out (sideways), pi/2 = back
            m |= leg & (np.abs(ang) < width / np.maximum(ri, 0.03)) & (self.z < self.z1 - 0.03)
        self.where(m, color, soft)
    def decal(self, img_fn, cx, cz, w, hgt, sides="front", colors=None):
        """project a picture (drawn by img_fn into a WxH canvas, RGBA) onto the body plane x/z"""
        W, H = 512, int(512 * hgt / w); can8 = np.zeros((H, W, 4), np.uint8); img_fn(can8); can = can8.astype(np.float32) / 255
        xs = -self.x if sides == "back" else self.x   # seen from behind, left is right
        u = (xs - (cx - w / 2)) / w; v = 1 - (self.z - (cz - hgt / 2)) / hgt
        ok = (u >= 0) & (u < 1) & (v >= 0) & (v < 1) & self.M
        if sides == "front": ok &= self.front
        elif sides == "back": ok &= self.back
        ui = np.clip((u * W).astype(int), 0, W - 1); vi = np.clip((v * H).astype(int), 0, H - 1)
        samp = can[vi, ui]; a = samp[..., 3] * ok
        a = cv2.GaussianBlur(a.astype(np.float32), (0, 0), 0.6)
        self.img = self.img * (1 - a[..., None]) + samp[..., :3] * a[..., None]
    def finish(self, contrast=1.0, shade=True):
        out = self.img * (self.weave[..., None] ** contrast if shade else 1)
        # ambient darkening in the armpits/crotch: deep inside texels (|normal.y| small and low) get a touch darker
        out = np.clip(out, 0, 1)
        out[~self.M] = out[self.M].mean(0)
        return (out[..., ::-1] * 255).astype(np.uint8)   # BGR for cv2

# ------------------------------------------------------------------ pictures for decals (drawn with cv2, RGBA float canvases)
def text_decal(txt, color, scale=1.0, thick=2, font=cv2.FONT_HERSHEY_DUPLEX, italic=False):
    def fn(can):
        H, W = can.shape[:2]; c = C4(color)
        (tw, th), _ = cv2.getTextSize(txt, font, scale * 3, thick * 3)
        cv2.putText(can, txt, ((W - tw) // 2, (H + th) // 2), font | (cv2.FONT_ITALIC if italic else 0), scale * 3, c, thick * 3, cv2.LINE_AA)
    return fn
def number_decal(n, color, outline=None):
    def fn(can):
        H, W = can.shape[:2]; c = C4(color)
        (tw, th), _ = cv2.getTextSize(n, cv2.FONT_HERSHEY_DUPLEX, 9, 22)
        if outline: cv2.putText(can, n, ((W - tw) // 2, (H + th) // 2), cv2.FONT_HERSHEY_DUPLEX, 9, C4(outline), 36, cv2.LINE_AA)
        cv2.putText(can, n, ((W - tw) // 2, (H + th) // 2), cv2.FONT_HERSHEY_DUPLEX, 9, c, 22, cv2.LINE_AA)
    return fn
def logo_decal(kind, color, color2=None):
    def fn(can):
        H, W = can.shape[:2]; c = C4(color); c2 = C4(color2 or color)
        if kind == "spade":
            pts = np.array([[W / 2, H * 0.08], [W * 0.9, H * 0.5], [W * 0.78, H * 0.72], [W * 0.56, H * 0.66], [W * 0.62, H * 0.9], [W * 0.38, H * 0.9], [W * 0.44, H * 0.66], [W * 0.22, H * 0.72], [W * 0.1, H * 0.5]], np.int32)
            cv2.fillPoly(can, [pts], c, cv2.LINE_AA); cv2.circle(can, (int(W * 0.3), int(H * 0.55)), int(W * 0.18), c, -1, cv2.LINE_AA); cv2.circle(can, (int(W * 0.7), int(H * 0.55)), int(W * 0.18), c, -1, cv2.LINE_AA)
        elif kind == "flame":
            cv2.ellipse(can, (W // 2, int(H * 0.62)), (int(W * 0.28), int(H * 0.36)), 0, 0, 360, c, -1, cv2.LINE_AA)
            pts = np.array([[W * 0.5, H * 0.05], [W * 0.72, H * 0.4], [W * 0.28, H * 0.4]], np.int32); cv2.fillPoly(can, [pts], c, cv2.LINE_AA)
            cv2.ellipse(can, (W // 2, int(H * 0.68)), (int(W * 0.14), int(H * 0.2)), 0, 0, 360, c2, -1, cv2.LINE_AA)
        elif kind == "drift":   # Drift Supply: a chevron wave
            for k in range(3):
                y = int(H * (0.3 + 0.2 * k)); cv2.polylines(can, [np.array([[W * 0.15, y + H * 0.1], [W * 0.5, y - H * 0.08], [W * 0.85, y + H * 0.1]], np.int32)], False, c, max(3, W // 26), cv2.LINE_AA)
        elif kind == "crest":   # Felt Kings crown in a shield
            pts = np.array([[W * 0.5, H * 0.95], [W * 0.1, H * 0.6], [W * 0.1, H * 0.1], [W * 0.9, H * 0.1], [W * 0.9, H * 0.6]], np.int32); cv2.fillPoly(can, [pts], c, cv2.LINE_AA)
            cr = np.array([[W * 0.25, H * 0.65], [W * 0.25, H * 0.35], [W * 0.38, H * 0.5], [W * 0.5, H * 0.25], [W * 0.62, H * 0.5], [W * 0.75, H * 0.35], [W * 0.75, H * 0.65]], np.int32); cv2.fillPoly(can, [cr], c2, cv2.LINE_AA)
        elif kind == "tree":    # Northline pine
            for k in range(3):
                t = H * (0.15 + 0.22 * k); pts = np.array([[W * 0.5, t], [W * (0.72 + 0.08 * k), t + H * 0.3], [W * (0.28 - 0.08 * k), t + H * 0.3]], np.int32); cv2.fillPoly(can, [pts], c, cv2.LINE_AA)
            cv2.rectangle(can, (int(W * 0.45), int(H * 0.8)), (int(W * 0.55), int(H * 0.97)), c2, -1)
        elif kind == "cards":   # two cards fanned
            for k, ang in enumerate((-18, 12)):
                box = cv2.boxPoints(((W * (0.42 + 0.16 * k), H * 0.5), (W * 0.38, H * 0.56), ang)).astype(np.int32)
                cv2.fillPoly(can, [box], c if k else c2, cv2.LINE_AA); cv2.polylines(can, [box], True, (25, 25, 25, 255), 3, cv2.LINE_AA)
        elif kind == "ring":
            cv2.circle(can, (W // 2, H // 2), int(W * 0.4), c, max(4, W // 10), cv2.LINE_AA)
    return fn
def pattern_argyle(p, c1, c2, line, z_lo, z_hi, size=0.075):
    """diamonds over the front torso"""
    u = (p.x / size); v = ((p.z - z_lo) / size)
    a = np.floor(u + v); b = np.floor(u - v)
    m1 = ((a + b) % 2 == 0) & p.front & (p.z > z_lo) & (p.z < z_hi) & (np.abs(p.x) < 0.2)
    p.where(m1, c2, 0.6)
    fa = np.abs(((u + v) % 1) - 0.5) < 0.06; fb = np.abs(((u - v) % 1) - 0.5) < 0.06
    p.where((fa | fb) & p.front & (p.z > z_lo) & (p.z < z_hi) & (np.abs(p.x) < 0.2), line, 0.4)
def pattern_plaid(p, c1, c2, c3, size=0.06):
    u = p.x / size; v = p.z / size
    bu = (np.floor(u) % 2 == 0); bv = (np.floor(v) % 2 == 0)
    p.where(bu & ~bv | ~bu & bv, c2, 0.5)
    p.where(bu & bv, c3, 0.5)
    thin = (np.abs((u % 1) - 0.5) < 0.05) | (np.abs((v % 1) - 0.5) < 0.05)
    p.where(thin, c1, 0.3)

# ------------------------------------------------------------------ the closet
# every item: base, body colour, weave, and a list of paint steps. Heights are metres on the body (hem ~1.0, chest
# ~1.35, collar ~1.5 for tops; waist ~1.0, knee ~0.5, ankle ~0.1 for legs).
def tops():
    I = {}
    def item(id_, base, color, weave, steps, **meta): I[id_] = dict(base=base, color=color, weave=weave, steps=steps, **meta)
    item("office", "tee", "#cfd7e3", "jersey", [("band", 1.44, 1.6, "#eef2f7"), ("stripe", -0.012, 0.012, "#24427e", 1.08, 1.5, "front"), ("band", 0.98, 1.03, "#b9c3d1")])
    item("tee", "tee", "#17171a", "jersey", [("band", 1.49, 1.6, "#2a2a2f")])
    item("jersey", "tee", "#f1efe8", "jersey", [("sleeves", "#b8262c", 0.2), ("band", 1.5, 1.6, "#b8262c"), ("decal", number_decal("7", "#b8262c", "#1b1b1f"), 0, 1.3, 0.2, 0.22, "front"), ("decal", number_decal("7", "#b8262c", "#1b1b1f"), 0, 1.32, 0.24, 0.26, "back"), ("decal", text_decal("FELT KINGS", "#1b1b1f", 0.8, 2), 0, 1.47, 0.26, 0.05, "back")])
    item("polo", "hoodie", "#1f3b2e", "jersey", [("stripe", -0.006, 0.006, "#c9a03e", 1.3, 1.56, "front"), ("band", 1.52, 1.62, "#163023"), ("decal", logo_decal("spade", "#c9a03e"), -0.1, 1.42, 0.05, 0.055, "front"), ("decal", text_decal("RIVERSIDE", "#c9a03e", 0.6, 2), 0, 1.3, 0.28, 0.04, "back")])
    item("hoodie", "hoodie", "#4a4d55", "fleece", [("band", 1.08, 1.16, "#3c3f46"), ("decal", logo_decal("drift", "#e9e7e1"), 0, 1.4, 0.16, 0.12, "front"), ("decal", text_decal("DRIFT SUPPLY", "#e9e7e1", 0.9, 2), 0, 1.27, 0.3, 0.05, "front")])
    item("fleece", "hoodie", "#2f5f52", "fleece", [("stripe", -0.02, 0.02, "#1d1d22", 1.2, 1.62, "front"), ("band", 1.55, 1.64, "#1d1d22"), ("decal", logo_decal("tree", "#e9e7e1", "#8a5f3c"), -0.11, 1.42, 0.06, 0.07, "front"), ("decal", text_decal("NORTHLINE", "#e9e7e1", 0.7, 2), 0, 1.3, 0.3, 0.045, "back")])
    item("cardigan", "hoodie", "#6b4a3a", "wool", [("stripe", -0.03, 0.03, "#4c3428", 1.0, 1.62, "front"), ("band", 1.0, 1.05, "#4c3428"), ("sleeves", "#6b4a3a", 0.26)])
    item("track", "jacket", "#14213d", "nylon", [("sleeve_stripes", "#e9e7e1", "#c8323a"), ("band", 1.58, 1.68, "#e9e7e1"), ("band", 0.98, 1.04, "#e9e7e1"), ("stripe", -0.008, 0.008, "#c8c8c8", 1.0, 1.6, "front"), ("decal", text_decal("DRIFT", "#e9e7e1", 0.8, 2, italic=True), -0.1, 1.42, 0.13, 0.05, "front")])
    item("denim", "jacket", "#3b5a86", "denim", [("stripe", -0.01, 0.01, "#2c4568", 1.0, 1.62, "front"), ("band", 1.0, 1.07, "#2c4568"), ("band", 1.52, 1.62, "#2c4568"), ("decal", logo_decal("ring", "#d9b36c"), -0.11, 1.38, 0.09, 0.09, "front"), ("decal", logo_decal("ring", "#d9b36c"), 0.11, 1.38, 0.09, 0.09, "front"), ("decal", text_decal("DRIFT", "#d9b36c", 1.2, 3), 0, 1.3, 0.3, 0.07, "back")])
    item("varsity", "jacket", "#1b1b1f", "wool", [("sleeves", "#efe3c8", 0.22), ("band", 1.58, 1.68, "#efe3c8"), ("band", 0.98, 1.05, "#efe3c8"), ("decal", logo_decal("crest", "#b8262c", "#efe3c8"), -0.1, 1.4, 0.09, 0.1, "front"), ("decal", text_decal("KINGS", "#efe3c8", 2.0, 4), 0, 1.33, 0.34, 0.1, "back")])
    item("leather", "jacket", "#2a221f", "leather", [("stripe", -0.012, 0.012, "#8a8f98", 1.0, 1.62, "front"), ("band", 1.56, 1.66, "#1c1614"), ("stripe", 0.2, 0.3, "#1c1614", 1.3, 1.5, "front")])
    item("bomber", "jacket", "#141416", "nylon", [("band", 1.56, 1.66, "#2a2a2f"), ("band", 0.98, 1.05, "#2a2a2f"), ("stripe", -0.008, 0.008, "#9a9ca4", 1.05, 1.6, "front"), ("decal", logo_decal("flame", "#f08a1c", "#ffd23f"), 0.11, 1.42, 0.05, 0.06, "front")])
    item("vest", "tee", "#eef2f7", "jersey", [("argyle", "#3a2f55", "#6a5a9a", "#c9a03e", 1.05, 1.5), ("band", 1.46, 1.6, "#3a2f55"), ("sleeves", "#eef2f7", 0.2)])
    item("blazer", "jacket", "#1f2a44", "suit", [("stripe", -0.03, 0.03, "#eef2f7", 1.0, 1.62, "front"), ("stripe", -0.004, 0.004, "#8a2a2a", 1.0, 1.4, "front"), ("band", 1.58, 1.68, "#172036")])
    item("suit", "jacket", "#1a1a1e", "suit", [("stripe", -0.03, 0.03, "#eef2f7", 1.0, 1.62, "front"), ("stripe", -0.005, 0.005, "#8a1a2a", 1.0, 1.4, "front"), ("band", 1.58, 1.68, "#121215")])
    item("designer", "jacket", "#4b1d2a", "suit", [("stripe", -0.03, 0.03, "#1a1a1e", 1.0, 1.62, "front"), ("stripe", -0.005, 0.005, "#d9b36c", 1.0, 1.4, "front"), ("band", 1.58, 1.68, "#1a1a1e")])
    item("tux", "jacket", "#0d0d10", "suit", [("stripe", -0.03, 0.03, "#f7f7f7", 1.0, 1.62, "front"), ("stripe", -0.012, 0.012, "#0d0d10", 1.0, 1.4, "front"), ("stripe", -0.075, -0.035, "#1c1c22", 1.18, 1.58, "front"), ("stripe", 0.035, 0.075, "#1c1c22", 1.18, 1.58, "front"), ("band", 1.58, 1.68, "#1c1c22")])
    item("flannel", "hoodie", "#8a2a2a", "canvas", [("plaid", "#1b1b1f", "#6a1f1f", "#a33838"), ("band", 1.55, 1.64, "#1b1b1f")])
    return I

def bottoms():
    I = {}
    def item(id_, base, color, weave, steps, **meta): I[id_] = dict(base=base, color=color, weave=weave, steps=steps, **meta)
    item("khakis", "slacks", "#b9a27a", "canvas", [("band", 1.0, 1.06, "#a08b63")])
    item("jeans", "slacks", "#3a5684", "denim", [("band", 1.0, 1.06, "#2c4568"), ("legstripe", "#2c4568", 0.006)])
    item("blackjeans", "slacks", "#1d1d22", "denim", [("band", 1.0, 1.06, "#121215")])
    item("sweats", "sweats", "#4a4d55", "fleece", [("band", 1.0, 1.07, "#3c3f46"), ("band", 0.1, 0.17, "#3c3f46")])
    item("joggers", "joggers", "#1b1b1f", "nylon", [("legstripe", "#e9e7e1", 0.014), ("band", 1.0, 1.06, "#e9e7e1")])
    item("trackpants", "joggers", "#14213d", "nylon", [("legstripe", "#e9e7e1", 0.02), ("legstripe", "#c8323a", 0.008), ("band", 1.0, 1.06, "#e9e7e1")])
    item("shorts", "shorts", "#1b1b1f", "nylon", [("band", 0.97, 1.03, "#2a2a2f"), ("decal", logo_decal("drift", "#e9e7e1"), 0.1, 0.75, 0.05, 0.04, "front")])
    item("cargo", "shorts", "#5a5f4a", "canvas", [("band", 0.97, 1.03, "#4a4f3c"), ("stripe", 0.12, 0.2, "#4a4f3c", 0.6, 0.8, "all")])
    item("slacks", "slacks", "#2b2f3a", "suit", [("band", 1.0, 1.05, "#1f222b"), ("stripe", -0.003, 0.003, "#1f222b", 0.1, 1.0, "front")])
    item("tuxpants", "slacks", "#0d0d10", "suit", [("legstripe", "#2b2b33", 0.012)])
    item("plaidpants", "slacks", "#2a3a4a", "wool", [("plaid", "#1b1b1f", "#223040", "#324860")])
    return I

def shoes():
    I = {}
    def item(id_, base, color, weave, steps, **meta): I[id_] = dict(base=base, color=color, weave=weave, steps=steps, **meta)
    # shoe heights: sole 0..0.03, upper to ~0.1 (lows) / 0.14 (highs)
    item("sneakers", "lows", "#f2f2f0", "nylon", [("band", -0.01, 0.03, "#e4e2dc"), ("stripe", -0.4, 0.4, "#1b1b1f", 0.028, 0.034, "all")])
    item("runners", "lows", "#1b1b1f", "nylon", [("band", -0.01, 0.028, "#f2f2f0"), ("decal", logo_decal("drift", "#f08a1c"), -0.2, 0.08, 0.06, 0.03, "all"), ("decal", logo_decal("drift", "#f08a1c"), 0.2, 0.08, 0.06, 0.03, "all")])
    item("dress", "lows", "#2a1a12", "leather", [("band", -0.01, 0.025, "#120c08")])
    item("loafers", "lows", "#5a3a22", "leather", [("band", -0.01, 0.025, "#2a1a12"), ("band", 0.06, 0.075, "#c9a03e")])
    item("highs", "highs", "#f2f2f0", "canvas", [("band", -0.01, 0.035, "#e4e2dc"), ("stripe", -0.4, 0.4, "#b8262c", 0.03, 0.036, "all")])
    item("boots", "highs", "#4a2e1c", "leather", [("band", -0.01, 0.03, "#1b1b1f"), ("band", 0.12, 0.15, "#2c1b10")])
    item("slides", "lows", "#1b1b1f", "nylon", [("band", -0.01, 0.03, "#2a2a2f"), ("band", 0.03, 0.2, "#d9b36c")])
    item("icy", "highs", "#eef6ff", "nylon", [("band", -0.01, 0.035, "#cfe0f5"), ("stripe", -0.4, 0.4, "#9fc3df", 0.05, 0.06, "all")])
    return I

def paint(id_, spec):
    p = Painter(spec["base"], spec["color"], spec["weave"], seed=abs(hash(id_)) % 1000)
    for st in spec["steps"]:
        k = st[0]
        if k == "band": p.band(st[1], st[2], st[3], st[4] if len(st) > 4 else "all")
        elif k == "stripe": p.stripe_x(st[1], st[2], st[3], st[4] if len(st) > 4 else None, st[5] if len(st) > 5 else None, st[6] if len(st) > 6 else "all")
        elif k == "sleeves": p.sleeves(st[1], st[2] if len(st) > 2 else 0.24)
        elif k == "legstripe": p.legs_side_stripe(st[1], st[2] if len(st) > 2 else 0.012)
        elif k == "sleeve_stripes":
            # three stripes down each arm: arms run along x past the shoulder
            for i, c in enumerate((st[1], st[2], st[1])):
                lo = 1.52 - i * 0.03; p.where((np.abs(p.x) > 0.26) & (p.z > lo - 0.012) & (p.z < lo + 0.012) & (p.N[..., 1] < 0.2), c, 0.8)
        elif k == "decal": p.decal(st[1], st[2], st[3], st[4], st[5], st[6] if len(st) > 6 else "front")
        elif k == "argyle": pattern_argyle(p, st[1], st[2], st[3], st[4], st[5])
        elif k == "plaid": pattern_plaid(p, st[1], st[2], st[3])
    return p.finish()

ALL = {"top": tops(), "bottom": bottoms(), "shoes": shoes()}

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "out/Clothes"; os.makedirs(out, exist_ok=True)
    only = set(sys.argv[2].split(",")) if len(sys.argv) > 2 else None
    meta = {}
    for slot, items in ALL.items():
        for id_, spec in items.items():
            b = BASES[spec["base"]]
            meta[slot + ":" + id_] = {"slot": b[0], "recipe": b[1], "wardrobeSlot": b[2], "textures": b[3]}
            if only and id_ not in only: continue
            img = paint(id_, spec)
            cv2.imwrite(os.path.join(out, f"{slot}_{id_}.png"), img)
            print(slot, id_)
    json.dump(meta, open(os.path.join(out, "clothes.json"), "w"), indent=1)
