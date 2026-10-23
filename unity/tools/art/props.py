# Furniture and props for the apartments. Each prop: origin on the floor at its centre, front facing +z,
# y up, metres. Materials are names the game maps to colours (some take the room's colour scheme).
import numpy as np, math, sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'rides'))
from mesh import Mesh, superellipse, loft, lathe, box, save_bin, render
from cars import orient

def add(m, V, F, mat, part="body", centre=None, smooth=True):
    c = np.mean(V, axis=0) if centre is None else np.array(centre)
    F = orient(V, F, lambda q: c); m.add(part, V, F, mat, smooth)

def cube(m, c, s, mat, part="body"):
    V, F = box(c, s); add(m, V, F, mat, part, c, smooth=False)

def rbox(m, c, s, mat, p=5.0, part="body", n=28, ends=0.35):
    """a soft box: superellipse sections along x, ends rounded over `ends` of the half-length"""
    cx, cy, cz = c; hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
    rings = []; xs = np.linspace(-1, 1, 21)
    for u in xs:
        k = 1.0 if abs(u) < 1 - ends else math.sqrt(max(0.0, 1 - ((abs(u) - (1 - ends)) / ends) ** 2))
        k = max(k, 0.02)
        rings.append([(cx + u * hx, cy + yy * hy * k, cz + zz * hz * (0.6 + 0.4 * k)) for (zz, yy) in superellipse(n, p)])
    V, F = loft(rings); add(m, V, F, mat, part, c)

def cyl(m, c, r, h, mat, n=20, part="body", r2=None, axis="y"):
    r2 = r if r2 is None else r2
    if axis == "y":
        V, F = lathe([(0, 0), (r, 0), (r2, h), (0, h)], n, (0, 0, 0), axis="x")
        V = [(c[0] + y_, c[1] + x_, c[2] + z_) for (x_, y_, z_) in V]   # lathe along x then swap x<->y
        V = [(c[0] + (v[0] - c[0]), v[1], v[2]) for v in V]
    else:
        V, F = lathe([(0, 0), (r, 0), (r2, h), (0, h)], n, c, axis="x")
    add(m, V, F, mat, part, (c[0], c[1] + h / 2, c[2]) if axis == "y" else (c[0] + h / 2, c[1], c[2]))

def lathe_y(m, c, prof, mat, n=24, part="body"):
    """profile [(r, y)] revolved around a vertical axis through c"""
    V, F = lathe([(r, y) for (r, y) in prof], n, (0, 0, 0), axis="x")
    V = [(c[0] + y_, c[1] + x_, c[2] + z_) for (x_, y_, z_) in V]
    add(m, V, F, mat, part, (c[0], c[1] + np.mean([p[1] for p in prof]), c[2]))

# ------------------------------------------------------------------ the props
def bed(w=1.5, d=2.05):
    m = Mesh()
    cube(m, (0, 0.17, 0), (w, 0.26, d), "wood")
    cube(m, (0, 0.55, -d / 2 + 0.04), (w + 0.04, 0.9, 0.08), "wood")                # headboard
    rbox(m, (0, 0.42, 0.0), (w - 0.06, 0.26, d - 0.08), "sheet", p=6)               # mattress
    for s in (-1, 1): rbox(m, (s * w * 0.24, 0.6, -d / 2 + 0.3), (w * 0.42, 0.13, 0.36), "pillow", p=4)
    rbox(m, (0, 0.56, 0.32), (w + 0.02, 0.06, d * 0.66), "blanket", p=8, ends=0.12)   # blanket over the foot
    return m

def desk(w=1.3, d=0.62, h=0.75):
    m = Mesh()
    cube(m, (0, h - 0.02, 0), (w, 0.04, d), "wood")
    for s in (-1, 1): cube(m, (s * (w / 2 - 0.03), (h - 0.04) / 2, 0), (0.04, h - 0.04, d - 0.04), "black")
    cube(m, (w / 2 - 0.25, h - 0.17, 0.02), (0.4, 0.22, d - 0.1), "wood_dark")         # drawers
    cube(m, (w / 2 - 0.25, h - 0.17, d / 2 - 0.07), (0.12, 0.015, 0.015), "metal")
    return m

def chair():
    m = Mesh()
    rbox(m, (0, 0.47, 0), (0.48, 0.09, 0.46), "chair", p=5)
    rbox(m, (0, 0.82, -0.22), (0.44, 0.56, 0.07), "chair", p=5)
    cyl(m, (0, 0.1, 0), 0.025, 0.36, "metal")
    for k in range(5):
        a = 2 * math.pi * k / 5; cube(m, (math.cos(a) * 0.15, 0.08, math.sin(a) * 0.15), (0.32 if k % 2 == 0 else 0.3, 0.035, 0.04), "black")
        cyl(m, (math.cos(a) * 0.29, 0.0, math.sin(a) * 0.29), 0.03, 0.05, "black", n=10)
    return m

def laptop():
    m = Mesh()
    rbox(m, (0, 0.009, 0), (0.34, 0.018, 0.24), "laptop", p=8, ends=0.06)
    # screen hinged at the back edge, tilted back
    t = math.radians(18); h = 0.22; zb = -0.115
    V = []; base = [(-0.17, 0, 0), (0.17, 0, 0), (0.17, h, 0), (-0.17, h, 0)]
    for (x, y, z) in base: V.append((x, 0.018 + y * math.cos(t), zb - y * math.sin(t)))
    lid = [(x, y, z - 0.008) for (x, y, z) in V]
    cube_V = V + lid
    F = [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
    add(m, cube_V, F, "laptop", centre=np.mean(cube_V, 0), smooth=False)
    inset = [(x * 0.92, 0.018 + (y * 0.9 + 0.01) * math.cos(t), zb - (y * 0.9 + 0.01) * math.sin(t) + 0.0015) for (x, y, z) in base]
    add(m, inset, [(0, 1, 2, 3)], "screen", centre=(0, 0.1, -0.5), smooth=False)
    m.screen = [tuple(v) for v in inset]
    cube(m, (0, 0.0185, 0.03), (0.28, 0.002, 0.1), "black")                              # keyboard
    return m

def crt():
    m = Mesh()
    rbox(m, (0, 0.27, -0.05), (0.62, 0.5, 0.48), "plastic_dk", p=6, ends=0.15)
    rbox(m, (0, 0.28, -0.32), (0.4, 0.34, 0.16), "plastic_dk", p=4)                      # tube bulge at the back
    V = [(-0.24, 0.1, 0.192), (0.18, 0.1, 0.192), (0.18, 0.44, 0.192), (-0.24, 0.44, 0.192)]
    add(m, V, [(0, 1, 2, 3)], "screen_tv", centre=(0, 0.27, -1), smooth=False)
    m.screen = [(x, y, z + 0.002) for (x, y, z) in V]
    for k in range(3): cyl(m, (0.23, 0.36 - k * 0.07, 0.19), 0.015, 0.02, "metal", n=10, axis="z")
    return m

def flat_tv(w=1.25):
    m = Mesh(); h = w * 0.58
    cube(m, (0, 0.12 + h / 2, 0), (w, h, 0.05), "black")
    V = [(-w / 2 + 0.02, 0.14, 0.026), (w / 2 - 0.02, 0.14, 0.026), (w / 2 - 0.02, 0.1 + h, 0.026), (-w / 2 + 0.02, 0.1 + h, 0.026)]
    add(m, V, [(0, 1, 2, 3)], "screen_tv", centre=(0, 0.4, -1), smooth=False)
    m.screen = [(x, y, z + 0.002) for (x, y, z) in V]
    cube(m, (0, 0.06, 0), (0.08, 0.12, 0.04), "black"); cube(m, (0, 0.01, 0), (0.4, 0.02, 0.22), "black")
    return m

def tv_stand(w=1.5):
    m = Mesh()
    cube(m, (0, 0.24, 0), (w, 0.4, 0.42), "wood_dark")
    for s in (-1, 1): cube(m, (s * w / 4, 0.24, 0.212), (w / 2 - 0.06, 0.32, 0.01), "wood")
    for s in (-1, 1): cube(m, (s * (w / 2 - 0.08), 0.02, 0), (0.05, 0.04, 0.36), "black")
    return m

def couch(w=2.1):
    m = Mesh()
    rbox(m, (0, 0.22, 0.02), (w, 0.3, 0.92), "fabric", p=6, ends=0.06)
    for k in range(3): rbox(m, (-w / 3 + k * w / 3, 0.44, 0.08), (w / 3 - 0.04, 0.16, 0.74), "fabric", p=5)
    rbox(m, (0, 0.62, -0.36), (w - 0.1, 0.5, 0.22), "fabric", p=5, ends=0.08)
    for s in (-1, 1): rbox(m, (s * (w / 2 - 0.09), 0.46, 0.02), (0.18, 0.36, 0.92), "fabric", p=5)
    for s in (-1, 1): rbox(m, (s * w * 0.3, 0.62, -0.18), (0.42, 0.36, 0.13), "pillow2", p=4)
    return m

def fridge(big=False):
    m = Mesh(); w, h, d = (0.82, 1.85, 0.72) if big else (0.5, 0.86, 0.55)
    rbox(m, (0, h / 2, 0), (w, h, d), "appliance", p=10, ends=0.04)
    if big: cube(m, (0, h * 0.68, d / 2 + 0.002), (w - 0.02, 0.008, 0.004), "black")
    cube(m, (w / 2 - 0.07, h * (0.8 if big else 0.6), d / 2 + 0.02), (0.025, 0.3 if big else 0.18, 0.03), "metal")
    return m

def can():
    m = Mesh()
    lathe_y(m, (0, 0, 0), [(0.0, 0.0), (0.028, 0.0), (0.033, 0.01), (0.033, 0.105), (0.027, 0.12), (0.0, 0.12)], "can", n=20)
    return m

def bong():
    m = Mesh()
    lathe_y(m, (0, 0, 0), [(0.0, 0.0), (0.07, 0.0), (0.075, 0.02), (0.07, 0.07), (0.03, 0.11), (0.022, 0.14), (0.022, 0.42), (0.03, 0.44), (0.0, 0.44)], "glass_tint", n=24)
    lathe_y(m, (0, 0, 0), [(0.0, 0.004), (0.066, 0.004), (0.06, 0.05), (0.0, 0.05)], "water", n=20)
    # the bowl on its stem
    V, F = lathe([(0.008, 0.0), (0.008, 0.12)], 10, (0.0, 0.0, 0.0), axis="x")
    ang = math.radians(40)
    V = [(0.03 + x * math.cos(ang), 0.06 + x * math.sin(ang), z) for (x, y_, z) in [(v[0], 0, v[2]) for v in V]]
    lathe_y(m, (0.11, 0.135, 0), [(0.0, 0.0), (0.018, 0.0), (0.024, 0.03), (0.0, 0.03)], "glass_tint", n=12)
    cube(m, (0.065, 0.1, 0), (0.09, 0.012, 0.012), "glass_tint")
    return m

def fish_tank(w=0.9):
    m = Mesh(); d, h = 0.36, 0.48
    cube(m, (0, 0.38, 0), (w + 0.06, 0.76, d + 0.06), "wood_dark")                       # stand
    cube(m, (0, 0.78, 0), (w, 0.04, d), "black")
    rbox(m, (0, 0.78 + h / 2, 0), (w, h, d), "tank_glass", p=14, ends=0.02)
    cube(m, (0, 0.78 + 0.03, 0), (w - 0.03, 0.05, d - 0.03), "gravel")
    cube(m, (0, 0.78 + h * 0.46, 0), (w - 0.03, h * 0.82, d - 0.03), "water")
    cube(m, (0, 0.78 + h + 0.02, 0), (w + 0.01, 0.04, d + 0.01), "black")                # hood
    for k in range(5):
        x = -w / 2 + 0.12 + k * (w - 0.24) / 4
        lathe_y(m, (x, 0.82, -0.08 + (k % 2) * 0.1), [(0.0, 0), (0.03, 0), (0.0, 0.14 + (k % 3) * 0.06)], "leaf", n=6)
    return m

def fish():
    m = Mesh()
    rbox(m, (0, 0, 0), (0.07, 0.035, 0.018), "fish", p=2.2, ends=0.5)
    V = [(-0.03, 0, 0), (-0.06, 0.02, 0), (-0.06, -0.02, 0)]
    add(m, V + [(v[0], v[1], v[2] + 0.002) for v in V], [(0, 1, 2), (3, 5, 4)], "fish", centre=(-0.045, 0, 0.001), smooth=False)
    return m

def wardrobe(w=1.0, h=2.0):
    m = Mesh(); d = 0.6
    cube(m, (0, h / 2, 0), (w, h, d), "wood")
    cube(m, (0, h / 2, d / 2 + 0.001), (0.006, h - 0.1, 0.004), "wood_dark")
    for s in (-1, 1): cube(m, (s * 0.06, h * 0.52, d / 2 + 0.02), (0.02, 0.28, 0.025), "metal")
    return m

def rack(w=1.2):
    m = Mesh(); h = 1.6
    for s in (-1, 1): cyl(m, (s * w / 2, 0, 0), 0.015, h, "metal", n=10); cube(m, (s * w / 2, 0.02, 0), (0.06, 0.04, 0.45), "metal")
    cyl(m, (-w / 2, h, 0), 0.012, w, "metal", n=10, axis="x")
    cols = ["shirt_a", "shirt_b", "shirt_c", "shirt_a", "shirt_d", "shirt_b"]
    for k, c in enumerate(cols):
        x = -w / 2 + 0.12 + k * (w - 0.24) / (len(cols) - 1)
        rbox(m, (x, h - 0.42, 0), (0.06, 0.72, 0.44), c, p=4, ends=0.2)
    return m

def nightstand():
    m = Mesh()
    cube(m, (0, 0.27, 0), (0.48, 0.54, 0.4), "wood")
    cube(m, (0, 0.38, 0.201), (0.4, 0.16, 0.004), "wood_dark")
    cube(m, (0, 0.38, 0.215), (0.1, 0.015, 0.02), "metal")
    return m

def lamp():
    m = Mesh()
    lathe_y(m, (0, 0, 0), [(0, 0), (0.16, 0), (0.15, 0.03), (0, 0.03)], "black")
    cyl(m, (0, 0.03, 0), 0.012, 1.45, "metal", n=10)
    lathe_y(m, (0, 1.4, 0), [(0.14, 0), (0.2, 0), (0.12, 0.28), (0.08, 0.28)], "lampshade", n=24)
    return m

def plant():
    m = Mesh()
    lathe_y(m, (0, 0, 0), [(0, 0), (0.13, 0), (0.17, 0.32), (0.18, 0.34), (0, 0.34)], "pot")
    for k in range(9):
        a = 2 * math.pi * k / 9 + 0.3; L = 0.45 + (k % 3) * 0.12; tilt = 0.5 + (k % 2) * 0.25
        cx, cy, cz = math.cos(a) * L * 0.35 * tilt, 0.34 + L * 0.55, math.sin(a) * L * 0.35 * tilt
        rbox(m, (cx, cy, cz), (0.07, L * 0.85, 0.02), "leaf", p=2.2, ends=0.5)
    return m

def rug(w=2.0, d=1.4):
    m = Mesh(); V = [(-w / 2, 0.006, -d / 2), (w / 2, 0.006, -d / 2), (w / 2, 0.006, d / 2), (-w / 2, 0.006, d / 2)]
    add(m, V, [(0, 1, 2, 3)], "rug", centre=(0, -1, 0), smooth=False); return m

def coffee_table():
    m = Mesh()
    cube(m, (0, 0.4, 0), (1.0, 0.04, 0.55), "wood")
    for sx in (-1, 1):
        for sz in (-1, 1): cube(m, (sx * 0.44, 0.19, sz * 0.22), (0.04, 0.38, 0.04), "black")
    return m

PROPS = {"bed": bed, "bed_single": lambda: bed(1.05, 2.0), "bed_king": lambda: bed(2.0, 2.15), "desk": desk, "chair": chair, "laptop": laptop, "crt": crt, "flat_tv": flat_tv, "flat_tv_big": lambda: flat_tv(1.8),
         "tv_stand": tv_stand, "couch": couch, "couch_big": lambda: couch(2.8), "mini_fridge": fridge, "fridge": lambda: fridge(True), "can": can, "bong": bong,
         "fish_tank": fish_tank, "fish_tank_big": lambda: fish_tank(1.6), "fish": fish, "wardrobe": wardrobe, "rack": rack, "nightstand": nightstand, "lamp": lamp,
         "plant": plant, "rug": rug, "coffee_table": coffee_table}

PREVIEW = {"wood": ((0.55, 0.36, 0.22), 0.3, 0), "wood_dark": ((0.3, 0.19, 0.12), 0.3, 0), "black": ((0.06, 0.06, 0.07), 0.3, 0), "metal": ((0.7, 0.7, 0.74), 1.0, 0),
           "sheet": ((0.9, 0.9, 0.92), 0.1, 0), "pillow": ((0.95, 0.95, 0.97), 0.1, 0), "blanket": ((0.25, 0.3, 0.55), 0.1, 0), "chair": ((0.12, 0.12, 0.14), 0.3, 0),
           "laptop": ((0.6, 0.6, 0.64), 0.8, 0), "screen": ((0.4, 0.8, 1.0), 0, 1), "plastic_dk": ((0.15, 0.14, 0.13), 0.4, 0), "screen_tv": ((0.3, 0.6, 0.3), 0, 1),
           "fabric": ((0.45, 0.3, 0.55), 0.1, 0), "pillow2": ((0.9, 0.6, 0.3), 0.1, 0), "appliance": ((0.9, 0.9, 0.9), 0.6, 0), "can": ((0.8, 0.75, 0.2), 1.0, 0),
           "glass_tint": ((0.4, 0.8, 0.7), 1.2, 0), "water": ((0.3, 0.5, 0.8), 0.8, 0), "tank_glass": ((0.6, 0.8, 0.9), 1.2, 0), "gravel": ((0.6, 0.55, 0.45), 0.1, 0),
           "leaf": ((0.2, 0.55, 0.25), 0.2, 0), "fish": ((1.0, 0.5, 0.1), 0.6, 0), "shirt_a": ((0.8, 0.2, 0.2), 0.1, 0), "shirt_b": ((0.2, 0.4, 0.8), 0.1, 0),
           "shirt_c": ((0.9, 0.9, 0.9), 0.1, 0), "shirt_d": ((0.15, 0.15, 0.15), 0.1, 0), "lampshade": ((1.0, 0.85, 0.6), 0, 1), "pot": ((0.75, 0.45, 0.3), 0.2, 0),
           "rug": ((0.5, 0.2, 0.2), 0.1, 0)}

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "out"; os.makedirs(out, exist_ok=True)
    from PIL import Image
    names = sys.argv[2].split(",") if len(sys.argv) > 2 else list(PROPS)
    tiles = []; meta = {}
    for n in names:
        m = PROPS[n](); save_bin(m, os.path.join(out, n + ".bytes"))
        Vall = np.vstack([np.array(p["v"]) for p in m.parts.values()])
        meta[n] = {"min": Vall.min(0).round(4).tolist(), "max": Vall.max(0).round(4).tolist()}
        if hasattr(m, "screen"): meta[n]["screen"] = [list(map(lambda q: round(float(q), 4), v)) for v in m.screen]
        V = np.vstack([np.array(p["v"]) for p in m.parts.values()]); c = (V.min(0) + V.max(0)) / 2; size = np.linalg.norm(V.max(0) - V.min(0))
        img = render(m, PREVIEW, size=(300, 260), cam=(c[0] + size * 0.9, c[1] + size * 0.75, c[2] + size * 1.5), look=tuple(c), fov=32, bg=(0.16, 0.13, 0.12),
                     lights=[((0.4, 0.8, 0.6), (1, 0.95, 0.9)), ((-0.6, 0.3, 0.4), (0.4, 0.45, 0.55))])
        tiles.append((n, Image.fromarray(img)))
    cols = 6; rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * 300, rows * 260), (20, 18, 16))
    from PIL import ImageDraw
    d = ImageDraw.Draw(sheet)
    for k, (n, im) in enumerate(tiles):
        sheet.paste(im, ((k % cols) * 300, (k // cols) * 260)); d.text(((k % cols) * 300 + 6, (k // cols) * 260 + 4), n, fill=(230, 220, 200))
    sheet.save(os.path.join(out, "_sheet.png")); print("ok", len(tiles))
    import json; json.dump(meta, open(os.path.join(out, "props.json"), "w"))
