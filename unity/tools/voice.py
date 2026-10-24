# Voice the story with Piper (offline neural TTS) and the LibriTTS multi-speaker voice: one clip per line.
#   python3 voice.py <voice_lines.json from Dream > Export voice lines> <out dir> [--piper <dir with piper/ and the .onnx>]
# Writes v_<key>.wav (22.05 kHz mono) for every line that doesn't have one yet; the key is MD5("sp|text")[:12],
# the same hash Voice.Key computes in the game. Each character has a speaker and a pace; legends draw from a pool.
import json, os, sys, subprocess, hashlib, zlib, wave, numpy as np, shutil
SRC, OUT = sys.argv[1], sys.argv[2]
PIPER = sys.argv[sys.argv.index("--piper") + 1] if "--piper" in sys.argv else os.path.expanduser("~/piper")
MODEL = os.path.join(PIPER, "en-us-libritts-high.onnx"); BIN = os.path.join(PIPER, "piper", "piper")
os.makedirs(OUT, exist_ok=True)

# speaker id, pace (1 = normal, bigger = slower), then the colour of the voice applied afterwards with ffmpeg:
# pitch in semitones (negative = deeper), gravel 0..1 (a little growl and less top end), warmth 0..1 (bass lift)
CAST = {
    "n": (49, 1.08, -1.0, 0.1, 0.4),    # the narrator: deep, unhurried
    "slick": (27, 1.05, -1.5, 0.25, 0.3),  # low and cool, a little rasp
    "derek": (43, 0.95, 0.5, 0.0, 0.0),   # Natso SweetLou: brisk, pleased with himself
    "lou": (3, 1.0, -2.0, 0.45, 0.3),     # Tommy "The Shark" Maio: the mafia guy, gravel in it
    "mom": (52, 0.97, 0.0, 0.0, 0.1),     # David Pec, your best friend
    "trent": (41, 0.9, 1.0, 0.0, 0.0),    # Mustich: fast, smug, a touch high
    "rosa": (6, 1.0, -1.0, 0.3, 0.2),     # Miles "The Snowplow" Gnomey, floor manager
    "host": (24, 0.92, 0.5, 0.0, 0.2),    # Dex Marlow, stream host
    "ghost": (31, 1.12, -2.5, 0.2, 0.5),  # Bio The Ghost: slow, measured, deep
    "you": (57, 1.0, 0.0, 0.0, 0.1),
}
LEGENDS = [5, 8, 16, 19, 20, 22, 34, 35, 47, 50, 51, 54, 7, 48, 42]
def cast(sp):
    if sp in CAST: return CAST[sp]
    if sp.startswith("L:"): return (LEGENDS[zlib.crc32(sp.encode()) % len(LEGENDS)], 1.0, 0.0, 0.0, 0.0)
    return (20, 1.0, 0.0, 0.0, 0.0)

def colour(path, pitch, gravel, warmth):
    """ffmpeg: pitch shift keeping the pace, a touch of growl, a bass lift"""
    if abs(pitch) < 0.05 and gravel < 0.05 and warmth < 0.05: return
    f = []
    if abs(pitch) >= 0.05:
        r = 2 ** (pitch / 12.0); f.append(f"asetrate=22050*{r:.5f},aresample=22050,atempo={1 / r:.5f}")
    if warmth >= 0.05: f.append(f"bass=g={3 * warmth:.2f}:f=150")
    if gravel >= 0.05: f.append(f"acompressor=threshold=-18dB:ratio=3,volume={1 + gravel * 0.6:.2f},alimiter=limit=0.95,treble=g={-4 * gravel:.2f}:f=3500")
    tmp = path + ".tmp.wav"
    r = subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", path, "-af", ",".join(f), "-ar", "22050", "-ac", "1", tmp], capture_output=True)
    if r.returncode == 0: shutil.move(tmp, path)
    else: print("ffmpeg:", r.stderr.decode()[-300:])

def key_of(sp, t): return hashlib.md5((sp + "|" + t.replace("\r", "")).encode("utf-8")).hexdigest()[:12]

def speakable(t):
    t = t.replace("\n", ". ").replace("…", "...").replace("—", ", ").replace("–", ", ").replace("·", ", ")
    t = t.replace("$", "$").replace("&", " and ").replace("%", " percent").replace("#", " number ")
    t = t.replace("GTO", "G T O").replace("ICM", "I C M").replace("WCS", "W C S").replace("SnG", "sit and go").replace("CF Tilt", "C F Tilt").replace("bb", " big blinds")
    t = "".join(ch for ch in t if ch.isprintable())
    while "  " in t: t = t.replace("  ", " ")
    return t.strip()

def finish(path):
    """trim the silence Piper leaves, level the line, keep a little air on both ends"""
    w = wave.open(path); sr = w.getframerate(); x = np.frombuffer(w.readframes(w.getnframes()), "<i2").astype(np.float32) / 32768; w.close()
    env = np.convolve(np.abs(x), np.ones(int(0.01 * sr)) / int(0.01 * sr), "same"); on = np.where(env > 0.01)[0]
    if len(on): a = max(0, on[0] - int(0.08 * sr)); b = min(len(x), on[-1] + int(0.16 * sr)); x = x[a:b]
    rms = x.std() + 1e-9; x = x * min(0.16 / rms, 0.95 / (np.abs(x).max() + 1e-9))
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as o: o.setnchannels(1); o.setsampwidth(2); o.setframerate(sr); o.writeframes(pcm.tobytes())
    return len(x) / sr

lines = json.load(open(SRC)); todo = {}
for L in lines:
    k = key_of(L["sp"], L["t"]); assert k == L["key"], (k, L["key"], L["t"][:40])
    out = os.path.join(OUT, f"v_{k}.wav")
    if os.path.exists(out): continue
    spk, pace, pitch, gravel, warmth = cast(L["sp"]); todo.setdefault(pace, []).append({"text": speakable(L["t"]), "speaker_id": spk, "output_file": out, "colour": (pitch, gravel, warmth)})
print(f"{len(lines)} lines, {sum(len(v) for v in todo.values())} to make")
for pace, jobs in todo.items():
    inp = "\n".join(json.dumps({k: v for k, v in j.items() if k != "colour"}) for j in jobs) + "\n"
    r = subprocess.run([BIN, "--model", MODEL, "--json-input", "--length_scale", str(pace), "--sentence_silence", "0.25"], input=inp.encode(), capture_output=True)
    if r.returncode != 0: print(r.stderr.decode()[-2000:]); sys.exit(1)
    for j in jobs:
        colour(j["output_file"], *j["colour"]); print(f"{os.path.basename(j['output_file'])} {finish(j['output_file']):.1f}s  {j['text'][:60]}")
print("ok")
