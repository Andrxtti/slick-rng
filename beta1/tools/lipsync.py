# Mouth shapes for every spoken film line (S7 / task 10): Rhubarb Lip Sync 1.13.0 (MIT, official release, outside the repo in
# C:\Dev\Beta1Assets\tools\rhubarb) reads each clip with its line as the dialog hint and writes Preston Blair shapes A-H, X.
#   python beta1/tools/lipsync.py [--force]
# Writes Assets/Dream/FilmData/Voice/<voice>.mouth.txt ("seconds<TAB>shape" per change) next to each clip; Film/Face.cs plays
# them. Narrator cards have no face and are skipped. A clip is redone when it is newer than its mouth file.
import json, os, subprocess, sys, tempfile
from concurrent.futures import ThreadPoolExecutor

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FILM = os.path.join(ROOT, "Assets", "Dream", "FilmData", "Film")
VOICE = os.path.join(ROOT, "Assets", "Dream", "FilmData", "Voice")
RHUBARB = r"C:\Dev\Beta1Assets\tools\rhubarb\Rhubarb-Lip-Sync-1.13.0-Windows\rhubarb.exe"
FACELESS = {"narrator", "card", "n"}


def one(L, force):
    wav = os.path.join(VOICE, L["voice"] + ".wav"); out = os.path.join(VOICE, L["voice"] + ".mouth.txt")
    if not os.path.exists(wav): return "missing"
    if not force and os.path.exists(out) and os.path.getmtime(out) >= os.path.getmtime(wav): return "kept"
    with tempfile.NamedTemporaryFile("w", suffix=".txt", delete=False, encoding="utf-8") as d:
        d.write(L["text"].replace("\n", " ").replace("/", " "))
    r = subprocess.run([RHUBARB, "-f", "tsv", "--extendedShapes", "GHX", "-d", d.name, "-o", out, wav], capture_output=True)
    os.remove(d.name)
    if r.returncode: return "error " + r.stderr.decode(errors="replace")[-200:]
    return "made"


def main():
    force = "--force" in sys.argv
    lines, seen = [], set()
    for fn in sorted(f for f in os.listdir(FILM) if f.endswith(".json")):
        data = json.load(open(os.path.join(FILM, fn), encoding="utf-8"))
        for L in data if isinstance(data, list) else data["lines"]:
            if L.get("voice") and L.get("text") and L.get("who") not in FACELESS and L["voice"] not in seen:
                seen.add(L["voice"]); lines.append(L)
    with ThreadPoolExecutor(6) as ex:
        res = list(ex.map(lambda L: one(L, force), lines))
    for L, r in zip(lines, res):
        if r.startswith("error") or r == "missing": print(f"  {L['voice']}: {r}")
    print(f"{len(lines)} lines: {res.count('made')} made, {res.count('kept')} kept, {sum(r.startswith('error') for r in res)} errors")


if __name__ == "__main__":
    main()
