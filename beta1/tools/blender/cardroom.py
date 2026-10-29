# The Biscayne Card Room (BETA1 task 8): carpet from 1994, sodium light, twenty tables, the list board, the cage.
#   blender -b -P tools/blender/cardroom.py [-- --preview]
# Room x -10..10, y 0 (front windows, palms outside) .. 14 (the cage wall), ceiling 3.4. Table 9 (yours) is at the
# front-centre: you sit in seat 4 facing +y, the dealer opposite; Tommy and Gnomey stand at the rail.
import math, os, sys, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from kit import reset, mat, box, cyl, torus, text, model, mark, export

reset(); random.seed(9)
R = math.radians

carpet = mat("M_Carpet", "dirty_carpet", tint=(0.55, 0.16, 0.2), rough=0.95)
wall = mat("M_Wall", "beige_wall_001", tint=(0.75, 0.68, 0.55))
ceiling = mat("M_Ceiling", color=(0.25, 0.23, 0.2), rough=0.9)
felt = mat("M_Felt", "dirty_carpet", tint=(0.13, 0.36, 0.23), rough=1.0)
rail = mat("M_Rail", "leather_white", tint=(0.06, 0.05, 0.05), rough=0.45)
wood = mat("M_Wood", "dark_paneled_wood", tint=(0.8, 0.7, 0.6), rough=0.4)
chairmat = mat("M_Chair", "leather_white", tint=(0.35, 0.08, 0.08), rough=0.5)
chrome = mat("M_Chrome", color=(0.85, 0.85, 0.87), rough=0.12, metal=1.0)
glass = mat("M_Glass", color=(0.7, 0.85, 0.9), rough=0.02, alpha=0.12)
board = mat("M_Board", color=(0.95, 0.95, 0.93), rough=0.5)
ink = mat("M_Ink", color=(0.05, 0.08, 0.3), rough=0.6)
redink = mat("M_RedInk", color=(0.6, 0.04, 0.04), rough=0.6)
lamp = mat("M_Lamp", color=(1, 0.8, 0.5), emit=(1.0, 0.75, 0.42), emit_strength=8.0)
shade = mat("M_Shade", color=(0.08, 0.06, 0.05), rough=0.6)
neon = mat("M_Neon", color=(1, 0.75, 0.2), emit=(1.0, 0.7, 0.18), emit_strength=12.0)
night = mat("M_Night", color=(0.02, 0.03, 0.06), rough=1.0)
trunk = mat("M_Trunk", color=(0.25, 0.2, 0.15), rough=0.9)
frond = mat("M_Frond", color=(0.05, 0.12, 0.06), rough=0.8)
card = mat("M_Card", color=(0.95, 0.95, 0.92), rough=0.35)
cardback = mat("M_CardBack", color=(0.55, 0.06, 0.07), rough=0.35)
CHIPS = [mat(f"M_Chip{i}", color=c, rough=0.35) for i, c in enumerate([(0.92, 0.9, 0.82), (0.7, 0.06, 0.06), (0.08, 0.2, 0.6), (0.04, 0.04, 0.04), (0.35, 0.1, 0.5)])]

box("Floor", (0, 7, -0.05), (20, 14, 0.1), carpet, uv=1.5)
box("Ceiling", (0, 7, 3.45), (20, 14, 0.1), ceiling, uv=0)
box("BackWall", (0, 14.1, 1.7), (20, 0.2, 3.4), wall, uv=2.0)
for s in (-1, 1): box(f"Side{s}", (10.1 * s, 7, 1.7), (0.2, 14, 3.4), wall, uv=2.0)
box("FrontLow", (0, 0, 0.5), (20, 0.25, 1.0), wall, uv=2.0)
box("FrontHigh", (0, 0, 3.0), (20, 0.25, 0.8), wall, uv=2.0)
box("Windows", (0, 0, 1.8), (20, 0.02, 1.6), glass, uv=0)
for i in range(11): box(f"WinPost{i}", (-10 + i * 2, 0, 1.8), (0.1, 0.2, 1.6), wood, uv=0)
box("Outside", (0, -8, 2), (40, 0.1, 6), night, uv=0)
for i, x in enumerate((-7, -2.5, 3, 7.5)):   # palms out front, black against the street light
    cyl(f"PalmTrunk{i}", (x, -3, 2.5), 0.12, 5, trunk, uv=0)
    for k in range(7):
        a = k / 7 * math.tau
        f = box(f"Frond{i}{k}", (x + math.cos(a) * 0.9, -3 + math.sin(a) * 0.9, 4.9), (1.8, 0.25, 0.03), frond, uv=0)
        f.rotation_euler = (R(random.uniform(-25, -10)), 0, a)
text("SignOut", "BISCAYNE CARD ROOM", (0, -0.2, 3.05), 0.38, neon)

# the cage and the board by it: names of Classic champions, in permanent marker
box("Cage", (6.5, 13.6, 1.4), (4.0, 0.8, 2.8), wood, uv=1.5, bevel=0.01)
for k in range(9): cyl(f"CageBar{k}", (4.8 + k * 0.42, 13.15, 1.9), 0.012, 1.4, chrome, uv=0)
box("ChampBoard", (2.2, 14.0, 1.9), (2.6, 0.05, 1.5), board, uv=0, bevel=0.01)
text("ChampTitle", "BISCAYNE CLASSIC CHAMPIONS", (2.2, 13.96, 2.5), 0.11, redink, extrude=0.001, bevel=0, rot=(R(90), 0, R(180)))
for k, nm in enumerate(("1994  R. DELGADO", "2003  T. MAIO  (runner-up)", "2011  M. OKAFOR", "2019  J. PIERCE", "2025  ________")):
    text(f"Champ{k}", nm, (2.2, 13.96, 2.25 - k * 0.2), 0.08, ink, extrude=0.001, bevel=0, rot=(R(90), 0, R(180)))
box("ListBoard", (-4.5, 14.0, 1.9), (3.0, 0.05, 1.4), board, uv=0, bevel=0.01)
text("ListTitle", "THE LIST   1/2 NL   2/5 NL   MIDNIGHT RUSH", (-4.5, 13.96, 2.45), 0.09, ink, extrude=0.001, bevel=0, rot=(R(90), 0, R(180)))
for k in range(5): text(f"ListName{k}", random.choice(["JOEY K", "TINA", "BIG MIKE", "P.R.", "SAL", "DEX", "ROSA", "LITTLE J"]), (-5.4 + (k % 3) * 0.9, 13.96, 2.15 - (k // 3) * 0.25), 0.08, ink, extrude=0.001, bevel=0, rot=(R(90), 0, R(180)))

# tables: an oval of felt, padded rail, wood edge, chairs, chips and a lamp over each
def table(name, cx, cy, rot, hero=False):
    t = cyl(f"{name}Felt", (cx, cy, 0.76), 1.0, 0.04, felt, verts=48, uv=0.6); t.scale = (1.35, 0.75, 1)
    r = torus(f"{name}Rail", (cx, cy, 0.79), 1.0, 0.08, rail); r.scale = (1.38, 0.8, 0.9)
    e = cyl(f"{name}Edge", (cx, cy, 0.71), 1.08, 0.08, wood, verts=48, uv=0); e.scale = (1.4, 0.82, 1)
    cyl(f"{name}Base", (cx, cy, 0.35), 0.3, 0.7, shade, uv=0)
    cyl(f"{name}LampShade", (cx, cy, 2.55), 0.5, 0.25, shade, verts=24, uv=0)
    cyl(f"{name}LampGlow", (cx, cy, 2.42), 0.45, 0.01, lamp, verts=24, uv=0)
    mark(f"L_table_{name}", (cx, cy, 2.35))
    for k in range(9):
        a = math.tau * k / 9 + R(rot)
        px, py = cx + math.cos(a) * 1.75, cy + math.sin(a) * 1.05
        if hero and k == 4: continue   # your seat: your own chair is below
        c = box(f"{name}Chair{k}", (px, py, 0.48), (0.48, 0.48, 0.1), chairmat, uv=0, bevel=0.03)
        b = box(f"{name}ChairBack{k}", (px + math.cos(a) * 0.24, py + math.sin(a) * 0.24, 0.8), (0.46, 0.08, 0.55), chairmat, uv=0, bevel=0.03)
        b.rotation_euler = (0, 0, a + R(90)); c.rotation_euler = (0, 0, a)
        cyl(f"{name}ChairPost{k}", (px, py, 0.22), 0.03, 0.44, chrome, uv=0)
        cyl(f"{name}ChairFoot{k}", (px, py, 0.02), 0.22, 0.03, shade, uv=0)
        sx, sy = cx + math.cos(a) * 1.05, cy + math.sin(a) * 0.6
        for j in range(random.randint(1, 3)):   # chip stacks in front of each seat
            col = random.choice(CHIPS); h = random.randint(4, 18) * 0.0034
            cyl(f"{name}Chips{k}{j}", (sx + j * 0.045, sy, 0.78 + h / 2), 0.02, h, col, verts=16, uv=0)
    for k in range(5):   # the board
        box(f"{name}Board{k}", (cx - 0.24 + k * 0.12, cy, 0.782), (0.064, 0.09, 0.002), card, uv=0)
    return t

for i, (x, y) in enumerate([(-6.5, 3.5), (-2.2, 3.5), (2.2, 3.5), (6.5, 3.5), (-6.5, 7.2), (-2.2, 7.2), (2.2, 7.2), (6.5, 7.2), (-4.4, 10.6), (0.0, 10.6)]):
    table(f"T{i}", x, y, 0, hero=(i == 1))
# your seat at table T1 (centre-front): seat 4 faces +y toward the dealer at seat 0's side
HX, HY = -2.2, 3.5
ya = math.tau * 4 / 9
yx, yy = HX + math.cos(ya) * 1.75, HY + math.sin(ya) * 1.05
box("YourChair", (yx, yy, 0.48), (0.48, 0.48, 0.1), chairmat, uv=0, bevel=0.03)
cyl("YourChairPost", (yx, yy, 0.22), 0.03, 0.44, chrome, uv=0)
for k in range(2):
    c = box(f"HoleCard{k}", (HX + math.cos(ya) * 0.95 + k * 0.07, HY + math.sin(ya) * 0.55, 0.782), (0.064, 0.09, 0.002), cardback, uv=0)
    c.name = f"Prop_Cards" if k == 0 else c.name

# people and cameras: you seated; the dealer opposite; Tommy and Gnomey standing by; Mustich at the next table
mark("MARK_you", (yx, yy, 0.55), math.degrees(ya) + 90 + 180)
mark("MARK_dealer", (HX, HY - 0.95, 0.0), 0)
mark("MARK_tommy", (yx + 0.75, yy + 0.55, 0.0), 160)
mark("MARK_gnomey", (yx - 0.9, yy + 0.7, 0.0), 210)
mark("MARK_slick", (yx + 0.75, yy + 0.55, 0.0), 160)   # Tommy's spot (classic_other: Slick holds the trophy; Tommy is not in that film)
mx, my = 2.2 + math.cos(math.tau * 5 / 9) * 1.75, 3.5 + math.sin(math.tau * 5 / 9) * 1.05
mark("MARK_mustich", (mx, my, 0.55), math.degrees(math.tau * 5 / 9) + 90 + 180)
mark("CAM_ext", (8.5, 12.5, 3.0), 0)
mark("CAM_wide", (-7.5, 12.0, 2.2), 0)
mark("L_window", (0, -2.0, 3.0))
mark("L_cage", (6.5, 12.6, 2.6))

if "--preview" in sys.argv:
    sc = bpy.context.scene; sc.render.engine = "BLENDER_EEVEE"; sc.render.resolution_x, sc.render.resolution_y = 1280, 720
    for o in [o for o in bpy.data.objects if o.name.startswith("L_table")]:
        l = bpy.data.lights.new(o.name + "x", "POINT"); l.energy = 150; l.color = (1, 0.75, 0.45)
        lo = bpy.data.objects.new(o.name + "x", l); sc.collection.objects.link(lo); lo.location = o.location
    cam = bpy.data.cameras.new("c"); cam.lens = 28; co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
    co.location = (8.5, 12.5, 3.0); co.rotation_euler = (R(72), 0, R(135))
    sc.render.filepath = r"C:\Dev\Beta1Assets\preview\cardroom_wide.png"; bpy.ops.render.render(write_still=True)
else:
    export("cardroom")
