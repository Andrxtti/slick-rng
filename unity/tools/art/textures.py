# Room textures: wood floor, rug, the night view out the window, the TV's late-night poker show, the laptop screen.
import numpy as np, cv2, os, math
out = "tex"; os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(3)
def save(n, img): cv2.imwrite(os.path.join(out, n), cv2.cvtColor(np.clip(img * 255, 0, 255).astype(np.uint8), cv2.COLOR_RGB2BGR if img.shape[2] == 3 else cv2.COLOR_RGBA2BGRA))
def hexc(h): h = h.lstrip('#'); return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)

# wood planks, tileable 1024 (8 planks across), light grey-brown; tinted per room in the game
S = 1024; img = np.zeros((S, S, 3), np.float32); pw = S // 8
for i in range(8):
    base = hexc('#b08a64') * rng.uniform(0.82, 1.08)
    off = int(rng.integers(0, S)); seg = int(rng.integers(S // 3, S))
    yy, xx = np.mgrid[0:S, 0:pw].astype(np.float32)
    grain = 0.06 * np.sin((yy * 0.05 + np.sin(xx * 0.08 + i) * 2.5 + rng.uniform(0, 6))) + 0.04 * np.sin(yy * 0.31 + xx * 0.02)
    col = base[None, None, :] * (1 + grain[..., None])
    img[:, i * pw:(i + 1) * pw] = col
    img[:, i * pw:i * pw + 2] *= 0.55                               # gaps between planks
    for k in range(2):
        y = (off + k * seg) % S; img[max(0, y - 1):y + 1, i * pw:(i + 1) * pw] *= 0.6
img = cv2.GaussianBlur(img, (0, 0), 0.7); save("floor_wood.png", img)

# rug: a woven geometric pattern, neutral so the room can tint it
R = 512; yy, xx = np.mgrid[0:R, 0:R].astype(np.float32)
u, v = xx / R, yy / R
rug = np.ones((R, R, 3), np.float32) * hexc('#d8cfc0')
border = (np.minimum(np.minimum(u, 1 - u), np.minimum(v, 1 - v)) < 0.07)
rug[border] = hexc('#3b2f2a')
diamond = (np.abs((u - 0.5) * 2) + np.abs((v - 0.5) * 2)) % 0.25 < 0.05
rug[diamond & ~border] = hexc('#8a3b2f')
noise = rng.normal(0, 0.04, (R, R, 1)).astype(np.float32); rug *= (1 + noise)
save("rug.png", rug)

# the window: our bay skyline at night (from the drive art), framed tighter
sky = cv2.imread("../drive/sky.png")[..., ::-1].astype(np.float32) / 255
city = cv2.imread("../drive/city.png", cv2.IMREAD_UNCHANGED).astype(np.float32) / 255
city = np.concatenate([city[..., 2::-1], city[..., 3:4]], -1)
comp = sky.copy(); a = city[..., 3:4]; comp = comp * (1 - a) + city[..., :3] * a
win = comp[180:900, 380:1660]; win = cv2.resize(win, (1024, 576)); save("window_city.png", win)
# the island: sunset over open water (no city)
isl = sky[180:900, 380:1660]; isl = cv2.resize(isl, (1024, 576)); save("window_ocean.png", isl)

# TV: a late-night poker broadcast, three frames (the hole cards change), with scanlines. Drawn in 8-bit.
def C(h): c = hexc(h) * 255; return (int(c[0]), int(c[1]), int(c[2]))
def tv_frame(k):
    W, H = 512, 384; t = np.zeros((H, W, 3), np.uint8); t[:] = C('#0c1a12')
    cv2.ellipse(t, (W // 2, int(H * 0.56)), (int(W * 0.47), int(H * 0.35)), 0, 0, 360, C('#3a2414'), -1, cv2.LINE_AA)
    cv2.ellipse(t, (W // 2, int(H * 0.56)), (int(W * 0.45), int(H * 0.33)), 0, 0, 360, C('#1f6b3a'), -1, cv2.LINE_AA)
    shown = 3 + (k > 0) + (k > 1)
    for i in range(5):
        x0 = 150 + i * 44; y0 = 170
        if i < shown:
            cv2.rectangle(t, (x0, y0), (x0 + 36, y0 + 50), (242, 242, 235), -1)
            cv2.putText(t, "AKQJT98"[(i + k * 2) % 7], (x0 + 9, y0 + 34), cv2.FONT_HERSHEY_SIMPLEX, 0.85, (200, 25, 25) if (i + k) % 2 else (15, 15, 15), 2, cv2.LINE_AA)
        else:
            cv2.rectangle(t, (x0, y0), (x0 + 36, y0 + 50), (242, 242, 235), -1); cv2.rectangle(t, (x0 + 3, y0 + 3), (x0 + 33, y0 + 47), (150, 40, 40), -1)
    for i, (cx, cy) in enumerate([(110, 262), (402, 262), (256, 300)]):
        for j in range(4 + (i + k) % 3): cv2.ellipse(t, (cx, cy - j * 6), (18, 7), 0, 0, 360, (215, 50, 50) if j % 2 else (230, 230, 230), -1, cv2.LINE_AA)
    for i, x in enumerate((14, 396)):
        cv2.rectangle(t, (x, 14), (x + 102, 92), (12, 12, 20), -1)
        for j in range(2):
            cv2.rectangle(t, (x + 8 + j * 46, 20), (x + 46 + j * 46, 60), (242, 242, 235), -1)
            cv2.putText(t, ["A", "K", "7", "7", "Q", "J"][(i * 2 + j + k) % 6], (x + 18 + j * 46, 50), cv2.FONT_HERSHEY_SIMPLEX, 0.85, (15, 15, 15), 2, cv2.LINE_AA)
        cv2.putText(t, ["58%", "42%", "71%", "29%", "50%", "50%"][(i + 2 * k) % 6], (x + 30, 84), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (255, 215, 80), 1, cv2.LINE_AA)
    cv2.rectangle(t, (0, H - 54), (W, H), (15, 10, 30), -1); cv2.rectangle(t, (0, H - 54), (W, H - 50), (230, 180, 50), -1)
    cv2.putText(t, ["HIGH STAKES LATE NIGHT", "ALL IN... AND A CALL!", "THE RIVER: WATCH THIS"][k], (14, H - 18), cv2.FONT_HERSHEY_DUPLEX, 0.75, (255, 255, 255), 1, cv2.LINE_AA)
    f = t.astype(np.float32) / 255; f[::3] *= 0.78; f = cv2.GaussianBlur(f, (0, 0), 0.6) * 1.05
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32); r = np.sqrt(((xx - W / 2) / (W / 2)) ** 2 + ((yy - H / 2) / (H / 2)) ** 2)
    return f * np.clip(1.15 - r * 0.35, 0, 1)[..., None]
for k in range(3): save(f"tv_poker_{k}.png", tv_frame(k))

# laptop: the hub on screen (calendar cards in the game's colours)
W, H = 512, 330; s_ = np.zeros((H, W, 3), np.uint8); s_[:] = C('#0b0a08')
cv2.rectangle(s_, (0, 0), (W, 26), C('#1a1712'), -1)
for i, c in enumerate(['#ff5f57', '#febc2e', '#28c840']): cv2.circle(s_, (14 + i * 16, 13), 5, C(c), -1, cv2.LINE_AA)
cv2.putText(s_, "cfdream.app", (200, 18), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (150, 140, 122), 1, cv2.LINE_AA)
cv2.putText(s_, "Tonight", (24, 70), cv2.FONT_HERSHEY_DUPLEX, 1.0, (242, 235, 220), 1, cv2.LINE_AA)
for i in range(3):
    y = 92 + i * 74; cv2.rectangle(s_, (24, y), (W - 24, y + 62), C('#1a1712'), -1)
    cv2.rectangle(s_, (34, y + 12), (220 - i * 30, y + 24), C('#e8c46a') if i == 0 else (128, 118, 102), -1)
    cv2.rectangle(s_, (34, y + 34), (300, y + 42), (90, 82, 72), -1)
    cv2.rectangle(s_, (W - 110, y + 18), (W - 40, y + 44), C('#d9b36c'), -1)
save("laptop_screen.png", s_.astype(np.float32) / 255)
print("ok"); os.system("ls -la tex")
