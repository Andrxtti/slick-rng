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
BRANDS = {"rescue": "Riverside Animal Rescue", "kennel": "Hillcrest Kennels", "sole": "Sole Society", "kestrelfoot": "Kestrel Footwear"}

# the closet split into top / bottom / shoes (tops are the old "outfit" items; models: unity/tools/art/clothes.py)
BOTTOMS = [
    ("khakis", "Clearance-rack khakis", "Where it all started", "standard", "valuemart", "They came with the office fit. Pleats and all."),
    ("jeans", "Straight-leg jeans", "Where it all started", "standard", "valuemart", "The pair you wore to the first game. Still fit."),
    ("blackjeans", "Black skinny jeans", "Play 3 tournaments", "standard", "drift", "Dark enough for a card room, tight enough to regret after dinner."),
    ("sweats", "Grey sweats", "Play 1 tournament", "standard", "drift", "Online grinder uniform. Nobody sees below the webcam."),
    ("joggers", "Side-stripe joggers", "Decision streak of 3", "rare", "drift", "The stripe says you're moving even when you're sitting."),
    ("trackpants", "Retro track pants", "Peak bankroll of $10,000", "rare", "drift", "Matches the jacket. Swish swish to the felt."),
    ("shorts", "Drift shorts", "Win a cash session", "rare", "drift", "Vegas in July. You tried pants once."),
    ("cargo", "Cargo shorts", "Play 5 tournaments", "standard", "valuemart", "Eleven pockets. One of them has your player's card."),
    ("slacks", "Charcoal slacks", "Reach chapter 2", "elite", "castellane", "Creased, pressed, serious."),
    ("plaidpants", "Plaid trousers", "Win a circuit ring", "elite", "castellane", "Loud legs, quiet hands."),
    ("tuxpants", "Tuxedo trousers", "Reach the Main Event final table", "iconic", "noir", "Satin stripe. Only come out for the big one."),
]
SHOES = [
    ("sneakers", "White sneakers", "Where it all started", "standard", "valuemart", "Clean on day one. That lasted a week."),
    ("runners", "Drift runners", "Play 3 tournaments", "standard", "drift", "Made for running. Used for walking to the cage."),
    ("highs", "Red-stripe high-tops", "Decision streak of 3", "rare", "sole", "Canvas classics with the red line."),
    ("slides", "Gold-strap slides", "Win a cash session", "rare", "sole", "Pool day footwear. Also final-table footwear, apparently."),
    ("loafers", "Tan loafers", "Reach chapter 2", "elite", "kestrelfoot", "No socks. Lots of confidence."),
    ("dress", "Black dress shoes", "Peak bankroll of $50,000", "elite", "castellane", "Polished enough to see your cards in."),
    ("boots", "Leather boots", "Win a circuit ring", "elite", "kestrelfoot", "For the long walk to the podium."),
    ("icy", "Icy-blue high-tops", "Peak bankroll of $1,000,000", "legendary", "sole", "Frosted. Limited. The felt gets cold when you walk up."),
]

def apply(d):
    items = d["DR_ITEMS"]; have = {(i.get("slot"), i.get("id")) for i in items}
    for (pid, name, req, grade, brand, story) in PETS:
        if ("pet", pid) not in have: items.append({"slot": "pet", "id": pid, "name": name, "req": req, "test": "__fn"})
        d["DR_META"]["pet:" + pid] = [grade, brand, story]
    for slot, rows in (("bottom", BOTTOMS), ("shoes", SHOES)):
        for (iid, name, req, grade, brand, story) in rows:
            if (slot, iid) not in have: items.append({"slot": slot, "id": iid, "name": name, "req": req, "test": "__fn"})
            d["DR_META"][slot + ":" + iid] = [grade, brand, story]
    # slots and closet categories in closet order: top, bottom, shoes, then the rest
    for sl, label in (("outfit", "Top"), ("bottom", "Bottom"), ("shoes", "Shoes"), ("pet", "Pet")):
        found = [x for x in d["DR_SLOTS"] if x[0] == sl]
        if found: found[0][1] = label
        else: d["DR_SLOTS"].append([sl, label])
    cats = [c for c in d["DR_CATS"] if c[0] not in ("bottom", "shoes", "pet")]
    i = max(k for k, c in enumerate(cats) if c[2] == "outfit") + 1
    cats[i:i] = [["bottom", "Bottoms", "bottom"], ["shoes", "Shoes", "shoes"]]
    cats.append(["pet", "Pets", "pet"])
    d["DR_CATS"] = cats
    d["DR_CATS"] = [[c[0], {"shirts": "Tops", "street": "Streetwear", "jackets": "Jackets", "formal": "Formal"}.get(c[0], c[1]), c[2]] for c in d["DR_CATS"]]
    d["DR_BRANDS"].update(BRANDS)
    return d

if __name__ == "__main__":
    path = sys.argv[1]; full = json.load(open(path))
    apply(full["data"])
    json.dump(full, open(path, "w"), separators=(",", ":"))
    print("ok", [c[0] for c in full["data"]["DR_CATS"]], len(full["data"]["DR_ITEMS"]), "items")
