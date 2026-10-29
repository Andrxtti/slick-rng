# The Lucky Seven Diner, 2:13 AM (BETA1 task 3). blender -b -P tools/blender/diner.py [-- --preview]
# Room: x -6..6 (left..right as you face the counter from the window), y 0 (window wall) .. 7.5 (back wall), z up.
# The counter runs along x at y 4.4..5.1; you sit at x 0, Slick at x -0.85, both facing +y. Outside is y < 0.
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from kit import reset, mat, box, cyl, torus, text, model, mark, export, box_uv

reset()
R = math.radians

# ---- materials (ART_DIRECTION §1: tungsten inside, red neon accent, cool wet street outside)
floor = mat("M_Floor", "floor_tiles_06", rough=0.35, tint=(0.95, 0.92, 0.88))
wall = mat("M_Wall", "beige_wall_001", tint=(1.0, 0.93, 0.80))
ceiling = mat("M_Ceiling", "beige_wall_001", tint=(0.85, 0.80, 0.72))
steel = mat("M_Steel", color=(0.80, 0.80, 0.82), rough=0.28, metal=1.0)
chrome = mat("M_Chrome", color=(0.92, 0.92, 0.94), rough=0.06, metal=1.0)
vinyl = mat("M_Vinyl", "leather_white", tint=(0.62, 0.04, 0.05), rough=0.35)
top = mat("M_CounterTop", "marble_01", tint=(1.0, 0.97, 0.92), rough=0.25)
table_top = mat("M_TableTop", color=(0.55, 0.06, 0.07), rough=0.2)   # red Formica
kick = mat("M_Kick", color=(0.05, 0.045, 0.04), rough=0.6)
glass = mat("M_Glass", color=(0.75, 0.85, 0.9), rough=0.02, alpha=0.12)
brick = mat("M_Brick", "brick_wall_04", tint=(0.7, 0.65, 0.65))
pave = mat("M_Sidewalk", "concrete_pavement_03", rough_scale=0.45, tint=(0.6, 0.6, 0.65))
street = mat("M_Street", "asphalt_02", rough_scale=0.25, tint=(0.45, 0.45, 0.5))   # wet
neon_red = mat("M_NeonRed", color=(1, 0.1, 0.12), emit=(1.0, 0.06, 0.08), emit_strength=18.0)
neon_aqua = mat("M_NeonAqua", color=(0.2, 0.9, 0.85), emit=(0.18, 0.95, 0.85), emit_strength=10.0)
case_light = mat("M_CaseLight", color=(1, 0.9, 0.7), emit=(1.0, 0.82, 0.6), emit_strength=6.0)
ceramic = mat("M_Ceramic", color=(0.93, 0.92, 0.88), rough=0.12)
coffee = mat("M_Coffee", color=(0.06, 0.03, 0.015), rough=0.05)
crust = mat("M_Crust", color=(0.72, 0.45, 0.2), rough=0.6)
filling = mat("M_Filling", color=(0.45, 0.06, 0.1), rough=0.3)   # cherry
envelope = mat("M_Envelope", color=(0.55, 0.03, 0.04), rough=0.55)
brass = mat("M_Brass", color=(0.85, 0.62, 0.3), rough=0.2, metal=1.0)
board = mat("M_Board", color=(0.03, 0.03, 0.035), rough=0.7)
menu_letters = mat("M_MenuLetters", color=(0.9, 0.88, 0.8), emit=(0.9, 0.85, 0.75), emit_strength=0.6)
ketchup = mat("M_Ketchup", color=(0.5, 0.02, 0.02), rough=0.15)

# ---- shell
box("Floor", (0, 3.75, -0.05), (12, 7.5, 0.1), floor, uv=1.2)
box("Ceiling", (0, 3.75, 3.25), (12, 7.5, 0.1), ceiling, uv=2.0)
box("WallBack", (0, 7.6, 1.6), (12, 0.2, 3.2), wall, uv=2.0)
box("WainscotBack", (0, 7.48, 0.55), (12, 0.04, 1.1), steel, uv=0)
for s in (-1, 1):
    box(f"WallSide{s}", (6.1 * s, 3.75, 1.6), (0.2, 7.9, 3.2), wall, uv=2.0)
    box(f"WainscotSide{s}", (5.98 * s, 3.75, 0.55), (0.04, 7.5, 1.1), steel, uv=0)
box("ChromeRail", (0, 7.45, 1.12), (12, 0.06, 0.04), chrome, uv=0, bevel=0.01)

# front wall: sill, header, window glass with chrome mullions, door on the right
box("Sill", (0, 0, 0.45), (12.2, 0.25, 0.9), steel, uv=0, bevel=0.01)
box("SillTop", (0, 0.05, 0.92), (12.2, 0.35, 0.04), chrome, uv=0, bevel=0.01)
box("Header", (0, 0, 2.95), (12.2, 0.25, 0.5), wall, uv=2.0)
box("PostR", (5.65, 0, 1.6), (0.9, 0.25, 3.2), wall, uv=2.0)
box("PostDoorL", (4.1, 0, 1.6), (0.2, 0.25, 3.2), wall, uv=2.0)
box("DoorHeader", (4.85, 0, 2.85), (1.5, 0.25, 0.7), wall, uv=2.0)
box("Window", (-0.95, 0.0, 1.8), (10.1, 0.02, 1.8), glass, uv=0)
for i in range(6):
    x = -6.0 + i * 2.02
    box(f"Mullion{i}", (x, 0, 1.8), (0.06, 0.1, 1.8), chrome, uv=0, bevel=0.01)
box("WindowTop", (-0.95, 0, 2.71), (10.1, 0.12, 0.05), chrome, uv=0, bevel=0.01)
# the door (glass in a chrome frame) and the bell above it
box("Door", (4.85, 0.02, 1.2), (1.1, 0.03, 2.3), glass, uv=0)
for dx in (-0.55, 0.55): box(f"DoorFrame{dx}", (4.85 + dx, 0.02, 1.2), (0.05, 0.06, 2.4), chrome, uv=0)
box("DoorFrameTop", (4.85, 0.02, 2.38), (1.15, 0.06, 0.05), chrome, uv=0)
box("DoorPush", (4.5, 0.08, 1.1), (0.04, 0.04, 0.5), chrome, uv=0, bevel=0.01)
cyl("BellArm", (4.85, 0.25, 2.47), 0.008, 0.3, brass, rot=(R(90), 0, 0), uv=0)
bell = cyl("Bell", (4.85, 0.38, 2.38), 0.06, 0.1, brass, uv=0)
bell.modifiers.new("taper", "SIMPLE_DEFORM").deform_method = "TAPER"

# outside: brick facade, sidewalk, wet street, the LUCKY SEVEN sign
box("FacadeTop", (0, -0.2, 3.6), (14, 0.15, 1.6), brick, uv=1.5)
box("FacadeLow", (-0.5, -0.2, 0.45), (11.4, 0.15, 0.9), brick, uv=1.5)
box("Sidewalk", (0, -2.0, -0.05), (30, 3.6, 0.1), pave, uv=2.0)
box("Curb", (0, -3.85, -0.1), (30, 0.2, 0.2), pave, uv=1.0)
box("Street", (0, -11, -0.2), (40, 14, 0.1), street, uv=4.0)
sign_back = box("SignBack", (-1, -0.35, 3.62), (6.2, 0.08, 1.0), board, uv=0, bevel=0.02)
text("SignLucky", "LUCKY SEVEN", (-1, -0.42, 3.75), 0.62, neon_red)
text("SignDiner", "DINER  ·  OPEN 24 HRS", (-1, -0.42, 3.3), 0.22, neon_aqua)
# a neon "7" hanging in the window, facing the street (reads backwards from inside, as it should)
text("WindowSeven", "7", (-3.2, 0.12, 2.1), 0.55, neon_red, extrude=0.01, bevel=0.012)
text("WindowOpen", "OPEN", (2.6, 0.12, 2.25), 0.26, neon_aqua, extrude=0.008, bevel=0.01)
model("street_lamp_01", (-3.5, -3.4, 0), rot_z=R(90))
model("fire_hydrant", (2.2, -3.3, 0), scale=1.0)

# ---- booths along the window (x centres), table between two benches
for i, bx in enumerate((-4.6, -2.6, -0.6)):
    box(f"BoothTable{i}", (bx, 0.75, 0.74), (0.75, 1.0, 0.04), table_top, uv=0, bevel=0.012)
    box(f"BoothTableEdge{i}", (bx, 0.75, 0.72), (0.77, 1.02, 0.025), chrome, uv=0, bevel=0.008)
    cyl(f"BoothPed{i}", (bx, 0.75, 0.36), 0.04, 0.72, chrome, uv=0)
    for s in (-1, 1):
        sx = bx + s * 0.62
        box(f"BoothSeat{i}{s}", (sx, 0.85, 0.42), (0.5, 1.2, 0.16), vinyl, uv=0.5, bevel=0.04, segments=3)
        box(f"BoothBack{i}{s}", (sx + s * 0.22, 0.85, 0.85), (0.14, 1.2, 0.75), vinyl, uv=0.5, bevel=0.05, segments=3)
        box(f"BoothBase{i}{s}", (sx, 0.85, 0.17), (0.5, 1.2, 0.34), kick, uv=0, bevel=0.01)
    cup = cyl(f"BoothSugar{i}", (bx + 0.2, 0.45, 0.82), 0.035, 0.11, glass, uv=0)
    cyl(f"BoothKetchup{i}", (bx + 0.12, 0.42, 0.85), 0.03, 0.18, ketchup, uv=0)
    box(f"BoothNapkins{i}", (bx - 0.15, 0.42, 0.82), (0.12, 0.08, 0.12), chrome, uv=0, bevel=0.01)

# ---- the counter: steel front, chrome edge, stone top
CX0, CX1, CY0, CY1, CH = -4.8, 2.6, 4.4, 5.1, 1.05
cx, cw = (CX0 + CX1) / 2, CX1 - CX0
box("CounterBody", (cx, (CY0 + CY1) / 2, CH / 2 - 0.02), (cw, CY1 - CY0, CH - 0.04), steel, uv=0, bevel=0.01)
box("CounterKick", (cx, CY0 + 0.1, 0.06), (cw, 0.2, 0.12), kick, uv=0)
box("CounterTop", (cx, (CY0 + CY1) / 2 - 0.08, CH + 0.005), (cw + 0.1, CY1 - CY0 + 0.25, 0.04), top, uv=1.0, bevel=0.006)
box("CounterEdge", (cx, CY0 - 0.205, CH - 0.005), (cw + 0.12, 0.03, 0.06), chrome, uv=0, bevel=0.01)
for k in range(3):   # ribbed chrome bands on the counter front
    box(f"CounterRib{k}", (cx, CY0 - 0.012, 0.3 + k * 0.22), (cw, 0.025, 0.035), chrome, uv=0, bevel=0.008)

# stools: chrome pedestal, red vinyl seat with a chrome ring
STOOLS = [CX0 + 0.55 + i * 0.85 for i in range(8)]
for i, sx in enumerate(STOOLS):
    y = CY0 - 0.55
    cyl(f"StoolBase{i}", (sx, y, 0.02), 0.2, 0.04, chrome, bevel=0.01, uv=0)
    cyl(f"StoolPost{i}", (sx, y, 0.38), 0.035, 0.7, chrome, uv=0)
    cyl(f"StoolRing{i}", (sx, y, 0.715), 0.2, 0.05, chrome, bevel=0.01, uv=0)
    cyl(f"StoolSeat{i}", (sx, y, 0.77), 0.19, 0.09, vinyl, bevel=0.035, uv=0.3)
    torus(f"StoolFoot{i}", (sx, y, 0.28), 0.17, 0.012, chrome)
YOU_X, SLICK_X = STOOLS[5], STOOLS[4]

# on the counter: coffee for two, the pie in front of you, the red envelope (moved by the film), the pie case
for who, x in (("You", YOU_X), ("Slick", SLICK_X)):
    cyl(f"Saucer{who}", (x + 0.18, CY0 + 0.05, CH + 0.03), 0.075, 0.012, ceramic, uv=0)
    cyl(f"Mug{who}", (x + 0.18, CY0 + 0.05, CH + 0.085), 0.042, 0.1, ceramic, uv=0)
    cyl(f"Coffee{who}", (x + 0.18, CY0 + 0.05, CH + 0.128), 0.038, 0.004, coffee, uv=0)
    torus(f"MugHandle{who}", (x + 0.23, CY0 + 0.05, CH + 0.09), 0.025, 0.007, ceramic, rot=(R(90), 0, 0))
cyl("PiePlate", (YOU_X - 0.05, CY0 + 0.08, CH + 0.03), 0.11, 0.012, ceramic, uv=0)
slice_ = cyl("PieSlice", (YOU_X - 0.05, CY0 + 0.08, CH + 0.06), 0.09, 0.045, filling, verts=3, uv=0)
slice_.data.materials.append(crust)
box("Envelope", (SLICK_X + 0.05, CY0 + 0.12, CH + 0.03), (0.22, 0.12, 0.004), envelope, uv=0).name = "Prop_Envelope"
# pie case: chrome base, glass box, a warm light strip, two cakes and a pie on stands
PCX = STOOLS[2] + 0.2
box("CaseBase", (PCX, 4.75, CH + 0.05), (0.9, 0.5, 0.06), chrome, uv=0, bevel=0.01)
box("CaseGlass", (PCX, 4.75, CH + 0.33), (0.88, 0.48, 0.5), glass, uv=0)
box("CaseLid", (PCX, 4.75, CH + 0.6), (0.92, 0.52, 0.04), chrome, uv=0, bevel=0.01)
box("CaseLight", (PCX, 4.75, CH + 0.575), (0.8, 0.06, 0.012), case_light, uv=0)
box("CaseShelf", (PCX, 4.75, CH + 0.3), (0.84, 0.44, 0.01), glass, uv=0)
model("carrot_cake", (PCX - 0.2, 4.75, CH + 0.08), scale=0.85)
model("strawberry_chocolate_cake", (PCX + 0.2, 4.75, CH + 0.08), scale=0.85)
cyl("CasePie", (PCX, 4.75, CH + 0.33), 0.13, 0.04, crust, uv=0)
model("CashRegister_01", (CX1 - 0.4, 4.8, CH + 0.02), rot_z=R(180))
for i, x in enumerate((-3.6, -0.6, 1.8)):
    box(f"Napkins{i}", (x, CY0 + 0.15, CH + 0.08), (0.12, 0.08, 0.13), chrome, uv=0, bevel=0.01)
    cyl(f"Sugar{i}", (x + 0.15, CY0 + 0.15, CH + 0.08), 0.035, 0.12, glass, uv=0)
    cyl(f"Ketchup{i}", (x - 0.15, CY0 + 0.15, CH + 0.1), 0.03, 0.18, ketchup, uv=0)

# ---- back bar: low steel counter, coffee machine, menu board, neon script, pass-through to the kitchen
box("BackCounter", (-0.5, 7.15, 0.47), (10, 0.6, 0.94), steel, uv=0, bevel=0.01)
box("BackCounterTop", (-0.5, 7.12, 0.96), (10, 0.66, 0.04), top, uv=1.0, bevel=0.005)
for k in range(2):
    box(f"CoffeeMachine{k}", (-3.4 + k * 0.7, 7.2, 1.25), (0.5, 0.4, 0.55), steel, uv=0, bevel=0.02)
    cyl(f"CoffeePot{k}", (-3.4 + k * 0.7, 7.0, 1.06), 0.07, 0.16, glass, uv=0)
for k in range(6):
    cyl(f"ShelfMug{k}", (0.8 + k * 0.18, 7.3, 1.67), 0.042, 0.1, ceramic, uv=0)
box("Shelf", (1.25, 7.35, 1.6), (1.4, 0.25, 0.03), chrome, uv=0, bevel=0.005)
box("MenuBoard", (-1.5, 7.48, 2.35), (3.2, 0.05, 0.9), board, uv=0, bevel=0.01)
for k, line in enumerate(("PIE  ·  ANY KIND  $4", "COFFEE  ·  BOTTOMLESS  $2", "EGGS ANY STYLE  $6")):
    text(f"Menu{k}", line, (-1.5, 7.44, 2.6 - k * 0.24), 0.13, menu_letters, extrude=0.002, bevel=0, rot=(R(90), 0, 0))
text("NeonBack", "Lucky 7", (2.9, 7.42, 2.3), 0.5, neon_red, rot=(R(90), 0, 0))
box("PassThrough", (3.9, 7.5, 1.5), (1.4, 0.25, 0.6), kick, uv=0)
box("PassShelf", (3.9, 7.38, 1.2), (1.5, 0.35, 0.04), steel, uv=0, bevel=0.005)

# ---- pendant lights over the counter, ceiling lights over the booths
for i, x in enumerate((-4.0, -2.3, -0.6, 1.1)):
    lamp = model("hanging_industrial_lamp", (x, 4.6, 0), name=f"Pendant{i}", bottom=2.15)
    bpy.context.view_layer.update(); lo, hi = __import__("kit").bounds(lamp); print("pendant bottom", round(lo.z, 2))
    mark(f"L_pendant_{i}", (x, 4.6, 2.0))
for i, x in enumerate((-4.6, -2.6, -0.6)):
    mark(f"L_booth_{i}", (x, 1.2, 3.1))
mark("L_case", (PCX, 4.75, CH + 0.55))
mark("L_neon_out", (-1, -1.2, 3.5))
mark("L_streetlamp", (-3.5, -3.4, 4.6))
mark("L_back", (0, 7.0, 2.8))

# ---- people and cameras (Unity reads these; MARK z = seat height)
mark("MARK_you", (YOU_X, CY0 - 0.55, 0.82), 0)
mark("MARK_slick", (SLICK_X, CY0 - 0.55, 0.82), 0)
mark("MARK_door", (4.85, 0.6, 0), 180)
mark("CAM_ext", (-0.6, -9.0, 1.6), 0)             # across the street, looking in
mark("CAM_wide", (-4.6, 0.9, 1.6), 0)             # from the far booth, along the counter

if "--preview" in sys.argv:
    import bpy
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"; sc.render.resolution_x, sc.render.resolution_y = 1280, 720
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.02, 0.025, 0.05, 1)
    for i, x in enumerate((-4.0, -2.3, -0.6, 1.1)):
        l = bpy.data.lights.new(f"p{i}", "POINT"); l.energy = 120; l.color = (1, 0.78, 0.55)
        o = bpy.data.objects.new(f"p{i}", l); sc.collection.objects.link(o); o.location = (x, 4.6, 1.9)
    l = bpy.data.lights.new("neon", "AREA"); l.energy = 300; l.color = (1, 0.1, 0.1); l.size = 4
    o = bpy.data.objects.new("neon", l); sc.collection.objects.link(o); o.location = (-1, -1.0, 3.4); o.rotation_euler = (R(-60), 0, 0)
    cam = bpy.data.cameras.new("c"); cam.lens = 35; co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
    views = {"two": ((-0.8, 6.6, 1.5), (R(90), 0, R(180))), "wide": ((-5.5, 0.6, 1.7), (R(88), 0, R(-70))), "ext": ((-0.6, -9, 1.7), (R(88), 0, 0))}
    for nm, (loc, rot) in views.items():
        co.location = loc; co.rotation_euler = rot
        sc.render.filepath = os.path.join(r"C:\Dev\Beta1Assets\preview", f"diner_{nm}.png"); bpy.ops.render.render(write_still=True)
else:
    export("diner")
    import shutil; shutil.copy(os.path.join(r"C:\Dev\Beta1Assets\polyhaven", "modern_evening_street", "modern_evening_street_2k.hdr"),
                               os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "Assets", "Dream", "FilmSets", "diner", "sky.hdr"))
