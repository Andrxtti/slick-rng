# Pets for the apartment: a cat and three dogs, built from lofts, tubes and ellipsoids. Each animal: origin on the
# floor under its middle, nose towards +z, y up, metres. Parts move on their own (the game animates them):
#   body (with neck and collar), head (ears, eyes, nose), tail, tongue (dogs), leg_fl / leg_fr / leg_bl / leg_br,
# each pivoting where it joins the body. Materials are names the game colours per pet (pets.json).
#   python3 pets.py <out dir>        -> <id>.bytes, pets.json, _pets.png (preview sheet)
import numpy as np, math, sys, os, json
from mesh import Mesh, superellipse, loft, save_bin, render, curve
from cars import orient

def add_faces(m, part, V, F, mat, centre_of, smooth=True):
    """add the faces F (indices into V) as `mat`, outward from centre_of(face centroid), keeping only used verts"""
    if not F: return
    V = np.asarray(V, float); F = orient(V, F, centre_of)
    used = sorted({i for f in F for i in f}); remap = {o: n for n, o in enumerate(used)}
    m.add(part, [tuple(V[i]) for i in used], [tuple(remap[i] for i in f) for f in F], mat, smooth)

def add_split(m, part, V, F, centre_of, matfn):
    """faces grouped by matfn(centroid) so one surface can carry several colours (belly, socks, markings)"""
    V = np.asarray(V, float); groups = {}
    for f in F: groups.setdefault(matfn(V[list(f)].mean(0)), []).append(f)
    for mat, fs in groups.items(): add_faces(m, part, V, fs, mat, centre_of)

def ell(m, part, c, r, mat, n=18, nr=12, rot=None, matfn=None):
    """ellipsoid centred c with radii r, optionally turned by the 3x3 matrix rot"""
    c = np.array(c, float); R = np.eye(3) if rot is None else np.array(rot)
    rings = []
    for i in range(nr + 1):
        ph = -math.pi / 2 + math.pi * i / nr; z = math.sin(ph); rr = math.cos(ph)
        ring = []
        for k in range(n):
            a = 2 * math.pi * k / n
            p = np.array([r[0] * rr * math.cos(a), r[1] * rr * math.sin(a), r[2] * z])
            ring.append(tuple(c + R @ p))
        rings.append(ring)
    V, F = loft(rings, cap=False)
    if matfn: add_split(m, part, V, F, lambda q: c, matfn)
    else: add_faces(m, part, V, F, mat, lambda q: c)

def frame(T):
    T = T / np.linalg.norm(T); ref = np.array([0, 1, 0]) if abs(T[1]) < 0.9 else np.array([1, 0, 0])
    N = np.cross(T, ref); N /= np.linalg.norm(N); B = np.cross(T, N); return N, B

def tube(m, part, pts, radii, mat, n=14, matfn=None, cap=True, squash=1.0):
    """a tube along the polyline pts (smoothly turned), radius per point; squash < 1 flattens it sideways"""
    P = [np.array(p, float) for p in pts]; rings = []; N = None
    for i, p in enumerate(P):
        T = (P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]); T /= np.linalg.norm(T)
        if N is None: N, B = frame(T)
        else:   # parallel transport: carry the last frame along so the tube doesn't twist at bends
            N = N - T * np.dot(N, T); N /= np.linalg.norm(N); B = np.cross(T, N)
        r = radii[i]
        rings.append([tuple(p + r * (math.cos(2 * math.pi * k / n) * N * squash + math.sin(2 * math.pi * k / n) * B)) for k in range(n)])
    V, F = loft(rings, cap=cap)
    def centre_of(q):
        d = [np.linalg.norm(q - p) for p in P]; return P[int(np.argmin(d))]
    if matfn: add_split(m, part, V, F, centre_of, matfn)
    else: add_faces(m, part, V, F, mat, centre_of)

def smooth_path(ctrl, n=10):
    """Catmull-Rom through control points"""
    C = [np.array(c, float) for c in ctrl]; out = []
    for i in range(len(C) - 1):
        p0, p1, p2, p3 = C[max(i - 1, 0)], C[i], C[i + 1], C[min(i + 2, len(C) - 1)]
        for k in range(n):
            t = k / n; t2, t3 = t * t, t * t * t
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(C[-1]); return out

def wedge(m, part, base, up, out_, w, h, t, mat, inner=None, round_=0.0):
    """a flat ear: triangle with base centre `base`, pointing along `up`, width along `out_`, thickness t.
    round_ > 0 bulges the sides (bat ears)."""
    base = np.array(base, float); up = np.array(up, float); up /= np.linalg.norm(up); out_ = np.array(out_, float); out_ /= np.linalg.norm(out_)
    nrm = np.cross(out_, up); nrm /= np.linalg.norm(nrm)
    outline = []
    for k in range(9):
        s = k / 8; x = (-0.5 + s) * w; y = 0.0
        outline.append((x, y))
    # up the right edge and down the left, bulging for round ears
    for k in range(1, 9):
        s = k / 8; x = 0.5 * w * (1 - s) + round_ * w * math.sin(math.pi * s) * 0.5; y = h * s
        outline.append((x, y))
    for k in range(1, 8):
        s = k / 8; x = -0.5 * w * s - round_ * w * math.sin(math.pi * (1 - s)) * 0.5; y = h * (1 - s)
        outline.append((x, y))
    front = [tuple(base + x * out_ + y * up + nrm * t / 2) for (x, y) in outline]
    back = [tuple(base + x * out_ + y * up - nrm * t / 2) for (x, y) in outline]
    V = front + back; n = len(outline); c = base + up * h * 0.35
    F = [tuple(range(n))] + [tuple(range(2 * n - 1, n - 1, -1))] + [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    add_faces(m, part, V, F, mat, lambda q: c, smooth=False)
    if inner:   # the pink inside, a smaller triangle just in front
        sc = 0.62; ib = base + up * h * 0.12
        pts = [tuple(ib + (x * sc) * out_ + (y * sc) * up + nrm * (t / 2 + 0.002)) for (x, y) in outline]
        add_faces(m, part, pts, [tuple(range(n))], inner, lambda q: ib - nrm, smooth=False)

def ring(m, part, c, axis, R, r, mat, n=20, nt=8):
    """a torus (collar) around `axis` through c"""
    c = np.array(c, float); A = np.array(axis, float); A /= np.linalg.norm(A); N, B = frame(A)
    rings = []
    for i in range(n + 1):
        a = 2 * math.pi * i / n; d = math.cos(a) * N + math.sin(a) * B; ctr = c + d * R
        rings.append([tuple(ctr + r * (math.cos(2 * math.pi * k / nt) * d + math.sin(2 * math.pi * k / nt) * A)) for k in range(nt)])
    V, F = loft(rings, cap=False)
    def centre_of(q):
        v = q - c; v -= A * np.dot(v, A); l = np.linalg.norm(v); return c + (v / l * R if l > 1e-9 else 0)
    add_faces(m, part, V, F, mat, centre_of)

def rot_x(deg):
    a = math.radians(deg); return [[1, 0, 0], [0, math.cos(a), -math.sin(a)], [0, math.sin(a), math.cos(a)]]
def rot_z(deg):
    a = math.radians(deg); return [[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1]]

# ------------------------------------------------------------------ the animals
PETS = {
    # leg: hip/shoulder height; len: hip-to-shoulder; bw/bh: body half width/height; head: skull radii; snout: (radii, offset)
    "cat":      {"name": "Mochi",   "kind": "cat", "leg": 0.20, "len": 0.30, "bw": 0.072, "bh": 0.076, "x": 0.048, "legr": 0.022,
                 "neck": (0.06, 0.07), "head": (0.078, 0.07, 0.072), "snout": ((0.036, 0.026, 0.03), (0, -0.006, 0.07)), "ears": "cat",
                 "tail": [(0, 0.03, -0.02), (0, 0.06, -0.10), (0, 0.17, -0.16), (0, 0.28, -0.14), (0, 0.33, -0.08)], "tailr": (0.021, 0.014),
                 "colors": {"fur": "#d9822b", "fur2": "#f4e9d8", "fur3": "#a4561b", "nose": "#e8879a", "inner": "#f2a7b3", "eye": "#0c0c0e", "iris": "#c6d64a", "hi": "#ffffff"}},
    "pup":      {"name": "Biscuit", "kind": "dog", "leg": 0.33, "len": 0.40, "bw": 0.095, "bh": 0.11, "x": 0.07, "legr": 0.036,
                 "neck": (0.11, 0.09), "head": (0.092, 0.088, 0.098), "snout": ((0.055, 0.05, 0.08), (0, -0.018, 0.10)), "ears": "floppy",
                 "tail": [(0, 0.02, -0.02), (0, 0.08, -0.08), (0, 0.16, -0.09), (0, 0.21, -0.05)], "tailr": (0.024, 0.012), "collar": "#c8323a",
                 "colors": {"fur": "#b8834f", "fur2": "#f2ebe0", "fur3": "#6b4426", "nose": "#151315", "eye": "#0c0b0c", "hi": "#ffffff", "tongue": "#e0707e", "collar": "#c8323a", "tag": "#e3b94f"}},
    "frenchie": {"name": "Tank",    "kind": "dog", "leg": 0.17, "len": 0.25, "bw": 0.11, "bh": 0.10, "x": 0.075, "legr": 0.034,
                 "neck": (0.05, 0.06), "head": (0.10, 0.088, 0.085), "snout": ((0.062, 0.045, 0.036), (0, -0.026, 0.075)), "ears": "bat",
                 "tail": [(0, 0.0, -0.01), (0, 0.02, -0.04)], "tailr": (0.022, 0.016), "collar": "#2f6fd0",
                 "colors": {"fur": "#c9a27a", "fur2": "#efe4d4", "fur3": "#2b2421", "nose": "#141214", "inner": "#d9a0a0", "eye": "#0c0b0c", "hi": "#ffffff", "tongue": "#e0707e", "collar": "#2f6fd0", "tag": "#dfe3ea"}},
    "doberman": {"name": "Duke",    "kind": "dog", "leg": 0.52, "len": 0.52, "bw": 0.098, "bh": 0.13, "x": 0.075, "legr": 0.04,
                 "neck": (0.17, 0.12), "head": (0.074, 0.074, 0.10), "snout": ((0.045, 0.045, 0.115), (0, -0.02, 0.13)), "ears": "pointy",
                 "tail": [(0, 0.02, -0.02), (0, 0.06, -0.09), (0, 0.13, -0.16), (0, 0.21, -0.19)], "tailr": (0.024, 0.012), "collar": "#c9a03e",
                 "colors": {"fur": "#1b191d", "fur2": "#a35e2c", "fur3": "#a35e2c", "nose": "#0e0d0f", "inner": "#3a2a2a", "eye": "#0c0b0c", "hi": "#ffffff", "tongue": "#d86a78", "collar": "#c9a03e", "tag": "#e3b94f"}},
}

def pet(id_):
    S = PETS[id_]; m = Mesh(); piv = {}
    cat = S["kind"] == "cat"
    leg, L, bw, bh, lx = S["leg"], S["len"], S["bw"], S["bh"], S["x"]
    zf, zh = L / 2, -L / 2                       # shoulder and hip along z
    yb = leg + bh * 0.55                          # body centre height
    # ---- body: a loft from rump to chest
    zr, zc = zh - bh * 0.85, zf + bh * 0.75
    W = lambda t: bw * curve([(0, 0.35), (0.07, 0.8), (0.2, 0.97), (0.5, 0.86), (0.78, 1.0), (0.93, 0.86), (1, 0.38)], t)
    Hh = lambda t: bh * curve([(0, 0.35), (0.08, 0.8), (0.22, 0.9), (0.5, 0.8 if not cat else 0.88), (0.78, 1.0), (0.94, 0.86), (1, 0.4)], t)
    Yc = lambda t: yb + bh * curve([(0, 0.05), (0.3, 0.08), (0.5, 0.12 if not cat else 0.02), (0.75, 0.0), (1, 0.06)], t)
    ts = np.linspace(0, 1, 26); ring0 = superellipse(22, 2.25); rings = []
    for t in ts:
        z = zr + (zc - zr) * t; w_, h_, y_ = W(t), Hh(t), Yc(t)
        rings.append([(w_ * xx, y_ + h_ * yy, z) for (xx, yy) in ring0])
    V, F = loft(rings)
    def body_centre(q):
        t = min(1, max(0, (q[2] - zr) / (zc - zr))); return np.array([0, Yc(t), q[2]])
    def body_mat(q):
        t = min(1, max(0, (q[2] - zr) / (zc - zr))); y0 = Yc(t); h_ = Hh(t)
        if id_ == "doberman":
            if q[1] < y0 - 0.3 * h_ and t > 0.8 and abs(q[0]) > 0.025: return "fur2"     # tan chest spots
            return "fur"
        if q[1] < y0 - 0.55 * h_: return "fur2"                                          # belly
        if t > 0.82 and q[1] < y0 + 0.1 * h_ and (id_ != "frenchie" or abs(q[0]) < 0.07): return "fur2"   # chest bib
        if cat and q[1] > y0 + 0.2 * h_ and math.sin(q[2] * 70) > 0.45: return "fur3"  # tabby stripes
        if id_ == "pup" and q[1] > y0 + 0.45 * h_ and 0.25 < t < 0.7: return "fur3"     # a darker saddle
        return "fur"
    add_split(m, "body", V, F, body_centre, body_mat)
    # ---- neck up to the head pivot
    nb = np.array([0, yb + bh * 0.45, zf + bh * 0.25]); hp = nb + np.array([0, S["neck"][0], S["neck"][1]])
    nr = min(bw, bh) * (0.72 if not cat else 0.68)
    npts = smooth_path([tuple(nb - np.array([0, 0.03, 0.03])), tuple(nb), tuple((nb + hp) / 2 + np.array([0, 0, 0.01])), tuple(hp)], 4)
    tube(m, "body", npts, list(np.linspace(nr * 1.15, nr * 0.85, len(npts))), "fur", n=16,
         matfn=(lambda q: "fur2" if (q[2] > hp[2] - 0.02 and q[1] < hp[1] - nr * 0.2 and id_ != "doberman") or (id_ == "doberman" and q[1] < hp[1] - 0.05 and q[2] > nb[2] + 0.05) else "fur"))
    if "collar" in S:
        cpos = nb + (hp - nb) * 0.55; axis = hp - nb
        ring(m, "body", cpos, axis, nr * 0.98, 0.012 if id_ != "frenchie" else 0.011, "collar")
        ell(m, "body", cpos + np.array([0, -nr * 0.98 - 0.012, 0.012]), (0.014, 0.016, 0.004), "tag", n=12, nr=6)
    piv["body"] = (0, yb, 0)
    # ---- head
    hx, hy, hz = S["head"]; sk = hp + np.array([0, hy * 0.35, hz * 0.35])
    def head_mat(q):
        rel = q - sk
        if id_ == "doberman":
            if rel[2] > hz * 0.3 and rel[1] < -hy * 0.05: return "fur2"                  # tan muzzle underside
            if rel[2] > hz * 0.55 and rel[1] > hy * 0.25 and abs(rel[0]) > hx * 0.25: return "fur2"   # brow dots
            return "fur"
        if cat and rel[1] > hy * 0.45 and abs(rel[0]) < hx * 0.45 and math.sin(rel[0] * 140) > 0.2: return "fur3"   # forehead stripes
        if id_ == "pup" and rel[2] > hz * 0.4 and abs(rel[0]) < hx * 0.22: return "fur2"   # white blaze
        return "fur"
    ell(m, "head", sk, (hx, hy, hz), "fur", n=22, nr=14, matfn=head_mat)
    (sx, sy, sz), so = S["snout"]; sn = sk + np.array(so)
    snout_mat = "fur3" if id_ == "frenchie" else "fur2"
    ell(m, "head", sn, (sx, sy, sz), snout_mat, n=18, nr=10, rot=rot_x(-8 if not cat else 0))
    if not cat:   # a jaw under the snout
        ell(m, "head", sn + np.array([0, -sy * 0.55, -sz * 0.15]), (sx * 0.8, sy * 0.5, sz * 0.85), snout_mat, n=14, nr=8)
    tip = sn + np.array([0, sy * (0.45 if not cat else 0.35), sz * 0.95])
    ell(m, "head", tip, (sx * (0.42 if not cat else 0.3), sy * (0.38 if not cat else 0.28), sz * 0.22), "nose", n=12, nr=6)
    # eyes: dark and glossy with a highlight (cats get an iris and a slit)
    for sgn in (-1, 1):
        ex = sgn * hx * (0.5 if not cat else 0.48); ey = hy * (0.32 if not cat else 0.18); ez = hz * (0.8 if not cat else 0.84)
        ec = sk + np.array([ex, ey, ez]); er = hx * (0.17 if not cat else 0.24)
        if cat:
            ell(m, "head", ec, (er, er * 0.9, er * 0.7), "iris", n=12, nr=8)
            ell(m, "head", ec + np.array([0, 0, er * 0.62]), (er * 0.22, er * 0.75, er * 0.12), "eye", n=10, nr=6)
        else:
            ell(m, "head", ec, (er, er, er * 0.75), "eye", n=12, nr=8)
        ell(m, "head", ec + np.array([sgn * er * 0.25 - er * 0.1, er * 0.35, er * 0.62]), (er * 0.22, er * 0.22, er * 0.1), "hi", n=8, nr=5)
    # ears
    for sgn in (-1, 1):
        if S["ears"] == "cat":
            wedge(m, "head", sk + np.array([sgn * hx * 0.55, hy * 0.62, -hz * 0.05]), (sgn * 0.25, 1, 0.05), (1, -sgn * 0.25, 0), 0.055, 0.06, 0.01, "fur", inner="inner")
        elif S["ears"] == "pointy":
            wedge(m, "head", sk + np.array([sgn * hx * 0.52, hy * 0.7, -hz * 0.22]), (sgn * 0.14, 1, -0.08), (1, -sgn * 0.14, 0), 0.068, 0.105, 0.009, "fur", inner="inner", round_=0.12)
        elif S["ears"] == "bat":
            wedge(m, "head", sk + np.array([sgn * hx * 0.58, hy * 0.62, -hz * 0.1]), (sgn * 0.32, 1, 0.0), (1, -sgn * 0.32, 0), 0.07, 0.085, 0.01, "fur", inner="inner", round_=0.35)
        else:   # floppy: hanging flaps beside the head
            ell(m, "head", sk + np.array([sgn * hx * 0.98, -hy * 0.05, -hz * 0.1]), (0.016, 0.07, 0.045), "fur3", n=12, nr=10, rot=rot_z(sgn * 12))
    piv["head"] = tuple(hp)
    # tongue: a little pink tongue, shown when the dog's happy
    if not cat:
        tp = sn + np.array([0, -sy * 0.7, sz * 0.6])
        ell(m, "tongue", tp + np.array([0, -0.02, 0.008]), (sx * 0.38, 0.03, sz * 0.25), "tongue", n=12, nr=8, rot=rot_x(-25))
        piv["tongue"] = tuple(tp)
    # ---- tail
    tb = np.array([0, Yc(0.04) + bh * 0.25, zr + bh * 0.2])
    tp_ = [tuple(tb + np.array(p)) for p in S["tail"]]
    tpts = smooth_path(tp_, 6) if len(tp_) > 2 else tp_
    r0, r1 = S["tailr"]
    def tail_mat(q):
        if cat: return "fur3" if math.sin((q[1] - tb[1]) * 60 + (q[2] - tb[2]) * 60) > 0.35 else "fur"
        if id_ == "pup" and q[1] > tb[1] + 0.17: return "fur2"    # white tip
        return "fur"
    tube(m, "tail", tpts, list(np.linspace(r0, r1, len(tpts))), "fur", n=12, matfn=tail_mat)
    if id_ == "frenchie": ell(m, "tail", tb + np.array([0, 0.012, -0.03]), (0.024, 0.024, 0.024), "fur", n=10, nr=6)
    piv["tail"] = tuple(tb)
    # ---- legs: front ones straight-ish, hind ones with a hock; paws flattened
    lr = S["legr"]
    for side, sgn in (("l", -1), ("r", 1)):
        for end, z0 in (("f", zf), ("b", zh)):
            top = np.array([sgn * lx, leg, z0])
            if end == "f": pts = [top + np.array([0, 0.03, 0]), top, top + np.array([0, -leg * 0.45, -0.01]), top + np.array([0, -leg * 0.82, 0.004]), np.array([sgn * lx, lr * 0.75, z0 + 0.01])]
            else: pts = [top + np.array([0, 0.03, 0]), top, top + np.array([0, -leg * 0.42, 0.045]), top + np.array([0, -leg * 0.74, -0.03]), np.array([sgn * lx, lr * 0.75, z0 - 0.012])]
            rad = [lr * 1.35, lr * 1.3, lr * 1.05, lr * 0.8, lr * 0.78] if end == "b" else [lr * 1.25, lr * 1.15, lr * 0.95, lr * 0.82, lr * 0.8]
            path = smooth_path([tuple(p) for p in pts], 4)
            rr = list(np.interp(np.linspace(0, 1, len(path)), np.linspace(0, 1, len(rad)), rad))
            sock_h = leg * (0.22 if id_ in ("cat", "pup", "frenchie") else 0.45)
            sock = "fur2" if id_ != "frenchie" or end == "f" else "fur"
            tube(m, "leg_" + end + side, path, rr, "fur", n=12, matfn=lambda q, sh=sock_h, sk_=sock: sk_ if q[1] < sh else "fur")
            ell(m, "leg_" + end + side, (sgn * lx, lr * 0.62, z0 + (0.02 if end == "f" else 0.0)), (lr * 1.08, lr * 0.62, lr * 1.35), sock if id_ != "doberman" else "fur2", n=12, nr=8)
            piv["leg_" + end + side] = tuple(top)
    info = {"name": S["name"], "kind": S["kind"], "leg": leg, "len": L, "height": float(sk[1] + hy + (0.1 if S["ears"] in ("pointy", "bat") else 0.04)),
            "bed": S["kind"] == "cat", "colors": S["colors"]}
    return m, piv, info

PREVIEW_BASE = {"collar": ((0.8, 0.2, 0.25), 0.4, 0), "tag": ((0.9, 0.75, 0.3), 1.0, 0), "tongue": ((0.9, 0.45, 0.5), 0.4, 0), "hi": ((1, 1, 1), 0, 1),
                "eye": ((0.05, 0.05, 0.06), 1.0, 0), "nose": ((0.08, 0.07, 0.08), 0.8, 0)}
def hexrgb(h): h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "out/Pets"; os.makedirs(out, exist_ok=True)
    from PIL import Image, ImageDraw
    meta = {}; tiles = []
    for id_ in PETS:
        m, piv, info = pet(id_)
        save_bin(m, os.path.join(out, id_ + ".bytes"), piv)
        meta[id_] = info
        mats = dict(PREVIEW_BASE); mats.update({k: (hexrgb(v), 0.15 if k.startswith("fur") else 0.5, 0) for k, v in info["colors"].items() if k not in ("hi",)})
        V = np.vstack([np.array(p["v"]) for p in m.parts.values()]); c = (V.min(0) + V.max(0)) / 2; size = np.linalg.norm(V.max(0) - V.min(0))
        for k, (cam, look) in enumerate([((c[0] + size * 1.2, c[1] + size * 0.45, c[2] + size * 1.1), c), ((c[0] - size * 1.7, c[1] + size * 0.25, c[2]), c)]):
            img = render(m, mats, size=(360, 300), cam=cam, look=tuple(look), fov=30, bg=(0.16, 0.13, 0.12),
                         lights=[((0.4, 0.8, 0.6), (1, 0.95, 0.9)), ((-0.6, 0.3, 0.4), (0.45, 0.5, 0.6)), ((0.0, 0.2, -1.0), (0.3, 0.3, 0.35))])
            tiles.append((id_ + ("" if k == 0 else " side"), Image.fromarray(img)))
    cols = 4; rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * 360, rows * 300), (20, 18, 16)); d = ImageDraw.Draw(sheet)
    for k, (n, im) in enumerate(tiles):
        sheet.paste(im, ((k % cols) * 360, (k // cols) * 300)); d.text(((k % cols) * 360 + 6, (k // cols) * 300 + 4), n, fill=(230, 220, 200))
    sheet.save(os.path.join(out, "_pets.png"))
    json.dump(meta, open(os.path.join(out, "pets.json"), "w"), indent=1)
    print("ok", {k: (round(v["height"], 3)) for k, v in meta.items()}, [os.path.getsize(os.path.join(out, k + ".bytes")) for k in PETS])
