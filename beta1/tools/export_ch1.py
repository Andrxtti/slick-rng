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
events = [e for e in data["DR_EVENTS"] if e.get("ch") in (0, 1) and not e.get("online") and e.get("venue") == "cardroom"]
fields = sorted({str(e["field"]) for e in events if "field" in e})
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
}
json.dump(out, open(os.path.join(OUT, "ch1.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
print(f"ch1.json: {len(events)} events ({', '.join(e['id'] for e in events)}), payouts for fields {fields}, {len(out['labels'])} labels")
