# Track H, test H1 (INBOX 20:15): can Blender beat today's Unity film frame? Slick's diner ECU rendered in Eevee and in
# Cycles (64 samples + OpenImageDenoise), each timed, for C:\Dev\Builds\day2\real_slick_ecu.png.
#   blender -b -P tools/blender/h1_test.py -- [eevee|cycles|both]
# The diner is built by diner.py (--h1: no export); Slick is his exported FBX (FilmCast/slick) standing at his stool mark
# (an ECU only sees the head; a seated pose is the next step). Skin: Principled BSDF with random-walk subsurface, the
# MakeHuman skin texture, roughness varied by a noise; eyes get a glossy coat. Light as the film does it: warm pendants,
# the red neon outside at the reduced level, a soft key, a cool fill and a cool rim. AgX, real depth of field.
import math, os, sys, time
import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = r"C:\Dev\Beta1Assets\h1"; os.makedirs(OUT, exist_ok=True)
mode = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "both"   # eevee | cycles | both [--strands]
R = math.radians

sys.argv += ["--h1"]
sys.path.insert(0, HERE)
exec(compile(open(os.path.join(HERE, "diner.py"), encoding="utf-8").read(), "diner.py", "exec"))
sc = bpy.context.scene

# Slick at his mark, facing the counter (+y)
CHAR = os.environ.get("CHAR", "slick")   # H2: any cast member at Slick's stool mark (FilmCast/<char>)
HAIR = {"slick": ("back", 0.9, 0.25), "mustich": ("curls", 0.18, 1.0)}.get(CHAR, ("back", 0.6, 0.4))   # style, melanin, redness
mk = bpy.data.objects["MARK_slick"]
before = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "Assets", "Dream", "FilmCast", CHAR, CHAR + ".fbx"))
new = [o for o in bpy.data.objects if o not in before]
root = next(o for o in new if o.parent is None)
root.location = (mk.location.x, mk.location.y - 0.1, 0); root.rotation_euler = (root.rotation_euler.x, 0, R(180))
bpy.context.view_layer.update()
meshes = [o for o in new if o.type == "MESH"]
# H2: a relaxed pose for the stills (the import is in an A-pose): each upper arm turns down about the body's front axis,
# in world space, so the bones' own axes don't matter. ARM_DEG flips the sign if a test shows it going the wrong way.
from mathutils import Matrix
rig = next((o for o in new if o.type == "ARMATURE"), None)
if rig is not None:
    deg = float(os.environ.get("ARM_DEG", "-40")); front_w = Vector((0, 1, 0))   # Slick faces +y in this scene
    for side, sgn in (("Left", 1), ("Right", -1)):
        pb = rig.pose.bones.get(f"mixamorig:{side}Arm")
        if pb is None: continue
        bpy.context.view_layer.update()
        head_w = rig.matrix_world @ pb.head
        rot = Matrix.Translation(head_w) @ Matrix.Rotation(math.radians(deg * sgn), 4, front_w) @ Matrix.Translation(-head_w)
        pb.matrix = rig.matrix_world.inverted() @ rot @ rig.matrix_world @ pb.matrix
    bpy.context.view_layer.update()
top = max((o.matrix_world @ Vector(c)).z for o in meshes for c in o.bound_box)
eye = Vector((mk.location.x, mk.location.y - 0.1 + 0.09, top - 0.2))   # the eyes: 20 cm under the top of the hair, 9 cm forward
import json
COLORS = json.load(open(os.path.join(ROOT, "Assets", "Dream", "FilmCast", CHAR, "manifest.json"), encoding="utf-8"))["colors"]

TEX = os.path.join(ROOT, "Assets", "Dream", "FilmCast", CHAR, "tex")
def img(name):
    p = os.path.join(TEX, name); return bpy.data.images.load(p, check_existing=True) if os.path.exists(p) else None
MAN = json.load(open(os.path.join(ROOT, "Assets", "Dream", "FilmCast", CHAR, "manifest.json"), encoding="utf-8"))
def rebuild(m, part):
    """the material as Unity's Materials.cs builds it from the manifest: diffuse x colour, normal map, alpha for strands"""
    spec = MAN["materials"].get(part, {}); col = MAN["colors"].get(part)
    m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial"); p = nt.nodes.new("ShaderNodeBsdfPrincipled"); nt.links.new(p.outputs[0], out.inputs[0])
    if spec.get("diffuse"):
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(os.path.join(ROOT, "Assets", "Dream", "FilmCast", CHAR, spec["diffuse"]), check_existing=True)
        c = t.outputs["Color"]
        if col:
            mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"; mix.blend_type = "MULTIPLY"; mix.inputs["Factor"].default_value = 1.0
            nt.links.new(c, mix.inputs["A"]); mix.inputs["B"].default_value = (*col[:3], 1.0); c = mix.outputs["Result"]
        nt.links.new(c, p.inputs["Base Color"])
        if part in ("hair", "haircards", "brows", "lashes"):
            nt.links.new(t.outputs["Alpha"], p.inputs["Alpha"]); m.blend_method = "HASHED" if hasattr(m, "blend_method") else None
    elif col: p.inputs["Base Color"].default_value = (*col[:3], 1.0)
    if spec.get("normalmap"):
        tn = nt.nodes.new("ShaderNodeTexImage"); tn.image = bpy.data.images.load(os.path.join(ROOT, "Assets", "Dream", "FilmCast", CHAR, spec["normalmap"]), check_existing=True)
        tn.image.colorspace_settings.name = "Non-Color"; nm = nt.nodes.new("ShaderNodeNormalMap"); nt.links.new(tn.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], p.inputs["Normal"])
    p.inputs["Roughness"].default_value = {"eyes": 0.05, "teeth": 0.3, "hair": 0.45, "haircards": 0.42}.get(part, 0.7)
    if "metal" in spec: p.inputs["Metallic"].default_value = spec["metal"]
    if part == "lenses":   # real glass, not an alpha card (Eevee drew the alpha as opaque white)
        p.inputs["Transmission Weight"].default_value = 1.0; p.inputs["IOR"].default_value = 1.5; p.inputs["Roughness"].default_value = 0.02
        p.inputs["Base Color"].default_value = (0.95, 0.97, 1.0, 1)
    if part in ("hair", "haircards", "brows", "lashes", "lenses"):
        try: m.surface_render_method = "DITHERED"
        except Exception: pass
    if part == "eyes": p.inputs["Coat Weight"].default_value = 1.0; p.inputs["Coat Roughness"].default_value = 0.02
    if part == "skin":   # the H1 skin: random-walk subsurface, roughness varied, fine pore bump
        p.subsurface_method = "RANDOM_WALK"; p.inputs["Subsurface Weight"].default_value = 1.0
        p.inputs["Subsurface Radius"].default_value = (1.0, 0.35, 0.2); p.inputs["Subsurface Scale"].default_value = 0.012
        nz = nt.nodes.new("ShaderNodeTexNoise"); nz.inputs["Scale"].default_value = 180
        mr = nt.nodes.new("ShaderNodeMapRange"); mr.inputs["To Min"].default_value = 0.38; mr.inputs["To Max"].default_value = 0.55
        nt.links.new(nz.outputs["Fac"], mr.inputs["Value"]); nt.links.new(mr.outputs["Result"], p.inputs["Roughness"])
        bump = nt.nodes.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = 0.08; bump.inputs["Distance"].default_value = 0.0005
        pores = nt.nodes.new("ShaderNodeTexVoronoi"); pores.inputs["Scale"].default_value = 900
        nt.links.new(pores.outputs["Distance"], bump.inputs["Height"]); nt.links.new(bump.outputs["Normal"], p.inputs["Normal"])
done = set()
for o in meshes:
    for slot in o.material_slots:
        m = slot.material
        if m is None or m.name in done: continue
        part = next((k for k in sorted(MAN["materials"], key=len, reverse=True) if k in m.name.lower()), None)
        if part: rebuild(m, part); done.add(m.name)
print("[h1] materials:", sorted(done), flush=True)

# H2: real strands instead of the cards ("--strands" after the engine)
if "--strands" in sys.argv:
    import strands
    shell = next(o for o in meshes if o.data.materials and any(m and m.name.lower().startswith("hair") and "cards" not in m.name.lower() for m in o.data.materials))
    for o in meshes:
        if o.data.materials and any(m and "haircards" in m.name.lower() for m in o.data.materials): o.hide_render = True
    strands.grow(shell, HAIR[0], melanin=HAIR[1], redness=HAIR[2], count=int(os.environ.get("STRANDS", "60000")))
    OUT_TAG = "_strands"
else:
    OUT_TAG = ""

# the film's light, Blender units (W)
for o in list(bpy.data.objects):
    if o.type == "LIGHT": bpy.data.objects.remove(o)
def light(name, kind, loc, energy, color, size=0.2, aim=None):
    l = bpy.data.lights.new(name, kind); l.energy = energy; l.color = color
    if kind == "AREA": l.size = size
    else: l.shadow_soft_size = size
    o = bpy.data.objects.new(name, l); sc.collection.objects.link(o); o.location = loc
    if aim is not None: o.rotation_euler = (Vector(aim) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    return o
for o in [o for o in bpy.data.objects if o.name.startswith("L_pendant")]:
    light(o.name + "_l", "POINT", o.location, 60, (1.0, 0.75, 0.5), 0.08)
light("Neon", "AREA", bpy.data.objects["L_neon_out"].location, 120, (1.0, 0.1, 0.12), 3.0, aim=eye)
front = Vector((0, 1, 0))   # Slick faces +y, toward the counter and the camera
light("Key", "AREA", eye + Vector((0.5, 1.0, 0.35)), 20, (1.0, 0.82, 0.66), 0.8, aim=eye)   # H2: more frontal (the ear blew out)
light("Fill", "AREA", eye + Vector((-0.9, 0.8, 0.0)), 7, (0.6, 0.7, 1.0), 0.8, aim=eye)
light("Rim", "AREA", eye + Vector((-0.4, -0.7, 0.3)), 18, (0.65, 0.75, 1.0), 0.3, aim=eye)
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.01, 0.012, 0.02, 1)

cam = bpy.data.cameras.new("ecu"); cam.lens = 85; cam.dof.use_dof = True; cam.dof.aperture_fstop = 2.0
co = bpy.data.objects.new("ecu", cam); sc.collection.objects.link(co); sc.camera = co
mcu = "--mcu" in sys.argv   # H2: a medium close-up, the whole head and the shoulders
co.location = eye + (Vector((0.3, 1.9, 0.0)) if mcu else Vector((0.18, 0.95, 0.03)))
aimpt = eye + Vector((0, 0, -0.06)) if mcu else eye
co.rotation_euler = (aimpt - co.location).to_track_quat("-Z", "Y").to_euler()
if mcu: cam.lens = 65
# the eye light: a dim soft source beside the lens, for catchlights (the lids shade Slick's eyes)
light("EyeLight", "AREA", co.location + Vector((0.08, 0, 0.05)), 1.5, (1.0, 0.95, 0.9), 0.25, aim=eye)
focus = bpy.data.objects.new("focus", None); sc.collection.objects.link(focus); focus.location = eye; cam.dof.focus_object = focus; cam.dof.aperture_fstop = 2.8
sc.render.resolution_x, sc.render.resolution_y = 1280, 720
sc.view_settings.view_transform = "AgX"; sc.view_settings.look = "AgX - Medium High Contrast"

for eng in (["eevee", "cycles"] if mode == "both" else [mode]):
    if eng == "eevee":
        sc.render.engine = "BLENDER_EEVEE"
        try: sc.eevee.use_raytracing = True
        except Exception: pass
    else:
        sc.render.engine = "CYCLES"; sc.cycles.samples = 64; sc.cycles.use_denoising = True; sc.cycles.denoiser = "OPENIMAGEDENOISE"
    sc.render.filepath = os.path.join(OUT, f"{CHAR}_{'mcu' if mcu else 'ecu'}_{eng}{OUT_TAG}.png")
    t0 = time.time(); bpy.ops.render.render(write_still=True)
    print(f"[h1] {eng}: {time.time() - t0:.1f} s per frame -> {sc.render.filepath}", flush=True)
