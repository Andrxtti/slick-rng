# Shared helpers for the set builders (run inside Blender: blender -b -P tools/blender/<set>.py).
# Units are metres, Z up. Materials are Principled BSDF with Poly Haven 1k maps, so glTF carries them into Unity as-is.
import math, os
import bpy, bmesh
from mathutils import Vector

PH = r"C:\Dev\Beta1Assets\polyhaven"
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name, tex=None, color=(0.8, 0.8, 0.8), rough=0.5, metal=0.0, tint=None, emit=None, emit_strength=0.0, alpha=1.0, rough_scale=1.0):
    """A Principled material. tex: Poly Haven id (diffuse, normal, roughness maps); tint multiplies the diffuse map."""
    m = bpy.data.materials.get(name)
    if m: return m
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*color, 1); b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if alpha < 1:
        b.inputs["Alpha"].default_value = alpha
        m.surface_render_method = "BLENDED"
    if emit:
        b.inputs["Emission Color"].default_value = (*emit, 1); b.inputs["Emission Strength"].default_value = emit_strength
    if tex:
        d = os.path.join(PH, tex)
        def img(suffix, non_color):
            p = os.path.join(d, f"{tex}_{suffix}_1k.jpg")
            if not os.path.exists(p): return None
            n = nt.nodes.new("ShaderNodeTexImage"); n.image = bpy.data.images.load(p, check_existing=True)
            if non_color: n.image.colorspace_settings.name = "Non-Color"
            return n
        diff = img("diff", False)
        if diff:
            if tint:
                mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"; mix.blend_type = "MULTIPLY"
                mix.inputs["Factor"].default_value = 1.0; mix.inputs[7].default_value = (*tint, 1)
                nt.links.new(diff.outputs["Color"], mix.inputs[6]); nt.links.new(mix.outputs[2], b.inputs["Base Color"])
            else:
                nt.links.new(diff.outputs["Color"], b.inputs["Base Color"])
        nor = img("nor_gl", True)
        if nor:
            nm = nt.nodes.new("ShaderNodeNormalMap"); nt.links.new(nor.outputs["Color"], nm.inputs["Color"])
            nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
        r = img("rough", True)
        if r:
            if rough_scale != 1.0:   # wet or polished: scale the roughness map down
                mm = nt.nodes.new("ShaderNodeMath"); mm.operation = "MULTIPLY"; mm.inputs[1].default_value = rough_scale
                nt.links.new(r.outputs["Color"], mm.inputs[0]); nt.links.new(mm.outputs[0], b.inputs["Roughness"])
            else:
                nt.links.new(r.outputs["Color"], b.inputs["Roughness"])
    return m


def box_uv(ob, size=1.0):
    """World-space box projection: one texture repeat per `size` metres on every face."""
    me = ob.data; bm = bmesh.new(); bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify(); mw = ob.matrix_world
    for f in bm.faces:
        n = (mw.to_3x3() @ f.normal); ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            p = mw @ l.vert.co
            u, v = [(p.y, p.z), (p.x, p.z), (p.x, p.y)][ax]
            l[uv].uv = (u / size, v / size)
    bm.to_mesh(me); bm.free()


def box(name, center, size, material, uv=1.0, bevel=0.0, segments=2, parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    ob = bpy.context.active_object; ob.name = name; ob.scale = size
    bpy.ops.object.transform_apply(scale=True)
    ob.data.materials.append(material)
    if uv: box_uv(ob, uv)
    if bevel: add_bevel(ob, bevel, segments)
    if parent: ob.parent = parent
    return ob


def cyl(name, center, r, h, material, verts=32, bevel=0.0, rot=None, uv=0.5):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=center, rotation=rot or (0, 0, 0))
    ob = bpy.context.active_object; ob.name = name; ob.data.materials.append(material)
    bpy.ops.object.shade_smooth_by_angle()
    if uv: box_uv(ob, uv)
    if bevel: add_bevel(ob, bevel, 2)
    return ob


def torus(name, center, R, r, material, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, location=center, rotation=rot, major_segments=48, minor_segments=12)
    ob = bpy.context.active_object; ob.name = name; ob.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    return ob


def add_bevel(ob, w, seg):
    m = ob.modifiers.new("bevel", "BEVEL"); m.width = w; m.segments = seg; m.limit_method = "ANGLE"
    m.harden_normals = True
    ob.data.shade_smooth() if hasattr(ob.data, "shade_smooth") else None


def text(name, body, center, size, material, extrude=0.015, bevel=0.006, rot=(math.radians(90), 0, 0), align="CENTER"):
    cu = bpy.data.curves.new(name, "FONT"); cu.body = body; cu.size = size; cu.extrude = extrude
    cu.bevel_depth = bevel; cu.align_x = align; cu.align_y = "CENTER"
    ob = bpy.data.objects.new(name, cu); bpy.context.collection.objects.link(ob)
    ob.location = center; ob.rotation_euler = rot
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.convert(target="MESH")
    ob = bpy.context.active_object; ob.data.materials.clear(); ob.data.materials.append(material)
    return ob


def bounds(root):
    pts = [o.matrix_world @ Vector(c) for o in [root] + list(root.children_recursive) if o.type == "MESH" for c in o.bound_box]
    return Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))), Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))


def model(aid, loc, rot_z=0.0, scale=1.0, name=None, top=None, bottom=None):
    """Import a Poly Haven glTF model under one empty and place it; top: put the model's highest point at this z."""
    path = os.path.join(PH, aid, f"{aid}_1k.gltf")
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    root = bpy.data.objects.new(name or aid, None); bpy.context.collection.objects.link(root)
    for o in new:
        if o.parent is None: o.parent = root
    root.location = loc; root.rotation_euler = (0, 0, rot_z); root.scale = (scale,) * 3
    if top is not None:
        bpy.context.view_layer.update(); lo, hi = bounds(root); root.location.z += top - hi.z
    if bottom is not None:
        bpy.context.view_layer.update(); lo, hi = bounds(root); root.location.z += bottom - lo.z
    return root


def mark(name, loc, rot_z_deg=0.0):
    """An empty Unity reads by name: MARK_* (where people sit or stand), CAM_* (camera spots), L_* (lights)."""
    e = bpy.data.objects.new(name, None); bpy.context.collection.objects.link(e)
    e.location = loc; e.rotation_euler = (0, 0, math.radians(rot_z_deg)); e.empty_display_size = 0.2
    return e


def export(set_id):
    out = os.path.join(ROOT, "Assets", "Dream", "FilmSets", set_id)   # generated, gitignored; film renders only
    os.makedirs(out, exist_ok=True)
    path = os.path.join(out, f"{set_id}.gltf")
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLTF_SEPARATE", export_texture_dir="tex", export_apply=True,
                              export_yup=True, export_lights=False, export_cameras=False, export_image_format="JPEG",
                              export_jpeg_quality=88)
    blend = os.path.join(r"C:\Dev\Beta1Assets\blend", f"{set_id}.blend"); os.makedirs(os.path.dirname(blend), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend)
    print("exported", path)
