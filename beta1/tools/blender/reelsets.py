# The highlight reel's vignettes (BETA1 task 9), each dressed only for what its one shot sees.
#   blender -b -P tools/blender/reelsets.py -- <casino|vegas|tv|titan|main|hall>
# Every vignette has CAM_hero and AIM_hero marks (the shot), L_* lights and, where people sit, MARK_* marks.
# Faces in the Hall are not drawn: the 27 legends are parody tributes, so their portraits are abstract canvases.
import math, os, sys, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from kit import reset, mat, box, cyl, torus, text, mark, export

which = sys.argv[sys.argv.index("--") + 1]
reset(); random.seed(3); R = math.radians

gold = mat("M_Gold", color=(1.0, 0.76, 0.33), rough=0.18, metal=1.0)
black = mat("M_Black", color=(0.012, 0.012, 0.015), rough=0.6)
felt = mat("M_Felt", "dirty_carpet", tint=(0.13, 0.36, 0.23), rough=1.0)
rail = mat("M_Rail", "leather_white", tint=(0.05, 0.05, 0.05), rough=0.45)
velvet = mat("M_Velvet", "leather_white", tint=(0.32, 0.08, 0.45), rough=0.85)
marble = mat("M_Marble", "marble_01", tint=(0.95, 0.96, 1.0), rough=0.15)
glassb = mat("M_GlassBlue", color=(0.3, 0.5, 0.9), rough=0.03, alpha=0.25)
gem = mat("M_Gem", color=(0.9, 0.95, 1.0), rough=0.0, emit=(0.8, 0.9, 1.0), emit_strength=2.0)
CHIPS = [mat(f"M_Chip{i}", color=c, rough=0.35) for i, c in enumerate([(0.92, 0.9, 0.82), (0.7, 0.06, 0.06), (0.08, 0.2, 0.6), (0.04, 0.04, 0.04), (0.35, 0.1, 0.5), (0.85, 0.65, 0.2)])]

def glow(name, col, s): return mat(name, color=col, emit=col, emit_strength=s)

def chips(x, y, z, n=6):
    for k in range(n):
        h = random.randint(6, 20) * 0.0034
        cyl(f"Chips{x:.2f}{y:.2f}{k}", (x + (k % 3) * 0.045, y + (k // 3) * 0.045, z + h / 2), 0.02, h, random.choice(CHIPS), verts=16, uv=0)

def poker_table(cx, cy, scale=1.0):
    t = cyl("Felt", (cx, cy, 0.76), 1.0 * scale, 0.04, felt, verts=48, uv=0.6); t.scale = (1.35, 0.75, 1)
    r = torus("Rail", (cx, cy, 0.79), 1.0 * scale, 0.08, rail); r.scale = (1.38, 0.8, 0.9)

if which == "casino":   # a Circuit ring on a pillow, roped off, chandeliers and slots glowing out of focus
    box("Floor", (0, 0, -0.05), (12, 12, 0.1), mat("M_Carpet", "dirty_carpet", tint=(0.35, 0.12, 0.3)), uv=1.0)
    box("Plinth", (0, 0, 0.5), (0.6, 0.6, 1.0), black, uv=0, bevel=0.01)
    box("Pillow", (0, 0, 1.06), (0.34, 0.34, 0.1), velvet, uv=0.3, bevel=0.04, segments=4)
    ring = torus("Ring", (0, 0, 1.15), 0.035, 0.009, gold, rot=(R(80), 0, 0))
    box("RingTop", (0, -0.003, 1.19), (0.03, 0.016, 0.02), gold, uv=0, bevel=0.004)
    cyl("RingGem", (0, -0.003, 1.203), 0.009, 0.008, gem, uv=0)
    for x in (-0.9, 0.9): cyl(f"Stanchion{x}", (x, 0.4, 0.5), 0.03, 1.0, gold, uv=0)
    box("Rope", (0, 0.4, 0.92), (1.8, 0.03, 0.03), velvet, uv=0)
    for i in range(40):   # the casino floor beyond: slots and chandeliers, for bokeh
        col = random.choice([(1, 0.6, 0.15), (0.6, 0.2, 1.0), (1, 0.2, 0.5), (0.2, 0.8, 1)])
        box(f"Slot{i}", (random.uniform(-6, 6), random.uniform(3, 9), random.uniform(0.6, 2.2)), (0.3, 0.05, 0.4), glow(f"M_Slot{i}", col, random.uniform(3, 8)), uv=0)
    for i in range(6): cyl(f"Chandelier{i}", (random.uniform(-5, 5), random.uniform(3, 8), 3.6), 0.4, 0.3, glow(f"M_Chand{i}", (1, 0.8, 0.5), 6), uv=0)
    mark("L_key", (0.6, -0.6, 2.0)); mark("L_rim", (-0.6, 0.9, 1.8))
    mark("CAM_hero", (0.0, -0.75, 1.32)); mark("AIM_hero", (0, 0, 1.16))
elif which == "vegas":   # the bracelet in its case under white heat, a sea of tables behind
    box("Floor", (0, 0, -0.05), (30, 30, 0.1), mat("M_Carpet", "dirty_carpet", tint=(0.25, 0.22, 0.3)), uv=1.0)
    box("CaseBase", (0, 0, 0.5), (0.7, 0.5, 1.0), black, uv=0, bevel=0.01)
    box("CaseGlass", (0, 0, 1.2), (0.66, 0.46, 0.4), mat("M_Glass", color=(0.9, 0.95, 1), rough=0.01, alpha=0.1), uv=0)
    box("CaseCushion", (0, 0, 1.03), (0.5, 0.32, 0.06), black, uv=0, bevel=0.02)
    b = torus("Bracelet", (0, 0, 1.1), 0.075, 0.012, gold, rot=(R(70), 0, 0)); b.scale = (1, 1, 0.75)
    box("BraceletPlate", (0, -0.07, 1.12), (0.07, 0.012, 0.035), gold, uv=0, bevel=0.004)
    for i in range(30):
        x, y = random.uniform(-12, 12), random.uniform(4, 20)
        t = cyl(f"BgFelt{i}", (x, y, 0.76), 1.0, 0.04, felt, verts=24, uv=0); t.scale = (1.35, 0.75, 1)
        cyl(f"BgLamp{i}", (x, y, 3.0), 0.5, 0.05, glow(f"M_BgLamp{i}", (1, 0.97, 0.9), 6), uv=0)
    mark("L_key", (0, -0.2, 2.6)); mark("L_rim", (0.8, 1.0, 1.6))
    mark("CAM_hero", (0.35, -0.85, 1.42)); mark("AIM_hero", (0, 0, 1.12))
elif which == "tv":   # the feature table: key-lit faces in a black room, cameras, the hole-card lens
    box("Floor", (0, 0, -0.05), (14, 14, 0.1), black, uv=0)
    poker_table(0, 0)
    for k in range(5): box(f"Board{k}", (-0.24 + k * 0.12, 0, 0.782), (0.064, 0.09, 0.002), mat("M_Card", color=(0.95, 0.95, 0.92)), uv=0)
    chips(-0.9, -0.25, 0.78); chips(0.9, 0.25, 0.78)
    for i, (x, y, rz) in enumerate([(-3, -2.5, 40), (3, -2.5, -40), (0, 3.2, 180)]):   # broadcast cameras on tripods
        box(f"Cam{i}", (x, y, 1.6), (0.25, 0.6, 0.3), black, uv=0, bevel=0.02).rotation_euler = (0, 0, R(rz))
        cyl(f"CamLens{i}", (x, y, 1.6), 0.08, 0.1, glow(f"M_Tally{i}", (1, 0.1, 0.1), 2), uv=0)
        cyl(f"Tripod{i}", (x, y, 0.8), 0.03, 1.6, black, uv=0)
    for i in range(10): cyl(f"Rig{i}", (-4 + i * 0.9, 1.5, 3.4), 0.12, 0.2, glow(f"M_Rig{i}", (1, 0.95, 0.85), 10), uv=0)
    text("Logo", "THE TOUR  ·  FEATURE TABLE", (0, 4.5, 2.6), 0.4, glow("M_Logo", (0.2, 0.8, 1.0), 4))
    mark("MARK_nick", (1.6, 1.2, 0), 225)
    mark("L_key", (1.0, -1.0, 3.0)); mark("L_rim", (0, 2.5, 2.6))
    mark("CAM_hero", (-2.6, -2.2, 1.7)); mark("AIM_hero", (0.4, 0.4, 1.0))
elif which == "titan":   # cold marble, blue glass, a view, the trident trophy
    box("Floor", (0, 0, -0.05), (12, 12, 0.1), marble, uv=2.0)
    box("Glass", (0, 4, 1.5), (12, 0.03, 3), glassb, uv=0)
    for i in range(30): box(f"City{i}", (random.uniform(-20, 20), random.uniform(15, 40), random.uniform(1, 8)), (0.2, 0.2, 0.2), glow(f"M_City{i}", (0.6, 0.75, 1), random.uniform(2, 6)), uv=0)
    box("Pedestal", (0, 0, 0.5), (0.5, 0.5, 1.0), marble, uv=1.0, bevel=0.01)
    cyl("TrophyBase", (0, 0, 1.05), 0.1, 0.1, black, uv=0)
    cyl("TrophyShaft", (0, 0, 1.3), 0.015, 0.4, mat("M_Silver", color=(0.9, 0.92, 0.95), rough=0.08, metal=1.0), uv=0)
    for dx in (-0.06, 0, 0.06):   # the trident's prongs
        cyl(f"Prong{dx}", (dx, 0, 1.58 + (0.04 if dx == 0 else 0)), 0.01, 0.2, mat("M_Silver", color=(0.9, 0.92, 0.95), rough=0.08, metal=1.0), uv=0)
    box("Crossbar", (0, 0, 1.49), (0.14, 0.02, 0.02), mat("M_Silver", color=(0.9, 0.92, 0.95), rough=0.08, metal=1.0), uv=0)
    mark("L_key", (-0.8, -0.7, 2.5)); mark("L_rim", (0.6, 1.5, 1.8))
    mark("CAM_hero", (0.7, -1.9, 1.45)); mark("AIM_hero", (0, 0, 1.4))
elif which == "main":   # the Main Event: a wall of lights behind the final table, Bio waiting
    box("Floor", (0, 0, -0.05), (20, 20, 0.1), black, uv=0)
    poker_table(0, 0)
    chips(-0.2, 0.4, 0.78, 9); chips(0.2, -0.45, 0.78, 6)
    for i in range(18):
        for j in range(7):
            cyl(f"Wall{i}_{j}", (-8.5 + i * 1.0, 6, 0.6 + j * 0.8), 0.18, 0.05, glow(f"M_Wall{i}{j}", (1, 0.9, 0.7), random.uniform(5, 12)), rot=(R(90), 0, 0), uv=0)
    mark("MARK_bio", (0, 0.95, 0.5), 180); mark("MARK_you", (0, -0.95, 0.5), 0)
    mark("L_key", (0.8, -0.2, 2.8)); mark("L_rim", (0, 3.0, 2.0))
    mark("CAM_hero", (-0.55, -1.6, 1.3)); mark("AIM_hero", (0, 0.95, 1.2))
elif which == "hall":   # gold on black: a row of portraits, one frame still empty
    box("Floor", (0, 0, -0.05), (16, 10, 0.1), mat("M_FloorBlack", color=(0.02, 0.02, 0.025), rough=0.12), uv=0)
    box("Wall", (0, 3, 2), (16, 0.2, 4), black, uv=0)
    for i in range(7):
        x = -6 + i * 2
        box(f"Frame{i}", (x, 2.85, 1.9), (1.2, 0.06, 1.5), gold, uv=0, bevel=0.02)
        if i != 3:   # abstract canvases: no faces
            col = random.choice([(0.25, 0.12, 0.08), (0.08, 0.12, 0.22), (0.18, 0.16, 0.1), (0.12, 0.06, 0.1)])
            box(f"Canvas{i}", (x, 2.81, 1.9), (1.0, 0.02, 1.3), mat(f"M_Canvas{i}", color=col, rough=0.8), uv=0)
        else:
            box(f"Canvas{i}", (x, 2.81, 1.9), (1.0, 0.02, 1.3), black, uv=0)
        box(f"Plate{i}", (x, 2.8, 1.0), (0.5, 0.02, 0.1), gold, uv=0, bevel=0.005)
        cyl(f"Spot{i}", (x, 2.2, 3.6), 0.08, 0.15, glow(f"M_Spot{i}", (1, 0.85, 0.55), 8), uv=0)
        mark(f"L_spot_{i}", (x, 1.6, 3.4))
    mark("CAM_hero", (-1.2, -2.5, 1.6)); mark("AIM_hero", (0, 2.8, 1.8))
export("reel_" + which)
