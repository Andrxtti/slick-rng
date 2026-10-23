# Apartment layouts for each home tier -> Homes/<tier>.json. Room: x across (-w/2..w/2), z from the back wall
# (-d/2) to the open front (+d/2), y up. Props face +z unless rotated (ry, degrees: 90 faces +x, -90 faces -x).
# "use" makes a prop interactive; "spot" is where you stand to use it [x, z, facing].
import json, os
P = json.load(open("out/props.json"))
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

if __name__ == "__main__":
    os.makedirs("out/Homes", exist_ok=True)
    for t in TIERS:
        json.dump(build(t), open(f"out/Homes/{t}.json", "w"), indent=1)
    print("ok", list(TIERS)); print(json.dumps(build("studio"))[:1500])
