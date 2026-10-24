# Apartment layouts for each home tier -> Homes/<tier>.json. Room: x across (-w/2..w/2), z from the back wall
# (-d/2) to the open front (+d/2), y up. Props face +z unless rotated (ry, degrees: 90 faces +x, -90 faces -x).
# "use" makes a prop interactive; "spot" is where you stand to use it [x, z, facing].
import json, os
P = json.load(open(os.environ.get("PROPS_JSON", "out/props.json")))
def depth(p, ry=0):  # a prop's back faces the wall it stands against, so its depth is always its own z extent
    mn, mx = P[p]["min"], P[p]["max"]
    return mx[2] - mn[2]

TIERS = {
    #            w    d    h    wall       left wall  floor      trim       view            accent     fabric     blanket
    "studio":   (5.6, 4.8, 2.6, "#5a5262", "#4f4858", "#c9b8a2", "#2a2430", "window_city",  "#e8c46a", "#5b4a6a", "#34507a"),
    "apt":      (6.0, 5.0, 2.7, "#3e4a5c", "#36414f", "#b89a78", "#1f2530", "window_city",  "#e8c46a", "#7a3b5a", "#7a2f3a"),
    "condo":    (7.0, 5.6, 2.8, "#d6d0c6", "#cbc4b8", "#7a5c44", "#3a332c", "window_city",  "#c9a03e", "#2f4f5f", "#e9e3d6"),
    "penthouse":(8.0, 6.2, 3.0, "#1d1c24", "#24222c", "#5a4636", "#0e0d12", "window_city",  "#d9b36c", "#2b2622", "#151318"),
    "mansion":  (9.0, 7.0, 3.3, "#efe7d8", "#e6dcc9", "#a07850", "#6b5236", "window_city",  "#c9a03e", "#6a1f2b", "#efe7d8"),
    "island":   (9.0, 7.0, 3.1, "#f3ead9", "#eadfca", "#c79a6a", "#7a5a3a", "window_ocean", "#3fb6c9", "#f2efe8", "#3fb6c9"),
}

def build(tier):
    w, d, h, wall, wall2, floor, trim, view, accent, fabric, blanket = TIERS[tier]
    big = tier in ("condo", "penthouse", "mansion", "island")
    lux = tier in ("penthouse", "mansion", "island")
    props = []
    def add(p, x, z, ry=0, y=0.0, use=None, label=None, spot=None, c=None, s=1.0):
        e = {"p": p, "x": round(x, 3), "y": round(y, 3), "z": round(z, 3), "ry": ry}
        if s != 1.0: e["s"] = s
        if use: e["use"] = use; e["label"] = label; e["spot"] = [round(v, 3) for v in spot]
        if c: e["c"] = c
        props.append(e)
    back = lambda p, ry=0: -d / 2 + depth(p, ry) / 2 + 0.03
    left = lambda p, ry=90: -w / 2 + depth(p, ry) / 2 + 0.03
    right = lambda p, ry=-90: w / 2 - depth(p, ry) / 2 - 0.03
    X = lambda f: -w / 2 + f * w         # across, as a fraction of the width
    Z = lambda f: -d / 2 + f * d         # deep, as a fraction of the depth

    # sleeping corner, back left
    bedp = "bed_king" if lux else "bed" if tier != "studio" else "bed"
    bw = P[bedp]["max"][0] - P[bedp]["min"][0]
    bx = -w / 2 + 0.25 + bw / 2; bz = back(bedp)
    add(bedp, bx, bz, use="bed", label="Bed", spot=[bx + bw / 2 + 0.45, bz + 0.5, -90], c={"blanket": blanket})
    add("nightstand", bx + bw / 2 + 0.32, back("nightstand"))
    # work corner, back right: desk, laptop, chair, lamp
    dx = w / 2 - 0.25 - 0.65; dz = back("desk")
    add("desk", dx, dz)
    add("laptop", dx - 0.1, dz - 0.02, y=0.75, use="laptop", label="Laptop", spot=[dx - 0.1, dz + 0.95, 180])
    add("chair", dx - 0.1, dz + 0.62, ry=180)
    add("lamp", w / 2 - 0.25, -d / 2 + 0.3)
    # TV wall on the left with something to sit on
    tvp = "crt" if tier == "studio" else "flat_tv_big" if big else "flat_tv"
    tz = Z(0.64 if tier == 'studio' else 0.58)
    add("tv_stand", left("tv_stand"), tz, ry=90)
    add(tvp, -w / 2 + 0.24, tz, ry=90, y=0.44, use="tv", label="TV", spot=[-w / 2 + 1.55, tz + 0.75, -110])
    if tier != "studio":
        cp = "couch_big" if big else "couch"
        add(cp, -w / 2 + 2.6 + (0.4 if big else 0), tz, ry=-90, c={"fabric": fabric})
    ctx = -w / 2 + (1.55 if tier != "studio" else 1.35); ctz = tz - (0.0 if tier != "studio" else 0.05)
    add("coffee_table", ctx, ctz, ry=90)
    add("bong", ctx, ctz - 0.22, y=0.42, use="bong", label="Bong", spot=[ctx + (0.7 if tier == "studio" else 0.0), ctz - (0.0 if tier == "studio" else 0.95), -90 if tier == "studio" else 0])
    # drinks: mini fridge (studio) or a full fridge near the front left
    fp = "mini_fridge" if tier == "studio" else "fridge"
    fz = d / 2 - 0.55
    add(fp, left(fp), fz, ry=90)
    fh = P[fp]["max"][1]
    canx = left(fp) if tier == "studio" else -w / 2 + 0.9
    add("can", canx, fz - (0.0 if tier == "studio" else 0.55), y=fh if tier == "studio" else 0.0, use="beer", label="Beer", spot=[-w / 2 + 1.35, fz - 0.1, -90])
    # clothes on the right wall, fish by the front right
    cl = "rack" if tier == "studio" else "wardrobe"
    cz = Z(0.42)
    add(cl, right(cl), cz, ry=-90, use="closet", label="Closet", spot=[right(cl) - 0.95, cz, 90])
    tk = "fish_tank_big" if big else "fish_tank"
    kz = d / 2 - 0.7
    add(tk, right(tk), kz, ry=-90, use="fish", label="Fish", spot=[right(tk) - 0.85, kz, 90])
    # greenery
    add("plant", X(0.5) - 1.05, -d / 2 + 0.35)
    if big: add("plant", w / 2 - 0.35, Z(0.62)); add("plant", -w / 2 + 0.35, -d / 2 + 0.35 + (2.4 if lux else 2.2))
    rugs = [{"x": round(ctx + 0.4, 3), "z": round(ctz, 3), "w": 2.6 if big else 2.0, "d": 1.8 if big else 1.4, "c": accent}]
    windows = [{"x": round(X(0.53), 3), "y": round(h * 0.56, 3), "w": round(min(w * 0.36, 3.6), 3), "h": round(h * (0.62 if lux else 0.45), 3)}]
    if lux: windows.append({"x": round(X(0.53) - w * 0.4, 3), "y": windows[0]["y"], "w": windows[0]["w"] * 0.5, "h": windows[0]["h"], "wall": "left", "z": round(Z(0.3), 3)})
    lights = [{"x": w / 2 - 0.25, "y": 1.55, "z": -d / 2 + 0.3, "c": "#ffcf9a", "i": 1.4, "r": 4.5},
              {"x": windows[0]["x"], "y": windows[0]["y"], "z": -d / 2 + 0.6, "c": "#ff6aa8" if view == "window_city" else "#ffb36b", "i": 1.0, "r": 4.0}]
    return {"id": tier, "w": w, "d": d, "h": h, "wall": wall, "wall2": wall2, "floor": floor, "trim": trim, "view": view, "accent": accent,
            "windows": windows, "rugs": rugs, "lights": lights, "props": props, "spawn": [round(X(0.55), 3), round(Z(0.62), 3), 20]}


# ------------------------------------------------------------------ the nicer places: their own plans
def plan(tier):
    """shared bits for the big places: colours, helpers and the list the layout fills"""
    w, d, h, wall, wall2, floor, trim, view, accent, fabric, blanket = TIERS[tier]
    props, lights, windows, rugs, pictures, pools = [], [], [], [], [], []
    def add(p, x, z, ry=0, y=0.0, use=None, label=None, spot=None, c=None, s=1.0, tex=None):
        e = {"p": p, "x": round(x, 3), "y": round(y, 3), "z": round(z, 3), "ry": ry}
        if s != 1.0: e["s"] = s
        if use: e["use"] = use; e["label"] = label; e["spot"] = [round(v, 3) for v in spot]
        if c: e["c"] = c
        if tex: e["tex"] = tex
        props.append(e); return e
    back = lambda p: -d / 2 + depth(p) / 2 + 0.03
    left = lambda p: -w / 2 + depth(p) / 2 + 0.03
    right = lambda p: w / 2 - depth(p) / 2 - 0.03
    def wall_art(tex, wall_, pos, y, wide=False):
        """a picture on the back wall (pos = x) or the left/right wall (pos = z)"""
        p = "art_wide" if wide else "art"
        if wall_ == "back": add(p, pos, -d / 2 + 0.03, 0, y=y, tex=tex)
        elif wall_ == "left": add(p, -w / 2 + 0.03, pos, 90, y=y, tex=tex)
        else: add(p, w / 2 - 0.03, pos, -90, y=y, tex=tex)
    def light(x, y, z, c="#ffcf9a", i=1.2, r=5): lights.append({"x": round(x, 3), "y": y, "z": round(z, 3), "c": c, "i": i, "r": r})
    def window(x, wd, hh, wall_="back", z=0): windows.append({"x": round(x, 3), "y": round(h * 0.56, 3), "w": wd, "h": round(hh, 3), **({"wall": wall_, "z": round(z, 3)} if wall_ == "left" else {})})
    def rug(x, z, rw, rd, c=None): rugs.append({"x": round(x, 3), "z": round(z, 3), "w": rw, "d": rd, "c": c or accent})
    def pool(x, z, pw, pd): pools.append({"x": round(x, 3), "z": round(z, 3), "w": pw, "d": pd})
    def done(spawn):
        return {"id": tier, "w": w, "d": d, "h": h, "wall": wall, "wall2": wall2, "floor": floor, "trim": trim, "view": view, "accent": accent,
                "windows": windows, "rugs": rugs, "lights": lights, "props": props, "pictures": pictures, "pools": pools, "spawn": [round(spawn[0], 3), round(spawn[1], 3), spawn[2]]}
    return dict(w=w, d=d, h=h, fabric=fabric, blanket=blanket, add=add, back=back, left=left, right=right, art=wall_art, light=light, window=window, rug=rug, pool=pool, done=done)

def tv_wall(P_, tvp, z, sofa_x, sofa="sofa_l", fabric=None, bong_spot=None):
    """the lounge: a wall TV on the left, a sofa facing it, a coffee table with the bong between"""
    add, left = P_["add"], P_["left"]; w = P_["w"]
    add("tv_stand", left("tv_stand"), z, ry=90)
    add(tvp, -w / 2 + 0.24, z, ry=90, y=0.44, use="tv", label="TV", spot=[-w / 2 + 1.5, z + 0.8, -110])
    add(sofa, sofa_x, z, ry=-90, c={"fabric": fabric or P_["fabric"]})
    ctx = -w / 2 + 1.5
    add("coffee_table", ctx, z, ry=90)
    add("bong", ctx, z - 0.2, y=0.42, use="bong", label="Bong", spot=bong_spot or [ctx, z - 0.95, 0])
    P_["rug"](ctx + 0.5, z, 3.0, 2.2)
    return ctx

def build_condo():
    P_ = plan("condo"); add, back, left, right, w, d = P_["add"], P_["back"], P_["left"], P_["right"], P_["w"], P_["d"]
    # kitchen, back left: the fridge against the wall, an island you can walk round, two stools
    add("fridge", left("fridge"), -d / 2 + 0.6, ry=90)
    ix, iz = -1.5, -d / 2 + 1.25
    add("kitchen_island", ix, iz)
    for sx in (-0.55, 0.45): add("bar_stool", ix + sx, iz + 0.8)
    add("can", ix + 0.95, iz + 0.05, y=0.94, use="beer", label="Beer", spot=[ix + 1.1, iz + 0.95, 180])
    # bed, back right
    bx = w / 2 - 0.25 - 1.0; bz = back("bed_king")
    add("bed_king", bx, bz, use="bed", label="Bed", spot=[bx - 1.0 - 0.45, bz + 0.6, 90], c={"blanket": P_["blanket"]})
    add("nightstand", bx - 1.0 - 0.32, back("nightstand"))
    P_["art"]("art_1", "back", bx, 1.95)
    # lounge on the left, desk and closet on the right
    tv_wall(P_, "flat_tv_big", 0.7, -w / 2 + 3.1)
    dz = -0.3; add("desk", right("desk"), dz, ry=-90)
    add("laptop", right("desk") - 0.02, dz - 0.1, y=0.75, ry=-90, use="laptop", label="Laptop", spot=[right("desk") - 0.95, dz - 0.1, 90])
    add("chair", right("desk") - 0.62, dz - 0.1, ry=-90)
    add("wardrobe", right("wardrobe"), 1.1, ry=-90, use="closet", label="Closet", spot=[right("wardrobe") - 0.95, 1.1, 90])
    add("fish_tank", right("fish_tank"), 2.1, ry=-90, use="fish", label="Fish", spot=[right("fish_tank") - 0.85, 2.1, 90])
    add("bookshelf", 0.2, back("bookshelf"))
    add("floor_lamp", -w / 2 + 0.5, 2.3)
    add("plant", 1.2, 2.4); add("plant", -0.3, -d / 2 + 0.4)
    P_["art"]("art_0", "left", 2.4, 1.6)
    P_["window"](-0.6, 2.4, P_["h"] * 0.5)
    P_["light"](w / 2 - 0.3, 1.6, -d / 2 + 0.4); P_["light"](-0.6, 1.6, -d / 2 + 0.6, "#ff6aa8", 1.0, 4.5); P_["light"](-w / 2 + 0.5, 1.7, 2.3, "#ffe2b0", 0.9, 3.5)
    return P_["done"]((0.6, 1.4, 20))

def build_penthouse():
    P_ = plan("penthouse"); add, back, left, right, w, d = P_["add"], P_["back"], P_["left"], P_["right"], P_["w"], P_["d"]
    add("fridge", left("fridge"), -d / 2 + 0.6, ry=90)
    ix, iz = -1.9, -d / 2 + 1.3
    add("kitchen_island", ix, iz, c={"cabinet": "#0e0d12", "marble": "#1b1a20"})
    for sx in (-0.7, 0.0, 0.7): add("bar_stool", ix + sx, iz + 0.8, c={"leather_seat": "#d9b36c"})
    add("can", ix + 0.95, iz + 0.05, y=0.94, use="beer", label="Beer", spot=[ix + 1.2, iz + 0.95, 180])
    bx = w / 2 - 0.25 - 1.0; bz = back("bed_king")
    add("bed_king", bx, bz, use="bed", label="Bed", spot=[bx - 1.0 - 0.45, bz + 0.6, 90], c={"blanket": P_["blanket"], "sheet": "#2a2630", "pillow": "#3a3542"})
    add("nightstand", bx - 1.0 - 0.32, back("nightstand"), c={"wood": "#1c1a22"})
    add("safe", w / 2 - 0.4, -0.5, ry=-90, use="safe", label="Safe", spot=[w / 2 - 1.3, -0.5, 90])
    # the games table in the middle, under the chandelier
    add("pool_table", 0.1, -0.2, use="pool", label="Pool", spot=[0.1 - 1.55, -0.2, 90])
    add("chandelier", 0.1, -0.2, y=2.25)
    tv_wall(P_, "flat_tv_big", 1.6, -w / 2 + 3.2, fabric="#2b2622")
    dz = 0.6; add("desk", right("desk"), dz, ry=-90, c={"wood": "#1c1a22"})
    add("laptop", right("desk") - 0.02, dz - 0.1, y=0.75, ry=-90, use="laptop", label="Laptop", spot=[right("desk") - 0.95, dz - 0.1, 90])
    add("chair", right("desk") - 0.62, dz - 0.1, ry=-90)
    add("wardrobe", right("wardrobe"), 1.9, ry=-90, use="closet", label="Closet", spot=[right("wardrobe") - 0.95, 1.9, 90], c={"wood": "#1c1a22"})
    add("fish_tank_big", right("fish_tank_big"), d / 2 - 0.95, ry=-90, use="fish", label="Fish", spot=[right("fish_tank_big") - 0.85, d / 2 - 0.95, 90])
    add("trophy_case", left("trophy_case"), d / 2 - 0.85, ry=90, use="trophies", label="Trophies", spot=[left("trophy_case") + 0.9, d / 2 - 0.85, -90])
    add("floor_lamp", -w / 2 + 0.5, -0.4); add("plant", 1.6, 2.6); add("plant", 0.1, -d / 2 + 0.4)
    P_["art"]("art_4", "left", 2.9, 1.8, wide=False); P_["art"]("art_0", "back", bx, 2.0)
    P_["window"](-0.2, 3.6, P_["h"] * 0.62); P_["window"](0, 1.8, P_["h"] * 0.62, "left", -1.6)
    P_["light"](0.1, 2.0, -0.2, "#ffe2b0", 1.6, 5.5); P_["light"](-0.2, 1.7, -d / 2 + 0.6, "#ff6aa8", 1.1, 5); P_["light"](w / 2 - 0.4, 1.6, -d / 2 + 0.5, "#ffcf9a", 1.0, 4)
    return P_["done"]((-0.9, 1.7, 30))

def build_mansion():
    P_ = plan("mansion"); add, back, left, right, w, d = P_["add"], P_["back"], P_["left"], P_["right"], P_["w"], P_["d"]
    # the hearth in the middle of the back wall, the TV above it
    add("fireplace", 0, back("fireplace"), use="fire", label="Fireplace", spot=[0.9, -d / 2 + 1.3, 200])
    add("flat_tv_big", 0, -d / 2 + 0.06, y=1.4, use="tv", label="TV", spot=[0.9, -d / 2 + 1.9, 190])
    import math as _m
    pa = _m.radians(35); px_, pz_ = -2.9 + 0.78 * _m.cos(pa), -2.3 - 0.78 * _m.sin(pa)   # 0.78 m out from the centre on the keyboard side
    add("grand_piano", -2.9, -2.3, ry=35, use="piano", label="Piano", spot=[round(px_, 2), round(pz_, 2), 35 - 90])
    bx = w / 2 - 0.25 - 1.0; bz = back("bed_king")
    add("bed_king", bx, bz, use="bed", label="Bed", spot=[bx - 1.0 - 0.45, bz + 0.6, 90], c={"blanket": P_["blanket"], "sheet": "#efe7d8"})
    add("nightstand", bx - 1.0 - 0.32, back("nightstand"))
    add("safe", 1.3, back("safe"), use="safe", label="Safe", spot=[1.3, -d / 2 + 1.1, 180])
    # the lounge faces the fire
    sx_ = -0.6; sz_ = -0.3
    add("sofa_l", sx_, sz_, ry=180, c={"fabric": P_["fabric"]})
    add("coffee_table", sx_ + 0.4, sz_ - 1.25)
    add("bong", sx_ + 0.4, sz_ - 1.45, y=0.42, use="bong", label="Bong", spot=[sx_ + 1.3, sz_ - 1.35, -90])
    add("can", sx_ + 0.7, sz_ - 1.15, y=0.42, use="beer", label="Beer", spot=[sx_ + 1.3, sz_ - 1.0, -90])
    P_["rug"](sx_ - 0.2, sz_ - 1.0, 3.6, 2.6)
    # dining on the right, the chandelier over it
    tx, tz = 1.9, 1.0
    add("dining_table", tx, tz)
    for k, (dx, dz, ry) in enumerate(((-0.45, -0.72, 0), (0.45, -0.72, 0), (-0.45, 0.72, 180), (0.45, 0.72, 180))): add("dining_chair", tx + dx, tz + dz, ry=ry)
    add("chandelier", tx, tz, y=2.5)
    add("bookshelf", left("bookshelf"), 0.3, ry=90)
    add("trophy_case", left("trophy_case"), 2.1, ry=90, use="trophies", label="Trophies", spot=[left("trophy_case") + 0.9, 2.1, -90])
    add("wardrobe", right("wardrobe"), -0.6, ry=-90, use="closet", label="Closet", spot=[right("wardrobe") - 0.95, -0.6, 90])
    dz = 2.6; add("desk", right("desk"), dz, ry=-90)
    add("laptop", right("desk") - 0.02, dz - 0.1, y=0.75, ry=-90, use="laptop", label="Laptop", spot=[right("desk") - 0.95, dz - 0.1, 90])
    add("chair", right("desk") - 0.62, dz - 0.1, ry=-90)
    add("fish_tank_big", right("fish_tank_big"), 1.1, ry=-90, use="fish", label="Fish", spot=[right("fish_tank_big") - 0.85, 1.1, 90])
    add("floor_lamp", -2.6, 1.6); add("plant", -w / 2 + 0.4, d / 2 - 0.5); add("plant", w / 2 - 0.4, -d / 2 + 0.5)
    P_["art"]("art_3", "back", 0, 2.45); P_["art"]("art_5", "right", 1.1, 1.9)
    P_["window"](-2.6, 1.8, P_["h"] * 0.5); P_["window"](2.9, 1.8, P_["h"] * 0.5)
    P_["light"](tx, 2.2, tz, "#ffe2b0", 1.5, 5.5); P_["light"](0, 0.6, -d / 2 + 0.7, "#ff8a3a", 1.4, 4); P_["light"](-2.6, 1.7, 1.6, "#ffe2b0", 0.9, 3.5); P_["light"](w / 2 - 0.4, 1.6, -d / 2 + 0.5)
    return P_["done"]((0.4, 1.9, 20))

def build_island():
    P_ = plan("island"); add, back, left, right, w, d = P_["add"], P_["back"], P_["left"], P_["right"], P_["w"], P_["d"]
    # the daybed is the bed; the tiki bar is the kitchen
    bx = -w / 2 + 0.3 + 1.05; bz = back("daybed")
    add("daybed", bx, bz, use="bed", label="Daybed", spot=[bx + 1.05 + 0.5, bz + 0.3, -90])
    tx = 2.6; add("tiki_bar", tx, back("tiki_bar"))
    for sx in (-0.5, 0.4): add("bar_stool", tx + sx, -d / 2 + 1.1)
    add("can", tx - 0.7, -d / 2 + 0.4, y=1.14, use="beer", label="Beer", spot=[tx - 0.9, -d / 2 + 1.2, 180])
    add("palm", 1.0, -d / 2 + 0.7); add("palm", -w / 2 + 0.6, d / 2 - 0.8); add("palm", w / 2 - 0.5, -0.2)
    add("fish_tank_big", -0.4, back("fish_tank_big"), use="fish", label="Fish", spot=[-0.4, -d / 2 + 1.1, 180])
    tv_wall(P_, "flat_tv_big", 0.6, -w / 2 + 3.2, fabric="#f2efe8", bong_spot=[-w / 2 + 1.5, 0.6 - 0.95, 0])
    # home game by the water
    gx, gz = 0.3, 2.4
    add("poker_table", gx, gz, use="homegame", label="Home game", spot=[gx, gz - 1.25, 0])
    P_["pool"](3.0, 2.0, 2.8, 2.4)
    dz = -1.1; add("desk", right("desk"), dz, ry=-90, c={"wood": "#b98a5a"})
    add("laptop", right("desk") - 0.02, dz - 0.1, y=0.75, ry=-90, use="laptop", label="Laptop", spot=[right("desk") - 0.95, dz - 0.1, 90])
    add("chair", right("desk") - 0.62, dz - 0.1, ry=-90)
    add("wardrobe", left("wardrobe"), d / 2 - 0.8, ry=90, use="closet", label="Closet", spot=[left("wardrobe") + 0.95, d / 2 - 0.8, -90], c={"wood": "#b98a5a"})
    add("trophy_case", right("trophy_case"), -2.6, ry=-90, use="trophies", label="Trophies", spot=[right("trophy_case") - 0.9, -2.6, 90], c={"wood_dark": "#7a5a3a"})
    P_["art"]("art_2", "left", 2.0, 1.8)
    P_["window"](0.4, 3.6, P_["h"] * 0.62); P_["window"](0, 1.8, P_["h"] * 0.62, "left", -1.4)
    P_["light"](0.4, 1.8, -d / 2 + 0.6, "#ffb36b", 1.4, 6); P_["light"](2.9, 0.6, 2.0, "#7fe0ff", 1.0, 4); P_["light"](tx, 1.9, -d / 2 + 0.6, "#ffcf9a", 1.0, 4)
    return P_["done"]((0.4, 0.4, 20))

BUILDERS = {"condo": build_condo, "penthouse": build_penthouse, "mansion": build_mansion, "island": build_island}

if __name__ == "__main__":
    out = os.environ.get("HOMES_OUT", "out/Homes"); os.makedirs(out, exist_ok=True)
    for t in TIERS:
        L = BUILDERS[t]() if t in BUILDERS else build(t)
        L["props"] = [p for p in L["props"] if p is not None]
        json.dump(L, open(f"{out}/{t}.json", "w"), indent=1)
    print("ok", {t: len((BUILDERS[t]() if t in BUILDERS else build(t))["props"]) for t in TIERS})
