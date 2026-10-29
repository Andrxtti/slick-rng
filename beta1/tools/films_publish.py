# The rendered films, made phone-sized for the game: 720p H.264 into Assets/StreamingAssets/Films/<id>.mp4 (outside
# git; staged into the web build). The reel goes in as reel.mp4.   python beta1/tools/films_publish.py
import os, subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = r"C:\Dev\Builds\Beta1\Film"
OUT = os.path.join(ROOT, "Assets", "StreamingAssets", "Films")
os.makedirs(OUT, exist_ok=True)
FILMS = ["p_office", "p_diner", "p_seat", "p_after", "p_quit", "p_keep", "c1", "lesson1", "needle", "classic", "classic_win", "classic_lose", "goal", "end"]
jobs = [(os.path.join(SRC, f + ".mp4"), os.path.join(OUT, f + ".mp4")) for f in FILMS]
jobs.append((r"C:\Dev\Builds\Beta1\Reel\the_dream_reel_720p.mp4", os.path.join(OUT, "reel.mp4")))
total = 0
for src, dst in jobs:
    if not os.path.exists(src): print("missing", src); continue
    if src.endswith("_720p.mp4"):
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", src, "-c", "copy", "-movflags", "+faststart", dst], check=True)
    else:
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", src, "-vf", "scale=1280:720", "-c:v", "libx264", "-profile:v", "main", "-crf", "25",
                        "-preset", "slow", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "112k", "-movflags", "+faststart", dst], check=True)
    total += os.path.getsize(dst); print(f"{os.path.basename(dst):20} {os.path.getsize(dst) / 1e6:5.1f} MB")
print(f"films: {total / 1e6:.1f} MB")
