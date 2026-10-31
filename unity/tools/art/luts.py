# Film grades for The Dream, one per set: .cube files (33^3, for ffmpeg's lut3d on the film renders) and URP Color
# Lookup strips (32^3 as 1024x32 PNG). Our own work: CC0. Also writes before/after previews from the day2 stills.
import os, sys, numpy as np
from PIL import Image

OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)

def lum(c): return c[..., 0] * 0.2126 + c[..., 1] * 0.7152 + c[..., 2] * 0.0722

def grade(c, shadows, highs, toe, contrast, sat, warm_skin=True, lift=0.0, ev=0.0):
    c = c.copy()
    if ev:   # exposure in linear light, with a soft shoulder so highlights don't clip
        lin = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4) * (2 ** ev)
        lin = lin / (1 + lin * (1 - 1 / (2 ** ev)) * 0.6)
        c = np.where(lin <= 0.0031308, lin * 12.92, 1.055 * np.power(np.clip(lin, 0, None), 1 / 2.4) - 0.055)
    L = lum(c)[..., None]
    # split tone: tint shadows and highlights (weights by luminance)
    ws = np.clip(1 - L / 0.55, 0, 1) ** 1.5; wh = np.clip((L - 0.45) / 0.55, 0, 1) ** 1.2
    c = c + ws * np.array(shadows) + wh * np.array(highs)
    # toe: lift the darkest values a little (murky rooms read better), then a gentle S-curve
    c = lift + (1 - lift) * c
    c = c + toe * (1 - c) * np.exp(-c * 9)
    c = np.clip(c, 0, 1)
    c = 0.5 + (c - 0.5) * contrast + (np.sin((c - 0.5) * np.pi) / np.pi - (c - 0.5)) * (contrast - 1) * 0.0
    s_curve = c * c * (3 - 2 * c)
    c = c * (1 - (contrast - 1)) + s_curve * (contrast - 1) if contrast > 1 else c
    # saturation, gentler on skin tones (orange hues)
    L2 = lum(c)[..., None]
    mx, mn = c.max(-1, keepdims=True), c.min(-1, keepdims=True)
    r, g, b = c[..., 0:1], c[..., 1:2], c[..., 2:3]
    skin = np.clip(1 - np.abs((r - g) - 0.12) / 0.18, 0, 1) * (r > b) if warm_skin else 0
    s = sat * (1 - 0.6 * skin) + 1.0 * 0.6 * skin if warm_skin else sat
    c = L2 + (c - L2) * s
    return np.clip(c, 0, 1)

GRADES = {
    # name: shadows tint, highlights tint, toe lift, contrast, saturation, black lift
    "diner_night":     dict(ev=0.3, shadows=(-0.02, 0.01, 0.035), highs=(0.03, 0.005, -0.03), toe=0.05, contrast=1.10, sat=1.08, lift=0.012),
    "office_dusk":     dict(shadows=(-0.015, 0.0, 0.03), highs=(0.035, 0.0, 0.015), toe=0.03, contrast=1.08, sat=1.05, lift=0.008),
    "cardroom_sodium": dict(ev=0.9, shadows=(-0.02, 0.015, 0.02), highs=(0.04, 0.02, -0.035), toe=0.10, contrast=1.0, sat=1.04, lift=0.02),
    "apartment_night": dict(ev=0.4, shadows=(-0.025, 0.0, 0.04), highs=(0.02, 0.01, 0.0), toe=0.06, contrast=1.08, sat=1.0, lift=0.012),
    "title_miami":     dict(shadows=(-0.01, -0.005, 0.045), highs=(0.045, 0.0, 0.02), toe=0.04, contrast=1.12, sat=1.12, lift=0.01),
}

def lattice(n):
    v = np.linspace(0, 1, n, dtype=np.float32)
    b, g, r = np.meshgrid(v, v, v, indexing="ij")          # r fastest, as .cube expects
    return np.stack([r, g, b], -1)

def apply_lut(img, lut, n):
    # trilinear lookup of an (n,n,n,3) table indexed [b][g][r]
    x = img * (n - 1); i0 = np.floor(x).astype(int).clip(0, n - 2); f = x - i0
    out = 0
    for dr in (0, 1):
        for dg in (0, 1):
            for db in (0, 1):
                w = (f[..., 0] if dr else 1 - f[..., 0]) * (f[..., 1] if dg else 1 - f[..., 1]) * (f[..., 2] if db else 1 - f[..., 2])
                out = out + w[..., None] * lut[i0[..., 2] + db, i0[..., 1] + dg, i0[..., 0] + dr]
    return out

for name, p in GRADES.items():
    n = 33; lat = lattice(n); g = grade(lat, **p)
    with open(os.path.join(OUT, f"{name}.cube"), "w") as f:
        f.write(f'TITLE "The Dream {name}"\nLUT_3D_SIZE {n}\n')
        for row in g.reshape(-1, 3): f.write(f"{row[0]:.6f} {row[1]:.6f} {row[2]:.6f}\n")
    m = 32; lat2 = lattice(m); g2 = grade(lat2, **p)      # URP strip: x = r + b*32, y = g (row 0 at the bottom)
    strip = np.zeros((m, m * m, 3), np.float32)
    for bi in range(m): strip[:, bi * m:(bi + 1) * m] = g2[bi]
    Image.fromarray((np.flipud(strip) * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, f"{name}_strip32.png"))
# the neutral strip, so the import settings can be checked (applying it must change nothing)
m = 32; lat2 = lattice(m); strip = np.zeros((m, m * m, 3), np.float32)
for bi in range(m): strip[:, bi * m:(bi + 1) * m] = lat2[bi]
Image.fromarray((np.flipud(strip) * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, "neutral_strip32.png"))

# before/after previews
for still, gname in [(a, b) for a, b in (x.split("=") for x in sys.argv[2:])]:
    im = np.array(Image.open(still).convert("RGB"), np.float32) / 255
    n = 33; lut = grade(lattice(n), **GRADES[gname])
    after = apply_lut(im, lut, n)
    h, w = im.shape[:2]; both = np.concatenate([im, after], 1)
    Image.fromarray((both * 255).astype(np.uint8)).save(os.path.join(OUT, f"ba_{gname}_{os.path.basename(still).split('.')[0]}.jpg"), quality=90)
print("luts done")
