# Closet tiles: each painted garment rendered on its own (no body), the way a store shows it. A small textured
# z-buffer renderer over the UMA slot mesh + our diffuse. -> <out>/<slot>_<id>.png (RGBA, 256x256)
import json, os, sys, math, numpy as np, cv2
from clothes import BASES, ALL, HERE
T = 256
def render(slot_name, tex_bgr, out_path, view):
    d = json.load(open(os.path.join(HERE, 'uv', slot_name + '_slot.json')))
    uv, pos, tris = np.array(d['uv']), np.array(d['pos']), np.array(d['tris']).reshape(-1, 3)
    # body space: x right, y front, z up -> view space
    P = pos.copy(); c = (P.min(0) + P.max(0)) / 2; size = np.linalg.norm(P.max(0) - P.min(0))
    yaw, pitch = view
    def rot(p):
        x, y, z = p[..., 0] - c[0], p[..., 1] - c[1], p[..., 2] - c[2]
        cy, sy = math.cos(yaw), math.sin(yaw); x, y = x * cy - y * sy, x * sy + y * cy
        cp, sp = math.cos(pitch), math.sin(pitch); y, z = y * cp - z * sp, y * sp + z * cp
        return np.stack([x, y, z], -1)
    V = rot(P)
    # orthographic: screen x = x, screen y = -z, depth = -y (camera at -y looking +y... front is +y so camera sits at +y looking -y)
    scale = (T * 0.84) / size * 1.15
    sx = V[:, 0] * scale + T / 2; sy = -V[:, 2] * scale + T / 2; depth = -V[:, 1]
    # normals
    vn = np.zeros_like(P)
    for t in tris:
        a, b, cc = P[t]; vn[t] += np.cross(b - a, cc - a)
    vn /= np.maximum(np.linalg.norm(vn, axis=1, keepdims=True), 1e-9); VN = rot(vn + c)
    img = np.zeros((T, T, 4), np.float32); zb = np.full((T, T), -np.inf)
    H, W = tex_bgr.shape[:2]; tex = tex_bgr[..., ::-1].astype(np.float32) / 255
    L1 = np.array([-0.4, -0.7, 0.6]); L1 /= np.linalg.norm(L1); L2 = np.array([0.6, -0.5, -0.2]); L2 /= np.linalg.norm(L2)
    for t in tris:
        xs, ys, zs = sx[t], sy[t], depth[t]
        x0, x1 = int(max(0, np.floor(xs.min()))), int(min(T - 1, np.ceil(xs.max()))); y0, y1 = int(max(0, np.floor(ys.min()))), int(min(T - 1, np.ceil(ys.max())))
        if x1 < x0 or y1 < y0: continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        (ax, ay), (bx, by), (cx, cy) = (xs[0], ys[0]), (xs[1], ys[1]), (xs[2], ys[2])
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-9: continue
        w0 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den; w1 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den; w2 = 1 - w0 - w1
        inside = (w0 >= -1e-3) & (w1 >= -1e-3) & (w2 >= -1e-3)
        if not inside.any(): continue
        z = w0 * zs[0] + w1 * zs[1] + w2 * zs[2]
        sub = zb[y0:y1 + 1, x0:x1 + 1]; upd = inside & (z > sub)
        if not upd.any(): continue
        u = w0 * uv[t[0], 0] + w1 * uv[t[1], 0] + w2 * uv[t[2], 0]; v = w0 * uv[t[0], 1] + w1 * uv[t[1], 1] + w2 * uv[t[2], 1]
        ui = np.clip((u * W).astype(int), 0, W - 1); vi = np.clip(((1 - v) * H).astype(int), 0, H - 1)
        col = tex[vi, ui]
        nn = w0[..., None] * VN[t[0]] + w1[..., None] * VN[t[1]] + w2[..., None] * VN[t[2]]; nn /= np.linalg.norm(nn, axis=-1, keepdims=True) + 1e-9
        # two-sided: flip normals facing away
        flip = nn[..., 1] > 0; nn[flip] = -nn[flip]
        d1 = np.clip((nn * L1).sum(-1), 0, 1); d2 = np.clip((nn * L2).sum(-1), 0, 1)
        shade = 0.42 + 0.5 * d1 + 0.2 * d2
        spec = np.clip((nn * (L1 + np.array([0, -1, 0])) / 2).sum(-1), 0, 1) ** 24 * 0.12
        c3 = np.clip(col * shade[..., None] + spec[..., None], 0, 1)
        sub[upd] = z[upd]
        img[y0:y1 + 1, x0:x1 + 1][upd] = np.concatenate([c3, np.ones(c3.shape[:2] + (1,))], -1)[upd]
    # soft drop shadow + anti-alias
    a = img[..., 3]; sh = cv2.GaussianBlur(a, (0, 0), 5) * 0.45
    out = np.zeros((T, T, 4), np.float32)
    out[..., 3] = np.clip(a + sh * (1 - a), 0, 1)
    out[..., :3] = img[..., :3] * a[..., None]   # premultiplied over shadow (black)
    out8 = np.zeros((T, T, 4), np.uint8); out8[..., 3] = (out[..., 3] * 255).astype(np.uint8)
    rgb = np.where(out[..., 3:4] > 0, out[..., :3] / np.maximum(out[..., 3:4], 1e-6), 0)
    out8[..., :3] = (np.clip(rgb, 0, 1)[..., ::-1] * 255).astype(np.uint8)
    cv2.imwrite(out_path, out8)

VIEWS = {"Chest": (-0.35, 0.12), "Legs": (-0.3, 0.1), "Feet": (-0.9, 0.35)}
if __name__ == "__main__":
    texdir, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
    only = set(sys.argv[3].split(",")) if len(sys.argv) > 3 else None
    for slot, items in ALL.items():
        for id_, spec in items.items():
            if only and id_ not in only: continue
            tp = os.path.join(texdir, f"{slot}_{id_}.png")
            if not os.path.exists(tp): continue
            b = BASES[spec["base"]]
            render(b[0], cv2.imread(tp), os.path.join(out, f"{slot}_{id_}.png"), VIEWS[b[2]]); print(slot, id_)
