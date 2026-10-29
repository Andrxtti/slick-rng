# Table plates for The Dream: a top-down poker table drawn in code (felt, rail, trim, light), RGBA with a soft
# shadow so the game can place it over any background. Our own work: CC0.
import numpy as np, cv2, sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)
FONTS = "/home/claude/slick-rng/unity/Assets/Dream/Resources/Dream/Fonts/"
rng = np.random.default_rng(7)

def srgb(c): return np.array(c, np.float32) / 255.0

def ellipse_sdf(h, w, cx, cy, a, b):
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    if True:   # racetrack: a rounded rectangle whose ends are half circles
        r = min(a, b); bx, by = a - r, b - r
        qx, qy = np.abs(x - cx) - bx, np.abs(y - cy) - by
        out = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2) + np.minimum(np.maximum(qx, qy), 0)
        return out - r, x, y
    # approximate signed distance to an ellipse (good enough near the edge for shading)
    nx, ny = (x - cx) / a, (y - cy) / b
    r = np.sqrt(nx * nx + ny * ny) + 1e-6
    grad = np.sqrt((nx / a) ** 2 + (ny / b) ** 2) / r + 1e-6
    return (r - 1.0) / grad, x, y   # pixels, negative inside

def fiber_noise(h, w, scale=1.0):
    n = rng.standard_normal((h, w)).astype(np.float32)
    n1 = cv2.GaussianBlur(n, (0, 0), 0.7 * scale)
    k = np.zeros((9, 9), np.float32); k[4, :] = 1; k /= k.sum()        # short horizontal fibers
    n2 = cv2.filter2D(rng.standard_normal((h, w)).astype(np.float32), -1, k)
    lo = cv2.GaussianBlur(rng.standard_normal((h // 8, w // 8)).astype(np.float32), (0, 0), 6)
    lo = cv2.resize(lo, (w, h), interpolation=cv2.INTER_CUBIC)
    return n1 * 0.55 + n2 * 0.35, lo

def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1); return t * t * (3 - 2 * t)

def plate(w, h, felt, felt_dark, rail, rail_hi, trim, logo, logo2, warm, name, portrait=False):
    S = 2   # supersample
    W, H = w * S, h * S
    cx, cy = W / 2, H / 2
    pad = 0.035 * min(W, H)
    A, Bv = W / 2 - pad, H / 2 - pad                       # outer rail
    railw = 0.085 * min(W, H)
    trimw = 0.034 * min(W, H)
    d, x, y = ellipse_sdf(H, W, cx, cy, A, Bv)
    inner = d + railw                                        # distance to the rail's inner edge (negative inside)
    feltd = inner + trimw                                    # distance to the felt edge
    img = np.zeros((H, W, 3), np.float32)

    # felt
    fn, lo = fiber_noise(H, W, S)
    base = srgb(felt)
    # the pendant pool: warm, bright in the middle, falling off to the rail
    ny, nx = (y - cy * (0.96 if not portrait else 0.98)) / (Bv * 0.95), (x - cx) / (A * 0.95)
    pool = np.exp(-(nx ** 2 + ny ** 2) * 1.35)
    light = 0.42 + 0.78 * pool
    felt_col = base[None, None, :] * light[..., None]
    felt_col *= (1 + 0.06 * fn[..., None] + 0.05 * lo[..., None])
    felt_col += srgb(warm)[None, None, :] * (pool ** 3)[..., None] * 0.06
    # the rail's shadow on the felt
    sh = smooth(-0.08 * min(W, H), 0, feltd)
    felt_col *= (1 - 0.55 * sh)[..., None]
    img[:] = felt_col

    # betting line
    bd, _, _ = ellipse_sdf(H, W, cx, cy, A - railw - trimw - 0.07 * min(W, H), Bv - railw - trimw - 0.07 * min(W, H))
    line = np.exp(-(bd / (1.3 * S)) ** 2)
    img = img * (1 - 0.22 * line[..., None]) + srgb(felt_dark)[None, None, :] * 0 + (srgb((230, 220, 190)) * 0.22)[None, None, :] * line[..., None] * light[..., None]

    # the racetrack: a walnut ring between felt and rail, grain running around the table, a brass bead on the felt side
    tmask = smooth(-trimw - 1.0 * S, -trimw + 1.0 * S, inner) * (1 - smooth(-1.0 * S, 1.0 * S, inner))
    ang0 = np.arctan2((y - cy) / Bv, (x - cx) / A)
    gn = cv2.GaussianBlur(rng.standard_normal((H // 2, W // 2)).astype(np.float32), (0, 0), 3)
    gn = cv2.resize(gn, (W, H))
    grain = 0.5 + 0.5 * np.sin(inner * (0.55 / S) + gn * 3.0 + np.sin(ang0 * 40) * 0.6)
    tcol = srgb(trim)[None, None, :] * (0.55 + 0.25 * grain[..., None]) * (0.6 + 0.7 * pool[..., None])
    gloss = np.exp(-((inner + trimw * 0.55) / (trimw * 0.18)) ** 2) * (0.5 + 0.5 * np.cos(ang0 + np.pi / 2) ** 2)
    tcol += srgb((255, 236, 200))[None, None, :] * (gloss * 0.12)[..., None]
    img = img * (1 - tmask[..., None]) + tcol * tmask[..., None]
    bead = np.exp(-((inner + trimw) / (0.9 * S)) ** 2)
    img = img * (1 - 0.8 * bead[..., None]) + srgb((205, 165, 90))[None, None, :] * (0.5 + 0.6 * pool[..., None]) * 0.8 * bead[..., None]

    # rail: padded leather, lit from above (a highlight on the inner crown), stitched
    rmask = smooth(-0.6 * S, 0.6 * S, inner) * (1 - smooth(-1.5 * S, 0.5 * S, d))
    t = np.clip((inner) / railw, 0, 1)                       # 0 at felt edge, 1 at outside
    crown = np.exp(-((t - 0.38) / 0.22) ** 2)
    # light comes from above the table's middle: rails nearer the top are lit on their inner side
    ang = np.arctan2((y - cy) / Bv, (x - cx) / A)
    facing = 0.55 + 0.45 * np.cos(ang + np.pi / 2) ** 2
    leather_n = cv2.GaussianBlur(rng.standard_normal((H, W)).astype(np.float32), (0, 0), 1.6 * S)
    rcol = srgb(rail)[None, None, :] * (0.55 + 0.9 * crown[..., None] * facing[..., None]) * (1 + 0.08 * leather_n[..., None])
    rcol += srgb(rail_hi)[None, None, :] * (np.exp(-((t - 0.30) / 0.06) ** 2) * facing * 0.35)[..., None]
    edge_dark = smooth(0.75, 1.0, t)
    rcol *= (1 - 0.6 * edge_dark)[..., None]
    # stitching: a dashed line near the inner edge of the rail
    sd = np.abs(t - 0.12)
    dash = (np.sin(ang * (A + Bv) / (5.5 * S)) > 0.2).astype(np.float32)
    stitch = np.exp(-(sd * railw / (1.0 * S)) ** 2) * dash
    rcol = rcol * (1 - 0.5 * stitch[..., None]) + srgb((70, 60, 50))[None, None, :] * 0.5 * stitch[..., None]
    img = img * (1 - rmask[..., None]) + rcol * rmask[..., None]

    alpha = 1 - smooth(-1.0 * S, 1.0 * S, d)
    # soft drop shadow under the table
    shadow = np.clip(1 - smooth(-0.02 * min(W, H), 0.06 * min(W, H), d), 0, 1) * 0.65
    out_a = np.maximum(alpha, shadow * (1 - alpha))
    rgb = img * alpha[..., None] / np.maximum(out_a[..., None], 1e-4)
    rgb = np.clip(rgb, 0, 1)
    rgba = np.dstack([rgb, out_a])
    im = Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA")

    # the logo, pressed into the felt
    if logo:
        lay = Image.new("RGBA", (W, H), (0, 0, 0, 0)); dr = ImageDraw.Draw(lay)
        fsz = int(0.065 * min(W, H)); f = ImageFont.truetype(FONTS + "Gloock-Regular.ttf", fsz)
        f2 = ImageFont.truetype(FONTS + "Inter-SemiBold.otf", int(fsz * 0.30))
        ly = cy - (0.30 if not portrait else 0.18) * Bv
        dr.text((cx, ly), logo, font=f, anchor="mm", fill=(0, 0, 0, 70))
        if logo2:
            spaced = " ".join(logo2)
            dr.text((cx, ly + fsz * 0.85), spaced, font=f2, anchor="mm", fill=(0, 0, 0, 60))
        hi = Image.new("RGBA", (W, H), (0, 0, 0, 0)); dh = ImageDraw.Draw(hi)
        dh.text((cx, ly - S), logo, font=f, anchor="mm", fill=(255, 255, 255, 22))
        lay = Image.alpha_composite(hi, lay).filter(ImageFilter.GaussianBlur(0.6 * S))
        im = Image.alpha_composite(im, lay)
    im = im.resize((w, h), Image.LANCZOS)
    im.save(os.path.join(OUT, name), optimize=True)
    return im

live = dict(felt=(30, 104, 64), felt_dark=(14, 52, 32), rail=(30, 24, 22), rail_hi=(170, 130, 95), trim=(120, 78, 44),
            warm=(255, 214, 160), logo="Biscayne", logo2="CARD ROOM")
online = dict(felt=(22, 86, 104), felt_dark=(10, 40, 50), rail=(30, 33, 40), rail_hi=(120, 150, 190), trim=(52, 60, 74),
              warm=(200, 230, 255), logo="CF Tilt", logo2="POKER")
plate(2400, 1200, name="table_live.png", **live)
plate(1200, 2000, name="table_live_portrait.png", portrait=True, **live)
plate(2400, 1200, name="table_online.png", **online)
plate(1200, 2000, name="table_online_portrait.png", portrait=True, **online)
print("plates done")
