# SCRIPT.md -> one JSON per scene in Assets/Dream/FilmData/Film/, plus the stings list for the reel and the voicer.
#   python beta1/tools/script_build.py
import json, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "story", "SCRIPT.md")
OUT = os.path.join(ROOT, "Assets", "Dream", "FilmData", "Film")

LINE = re.compile(r"^(\d+)\.\s+(.+?)\s+\[(\w+)\]\s*(?:\((.*?)\))?:\s*(.*)$")
STING = re.compile(r"^-\s+(.+?)\s+\[(\w+)\]:\s*(.+)$")
HEAD = re.compile(r"^## (\w+) — (.+)$")


def parse_cue(cue):
    shot, subject, act, nxt, hold = "two", "", "idle", "", 0.0
    if cue:
        parts = [p.strip() for p in cue.split(";")]
        s = parts[0].split()
        shot = s[0]; subject = s[1] if len(s) > 1 else ""
        if len(parts) > 1:
            a = [x.strip() for x in parts[1].split(">")]
            act = a[0]; nxt = a[1] if len(a) > 1 else ""
        if len(parts) > 2 and parts[2].endswith("s"): hold = float(parts[2][:-1])   # "2.5s": hold the shot this long
    return shot, subject, act, nxt, hold


def build():
    scenes, stings, cur = {}, [], None
    for raw in open(SRC, encoding="utf-8"):
        line = raw.rstrip("\n")
        h = HEAD.match(line)
        if h:
            cur = None if h.group(1) in ("How", "Stings", "The") else {"id": h.group(1), "title": h.group(2), "lines": [], "directions": []}
            if cur: scenes[cur["id"]] = cur
            continue
        if line.startswith("## Stings"): cur = "stings"; continue
        if line.startswith("## "): cur = None; continue
        if cur == "stings":
            m = STING.match(line)
            if m: stings.append({"who": m.group(2), "name": m.group(1), "text": m.group(3), "voice": f"sting_{len(stings) + 1:02d}"})
            continue
        if not isinstance(cur, dict): continue
        m = re.match(r"^place: (\w+) · time: (.+?) · cast: (.+)$", line)
        if m: cur.update(place=m.group(1), time=m.group(2), cast=[c.strip() for c in m.group(3).split(",")]); continue
        if line.startswith("{") and line.endswith("}"):
            cur["directions"].append({"after": len(cur["lines"]), "text": line[1:-1]}); continue
        m = LINE.match(line)
        if m:
            n, name, who, cue, text = m.groups()
            shot, subject, act, nxt, hold = parse_cue(cue)
            text = "" if text.strip() == "-" else text.strip().replace(" / ", "\n")   # " / " breaks a card's line
            cur["lines"].append({"n": int(n), "who": who, "name": name, "shot": shot, "subject": subject, "act": act,
                                 "next": nxt, "hold": hold, "text": text, "voice": "" if who == "card" else f"{cur['id']}_{int(n):02d}"})
    os.makedirs(OUT, exist_ok=True)
    for sid, sc in scenes.items():
        with open(os.path.join(OUT, sid + ".json"), "w", encoding="utf-8") as f: json.dump(sc, f, indent=1, ensure_ascii=False)
    with open(os.path.join(OUT, "stings.json"), "w", encoding="utf-8") as f: json.dump(stings, f, indent=1, ensure_ascii=False)
    voiced = sum(1 for s in scenes.values() for l in s["lines"] if l["voice"]) + len(stings)
    print(f"{len(scenes)} scenes, {voiced} voiced lines, {len(stings)} stings -> {OUT}")
    for sid, sc in scenes.items(): print(f"  {sid}: {len(sc['lines'])} lines, place {sc.get('place', '-')}")


if __name__ == "__main__":
    build()
