# Your apartment, Monday night (DAY2 priority 1: p_offer, p_quit, p_keep). blender -b -P tools/blender/home.py [-- --preview]
# A small Miami studio: x -3.2..3.2, y 0 (the window wall, blinds, the city at night) .. 5.5 (kitchenette), ceiling 2.7.
# You stand at the counter facing +y, the blinds behind you; the phone lies on the counter in front of you.
import math, os, sys, shutil
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from kit import reset, mat, box, cyl, text, model, mark, export
from mathutils import Matrix, Vector

reset()
R = math.radians

floor = mat("M_Floor", "wood_floor", tint=(0.75, 0.7, 0.66), rough=0.55)
wall = mat("M_Wall", "beige_wall_001", tint=(0.72, 0.72, 0.74), rough=0.9)
ceiling = mat("M_Ceiling", color=(0.78, 0.78, 0.77), rough=0.9)
stone = mat("M_Counter", color=(0.035, 0.035, 0.04), rough=0.16)
cabinet = mat("M_Cabinet", color=(0.09, 0.1, 0.11), rough=0.45)
handle = mat("M_Handle", color=(0.7, 0.7, 0.72), rough=0.2, metal=1.0)
frame = mat("M_Frame", color=(0.85, 0.85, 0.83), rough=0.4)
slat = mat("M_Slat", color=(0.86, 0.85, 0.82), rough=0.6)
glass = mat("M_Glass", color=(0.6, 0.7, 0.8), rough=0.02, alpha=0.06)
steel = mat("M_Steel", color=(0.62, 0.63, 0.64), rough=0.3, metal=1.0)
black = mat("M_Black", color=(0.02, 0.02, 0.025), rough=0.35)
strip = mat("M_Strip", color=(1.0, 0.85, 0.65), emit=(1.0, 0.78, 0.52), emit_strength=3.0)
phone_glass = mat("M_PhoneScreen", color=(0.01, 0.012, 0.02), rough=0.45, emit=(0.02, 0.025, 0.04), emit_strength=1.0)
notif = mat("M_Notif", color=(0.3, 0.32, 0.36), rough=0.3, emit=(0.05, 0.055, 0.065), emit_strength=1.0)
notif_ink = mat("M_NotifInk", color=(0.9, 0.9, 0.92), rough=0.5, emit=(0.5, 0.5, 0.52), emit_strength=1.0)
phone_body = mat("M_PhoneBody", color=(0.05, 0.05, 0.06), rough=0.3, metal=0.6)
laptop = mat("M_Laptop", color=(0.55, 0.56, 0.58), rough=0.3, metal=1.0)
sheet = mat("M_Sheet", color=(0.95, 0.96, 0.95), emit=(0.2, 0.22, 0.21), emit_strength=1.0)
sheet_ink = mat("M_SheetInk", color=(0.05, 0.25, 0.12), rough=0.5)
grid = mat("M_SheetGrid", color=(0.55, 0.6, 0.58), rough=0.5)
paper = mat("M_Paper", color=(0.9, 0.88, 0.84), rough=0.85)
print_ = mat("M_Print", color=(0.12, 0.09, 0.08), rough=0.7, emit=(0.6, 0.15, 0.1), emit_strength=0.0)



def turn(ob, center, euler):
    """kit.box bakes its location into the mesh (origin at the world origin): move the origin back to the box's centre
    before turning it, or it swings round the room."""
    ob.data.transform(Matrix.Translation(-Vector(center))); ob.location = center; ob.rotation_euler = euler
    return ob


W, D, H = 3.2, 5.5, 2.7
# shell
box("Floor", (0, D / 2, -0.05), (2 * W, D, 0.1), floor, uv=1.5)
box("Ceiling", (0, D / 2, H + 0.05), (2 * W, D, 0.1), ceiling, uv=0)
box("Back", (0, D + 0.1, H / 2), (2 * W, 0.2, H), wall, uv=1.5)
for s in (-1, 1): box(f"Side{s}", (s * (W + 0.1), D / 2, H / 2), (0.2, D, H), wall, uv=1.5)
# the window wall: wall above and below, a wide window x -2.4..1.8, z 0.55..2.4
x0, x1, z0, z1 = -2.4, 1.8, 0.55, 2.4
box("WinBelow", (0, -0.1, z0 / 2), (2 * W, 0.2, z0), wall, uv=1.5)
box("WinAbove", (0, -0.1, (z1 + H) / 2), (2 * W, 0.2, H - z1), wall, uv=1.5)
box("WinLeft", ((-W + x0) / 2, -0.1, (z0 + z1) / 2), (x0 + W, 0.2, z1 - z0), wall, uv=1.5)
box("WinRight", ((W + x1) / 2, -0.1, (z0 + z1) / 2), (W - x1, 0.2, z1 - z0), wall, uv=1.5)
box("Glass", ((x0 + x1) / 2, -0.12, (z0 + z1) / 2), (x1 - x0, 0.01, z1 - z0), glass, uv=0)
for i, x in enumerate((x0, (x0 + x1) / 2, x1)): box(f"Mullion{i}", (x, -0.08, (z0 + z1) / 2), (0.05, 0.06, z1 - z0), frame, uv=0)
for i, z in enumerate((z0, z1)): box(f"Transom{i}", ((x0 + x1) / 2, -0.08, z), (x1 - x0, 0.06, 0.05), frame, uv=0)
box("Sill", ((x0 + x1) / 2, 0.02, z0 - 0.02), (x1 - x0 + 0.1, 0.18, 0.03), frame, uv=0)
# horizontal blinds, tilted half open: the city comes through in stripes (the left third pulled up a little higher)
box("BlindHead", ((x0 + x1) / 2, 0.05, z1 - 0.03), (x1 - x0, 0.06, 0.05), slat, uv=0)
n = 0
z = z1 - 0.08
while z > z0 + 0.02:
    for k, (a, b) in enumerate(((x0 + 0.02, -1.0), (-1.0, x1 - 0.02))):
        if k == 0 and z < 1.15: continue   # the left blind is raised to here: an open gap of city
        ob = box(f"Slat{n}_{k}", ((a + b) / 2, 0.05, z), (b - a, 0.05, 0.003), slat, uv=0)
        turn(ob, ((a + b) / 2, 0.05, z), (R(-22), 0, 0))
    z -= 0.042; n += 1
box("BlindRail0", ((x0 + 0.02 - 1.0) / 2, 0.05, 1.13), (abs(-1.0 - x0), 0.05, 0.012), slat, uv=0)

# the kitchenette on the back wall: base cabinets, upper cabinets, an under-cabinet light strip, a sink, a small fridge
box("BaseCab", (0.4, D - 0.32, 0.44), (3.6, 0.62, 0.88), cabinet, uv=0, bevel=0.004)
box("BaseTop", (0.4, D - 0.32, 0.9), (3.64, 0.66, 0.04), stone, uv=0.8, bevel=0.003)
for i in range(6): box(f"BaseHandle{i}", (-1.2 + i * 0.6 + 0.25, D - 0.64, 0.8), (0.18, 0.015, 0.012), handle, uv=0)
box("UpperCab", (0.4, D - 0.18, 1.95), (3.6, 0.35, 0.75), cabinet, uv=0, bevel=0.004)
box("Strip", (0.4, D - 0.3, 1.565), (3.4, 0.03, 0.012), strip, uv=0)
box("Backsplash", (0.4, D - 0.005, 1.25), (3.6, 0.01, 0.66), stone, uv=0.6)
box("Sink", (1.2, D - 0.32, 0.91), (0.55, 0.4, 0.02), steel, uv=0)
cyl("Tap", (1.2, D - 0.1, 1.05), 0.012, 0.3, steel, uv=0)
box("Fridge", (2.65, D - 0.36, 0.95), (0.7, 0.7, 1.9), mat("M_Fridge", color=(0.75, 0.76, 0.77), rough=0.25, metal=0.8), uv=0, bevel=0.01)
model("vintage_electric_kettle", (-0.5, D - 0.35, 0.92), rot_z=R(200), bottom=0.92)
model("wine_bottles_01", (-1.3, D - 0.3, 0.92), rot_z=R(10), bottom=0.92)

# the counter you stand at: a peninsula from the right wall, marble top, dark base, two stools on the far side
cy = 2.35
box("CounterBase", (0.9, cy, 0.45), (3.2, 0.6, 0.9), cabinet, uv=0, bevel=0.004)
box("CounterTop", (0.9, cy, 0.92), (3.3, 0.72, 0.04), stone, uv=0.8, bevel=0.003)
model("metal_stool_01", (0.2, cy + 0.6, 0), rot_z=R(15), name="Stool0")
model("metal_stool_01", (1.0, cy + 0.6, 0), rot_z=R(-10), name="Stool1")
# on the counter: the phone (face up), a takeout box, the lanyard, a glass, the laptop (p_keep's BANKROLL sheet)
ph = turn(box("Prop_Phone", (0.18, cy - 0.12, 0.945), (0.075, 0.155, 0.008), phone_body, uv=0, bevel=0.006), (0.18, cy - 0.12, 0.945), (0, 0, R(-14)))
scr = turn(box("PhoneScreen", (0.18, cy - 0.12, 0.9495), (0.068, 0.146, 0.001), phone_glass, uv=0), (0.18, cy - 0.12, 0.9495), (0, 0, R(-14)))
# the lock screen when it lights up (Business shows it on the buzz): a notification from Slick
nb = bpy.data.objects.new("PhoneNotif", None); bpy.context.collection.objects.link(nb)
nb.location = (0.18, cy - 0.12, 0.9502); nb.rotation_euler = (0, 0, R(-14))
parts = [box("NotifCard", (0, 0.035, 0), (0.06, 0.03, 0.0004), notif, uv=0, bevel=0.002),
         text("NotifFrom", "SLICK", (-0.026, 0.044, 0.0003), 0.0055, notif_ink, extrude=0, bevel=0, rot=(0, 0, 0), align="LEFT"),
         text("NotifNow", "now", (0.026, 0.044, 0.0003), 0.0045, notif_ink, extrude=0, bevel=0, rot=(0, 0, 0), align="RIGHT"),
         text("NotifBody", "Saw your results from Saturday.", (-0.026, 0.034, 0.0003), 0.0042, notif_ink, extrude=0, bevel=0, rot=(0, 0, 0), align="LEFT"),
         text("NotifBody2", "Here's my offer.", (-0.026, 0.0265, 0.0003), 0.0042, notif_ink, extrude=0, bevel=0, rot=(0, 0, 0), align="LEFT"),
         text("LockTime", "11:20", (0, 0.0, 0.0003), 0.014, notif_ink, extrude=0, bevel=0, rot=(0, 0, 0))]
for o in parts: o.parent = nb
box("Takeout", (-0.35, cy + 0.05, 0.98), (0.2, 0.15, 0.1), paper, uv=0, bevel=0.005)
box("Lanyard", (0.45, cy - 0.05, 0.942), (0.06, 0.09, 0.004), mat("M_Lanyard", color=(0.05, 0.2, 0.55), rough=0.7), uv=0)
cyl("Glass", (0.62, cy + 0.12, 1.0), 0.035, 0.12, glass, uv=0)
lx, ly = 1.25, cy + 0.02
box("LaptopBase", (lx, ly, 0.948), (0.32, 0.22, 0.014), laptop, uv=0, bevel=0.004)
lid = turn(box("Prop_Laptop", (lx, ly + 0.11 + 0.01, 1.05), (0.32, 0.006, 0.21), laptop, uv=0, bevel=0.003), (lx, ly + 0.11 + 0.01, 1.05), (R(-18), 0, 0))
face = turn(box("LaptopScreen", (lx, ly + 0.11 - 0.004, 1.05), (0.3, 0.001, 0.19), sheet, uv=0), (lx, ly + 0.11 - 0.004, 1.05), (R(-18), 0, 0))
t = text("BankrollTitle", "BANKROLL", (lx - 0.13, ly + 0.11 - 0.006 + 0.075 * math.tan(R(18)), 1.125), 0.022, sheet_ink, extrude=0.0003, bevel=0, rot=(R(90 - 18), 0, 0), align="LEFT")
text("BankrollRow", "Jan 12     Savings              $2,000", (lx - 0.13, ly + 0.11 - 0.006 + 0.034 * math.tan(R(18)), 1.084), 0.011, sheet_ink, extrude=0.0003, bevel=0, rot=(R(90 - 18), 0, 0), align="LEFT")
for r in range(6):   # the sheet's grid
    zz = 1.1 - r * 0.026
    gc = (lx, ly + 0.11 - 0.006 + (zz - 1.05) * math.tan(R(18)), zz)
    turn(box(f"Row{r}", gc, (0.28, 0.0008, 0.0012), grid, uv=0), gc, (R(-18), 0, 0))

# the living side: sofa facing the window, coffee table, a lamp on a side table, a plant, a box never unpacked
model("sofa_02", (-2.0, 3.6, 0), rot_z=R(180), name="Sofa")
model("modern_coffee_table_01", (-2.0, 2.5, 0), rot_z=R(90))
model("side_table_01", (-2.95, 4.5, 0), name="SideTable")
cyl("LampBase", (-2.95, 4.5, 0.66), 0.06, 0.25, black, uv=0)
cyl("LampShade", (-2.95, 4.5, 0.9), 0.15, 0.22, mat("M_Shade", color=(0.95, 0.88, 0.75), rough=0.9, emit=(1.0, 0.75, 0.45), emit_strength=1.2), verts=24, uv=0)
model("potted_plant_02", (2.6, 0.45, 0), name="Plant")
model("cardboard_box_01", (2.6, 4.2, 0), rot_z=R(20), name="Box")
model("modern_ceiling_lamp_01", (1.6, cy, H), scale=0.7, name="Pendant", top=H)
# a framed print on the side wall: a final table under hot lights, abstract (the night he was nine)
box("PrintFrame", (-W + 0.02, 1.6, 1.6), (0.03, 0.9, 0.62), black, uv=0)
box("Print", (-W + 0.04, 1.6, 1.6), (0.005, 0.8, 0.52), print_, uv=0)
cyl("PrintFelt", (-W + 0.045, 1.6, 1.52), 0.25, 0.003, mat("M_PrintFelt", color=(0.05, 0.25, 0.15), rough=0.8), rot=(0, R(90), 0), uv=0)

# lights (Unity): the city through the blinds (shadows: the stripes), the strip under the cabinets, the lamp, the phone
mark("L_city", (-0.4, -2.2, 3.4))
mark("L_fill", (1.0, -1.5, 1.2))
mark("L_strip", (0.4, D - 0.45, 1.5))
mark("L_lamp", (-2.95, 4.5, 1.0))
mark("L_phone", (0.2, cy - 0.38, 1.22))   # the phone's glow, up on his face (not on the phone itself)
mark("AIM_hero", (0.25, cy - 0.5, 1.25))   # the city spot through the blinds aims here (Sets.Dress)
# people and cameras
mark("MARK_you", (0.25, cy - 0.62, 0), 0)
mark("CAM_phone", (0.1, cy - 0.33, 1.2))   # p_offer: down onto the lock screen
mark("AIM_phone", (0.18, cy - 0.1, 0.95))
mark("CAM_laptop", (lx - 0.12, ly - 0.42, 1.28))   # p_keep: over his shoulder onto the BANKROLL sheet
mark("AIM_laptop", (lx, ly + 0.1, 1.06))
mark("CAM_ext", (-2.7, 5.1, 1.9), 0)
mark("CAM_wide", (2.0, 4.3, 1.6), 0)

if "--preview" in sys.argv:
    sc = bpy.context.scene; sc.render.engine = "BLENDER_EEVEE"; sc.render.resolution_x, sc.render.resolution_y = 1280, 720
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    env = w.node_tree.nodes.new("ShaderNodeTexEnvironment")
    env.image = bpy.data.images.load(r"C:\Dev\Beta1Assets\polyhaven\modern_buildings_night\modern_buildings_night_2k.hdr")
    w.node_tree.links.new(env.outputs["Color"], w.node_tree.nodes["Background"].inputs["Color"])
    cam = bpy.data.cameras.new("c"); cam.lens = 28; co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
    for nm, loc, rot in (("wide", (2.0, 4.3, 1.6), (R(84), 0, R(160))), ("ext", (-2.7, 5.1, 1.9), (R(80), 0, R(215)))):
        co.location = loc; co.rotation_euler = rot
        sc.render.filepath = os.path.join(r"C:\Dev\Beta1Assets\preview", f"home_{nm}.png"); bpy.ops.render.render(write_still=True)
else:
    export("home")
    shutil.copy(r"C:\Dev\Beta1Assets\polyhaven\modern_buildings_night\modern_buildings_night_2k.hdr",
                os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "Assets", "Dream", "FilmSets", "home", "sky.hdr"))
