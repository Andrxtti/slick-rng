# Cards, chips, the dealer button and 9-slice UI sprites for The Dream, drawn in code (our own work: CC0).
import os, sys, json, math
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT = sys.argv[1]
FONTS = "/home/claude/slick-rng/unity/Assets/Dream/Resources/Dream/Fonts/"
GLOOCK, INTER_B, INTER_SB, SUITF = FONTS + "Gloock-Regular.ttf", FONTS + "Inter-Bold.otf", FONTS + "Inter-SemiBold.otf", FONTS + "DejaVuSans-Bold.ttf"
rng = np.random.default_rng(11)
for d in ("cards", "chips", "ui"): os.makedirs(os.path.join(OUT, d), exist_ok=True)
SS = 4   # supersampling

def down(im, size): return im.resize(size, Image.LANCZOS)

def rrect_mask(w, h, r, ss=SS):
    m = Image.new("L", (w * ss, h * ss), 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, w * ss - 1, h * ss - 1), r * ss, fill=255)
    return m

def noise_img(w, h, amp, blur=0.8, seed=0):
    n = np.random.default_rng(seed).standard_normal((h, w)).astype(np.float32)
    n = np.array(Image.fromarray(((n * 0.15 + 0.5).clip(0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(blur)), np.float32) / 255 - 0.5
    return n * amp

# ---------------------------------------------------------------- cards
SUITS = {"s": ("♠", (24, 24, 30)), "h": ("♥", (198, 40, 40)), "d": ("♦", (30, 92, 205)), "c": ("♣", (28, 132, 64))}
RANKS = ["A", "K", "Q", "J", "T", "9", "8", "7", "6", "5", "4", "3", "2"]
CW, CH, CR = 360, 504, 30

def card_base(fill=(247, 244, 236)):
    W, H = CW * SS, CH * SS
    face = np.zeros((H, W, 3), np.float32) + np.array(fill, np.float32)
    face += noise_img(W, H, 10, 1.2, 3)[..., None]                          # paper
    y = np.linspace(0, 1, H)[:, None, None]
    face *= (1.02 - 0.05 * y)                                                # a soft top light
    im = Image.fromarray(face.clip(0, 255).astype(np.uint8), "RGB").convert("RGBA")
    im.putalpha(rrect_mask(CW, CH, CR))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((SS * 2, SS * 2, W - SS * 3, H - SS * 3), (CR - 2) * SS, outline=(0, 0, 0, 38), width=SS * 2)
    return im

def text_centered(d, xy, s, font, fill):
    d.text(xy, s, font=font, anchor="mm", fill=fill)

def card_face(rank, suit):
    im = card_base(); d = ImageDraw.Draw(im)
    sym, col = SUITS[suit]
    W, H = CW * SS, CH * SS
    r = "10" if rank == "T" else rank
    fr = ImageFont.truetype(GLOOCK, int((190 if r != "10" else 160) * SS))
    fs = ImageFont.truetype(SUITF, int(118 * SS))
    fbig = ImageFont.truetype(SUITF, int(300 * SS))
    # big readable index, top left: rank, suit under it (made for a phone)
    d.text((W * 0.08, H * 0.03), r, font=fr, anchor="la", fill=col + (255,))
    d.text((W * 0.10 + (8 if r == "10" else 0) * SS, H * 0.43), sym, font=fs, anchor="la", fill=col + (255,))
    # a large soft pip, bottom right
    pip = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ImageDraw.Draw(pip).text((W * 0.70, H * 0.74), sym, font=fbig, anchor="mm", fill=col + (34,))
    im = Image.alpha_composite(im, pip)
    # the small mirrored corner index, bottom right
    sm = Image.new("RGBA", im.size, (0, 0, 0, 0)); ds = ImageDraw.Draw(sm)
    ds.text((W * 0.08, H * 0.035), r, font=ImageFont.truetype(GLOOCK, int(56 * SS)), anchor="la", fill=col + (200,))
    sm = sm.rotate(180)
    return down(Image.alpha_composite(im, sm), (CW, CH))

def card_back(name, base, lattice, gold, mono):
    W, H = CW * SS, CH * SS
    im = Image.new("RGBA", (W, H), base + (255,)); d = ImageDraw.Draw(im)
    step = 26 * SS
    for k in range(-H, W + H, step):                                         # a fine diamond lattice
        d.line((k, 0, k + H, H), fill=lattice + (255,), width=int(1.6 * SS))
        d.line((k, H, k + H, 0), fill=lattice + (255,), width=int(1.6 * SS))
    m = 22 * SS
    d.rounded_rectangle((m, m, W - m, H - m), (CR - 12) * SS, outline=gold + (255,), width=3 * SS)
    d.rounded_rectangle((m + 10 * SS, m + 10 * SS, W - m - 10 * SS, H - m - 10 * SS), (CR - 18) * SS, outline=gold + (120,), width=1 * SS)
    ew, eh = W * 0.42, H * 0.25
    d.ellipse((W / 2 - ew / 2, H / 2 - eh / 2, W / 2 + ew / 2, H / 2 + eh / 2), fill=base + (255,), outline=gold + (255,), width=3 * SS)
    d.text((W / 2, H / 2 + 4 * SS), mono, font=ImageFont.truetype(GLOOCK, int(84 * SS)), anchor="mm", fill=gold + (255,))
    shade = Image.new("L", (W, H)); ImageDraw.Draw(shade).rectangle((0, 0, W, H), fill=0)
    im.putalpha(rrect_mask(CW, CH, CR))
    border = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(border).rounded_rectangle((SS * 6, SS * 6, W - SS * 7, H - SS * 7), (CR - 6) * SS, outline=(250, 246, 238, 255), width=8 * SS)
    im = Image.alpha_composite(im, border)
    down(im, (CW, CH)).save(os.path.join(OUT, "cards", name), optimize=True)

for s in SUITS:
    for r in RANKS:
        card_face(r, s).save(os.path.join(OUT, "cards", f"{r}{s}.png"), optimize=True)
card_back("back_live.png", (118, 22, 32), (138, 40, 50), (214, 176, 98), "D")
card_back("back_online.png", (16, 58, 74), (26, 74, 92), (150, 214, 226), "CF")

# ---------------------------------------------------------------- chips
CHIPS = {"1": ((236, 232, 222), (36, 92, 190)), "5": ((196, 36, 40), (245, 240, 230)), "25": ((24, 128, 70), (245, 240, 230)),
         "100": ((26, 26, 30), (236, 232, 222)), "500": ((108, 52, 160), (245, 230, 140)), "1K": ((236, 186, 52), (40, 30, 20))}
CS = 160

def chip_top(v, body, spot):
    W = CS * SS; im = Image.new("RGBA", (W, W), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    c = W / 2; R = W * 0.48
    d.ellipse((c - R, c - R, c + R, c + R), fill=body + (255,))
    for k in range(6):                                                       # edge inserts
        a0 = k * 60 - 12
        d.pieslice((c - R, c - R, c + R, c + R), a0, a0 + 24, fill=spot + (255,))
    ri = R * 0.80
    d.ellipse((c - ri, c - ri, c + ri, c + ri), fill=body + (255,))
    for k in range(36):                                                      # the dashed inner ring
        a = math.radians(k * 10)
        x0, y0 = c + math.cos(a) * ri * 0.94, c + math.sin(a) * ri * 0.94
        d.ellipse((x0 - 2.2 * SS, y0 - 2.2 * SS, x0 + 2.2 * SS, y0 + 2.2 * SS), fill=spot + (200,))
    rc = R * 0.56
    inlay = tuple(int(0.94 * x + 0.06 * 255) for x in (245, 242, 235)) if body != (236, 232, 222) else (250, 248, 242)
    d.ellipse((c - rc, c - rc, c + rc, c + rc), fill=inlay + (255,), outline=spot + (255,), width=3 * SS)
    f = ImageFont.truetype(INTER_B, int({1: 60, 2: 52}.get(len(v), 40) * SS))
    ink = {(236, 232, 222): (36, 92, 190), (236, 186, 52): (120, 84, 10)}.get(body, body)
    d.text((c, c + 2 * SS), v, font=f, anchor="mm", fill=ink + (255,))
    # light from above: a soft sheen, darker lower edge
    a = np.array(im, np.float32); h = a.shape[0]
    yy, xx = np.mgrid[0:h, 0:h].astype(np.float32)
    sheen = np.clip(1.0 + 0.10 * (1 - yy / h) - 0.10 * (yy / h) ** 2, 0.85, 1.12)
    a[..., :3] *= sheen[..., None]
    im = Image.fromarray(a.clip(0, 255).astype(np.uint8), "RGBA")
    return down(im, (CS, CS))

def chip_side(v, body, spot):
    # one chip seen from the side, to stack: 160 wide, 44 tall (ellipse top and the edge band)
    W, H = CS * SS, 44 * SS; im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    ry = H * 0.30; band = H * 0.38; c = W / 2; R = W * 0.48
    dark = tuple(int(x * 0.72) for x in body)
    d.rectangle((c - R, ry, c + R, ry + band), fill=dark + (255,))
    d.ellipse((c - R, band, c + R, band + 2 * ry), fill=dark + (255,))
    for k in range(7):                                                       # edge stripes
        x = c - R + (k + 0.5) * (2 * R / 7)
        d.rectangle((x - R * 0.06, ry + 2, x + R * 0.06, ry + band), fill=spot + (255,))
    d.ellipse((c - R, 0, c + R, 2 * ry), fill=body + (255,))
    d.ellipse((c - R * 0.62, ry * 0.38, c + R * 0.62, ry * 1.62), outline=spot + (220,), width=2 * SS)
    return down(im, (CS, 44))

meta = {"chips": {}}
for v, (body, spot) in CHIPS.items():
    chip_top(v, body, spot).save(os.path.join(OUT, "chips", f"chip_{v}.png"), optimize=True)
    chip_side(v, body, spot).save(os.path.join(OUT, "chips", f"chip_{v}_side.png"), optimize=True)
    meta["chips"][v] = {"top": f"chip_{v}.png", "side": f"chip_{v}_side.png", "stack_step_px": 9}

# the dealer button
W = 120 * SS; im = Image.new("RGBA", (W, W), (0, 0, 0, 0)); d = ImageDraw.Draw(im); c = W / 2
d.ellipse((2 * SS, 4 * SS, W - 2 * SS, W), fill=(150, 146, 136, 255))
d.ellipse((2 * SS, 0, W - 2 * SS, W - 4 * SS), fill=(250, 248, 242, 255), outline=(200, 196, 186, 255), width=2 * SS)
d.text((c, c - 6 * SS), "D", font=ImageFont.truetype(GLOOCK, 58 * SS), anchor="mm", fill=(24, 24, 30, 255))
d.text((c, c + 30 * SS), "DEALER", font=ImageFont.truetype(INTER_B, 13 * SS), anchor="mm", fill=(60, 60, 66, 255))
down(im, (120, 120)).save(os.path.join(OUT, "chips", "dealer_button.png"), optimize=True)

# ---------------------------------------------------------------- 9-slice UI sprites (white, the game tints them)
ui = {}
def nine(name, w, h, r, border, draw=None):
    im = Image.new("RGBA", (w * SS, h * SS), (255, 255, 255, 0)); d = ImageDraw.Draw(im)
    if draw: draw(d, w * SS, h * SS, r * SS)
    else: d.rounded_rectangle((0, 0, w * SS - 1, h * SS - 1), r * SS, fill=(255, 255, 255, 255))
    down(im, (w, h)).save(os.path.join(OUT, "ui", name), optimize=True)
    ui[name] = {"size": [w, h], "border_LRTB": [border, border, border, border], "image_type": "Sliced", "pixels_per_unit_multiplier": 1}

nine("pill.png", 128, 128, 64, 63)
nine("rounded_24.png", 96, 96, 24, 26)
nine("rounded_12.png", 48, 48, 12, 14)
def outline(d, W, H, R): d.rounded_rectangle((SS, SS, W - SS - 1, H - SS - 1), R, outline=(255, 255, 255, 255), width=3 * SS)
nine("pill_outline.png", 128, 128, 64, 63, outline)
def plate_draw(d, W, H, R):                                                   # seat plate: glass with a top highlight
    d.rounded_rectangle((0, 0, W - 1, H - 1), R, fill=(255, 255, 255, 235))
    d.rounded_rectangle((SS, SS, W - SS - 1, H * 0.5), R, fill=(255, 255, 255, 255))
nine("seat_plate.png", 96, 96, 22, 24, plate_draw)
# a soft shadow to sit under a card (sliced)
sh = Image.new("RGBA", (128 * SS, 128 * SS), (0, 0, 0, 0)); dsh = ImageDraw.Draw(sh)
dsh.rounded_rectangle((24 * SS, 28 * SS, 104 * SS, 108 * SS), 14 * SS, fill=(0, 0, 0, 150))
sh = sh.filter(ImageFilter.GaussianBlur(9 * SS)); down(sh, (128, 128)).save(os.path.join(OUT, "ui", "card_shadow.png"), optimize=True)
ui["card_shadow.png"] = {"size": [128, 128], "border_LRTB": [50, 50, 50, 50], "image_type": "Sliced", "note": "black: use as is, offset 0,-6 px under a card"}
# a soft glow ring for the seat whose turn it is (sliced too)
g = Image.new("RGBA", (192 * SS, 192 * SS), (255, 255, 255, 0)); dg = ImageDraw.Draw(g)
dg.rounded_rectangle((40 * SS, 40 * SS, 152 * SS, 152 * SS), 34 * SS, fill=(255, 255, 255, 255))
g = g.filter(ImageFilter.GaussianBlur(14 * SS)); down(g, (192, 192)).save(os.path.join(OUT, "ui", "glow.png"), optimize=True)
ui["glow.png"] = {"size": [192, 192], "border_LRTB": [80, 80, 80, 80], "image_type": "Sliced"}
meta["ui"] = ui
meta["cards"] = {"size": [CW, CH], "names": "rank+suit, e.g. As, Td, 7h; backs back_live.png and back_online.png",
                 "colours": {"s": "#18181E", "h": "#C62828", "d": "#1E5CCD", "c": "#1C8440"}}
json.dump(meta, open(os.path.join(OUT, "kit.json"), "w"), indent=1)
print("kit done")
