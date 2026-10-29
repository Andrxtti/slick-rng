# One still per line of a rendered film, from the cue sheet the player writes (Logs/film_<id>.txt), plus a labelled
# contact sheet. python beta1/tools/stills.py <id> [--offset 1.2]
#   -> C:\Dev\Builds\Beta1\Film\<id>_stills\NN.png and <id>_sheet.jpg
import os, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FILM = r"C:\Dev\Builds\Beta1\Film"
sid = sys.argv[1]
off = float(sys.argv[sys.argv.index("--offset") + 1]) if "--offset" in sys.argv else 1.2
cues = [l.rstrip("\n").split("\t") for l in open(os.path.join(ROOT, "Logs", f"film_{sid}.txt"), encoding="utf-8") if l.strip()]
out = os.path.join(FILM, f"{sid}_stills"); os.makedirs(out, exist_ok=True)
tiles = []
for k, (n, t, shot, text, *_) in enumerate(cues):
    p = os.path.join(out, f"{int(n):02d}.png")
    end = float(cues[k + 1][1]) if k + 1 < len(cues) else float(t) + 2 * off
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-ss", f"{(float(t) + end) / 2:.2f}", "-i", os.path.join(FILM, sid + ".mp4"), "-frames:v", "1", p], check=True)
    tiles.append((p, f"{n} {shot}"))
inputs, filt = [], []
for i, (p, label) in enumerate(tiles):
    inputs += ["-i", p]
    lab = label.replace(":", " ").replace("'", "")
    filt.append(f"[{i}]scale=480:270,drawtext=fontfile='C\:/Windows/Fonts/arial.ttf':text='{lab}':x=8:y=8:fontsize=22:fontcolor=white:box=1:boxcolor=black@0.6[t{i}]")
cols = 4; rows = (len(tiles) + cols - 1) // cols
pad = cols * rows - len(tiles)
for k in range(pad):
    filt.append(f"color=black:s=480x270:d=1[p{k}]")
names = [f"[t{i}]" for i in range(len(tiles))] + [f"[p{k}]" for k in range(pad)]
filt.append("".join(names) + f"xstack=inputs={len(names)}:layout=" + "|".join(f"{(i % cols) * 480}_{(i // cols) * 270}" for i in range(len(names))))
subprocess.run(["ffmpeg", "-v", "error", "-y", *inputs, "-filter_complex", ";".join(filt), "-frames:v", "1", os.path.join(FILM, f"{sid}_sheet.jpg")], check=True)
print(os.path.join(FILM, f"{sid}_sheet.jpg"))
