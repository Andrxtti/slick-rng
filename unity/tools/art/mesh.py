# Tiny mesh toolkit: lofts, lathes, boxes, OBJ export and a numpy preview renderer (z-buffer, Blinn-Phong).
import numpy as np, math

class Mesh:
    def __init__(self): self.parts = {}   # object name -> {"v": [], "f": {mat: []}, "smooth": bool}
    def part(self, name, smooth=True):
        if name not in self.parts: self.parts[name] = {"v": [], "f": {}, "smooth": smooth}
        return self.parts[name]
    def add(self, name, verts, faces, mat, smooth=True):
        p = self.part(name, smooth); o = len(p["v"]); p["v"].extend([tuple(map(float, v)) for v in verts])
        p["f"].setdefault(mat, []).extend([tuple(i + o for i in f) for f in faces])
    def merge(self, other, prefix=""):
        for n, p in other.parts.items():
            for m, fs in p["f"].items():
                self.add(prefix + n, p["v"], [], m, p["smooth"])
            q = self.parts[prefix + n]; base = len(q["v"]) - len(p["v"])
            for m, fs in p["f"].items(): q["f"].setdefault(m, []).extend([tuple(i + base for i in f) for f in fs])
    def save_obj(self, path, mtl_names=True):
        out = ["# The Dream ride model"]; vo = 1
        for n, p in self.parts.items():
            out.append(f"o {n}")
            V = np.array(p["v"], np.float64)
            for v in V: out.append(f"v {-v[0]:.5f} {v[1]:.5f} {v[2]:.5f}")   # Unity flips x on OBJ import; pre-flip so +x stays +x
            if p["smooth"]:
                N = vertex_normals(V, [f for fs in p["f"].values() for f in fs])
                for nn in N: out.append(f"vn {-nn[0]:.4f} {nn[1]:.4f} {nn[2]:.4f}")
            for m, fs in p["f"].items():
                out.append(f"usemtl {m}")
                for f in fs:
                    f2 = f[::-1]   # winding flips with the mirrored x
                    if p["smooth"]: out.append("f " + " ".join(f"{i + vo}//{i + vo}" for i in f2))
                    else: out.append("f " + " ".join(str(i + vo) for i in f2))
            vo += len(V)
        open(path, "w").write("\n".join(out) + "\n")

def vertex_normals(V, F):
    N = np.zeros_like(V)
    for f in F:
        for k in range(1, len(f) - 1):
            a, b, c = V[f[0]], V[f[k]], V[f[k + 1]]
            n = np.cross(b - a, c - a); N[f[0]] += n; N[f[k]] += n; N[f[k + 1]] += n
    l = np.linalg.norm(N, axis=1, keepdims=True); return N / np.maximum(l, 1e-9)

def superellipse(n, p):
    """unit ring in (z, y), n points, exponent p (2 = ellipse, higher = boxier). Starts at the bottom centre."""
    pts = []
    for i in range(n):
        a = -math.pi / 2 + 2 * math.pi * i / n
        c, s = math.cos(a), math.sin(a)
        pts.append((math.copysign(abs(c) ** (2 / p), c), math.copysign(abs(s) ** (2 / p), s)))
    return pts

def loft(rings, cap=True):
    """rings: list of lists of 3d points (same count). Returns verts, quads (+ caps)."""
    n = len(rings[0]); V = [p for r in rings for p in r]; F = []
    for i in range(len(rings) - 1):
        for j in range(n):
            a = i * n + j; b = i * n + (j + 1) % n; c = (i + 1) * n + (j + 1) % n; d = (i + 1) * n + j
            F.append((a, d, c, b))
    if cap:
        c0 = np.mean(rings[0], axis=0); c1 = np.mean(rings[-1], axis=0)
        i0 = len(V); V.append(tuple(c0)); i1 = len(V); V.append(tuple(c1))
        last = (len(rings) - 1) * n
        for j in range(n):
            F.append((i0, j, (j + 1) % n)); F.append((i1, last + (j + 1) % n, last + j))
    return V, F

def lathe(profile, n, axis_center, axis="z", cap=True):
    """profile: list of (r, t) along the axis; revolve around an axis through axis_center."""
    rings = []
    for (r, t) in profile:
        ring = []
        for i in range(n):
            a = 2 * math.pi * i / n
            if axis == "z": ring.append((axis_center[0] + r * math.cos(a), axis_center[1] + r * math.sin(a), axis_center[2] + t))
            else: ring.append((axis_center[0] + t, axis_center[1] + r * math.cos(a), axis_center[2] + r * math.sin(a)))
        rings.append(ring)
    return loft(rings, cap=cap)

def box(c, s, r=0.0):
    x, y, z = c; a, b, d = s[0] / 2, s[1] / 2, s[2] / 2
    V = [(x - a, y - b, z - d), (x + a, y - b, z - d), (x + a, y + b, z - d), (x - a, y + b, z - d),
         (x - a, y - b, z + d), (x + a, y - b, z + d), (x + a, y + b, z + d), (x - a, y + b, z + d)]
    F = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (2, 3, 7, 6), (1, 2, 6, 5), (0, 4, 7, 3)]
    return V, F

def curve(ctrl, x):
    """smooth interpolation through control points [(x, v)] (monotone cubic-ish via Catmull-Rom on x)"""
    xs = [c[0] for c in ctrl]; vs = [c[1] for c in ctrl]
    if x <= xs[0]: return vs[0]
    if x >= xs[-1]: return vs[-1]
    i = max(k for k in range(len(xs) - 1) if xs[k] <= x)
    t = (x - xs[i]) / (xs[i + 1] - xs[i])
    p0 = vs[max(0, i - 1)]; p1 = vs[i]; p2 = vs[i + 1]; p3 = vs[min(len(vs) - 1, i + 2)]
    t2, t3 = t * t, t * t * t
    return 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)

# ------------------------------------------------------------------ preview renderer
def render(mesh, mats, size=(900, 420), cam=(6.5, 1.6, 7.5), look=(2.2, 0.6, 0.0), fov=26, bg=(0.18, 0.05, 0.25), lights=None):
    Wd, Ht = size
    cam = np.array(cam, float); look = np.array(look, float)
    fwd = look - cam; fwd /= np.linalg.norm(fwd); right = np.cross(fwd, [0, 1, 0]); right /= np.linalg.norm(right); up = np.cross(right, fwd)
    f = 1 / math.tan(math.radians(fov) / 2)
    img = np.zeros((Ht, Wd, 3)); img[:] = bg; zb = np.full((Ht, Wd), np.inf)
    lights = lights or [((-0.5, 0.6, 0.6), (1.0, 0.55, 0.75)), ((0.7, 0.4, 0.5), (0.4, 0.8, 1.0)), ((0.2, 1.0, -0.3), (1.0, 0.9, 0.8))]
    for n_, p in mesh.parts.items():
        V = np.array(p["v"]); N = vertex_normals(V, [f_ for fs in p["f"].values() for f_ in fs])
        rel = V - cam; X = rel @ right; Y = rel @ up; Z = rel @ fwd
        sx = (X / Z * f * Ht / 2 + Wd / 2); sy = (-Y / Z * f * Ht / 2 + Ht / 2)
        for m, fs in p["f"].items():
            col, spec, emi = mats.get(m, ((0.7, 0.7, 0.7), 0.3, 0))
            col = np.array(col)
            for face in fs:
                for k in range(1, len(face) - 1):
                    tri = (face[0], face[k], face[k + 1])
                    xs, ys, zs = sx[list(tri)], sy[list(tri)], Z[list(tri)]
                    if min(zs) <= 0.05: continue
                    x0, x1 = int(max(0, math.floor(min(xs)))), int(min(Wd - 1, math.ceil(max(xs))))
                    y0, y1 = int(max(0, math.floor(min(ys)))), int(min(Ht - 1, math.ceil(max(ys))))
                    if x1 < x0 or y1 < y0: continue
                    gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
                    (ax, ay), (bx, by), (cx, cy) = (xs[0], ys[0]), (xs[1], ys[1]), (xs[2], ys[2])
                    den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
                    if abs(den) < 1e-9: continue
                    w0 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den
                    w1 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den
                    w2 = 1 - w0 - w1
                    inside = (w0 >= -1e-4) & (w1 >= -1e-4) & (w2 >= -1e-4)
                    if not inside.any(): continue
                    z = 1 / (w0 / zs[0] + w1 / zs[1] + w2 / zs[2])
                    sub = zb[y0:y1 + 1, x0:x1 + 1]
                    upd = inside & (z < sub)
                    if not upd.any(): continue
                    nn = (w0[..., None] * N[tri[0]] + w1[..., None] * N[tri[1]] + w2[..., None] * N[tri[2]])
                    nn /= np.linalg.norm(nn, axis=-1, keepdims=True) + 1e-9
                    pos = (w0[..., None] * V[tri[0]] + w1[..., None] * V[tri[1]] + w2[..., None] * V[tri[2]])
                    vdir = cam - pos; vdir /= np.linalg.norm(vdir, axis=-1, keepdims=True)
                    if (nn * vdir).sum(-1).mean() < 0: nn = -nn
                    c = np.zeros(nn.shape[:2] + (3,)) + col * 0.12
                    for (ld, lc) in lights:
                        ld = np.array(ld, float); ld /= np.linalg.norm(ld)
                        nd = np.clip((nn * ld).sum(-1), 0, 1)[..., None]
                        h = ld + vdir; h /= np.linalg.norm(h, axis=-1, keepdims=True)
                        sp = np.clip((nn * h).sum(-1), 0, 1)[..., None] ** 40 * spec
                        c += col * nd * np.array(lc) * 0.8 + sp * np.array(lc)
                    # fake environment: sky gradient reflection on glossy paint
                    rz = (nn[..., 1:2] * 0.5 + 0.5)
                    c += spec * 0.25 * (rz * np.array([0.9, 0.3, 0.6]) + (1 - rz) * np.array([0.1, 0.05, 0.15]))
                    if emi: c = col * (1.2 + 0 * c)
                    sub[upd] = z[upd]
                    img[y0:y1 + 1, x0:x1 + 1][upd] = np.clip(c[upd], 0, 1)
    return (img * 255).astype(np.uint8)

def save_bin(mesh, path, pivots=None):
    """DRM1 binary: parts with pivot, positions, normals and per-material index lists (read by RideModel.cs).
    Smooth parts share vertices; flat parts get per-face vertices so their edges stay crisp."""
    import struct
    pivots = pivots or {}
    out = bytearray(b"DRM1"); parts = list(mesh.parts.items()); out += struct.pack("<i", len(parts))
    def s(t): b = t.encode(); return struct.pack("<i", len(b)) + b
    for name, p in parts:
        V = np.array(p["v"], np.float64); allf = [f for fs in p["f"].values() for f in fs]
        piv = np.array(pivots.get(name, (0, 0, 0)), np.float64)
        if p["smooth"]:
            N = vertex_normals(V, allf); P = V - piv
            subs = {m: [i for f in fs for k in range(1, len(f) - 1) for i in (f[0], f[k], f[k + 1])] for m, fs in p["f"].items()}
        else:
            P, N, subs = [], [], {}
            for m, fs in p["f"].items():
                idx = []
                for f in fs:
                    pts = V[list(f)]; n = np.zeros(3)
                    for k in range(1, len(f) - 1): n += np.cross(pts[k] - pts[0], pts[k + 1] - pts[0])
                    n /= max(1e-9, np.linalg.norm(n)); b = len(P)
                    for q in pts: P.append(q - piv); N.append(n)
                    for k in range(1, len(f) - 1): idx += [b, b + k, b + k + 1]
                subs[m] = idx
            P, N = np.array(P), np.array(N)
        out += s(name) + struct.pack("<3f", *piv) + struct.pack("<i", len(P))
        out += np.asarray(P, "<f4").tobytes() + np.asarray(N, "<f4").tobytes()
        out += struct.pack("<i", len(subs))
        for m, idx in subs.items(): out += s(m) + struct.pack("<i", len(idx)) + np.asarray(idx, "<i4").tobytes()
    open(path, "wb").write(bytes(out))
