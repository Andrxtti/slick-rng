# The game's sound, synthesised: ambience loops for every set, a small score, and stingers.
#   python3 audio.py <out dir>   -> <out>/amb_<place>.wav, music_<cue>.wav, sfx_<name>.wav  (22.05 kHz, 16-bit, stereo)
# Everything loops cleanly: loops are built in a circular buffer and cross-faded at the seam.
import numpy as np, os, sys, wave, math
SR = 22050
out = sys.argv[1] if len(sys.argv) > 1 else "audio"; os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(11)

def save(name, L, R=None, gain=0.9):
    R = L if R is None else R
    m = max(1e-9, max(np.abs(L).max(), np.abs(R).max())); L = L / m * gain; R = R / m * gain
    data = np.stack([L, R], 1); pcm = (np.clip(data, -1, 1) * 32767).astype("<i2")
    with wave.open(os.path.join(out, name + ".wav"), "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    print(name, round(len(L) / SR, 1), "s")

def seam(x, n=int(0.6 * SR)):
    """make a buffer loop: cross-fade its tail into its head"""
    y = x.copy(); f = np.linspace(0, 1, n)
    y[:n] = x[:n] * f + x[-n:] * (1 - f); return y[:-n]

def noise(n, color=1.0):
    """1/f^color noise, unit RMS"""
    w = rng.standard_normal(n); S = np.fft.rfft(w); f = np.fft.rfftfreq(n, 1 / SR); f[0] = f[1]
    S /= f ** (color / 2); y = np.fft.irfft(S, n); return y / (y.std() + 1e-9)

def lowpass(x, fc):
    a = math.exp(-2 * math.pi * fc / SR); y = np.empty_like(x); acc = 0.0
    for i in range(len(x)): acc = a * acc + (1 - a) * x[i]; y[i] = acc
    return y
def bandpass(x, lo, hi): return lowpass(x, hi) - lowpass(x, lo)

def env(n, a, d, s, r):
    """ADSR over n samples (seconds for a, d, r; s = sustain level)"""
    a, d, r = int(a * SR), int(d * SR), int(r * SR); e = np.zeros(n)
    e[:a] = np.linspace(0, 1, a); e[a:a + d] = np.linspace(1, s, d); e[a + d:max(a + d, n - r)] = s
    e[max(a + d, n - r):] = np.linspace(s, 0, n - max(a + d, n - r)); return e

def tone(freq, n, kind="sine", detune=0.0):
    t = np.arange(n) / SR; f = freq * (1 + detune)
    if kind == "saw": return 2 * ((t * f) % 1) - 1
    if kind == "square": return np.sign(np.sin(2 * np.pi * f * t))
    if kind == "tri": return 2 * np.abs(2 * ((t * f) % 1) - 1) - 1
    return np.sin(2 * np.pi * f * t)

def pad(freqs, n, bright=900):
    """a soft chord: detuned saws through a slow lowpass, with a little breath"""
    y = np.zeros(n); kind = "tri" if bright < 1000 else "saw"   # the quiet cues are soft triangles, the bright ones saws
    for f in freqs:
        for dt in (-0.004, 0, 0.004): y += tone(f, n, kind, dt)
    y = lowpass(lowpass(y, bright), bright * 1.5); y *= env(n, 0.8, 0.5, 0.85, 1.2); return y / (len(freqs) * 3)

def pluck(freq, n, decay=1.6):
    t = np.arange(n) / SR; y = (np.sin(2 * np.pi * freq * t) + 0.35 * np.sin(4 * np.pi * freq * t) + 0.12 * np.sin(6 * np.pi * freq * t))
    return y * np.exp(-t * decay) * env(n, 0.004, 0.05, 0.9, 0.2)

def place(y, buf, at, pan=0.0, gain=1.0):
    """drop a sound into a stereo loop buffer, wrapping around the end"""
    n = len(buf[0]); i = int(at * SR) % n; m = len(y); idx = (np.arange(m) + i) % n
    buf[0][idx] += y * gain * (1 - max(0, pan)); buf[1][idx] += y * gain * (1 + min(0, pan))

def murmur(n, level, color=1.2, lo=180, hi=1400):
    """a room full of people talking, far away: band-limited noise with slow swells"""
    x = bandpass(noise(n, color), lo, hi); sw = 0.6 + 0.4 * np.sin(np.arange(n) / SR * 0.37) * np.sin(np.arange(n) / SR * 0.11 + 1)
    return x * sw * level

def click(n, f=2600, dec=120):
    t = np.arange(n) / SR; return np.sin(2 * np.pi * f * t) * np.exp(-t * dec)

# ------------------------------------------------------------ ambiences
def ambience(name, secs, build):
    n = int(secs * SR); buf = [np.zeros(n), np.zeros(n)]
    build(buf, n)
    save("amb_" + name, seam(buf[0]), seam(buf[1]), gain=0.6)

def office(buf, n):
    hum = lowpass(noise(n, 1.5), 140) * 0.9 + 0.35 * tone(60, n) * 0.4 + 0.2 * tone(120, n) * 0.3   # HVAC and mains
    buf[0] += hum; buf[1] += hum * 0.9
    for k in range(70):   # a keyboard a few desks over
        at = rng.uniform(0, n / SR); place(click(int(0.03 * SR), rng.uniform(1800, 3200), 260) * 0.25, buf, at, pan=0.5)
    place(bandpass(noise(int(1.5 * SR), 1), 400, 900) * np.linspace(1, 0, int(1.5 * SR)) * 0.15, buf, 6.0, pan=-0.6)   # a printer
def diner(buf, n):
    m = murmur(n, 0.5, 1.3, 200, 1200); buf[0] += m; buf[1] += np.roll(m, 311)
    buzz = (tone(120, n, "square") * 0.015 + tone(240, n) * 0.012) * (0.8 + 0.2 * np.sin(np.arange(n) / SR * 7))   # the neon sign
    buf[0] += buzz; buf[1] += buzz * 0.6
    for k in range(9):   # cups and cutlery
        at = rng.uniform(0, n / SR); place(click(int(0.25 * SR), rng.uniform(2400, 4200), 24) * 0.18, buf, at, pan=rng.uniform(-0.8, 0.8))
    fry = bandpass(noise(n, 0.5), 3000, 7000) * 0.05; buf[0] += fry * 0.4; buf[1] += fry   # the grill
def cardroom(buf, n):
    m = murmur(n, 0.55, 1.2, 160, 1600); buf[0] += m; buf[1] += np.roll(m, 503)
    for k in range(60):   # chips riffling at other tables
        at = rng.uniform(0, n / SR); pan = rng.uniform(-0.9, 0.9)
        for j in range(int(rng.integers(4, 10))): place(click(int(0.02 * SR), rng.uniform(2800, 3600), 400) * 0.22, buf, at + j * 0.045, pan=pan)
    buf[0] += lowpass(noise(n, 1.4), 120) * 0.35; buf[1] += lowpass(noise(n, 1.4), 120) * 0.35
def casino(buf, n):
    m = murmur(n, 0.7, 1.1, 150, 2000); buf[0] += m; buf[1] += np.roll(m, 701)
    for k in range(14):   # slot machines far off
        at = rng.uniform(0, n / SR); f0 = rng.choice([523, 659, 784, 1047])
        for j in range(3): place(tone(f0 * (1 + 0.26 * j), int(0.12 * SR)) * env(int(0.12 * SR), 0.005, 0.03, 0.5, 0.06) * 0.06, buf, at + j * 0.13, pan=rng.uniform(-0.9, 0.9))
    for k in range(40):
        at = rng.uniform(0, n / SR); pan = rng.uniform(-0.9, 0.9)
        for j in range(int(rng.integers(3, 8))): place(click(int(0.02 * SR), rng.uniform(2800, 3600), 400) * 0.15, buf, at + j * 0.045, pan=pan)
def vegas(buf, n):
    tr = lowpass(noise(n, 1.6), 260) * 0.9; buf[0] += tr; buf[1] += np.roll(tr, 1500)   # traffic on the Strip
    m = murmur(n, 0.35, 1.0, 300, 2200); buf[0] += np.roll(m, 90); buf[1] += m
    for k in range(5):   # a horn, a siren far away
        at = rng.uniform(0, n / SR); place(tone(rng.choice([349, 415, 440]), int(0.5 * SR), "square") * env(int(0.5 * SR), 0.02, 0.1, 0.6, 0.2) * 0.04, buf, at, pan=rng.uniform(-0.9, 0.9))
def tv(buf, n):
    buf[0] += lowpass(noise(n, 1.5), 180) * 0.6; buf[1] += lowpass(noise(n, 1.5), 180) * 0.6
    m = murmur(n, 0.3, 1.2, 200, 1500); buf[0] += m; buf[1] += np.roll(m, 400)
    ap = bandpass(noise(int(3 * SR), 0.6), 800, 4000) * env(int(3 * SR), 0.3, 0.5, 0.8, 1.2) * 0.35   # a burst of applause
    place(ap, buf, 7.0); place(ap * 0.8, buf, 18.5, pan=0.2)
def titan(buf, n):
    buf[0] += lowpass(noise(n, 1.8), 100) * 0.7; buf[1] += lowpass(noise(n, 1.8), 100) * 0.7   # a very quiet, very expensive room
    for k in range(int(n / SR)): place(click(int(0.05 * SR), 1800, 140) * 0.1, buf, k + 0.0, pan=0.7)   # a clock
    m = murmur(n, 0.12, 1.4, 150, 900); buf[0] += m; buf[1] += np.roll(m, 250)
def main(buf, n):
    m = murmur(n, 0.9, 1.0, 120, 2400); buf[0] += m; buf[1] += np.roll(m, 900)   # eight thousand people
    buf[0] += lowpass(noise(n, 1.5), 150) * 0.5; buf[1] += lowpass(noise(n, 1.5), 150) * 0.5
    for k in range(30):
        at = rng.uniform(0, n / SR); pan = rng.uniform(-0.9, 0.9)
        for j in range(int(rng.integers(3, 8))): place(click(int(0.02 * SR), rng.uniform(2800, 3600), 400) * 0.12, buf, at + j * 0.045, pan=pan)
def hall(buf, n):
    rt = lowpass(noise(n, 1.7), 220) * 0.6; buf[0] += rt; buf[1] += np.roll(rt, 2200)
    for k in range(6):   # footsteps with a long tail
        at = rng.uniform(0, n / SR); st = click(int(0.6 * SR), 180, 9) * 0.25; place(st, buf, at, pan=rng.uniform(-0.6, 0.6))
def home(buf, n):
    buf[0] += lowpass(noise(n, 1.6), 160) * 0.5; buf[1] += lowpass(noise(n, 1.6), 160) * 0.5
    fr = tone(50, n) * 0.08 + tone(100, n) * 0.03; buf[0] += fr * 0.7; buf[1] += fr   # the fridge
    city = murmur(n, 0.12, 1.5, 100, 700); buf[0] += city; buf[1] += np.roll(city, 600)
    place(tone(1800, int(0.4 * SR)) * env(int(0.4 * SR), 0.002, 0.05, 0.3, 0.3) * 0.03, buf, 9.3, pan=0.4)   # a notification, somewhere

for name, fn in [("office", office), ("diner", diner), ("cardroom", cardroom), ("casino", casino), ("vegas", vegas), ("tv", tv), ("titan", titan), ("main", main), ("hall", hall), ("home", home)]:
    ambience(name, 20, fn)

# ------------------------------------------------------------ the score (loops)
def music(name, bpm, bars, chords, bass=True, arp=True, bright=1100, pulse=False, gain=0.7):
    beat = 60 / bpm; bar = 4 * beat; n = int(bars * bar * SR); buf = [np.zeros(n), np.zeros(n)]
    per = bars // len(chords)
    for ci, ch in enumerate(chords):
        at = ci * per * bar; L = int(per * bar * SR)
        p = pad(ch, L, bright); place(p, buf, at, pan=-0.2, gain=0.55); place(np.roll(p, 700), buf, at, pan=0.2, gain=0.55)
        if bass:
            root = ch[0] / 2
            for b in range(per * 4): place(pluck(root, int(beat * 0.9 * SR), 2.2) * (0.9 if b % 4 == 0 else 0.55), buf, at + b * beat, gain=0.5)
        if arp:
            notes = ch + [ch[0] * 2]
            for k in range(per * 8):
                f = notes[(k * 2 + (k // 4)) % len(notes)] * 2
                place(pluck(f, int(beat * 0.45 * SR), 5) * 0.4, buf, at + k * beat / 2, pan=(0.6 if k % 2 else -0.6))
        if pulse:
            for b in range(per * 8):
                place(lowpass(noise(int(0.07 * SR), 0.3), 2500) * env(int(0.07 * SR), 0.001, 0.02, 0.2, 0.04) * (0.5 if b % 2 == 0 else 0.25), buf, at + b * beat / 2, gain=0.6)
                if b % 4 == 0: place(np.sin(2 * np.pi * 55 * np.arange(int(0.25 * SR)) / SR * np.exp(-np.arange(int(0.25 * SR)) / SR * 9)) * np.exp(-np.arange(int(0.25 * SR)) / SR * 14), buf, at + b * beat / 2, gain=0.9)
    save("music_" + name, seam(buf[0], int(0.5 * SR)), seam(buf[1], int(0.5 * SR)), gain=gain)

A3, C4, D4, E4, F4, G4, A4, B4, C5, D5, E5 = 220, 261.63, 293.66, 329.63, 349.23, 392, 440, 493.88, 523.25, 587.33, 659.25
Bb3, Eb4, Ab4, Bb4, F3, G3 = 233.08, 311.13, 415.3, 466.16, 174.61, 196
music("night", 72, 8, [[A3, C4, E4], [F3, A3, C4], [G3, B4 / 2, D4], [E4 / 2, G3, B4 / 2]], arp=False, bright=700)        # 2 AM, a diner, a decision
music("grind", 108, 8, [[A3, C4, E4], [A3, C4, E4], [F3, A3, C4], [G3, B4 / 2, D4]], pulse=True, bright=1400)              # the card room, the work
music("triumph", 96, 8, [[C4, E4, G4], [G3, B4 / 2, D4], [A3, C4, E4], [F3, A3, C4]], bright=1800, gain=0.75)            # a title, a walk to the cage
music("tension", 84, 8, [[D4, F4, A4], [Bb3, D4, F4], [D4, F4, A4], [A3, 277.18, E4]], arp=False, bass=True, bright=600, pulse=True, gain=0.65)   # the bubble, the big one

# ------------------------------------------------------------ stingers and ticks
def oneshot(name, L, R=None, gain=0.8): save("sfx_" + name, L, R, gain)
n = int(2.2 * SR)
up = sum(pluck(f, n, 1.1) for f in (C4, E4, G4, C5)) + 0.5 * pad([C4, E4, G4, C5], n, 2000); oneshot("win", up, np.roll(up, 300))
n = int(2.6 * SR)
dn = sum(pluck(f, n, 0.9) for f in (A3, 207.65, 164.81)) + 0.6 * pad([A3 / 2, 164.81, 207.65], n, 500); oneshot("bust", dn, np.roll(dn, 400))
n = int(0.12 * SR); oneshot("tick", click(n, 2200, 90) * 0.5 + click(n, 3300, 160) * 0.2, gain=0.5)
n = int(0.5 * SR); oneshot("cut", lowpass(noise(n, 0.8), 1200) * env(n, 0.002, 0.08, 0.1, 0.3), gain=0.35)
n = int(1.2 * SR); oneshot("text", pluck(E5, n, 4) + 0.6 * pluck(B4, n, 5), gain=0.5)
n = int(0.6 * SR); oneshot("chips", sum(np.roll(click(n, 3000 + 120 * k, 60) * 0.3, int(0.035 * k * SR)) for k in range(6)), gain=0.5)
print("ok")
