# Synthwave bay drive: painted as separate layers so the cutscene can scroll them at different speeds.
#   sky.png    sky, sun, clouds, water (static)
#   city.png   skyline + far shore lights + reflections (drifts slowly)
#   road.png   causeway road, rail, street lights (tiles horizontally, scrolls at car speed)
#   palms.png  foreground palm silhouettes (tiles, scrolls fastest)
import numpy as np, cv2, math, random, sys, os
W, H = 2048, 1152
HOR = int(H * 0.58)          # horizon
ROAD_TOP = int(H * 0.80)     # where the causeway starts
OUT = sys.argv[1] if len(sys.argv) > 1 else "."
rng = np.random.default_rng(7); random.seed(7)

def hexc(h, a=1.0):
    h = h.lstrip('#'); return np.array([int(h[i:i+2], 16) / 255 for i in (0, 2, 4)] + [a], np.float32)
def lerp(a, b, t): return a + (b - a) * t
def grad(stops, n):
    """stops: [(t, hex)] -> n x 3"""
    ts = np.linspace(0, 1, n); out = np.zeros((n, 3), np.float32)
    for i in range(len(stops) - 1):
        t0, c0 = stops[i]; t1, c1 = stops[i + 1]
        m = (ts >= t0) & (ts <= t1); k = ((ts[m] - t0) / max(1e-6, t1 - t0))[:, None]
        out[m] = lerp(hexc(c0)[:3], hexc(c1)[:3], k)
    return out
def over(dst, src):
    """alpha-composite RGBA float src over dst (both HxWx4, straight alpha)"""
    a = src[..., 3:4]; da = dst[..., 3:4]
    oa = a + da * (1 - a)
    rgb = (src[..., :3] * a + dst[..., :3] * da * (1 - a)) / np.maximum(oa, 1e-6)
    return np.concatenate([rgb, oa], -1)
def layer(): return np.zeros((H, W, 4), np.float32)
def blur(img, s): return cv2.GaussianBlur(img, (0, 0), s)
def save(img, name):
    o = np.clip(img * 255 + 0.5, 0, 255).astype(np.uint8)
    cv2.imwrite(os.path.join(OUT, name), cv2.cvtColor(o, cv2.COLOR_RGBA2BGRA))
def fill_poly(lay, pts, col, aa=True):
    m = np.zeros((H, W), np.uint8)
    cv2.fillPoly(m, [np.array(pts, np.int32).reshape(-1, 1, 2)], 255, cv2.LINE_AA if aa else cv2.LINE_8)
    a = (m / 255.0)[..., None] * col[3]
    lay[:] = over(lay, np.concatenate([np.broadcast_to(col[:3], (H, W, 3)), a], -1))
def glow(lay, mask, col, sigma, strength=1.0):
    g = blur(mask.astype(np.float32), sigma) * strength
    g = np.clip(g, 0, 1)[..., None] * col[3]
    lay[:] = over(lay, np.concatenate([np.broadcast_to(col[:3], (H, W, 3)), g], -1))

# ------------------------------------------------------------------ sky + sun + water
sky = np.zeros((H, W, 4), np.float32); sky[..., 3] = 1
g = grad([(0, '#16052e'), (0.22, '#3b0b5c'), (0.48, '#8e1667'), (0.72, '#e2347a'), (0.9, '#ff6a6a'), (1.0, '#ff9a6b')], HOR)
sky[:HOR, :, :3] = g[:, None, :]
# soft cloud bands (purple, lit pink underneath)
for i in range(14):
    cy = rng.uniform(0.12, 0.5) * HOR; cx = rng.uniform(0, W); w = rng.uniform(260, 700); h = rng.uniform(10, 28)
    m = np.zeros((H, W), np.float32)
    for j in range(7):
        ex = cx + rng.uniform(-w / 2, w / 2); ey = cy + rng.uniform(-h, h * 0.4)
        cv2.ellipse(m, (int(ex), int(ey)), (int(rng.uniform(w * 0.15, w * 0.35)), int(h)), 0, 0, 360, 1, -1, cv2.LINE_AA)
    m = blur(m, 6)
    body = np.concatenate([np.broadcast_to(hexc('#4a1466')[:3], (H, W, 3)), (m * 0.55)[..., None]], -1)
    sky = over(sky, body)
    rim = np.clip(m - np.roll(blur(m, 3), -6, 0), 0, 1)  # lit underside
    sky = over(sky, np.concatenate([np.broadcast_to(hexc('#ff6fa8')[:3], (H, W, 3)), (rim * 0.8)[..., None]], -1))
# stars up top
for i in range(260):
    x, y = rng.uniform(0, W), rng.uniform(0, HOR * 0.4); r = rng.uniform(0.6, 1.6)
    m = np.zeros((H, W), np.float32); cv2.circle(m, (int(x), int(y)), int(r) + 1, 1, -1, cv2.LINE_AA)
    sky = over(sky, np.concatenate([np.ones((H, W, 3), np.float32), (m * rng.uniform(0.25, 0.8) * (1 - y / (HOR * 0.4)))[..., None]], -1))
# the sun: big disc, yellow to hot pink, stripes cut through its lower half
SX, SY, SR = int(W * 0.5), int(HOR - H * 0.2), int(H * 0.235)
yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
d = np.sqrt((xx - SX) ** 2 + (yy - SY) ** 2)
halo = np.exp(-np.maximum(d - SR, 0) / (H * 0.12)) * (d > SR * 0.98)
sky = over(sky, np.concatenate([np.broadcast_to(hexc('#ff4f8b')[:3], (H, W, 3)), (halo * 0.55)[..., None]], -1))
disc = np.clip(SR - d + 1.5, 0, 1)
t = np.clip((yy - (SY - SR)) / (2 * SR), 0, 1)
suncol = np.stack([lerp(1.0, 1.0, t), lerp(0.93, 0.36, t), lerp(0.42, 0.55, t)], -1)
# stripes: gaps that thicken towards the bottom, only below the sun's middle
v = (yy - SY) / SR
stripe = np.ones_like(v)
for k in range(8):
    y0 = 0.1 + k * 0.12; th = 0.012 + k * 0.009
    stripe *= 1 - np.clip(1 - np.abs(v - y0) / th, 0, 1) ** 0.25 * (np.abs(v - y0) < th)
disc = disc * stripe
sky = over(sky, np.concatenate([suncol, disc[..., None]], -1))
# horizon haze
hz = np.exp(-np.abs(yy - HOR) / 18) * 0.5
sky = over(sky, np.concatenate([np.broadcast_to(hexc('#ff8fb0')[:3], (H, W, 3)), hz[..., None]], -1))
# water below the horizon: deep magenta to violet, sun column with ripple lines
wg = grad([(0, '#c42b7a'), (0.35, '#7a1a6a'), (1, '#250a3a')], H - HOR)
sky[HOR:, :, :3] = wg[:, None, :]
wy = yy - HOR
col = np.exp(-((xx - SX) / (SR * (0.55 + wy / H * 1.4))) ** 2)
rip = (np.sin(wy * 0.9 + np.sin(xx * 0.013) * 3) > 0.15).astype(np.float32)
ripple = col * rip * np.exp(-wy / (H * 0.25)) * (yy >= HOR)
sky = over(sky, np.concatenate([np.broadcast_to(hexc('#ffd36b')[:3], (H, W, 3)), (np.clip(ripple * 1.1, 0, 1))[..., None]], -1))
lines = ((np.sin(wy * 0.55 + xx * 0.002) > 0.96) * (yy > HOR)).astype(np.float32) * 0.08
sky = over(sky, np.concatenate([np.broadcast_to(hexc('#ff7ab8')[:3], (H, W, 3)), lines[..., None]], -1))
save(sky, 'sky.png')

# ------------------------------------------------------------------ city: skyline + reflection + far shore lights
city = layer()
base_y = HOR + 4
def row(n_seed, hmin, hmax, wmin, wmax, tall_p, sun_low):
    r = np.random.default_rng(n_seed); out = []; x = -60
    while x < W + 60:
        w = int(r.integers(wmin, wmax)); hgt = int(r.integers(hmin, hmax))
        if r.random() < tall_p: hgt = int(r.integers(int(hmax * 1.3), int(hmax * 1.9)))
        if sun_low and abs(x + w / 2 - SX) < SR * 0.9: hgt = int(hgt * 0.55)
        out.append((x, w, hgt, r.random())); x += w + int(r.integers(-10, 14))
    return out
def shape(x0, w, top, kind):
    b = base_y
    if kind < 0.12: return [(x0, b), (x0, top + 26), (x0 + w // 2, top - 14), (x0 + w, top + 26), (x0 + w, b)]                 # pointed
    if kind < 0.24: return [(x0, b), (x0, top), (x0 + int(w * 0.65), top), (x0 + w, top + 34), (x0 + w, b)]                     # slanted
    if kind < 0.36: return [(x0, b), (x0, top + 30), (x0 + 8, top + 30), (x0 + 8, top + 12), (x0 + w - 8, top + 12), (x0 + w - 8, top), (x0 + w, top), (x0 + w, b)]  # stepped
    return [(x0, b), (x0, top), (x0 + w, top), (x0 + w, b)]
def draw_row(towers, body, rimcol, winp, haze_amt, light):
    global city
    cm = np.zeros((H, W), np.float32); rim = np.zeros((H, W), np.float32); wn = np.zeros((H, W), np.float32)
    for (x0, w, hgt, kind) in towers:
        top = base_y - hgt; pts = shape(x0, w, top, kind)
        m = np.zeros((H, W), np.uint8); cv2.fillPoly(m, [np.array(pts, np.int32)], 255, cv2.LINE_AA); cm = np.maximum(cm, m / 255.0)
        edge = x0 + w if x0 + w / 2 < SX else x0
        cv2.line(rim, (int(edge), int(top + 4)), (int(edge), base_y), 1, 2, cv2.LINE_AA)
        cv2.polylines(rim, [np.array(pts[1:-1], np.int32)], False, 0.8, 2, cv2.LINE_AA)
        if kind > 0.92 and hgt > 200: cv2.line(rim, (x0 + w // 2, top), (x0 + w // 2, top - 60), 1, 2, cv2.LINE_AA); cv2.circle(wn, (x0 + w // 2, top - 62), 3, 1, -1, cv2.LINE_AA)
        if light:
            cols = max(2, w // 10); rows = max(2, hgt // 12)
            band = kind > 0.7   # some towers light in horizontal bands
            for j2 in range(rows):
                lit_row = (not band) or (j2 % 3 == 0)
                for i2 in range(cols):
                    if lit_row and np.random.random() < winp:
                        wx = x0 + 5 + i2 * (w - 10) / cols; wy2 = top + 12 + j2 * 12
                        if wy2 > base_y - 6: continue
                        cv2.rectangle(wn, (int(wx), int(wy2)), (int(wx + 3), int(wy2 + 4)), np.random.uniform(0.5, 1.0), -1)
    sil = np.concatenate([np.broadcast_to(body[:3], (H, W, 3)), cm[..., None]], -1)
    hz = np.clip((yy - (base_y - 420)) / 420, 0, 1) * haze_amt
    sil[..., :3] = lerp(sil[..., :3], hexc('#7a2378')[:3], hz[..., None])
    city = over(city, sil)
    city = over(city, np.concatenate([np.broadcast_to(rimcol[:3], (H, W, 3)), (rim * cm.clip(0, 1) * rimcol[3])[..., None]], -1))
    if light:
        wm = wn * np.maximum(cm, (yy < base_y))
        wcol = np.where((np.random.random((H, W)) < 0.22)[..., None], hexc('#7fe9ff')[:3], hexc('#ffd27a')[:3])
        city = over(city, np.concatenate([wcol, np.clip(wm, 0, 1)[..., None]], -1))
        glow(city, wm, hexc('#ffb36b', 0.55), 3, 1.5)
np.random.seed(3)
draw_row(row(11, 70, 170, 40, 90, 0.2, False), hexc('#5a1a66'), hexc('#ff8fd0', 0.35), 0.18, 0.7, True)   # far, hazy
draw_row(row(5, 90, 220, 60, 130, 0.28, True), hexc('#22093a'), hexc('#ff6ac1', 0.9), 0.36, 0.3, True)    # near
# far shore: a thin dark strip with a string of lights
shore = np.zeros((H, W), np.float32); cv2.rectangle(shore, (0, base_y - 3), (W, base_y + 6), 1, -1)
city = over(city, np.concatenate([np.broadcast_to(hexc('#1d0833')[:3], (H, W, 3)), shore[..., None]], -1))
dots = np.zeros((H, W), np.float32)
for x in range(0, W, 7):
    if rng.random() < 0.7: cv2.circle(dots, (x + int(rng.integers(0, 4)), base_y + int(rng.integers(-1, 3))), 1, rng.uniform(0.5, 1), -1, cv2.LINE_AA)
city = over(city, np.concatenate([np.broadcast_to(hexc('#ffcf7a')[:3], (H, W, 3)), dots[..., None]], -1))
glow(city, dots, hexc('#ff9a5a', 0.7), 4, 2.0)
# reflection: flip the city above the shore into the water, stretched, rippled, faded
above = city[:base_y].copy()
ref = above[::-1]
ref = cv2.resize(ref, (W, int(ref.shape[0] * 0.75)))
rh = min(ref.shape[0], H - base_y - 8)
ref = ref[:rh]
shift = (np.sin(np.arange(rh) * 0.7) * 3 + np.sin(np.arange(rh) * 0.13) * 5).astype(np.int32)
for r in range(rh): ref[r] = np.roll(ref[r], shift[r], 0)
fade = np.exp(-np.arange(rh) / (rh * 0.45))[:, None] * 0.7
ref[..., 3] *= fade
ref = blur(ref, 1.2)
refl = layer(); refl[base_y + 8:base_y + 8 + rh] = ref
city = over(refl, city)
save(city, 'city.png')

# ------------------------------------------------------------------ road: causeway, tiles horizontally (period = W)
road = layer()
RY = ROAD_TOP
# rail / seawall
fill_poly(road, [(0, RY - 10), (W, RY - 10), (W, RY + 6), (0, RY + 6)], hexc('#1a0a2a'))
# asphalt
asp = np.zeros((H, W, 4), np.float32); asp[RY + 6:, :, :3] = grad([(0, '#241433'), (1, '#0d0714')], H - RY - 6)[:, None, :]; asp[RY + 6:, :, 3] = 1
road = over(road, asp)
# wet sheen: sun streak on the road
sheen = np.exp(-((xx - SX) / 260) ** 2) * (yy > RY + 6) * np.exp(-(yy - RY) / 120) * 0.35
road = over(road, np.concatenate([np.broadcast_to(hexc('#ff5fa0')[:3], (H, W, 3)), sheen[..., None]], -1))
# neon edge lines
ln = np.zeros((H, W), np.float32)
cv2.line(ln, (0, RY + 7), (W, RY + 7), 1, 3, cv2.LINE_AA)
road = over(road, np.concatenate([np.broadcast_to(hexc('#ff3fb4')[:3], (H, W, 3)), ln[..., None]], -1))
glow(road, ln, hexc('#ff3fb4', 0.9), 8, 2.2)
ln2 = np.zeros((H, W), np.float32); cv2.line(ln2, (0, H - 26), (W, H - 26), 1, 3, cv2.LINE_AA)
road = over(road, np.concatenate([np.broadcast_to(hexc('#39e6ff')[:3], (H, W, 3)), ln2[..., None]], -1))
glow(road, ln2, hexc('#39e6ff', 0.8), 8, 2.0)
# lane dashes (period divides W)
dash = np.zeros((H, W), np.float32); lane_y = int((RY + H) / 2) + 18
for x in range(0, W, 128): cv2.rectangle(dash, (x, lane_y), (x + 64, lane_y + 5), 1, -1)
road = over(road, np.concatenate([np.broadcast_to(hexc('#ffe9a8')[:3], (H, W, 3)), (dash * 0.85)[..., None]], -1))
# rail posts + street lights (period 512 -> 4 per tile)
posts = np.zeros((H, W), np.float32); heads = np.zeros((H, W), np.float32)
for x in range(0, W, 64): cv2.rectangle(posts, (x, RY - 22), (x + 4, RY - 4), 1, -1)
cv2.rectangle(posts, (0, RY - 24), (W, RY - 21), 1, -1)
for x in range(200, W, 512):
    cv2.rectangle(posts, (x, RY - 300), (x + 7, RY), 1, -1)                      # pole
    cv2.ellipse(posts, (x - 20, RY - 300), (30, 30), 0, 180, 270, 1, 5, cv2.LINE_AA)  # arm
    cv2.rectangle(posts, (x - 66, RY - 334), (x - 16, RY - 324), 1, -1)
    cv2.rectangle(heads, (x - 62, RY - 324), (x - 20, RY - 319), 1, -1)
road = over(road, np.concatenate([np.broadcast_to(hexc('#12061f')[:3], (H, W, 3)), posts[..., None]], -1))
road = over(road, np.concatenate([np.broadcast_to(hexc('#fff1c9')[:3], (H, W, 3)), heads[..., None]], -1))
glow(road, heads, hexc('#ffb36b', 0.9), 14, 3.0)
# light pools on the road under each lamp
pool = np.zeros((H, W), np.float32)
for x in range(200, W, 512): cv2.ellipse(pool, (x - 40, RY + 60), (150, 34), 0, 0, 360, 1, -1, cv2.LINE_AA)
road = over(road, np.concatenate([np.broadcast_to(hexc('#ff9a6b')[:3], (H, W, 3)), (blur(pool, 22) * 0.35)[..., None]], -1))
# make it tile: blend the seam
save(road, 'road.png')

# ------------------------------------------------------------------ palms: foreground silhouettes, tile (period W)
palms = layer()
def palm(lay, bx, by, hgt, lean, scale, col, seed):
    r = random.Random(seed)
    m = np.zeros((H, W), np.uint8)
    pts = []
    for i in range(61):
        t = i / 60; x = bx + lean * hgt * (t ** 1.7) + math.sin(t * 3) * 8 * scale; y = by - hgt * t
        pts.append((x, y))
    for i in range(60):
        w = int(scale * (26 - 13 * i / 60))
        cv2.line(m, (int(pts[i][0]), int(pts[i][1])), (int(pts[i + 1][0]), int(pts[i + 1][1])), 255, max(2, w), cv2.LINE_AA)
    top = pts[-1]
    cv2.circle(m, (int(top[0]), int(top[1])), int(14 * scale), 255, -1, cv2.LINE_AA)
    n = 14
    for k in range(n):
        ang = math.radians(-180 + k * (360 / n) * 0.55 + r.uniform(-10, 10)) if k % 2 == 0 else math.radians(-20 - k * (360 / n) * 0.5 + r.uniform(-10, 10))
        L = scale * r.uniform(190, 290); droop = r.uniform(0.45, 0.9)
        prev = top; spine = []
        for s_ in range(1, 41):
            t = s_ / 40
            x = top[0] + math.cos(ang) * L * t
            y = top[1] + math.sin(ang) * L * t * 0.55 + (L * droop) * t * t
            cv2.line(m, (int(prev[0]), int(prev[1])), (int(x), int(y)), 255, max(1, int(scale * (5 - 4 * t))), cv2.LINE_AA)
            spine.append((x, y, prev)); prev = (x, y)
        # leaflets: long, thin, hanging down and outward, densest mid-frond
        for (x, y, p0) in spine[2::1]:
            dx, dy = x - p0[0], y - p0[1]; ln = math.hypot(dx, dy) + 1e-6; dx /= ln; dy /= ln
            t = math.hypot(x - top[0], y - top[1]) / L
            lf = scale * 62 * math.sin(math.pi * min(1.0, t * 1.05)) ** 0.8
            for sg in (-1, 1):
                nx, ny = -dy * sg, dx * sg
                ex = x + (nx * 0.55 + dx * 0.45) * lf; ey = y + (ny * 0.55 + dy * 0.45) * lf + lf * 0.55
                cv2.line(m, (int(x), int(y)), (int(ex), int(ey)), 255, max(1, int(scale * 2.2)), cv2.LINE_AA)
    a = (m / 255.0)[..., None]
    lay[:] = over(lay, np.concatenate([np.broadcast_to(col[:3], (H, W, 3)), a], -1))
pc = hexc('#0a0312')
palm(palms, 150, H + 30, 820, 0.16, 1.55, pc, 1)
palm(palms, 260, H + 30, 620, -0.14, 1.25, pc, 2)
palm(palms, 1180, H + 30, 900, -0.08, 1.7, pc, 3)
palm(palms, 1900, H + 30, 700, 0.12, 1.4, pc, 4)
# low bushes along the bottom
bush = np.zeros((H, W), np.float32)
for x in range(0, W, 26):
    cv2.ellipse(bush, (x, H - 6), (int(rng.uniform(26, 50)), int(rng.uniform(18, 44))), 0, 180, 360, 1, -1, cv2.LINE_AA)
palms = over(palms, np.concatenate([np.broadcast_to(pc[:3], (H, W, 3)), (bush * (rng.random() * 0 + 1))[..., None] * ((xx < 420) | ((xx > 1080) & (xx < 1320)) | (xx > 1820))[..., None]], -1))
# rim light from the sun on palm edges (thin pink)
pa = palms[..., 3]
edge = np.clip(pa - np.roll(pa, 3, 1), 0, 1) * (xx < SX) + np.clip(pa - np.roll(pa, -3, 1), 0, 1) * (xx >= SX)
palms = over(palms, np.concatenate([np.broadcast_to(hexc('#ff4fa0')[:3], (H, W, 3)), (edge * 0.6)[..., None]], -1))
save(palms, 'palms.png')

# ------------------------------------------------------------------ preview composite
comp = sky.copy()
for L in (city, road, palms): comp = over(comp, L)
save(comp, 'preview.png')
print('ok')
