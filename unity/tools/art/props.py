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


# ------------------------------------------------------------------ the nicer places
def kitchen_island(w=2.2):
    m = Mesh(); d = 0.9; h = 0.9
    cube(m, (0, h / 2 - 0.02, 0), (w, h - 0.04, d), "cabinet")                       # cabinets
    cube(m, (0, h, 0), (w + 0.08, 0.04, d + 0.08), "marble")                         # the slab
    for k in range(3): cube(m, (-w / 2 + 0.15 + k * 0.3, h - 0.3, -d / 2 - 0.012), (0.015, 0.3, 0.015), "metal")   # handles at the back
    cube(m, (w * 0.22, h + 0.02, -0.1), (0.55, 0.012, 0.42), "metal")               # sink
    lathe_y(m, (w * 0.22, h + 0.02, -0.34), [(0.0, 0), (0.012, 0), (0.012, 0.3), (0.0, 0.3)], "metal", n=10)
    cube(m, (w * 0.22, h + 0.3, -0.24), (0.024, 0.02, 0.2), "metal")
    cube(m, (-w * 0.25, h + 0.03, 0.05), (0.4, 0.02, 0.3), "wood_dark")              # a board with lemons
    lathe_y(m, (-w * 0.25 - 0.08, h + 0.04, 0.05), [(0.0, 0), (0.035, 0), (0.04, 0.03), (0.03, 0.06), (0.0, 0.07)], "lemon", n=12)
    lathe_y(m, (-w * 0.25 + 0.05, h + 0.04, 0.09), [(0.0, 0), (0.035, 0), (0.04, 0.03), (0.03, 0.06), (0.0, 0.07)], "lemon", n=12)
    # glass and a bottle
    lathe_y(m, (w * 0.42, h + 0.04, 0.2), [(0.0, 0), (0.03, 0), (0.032, 0.14), (0.0, 0.14)], "glass_tint", n=14)
    lathe_y(m, (w * 0.42, h + 0.04, -0.2), [(0.0, 0), (0.035, 0), (0.035, 0.18), (0.012, 0.24), (0.012, 0.3), (0.0, 0.3)], "bottle", n=14)
    return m

def bar_stool():
    m = Mesh()
    lathe_y(m, (0, 0, 0), [(0, 0), (0.18, 0), (0.17, 0.02), (0.0, 0.02)], "metal")
    cyl(m, (0, 0.02, 0), 0.022, 0.64, "metal", n=12)
    rbox(m, (0, 0.7, 0), (0.36, 0.08, 0.36), "leather_seat", p=6, ends=0.3)
    return m

def sofa_l(w=2.8, d=2.0):
    m = Mesh(); dd = 0.95
    rbox(m, (0, 0.2, 0.0), (w, 0.26, dd), "fabric", p=6, ends=0.06)                 # the long seat
    for k in range(3): rbox(m, (-w / 3 + k * w / 3, 0.4, 0.06), (w / 3 - 0.04, 0.14, dd - 0.2), "fabric", p=5)
    rbox(m, (0, 0.58, -dd / 2 + 0.11), (w, 0.44, 0.22), "fabric", p=5, ends=0.08)    # back
    # the chaise leg coming forward on the right
    rbox(m, (w / 2 - dd / 2, 0.2, d / 2 - 0.1), (dd, 0.26, d - dd), "fabric", p=6, ends=0.08)
    rbox(m, (w / 2 - dd / 2, 0.4, d / 2 - 0.1), (dd - 0.2, 0.14, d - dd - 0.1), "fabric", p=5)
    rbox(m, (w / 2 - 0.09, 0.5, d / 2 - 0.1), (0.18, 0.34, d - dd), "fabric", p=5)
    rbox(m, (-w / 2 + 0.09, 0.46, 0.02), (0.18, 0.36, dd), "fabric", p=5)
    for k, x in enumerate((-w * 0.32, -w * 0.05)): rbox(m, (x, 0.6, -0.2), (0.44, 0.38, 0.14), "pillow2" if k else "pillow3", p=4)
    m.blocks = [(-w / 2, -dd / 2, w / 2, dd / 2), (w / 2 - dd, dd / 2 - 0.1, w / 2, d - dd / 2 - 0.1)]   # the seat and the chaise, not the corner between
    return m

def pool_table():
    m = Mesh(); w, d, h = 2.3, 1.3, 0.8
    cube(m, (0, h - 0.12, 0), (w, 0.16, d), "wood_dark")                           # frame
    cube(m, (0, h - 0.035, 0), (w - 0.26, 0.01, d - 0.26), "felt")                  # the cloth
    for sx in (-1, 1):
        cube(m, (sx * (w / 2 - 0.08), h - 0.02, 0), (0.16, 0.05, d - 0.3), "rail")
        for sz in (-1, 1): cube(m, (sx * (w / 2 - 0.09), (h - 0.2) / 2, sz * (d / 2 - 0.09)), (0.14, h - 0.2, 0.14), "wood_dark")
    for sz in (-1, 1): cube(m, (0, h - 0.02, sz * (d / 2 - 0.08)), (w - 0.3, 0.05, 0.16), "rail")
    for sx in (-1, 0, 1):
        for sz in (-1, 1): lathe_y(m, (sx * (w / 2 - 0.13), h - 0.04, sz * (d / 2 - 0.13)), [(0, 0), (0.06, 0), (0.06, 0.03), (0, 0.03)], "black", n=12)
    # balls racked, and the cue ball at the other end
    cols = ["ball_yellow", "ball_blue", "ball_red", "ball_purple", "ball_orange", "ball_green", "ball_maroon", "black", "ball_yellow", "ball_blue"]
    k = 0
    for row in range(4):
        for i in range(row + 1):
            x = w * 0.22 + row * 0.05; z = (i - row / 2) * 0.058
            m_ = m; lathe_y(m_, (x, h - 0.03, z), [(0, 0), (0.026, 0.002), (0.028, 0.028), (0.02, 0.05), (0, 0.056)], cols[k % len(cols)], n=12); k += 1
    lathe_y(m, (-w * 0.28, h - 0.03, 0.0), [(0, 0), (0.026, 0.002), (0.028, 0.028), (0.02, 0.05), (0, 0.056)], "ball_white", n=12, part="cueball")
    m.cue = (-w * 0.28, h, 0.0)
    cyl(m, (-w / 2 + 0.3, h + 0.02, d / 2 - 0.05), 0.012, 1.4, "wood", n=8, axis="x")   # a cue resting on the rail
    return m

def grand_piano():
    m = Mesh(); h = 0.95
    # the body: a lid-shaped loft in plan (straight on the keyboard side, curved tail)
    pts = []
    for k in range(13):
        a = math.pi * k / 12; pts.append((-0.6 + math.cos(a) * 0.75 * (0.7 if k > 6 else 1.0), -0.7 + math.sin(a) * 0.8))
    plan = [(-1.35, -0.75), (0.15, -0.75), (0.15, 0.75), (-0.9, 0.75)] + pts
    plan = [(-1.35, -0.75), (0.15, -0.75), (0.15, 0.75)] + [(-0.3 - 0.55 * math.sin(math.pi * k / 10), 0.75 - 0.0 * k) for k in range(1)] + [(-0.5, 0.75), (-0.95, 0.65), (-1.25, 0.35), (-1.38, -0.1), (-1.35, -0.5)]
    V = [(x, h - 0.3, z) for (x, z) in plan] + [(x, h, z) for (x, z) in plan]; n = len(plan)
    F = [tuple(range(n - 1, -1, -1)), tuple(range(n, 2 * n))] + [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    add(m, V, F, "piano", centre=(-0.5, h - 0.15, 0), smooth=False)
    # the lid, propped open
    lidV = [(x, h + 0.02 + (x + 1.35) * 0.3, z) for (x, z) in plan]; lidV2 = [(x, y + 0.03, z) for (x, y, z) in lidV]
    LV = lidV + lidV2; LF = [tuple(range(n - 1, -1, -1)), tuple(range(n, 2 * n))] + [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    add(m, LV, LF, "piano", centre=(-0.5, h + 0.2, 0), smooth=False)
    cyl(m, (-0.3, h, 0.5), 0.015, 0.4, "wood_dark", n=8)                         # the prop stick
    cube(m, (0.3, h - 0.18, 0), (0.3, 0.06, 1.3), "piano")                        # key bed
    cube(m, (0.33, h - 0.14, 0), (0.24, 0.02, 1.2), "ivory")
    for k in range(18): cube(m, (0.31, h - 0.12, -0.57 + k * 0.067), (0.14, 0.015, 0.02), "black")
    for (x, z) in ((-1.1, -0.5), (-1.1, 0.45), (0.3, -0.6), (0.3, 0.6)): cyl(m, (x, 0, z), 0.04, h - 0.3, "piano", n=10)
    cube(m, (1.2, 0.5, 0), (0.4, 0.06, 0.9), "piano"); cube(m, (1.2, 0.25, 0), (0.3, 0.5, 0.1), "piano")   # the bench, room to stand at the keys
    m.keys = (0.33, h - 0.1, 0)
    m.blocks = [(-1.38, -0.75, 0.45, 0.75), (1.0, -0.45, 1.4, 0.45)]   # body and bench; you stand between them
    return m

def fireplace(w=1.8):
    m = Mesh(); h = 1.2; d = 0.45; ow = w * 0.55
    pw = (w - ow) / 2
    for sx in (-1, 1): cube(m, (sx * (w / 2 - pw / 2), h / 2, 0), (pw, h, d), "stone")     # the surround
    cube(m, (0, h - 0.15, 0), (ow, 0.3, d), "stone")
    cube(m, (0, 0.02, 0), (ow, 0.04, d), "stone")
    cube(m, (0, 0.45, -d / 2 + 0.04), (ow, 0.9, 0.08), "firebox")                      # the dark back of the hearth
    for sx in (-1, 1): cube(m, (sx * (ow / 2 - 0.02), 0.45, 0), (0.04, 0.9, d), "firebox")
    cube(m, (0, h + 0.04, 0), (w + 0.1, 0.08, d + 0.1), "marble")                      # mantel
    for k in range(3): cyl(m, (-0.3 + k * 0.3, 0.09 + (k % 2) * 0.06, -0.12), 0.05, 0.3, "wood_dark", n=8, axis="z")
    # flames: crossed fins the game can glow, and a bed of embers
    for (dx, sc) in ((-0.2, 0.8), (0.05, 1.0), (0.28, 0.7)):
        V = [(dx - 0.16 * sc, 0.08, 0.02), (dx + 0.16 * sc, 0.08, 0.02), (dx + 0.07 * sc, 0.48 * sc, 0.02), (dx - 0.02 * sc, 0.7 * sc, 0.02), (dx - 0.13 * sc, 0.4 * sc, 0.02)]
        add(m, V, [(0, 1, 2, 3, 4)], "flame", part="flame", centre=(dx, 0.3, -1), smooth=False)
        V2 = [(dx, 0.08, -0.14), (dx, 0.08, 0.16), (dx, 0.44 * sc, 0.1), (dx, 0.64 * sc, 0.0), (dx, 0.44 * sc, -0.1)]
        add(m, V2, [(0, 1, 2, 3, 4)], "flame", part="flame", centre=(dx - 1, 0.3, 0.0), smooth=False)
    cube(m, (0, 0.05, -0.02), (ow - 0.1, 0.03, 0.28), "ember", part="flame")
    lathe_y(m, (-0.55, h + 0.08, 0), [(0, 0), (0.05, 0), (0.07, 0.12), (0.03, 0.14), (0, 0.14)], "ceramic", n=12)   # vase
    cube(m, (0.5, h + 0.2, 0), (0.28, 0.24, 0.02), "frame"); cube(m, (0.5, h + 0.2, 0.012), (0.22, 0.18, 0.004), "photo")
    return m

def chandelier():
    m = Mesh()
    cyl(m, (0, 0.6, 0), 0.008, 0.5, "gold", n=8)
    lathe_y(m, (0, 0.5, 0), [(0, 0), (0.08, 0), (0.1, 0.06), (0.0, 0.08)], "gold", n=16)
    lathe_y(m, (0, 0.0, 0), [(0.0, 0), (0.12, 0.0), (0.38, 0.3), (0.42, 0.34), (0.1, 0.5), (0, 0.5)], "gold", n=20)
    for k in range(8):
        a = 2 * math.pi * k / 8
        for r_, y_ in ((0.34, 0.25), (0.2, 0.12)):
            lathe_y(m, (math.cos(a) * r_, y_, math.sin(a) * r_), [(0, 0), (0.015, 0), (0.02, 0.1), (0.0, 0.1)], "crystal", n=8)
    lathe_y(m, (0, 0.36, 0), [(0, 0), (0.14, 0), (0.2, 0.1), (0.14, 0.2), (0, 0.2)], "lampshade", n=16)
    return m

def open_box(m, w, h, d, mat, back=0.02, side=0.03):
    """a cabinet: back panel, two sides, top and bottom; open at the front (+z)"""
    cube(m, (0, h / 2, -d / 2 + back / 2), (w, h, back), mat)
    for sx in (-1, 1): cube(m, (sx * (w / 2 - side / 2), h / 2, 0), (side, h, d), mat)
    cube(m, (0, h - 0.015, 0), (w, 0.03, d), mat); cube(m, (0, 0.03, 0), (w, 0.06, d), mat)

def bookshelf(w=1.6, h=2.1):
    m = Mesh(); d = 0.36
    open_box(m, w, h, d, "wood_dark")
    cols = ["book_a", "book_b", "book_c", "book_d", "book_a", "book_c"]
    for row in range(5):
        y = 0.12 + row * 0.4
        cube(m, (0, y, 0), (w - 0.06, 0.03, d - 0.04), "wood")
        x = -w / 2 + 0.08; k = 0
        while x < w / 2 - 0.2:
            bw_ = 0.03 + (k % 3) * 0.012; bh = 0.22 + (k % 4) * 0.03
            if (k + row) % 7 == 3: x += 0.12
            cube(m, (x + bw_ / 2, y + 0.015 + bh / 2, 0.02), (bw_, bh, d - 0.12), cols[(k + row) % len(cols)]); x += bw_ + 0.006; k += 1
    lathe_y(m, (w * 0.3, 1.74, 0.02), [(0, 0), (0.05, 0), (0.06, 0.1), (0.1, 0.22), (0.0, 0.24)], "gold", n=12)   # a trophy on top shelf
    return m

def dining_table(w=1.8):
    m = Mesh(); h = 0.76; d = 0.95
    cube(m, (0, h - 0.02, 0), (w, 0.04, d), "wood")
    for sx in (-1, 1):
        for sz in (-1, 1): cube(m, (sx * (w / 2 - 0.06), (h - 0.04) / 2, sz * (d / 2 - 0.06)), (0.06, h - 0.04, 0.06), "wood_dark")
    for k in range(2):
        px = -w * 0.25 + k * w * 0.5
        lathe_y(m, (px, h, 0.0), [(0, 0), (0.13, 0), (0.14, 0.012), (0, 0.012)], "ceramic", n=16)
    lathe_y(m, (0, h, 0), [(0, 0), (0.04, 0), (0.03, 0.2), (0.02, 0.3), (0.0, 0.32)], "glass_tint", n=12)   # a vase
    return m

def dining_chair():
    m = Mesh()
    rbox(m, (0, 0.45, 0), (0.44, 0.06, 0.44), "leather_seat", p=5)
    rbox(m, (0, 0.78, -0.2), (0.42, 0.6, 0.05), "leather_seat", p=5)
    for sx in (-1, 1):
        for sz in (-1, 1): cube(m, (sx * 0.19, 0.21, sz * 0.19), (0.03, 0.42, 0.03), "metal")
    return m

def floor_lamp():
    m = Mesh()
    lathe_y(m, (0, 0, 0), [(0, 0), (0.17, 0), (0.16, 0.025), (0, 0.025)], "metal")
    m.blocks = [(-0.2, -0.2, 0.2, 0.2)]   # only the base is on the floor; you walk under the arc
    cyl(m, (0, 0.02, 0), 0.012, 1.3, "metal", n=10)
    # the arc, as a tube of short cylinders
    for k in range(10):
        a = math.pi * 0.5 * k / 10; x = math.sin(a) * 0.8; y = 1.32 + (1 - math.cos(a)) * 0.0 + math.sin(a * 2) * 0.1
        cyl(m, (x, y + 0.35 * (k / 10), 0), 0.012, 0.09, "metal", n=8, axis="x")
    lathe_y(m, (0.8, 1.35, 0), [(0.12, 0.3), (0.22, 0.3), (0.3, 0.0), (0.12, 0.0)], "lampshade", n=24)
    return m

def art(w=1.2, h=0.9):
    """a framed canvas; the game puts the picture on the quad"""
    m = Mesh()
    cube(m, (0, 0, 0), (w + 0.08, h + 0.08, 0.04), "frame")
    V = [(-w / 2, -h / 2, 0.022), (w / 2, -h / 2, 0.022), (w / 2, h / 2, 0.022), (-w / 2, h / 2, 0.022)]
    add(m, V, [(0, 1, 2, 3)], "canvas", centre=(0, 0, -1), smooth=False)
    m.screen = [(x, y, z + 0.002) for (x, y, z) in V]
    return m

def daybed():
    m = Mesh(); w, d = 2.1, 1.1
    cube(m, (0, 0.16, 0), (w, 0.1, d), "rattan")
    for sx in (-1, 1):
        for sz in (-1, 1): cube(m, (sx * (w / 2 - 0.08), 0.06, sz * (d / 2 - 0.08)), (0.08, 0.12, 0.08), "rattan")
    rbox(m, (0, 0.3, 0), (w - 0.1, 0.2, d - 0.1), "sheet", p=6, ends=0.08)
    for s in (-1, 1): rbox(m, (s * w * 0.3, 0.46, -d / 2 + 0.3), (0.5, 0.14, 0.4), "pillow2", p=4)
    # a canopy on four posts
    for sx in (-1, 1):
        for sz in (-1, 1): cyl(m, (sx * (w / 2 - 0.05), 0, sz * (d / 2 - 0.05)), 0.03, 2.1, "rattan", n=10)
    cube(m, (0, 2.12, 0), (w + 0.1, 0.04, d + 0.1), "rattan")
    rbox(m, (0, 2.0, 0), (w, 0.08, d), "sheet", p=3, ends=0.2)
    return m

def frond(m, top, ang, L, mat="leaf", lift=0.3):
    """one palm frond: a tapered ribbon that arcs up and droops, two-sided"""
    pts = []
    for k in range(9):
        t = k / 8; r = L * t; y = top[1] + lift * math.sin(t * math.pi) - 0.9 * t * t
        pts.append((top[0] + math.cos(ang) * r, y, top[2] + math.sin(ang) * r))
    V = []; F = []
    for k, (x, y, z) in enumerate(pts):
        hw = 0.16 * math.sin(math.pi * min(1, 0.15 + k / 8 * 0.95)) + 0.02
        sx, sz = -math.sin(ang) * hw, math.cos(ang) * hw
        V.append((x + sx, y, z + sz)); V.append((x - sx, y, z - sz))
    n = len(V); V = V + [(x, y - 0.012, z) for (x, y, z) in V]   # a thin leaf: top and underside a hair apart
    for k in range(8):
        a, b = 2 * k, 2 * k + 1; c, d = 2 * k + 2, 2 * k + 3
        F.append((a, b, d, c)); F.append((n + c, n + d, n + b, n + a))
    m.add("leaves", V, F, mat, True)

def palm():
    m = Mesh()
    lathe_y(m, (0, 0, 0), [(0, 0), (0.3, 0), (0.33, 0.4), (0.3, 0.44), (0, 0.44)], "pot")
    m.blocks = [(-0.36, -0.36, 0.36, 0.36)]   # the pot; the fronds are overhead
    for k in range(10):
        y = 0.44 + k * 0.2; cyl(m, (math.sin(k * 0.25) * 0.06, y, 0), 0.08 - k * 0.004, 0.22, "trunk", n=10)
    top = (math.sin(9 * 0.25) * 0.06, 2.5, 0)
    for k in range(9):
        a = 2 * math.pi * k / 9 + 0.2; frond(m, top, a, 1.3 + (k % 2) * 0.25, lift=0.25 + (k % 3) * 0.1)
    lathe_y(m, top, [(0, 0), (0.1, 0), (0.08, 0.12), (0, 0.14)], "trunk", n=8)
    return m

def tiki_bar(w=2.0):
    m = Mesh(); h = 1.1; d = 0.7
    cube(m, (0, h / 2, 0), (w, h, d), "bamboo")
    cube(m, (0, h, 0), (w + 0.1, 0.06, d + 0.1), "wood")
    for k in range(6): cyl(m, (-w / 2 + 0.1 + k * (w - 0.2) / 5, 0, d / 2 + 0.01), 0.025, h, "bamboo", n=8)
    for sx in (-1, 1): cyl(m, (sx * (w / 2 - 0.1), 0, -d / 2 + 0.1), 0.04, 2.3, "bamboo", n=10)
    # a thatched roof
    for k in range(7):
        y = 2.3 - k * 0.07; cube(m, (0, y, -d / 2 + 0.1 + k * 0.13), (w + 0.5, 0.05, 0.3), "thatch")
    for k in range(3): lathe_y(m, (-w * 0.3 + k * 0.3, h + 0.06, 0.1), [(0, 0), (0.035, 0), (0.03, 0.12), (0.04, 0.16), (0, 0.17)], "glass_tint", n=12)
    lathe_y(m, (w * 0.35, h + 0.06, 0.0), [(0, 0), (0.1, 0), (0.1, 0.25), (0.06, 0.3), (0, 0.32)], "pineapple", n=12)
    return m

def trophy_case(w=1.4, h=1.9):
    m = Mesh(); d = 0.45
    open_box(m, w, h, d, "wood_dark")
    cube(m, (0, h / 2 + 0.1, d / 2 - 0.005), (w - 0.1, h - 0.3, 0.004), "tank_glass")
    for row in range(3):
        y = 0.3 + row * 0.5
        cube(m, (0, y, 0), (w - 0.08, 0.025, d - 0.06), "wood")
        for k in range(3):
            x = -w / 2 + 0.25 + k * (w - 0.5) / 2
            if (row + k) % 3 == 0: lathe_y(m, (x, y + 0.013, 0.02), [(0, 0), (0.07, 0), (0.05, 0.03), (0.02, 0.12), (0.09, 0.26), (0.0, 0.3)], "gold", n=14)   # cup
            elif (row + k) % 3 == 1: lathe_y(m, (x, y + 0.013, 0.02), [(0, 0), (0.05, 0), (0.05, 0.02), (0.012, 0.04), (0.012, 0.18), (0.0, 0.18)], "gold", n=10); lathe_y(m, (x, y + 0.19, 0.02), [(0, 0), (0.04, 0), (0.05, 0.02), (0.0, 0.04)], "gold", n=12)   # ring on a stand
            else: cube(m, (x, y + 0.1, 0.0), (0.16, 0.17, 0.03), "frame"); cube(m, (x, y + 0.1, 0.017), (0.12, 0.13, 0.004), "photo")   # a framed bracelet
    for k in range(3): cube(m, (0, 0.3 + k * 0.5 + 0.47, 0), (w - 0.08, 0.01, d - 0.06), "lampshade")   # shelf lights
    return m

def poker_table():
    m = Mesh(); h = 0.78
    # an oval: a loft of superellipse rings
    rings = []
    for (y, k) in ((h - 0.1, 1.0), (h - 0.02, 1.0), (h, 0.97)):
        rings.append([(xx * 1.2 * k, y, yy * 0.7 * k) for (yy, xx) in superellipse(40, 2.4)])
    V, F = loft(rings); add(m, V, F, "rail_pad", centre=(0, h - 0.05, 0))
    ring_in = [(xx * 1.05, h + 0.001, yy * 0.56) for (yy, xx) in superellipse(40, 2.4)]
    cen = len(ring_in); V2 = ring_in + [(0, h + 0.001, 0)]
    add(m, V2, [(i, (i + 1) % cen, cen) for i in range(cen)], "felt", centre=(0, -1, 0), smooth=False)
    lathe_y(m, (0, 0, 0), [(0, 0), (0.5, 0), (0.2, 0.08), (0.15, h - 0.15), (0.6, h - 0.1), (0, h - 0.1)], "wood_dark", n=24)
    # chips and cards
    for k in range(6):
        a = 2 * math.pi * k / 6 + 0.4; x, z = math.cos(a) * 0.72, math.sin(a) * 0.36
        for j in range(3): lathe_y(m, (x + j * 0.05, h, z), [(0, 0), (0.02, 0), (0.02, 0.03), (0, 0.03)], ["chip_red", "chip_blue", "chip_black"][j], n=10)
        cube(m, (x - 0.05, h + 0.002, z + 0.1), (0.06, 0.002, 0.09), "card")
    cube(m, (0.1, h + 0.002, 0.02), (0.07, 0.002, 0.1), "card"); cube(m, (0.0, h + 0.002, 0.0), (0.07, 0.002, 0.1), "card"); cube(m, (-0.1, h + 0.002, -0.02), (0.07, 0.002, 0.1), "card")
    lathe_y(m, (-0.3, h, 0.0), [(0, 0), (0.035, 0), (0.035, 0.012), (0, 0.012)], "ivory", n=12)   # the button
    return m

def safe():
    m = Mesh(); w, h, d = 0.7, 0.9, 0.6
    rbox(m, (0, h / 2, 0), (w, h, d), "safe_steel", p=8, ends=0.05)
    cube(m, (0, h / 2, d / 2 + 0.01), (w - 0.1, h - 0.1, 0.02), "safe_door")
    lathe_y(m, (0, h / 2, d / 2 + 0.02), [(0, 0), (0.09, 0), (0.09, 0.03), (0, 0.03)], "metal", n=16)   # dial (on its side: cheat with a flat disc)
    for k in range(3): cyl(m, (-w / 2 + 0.12, h * 0.3 + k * 0.15, d / 2 + 0.02), 0.012, 0.08, "metal", n=8, axis="z")
    cube(m, (0.18, h / 2, d / 2 + 0.035), (0.03, 0.2, 0.03), "metal")
    return m

PROPS = {"bed": bed, "bed_single": lambda: bed(1.05, 2.0), "bed_king": lambda: bed(2.0, 2.15), "desk": desk, "chair": chair, "laptop": laptop, "crt": crt, "flat_tv": flat_tv, "flat_tv_big": lambda: flat_tv(1.8),
         "tv_stand": tv_stand, "couch": couch, "couch_big": lambda: couch(2.8), "mini_fridge": fridge, "fridge": lambda: fridge(True), "can": can, "bong": bong,
         "fish_tank": fish_tank, "fish_tank_big": lambda: fish_tank(1.6), "fish": fish, "wardrobe": wardrobe, "rack": rack, "nightstand": nightstand, "lamp": lamp,
         "plant": plant, "rug": rug, "coffee_table": coffee_table,
         "kitchen_island": kitchen_island, "bar_stool": bar_stool, "sofa_l": sofa_l, "pool_table": pool_table, "grand_piano": grand_piano, "fireplace": fireplace,
         "chandelier": chandelier, "bookshelf": bookshelf, "dining_table": dining_table, "dining_chair": dining_chair, "floor_lamp": floor_lamp, "art": art, "art_wide": lambda: art(1.8, 1.0),
         "daybed": daybed, "palm": palm, "tiki_bar": tiki_bar, "trophy_case": trophy_case, "poker_table": poker_table, "safe": safe}

PREVIEW = {"wood": ((0.55, 0.36, 0.22), 0.3, 0), "wood_dark": ((0.3, 0.19, 0.12), 0.3, 0), "black": ((0.06, 0.06, 0.07), 0.3, 0), "metal": ((0.7, 0.7, 0.74), 1.0, 0),
           "sheet": ((0.9, 0.9, 0.92), 0.1, 0), "pillow": ((0.95, 0.95, 0.97), 0.1, 0), "blanket": ((0.25, 0.3, 0.55), 0.1, 0), "chair": ((0.12, 0.12, 0.14), 0.3, 0),
           "laptop": ((0.6, 0.6, 0.64), 0.8, 0), "screen": ((0.4, 0.8, 1.0), 0, 1), "plastic_dk": ((0.15, 0.14, 0.13), 0.4, 0), "screen_tv": ((0.3, 0.6, 0.3), 0, 1),
           "fabric": ((0.45, 0.3, 0.55), 0.1, 0), "pillow2": ((0.9, 0.6, 0.3), 0.1, 0), "appliance": ((0.9, 0.9, 0.9), 0.6, 0), "can": ((0.8, 0.75, 0.2), 1.0, 0),
           "glass_tint": ((0.4, 0.8, 0.7), 1.2, 0), "water": ((0.3, 0.5, 0.8), 0.8, 0), "tank_glass": ((0.6, 0.8, 0.9), 1.2, 0), "gravel": ((0.6, 0.55, 0.45), 0.1, 0),
           "leaf": ((0.2, 0.55, 0.25), 0.2, 0), "fish": ((1.0, 0.5, 0.1), 0.6, 0), "shirt_a": ((0.8, 0.2, 0.2), 0.1, 0), "shirt_b": ((0.2, 0.4, 0.8), 0.1, 0),
           "shirt_c": ((0.9, 0.9, 0.9), 0.1, 0), "shirt_d": ((0.15, 0.15, 0.15), 0.1, 0), "lampshade": ((1.0, 0.85, 0.6), 0, 1), "pot": ((0.75, 0.45, 0.3), 0.2, 0),
           "rug": ((0.5, 0.2, 0.2), 0.1, 0), "cabinet": ((0.16, 0.17, 0.19), 0.3, 0), "marble": ((0.92, 0.9, 0.86), 0.7, 0), "lemon": ((0.95, 0.8, 0.2), 0.3, 0),
           "bottle": ((0.2, 0.35, 0.2), 0.9, 0), "leather_seat": ((0.3, 0.2, 0.15), 0.3, 0), "pillow3": ((0.85, 0.85, 0.82), 0.1, 0), "felt": ((0.1, 0.45, 0.25), 0.05, 0), "rail": ((0.3, 0.18, 0.1), 0.3, 0),
           "ball_yellow": ((0.95, 0.8, 0.1), 0.9, 0), "ball_blue": ((0.1, 0.3, 0.8), 0.9, 0), "ball_red": ((0.8, 0.1, 0.1), 0.9, 0), "ball_purple": ((0.4, 0.1, 0.5), 0.9, 0), "ball_orange": ((0.95, 0.5, 0.1), 0.9, 0),
           "ball_green": ((0.1, 0.5, 0.2), 0.9, 0), "ball_maroon": ((0.5, 0.1, 0.15), 0.9, 0), "ball_white": ((0.95, 0.95, 0.9), 0.9, 0), "piano": ((0.05, 0.05, 0.06), 1.0, 0), "ivory": ((0.95, 0.93, 0.85), 0.4, 0),
           "stone": ((0.5, 0.48, 0.45), 0.1, 0), "firebox": ((0.08, 0.06, 0.05), 0.0, 0), "flame": ((1.0, 0.5, 0.1), 0, 1), "ceramic": ((0.3, 0.5, 0.6), 0.5, 0), "frame": ((0.2, 0.15, 0.1), 0.3, 0),
           "photo": ((0.6, 0.6, 0.65), 0.2, 0), "gold": ((0.9, 0.75, 0.3), 1.0, 0), "crystal": ((0.9, 0.95, 1.0), 1.2, 0), "book_a": ((0.6, 0.2, 0.2), 0.1, 0), "book_b": ((0.2, 0.3, 0.6), 0.1, 0),
           "book_c": ((0.85, 0.8, 0.65), 0.1, 0), "book_d": ((0.2, 0.45, 0.3), 0.1, 0), "canvas": ((0.6, 0.5, 0.7), 0.1, 0), "rattan": ((0.7, 0.55, 0.35), 0.2, 0), "trunk": ((0.45, 0.35, 0.25), 0.1, 0),
           "bamboo": ((0.8, 0.7, 0.4), 0.2, 0), "thatch": ((0.75, 0.65, 0.35), 0.0, 0), "pineapple": ((0.9, 0.7, 0.2), 0.3, 0), "rail_pad": ((0.15, 0.1, 0.08), 0.4, 0), "chip_red": ((0.8, 0.1, 0.1), 0.4, 0),
           "chip_blue": ((0.1, 0.2, 0.7), 0.4, 0), "chip_black": ((0.08, 0.08, 0.08), 0.4, 0), "card": ((0.95, 0.95, 0.95), 0.2, 0), "safe_steel": ((0.25, 0.26, 0.3), 0.8, 0), "ember": ((1.0, 0.3, 0.05), 0, 1), "safe_door": ((0.2, 0.21, 0.24), 0.8, 0)}

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
        for extra in ("cue", "keys"):
            if hasattr(m, extra): meta[n][extra] = [round(float(q), 4) for q in getattr(m, extra)]
        if hasattr(m, "blocks"): meta[n]["blocks"] = [[round(float(q), 4) for q in b] for b in m.blocks]   # floor footprints (x0, z0, x1, z1), else the bounds
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
