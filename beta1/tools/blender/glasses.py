# Glasses (DAY2 Batch A5), imported by character.py when a spec has "glasses": {"style": ..., "up": bool}.
# Two rims round the eyes, a bridge, and temples back to the ears, built from curves (bevelled) and skinned 100% to the
# head bone. Lenses are thin discs with their own material ("lenses": clear, or tinted). "up": pushed up onto the head,
# as Slick wears his indoors. Styles: wire (Mustich: thin gold wire, round-ish), clear (Nick: clear acetate, tinted
# lenses), dark (Slick: black rectangular acetate).
import math
import bpy
from mathutils import Vector, Matrix

STYLES = {
    "wire":  {"w": 0.023, "h": 0.019, "round": 0.9, "thick": 0.0009, "frame": (0.62, 0.5, 0.28), "metal": 1.0, "tint": (0.85, 0.88, 0.9, 0.12)},
    "clear": {"w": 0.025, "h": 0.021, "round": 0.55, "thick": 0.0022, "frame": (0.8, 0.8, 0.82), "metal": 0.0, "tint": (0.35, 0.45, 0.55, 0.45)},
    "dark":  {"w": 0.026, "h": 0.017, "round": 0.25, "thick": 0.0028, "frame": (0.02, 0.02, 0.025), "metal": 0.0, "tint": (0.05, 0.05, 0.06, 0.85)},
}


def rim_points(cx, cz, w, h, r, n=40):
    """A rounded rectangle (superellipse) in the x/z plane round (cx, cz)."""
    e = 2 + (1 - r) * 6   # 2 = ellipse, bigger = squarer
    pts = []
    for k in range(n):
        a = k / n * math.tau; c, s = math.cos(a), math.sin(a)
        pts.append((cx + w * math.copysign(abs(c) ** (2 / e), c), cz + h * math.copysign(abs(s) ** (2 / e), s)))
    return pts


def curve(name, pts3, thick, cyclic, mat):
    cu = bpy.data.curves.new(name, "CURVE"); cu.dimensions = "3D"; cu.bevel_depth = thick; cu.bevel_resolution = 2
    sp = cu.splines.new("POLY"); sp.points.add(len(pts3) - 1)
    for p, q in zip(sp.points, pts3): p.co = (*q, 1)
    sp.use_cyclic_u = cyclic
    ob = bpy.data.objects.new(name, cu); bpy.context.collection.objects.link(ob); cu.materials.append(mat)
    return ob


def build(eyes, rig, spec, log=print):
    st = STYLES[spec.get("style", "wire")]
    head = rig.pose.bones.get("mixamorig:Head") or rig.pose.bones.get("Head")
    vs = [eyes.matrix_world @ v.co for v in eyes.data.vertices]
    L = [v for v in vs if v.x > 0]; R = [v for v in vs if v.x <= 0]
    cl = sum(L, Vector()) / len(L); cr = sum(R, Vector()) / len(R)
    front = min(v.y for v in vs) - 0.012            # the face looks down -Y; the lenses sit 12 mm in front of the eyes
    ear_y = (cl.y + cr.y) / 2 + 0.085
    z0 = (cl.z + cr.z) / 2
    up = spec.get("up", False)
    fm = bpy.data.materials.new("glasses"); fm.use_nodes = True; b = fm.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*st["frame"], 1); b.inputs["Metallic"].default_value = st["metal"]; b.inputs["Roughness"].default_value = 0.25
    lm = bpy.data.materials.new("lenses"); lm.use_nodes = True; b = lm.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*st["tint"][:3], 1); b.inputs["Alpha"].default_value = st["tint"][3]; b.inputs["Roughness"].default_value = 0.02
    lm.surface_render_method = "BLENDED"
    parts = []
    for c in (cl, cr):
        rim = rim_points(c.x, z0, st["w"], st["h"], st["round"])
        parts.append(curve("rim", [(x, front, z) for x, z in rim], st["thick"], True, fm))
        me = bpy.data.meshes.new("lens"); me.from_pydata([(x, front + 0.0005, z) for x, z in rim], [], [list(range(len(rim)))])
        ob = bpy.data.objects.new("lens", me); bpy.context.collection.objects.link(ob); me.materials.append(lm); parts.append(ob)
    inner = st["w"] * 0.95
    parts.append(curve("bridge", [(cr.x + inner, front, z0 + st["h"] * 0.35), (0, front - 0.003, z0 + st["h"] * 0.5), (cl.x - inner, front, z0 + st["h"] * 0.35)], st["thick"], False, fm))
    for c, sgn in ((cl, 1), (cr, -1)):
        x = c.x + sgn * st["w"]
        parts.append(curve("temple", [(x, front, z0 + st["h"] * 0.4), (x + sgn * 0.012, front + 0.03, z0 + st["h"] * 0.4), (x + sgn * 0.016, ear_y, z0 + 0.002), (x + sgn * 0.012, ear_y + 0.012, z0 - 0.02)], st["thick"], False, fm))
    for p in parts:   # one at a time: convert acts on everything selected (the body would lose its shape keys)
        if p.type != "CURVE": continue
        bpy.ops.object.select_all(action="DESELECT"); bpy.context.view_layer.objects.active = p; p.select_set(True)
        bpy.ops.object.convert(target="MESH")
    bpy.ops.object.select_all(action="DESELECT")
    # one object per material: "glasses" (frame) and "lenses" (Unity names its parts by object)
    out = []
    for name in ("glasses", "lenses"):
        group = [o for o in bpy.context.collection.objects if o.type == "MESH" and o.data.materials and o.data.materials[0].name.startswith(name) and o.parent is None and (o.name.startswith(("rim", "bridge", "temple")) if name == "glasses" else o.name.startswith("lens"))]
        bpy.ops.object.select_all(action="DESELECT")
        for o in group: o.select_set(True)
        bpy.context.view_layer.objects.active = group[0]; bpy.ops.object.join(); ob = bpy.context.active_object; ob.name = name
        if up:   # pushed up on the head: lifted over the brow and tipped back onto the hair
            pivot = Vector((0, ear_y, z0))
            ob.data.transform(Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(-38), 4, "X") @ Matrix.Translation(-pivot))
            ob.data.transform(Matrix.Translation((0, -0.004, 0.035)))
        ob.parent = rig
        g = ob.vertex_groups.new(name=head.name); g.add(range(len(ob.data.vertices)), 1.0, "REPLACE")
        ob.modifiers.new("Armature", "ARMATURE").object = rig
        out.append(ob)
    log("glasses", spec.get("style"), "up" if up else "on", [len(o.data.vertices) for o in out])
    return out
