# Floor 34, Brickell, 6:47 PM (BETA1 task 6). blender -b -P tools/blender/office.py [-- --preview]
# Open-plan floor: x -7..7, y 0 (the glass wall, the bay going pink outside) .. 9 (core wall), z up, ceiling 2.9.
# Your desk is by the glass: you sit facing +y (into the office, the sunset behind you); Lou stands at the desk's side.
import math, os, sys, shutil
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from kit import reset, mat, box, cyl, text, model, mark, export

reset()
R = math.radians

carpet = mat("M_Carpet", "dirty_carpet", tint=(0.42, 0.45, 0.5), rough=0.9)
ceiling = mat("M_Ceiling", color=(0.82, 0.83, 0.85), rough=0.8)
panel = mat("M_Panel", color=(0.95, 0.97, 1.0), emit=(0.85, 0.95, 1.0), emit_strength=0.0)   # fluorescents, off after hours
core = mat("M_Core", color=(0.55, 0.57, 0.6), rough=0.6)
desk = mat("M_Desk", "laminate_floor_02", tint=(0.85, 0.82, 0.78), rough=0.35)
metal = mat("M_Metal", color=(0.3, 0.31, 0.33), rough=0.35, metal=1.0)
alu = mat("M_Alu", color=(0.78, 0.79, 0.8), rough=0.25, metal=1.0)
glass = mat("M_Glass", color=(0.7, 0.85, 0.95), rough=0.02, alpha=0.08)
mullion = mat("M_Mullion", color=(0.12, 0.13, 0.14), rough=0.4, metal=1.0)
screen = mat("M_Screen", color=(0.2, 0.25, 0.3), emit=(0.55, 0.75, 1.0), emit_strength=2.2)
slide = mat("M_Slide", color=(0.95, 0.95, 0.97), emit=(0.92, 0.94, 1.0), emit_strength=2.6)
slide_ink = mat("M_SlideInk", color=(0.05, 0.1, 0.25), rough=0.5)
black = mat("M_Black", color=(0.02, 0.02, 0.025), rough=0.4)
fabric = mat("M_Partition", color=(0.32, 0.36, 0.42), rough=0.95)
paper = mat("M_Paper", color=(0.92, 0.92, 0.9), rough=0.8)
cup = mat("M_Cup", color=(0.95, 0.94, 0.9), rough=0.3)
coffee = mat("M_Coffee", color=(0.07, 0.04, 0.02), rough=0.05)
lanyard = mat("M_Lanyard", color=(0.05, 0.2, 0.55), rough=0.7)

# shell
box("Floor", (0, 4.5, -0.05), (14, 9, 0.1), carpet, uv=2.0)
box("Ceiling", (0, 4.5, 2.95), (14, 9, 0.1), ceiling, uv=0)
box("Core", (0, 9.1, 1.45), (14, 0.2, 2.9), core, uv=0)
for s in (-1, 1): box(f"Side{s}", (7.1 * s, 4.5, 1.45), (0.2, 9, 2.9), core, uv=0)
for i in range(4):   # ceiling light panels, dark now
    for j in range(3): box(f"Panel{i}{j}", (-4.5 + i * 3, 1.8 + j * 2.6, 2.89), (1.2, 0.6, 0.02), panel, uv=0)
# the glass wall: floor to ceiling, black mullions every 1.5 m, a spandrel band at the ceiling
box("Glass", (0, -0.02, 1.45), (14, 0.02, 2.9), glass, uv=0)
for i in range(11): box(f"Mullion{i}", (-7.5 + i * 1.5, 0, 1.45), (0.07, 0.12, 2.9), mullion, uv=0)
box("Sill", (0, 0.05, 0.03), (14, 0.15, 0.06), mullion, uv=0)

# desks: rows of benching with low partitions; yours is first row by the glass, centre
def desk_at(name, x, y, rot, mine=False):
    box(f"{name}Top", (x, y, 0.74), (1.6, 0.8, 0.03), desk, uv=1.0, bevel=0.005)
    for dx in (-0.75, 0.75): box(f"{name}Leg{dx}", (x + dx, y, 0.37), (0.05, 0.7, 0.74), metal, uv=0)
    box(f"{name}Screen", (x, y + 0.42, 1.0), (1.6, 0.03, 0.5), fabric, uv=0)
    # two monitors
    for k, dx in enumerate((-0.33, 0.33)):
        box(f"{name}Mon{k}", (x + dx, y + 0.22, 1.08), (0.6, 0.025, 0.36), black, uv=0, bevel=0.005)
        box(f"{name}MonFace{k}", (x + dx, y + 0.205, 1.085), (0.56, 0.005, 0.32), slide if (mine and k == 0) else screen, uv=0)
        cyl(f"{name}MonStem{k}", (x + dx, y + 0.25, 0.85), 0.02, 0.22, alu, uv=0)
        box(f"{name}MonFoot{k}", (x + dx, y + 0.25, 0.76), (0.2, 0.15, 0.01), alu, uv=0)
    box(f"{name}Keys", (x - 0.05, y - 0.05, 0.765), (0.42, 0.13, 0.015), black, uv=0, bevel=0.003)
    box(f"{name}Mouse", (x + 0.3, y - 0.05, 0.765), (0.06, 0.1, 0.02), black, uv=0, bevel=0.008)
    if mine:
        # the Henderson deck on screen: title bar and bars of a chart
        text("HendersonTitle", "HENDERSON  Q3  REVIEW", (x - 0.33, y + 0.2, 1.21), 0.035, slide_ink, extrude=0.0005, bevel=0, rot=(R(90), 0, 0))
        for b in range(5):
            h = (0.05, 0.09, 0.07, 0.12, 0.15)[b]
            box(f"Bar{b}", (x - 0.5 + b * 0.08, y + 0.2, 0.97 + h / 2), (0.05, 0.003, h), slide_ink, uv=0)
        cyl("ColdCoffee", (x + 0.62, y - 0.15, 0.81), 0.04, 0.1, cup, uv=0)
        cyl("CoffeeTop", (x + 0.62, y - 0.15, 0.858), 0.036, 0.003, coffee, uv=0)
        box("Lanyard", (x - 0.55, y - 0.2, 0.757), (0.06, 0.09, 0.004), lanyard, uv=0)
        box("Papers", (x + 0.45, y + 0.05, 0.76), (0.22, 0.3, 0.012), paper, uv=0)
        model("desk_lamp_arm_01", (x - 0.7, y + 0.2, 0.755), rot_z=R(200))
        model("office_notepads", (x - 0.4, y - 0.15, 0.755))
for i, x in enumerate((-4.2, -2.4, 0.0, 2.4, 4.2)):
    desk_at(f"DeskA{i}", x, 1.9, 0, mine=(x == 0.0))
    desk_at(f"DeskB{i}", x, 4.6, 0)
    model("modern_arm_chair_01", (x, 1.3 if x != 0.0 else 1.25, 0), rot_z=R(0), name=f"ChairA{i}")
    model("modern_arm_chair_01", (x, 4.0, 0), rot_z=R(10 * (i - 2)), name=f"ChairB{i}")
model("calathea_orbifolia_01", (6.3, 0.6, 0), scale=1.4)
model("wall_clock", (2.0, 8.98, 2.2), rot_z=R(180))
box("ConfDoor", (-3, 8.95, 1.05), (1.0, 0.1, 2.1), black, uv=0)

# lights (Unity): the monitors' glow, the sunset through the glass, a few practicals left on
mark("L_monitor_0", (0, 1.6, 1.1))
mark("L_sun", (0, -6, 3.0))
mark("L_exit", (-6.5, 8.8, 2.6))
mark("L_desklamp", (-0.7, 2.05, 1.15))
# people and cameras
mark("MARK_you", (0.0, 1.35, 0.52), 0)
mark("MARK_lou", (1.15, 2.2, 0), 120)
mark("CAM_ext", (-6.2, 7.5, 1.7), 0)
mark("CAM_wide", (-3.5, 5.5, 1.6), 0)

if "--preview" in sys.argv:
    sc = bpy.context.scene; sc.render.engine = "BLENDER_EEVEE"; sc.render.resolution_x, sc.render.resolution_y = 1280, 720
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    env = w.node_tree.nodes.new("ShaderNodeTexEnvironment")
    env.image = bpy.data.images.load(r"C:\Dev\Beta1Assets\polyhaven\sunset_jhbcentral\sunset_jhbcentral_2k.hdr")
    w.node_tree.links.new(env.outputs["Color"], w.node_tree.nodes["Background"].inputs["Color"])
    cam = bpy.data.cameras.new("c"); cam.lens = 30; co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co); sc.camera = co
    for nm, loc, rot in (("wide", (-6.2, 7.5, 1.7), (R(88), 0, R(215))), ("desk", (0.6, 3.6, 1.5), (R(85), 0, R(170)))):
        co.location = loc; co.rotation_euler = rot
        sc.render.filepath = os.path.join(r"C:\Dev\Beta1Assets\preview", f"office_{nm}.png"); bpy.ops.render.render(write_still=True)
else:
    export("office")
    shutil.copy(r"C:\Dev\Beta1Assets\polyhaven\sunset_jhbcentral\sunset_jhbcentral_2k.hdr",
                os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "Assets", "Dream", "FilmSets", "office", "sky.hdr"))
