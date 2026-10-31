# Miami out of the window: three painted views for the sets and homes.
#   window_miami.png  Brickell towers over the bay at dusk, palms at the edges        (office, homes)
#   window_deco.png   Ocean Drive at night: pastel deco fronts with neon, a palm      (diner)
#   window_palms.png  a card-room parking lot at night: palms, a neon sign's glow     (card room)
#   python3 miami.py [out dir]
import numpy as np, cv2, os, sys, math
out = sys.argv[1] if len(sys.argv) > 1 else "tex"; os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(305)
W, H = 1024, 576

def hexc(h): h = h.lstrip('#'); return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)
def save(n, img): cv2.imwrite(os.path.join(out, n), cv2.cvtColor(np.clip(img * 255, 0, 255).astype(np.uint8), cv2.COLOR_RGB2BGR))
def C(h): c = hexc(h) * 255; return (int(c[0]), int(c[1]), int(c[2]))
def gradient(stops):
    """vertical gradient from (t, hex) stops, t in 0..1 top to bottom"""
    img = np.zeros((H, W, 3), np.float32); ys = np.linspace(0, 1, H)
    ts = [s[0] for s in stops]; cs = [hexc(s[1]) for s in stops]
    for ch in range(3): img[:, :, ch] = np.interp(ys, ts, [c[ch] for c in cs])[:, None]
    return img
def to8(img): return np.clip(img * 255, 0, 255).astype(np.uint8)
def glow(img8, mask, color, sigma, amount):
    """add a soft coloured glow where mask is set"""
    m = cv2.GaussianBlur(mask.astype(np.float32) / 255, (0, 0), sigma)[..., None]
    f = img8.astype(np.float32) / 255; f = f + m * amount * np.array(hexc(color))[None, None, :]
    return to8(f)

def palm(img, x, y, h, lean=0.0, scale=1.0, col=(8, 10, 14)):
    """a palm silhouette: a curved trunk, a crown of drooping fronds"""
    pts = []
    for i in range(12):
        t = i / 11; pts.append((int(x + lean * h * t * t), int(y - h * t)))
    for i in range(11): cv2.line(img, pts[i], pts[i + 1], col, max(2, int(7 * scale * (1 - 0.5 * i / 11))), cv2.LINE_AA)
    top = pts[-1]
    for k in range(9):
        a = -math.pi / 2 + (k - 4) * 0.42 + rng.uniform(-0.12, 0.12); L = 62 * scale * rng.uniform(0.8, 1.15)
        fr = [top]
        for j in range(1, 8):
            t = j / 7; fr.append((int(top[0] + math.cos(a) * L * t), int(top[1] + math.sin(a) * L * t + 46 * scale * t * t)))
        for j in range(7):
            cv2.line(img, fr[j], fr[j + 1], col, max(1, int(4 * scale * (1 - j / 7))), cv2.LINE_AA)
            # leaflets
            if j > 1:
                n = (fr[j + 1][0] - fr[j][0], fr[j + 1][1] - fr[j][1]); ln = math.hypot(*n) + 1e-6; px, py = -n[1] / ln, n[0] / ln
                for s in (-1, 1):
                    cv2.line(img, fr[j], (int(fr[j][0] + px * s * 13 * scale + n[0] * 0.6), int(fr[j][1] + py * s * 13 * scale + n[1] * 0.6 + 6 * scale)), col, 1, cv2.LINE_AA)

# ------------------------------------------------------------ Brickell at dusk
sky = gradient([(0, '#2a1f5e'), (0.3, '#6a3a86'), (0.52, '#d9637c'), (0.62, '#ff9a5c'), (0.68, '#ffd08a')])
img = to8(sky); horizon = int(H * 0.66)
# the sun's glow (the disc comes after the water, so the water only carries a streak of it)
img = glow(img, cv2.circle(np.zeros((H, W), np.uint8), (int(W * 0.62), horizon - 10), 70, 255, -1), '#ff9a5c', 60, 0.9)
# a few streaks of cloud catching the light
for k in range(7):
    y = int(rng.uniform(H * 0.12, H * 0.5)); x = int(rng.uniform(0, W)); ln = int(rng.uniform(120, 360)); th = int(rng.uniform(3, 9))
    cv2.ellipse(img, (x, y), (ln, th), 0, 0, 360, C('#ff8fb0') if y > H * 0.3 else C('#8a5aa8'), -1, cv2.LINE_AA)
img = cv2.GaussianBlur(img, (0, 0), 1.2)
# the water below the horizon: the sky mirrored, darker, with ripples
water = cv2.flip(img[:horizon], 0)[:H - horizon].astype(np.float32) * 0.62
yy, xx = np.mgrid[0:H - horizon, 0:W].astype(np.float32)
ripple = 1 + 0.08 * np.sin(yy * 0.9 + xx * 0.05) * np.sin(yy * 0.23 + 1.7) + 0.05 * rng.normal(0, 1, (H - horizon, W))
water *= ripple[..., None]; water = cv2.GaussianBlur(water, (0, 0), 1.0)
img[horizon:] = np.clip(water, 0, 255).astype(np.uint8)
# the sun itself, half down, and its streak on the water
cv2.circle(img, (int(W * 0.62), horizon - 10), 58, C('#ffe6a8'), -1, cv2.LINE_AA); cv2.rectangle(img, (0, horizon), (W, horizon + 50), (0, 0, 0), -1) if False else None
streak = np.zeros((H, W), np.uint8)
for k in range(40): cv2.line(streak, (int(W * 0.62 - 40 + rng.uniform(-30, 30)), horizon + int(k * 3.4)), (int(W * 0.62 + 40 + rng.uniform(-30, 30)), horizon + int(k * 3.4)), 255, 1)
img = glow(img, streak, '#ffd08a', 4, 0.8)
# towers: Brickell's wall of glass, in silhouette against the sunset with lit windows
towers = []
x = 40
while x < W - 60:
    w = int(rng.uniform(34, 86)); h = int(rng.uniform(90, 290)) if abs(x - W * 0.42) > 160 else int(rng.uniform(180, 330))
    towers.append((x, w, h)); x += w + int(rng.uniform(4, 22))
base = np.zeros((H, W, 3), np.uint8)
for (x, w, h) in towers:
    top = horizon - h; col = C('#1a1530') if rng.uniform() < 0.6 else C('#241a3a')
    cv2.rectangle(img, (x, top), (x + w, horizon), col, -1)
    if rng.uniform() < 0.35: cv2.rectangle(img, (x + w // 3, top - 14), (x + 2 * w // 3, top), col, -1)   # a crown
    # lit windows, dense on the lower floors, in warm and cool
    for wy in range(top + 8, horizon - 6, 9):
        for wx in range(x + 4, x + w - 4, 7):
            if rng.uniform() < 0.42: cv2.rectangle(img, (wx, wy), (wx + 3, wy + 4), C('#ffd79a') if rng.uniform() < 0.7 else C('#9ad7ff'), -1)
    # a neon crown line on some towers (Miami at night does this)
    if rng.uniform() < 0.3:
        c = ['#ff5fa2', '#4fd7e8', '#ffb347', '#b06cff'][int(rng.integers(0, 4))]
        cv2.line(img, (x, top), (x + w, top), C(c), 2, cv2.LINE_AA); base[top - 2:top + 3, x:x + w] = 255
img = glow(img, base[..., 0], '#ff7fc0', 7, 0.6)
# reflections of the towers' lights on the water
rh = H - horizon; refl = cv2.flip(img[horizon - rh:horizon], 0).astype(np.float32) * 0.35
refl = cv2.GaussianBlur(refl, (0, 0), sigmaX=1, sigmaY=6); refl *= (1 + 0.1 * np.sin(np.arange(rh)[:, None, None] * 0.7)) * np.linspace(1, 0.3, rh)[:, None, None]
img[horizon:] = np.clip(img[horizon:].astype(np.float32) * 0.8 + refl, 0, 255).astype(np.uint8)
# palms at the edges, in the foreground
palm(img, 70, H + 10, 420, lean=0.25, scale=1.5); palm(img, W - 60, H + 30, 470, lean=-0.3, scale=1.6); palm(img, W - 150, H + 40, 300, lean=-0.1, scale=1.1)
save("window_miami.png", img.astype(np.float32) / 255)

# ------------------------------------------------------------ Ocean Drive at night
sky = gradient([(0, '#07081c'), (0.5, '#141a3f'), (0.8, '#22284f')])
img = to8(sky); ground = int(H * 0.82)
# the street and a sidewalk
cv2.rectangle(img, (0, ground), (W, H), C('#1b1a22'), -1); cv2.rectangle(img, (0, ground), (W, ground + 10), C('#5a5566'), -1)
# deco hotel fronts: pastel blocks with stepped tops, porthole windows, neon outlines
fronts = [(0, 230, '#8fd3c7', '#ff5fa2'), (240, 190, '#f6b4c6', '#4fd7e8'), (440, 260, '#ffd58a', '#ff6a8a'), (710, 200, '#b9e0f5', '#ffd166'), (920, 160, '#f7c8a0', '#8fd3c7')]
neon = np.zeros((H, W), np.uint8)
for (x, w, col, nc) in fronts:
    h = int(rng.uniform(180, 300)); top = ground - h
    cv2.rectangle(img, (x, top), (x + w, ground), C(col), -1)
    # the night dims the pastel; the neon lifts it back
    img[top:ground, x:x + w] = (img[top:ground, x:x + w] * 0.45).astype(np.uint8)
    # stepped parapet
    cv2.rectangle(img, (x + w // 4, top - 18), (x + 3 * w // 4, top), C(col), -1); img[top - 18:top, x + w // 4:x + 3 * w // 4] = (img[top - 18:top, x + w // 4:x + 3 * w // 4] * 0.45).astype(np.uint8)
    # racing stripes and portholes
    for k in range(3): cv2.line(img, (x + 6, top + 30 + k * 7), (x + w - 6, top + 30 + k * 7), C('#f4f1ea'), 1, cv2.LINE_AA)
    for wy in range(top + 60, ground - 40, 44):
        for wx in range(x + 24, x + w - 20, 40):
            lit = rng.uniform() < 0.5
            cv2.rectangle(img, (wx, wy), (wx + 16, wy + 24), C('#ffe9b0') if lit else C('#1a1f3a'), -1)
    cv2.circle(img, (x + w // 2, top + 14), 6, C('#ffe9b0'), -1, cv2.LINE_AA)
    # neon: an outline up the sides and along the parapet, plus a vertical sign
    cv2.polylines(neon, [np.array([(x + 3, ground), (x + 3, top + 3), (x + w // 4, top + 3), (x + w // 4, top - 15), (x + 3 * w // 4, top - 15), (x + 3 * w // 4, top + 3), (x + w - 3, top + 3), (x + w - 3, ground)])], False, 255, 2)
    cv2.rectangle(neon, (x + w - 30, top + 50), (x + w - 14, top + 150), 255, 2)
    img[neon > 0] = C(nc) if False else img[neon > 0]
    m = neon.copy(); neon[:] = 0
    img[m > 0] = np.array(C(nc), np.uint8)
    img = glow(img, m, nc, 9, 0.9)
# awning stripes and a parked car silhouette
cv2.rectangle(img, (470, ground - 40), (640, ground - 30), C('#ff5fa2'), -1)
cv2.rectangle(img, (300, ground - 28), (420, ground + 2), C('#0e0e14'), -1); cv2.ellipse(img, (360, ground - 28), (52, 16), 0, 180, 360, C('#0e0e14'), -1, cv2.LINE_AA)
for cx in (316, 404): cv2.circle(img, (cx, ground + 2), 9, C('#06060a'), -1, cv2.LINE_AA)
# streetlight pools and a palm
for sx in (150, 820): img = glow(img, cv2.circle(np.zeros((H, W), np.uint8), (sx, ground - 160), 6, 255, -1), '#ffd79a', 40, 1.1); cv2.circle(img, (sx, ground - 160), 5, C('#fff3cf'), -1, cv2.LINE_AA); cv2.line(img, (sx, ground - 160), (sx, ground), C('#2a2a33'), 3)
palm(img, 660, ground + 6, 300, lean=0.18, scale=1.15, col=(6, 7, 12))
img = cv2.GaussianBlur(img, (0, 0), 0.6)
save("window_deco.png", img.astype(np.float32) / 255)

# ------------------------------------------------------------ the card room's lot
sky = gradient([(0, '#090a1a'), (0.6, '#17183a'), (0.85, '#2a2648')])
img = to8(sky); ground = int(H * 0.78)
cv2.rectangle(img, (0, ground), (W, H), C('#15141c'), -1)
for k in range(6): cv2.line(img, (60 + k * 170, ground + 40), (120 + k * 170, H), C('#3a3848'), 2)   # parking bays
# a low building with a neon sign: ALL-NIGHT POKER
cv2.rectangle(img, (520, ground - 150), (W, ground), C('#2a2232'), -1)
sign = np.zeros((H, W), np.uint8); cv2.putText(sign, "POKER", (600, ground - 60), cv2.FONT_HERSHEY_DUPLEX, 2.6, 255, 3, cv2.LINE_AA)
cv2.putText(sign, "24 HRS", (640, ground - 20), cv2.FONT_HERSHEY_SIMPLEX, 1.1, 255, 2, cv2.LINE_AA)
img[sign > 0] = np.array(C('#ff5fa2'), np.uint8); img = glow(img, sign, '#ff5fa2', 14, 1.2)
cv2.rectangle(img, (560, ground - 150), (W, ground - 146), C('#4fd7e8'), -1); img = glow(img, cv2.rectangle(np.zeros((H, W), np.uint8), (560, ground - 150), (W, ground - 146), 255, -1), '#4fd7e8', 10, 0.8)
# cars under the sodium light
for cx, cw in ((140, 130), (330, 120)):
    cv2.rectangle(img, (cx, ground - 30), (cx + cw, ground + 4), C('#1a1a22'), -1); cv2.ellipse(img, (cx + cw // 2, ground - 30), (cw // 2 - 8, 18), 0, 180, 360, C('#1a1a22'), -1, cv2.LINE_AA)
    cv2.rectangle(img, (cx + 18, ground - 44), (cx + cw - 30, ground - 30), C('#2a2d3a'), -1)
img = glow(img, cv2.circle(np.zeros((H, W), np.uint8), (250, ground - 190), 8, 255, -1), '#ffb45c', 70, 1.3)
cv2.line(img, (250, ground - 190), (250, ground), C('#2a2a33'), 4); cv2.circle(img, (250, ground - 190), 7, C('#fff0c8'), -1, cv2.LINE_AA)
# the moon and palms
cv2.circle(img, (880, 90), 22, C('#f2efe8'), -1, cv2.LINE_AA); img = glow(img, cv2.circle(np.zeros((H, W), np.uint8), (880, 90), 26, 255, -1), '#cfd6ff', 30, 0.5)
palm(img, 60, ground + 10, 330, lean=0.2, scale=1.2, col=(6, 7, 12)); palm(img, 470, ground + 8, 260, lean=-0.15, scale=0.95, col=(6, 7, 12)); palm(img, 1000, ground + 20, 380, lean=-0.3, scale=1.4, col=(6, 7, 12))
img = cv2.GaussianBlur(img, (0, 0), 0.6)
save("window_palms.png", img.astype(np.float32) / 255)
print("ok")
