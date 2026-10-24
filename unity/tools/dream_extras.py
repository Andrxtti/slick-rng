# Additions the Unity build makes on top of the web game's data (dream_data.json), applied by export_dream.py and
# runnable on its own:  python3 unity/tools/dream_extras.py unity/Assets/Dream/Resources/Dream/dream_data.json
# Idempotent: running it twice changes nothing.
import json, sys

# pets: unlockables that live in your apartment (models: unity/tools/art/pets.py, rules: Career.cs)
PETS = [
    ("cat", "Mochi, the alley cat", "Play 5 tournaments", "standard", "rescue", "Found you behind the laundromat. Lives on your bed now. Your bed."),
    ("pup", "Biscuit, the rescue pup", "Win your first title", "rare", "rescue", "Half lab, half something. All good boy. Follows you everywhere."),
    ("frenchie", "Tank, the French bulldog", "Win a circuit ring", "elite", "kennel", "Snores louder than the dryers downstairs. Worth it."),
    ("doberman", "Duke, the Doberman", "Peak bankroll of $1,000,000", "legendary", "kennel", "Sleek, loyal and very good at looking expensive."),
]
BRANDS = {"rescue": "Riverside Animal Rescue", "kennel": "Hillcrest Kennels"}

def apply(d):
    items = d["DR_ITEMS"]; have = {(i.get("slot"), i.get("id")) for i in items}
    for (pid, name, req, grade, brand, story) in PETS:
        if ("pet", pid) not in have: items.append({"slot": "pet", "id": pid, "name": name, "req": req, "test": "__fn"})
        d["DR_META"]["pet:" + pid] = [grade, brand, story]
    if not any(s[0] == "pet" for s in d["DR_SLOTS"]): d["DR_SLOTS"].append(["pet", "Pet"])
    if not any(c[0] == "pet" for c in d["DR_CATS"]): d["DR_CATS"].append(["pet", "Pets", "pet"])
    d["DR_BRANDS"].update(BRANDS)
    return d

if __name__ == "__main__":
    path = sys.argv[1]; full = json.load(open(path))
    apply(full["data"])
    json.dump(full, open(path, "w"), separators=(",", ":"))
    print("ok", [i["id"] for i in full["data"]["DR_ITEMS"] if i.get("slot") == "pet"])
