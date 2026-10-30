# One realistic character from a spec (BETA1 §8): MPFB 2 body and face -> mixamo_unity rig (Unity Humanoid, so the
# Mixamo acting clips play) -> MakeHuman expression units as shape keys (blink, brows, smile, mouth shapes for
# lip-sync) -> eyes, brows, lashes, teeth, hair, clothes fitted and given the same shape keys -> FBX for Unity, plus
# manifest.json naming the textures each material needs. Output is CC0 (MPFB + MakeHuman system assets).
#   blender -b -P tools/blender/character.py -- tools/blender/chars/slick.json [--preview]
# The head is the body's own mesh for now; a scanned head replaces the face later without touching the rig (same
# topology targets, or a separate head mesh skinned to the same Head bone).
import json, os, re, shutil, sys, math
import bpy, bmesh

argv = sys.argv[sys.argv.index("--") + 1:]
SPEC = json.load(open(argv[0], encoding="utf-8"))
NAME = SPEC["name"]
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.environ.get("CHAR_OUT") or os.path.join(ROOT, "Assets", "Dream", "FilmCast", NAME)   # generated, gitignored (CHAR_OUT: build elsewhere, e.g. closet variants)
MPFB_SYS = os.path.join(os.environ["APPDATA"], "Blender Foundation", "Blender", "5.2", "extensions", "blender_org", "mpfb", "data")
MPFB_USER = os.path.join(os.environ["APPDATA"], "Blender Foundation", "Blender", "5.2", "extensions", ".user", "blender_org", "mpfb", "data")

from bl_ext.blender_org.mpfb.services.humanservice import HumanService
from bl_ext.blender_org.mpfb.services.targetservice import TargetService
from bl_ext.blender_org.mpfb.services.faceservice import FaceService

# the expression units used in Unity (shape key name in Unity = the unit name)
UNITS = ["eye-left-closure", "eye-right-closure", "eye-left-slit", "eye-right-slit", "eyebrows-left-down", "eyebrows-right-down",
         "eyebrows-left-inner-up", "eyebrows-right-inner-up", "eyebrows-left-up", "eyebrows-right-up", "mouth-open",
         "mouth-pursing", "mouth-retraction", "mouth-compression", "mouth-corner-puller", "mouth-depression",
         "mouth-protusion", "mouth-parling", "nose-left-elevation", "nose-right-elevation", "mouth-upward-retraction"]


def log(*a): print("[char]", *a, flush=True)


def mhmat_textures(path):
    d = {}
    for line in open(path, encoding="utf-8", errors="ignore"):
        p = line.split()
        if len(p) >= 2 and p[0] in ("diffuseTexture", "normalmapTexture", "transparencymapTexture", "specularmapTexture", "bumpmapTexture"):
            d[p[0]] = os.path.join(os.path.dirname(path), p[1])
    return d


def asset(kind, name):
    for base in (MPFB_USER, MPFB_SYS):
        p = os.path.join(base, kind, name, name + ".mhclo")
        if os.path.exists(p): return p
    raise FileNotFoundError(f"{kind}/{name}")


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    macro = TargetService.get_default_macro_info_dict()
    for k, v in SPEC["macro"].items():
        if k == "race": macro["race"].update(v)
        else: macro[k] = v
    base = HumanService.create_human(macro_detail_dict=macro)
    base.name = NAME
    for rel, w in SPEC.get("targets", {}).items():   # face and body shaping
        TargetService.load_target(base, os.path.join(MPFB_SYS, "targets", rel + ".target.gz"), weight=w)
    TargetService.bake_targets(base)
    log("shaped and baked")

    rig = HumanService.add_builtin_rig(base, "mixamo_unity")
    log("rig", rig.name, len(rig.data.bones), "bones")

    race = max(SPEC["macro"].get("race", {"caucasian": 1}).items(), key=lambda kv: kv[1])[0]
    for u in UNITS:
        TargetService.load_target(base, os.path.join(MPFB_SYS, "targets", "expression", "units", race, u + ".target.gz"), weight=0.0, name=u)
    log("expression units", len(UNITS))

    parts = {}
    def add(kind, name, atype):
        ob = HumanService.add_mhclo_asset(asset(kind, name), base, asset_type=atype, subdiv_levels=0, material_type="MAKESKIN")
        parts[ob.name] = (kind, name); return ob
    eyes = SPEC.get("eyes_mesh", "high-poly")
    add("eyes", eyes, "Eyes")
    add("eyebrows", SPEC.get("eyebrows", "eyebrow001"), "Eyebrows")
    add("eyelashes", SPEC.get("eyelashes", "eyelashes01"), "Eyelashes")
    add("teeth", "teeth_base", "Teeth")
    add("tongue", "tongue01", "Tongue")
    if SPEC.get("hair"): add("hair", SPEC["hair"], "Hair")
    for c in SPEC.get("clothes", []): add("clothes", c, "Clothes")
    FaceService.interpolate_targets(base)
    log("parts", list(parts.keys()))

    # skin
    skin = os.path.join(MPFB_USER, "skins", SPEC["skin"], SPEC["skin"] + ".mhmat")
    HumanService.set_character_skin(skin, base, skin_type="MAKESKIN")

    # the helper geometry and anything clothes hide: delete it for real (mask modifiers and shape keys don't mix in FBX)
    for ob in [base] + [o for o in bpy.data.objects if o.parent == rig and o != base]:
        if ob.type != "MESH": continue
        masks = [m for m in ob.modifiers if m.type == "MASK"]
        if not masks: continue
        bpy.context.view_layer.objects.active = ob
        for m in masks:
            g = ob.vertex_groups.get(m.vertex_group)
            hide = set()
            if g:
                gi = g.index
                for v in ob.data.vertices:
                    inside = any(e.group == gi and e.weight > 0.5 for e in v.groups)
                    if inside == m.invert_vertex_group: hide.add(v.index)
            ob.modifiers.remove(m)
            if hide:
                bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="DESELECT"); bpy.ops.object.mode_set(mode="OBJECT")
                for i in hide: ob.data.vertices[i].select = True
                bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.delete(type="VERT"); bpy.ops.object.mode_set(mode="OBJECT")
        for m in [m for m in ob.modifiers if m.type == "SUBSURF"]: ob.modifiers.remove(m)
    log("helpers removed; base verts", len(base.data.vertices))

    # materials: one per part, named for Unity, with the textures it needs listed in the manifest
    os.makedirs(os.path.join(OUT, "tex"), exist_ok=True)
    manifest = {"name": NAME, "materials": {}, "colors": SPEC.get("colors", {})}
    def material_for(ob, mname, mhmat):
        tex = mhmat_textures(mhmat) if mhmat and os.path.exists(mhmat) else {}
        entry = {}
        for k, p in tex.items():
            if os.path.exists(p):
                dst = f"{mname}_{k.replace('Texture', '')}{os.path.splitext(p)[1]}"
                shutil.copy(p, os.path.join(OUT, "tex", dst)); entry[k.replace("Texture", "")] = "tex/" + dst
        m = bpy.data.materials.new(mname); ob.data.materials.clear(); ob.data.materials.append(m)
        m.use_nodes = True; bsdf = m.node_tree.nodes["Principled BSDF"]; nt = m.node_tree
        col = SPEC.get("colors", {}).get(mname)
        if "diffuse" in entry:   # same look as Unity will build: diffuse (tinted), alpha from the texture
            im = nt.nodes.new("ShaderNodeTexImage"); im.image = bpy.data.images.load(os.path.join(OUT, entry["diffuse"]))
            if col:
                mx = nt.nodes.new("ShaderNodeMix"); mx.data_type = "RGBA"; mx.blend_type = "MULTIPLY"; mx.inputs["Factor"].default_value = 1
                mx.inputs[7].default_value = (*col, 1)   # same multiply Unity applies
                nt.links.new(im.outputs["Color"], mx.inputs[6]); nt.links.new(mx.outputs[2], bsdf.inputs["Base Color"])
            else:
                nt.links.new(im.outputs["Color"], bsdf.inputs["Base Color"])
            if mname in ("brows", "lashes", "hair", "eyes"):
                nt.links.new(im.outputs["Alpha"], bsdf.inputs["Alpha"]); m.surface_render_method = "DITHERED"
        elif col:
            bsdf.inputs["Base Color"].default_value = (*col, 1)
        bsdf.inputs["Roughness"].default_value = {"skin": 0.5, "eyes": 0.05, "teeth": 0.2, "hair": 0.45}.get(mname, 0.7)
        if mname == "skin": bsdf.inputs["Subsurface Weight"].default_value = 0.15; bsdf.inputs["Subsurface Radius"].default_value = (1.0, 0.35, 0.2)
        manifest["materials"][mname] = entry
    material_for(base, "skin", skin)
    if SPEC.get("beard"):
        paint_beard(base, os.path.join(OUT, manifest["materials"]["skin"]["diffuse"]), SPEC["beard"])
        bpy.data.images.load(os.path.join(OUT, manifest["materials"]["skin"]["diffuse"]), check_existing=True).reload()
    for ob in [o for o in bpy.data.objects if o.parent == rig and o != base and o.type == "MESH"]:
        kind, name = parts.get(ob.name, (None, None))
        if kind is None: continue
        mh = None
        for b in (MPFB_USER, MPFB_SYS):
            d = os.path.join(b, kind, name)
            if os.path.isdir(d):
                cands = [f for f in os.listdir(d) if f.endswith(".mhmat")]
                if kind == "eyes": cands = []
                if cands: mh = os.path.join(d, cands[0]); break
        mname = {"eyes": "eyes", "eyebrows": "brows", "eyelashes": "lashes", "teeth": "teeth", "tongue": "tongue", "hair": "hair"}.get(kind, "cloth_" + name)
        if kind == "eyes":   # the iris texture lives under eyes/materials
            mh = os.path.join(MPFB_USER, "eyes", "materials", SPEC.get("eye_color", "brown") + ".mhmat")
        material_for(ob, mname, mh)
        ob.name = mname
    for cname, rules in SPEC.get("paint", {}).items():   # garment colours by body region (paint_cloth)
        ob = bpy.data.objects.get(cname); e = manifest["materials"].get(cname, {})
        if ob is not None and "diffuse" in e:
            paint_cloth(ob, os.path.join(OUT, e["diffuse"]), rules)
            manifest["colors"][cname] = [1.0, 1.0, 1.0]   # the colours are in the texture now
    if SPEC.get("mask"): add_mask(base, SPEC["mask"], manifest)
    if SPEC.get("hair_cards") and bpy.data.objects.get("hair"):   # strands over the MakeHuman hair (tools/blender/hair.py)
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import hair
        hair.build(bpy.data.objects["hair"], rig, os.path.join(OUT, "tex"), SPEC["hair_cards"], log, SPEC.get("colors", {}).get("hair"))
        manifest["materials"]["haircards"] = {"diffuse": "tex/haircards_diffuse.png"}
        if "hair" in manifest["colors"]: manifest["colors"]["haircards"] = manifest["colors"]["hair"]
    if SPEC.get("glasses") and bpy.data.objects.get("eyes"):   # frames and lenses (tools/blender/glasses.py)
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import glasses
        g = SPEC["glasses"]; st = glasses.STYLES[g.get("style", "wire")]
        glasses.build(bpy.data.objects["eyes"], rig, g, log)
        manifest["materials"]["glasses"] = {"metal": st["metal"]}; manifest["materials"]["lenses"] = {"alpha": st["tint"][3]}
        manifest["colors"]["glasses"] = list(st["frame"]); manifest["colors"]["lenses"] = list(st["tint"][:3])
    json.dump(manifest, open(os.path.join(OUT, "manifest.json"), "w"), indent=1)
    return base, rig


def paint_cloth(ob, img_path, rules):
    """Repaint a garment's texture by where its polygons sit on the body (DAY2 A4: Lou's red tie, David's shirt and tie,
    Gnomey's striped jersey, the tee under Slick's jacket). Each rule: a colour, optional box (x: |x| range, z: height
    range, front: only polygons facing forward) and optional horizontal stripes (period in metres, second colour). The
    texture's own shading (folds, seams) is kept: the new colour multiplies its brightness."""
    import numpy as np
    from mathutils import Vector
    im = bpy.data.images.load(img_path); W, H = im.size
    px = np.array(im.pixels[:], dtype=np.float32).reshape(H, W, 4)
    lum = (px[..., :3] @ np.array([0.3, 0.59, 0.11], np.float32))
    shade = np.clip(lum / max(1e-3, float(np.percentile(lum, 80))), 0.3, 1.25)[..., None]
    me = ob.data; mw = ob.matrix_world; uv = me.uv_layers.active.data
    vs = [mw @ v.co for v in me.vertices]
    out = px.copy()
    for poly in me.polygons:
        c = sum((vs[i] for i in poly.vertices), Vector()) / len(poly.vertices)
        n = (mw.to_3x3() @ poly.normal).normalized()
        col = None
        for r in rules:
            if "x" in r and not (r["x"][0] <= abs(c.x) <= r["x"][1]): continue
            if "z" in r and not (r["z"][0] <= c.z <= r["z"][1]): continue
            if r.get("front") and n.y > -0.3: continue
            col = r["color"]
            if "stripes" in r and int(c.z / r["stripes"]) % 2: col = r["color2"]
            break
        if col is None: continue
        us = [uv[li].uv for li in poly.loop_indices]
        x0, x1 = int(min(u.x for u in us) * W), int(max(u.x for u in us) * W) + 1
        y0, y1 = int(min(u.y for u in us) * H), int(max(u.y for u in us) * H) + 1
        if x1 - x0 > W // 6: continue   # across a seam
        ys, xs = slice(max(0, y0), min(H, y1)), slice(max(0, x0), min(W, x1))
        out[ys, xs, :3] = np.array(col, np.float32) * shade[ys, xs]
    im.pixels[:] = out.ravel(); im.filepath_raw = img_path; im.file_format = "PNG"; im.save()
    log("cloth painted", ob.name, len(rules), "rules")


def paint_beard(base, img_path, b):
    """Facial hair painted into the skin texture through the face UVs: moustache, chin, jaw, cheeks, sideburns
    (b: density 0..1, color, reach 0..1 for how high on the cheeks; "stubble" is a low density)."""
    import numpy as np
    from mathutils import Vector
    im = bpy.data.images.load(img_path); W, H = im.size
    px = np.array(im.pixels[:], dtype=np.float32).reshape(H, W, 4)
    me = base.data; mw = base.matrix_world; uv = me.uv_layers.active.data
    vs = [mw @ v.co for v in me.vertices]
    head_z = max(v.z for v in vs) - 0.25
    nose = min((v for v in vs if abs(v.x) < 0.006 and v.z > head_z), key=lambda v: v.y)   # the face looks down -Y
    reach = b.get("reach", 0.5)
    def weight(c):
        x, dz, back = abs(c.x - nose.x), c.z - nose.z, c.y - nose.y
        if back > 0.13 or dz > 0.05 or dz < -0.17: return 0.0
        if x < 0.027 and -0.052 < dz < -0.028 and back < 0.03: return 0.0                    # lips
        if x < 0.036 and -0.03 <= dz <= -0.012: return 1.0                                     # moustache
        if dz < -0.05: return 1.0 if dz > -0.125 else max(0.0, 1 - (-0.125 - dz) / 0.04) * (1 if back < 0.09 else 0)  # chin, under-jaw
        top = -0.036 + (x - 0.03) * 0.75 * (0.5 + reach)                                        # mouth corner to earlobe
        if x >= 0.03 and dz < top: return 1.0
        if x > 0.064 and dz < 0.02 and 0.06 < back < 0.12: return 1.0                          # sideburns
        return 0.0
    mask = np.zeros((H, W), np.float32)
    for poly in me.polygons:
        c = sum((vs[i] for i in poly.vertices), Vector()) / len(poly.vertices)
        w = weight(c)
        if w <= 0: continue
        us = [uv[li].uv for li in poly.loop_indices]
        x0, x1 = int(min(u.x for u in us) * W), int(max(u.x for u in us) * W) + 1
        y0, y1 = int(min(u.y for u in us) * H), int(max(u.y for u in us) * H) + 1
        if x1 - x0 > W // 8: continue   # a polygon across a seam
        mask[max(0, y0):min(H, y1), max(0, x0):min(W, x1)] = np.maximum(mask[max(0, y0):min(H, y1), max(0, x0):min(W, x1)], w)
    k = max(2, W // 300)   # soften the edge (the UV stamps are coarse)
    for _ in range(9):
        mask = (np.roll(mask, k, 0) + np.roll(mask, -k, 0) + np.roll(mask, k, 1) + np.roll(mask, -k, 1) + mask) / 5
    rng = np.random.default_rng(7)
    hair = rng.random((H, W)).astype(np.float32)
    hair = (hair + np.roll(hair, 1, 0) + np.roll(hair, 2, 0)) / 3    # short vertical strokes
    a = np.clip(np.clip(mask * 1.6 - 0.25, 0, 1) * b.get("density", 0.8) * (0.8 + 0.35 * hair), 0, 1)[..., None]
    col = np.array(b.get("color", [0.05, 0.04, 0.035]) + [1.0], np.float32)
    px = px * (1 - a) + col * a
    im.pixels[:] = px.ravel(); im.filepath_raw = img_path; im.file_format = "PNG"; im.save()
    log("beard painted", int((mask > 0.5).sum()), "px")


def add_mask(base, spec, manifest):
    """Bio's fabric mask, nose to chin: a copy of that part of the face, pushed out 4 mm, skinned like the face, with
    our own busy illustrated print (generated here, so the design is ours)."""
    import numpy as np
    me = base.data.copy(); ob = base.copy(); ob.data = me; ob.name = "mask"; bpy.context.collection.objects.link(ob)
    ob.shape_key_clear()
    mw = ob.matrix_world; vs = [mw @ v.co for v in me.vertices]
    head_z = max(v.z for v in vs) - 0.25
    nose = min((v for v in vs if abs(v.x) < 0.006 and v.z > head_z), key=lambda v: v.y)
    keep = set()
    for i, v in enumerate(vs):
        x, dz, back = abs(v.x - nose.x), v.z - nose.z, v.y - nose.y
        if -0.115 < dz < 0.03 and back < 0.085 - max(0, x - 0.06) * 0.5 and x < 0.085: keep.add(i)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="DESELECT"); bpy.ops.object.mode_set(mode="OBJECT")
    for v in me.vertices: v.select = v.index not in keep
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.delete(type="VERT"); bpy.ops.object.mode_set(mode="OBJECT")
    for v in me.vertices: v.co += v.normal * 0.004
    rng = np.random.default_rng(spec.get("seed", 14)); N = 1024
    img = np.zeros((N, N, 4), np.float32); img[..., :3] = (0.1, 0.55, 0.6); img[..., 3] = 1
    yy, xx = np.mgrid[0:N, 0:N]
    pal = [(1, 0.31, 0.64), (0.18, 0.83, 0.78), (1, 0.85, 0.2), (0.45, 0.25, 0.75), (0.97, 0.95, 0.9), (0.08, 0.08, 0.12)]
    for _ in range(1400):
        cx, cy, r = rng.integers(0, N), rng.integers(0, N), rng.integers(3, 13)
        d = (xx - cx) ** 2 + ((yy - cy) * rng.uniform(0.5, 1.6)) ** 2
        img[d < r * r, :3] = pal[rng.integers(0, len(pal))]
        img[(d >= r * r) & (d < (r + 2) ** 2), :3] = (0.05, 0.05, 0.08)   # ink outline
    im = bpy.data.images.new("mask_print", N, N); im.pixels[:] = img.ravel()
    im.filepath_raw = os.path.join(OUT, "tex", "mask_diffuse.png"); im.file_format = "PNG"; im.save()
    m = bpy.data.materials.new("mask"); me.materials.clear(); me.materials.append(m)
    m.use_nodes = True; tn = m.node_tree.nodes.new("ShaderNodeTexImage"); tn.image = im
    m.node_tree.links.new(tn.outputs["Color"], m.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    manifest["materials"]["mask"] = {"diffuse": "tex/mask_diffuse.png"}
    log("mask", len(me.vertices), "verts")


def export(base, rig):
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    for o in rig.children: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, NAME + ".fbx"), use_selection=True, object_types={"ARMATURE", "MESH"},
                             add_leaf_bones=False, bake_anim=False, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             use_mesh_modifiers=True, mesh_smooth_type="FACE", use_armature_deform_only=True, path_mode="STRIP")
    blend = os.path.join(r"C:\Dev\Beta1Assets\chars", NAME + ".blend"); os.makedirs(os.path.dirname(blend), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend)
    log("exported", os.path.join(OUT, NAME + ".fbx"))


def preview():
    """EEVEE stills: face front, face three-quarter, full body, and a blink+smile test."""
    sc = bpy.context.scene; sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x, sc.render.resolution_y = 900, 900
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.05, 0.05, 0.06, 1)
    def light(n, loc, e, col):
        l = bpy.data.lights.new(n, "AREA"); l.energy = e; l.size = 1.5; l.color = col
        o = bpy.data.objects.new(n, l); sc.collection.objects.link(o); o.location = loc
        d = bpy.data.objects.new(n + "_t", None); sc.collection.objects.link(d); d.location = (0, 0, 1.6)
        c = o.constraints.new("TRACK_TO"); c.target = d
    light("key", (1.2, -1.6, 2.0), 120, (1, 0.85, 0.7)); light("fill", (-1.5, -1.2, 1.6), 30, (0.7, 0.8, 1)); light("rim", (0, 1.5, 2.2), 80, (1, 1, 1))
    head = rig.pose.bones.get("mixamorig:Head") or rig.pose.bones.get("Head")
    hz = (rig.matrix_world @ head.head).z if head else 1.6
    cam = bpy.data.cameras.new("c"); co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
    tgt = bpy.data.objects.new("ct", None); sc.collection.objects.link(tgt); tc = co.constraints.new("TRACK_TO"); tc.target = tgt
    out = os.path.join(r"C:\Dev\Beta1Assets\preview", NAME)
    os.makedirs(out, exist_ok=True)
    shots = [("face", (0, -0.75, hz + 0.08), 85, (0, 0, hz + 0.08)), ("face34", (0.5, -0.6, hz + 0.1), 85, (0, 0, hz + 0.08)),
             ("body", (0, -4.0, 1.0), 50, (0, 0, 0.95))]
    for nm, loc, mm, look in shots:
        co.location = loc; cam.lens = mm; tgt.location = look
        sc.render.filepath = os.path.join(out, nm + ".png"); bpy.ops.render.render(write_still=True)
    kb = base.data.shape_keys.key_blocks
    for k in ("eye-left-closure", "eye-right-closure"): kb[k].value = 1.0
    for k in ("mouth-corner-puller",): kb[k].value = 0.8
    co.location, cam.lens, tgt.location = shots[0][1], 85, shots[0][3]
    sc.render.filepath = os.path.join(out, "blink_smile.png"); bpy.ops.render.render(write_still=True)


base, rig = build()
if "--preview" in argv:
    preview()
else:
    export(base, rig)
