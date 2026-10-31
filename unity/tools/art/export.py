# crop the drive layers to their content, paint the small extras, export ride OBJs, write drive.json
import numpy as np, cv2, json, sys, os
sys.path.insert(0, 'rides')
H0 = 1152
layers = [("sky", 0.0, False), ("city", 0.03, False), ("road", 1.0, True), ("palms", 1.75, True)]
meta = {"w": 2048, "h": H0, "layers": []}
for name, speed, tile in layers:
    im = cv2.imread(f"drive/{name}.png", cv2.IMREAD_UNCHANGED)
    if name == "sky":
        top, bot = 0, im.shape[0]
        cv2.imwrite(f"out/Drive/{name}.png", im[..., :3])     # opaque: no alpha channel needed
    else:
        a = im[..., 3]; rows = np.where(a.max(1) > 2)[0]; top, bot = int(rows.min()), int(rows.max()) + 1
        cv2.imwrite(f"out/Drive/{name}.png", im[top:bot])
    meta["layers"].append({"name": name, "y": top, "h": bot - top, "speed": speed, "tile": tile})
# where the car drives: the near lane of the causeway (in 1152-space)
meta["laneY"] = 1060; meta["roadTop"] = int(H0 * 0.80)
# soft extras: ground shadow and headlight beam
sh = np.zeros((128, 512, 4), np.uint8); m = np.zeros((128, 512), np.float32)
cv2.ellipse(m, (256, 64), (230, 30), 0, 0, 360, 1, -1, cv2.LINE_AA); m = cv2.GaussianBlur(m, (0, 0), 14)
sh[..., 3] = np.clip(m * 220, 0, 255).astype(np.uint8); cv2.imwrite("out/Drive/shadow.png", sh)
bw, bh = 768, 256; yy, xx = np.mgrid[0:bh, 0:bw].astype(np.float32)
t = xx / bw; spread = 10 + t * bh * 0.42; cy = bh / 2
cone = np.exp(-((yy - cy) / spread) ** 2) * (1 - t) ** 1.4 * np.clip(t * 12, 0, 1)
beam = np.zeros((bh, bw, 4), np.uint8); beam[..., 0] = 230; beam[..., 1] = 240; beam[..., 2] = 255
beam[..., 3] = np.clip(cone * 150, 0, 255).astype(np.uint8); cv2.imwrite("out/Drive/beam.png", beam)
json.dump(meta, open("out/Drive/drive.json", "w"), indent=1)
# rides
from cars import car, RIDES
for id_, sp in RIDES.items():
    m = car(sp); m.save_obj(f"out/Rides/{id_}.obj")
print(json.dumps(meta)); os.system("ls -la out/Drive out/Rides")
