# The narrator's voice (N2b, Steven 19:01): Kokoro-82M (weights Apache-2.0, hexgrad/Kokoro-82M) through kokoro-onnx (MIT),
# offline on the CPU. No pitch shifting: a naturally deep voice slowed with Kokoro's own speed, then EQ only (low shelf
# +4 dB at 120 Hz, -2.5 dB at 300 Hz, +2 dB at 3 kHz, 3:1 compression, a short warm room). Lines are read with pauses:
# split at "/" and full stops.
#   python beta1/tools/narrator.py --audition   -> C:\Dev\Builds\day2\narrator_audition2.mp3
# voice.py calls say() for the narrator's lines.
import os, re, subprocess, sys, tempfile
import numpy as np

KOKORO = r"C:\Dev\Beta1Assets\dl\kokoro"
VOICE, SPEED = "am_onyx", 0.9   # chosen from the audition (see DAY_REPORT)
CHAIN = ("bass=g=4:f=120,equalizer=f=300:t=q:w=1.2:g=-2.5,equalizer=f=3000:t=q:w=1:g=2,"
         "acompressor=threshold=-20dB:ratio=3:attack=8:release=150,aecho=0.85:0.5:18|31:0.12|0.07")
_k = None


def kokoro():
    global _k
    if _k is None:
        from kokoro_onnx import Kokoro
        _k = Kokoro(os.path.join(KOKORO, "kokoro-v1.0.onnx"), os.path.join(KOKORO, "voices-v1.0.bin"))
    return _k


def parts(text):
    out = []
    for chunk in text.split("/"):
        out += [p.strip() for p in re.split(r"(?<=[.!?])\s+", chunk) if p.strip()]
    return out


def say(text, dst, voice=VOICE, speed=SPEED, chain=CHAIN):
    """the line to a mono 22.05 kHz wav at dst, phrase by phrase with a breath between"""
    k = kokoro(); lang = "en-gb" if voice.startswith("b") else "en-us"; sr = 24000; audio = []
    for i, p in enumerate(parts(text)):
        s, sr = k.create(p, voice=voice, speed=speed, lang=lang)
        if i: audio.append(np.zeros(int(sr * 0.45), np.float32))
        audio.append(s.astype(np.float32))
    import wave
    raw = tempfile.mktemp(suffix=".wav")
    with wave.open(raw, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(sr)
        w.writeframes((np.clip(np.concatenate(audio), -1, 1) * 32767).astype("<i2").tobytes())
    f = chain + ",silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.08," \
        "areverse,silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.25,areverse,loudnorm=I=-18:TP=-2:LRA=7"
    r = subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", raw, "-af", f, "-ar", "22050", "-ac", "1", dst], capture_output=True)
    os.remove(raw)
    if r.returncode: print("ffmpeg:", r.stderr.decode()[-300:])


def audition():
    lines = ["Biscayne Card Room, off the causeway.", "Everybody has a dream."]
    takes = [("am_onyx", 0.9), ("am_fenrir", 0.88), ("bm_george", 0.9), ("am_michael", 0.88)]
    tmp = tempfile.mkdtemp(); files = []
    for n, (v, sp) in enumerate(takes, 1):
        for j, t in enumerate(lines):
            w = os.path.join(tmp, f"{n}_{j}.wav"); say(t, w, v, sp); files.append(w)
        files.append(None)
        print(f"take {n}: {v} at {sp}")
    lst = os.path.join(tmp, "l.txt"); sil = os.path.join(tmp, "sil.wav")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-f", "lavfi", "-i", "anullsrc=r=22050:cl=mono", "-t", "1.2", sil])
    with open(lst, "w") as f:
        for w in files: f.write(f"file '{(w or sil).replace(os.sep, '/')}'\nfile '{sil.replace(os.sep, '/')}'\n")
    out = r"C:\Dev\Builds\day2\narrator_audition2.mp3"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", lst, "-b:a", "160k", out])
    print(out)


if __name__ == "__main__":
    if "--audition" in sys.argv: audition()
