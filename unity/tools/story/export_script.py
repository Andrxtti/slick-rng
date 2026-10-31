# Writes the whole story as one script (unity/docs/SCRIPT_ALL.md) from the scene tree's data, with Steven's saved edits
# (the artifact's `edits` collection, saved by ArtifactData to a folder of JSON files) laid over the original lines.
# Usage: python3 export_script.py data.json <edits dir or -> out.md
import json, os, sys
D = json.load(open(sys.argv[1])); SC = D["scenes"]
ED = {}
if sys.argv[2] != "-" and os.path.isdir(sys.argv[2]):
    for dp, _, fs in os.walk(sys.argv[2]):
        for f in fs:
            if f.endswith(".json"):
                j = json.load(open(os.path.join(dp, f))); v = j.get("data", j)
                if v.get("lines"): ED[f[:-5]] = v
def line(n, l):
    tag = " (text)" if l.get("text") else (f" ({l['tone']})" if l.get("tone") else "")
    shot = f"  {{{l['shot']}}}" if l.get("shot") else ""
    out = [f"{n}. {l['who']} [{l['sp']}]{tag}: {l['t']}{shot}"]
    for c in l.get("choices", []):
        s = f"    - Choice: {c['c']}"
        if c.get("go"): s += f" → {c['go']}"
        if c.get("r"): s += f" | {c['r']['who']} [{c['r']['sp']}]: {c['r']['t']}"
        out.append(s)
    return out
order = []
def walk(nodes):
    for n in nodes:
        if n["t"] == "scene" and n["id"] not in order: order.append(n["id"])
        if n["t"] == "fork":
            for b in n["branches"]: walk(b["nodes"])
for ch in D["chapters"]: walk(ch["nodes"])
for k in SC:
    if k not in order: order.append(k)
out = ["# The Dream — the full script (exported from Steven's scene tree)", "",
       "Generated. Do not hand-edit: Steven edits in the scene tree; scenes marked EDITED carry his saved changes.", ""]
edited = []
for k in order:
    s = SC[k]; e = ED.get(k); lines = e["lines"] if e else s["lines"]
    if e: edited.append(k)
    out.append(f"## {k} — {s['title']} ({s.get('place') or s.get('set') or ''}){'  <!-- EDITED by Steven ' + e.get('at','') + ' -->' if e else ''}")
    out.append("")
    for i, l in enumerate(lines): out += line(i + 1, l)
    out.append("")
open(sys.argv[3], "w").write("\n".join(out))
print(f"{len(order)} scenes, edited: {edited}")
