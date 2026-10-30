# Track H / H2: the closet's picture of you in each outfit (INBOX 20:15, "closet previews as pre-rendered layers"). For every
# outfit, your character is built in the nearest MakeHuman garment (tinted where the item asks), rendered in the studio
# (h1_test.py --studio, Cycles, strands) tall and wide, and saved at phone size as StreamingAssets/Home/you_<item>_*.jpg.
#   python beta1/tools/blender/closet.py [item ...]
# Glasses, chains and hats aren't layered yet; the picture changes with the outfit.
import json, os, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
WORK = r"C:\Dev\Beta1Assets\closet"
HOME = os.path.join(ROOT, "Assets", "StreamingAssets", "Home")
LOGO = {"male_casualsuit02", "male_casualsuit04", "male_casualsuit06"}   # stock tees with a MakeHuman logo: always repainted
# closet item -> (MakeHuman garment, tint or None); the nearest garment where there's no exact one (said in DAY_REPORT)
OUTFITS = {
    "tee": ("male_casualsuit06", [0.06, 0.06, 0.07]),
    "office": ("male_casualsuit01", None),
    "hoodie": ("male_casualsuit02", [0.32, 0.33, 0.36]),
    "polo": ("male_casualsuit03", None),
    "cardigan": ("male_casualsuit03", [0.55, 0.42, 0.3]),
    "jersey": ("male_casualsuit04", [0.75, 0.12, 0.12]),
    "bomber": ("male_casualsuit05", [0.08, 0.08, 0.09]),
    "leather": ("male_casualsuit05", [0.35, 0.2, 0.12]),
    "vest": ("male_elegantsuit01", [0.6, 0.5, 0.38]),
    "blazer": ("male_elegantsuit01", [0.15, 0.2, 0.35]),
    "suit": ("male_elegantsuit01", None),
    "tux": ("male_elegantsuit01", [0.04, 0.04, 0.05]),
}


def run(cmd, env=None):
    r = subprocess.run(cmd, env=env, capture_output=True, text=True, errors="replace")
    for line in r.stdout.splitlines():
        if "[h1]" in line or "exported" in line: print("   ", line.strip())
    return r.returncode


def main():
    base = json.load(open(os.path.join(ROOT, "tools", "blender", "chars", "you.json"), encoding="utf-8"))
    items = sys.argv[1:] or list(OUTFITS)
    os.makedirs(os.path.join(WORK, "specs"), exist_ok=True)
    for item in items:
        cloth, tint = OUTFITS[item]; name = "you_" + item
        spec = dict(base); spec["name"] = name; spec["clothes"] = [cloth, "shoes01"]
        spec["paint"] = {}
        if tint or cloth in LOGO: spec["paint"]["cloth_" + cloth] = [{"color": tint or [0.92, 0.92, 0.9]}]   # one colour over the whole garment (also paints out logos)
        sp = os.path.join(WORK, "specs", name + ".json"); json.dump(spec, open(sp, "w", encoding="utf-8"), indent=1)
        out = os.path.join(WORK, "build", name); env = dict(os.environ, CHAR_OUT=out)
        print(item, "->", cloth, tint or "")
        run([BLENDER, "-b", "-P", os.path.join(ROOT, "tools", "blender", "character.py"), "--", sp], env)
        for res, tag in (("740x1600", "tall"), ("1280x720", "wide")):
            env2 = dict(os.environ, CHAR=name, CHAR_DIR=out, RES=res, STRANDS="60000")
            run([BLENDER, "-b", "-P", os.path.join(ROOT, "tools", "blender", "h1_test.py"), "--", "cycles", "--strands", "--mcu", "--studio"], env2)
        src = r"C:\Dev\Beta1Assets\h1"
        tall = os.path.join(src, f"{name}_mcu_cycles_strands_studio_tall.png"); wide = os.path.join(src, f"{name}_mcu_cycles_strands_studio.png")
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", tall, "-vf", "crop=740:1360:0:240,pad=740:1600:0:0:color=0x08080a,scale=596:1290:flags=lanczos",
                        "-q:v", "4", os.path.join(HOME, f"{name}_portrait.jpg")], check=True)
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", wide, "-vf", "scale=1290:-2:flags=lanczos", "-q:v", "4", os.path.join(HOME, f"{name}_landscape.jpg")], check=True)


if __name__ == "__main__":
    main()
