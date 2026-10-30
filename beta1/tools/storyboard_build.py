# Storyboards (Milestone A3, THE BASE): every scene of the one script plays in the game, film or not.
#   python beta1/tools/storyboard_build.py
# Reads unity/docs/SCRIPT_ALL.md from origin/unity (Steven's scene tree, exported by the cloud; a copy is kept in
# beta1/story/SCRIPT_ALL.md) and writes Assets/Dream/Resources/Story/scenes.json for Game/Storyboard.cs: each scene's
# place, its set still, and its spoken lines. A scene plays as a storyboard when it has no published film, or when Steven
# EDITED it and its lines no longer match the film's captions (that film is listed as "needs re-render").
import json, os, re, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = os.path.dirname(ROOT)
FILMS = os.path.join(ROOT, "Assets", "StreamingAssets", "Films")
OUT = os.path.join(ROOT, "Assets", "Dream", "Resources", "Story", "scenes.json")
COPY = os.path.join(ROOT, "story", "SCRIPT_ALL.md")
# the scene tree's speaker keys -> this game's cast ids (voices, portraits)
WHO = {"n": "narrator", "derek": "lou", "rosa": "gnomey", "trent": "mustich", "lou": "tommy", "host": "nick", "ghost": "bio", "mom": "david"}
SETS = [("floor 34", "office"), ("office", "office"), ("diner", "diner"), ("card room", "cardroom"), ("apartment", "home")]


def source():
    r = subprocess.run(["git", "-C", REPO, "show", "origin/unity:unity/docs/SCRIPT_ALL.md"], capture_output=True)
    if r.returncode == 0:
        text = r.stdout.decode("utf-8")
        os.makedirs(os.path.dirname(COPY), exist_ok=True)
        open(COPY, "w", encoding="utf-8", newline="\n").write(text)
        return text
    print("origin/unity not readable, using the copy:", r.stderr.decode()[-200:])
    return open(COPY, encoding="utf-8").read()


def norm(t):
    return re.sub(r"[^a-z0-9]+", " ", t.lower()).strip()


def main():
    text = source()
    scenes, cur = [], None
    for line in text.splitlines():
        m = re.match(r"^## (\S+) — (.+?)(?: \(([^)]*)\))?\s*(<!--.*EDITED.*-->)?\s*$", line)
        if m:
            place = m.group(3) or ""
            cur = {"id": m.group(1), "title": m.group(2), "place": place, "edited": bool(m.group(4)),
                   "set": next((s for k, s in SETS if k in place.lower()), ""), "lines": []}
            scenes.append(cur); continue
        if cur is None: continue
        if "EDITED" in line and line.strip().startswith("<!--"): cur["edited"] = True; continue
        m = re.match(r"^\d+\.\s+(.+?)\s+\[([^\]]+)\](?:\s+\(([^)]*)\))?:\s*(.*?)(?:\s+\{[^}]*\})?\s*$", line)
        if m:
            name, key, mode, words = m.group(1), m.group(2), (m.group(3) or ""), m.group(4)
            key = key.split(":")[-1]
            who = WHO.get(key, key)
            cur["lines"].append({"who": who, "name": "" if who == "narrator" else name, "mode": mode, "text": words.replace(" / ", "\n")})
    rerender = []
    for sc in scenes:
        film = os.path.join(FILMS, sc["id"] + ".mp4")
        sc["film"] = os.path.exists(film)
        sc["storyboard"] = not sc["film"]
        cap = os.path.join(FILMS, sc["id"] + ".json")
        if sc["film"] and sc["edited"] and os.path.exists(cap):
            said = [norm(c["text"]) for c in json.load(open(cap, encoding="utf-8")).get("lines", [])]
            if said != [norm(l["text"]) for l in sc["lines"]]:
                sc["storyboard"] = True; rerender.append(sc["id"])
    json.dump({"scenes": scenes}, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=0)
    print(f"{len(scenes)} scenes, {sum(s['storyboard'] for s in scenes)} storyboards, {sum(s['edited'] for s in scenes)} edited")
    print("needs re-render:", ", ".join(rerender) or "none")


if __name__ == "__main__":
    main()
