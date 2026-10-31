# The Dream's rides, built as lofts: a lower body, a glass greenhouse with a painted roof and pillars,
# lights, trim and four separate wheels (so the game can spin them). x = forward, y = up, z = right. Metres.
import numpy as np, math, sys, os, json
from mesh import Mesh, superellipse, loft, lathe, box, curve, render

def orient(V, F, centre_of):
    """flip faces so their normals point away from the shape's local centre"""
    V = np.array(V, float); out = []
    for f in F:
        P = V[list(f)]; n = np.zeros(3)
        for k in range(1, len(f) - 1): n += np.cross(P[k] - P[0], P[k + 1] - P[0])
        c = P.mean(0)
        out.append(f if np.dot(n, c - centre_of(c)) >= 0 else f[::-1])
    return out

def body_loft(m, part, xs, w, yb, yt, p, mat_of, n=48, zc=0.0):
    rings = []; ring0 = superellipse(n, p)
    for x in xs:
        W_, b, t = w(x), yb(x), yt(x); cy = (b + t) / 2; h = max(0.005, (t - b) / 2)
        rings.append([(x, cy + h * yy, zc + W_ * zz) for (zz, yy) in ring0])
    V, F = loft(rings)
    cen = lambda c: np.array([c[0], (yb(c[0]) + yt(c[0])) / 2, zc])
    F = orient(V, F, cen)
    by = {}
    for f in F:
        c = np.mean([V[i] for i in f], axis=0); by.setdefault(mat_of(c), []).append(f)
    for mat, fs in by.items(): m.add(part, V, [], mat); m.parts[part]["f"].setdefault(mat, [])
    # add once with all faces split by material (shared vertices for smooth normals)
    p_ = m.part(part); o = len(p_["v"]) - len(V) * len(by)
    # rebuild cleanly: drop the duplicate vertex blocks we just appended
    p_["v"] = p_["v"][:len(p_["v"]) - len(V) * len(by)]
    o = len(p_["v"]); p_["v"].extend([tuple(map(float, v)) for v in V])
    for mat, fs in by.items(): p_["f"].setdefault(mat, []).extend([tuple(i + o for i in f) for f in fs])

def add_box(m, part, c, s, mat):
    V, F = box(c, s); F = orient(V, F, lambda q: np.array(c)); m.add(part, V, F, mat, smooth=False)

def wheel(m, name, cx, cy, cz, r, w, side, spokes=5, style="alloy"):
    # tyre: rounded profile revolved around the z axis
    prof = [(r * 0.62, -w / 2), (r * 0.9, -w / 2), (r * 0.98, -w * 0.42), (r, -w * 0.2), (r, w * 0.2), (r * 0.98, w * 0.42), (r * 0.9, w / 2), (r * 0.62, w / 2)]
    V, F = lathe(prof, 40, (cx, cy, cz), cap=False)
    F = orient(V, F, lambda c: np.array([cx, cy, c[2]]))
    m.add(name, V, F, "tire")
    # rim: a dish on the outer face, spokes and a hub
    face = cz + side * w * 0.42
    V, F = lathe([(0.0, face - side * 0.01), (r * 0.6, face - side * 0.01), (r * 0.6, face - side * 0.06), (0.0, face - side * 0.06)], 36, (cx, cy, 0))
    F = orient(V, F, lambda c: np.array([cx, cy, face - side * 0.035]))
    m.add(name, V, F, "rim_dark")
    for k in range(spokes):
        a = 2 * math.pi * k / spokes
        ln = r * 0.56; sw = r * (0.13 if style == "alloy" else 0.2)
        dx, dy = math.cos(a), math.sin(a); px, py = -dy, dx
        q = [(cx + px * sw / 2, cy + py * sw / 2), (cx + dx * ln + px * sw * 0.35, cy + dy * ln + py * sw * 0.35),
             (cx + dx * ln - px * sw * 0.35, cy + dy * ln - py * sw * 0.35), (cx - px * sw / 2, cy - py * sw / 2)]
        V = [(x, y, face) for x, y in q] + [(x, y, face - side * 0.03) for x, y in q]
        F = [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
        F = orient(V, F, lambda c: np.array([cx, cy, face - side * 0.015]))
        m.add(name, V, F, "rim", smooth=False)
    V, F = lathe([(0.0, face + side * 0.012), (r * 0.12, face + side * 0.012), (r * 0.12, face - side * 0.02), (0.0, face - side * 0.02)], 16, (cx, cy, 0))
    F = orient(V, F, lambda c: np.array([cx, cy, face]))
    m.add(name, V, F, "rim")
    # the rim lip
    V, F = lathe([(r * 0.6, face), (r * 0.66, face), (r * 0.66, face - side * 0.05), (r * 0.6, face - side * 0.05)], 36, (cx, cy, 0), cap=False)
    F = orient(V, F, lambda c: np.array([cx, cy, c[2]]))
    m.add(name, V, F, "rim")

def car(spec):
    """spec: length L, half width hw, wheel radius r, wheel x positions, profile control points"""
    m = Mesh(); L = spec["L"]; hw = spec["hw"]; r = spec["r"]; wx = spec["wheels"]; gy = r  # wheel centre height
    arch_r = r * 1.12
    def yb(x):
        y = spec.get("ride", 0.18)
        for w_ in wx:
            d = x - w_
            if abs(d) < arch_r: y = max(y, gy + math.sqrt(arch_r * arch_r - d * d) * 0.92 - 0.02)
        return y
    yt = lambda x: curve(spec["belt"], x)
    W = lambda x: curve(spec["plan"], x)
    xs = np.concatenate([np.linspace(0, 0.25, 8), np.linspace(0.25, L - 0.25, 90), np.linspace(L - 0.25, L, 8)])
    def body_mat(c):
        x, y, z = c
        if y < yb(x) + 0.07 and not any(abs(x - w_) < arch_r * 1.02 for w_ in wx): return "trim"   # rocker / lower valance
        return "paint"
    body_loft(m, "body", xs, W, yb, yt, spec.get("p", 3.2), body_mat)
    # greenhouse
    g0, g1 = spec["glass"]
    gx = np.linspace(g0, g1, 60)
    roof = lambda x: curve(spec["roof"], x)
    belt = lambda x: yt(x) - 0.04
    GW = lambda x: W(x) * spec.get("tumble", 0.8)
    pillars = spec.get("pillars", [])
    def glass_mat(c):
        x, y, z = c
        if y > roof(x) - spec.get("roofcap", 0.05): return "paint"
        if any(abs(x - px) < pw for px, pw in pillars): return "pillar"
        return "glass"
    body_loft(m, "body", gx, GW, belt, roof, spec.get("gp", 2.6), glass_mat, n=40)
    # mirrors, on the door just below the glass
    mx = spec.get("mirror", g1 - 0.25)
    for s in (-1, 1): add_box(m, "body", (mx, yt(mx) + 0.06, s * (GW(mx) + 0.1)), (0.16, 0.1, 0.16), "paint")
    # lights: lens units set into the nose and tail, following the body's plan shape
    def lamp(x0, x1, yc, h, zc, zw, mat, n=16):
        rings = []
        for x in np.linspace(x0, x1, 6):
            rings.append([(x, yc + h / 2 * yy, zc + zw / 2 * zz) for (zz, yy) in superellipse(n, 4)])
        V, F = loft(rings); F = orient(V, F, lambda c: np.array([c[0], yc, zc])); m.add("body", V, F, mat)
    hl = spec.get("headlight", (0.13, 0.36, 0.22))   # height below the belt, width, height
    tl = spec.get("taillight", (0.12, 0.34, 0.12))
    for s in (-1, 1):
        zc = s * (W(L - 0.1) - hl[1] / 2 - 0.04)
        lamp(L - 0.22, L + 0.005, yt(L - 0.1) - hl[0], hl[2], zc, hl[1], "light_f")
        zc = s * (W(0.1) - tl[1] / 2 - 0.03)
        lamp(-0.005, 0.2, yt(0.1) - tl[0], tl[2], zc, tl[1], "light_r")
    # grille and plate
    gw_ = spec.get("grille", 0.6)
    if gw_ > 0: add_box(m, "body", (L - 0.03, yt(L - 0.1) - hl[0] - 0.1, 0), (0.08, 0.16, gw_), "grille")
    # wheels: four separate objects
    for i, w_ in enumerate(wx):
        for s, side in ((-1, "l"), (1, "r")):
            wheel(m, f"wheel_{i}{side}", w_, gy, s * (hw - spec.get("inset", 0.12)), r, spec.get("tw", 0.24), s, spec.get("spokes", 5))
    return m

RIDES = {
    # small hatchback: short overhangs, tall cabin, steep tail
    "hatch": dict(L=3.9, hw=0.86, r=0.32, wheels=[0.62, 3.12], ride=0.2, p=4.4,
                  belt=[(0, 0.62), (0.15, 0.86), (0.6, 0.92), (2.9, 0.98), (3.5, 0.9), (3.8, 0.78), (3.9, 0.62)],
                  plan=[(0, 0.7), (0.2, 0.84), (3.6, 0.86), (3.9, 0.74)],
                  glass=(0.18, 2.95), roof=[(0.18, 0.98), (0.3, 1.42), (1.6, 1.5), (2.4, 1.45), (2.95, 1.0)],
                  pillars=[(0.25, 0.08), (1.62, 0.05)], tumble=0.84, mirror=2.6),
    # sports coupe: long hood, low fastback roof
    "coupe": dict(L=4.55, hw=0.93, r=0.35, wheels=[0.78, 3.6], ride=0.15, p=4.6,
                  belt=[(0, 0.66), (0.15, 0.86), (0.9, 0.9), (2.4, 0.86), (3.4, 0.8), (4.3, 0.72), (4.55, 0.6)],
                  plan=[(0, 0.82), (0.35, 0.93), (4.0, 0.92), (4.55, 0.74)],
                  glass=(0.45, 2.75), roof=[(0.45, 0.88), (0.9, 1.12), (1.55, 1.24), (2.15, 1.2), (2.75, 0.84)],
                  pillars=[(1.75, 0.045)], tumble=0.74, gp=3.0, mirror=2.55, spokes=10, headlight=(0.1, 0.4, 0.13), taillight=(0.1, 0.5, 0.09)),
    # executive sedan: long, upright, chrome-y
    "sedan": dict(L=5.1, hw=0.95, r=0.36, wheels=[1.0, 4.0], ride=0.2, p=4.4, headlight=(0.12, 0.38, 0.14), taillight=(0.1, 0.42, 0.12), grille=0.7,
                  belt=[(0, 0.66), (0.2, 0.95), (1.2, 1.0), (3.9, 0.98), (4.7, 0.88), (5.1, 0.66)],
                  plan=[(0, 0.82), (0.4, 0.95), (4.6, 0.95), (5.1, 0.82)],
                  glass=(0.85, 3.85), roof=[(0.85, 1.0), (1.45, 1.44), (2.4, 1.5), (3.3, 1.44), (3.85, 1.0)],
                  pillars=[(2.45, 0.06)], tumble=0.82, mirror=3.6, spokes=6),
    # supercar: wedge nose, cab-forward, wide
    "super": dict(L=4.6, hw=1.0, r=0.35, wheels=[0.95, 3.75], ride=0.12, p=5.0, headlight=(0.06, 0.34, 0.07), taillight=(0.12, 0.8, 0.06), grille=0,
                  belt=[(0, 0.7), (0.3, 0.86), (1.4, 0.92), (2.6, 0.82), (4.0, 0.62), (4.6, 0.42)],
                  plan=[(0, 0.95), (0.6, 1.0), (3.4, 1.0), (4.6, 0.8)],
                  glass=(1.2, 3.3), roof=[(1.2, 0.86), (1.7, 1.08), (2.3, 1.14), (2.9, 1.04), (3.3, 0.8)],
                  pillars=[], tumble=0.62, gp=2.4, mirror=3.05, spokes=10),
    # city bus: a box with a long window band
    "bus": dict(L=10.5, hw=1.25, r=0.5, wheels=[2.1, 8.6], ride=0.3, p=7.0,
                belt=[(0, 1.6), (0.2, 1.62), (10.2, 1.62), (10.5, 1.55)],
                plan=[(0, 1.18), (0.2, 1.25), (10.3, 1.25), (10.5, 1.18)],
                glass=(0.15, 10.35), roof=[(0.15, 2.9), (0.4, 3.05), (10.1, 3.05), (10.35, 2.9)],
                pillars=[(x, 0.06) for x in np.arange(1.3, 10.0, 1.25)], tumble=0.99, gp=7.0, roofcap=0.32, mirror=10.0, spokes=8, inset=0.2, tw=0.3,
                headlight=(0.9, 0.3, 0.16), taillight=(0.5, 0.24, 0.3), grille=1.2),
}
COLORS = {"hatch": "#8a8f98", "coupe": "#c0222b", "sedan": "#141b33", "super": "#f08a1c", "bus": "#2f6fb0"}

def hexrgb(h): h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))
def mats_for(id_):
    paint = hexrgb(COLORS[id_])
    return {"paint": (paint, 1.0, 0), "glass": ((0.05, 0.06, 0.1), 1.2, 0), "pillar": ((0.03, 0.03, 0.04), 0.4, 0), "trim": ((0.06, 0.06, 0.07), 0.3, 0),
            "grille": ((0.03, 0.03, 0.03), 0.2, 0), "light_f": ((1.0, 0.96, 0.85), 0, 1), "light_r": ((1.0, 0.1, 0.18), 0, 1),
            "tire": ((0.05, 0.05, 0.055), 0.15, 0), "rim": ((0.75, 0.76, 0.8), 1.4, 0), "rim_dark": ((0.12, 0.12, 0.14), 0.5, 0)}

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    ids = sys.argv[2].split(",") if len(sys.argv) > 2 else list(RIDES)
    from PIL import Image
    for id_ in ids:
        m = car(RIDES[id_]); L = RIDES[id_]["L"]
        m.save_obj(os.path.join(out, f"{id_}.obj"))
        sc = L / 4.5
        img = render(m, mats_for(id_), cam=(L * 0.5 + 3.2 * sc, 1.5 * sc, 7.2 * sc), look=(L * 0.5, 0.65 * sc, 0))
        Image.fromarray(img).save(os.path.join(out, f"{id_}_prev.png"))
        print(id_, sum(len(p["v"]) for p in m.parts.values()), "verts")

def jet():
    """a mid-size business jet, nose at +x"""
    m = Mesh(); L = 15.0
    prof = lambda x: curve([(0, 0.05), (0.6, 0.55), (2.5, 0.95), (9.5, 0.98), (12.8, 0.6), (15.0, 0.12)], x)
    yc = lambda x: curve([(0, 2.45), (2.0, 2.2), (9.5, 2.0), (13.5, 2.05), (15.0, 2.0)], x)
    rings = []
    for x in np.linspace(0, L, 70):
        r = prof(L - x)
        rings.append([(x, yc(x) + r * yy * 1.05, r * zz) for (zz, yy) in superellipse(36, 2.2)])
    V, F = loft(rings); F = orient(V, F, lambda c: np.array([c[0], yc(c[0]), 0]))
    def fus_mat(c):
        x, y, z = c
        if x > 12.4 and y > yc(x) + 0.15 and abs(z) < 0.75: return "glass"
        if 4.0 < x < 11.5 and abs(y - (yc(x) + 0.25)) < 0.12 and int((x - 4.0) / 0.62) % 2 == 0: return "glass"   # cabin windows
        if abs(y - (yc(x) - 0.15)) < 0.05: return "stripe"
        return "paint"
    by = {}
    for f in F: by.setdefault(fus_mat(np.mean([V[i] for i in f], 0)), []).append(f)
    p_ = m.part("body"); o = len(p_["v"]); p_["v"].extend([tuple(map(float, v)) for v in V])
    for mat, fs in by.items(): p_["f"].setdefault(mat, []).extend([tuple(i + o for i in f) for f in fs])
    def plate(name, root_x, root_c, tip_x, tip_c, y0, y1, z0, z1, th, mat, vertical=False):
        # tapered wing/fin as a thin lofted aerofoil from root to tip
        rings = []
        for t in np.linspace(0, 1, 8):
            x0 = root_x + (tip_x - root_x) * t; ch = root_c + (tip_c - root_c) * t
            y = y0 + (y1 - y0) * t; z = z0 + (z1 - z0) * t; T = th * (1 - 0.6 * t)
            ring = []
            for (zz, yy) in superellipse(16, 2.0):
                if vertical: ring.append((x0 + ch * (yy * 0.5 + 0.5) * -1 + ch, y, z + zz * T))
                else: ring.append((x0 + ch * (yy * 0.5 + 0.5) * -1 + ch, y + zz * T, z))
            rings.append(ring)
        V, F = loft(rings)
        ctr = np.mean(V, 0); F = orient(V, F, lambda c: np.array([c[0], (y0 + y1) / 2 if not vertical else c[1], c[2] if vertical else (z0 + z1) / 2]) if False else ctr)
        m.add("body", V, F, mat)
    for s in (-1, 1):
        plate("wing", 6.0, 3.4, 4.2, 1.2, 1.55, 1.85, s * 0.8, s * 7.2, 0.16, "paint")
        plate("htail", 1.3, 1.6, 0.6, 0.8, 4.35, 4.45, s * 0.2, s * 2.6, 0.08, "paint")
        # engines on the rear fuselage
        V, F = lathe([(0.0, -1.4), (0.42, -1.3), (0.46, -0.4), (0.42, 1.2), (0.3, 1.4), (0.0, 1.4)], 20, (3.3, 2.55, s * 1.35), axis="x")
        F = orient(V, F, lambda c: np.array([c[0], 2.55, s * 1.35])); m.add("body", V, F, "engine")
    plate("fin", 2.2, 2.6, 0.8, 1.6, 2.8, 4.4, 0, 0, 0.12, "paint", vertical=True)
    return m
