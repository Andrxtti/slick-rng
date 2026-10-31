# top-down plan of each home from its json: footprints (grey), uses (gold) with their standing spots (dots), pools (blue)
import json, sys, os, math, numpy as np, cv2
P = json.load(open(sys.argv[1])); hd = sys.argv[2]; out = sys.argv[3]; os.makedirs(out, exist_ok=True)
S = 90
for t in ["studio", "apt", "condo", "penthouse", "mansion", "island"]:
    L = json.load(open(os.path.join(hd, t + ".json"))); w, d = L["w"], L["d"]
    W, H = int(w * S) + 40, int(d * S) + 40
    img = np.full((H, W, 3), 30, np.uint8)
    def px(x, z): return int(20 + (x + w / 2) * S), int(20 + (d / 2 - z) * S)   # back wall at the top
    cv2.rectangle(img, px(-w / 2, d / 2), px(w / 2, -d / 2), (70, 70, 70), -1)
    for pool in L.get("pools", []): cv2.rectangle(img, px(pool["x"] - pool["w"] / 2, pool["z"] + pool["d"] / 2), px(pool["x"] + pool["w"] / 2, pool["z"] - pool["d"] / 2), (160, 110, 40), -1)
    for r in L.get("rugs", []): cv2.rectangle(img, px(r["x"] - r["w"] / 2, r["z"] + r["d"] / 2), px(r["x"] + r["w"] / 2, r["z"] - r["d"] / 2), (60, 80, 90), -1)
    for p in L["props"]:
        if p is None or p["p"] not in P: continue
        mn, mx = P[p["p"]]["min"], P[p["p"]]["max"]; a = math.radians(p["ry"]); s_ = p.get("s", 1)
        cs = [(mn[0], mn[2]), (mx[0], mn[2]), (mx[0], mx[2]), (mn[0], mx[2])]
        pts = []
        for (x, z) in cs:
            x *= s_; z *= s_; rx = x * math.cos(a) + z * math.sin(a); rz = -x * math.sin(a) + z * math.cos(a)
            pts.append(px(p["x"] + rx, p["z"] + rz))
        col = (60, 170, 220) if p.get("use") else (150, 150, 150)
        if p["p"].startswith("art") or p["p"] == "chandelier": col = (120, 90, 170)
        cv2.fillPoly(img, [np.array(pts, np.int32)], col); cv2.polylines(img, [np.array(pts, np.int32)], True, (20, 20, 20), 1)
        cv2.putText(img, p["p"][:9], (min(q[0] for q in pts) + 2, min(q[1] for q in pts) + 10), cv2.FONT_HERSHEY_SIMPLEX, 0.3, (255, 255, 255), 1)
        if p.get("spot"):
            sp = px(p["spot"][0], p["spot"][1]); cv2.circle(img, sp, 5, (40, 220, 255), -1)
            f = math.radians(p["spot"][2]); cv2.line(img, sp, (int(sp[0] + 14 * math.sin(f)), int(sp[1] - 14 * math.cos(f))), (40, 220, 255), 2)
    sp = px(L["spawn"][0], L["spawn"][1]); cv2.circle(img, sp, 7, (80, 255, 80), 2)
    cv2.putText(img, t, (8, 14), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (255, 255, 255), 1)
    cv2.imwrite(os.path.join(out, t + ".png"), img)
print("ok")
