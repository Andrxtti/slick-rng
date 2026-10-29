# Hair cards (DAY2 Batch A2): strands that break up the silhouette of MakeHuman's helmet-like hair. Imported by
# character.py when a spec has "hair_cards": {"style": ..., ...}.
# Roots are scattered over the fitted MakeHuman hair (the shell, which stays as the dense under-layer, so no scalp shows).
# Each card grows across the shell along the style's flow (back, fringe, crop, tousled, side), lifts a few millimetres
# off it, and tapers; two layers, a dense short one and a sparser long one on top. The strand texture is drawn here
# (grey, tinted by the hair colour in Unity), so it is ours (CC0). Every card is skinned 100% to the head bone.
import math, os
import bpy, bmesh
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

STYLES = {
    # flow: where hair goes from the hairline; length (m); lift (m); count; jitter (radians of randomness)
    "back":    {"flow": "back", "length": 0.075, "lift": 0.003, "count": 1700, "jitter": 0.1},    # Slick, Tommy, David
    "crop":    {"flow": "back", "length": 0.026, "lift": 0.002, "count": 2600, "jitter": 0.25},   # Mustich, Lou
    "fringe":  {"flow": "fringe", "length": 0.07, "lift": 0.004, "count": 1700, "jitter": 0.22},  # Bio
    "tousled": {"flow": "up", "length": 0.05, "lift": 0.007, "count": 1900, "jitter": 0.7},       # Nick
    "side":    {"flow": "side", "length": 0.06, "lift": 0.004, "count": 1700, "jitter": 0.2},     # the player's default
}


def strand_texture(path, seed=3):
    """4 strand clumps side by side (u quarters), roots at v=0 and ragged tips at v=1; grey, alpha = strands."""
    W, H = 512, 1024
    rng = np.random.default_rng(seed)
    img = np.zeros((H, W, 4), np.float32)
    v = np.linspace(0, 1, H)[:, None]
    for q in range(4):
        for _ in range(70):
            x0 = q * W / 4 + rng.uniform(6, W / 4 - 6); wid = rng.uniform(0.7, 2.2)
            wave = rng.uniform(0, 5) * np.sin(v * rng.uniform(2, 6) + rng.uniform(0, 6))
            tip = rng.uniform(0.6, 1.0)
            xs = np.arange(W)[None, :]
            a = np.clip(1 - np.abs(xs - (x0 + wave)) / wid, 0, 1) * (v < tip) * np.clip((tip - v) / 0.15, 0, 1)
            shade = rng.uniform(0.65, 1.0)
            img[..., 3] = np.maximum(img[..., 3], a)
            img[..., :3] = np.where(a[..., None] > 0.02, np.maximum(img[..., :3], shade * a[..., None] ** 0.3), img[..., :3])
    img[..., :3] = np.where(img[..., 3:4] > 0, img[..., :3], 0.7)
    img = img[::-1]   # Blender images start at the bottom row
    im = bpy.data.images.new("haircards", W, H, alpha=True); im.pixels[:] = img.ravel()
    im.filepath_raw = path; im.file_format = "PNG"; im.save()
    return im


def build(shell, rig, out_tex, spec, log=print, color=None):
    st = dict(STYLES[spec.get("style", "back")]); st.update({k: v for k, v in spec.items() if k != "style"})
    rng = np.random.default_rng(spec.get("seed", 11))
    head = rig.pose.bones.get("mixamorig:Head") or rig.pose.bones.get("Head")
    deps = bpy.context.evaluated_depsgraph_get()
    sh = shell.evaluated_get(deps); me = sh.to_mesh()
    mw = shell.matrix_world
    bm = bmesh.new(); bm.from_mesh(me); bm.transform(mw); bm.normal_update()
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    tree = BVHTree.FromBMesh(bm)
    vs = [v.co.copy() for v in bm.verts]
    centre = sum(vs, Vector()) / len(vs); top = max(v.z for v in vs)
    front = Vector((0, -1, 0))   # the face looks down -Y (as in character.py)
    # area-weighted roots, upper part of the shell only (not the nape's inside)
    faces = [f for f in bm.faces if f.normal.z > -0.35]
    areas = np.array([f.calc_area() for f in faces]); areas /= areas.sum()
    part_x = centre.x + 0.025   # the side part, a little off centre

    def flow(p, n):
        d = p - centre
        if st["flow"] == "back": want = Vector((0, 0.4, 1)) if n.y < -0.3 else Vector((0, 1, -0.35))   # off the hairline: up, then back
        elif st["flow"] == "fringe": want = Vector((0, -1, -0.9)) if d.y < 0.02 else Vector((0, 0.2, -1))
        elif st["flow"] == "up": want = Vector((d.x * 0.6, -0.3, 1.0))
        else: want = Vector((1 if p.x > part_x else -1, 0.35, -0.6)) if n.y > -0.3 else Vector((1 if p.x > part_x else -1, 0.2, 0.8))
        if st["flow"] in ("back", "side"):
            if d.y > 0.04 and p.z < top - 0.06: want = Vector((d.x * 0.5, 0.3, -1))     # the nape: down
            elif abs(d.x) > 0.055 and p.z < top - 0.05: want = Vector((0, 1, -0.45))   # neat sides: back, a little down
        t = want - n * want.dot(n)
        if t.length < 1e-5: t = Vector((0, 1, 0)) - n * n.y
        a = rng.normal(0, st["jitter"])
        return (t.normalized() * math.cos(a) + n.cross(t.normalized()) * math.sin(a)).normalized()

    verts, faces_out, uvs = [], [], []
    for layer, (count, length, lift, width) in enumerate(((int(st["count"]), st["length"] * 0.7, st["lift"], 0.010),
                                                          (int(st["count"] * 0.45), st["length"], st["lift"] * 1.3, 0.013))):
        picks = rng.choice(len(faces), size=count, p=areas)
        for fi in picks:
            f = faces[fi]; a, b, c = (l.vert.co for l in f.loops)
            r1, r2 = rng.random(), rng.random()
            if r1 + r2 > 1: r1, r2 = 1 - r1, 1 - r2
            p = a + (b - a) * r1 + (c - a) * r2; n = f.normal.copy()
            L = length * rng.uniform(0.7, 1.15); segs = 5; q = rng.integers(0, 4)
            w = width * rng.uniform(0.75, 1.25)
            pts = []
            for s in range(segs + 1):
                k = s / segs
                hit = tree.find_nearest(p)
                if hit[0] is not None: p, n = hit[0], hit[1].normalized()
                pts.append((p + n * (lift * (0.4 + k)), n))
                p = p + flow(p, n) * (L / segs)
            base = len(verts)
            for s, (pp, nn) in enumerate(pts):
                tdir = (pts[min(s + 1, segs)][0] - pts[max(s - 1, 0)][0]).normalized()
                side = tdir.cross(nn).normalized() * (w * (1 - 0.6 * s / segs) / 2)
                verts += [pp - side, pp + side]
                uvs += [((q + 0.05) / 4, s / segs), ((q + 0.95) / 4, s / segs)]
            for s in range(segs):
                i = base + s * 2; faces_out.append((i, i + 1, i + 3, i + 2))
    bm.free(); sh.to_mesh_clear()

    mesh = bpy.data.meshes.new("haircards"); mesh.from_pydata([tuple(v) for v in verts], [], faces_out)
    uvl = mesh.uv_layers.new(name="UVMap")
    for poly in mesh.polygons:
        for li in poly.loop_indices: uvl.data[li].uv = uvs[mesh.loops[li].vertex_index]
    ob = bpy.data.objects.new("haircards", mesh); bpy.context.collection.objects.link(ob)
    ob.parent = rig
    g = ob.vertex_groups.new(name=head.name); g.add(range(len(verts)), 1.0, "REPLACE")
    ob.modifiers.new("Armature", "ARMATURE").object = rig
    # material: the strand texture, alpha as the cutout (Unity: Materials "haircards", tinted by the hair colour)
    path = os.path.join(out_tex, "haircards_diffuse.png"); im = strand_texture(path, spec.get("seed", 11))
    m = bpy.data.materials.new("haircards"); m.use_nodes = True; nt = m.node_tree; bsdf = nt.nodes["Principled BSDF"]
    tn = nt.nodes.new("ShaderNodeTexImage"); tn.image = im
    if color:   # the same multiply Unity applies, so the preview matches
        mx = nt.nodes.new("ShaderNodeMix"); mx.data_type = "RGBA"; mx.blend_type = "MULTIPLY"; mx.inputs["Factor"].default_value = 1
        mx.inputs[7].default_value = (*color, 1); nt.links.new(tn.outputs["Color"], mx.inputs[6]); nt.links.new(mx.outputs[2], bsdf.inputs["Base Color"])
    else: nt.links.new(tn.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(tn.outputs["Alpha"], bsdf.inputs["Alpha"]); bsdf.inputs["Roughness"].default_value = 0.45
    m.surface_render_method = "DITHERED"; mesh.materials.append(m)
    log("hair cards", st.get("flow"), len(faces_out), "cards segments,", len(verts), "verts")
    return ob
