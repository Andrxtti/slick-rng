# Track H, H2: real strand hair (INBOX 20:15 "no cards"). Strands grow over the fitted MakeHuman hair shell along the
# style's flow, the same flow as hair.py's cards, as Blender hair curves; Principled Hair BSDF (melanin). For the Cycles
# stills and inserts. The shell stays underneath, matte and dark, so no scalp shows.
#   import strands; strands.grow(shell_object, style="back", melanin=0.9, count=60000)
import math
import bpy, bmesh
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

STYLES = {   # length (m), lift (m), jitter (radians), as hair.py (a strand is finer, so more of them)
    "back": {"flow": "back", "length": 0.09, "lift": 0.0025, "jitter": 0.12},
    "crop": {"flow": "back", "length": 0.03, "lift": 0.0015, "jitter": 0.35},
}


def grow(shell, style="back", melanin=0.9, count=60000, seed=5, segs=8, radius=0.00005):
    st = STYLES[style]; rng = np.random.default_rng(seed)
    deps = bpy.context.evaluated_depsgraph_get(); sh = shell.evaluated_get(deps); me = sh.to_mesh()
    bm = bmesh.new(); bm.from_mesh(me); bm.normal_update(); bmesh.ops.triangulate(bm, faces=bm.faces[:])
    tree = BVHTree.FromBMesh(bm)
    vs = [v.co.copy() for v in bm.verts]; centre = sum(vs, Vector()) / len(vs); top = max(v.z for v in vs)
    faces = [f for f in bm.faces if f.normal.z > -0.35]   # the shell's own space: the face looks down -Y
    areas = np.array([f.calc_area() for f in faces]); areas /= areas.sum()

    def flow(p, n):
        d = p - centre
        want = Vector((0, 0.4, 1)) if n.y < -0.3 else Vector((0, 1, -0.35))   # off the hairline: up, then back
        if d.y > 0.04 and p.z < top - 0.06: want = Vector((d.x * 0.5, 0.3, -1))     # the nape: down
        elif abs(d.x) > 0.055 and p.z < top - 0.05: want = Vector((0, 1, -0.45))   # the sides: back, a little down
        t = want - n * want.dot(n)
        if t.length < 1e-5: t = Vector((0, 1, 0)) - n * n.y
        a = rng.normal(0, st["jitter"]); t = t.normalized()
        return (t * math.cos(a) + n.cross(t) * math.sin(a)).normalized()

    pos = np.zeros((count, segs + 1, 3), np.float32)
    picks = rng.choice(len(faces), size=count, p=areas)
    for i, fi in enumerate(picks):
        f = faces[fi]; a, b, c = (l.vert.co for l in f.loops)
        r1, r2 = rng.random(), rng.random()
        if r1 + r2 > 1: r1, r2 = 1 - r1, 1 - r2
        p = a + (b - a) * r1 + (c - a) * r2; n = f.normal.copy()
        L = st["length"] * rng.uniform(0.6, 1.2); lift = st["lift"] * rng.uniform(0.5, 1.5)
        for s in range(segs + 1):
            hit = tree.find_nearest(p)
            if hit[0] is not None: p, n = hit[0], hit[1].normalized()
            pos[i, s] = p + n * (lift * (0.3 + s / segs))
            p = p + flow(p, n) * (L / segs)
    bm.free(); sh.to_mesh_clear()

    cv = bpy.data.hair_curves.new("strands"); cv.add_curves([segs + 1] * count)
    cv.position_data.foreach_set("vector", pos.reshape(-1))
    rad = cv.attributes.get("radius") or cv.attributes.new("radius", "FLOAT", "POINT")
    taper = np.tile(np.linspace(1.0, 0.25, segs + 1, dtype=np.float32) * radius, count)
    rad.data.foreach_set("value", taper)
    ob = bpy.data.objects.new("strands", cv); bpy.context.collection.objects.link(ob)
    ob.matrix_world = shell.matrix_world.copy()
    m = bpy.data.materials.new("strands"); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial"); hb = nt.nodes.new("ShaderNodeBsdfHairPrincipled")
    hb.parametrization = "MELANIN"; hb.inputs["Melanin"].default_value = melanin; hb.inputs["Melanin Redness"].default_value = 0.25
    hb.inputs["Roughness"].default_value = 0.28; hb.inputs["Radial Roughness"].default_value = 0.4; hb.inputs["Coat"].default_value = 0.1
    nt.links.new(hb.outputs[0], out.inputs[0]); cv.materials.append(m)
    # the shell under it: matte and dark, a scalp that never shows through as skin
    for slot in shell.material_slots:
        if slot.material and slot.material.use_nodes:
            p = next((x for x in slot.material.node_tree.nodes if x.type == "BSDF_PRINCIPLED"), None)
            if p:
                for l in list(p.inputs["Base Color"].links): slot.material.node_tree.links.remove(l)
                p.inputs["Base Color"].default_value = (0.02, 0.015, 0.012, 1); p.inputs["Roughness"].default_value = 0.9
    print(f"[strands] {count} strands x {segs + 1} points, melanin {melanin}", flush=True)
    return ob
