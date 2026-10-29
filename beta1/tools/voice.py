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
    "slick": (27, 1.08, -1.5, 0.25, 0.3),    # low and cool, a little rasp, never hurried
    "you": (57, 1.0, 0.0, 0.0, 0.1),
    "lou": (43, 0.95, 0.5, 0.0, 0.0),        # Natso SweetLou: brisk, pleased with himself
    "tommy": (3, 1.0, -2.0, 0.45, 0.3),      # Tommy "The Shark": gravel
    "david": (52, 0.95, 0.0, 0.0, 0.1),      # David Pec: fast and warm
    "mustich": (41, 0.9, 1.0, 0.0, 0.0),     # Mustich: quick, smug, a touch high
    "gnomey": (6, 1.0, -1.0, 0.3, 0.2),      # Miles "The Snowplow" Gnomey
    "nick": (24, 0.92, 0.5, 0.0, 0.2),       # Nick "Baby Goat" Twogloves, the host
    "bio": (31, 1.15, -2.5, 0.2, 0.5),       # Bio "The Ghost": slow, measured, deep
    "dealer": (20, 1.0, 0.0, 0.0, 0.0),
}


def speakable(t):
    t = t.replace("…", "...").replace("—", ", ").replace("–", ", ").replace("$", "$").replace("&", " and ")
    t = t.replace("Mustich", "Mustick").replace("BRO.", "Bro!").replace("YOU'RE", "you're").replace("STREAM", "stream")
    return " ".join(t.split())


def colour(src, dst, pitch, gravel, warmth):
    f = []
    if abs(pitch) >= 0.05:
        r = 2 ** (pitch / 12.0); f.append(f"asetrate=22050*{r:.5f},aresample=22050,atempo={1 / r:.5f}")
    if warmth >= 0.05: f.append(f"bass=g={3 * warmth:.2f}:f=150")
    if gravel >= 0.05: f.append(f"acompressor=threshold=-18dB:ratio=3,treble=g={-4 * gravel:.2f}:f=3500")
    # trim the silence Piper leaves (keep a little air), then level every line the same
    f.append("silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.08")
    f.append("areverse,silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.16,areverse")
    f.append("loudnorm=I=-18:TP=-2:LRA=7")
    r = subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", src, "-af", ",".join(f), "-ar", "22050", "-ac", "1", dst], capture_output=True)
    if r.returncode: print("ffmpeg:", r.stderr.decode()[-300:])


def main():
    force = "--force" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    log_path = os.path.join(OUT, "voices.json")
    done = json.load(open(log_path, encoding="utf-8")) if os.path.exists(log_path) else {}
    lines = []
    for fn in sorted(f for f in os.listdir(FILM) if f.endswith(".json")):
        data = json.load(open(os.path.join(FILM, fn), encoding="utf-8"))
        for L in data if isinstance(data, list) else data["lines"]:
            if L.get("voice") and L.get("text"): lines.append(L)   # reel shots reuse sting voices without text
    todo = [L for L in lines if force or done.get(L["voice"]) != L["text"] or not os.path.exists(os.path.join(OUT, L["voice"] + ".wav"))]
    print(f"{len(lines)} lines, {len(todo)} to voice")
    tmp = os.path.join(PIPER, "tmp"); os.makedirs(tmp, exist_ok=True)
    for pace in sorted({CAST[L["who"]][1] for L in todo}):
        jobs = [L for L in todo if CAST[L["who"]][1] == pace]
        inp = "\n".join(json.dumps({"text": speakable(L["text"]), "speaker_id": CAST[L["who"]][0],
                                    "output_file": os.path.join(tmp, L["voice"] + ".wav")}) for L in jobs) + "\n"
        r = subprocess.run([BIN, "--model", MODEL, "--json-input", "--length_scale", str(pace), "--sentence_silence", "0.3"],
                           input=inp.encode(), capture_output=True)
        if r.returncode: print(r.stderr.decode()[-1500:]); sys.exit(1)
        for L in jobs:
            _, _, pitch, gravel, warmth = CAST[L["who"]]
            colour(os.path.join(tmp, L["voice"] + ".wav"), os.path.join(OUT, L["voice"] + ".wav"), pitch, gravel, warmth)
            done[L["voice"]] = L["text"]
            print(f"  {L['voice']:16} {L['who']:8} {L['text'][:60]}")
    json.dump(done, open(log_path, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    print("ok")


if __name__ == "__main__":
    main()
