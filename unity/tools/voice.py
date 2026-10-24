# Voice the story with Piper (offline neural TTS) and the LibriTTS multi-speaker voice: one clip per line.
#   python3 voice.py <voice_lines.json from Dream > Export voice lines> <out dir> [--piper <dir with piper/ and the .onnx>]
# Writes v_<key>.wav (22.05 kHz mono) for every line that doesn't have one yet; the key is MD5("sp|text")[:12],
# the same hash Voice.Key computes in the game. Each character has a speaker and a pace; legends draw from a pool.
import json, os, sys, subprocess, hashlib, zlib, wave, numpy as np, shutil
SRC, OUT = sys.argv[1], sys.argv[2]
PIPER = sys.argv[sys.argv.index("--piper") + 1] if "--piper" in sys.argv else os.path.expanduser("~/piper")
MODEL = os.path.join(PIPER, "en-us-libritts-high.onnx"); BIN = os.path.join(PIPER, "piper", "piper")
os.makedirs(OUT, exist_ok=True)

#        speaker id, pace (1 = normal, bigger = slower)
CAST = {
    "n": (49, 1.08),      # the narrator: deep, unhurried
    "slick": (27, 1.05),  # low and cool
    "derek": (43, 0.95),  # Natso SweetLou: brisk, pleased with himself
    "lou": (3, 1.0),      # Tommy "The Shark" Maio
    "mom": (52, 0.97),    # David Pec, your best friend
    "trent": (41, 0.9),   # Mustich: fast, smug
    "rosa": (6, 1.0),     # Miles "The Snowplow" Gnomey, floor manager
    "host": (24, 0.92),   # Dex Marlow, stream host
    "ghost": (31, 1.12),  # Bio The Ghost: slow, measured
    "you": (57, 1.0),
}
LEGENDS = [5, 8, 16, 19, 20, 22, 34, 35, 47, 50, 51, 54, 7, 48, 42]
def cast(sp):
    if sp in CAST: return CAST[sp]
    if sp.startswith("L:"): return (LEGENDS[zlib.crc32(sp.encode()) % len(LEGENDS)], 1.0)
    return (20, 1.0)

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
    spk, pace = cast(L["sp"]); todo.setdefault(pace, []).append({"text": speakable(L["t"]), "speaker_id": spk, "output_file": out})
print(f"{len(lines)} lines, {sum(len(v) for v in todo.values())} to make")
for pace, jobs in todo.items():
    inp = "\n".join(json.dumps(j) for j in jobs) + "\n"
    r = subprocess.run([BIN, "--model", MODEL, "--json-input", "--length_scale", str(pace), "--sentence_silence", "0.25"], input=inp.encode(), capture_output=True)
    if r.returncode != 0: print(r.stderr.decode()[-2000:]); sys.exit(1)
    for j in jobs: print(f"{os.path.basename(j['output_file'])} {finish(j['output_file']):.1f}s  {j['text'][:60]}")
print("ok")
