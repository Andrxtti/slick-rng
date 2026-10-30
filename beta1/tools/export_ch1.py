# What Chapter 1 needs from the old game's data, and the hand library itself (BETA1 §5).
#   python beta1/tools/export_ch1.py
# - spots.json is copied byte for byte from unity/Assets/Dream/Resources/Dream/spots.json (never regenerated).
# - ch1.json: grade labels, the stage -> library map, hand classes, payouts for the Chapter 1 fields, the prologue and
#   Chapter 1 live card-room events, and the Chapter 1 goal. Taken from unity/.../dream_data.json (read only).
import hashlib, json, os, shutil

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OLD = os.path.join(os.path.dirname(ROOT), "unity", "Assets", "Dream", "Resources", "Dream")
OUT = os.path.join(ROOT, "Assets", "Dream", "Resources", "Data")
os.makedirs(OUT, exist_ok=True)

src, dst = os.path.join(OLD, "spots.json"), os.path.join(OUT, "spots.json")
shutil.copyfile(src, dst)
h = [hashlib.sha256(open(p, "rb").read()).hexdigest() for p in (src, dst)]
assert h[0] == h[1]
print("spots.json copied, sha256", h[0][:16])

d = json.load(open(os.path.join(OLD, "dream_data.json"), encoding="utf-8"))
data = d["data"]
# Chapter 1's events as Steven's Unity game ran them (old Story.cs ApplyOverrides): the free Biscayne Championship, the
# Winn Nightly $110, CF Tilt's $55 Global Rush (online), the Biscayne Classic over two days (Day 1 bags 40 for Day 2,
# which pays the whole 212-player prize pool), and the Comeback Freeroll when the roll runs low.
ev = {e["id"]: dict(e) for e in data["DR_EVENTS"]}
nightly = dict(ev["nightly"], name="The Winn Nightly $110", buyin=110, field=60, turbo=False, blurb="Biscayne's nightly. Sixty players, deep enough to actually play.")
glob = dict(ev["tilt11"], id="glob55", name="CF $55 Global Rush", buyin=55, field=1104, turbo=False, blurb="CF Tilt's nightly flagship. Players from everywhere, a real prize pool.")
day1 = dict(ev["classic"], name="Biscayne Classic · Day 1", kind="sat", seats=40, target="classic2", stages=["early", "middle"], perS=[4, 3],
            blurb="The big one at Biscayne, over two days. Survive Saturday and bag your chips for Sunday. Mustich wants it too.")
day1.pop("title", None); day1.pop("boss", None)
day2 = dict(ev["classic"], id="classic2", name="Biscayne Classic · Day 2", field=40, reentry=False, free=True, stages=["bubble", "deep", "final"], perS=[2, 2, 3],
            blurb="Forty players come back Sunday. Thirty-two get paid. One gets the trophy.")
free = dict(ev["freeroll"], prizeBase=1)
local = dict(ev["local"], prizeBase=89)   # Test Beta 1.1: the free Championship pays about $2,500 for first (was $7,000)
# Chapter 2, the Circuit at Silver Strike (Milestone B): the old game's events as they were
ch2 = [dict(ev[i]) for i in ("sat1700", "c25", "ring400", "ring600", "tilt55", "ring1700")]
events = [local, nightly, glob, day1, day2, free] + ch2
fields = sorted({str(e["field"]) for e in events if "field" in e} | {"212"})
ch = data["DR_CHAPTERS"] if "DR_CHAPTERS" in data else None
out = {
    "labels": data["DR_LABELS"],
    "stage": data["DR_STAGE"],
    "classes": d["classes"],
    "payouts": {f: d["payouts"][f] for f in fields if f in d["payouts"]},
    "events": events,
    "hands": data.get("DR_HANDS"), "cashHands": data.get("DR_CASH_HANDS"),
    "goal": {"bankroll": 10000, "title": "room", "text": "Win the Biscayne Classic or build a $10,000 bankroll"},
    "start": {"bankroll": 2000},
    "legends": [l for l in data["DR_LEGENDS"] if l.get("ch") == 2],
}
# US spelling in everything the player reads (Steven, 2026-10-29: "favourite = favorite"); the old data is British
US = {"favourite": "favorite", "colour": "color", "centre": "center", "realise": "realize", "behaviour": "behavior",
      "grey": "gray", "theatre": "theater", "cheque": "check", "neighbour": "neighbor", "honour": "honor", "travelling": "traveling"}
def us(v):
    if isinstance(v, str):
        for k, w in US.items(): v = v.replace(k, w).replace(k.capitalize(), w.capitalize())
        return v
    if isinstance(v, list): return [us(x) for x in v]
    if isinstance(v, dict): return {k: us(x) for k, x in v.items()}
    return v
out = us(out)
json.dump(out, open(os.path.join(OUT, "ch1.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
print(f"ch1.json: {len(events)} events ({', '.join(e['id'] for e in events)}), payouts for fields {fields}, {len(out['labels'])} labels")
