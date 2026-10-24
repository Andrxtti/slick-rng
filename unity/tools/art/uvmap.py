# Draw each garment's UV layout coloured by where it sits on the body (red = front, blue = back; bright = high),
# so the clothes painter knows which pixels are the chest, the back, the sleeves and the hem.
import json, sys, os, numpy as np, cv2
def load(name):
    d = json.load(open(os.path.join(os.path.dirname(__file__), 'uv', name + '_slot.json')))
    return np.array(d['uv']), np.array(d['pos']), np.array(d['tris']).reshape(-1, 3)
def region_map(name, S=512):
    """per-texel: y (0..1 over the garment), z (front +), x (right +), mask"""
    uv, pos, tris = load(name)
    y0, y1 = pos[:, 1].min(), pos[:, 1].max()
    img = np.zeros((S, S, 4), np.float32)
    for t in tris:
        p = uv[t]; pts = np.array([[p[i, 0] * S, (1 - p[i, 1]) * S] for i in range(3)], np.int32)
        q = pos[t].mean(0)
        col = ((q[1] - y0) / max(1e-6, y1 - y0), q[2], q[0], 1.0)
        cv2.fillConvexPoly(img, pts, col)
    return img, (y0, y1)
if __name__ == '__main__':
    out = sys.argv[1]; os.makedirs(out, exist_ok=True)
    names = [f[:-10] for f in sorted(os.listdir(os.path.join(os.path.dirname(__file__), 'uv'))) if f.endswith('_slot.json')]
    for n in names:
        img, (y0, y1) = region_map(n)
        vis = np.zeros((512, 512, 3), np.uint8)
        h = img[..., 0]; z = img[..., 1]; x = img[..., 2]; m = img[..., 3] > 0
        vis[..., 2] = np.clip((z > 0) * (80 + 170 * h), 0, 255)   # front: red
        vis[..., 0] = np.clip((z <= 0) * (80 + 170 * h), 0, 255)  # back: blue
        vis[..., 1] = np.clip(128 + x * 600, 0, 255) * m          # right side greener
        vis[~m] = 20
        cv2.putText(vis, f'{n} y {y0:.2f}..{y1:.2f} z {img[...,1][m].min():.2f}..{img[...,1][m].max():.2f}', (6, 18), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (255, 255, 255), 1)
        cv2.imwrite(os.path.join(out, n + '.png'), vis)
    print(names)
