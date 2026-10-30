# Voice every line in FilmData/Film/*.json with Piper (offline neural TTS, LibriTTS multi-speaker model).
#   python beta1/tools/voice.py [--force]
# Writes Assets/Dream/FilmData/Voice/<voice>.wav (mono, 22.05 kHz) for each line that has no clip yet, or whose text
# changed (the text of every clip is kept in Voice/voices.json). Speaker settings come from the old project's
# unity/tools/voice.py, mapped to the new voice ids.
import json, os, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FILM = os.path.join(ROOT, "Assets", "Dream", "FilmData", "Film")
OUT = os.path.join(ROOT, "Assets", "Dream", "FilmData", "Voice")
PIPER = r"C:\Dev\Beta1Assets\tools\piper"
BIN = os.path.join(PIPER, "piper", "piper.exe")
MODEL = os.path.join(PIPER, "en-us-libritts-high.onnx")

# speaker id, pace (bigger = slower), pitch (semitones), gravel 0..1, warmth 0..1
CAST = {
    "slick": (27, 1.08, -1.0, 0.25, 0.3),    # low and cool, a little rasp, never hurried
    "you": (57, 1.0, 0.0, 0.0, 0.1),
    "lou": (43, 0.95, 0.5, 0.0, 0.0),        # Natso SweetLou: brisk, pleased with himself
    "tommy": (3, 1.0, -1.0, 0.45, 0.3),      # Tommy "The Shark": gravel
    "david": (52, 0.95, 0.0, 0.0, 0.1),      # David Pec: fast and warm
    "mustich": (41, 0.9, 1.0, 0.0, 0.0),     # Mustich: quick, smug, a touch high
    "gnomey": (6, 1.0, -1.0, 0.3, 0.2),      # Miles "The Snowplow" Gnomey
    "nick": (24, 0.92, 0.5, 0.0, 0.2),       # Nick "Baby Goat" Twogloves, the host
    "bio": (31, 1.15, -1.0, 0.2, 0.5),       # Bio "The Ghost": slow, measured, deep
    "dealer": (20, 1.0, 0.0, 0.0, 0.0),
    # the narrator: Kokoro, not Piper (tools/narrator.py, N2b: Steven 19:01, the pitch-shifted LibriTTS voice sounded
    # disguised); the Piper settings here are unused for him. No cast voice shifts more than 1 semitone (that's the effect).
    "narrator": (498, 1.0, 0.0, 0.0, 0.0),
}
CAST["card"] = CAST["n"] = CAST["narrator"]   # narrator cards and Narrator [n] lines
TRAILER = {"narrator", "card", "n"}


def speakable(t):
    t = t.replace("…", "...").replace("—", ", ").replace("–", ", ").replace("$", "$").replace("&", " and ")
    t = t.replace("Mustich", "Mustick").replace("BRO.", "Bro!").replace("YOU'RE", "you're").replace("STREAM", "stream")
    return " ".join(t.split())


def colour(src, dst, pitch, gravel, warmth, trailer=False):
    f = []
    if abs(pitch) >= 0.05:
        r = 2 ** (pitch / 12.0); f.append(f"asetrate=22050*{r:.5f},aresample=22050,atempo={1 / r:.5f}")
    if warmth >= 0.05: f.append(f"bass=g={3 * warmth:.2f}:f=150")
    if gravel >= 0.05: f.append(f"acompressor=threshold=-18dB:ratio=3,treble=g={-4 * gravel:.2f}:f=3500")
    # trim the silence Piper leaves (keep a little air), then level every line the same
    f.append("silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.08")
    f.append("areverse,silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.16,areverse")
    if trailer: f.append("acompressor=threshold=-20dB:ratio=3:attack=5:release=120,aecho=0.8:0.5:40:0.2")
    f.append("loudnorm=I=-18:TP=-2:LRA=7")
    r = subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", src, "-af", ",".join(f), "-ar", "22050", "-ac", "1", dst], capture_output=True)
    if r.returncode: print("ffmpeg:", r.stderr.decode()[-300:])


# ------------------------------------------------------------ lines outside the films
LINES = os.path.join(ROOT, "Assets", "Dream", "Resources", "Voice", "Lines")   # in the build: the game plays these
YARN = os.path.join(ROOT, "Assets", "Dream", "Resources", "Story")


def key(who, text):
    """Voice.Key in the game: FNV-1a over the UTF-16 code units of "who|text" (who lower-cased), as 8 hex digits"""
    h = 2166136261
    data = (who.lower() + "|" + text).encode("utf-16-le")
    for i in range(0, len(data), 2):
        h ^= data[i] | (data[i + 1] << 8); h = (h * 16777619) & 0xFFFFFFFF
    return f"{h:08x}"


def yarn_lines():
    """Phone texts (Name: words #text) in the sender's voice; plain narration lines in the narrator's. A {value} line
    stays silent (it is filled in at play time); #card and #hint lines are the choice card's UI, not spoken."""
    import re
    out = []
    for fn in sorted(f for f in os.listdir(YARN) if f.endswith(".yarn")):
        body = False
        for raw in open(os.path.join(YARN, fn), encoding="utf-8"):
            line = raw.strip()
            if line == "---": body = True; continue
            if line == "===": body = False; continue
            if not body or not line or line.startswith(("//", "<<", "->", "#")): continue
            tags = re.findall(r"#[\w:]+", line)
            text = re.sub(r"\s*#[\w:]+", "", line).strip()
            if "#card" in tags or "#hint" in tags or "{" in text: continue
            m = re.match(r"^([A-Za-z]+):\s*(.+)$", text)
            if "#text" in tags and m: who, words = m.group(1).lower(), m.group(2)
            elif "#text" not in tags and not m: who, words = "narrator", text
            else: continue
            if who not in CAST: print("  no voice for", who, "-", words[:40]); continue
            out.append({"who": who, "text": words, "voice": key(who, words), "_out": LINES})
    return out


def reel_cards():
    """the reel's title cards, read by the narrator (tools/reel_cut.py lays them over the cards)"""
    import re
    src = open(os.path.join(ROOT, "tools", "reel_cut.py"), encoding="utf-8").read()
    return [{"who": "narrator", "text": t, "voice": key("narrator", t), "_out": LINES}
            for t in re.findall(r'\("card", "([^"]+)"', src)]


# storyboards that ship voiced (their clips are in the first download, so only the ones this build reaches)
STORYBOARDS = {"c2_drive", "c2"}


def storyboard_lines():
    path = os.path.join(ROOT, "Assets", "Dream", "Resources", "Story", "scenes.json")
    if not os.path.exists(path): return []
    out = []
    for sc in json.load(open(path, encoding="utf-8"))["scenes"]:
        if sc["id"] not in STORYBOARDS or not sc["storyboard"]: continue
        for L in sc["lines"]:
            if L["who"] in CAST: out.append({"who": L["who"], "text": L["text"], "voice": key(L["who"], L["text"]), "_out": LINES})
    return out


def main():
    force = "--force" in sys.argv
    redo = set(sys.argv[sys.argv.index("--who") + 1].split(",")) if "--who" in sys.argv else set()   # re-voice these speakers
    os.makedirs(OUT, exist_ok=True); os.makedirs(LINES, exist_ok=True)
    log_path = os.path.join(OUT, "voices.json")
    done = json.load(open(log_path, encoding="utf-8")) if os.path.exists(log_path) else {}
    lines = []
    for fn in sorted(f for f in os.listdir(FILM) if f.endswith(".json")):
        data = json.load(open(os.path.join(FILM, fn), encoding="utf-8"))
        for L in data if isinstance(data, list) else data["lines"]:
            if L.get("voice") and L.get("text"): lines.append(L)   # reel shots reuse sting voices without text
    lines += yarn_lines() + reel_cards() + storyboard_lines()
    todo = [L for L in lines if force or L["who"] in redo or done.get(L["voice"]) != L["text"] or not os.path.exists(os.path.join(L.get("_out", OUT), L["voice"] + ".wav"))]
    print(f"{len(lines)} lines, {len(todo)} to voice")
    tmp = os.path.join(PIPER, "tmp"); os.makedirs(tmp, exist_ok=True)
    import narrator
    for L in [L for L in todo if L["who"] in TRAILER]:
        narrator.say(L["text"], os.path.join(L.get("_out", OUT), L["voice"] + ".wav"))
        done[L["voice"]] = L["text"]
        print(f"  {L['voice']:16} {L['who']:8} {L['text'][:60]}")
    todo = [L for L in todo if L["who"] not in TRAILER]
    for pace in sorted({CAST[L["who"]][1] for L in todo}):
        jobs = [L for L in todo if CAST[L["who"]][1] == pace]
        inp = "\n".join(json.dumps({"text": speakable(L["text"]), "speaker_id": CAST[L["who"]][0],
                                    "output_file": os.path.join(tmp, L["voice"] + ".wav")}) for L in jobs) + "\n"
        r = subprocess.run([BIN, "--model", MODEL, "--json-input", "--length_scale", str(pace), "--sentence_silence", "0.3"],
                           input=inp.encode(), capture_output=True)
        if r.returncode: print(r.stderr.decode()[-1500:]); sys.exit(1)
        for L in jobs:
            _, _, pitch, gravel, warmth = CAST[L["who"]]
            colour(os.path.join(tmp, L["voice"] + ".wav"), os.path.join(L.get("_out", OUT), L["voice"] + ".wav"), pitch, gravel, warmth, L["who"] in TRAILER)
            done[L["voice"]] = L["text"]
            print(f"  {L['voice']:16} {L['who']:8} {L['text'][:60]}")
    json.dump(done, open(log_path, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    print("ok")


if __name__ == "__main__":
    main()
