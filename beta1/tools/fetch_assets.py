# Download CC0 assets from Poly Haven into C:\Dev\Beta1Assets\polyhaven\<id>\ and record each one in beta1/ASSETS.md.
#   python beta1/tools/fetch_assets.py tex:leather_red_02 model:metal_stool_02 hdri:modern_evening_street ...
# Textures: 1k JPG (diffuse, GL normal, roughness, metal/AO when there). Models: 1k glTF with its textures. HDRIs: 2k .hdr.
import json, os, sys, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STORE = r"C:\Dev\Beta1Assets\polyhaven"
LEDGER = os.path.join(ROOT, "ASSETS.md")
UA = {"User-Agent": "ConfirmedFire-TheDream-beta1/1.0"}


def get(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=120).read()


def save(url, path):
    if os.path.exists(path): return
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = get(url)
    with open(path, "wb") as f: f.write(data)


def fetch(kind, aid):
    files = json.loads(get(f"https://api.polyhaven.com/files/{aid}"))
    info = json.loads(get(f"https://api.polyhaven.com/info/{aid}"))
    out = os.path.join(STORE, aid)
    if kind == "hdri":
        save(files["hdri"]["2k"]["hdr"]["url"], os.path.join(out, f"{aid}_2k.hdr"))
    elif kind == "tex":
        for m in ("Diffuse", "nor_gl", "Rough", "Metal", "AO"):
            src = m if m in files else ("coll1" if m == "Diffuse" and "coll1" in files else None)   # some ship colour as coll1
            if src and "1k" in files[src]:
                f = files[src]["1k"].get("jpg") or files[src]["1k"].get("png")
                name = os.path.basename(f["url"]).replace("_coll1_", "_diff_")
                save(f["url"], os.path.join(out, name))
    else:
        g = files["gltf"]["1k"]["gltf"]
        save(g["url"], os.path.join(out, os.path.basename(g["url"])))
        for rel, inc in g.get("include", {}).items(): save(inc["url"], os.path.join(out, rel))
    authors = ", ".join(info.get("authors", {}).keys())
    return f"| {aid} | {kind} | Poly Haven, https://polyhaven.com/a/{aid} | {authors} | CC0 |"


def main():
    ledger = open(LEDGER, encoding="utf-8").read() if os.path.exists(LEDGER) else (
        "# Beta 1.0 assets\n\nEvery third-party asset in the beta1 project, with its source, author and licence. Sources live in\n"
        "`C:\\Dev\\Beta1Assets` (outside git); CC0 sources can be downloaded again with `tools/fetch_assets.py`.\n\n"
        "| asset | kind | source | author | licence |\n|---|---|---|---|---|\n")
    for arg in sys.argv[1:]:
        kind, aid = arg.split(":", 1)
        if f"| {aid} |" in ledger: print("have", aid); fetch(kind, aid); continue
        row = fetch(kind, aid); ledger += row + "\n"; print(row)
    with open(LEDGER, "w", encoding="utf-8") as f: f.write(ledger)


if __name__ == "__main__":
    main()
