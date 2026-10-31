# Pictures for the walls of the nicer places, plus water for the island pool. -> <out>/art_0..5.png, pool_water.png
#   python3 art_textures.py <out dir>
import numpy as np, cv2, os, sys, math
out = sys.argv[1] if len(sys.argv) > 1 else "tex"; os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(7)
def hexc(h): h = h.lstrip('#'); return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)
def C8(h): c = hexc(h) * 255; return (int(c[2]), int(c[1]), int(c[0]))   # BGR for cv2
def save(n, img): cv2.imwrite(os.path.join(out, n), img)

W, H = 768, 512
# 0: a big abstract in gold and black (penthouse)
img = np.zeros((H, W, 3), np.uint8); img[:] = C8('#15131a')
for k in range(14):
    cx, cy = int(rng.uniform(0, W)), int(rng.uniform(0, H)); r = int(rng.uniform(40, 220))
    col = C8(['#d9b36c', '#c9a03e', '#2a2436', '#f3eadb', '#8a2a2a'][k % 5])
    cv2.circle(img, (cx, cy), r, col, -1 if k % 3 else 6, cv2.LINE_AA)
img = cv2.GaussianBlur(img, (0, 0), 1.2)
for k in range(40):   # brush grain
    y = int(rng.uniform(0, H)); cv2.line(img, (0, y), (W, y + int(rng.uniform(-30, 30))), (int(rng.uniform(0, 60)),) * 3, 1, cv2.LINE_AA)
save("art_0.png", img)

# 1: the royal flush, pop-art (condo)
img = np.zeros((H, W, 3), np.uint8); img[:] = C8('#e9e7e1')
cards = ['10', 'J', 'Q', 'K', 'A']
for k, c in enumerate(cards):
    x0 = 40 + k * 140; cv2.rectangle(img, (x0, 90), (x0 + 120, 420), C8('#f7f7f4'), -1); cv2.rectangle(img, (x0, 90), (x0 + 120, 420), C8('#1b1b1f'), 3)
    cv2.putText(img, c, (x0 + 12, 150), cv2.FONT_HERSHEY_DUPLEX, 1.4, C8('#b8262c'), 3, cv2.LINE_AA)
    # a heart
    hx, hy = x0 + 60, 270
    cv2.circle(img, (hx - 22, hy - 15), 26, C8('#b8262c'), -1, cv2.LINE_AA); cv2.circle(img, (hx + 22, hy - 15), 26, C8('#b8262c'), -1, cv2.LINE_AA)
    cv2.fillPoly(img, [np.array([[hx - 48, hy - 5], [hx + 48, hy - 5], [hx, hy + 55]], np.int32)], C8('#b8262c'), cv2.LINE_AA)
save("art_1.png", img)

# 2: a neon palm at night (island / penthouse)
img = np.zeros((H, W, 3), np.uint8)
for y in range(H): img[y, :] = np.array(C8('#1a0b2e')) * (1 - y / H) + np.array(C8('#ff5f8f')) * (y / H) * 0.6
cv2.circle(img, (W // 2, int(H * 0.72)), 150, C8('#ffb36b'), -1, cv2.LINE_AA)
for k in range(8): cv2.line(img, (W // 2 - 150, int(H * 0.72) - 40 + k * 22), (W // 2 + 150, int(H * 0.72) - 40 + k * 22), C8('#1a0b2e'), 4)
cv2.rectangle(img, (0, int(H * 0.72)), (W, H), C8('#120820'), -1)
cv2.line(img, (int(W * 0.3), H), (int(W * 0.36), int(H * 0.25)), C8('#07030d'), 14, cv2.LINE_AA)
for a in range(7):
    ang = -0.4 - a * 0.42; L = 170
    cv2.line(img, (int(W * 0.36), int(H * 0.25)), (int(W * 0.36 + math.cos(ang) * L), int(H * 0.25 + math.sin(ang) * L * 0.8)), C8('#07030d'), 10, cv2.LINE_AA)
cv2.rectangle(img, (0, 0), (W - 1, H - 1), C8('#ff5f8f'), 6)
save("art_2.png", img)

# 3: a line portrait of a poker face in one stroke (mansion)
img = np.zeros((H, W, 3), np.uint8); img[:] = C8('#f3eadb')
pts = []
for t in np.linspace(0, 2 * math.pi, 400):
    r = 150 + 25 * math.sin(3 * t) + 12 * math.sin(7 * t + 1); pts.append([int(W / 2 + r * math.cos(t) * 0.8), int(H / 2 + r * math.sin(t))])
cv2.polylines(img, [np.array(pts, np.int32)], True, C8('#1b1b1f'), 3, cv2.LINE_AA)
cv2.ellipse(img, (W // 2 - 45, H // 2 - 20), (22, 10), 0, 0, 360, C8('#1b1b1f'), 3, cv2.LINE_AA); cv2.ellipse(img, (W // 2 + 45, H // 2 - 20), (22, 10), 0, 0, 360, C8('#1b1b1f'), 3, cv2.LINE_AA)
cv2.line(img, (W // 2 - 30, H // 2 + 60), (W // 2 + 30, H // 2 + 60), C8('#1b1b1f'), 3, cv2.LINE_AA)
cv2.putText(img, "ALL IN", (W // 2 - 95, H - 40), cv2.FONT_HERSHEY_DUPLEX, 1.6, C8('#c9a03e'), 3, cv2.LINE_AA)
save("art_3.png", img)

# 4: Vegas strip at night, blocky (penthouse, wide)
img = np.zeros((H, W, 3), np.uint8); img[:] = C8('#0a0a14')
for k in range(40):
    x = int(rng.uniform(0, W)); w_ = int(rng.uniform(20, 70)); h_ = int(rng.uniform(80, 380))
    cv2.rectangle(img, (x, H - h_), (x + w_, H), C8(['#151526', '#1d1d33', '#111120'][k % 3]), -1)
    for yy in range(H - h_ + 10, H - 10, 14):
        for xx in range(x + 5, x + w_ - 5, 10):
            if rng.random() < 0.55: cv2.rectangle(img, (xx, yy), (xx + 5, yy + 7), C8(['#ffd36b', '#ff6aa8', '#6bd7ff', '#ffffff'][int(rng.integers(0, 4))]), -1)
cv2.putText(img, "VEGAS", (W // 2 - 150, 120), cv2.FONT_HERSHEY_DUPLEX, 3.0, C8('#ff6aa8'), 6, cv2.LINE_AA)
img = cv2.addWeighted(img, 1.0, cv2.GaussianBlur(img, (0, 0), 12), 0.6, 0)
save("art_4.png", img)

# 5: a chip stack photo-poster (mansion)
img = np.zeros((H, W, 3), np.uint8); img[:] = C8('#1b1b1f')
for k in range(9):
    x = 100 + k * 70; n = int(rng.uniform(4, 14)); col = C8(['#b8262c', '#2f5fb0', '#1b1b1f', '#2a8a4a', '#d9b36c'][k % 5])
    for j in range(n): cv2.ellipse(img, (x, H - 60 - j * 12), (34, 12), 0, 0, 360, col, -1, cv2.LINE_AA); cv2.ellipse(img, (x, H - 60 - j * 12), (34, 12), 0, 0, 360, C8('#f3eadb'), 1, cv2.LINE_AA)
cv2.putText(img, "STACKS", (W // 2 - 130, 90), cv2.FONT_HERSHEY_DUPLEX, 2.2, C8('#f3eadb'), 4, cv2.LINE_AA)
save("art_5.png", img)

# pool water, tileable, light caustics on turquoise
S = 512; yy, xx = np.mgrid[0:S, 0:S].astype(np.float32) / S * 2 * math.pi
c = np.zeros((S, S, 3), np.float32)
caus = 0.5 + 0.5 * np.sin(xx * 3 + np.sin(yy * 2) * 2) * np.sin(yy * 3 + np.sin(xx * 2) * 2)
caus = np.clip(caus ** 3 * 1.4, 0, 1)
base = hexc('#3fb6c9'); hi = hexc('#d8f6ff')
c = base[None, None, :] * (1 - caus[..., None]) + hi[None, None, :] * caus[..., None]
save("pool_water.png", (np.clip(c, 0, 1)[..., ::-1] * 255).astype(np.uint8))
print("ok")
