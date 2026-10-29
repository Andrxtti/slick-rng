# The reel's vignette shots as film scenes (FilmData/Film/reel_*.json), rendered with `render reel_casino ...`.
# The cut itself (order, titles, music, stings) is tools/reel_cut.py; story/SCRIPT.md has the plan.
#   python beta1/tools/reel_build.py
import json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Dream", "FilmData", "Film")


def line(n, who="card", shot="cam", subject="hero", hold=0, voice="", act="idle", text=""):
    return {"n": n, "who": who, "name": who.upper(), "shot": shot, "subject": subject, "act": act, "next": "", "text": text, "voice": voice, "hold": hold}


REEL = {
    "reel_casino": {"cast": [], "lines": [line(1, hold=5)]},
    "reel_vegas": {"cast": [], "lines": [line(1, hold=5)]},
    "reel_tv": {"cast": ["nick"], "lines": [line(1, hold=1.5), line(2, who="nick", shot="cu", subject="nick", voice="sting_06", act="talk")]},
    "reel_titan": {"cast": [], "lines": [line(1, hold=4.5)]},
    "reel_main": {"cast": ["bio", "you"], "lines": [line(1, hold=2.5), line(2, who="bio", shot="ecu", subject="bio", voice="sting_04", act="idle", hold=4)]},
    "reel_hall": {"cast": [], "lines": [line(1, hold=6.5)]},
}
for sid, sc in REEL.items():
    data = {"id": sid, "title": "reel", "place": sid, "time": "", "cast": sc["cast"], "lines": sc["lines"], "directions": []}
    json.dump(data, open(os.path.join(OUT, sid + ".json"), "w", encoding="utf-8"), indent=1)
print("reel scenes:", ", ".join(REEL))
