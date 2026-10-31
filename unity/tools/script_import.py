# Turn an edited script (Logs/script.md from Dream > Export script, or the same text pasted back from a doc) into
# Resources/Dream/script_edits.json, which DB.Load lays over the scenes: a line's text, its speaker's display name,
# and choice labels, matched by scene id and line order (the numbers are ignored, so renumbering is harmless).
#   python3 script_import.py <script.md> <out script_edits.json>
import re, sys, json
src, out = sys.argv[1], sys.argv[2]
edits, scene, beats = {}, None, None
head = re.compile(r"^#{1,3}\s*([A-Za-z0-9_]+)\s*(?:—|-|–)")          # ## p_office — Prologue ...
line = re.compile(r"^\s*(?:\d+[.)]|[-*•])?\s*(.+?)\s*\[([A-Za-z:_0-9]+)\]\s*((?:\([a-z]+\)\s*)*):\s*(.*)$")   # NAME [sp] (text) (mood): words
choice = re.compile(r"^\s*(?:[-*•]|\d+[.)])?\s*(?:>\s*)?Choice:\s*(.*?)(?:\s*(?:→|->)\s*([A-Za-z0-9_]+))?(?:\s*\|\s*(.+?)\s*\[([A-Za-z:_0-9]+)\]\s*:\s*(.*))?\s*$")
def words(t):
    t = re.sub(r"\s*\{[^{}]*\}\s*$", "", t)   # the shot list in braces after a line is not part of the line
    return t.strip().replace(" / ", "\n").replace("/ ", "\n").strip()
def unescape(t):   # a doc export escapes markdown punctuation: p\_office, \[slick\]
    for a, b in (("\\_", "_"), ("\\[", "["), ("\\]", "]"), ("\\*", "*"), ("\\#", "#"), ("\\>", ">")): t = t.replace(a, b)
    return t
for raw in open(src, encoding="utf-8"):
    s = unescape(raw.rstrip("\n"))
    m = head.match(s)
    if m: scene = m.group(1); beats = edits.setdefault(scene, []); continue
    if scene is None or not s.strip(): continue
    m = choice.match(s)
    if m and beats:
        c = {"t": words(m.group(1))}
        if m.group(5) is not None: c["line"] = words(m.group(5)); c["who"] = m.group(3).strip()
        beats[-1].setdefault("choice", []).append(c); continue
    m = line.match(s)
    if m:
        name, sp, text = m.group(1).strip(), m.group(2), words(m.group(4))
        b = {"sp": sp, "t": text}
        if sp not in ("n", "you"): b["who"] = name
        beats.append(b)
n = sum(len(v) for v in edits.values())
json.dump(edits, open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=0)
print(f"{len(edits)} scenes, {n} lines -> {out}")
