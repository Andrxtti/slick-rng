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

try:
    from scipy.signal import lfilter
except ImportError:
    lfilter = None
def lowpass(x, fc):
    a = math.exp(-2 * math.pi * fc / SR)
    if lfilter is not None: return lfilter([1 - a], [1, -a], x)
    y = np.empty_like(x); acc = 0.0
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
    L, R = seam(buf[0]), seam(buf[1])
    if name in ("cardroom", "casino", "main", "hall", "tv"):   # the big rooms ring a little
        L = reverb(L, 1.6, 0.25, 2500); R = reverb(R, 1.6, 0.25, 2500)
    save("amb_" + name, L, R, gain=0.6)

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


# ------------------------------------------------------------ the score (loops)
# A small film-score kit: a string section (detuned saws with vibrato), a felt piano, a sub, brass stabs, a kick,
# a taiko, hats and risers, all through a circular reverb so the loops stay seamless.
def reverb(x, secs=2.2, mix=0.35, lp=3200):
    n = len(x); m = min(n, int(secs * SR)); t = np.arange(m) / SR
    ir = rng.standard_normal(m) * np.exp(-t / (secs / 3.5)); ir = lowpass(ir, lp); ir[:int(0.004 * SR)] = 0
    wet = np.fft.irfft(np.fft.rfft(x) * np.fft.rfft(ir, n), n)   # circular: the tail wraps to the head, so a loop stays a loop
    wet *= (x.std() + 1e-9) / (wet.std() + 1e-9)
    return x + wet * mix

def strings(freqs, n, bright=1200, vib=5.0, attack=1.2, gain=1.0):
    t = np.arange(n) / SR; y = np.zeros(n)
    for f in freqs:
        for dt, ph in ((-0.007, 0.0), (-0.002, 1.1), (0.003, 2.3), (0.008, 0.5)):
            ff = f * (1 + dt) * (1 + 0.0035 * np.sin(2 * np.pi * vib * t + ph) * np.minimum(1, t / 0.8))
            y += 2 * ((np.cumsum(ff) / SR) % 1) - 1
    y = lowpass(lowpass(y, bright), bright * 1.4)
    return y * env(n, attack, 0.6, 0.85, 1.4) / (len(freqs) * 4) * gain

def piano(freq, n, vel=1.0):
    t = np.arange(n) / SR
    y = (np.sin(2 * np.pi * freq * t) * np.exp(-t * 1.1) + 0.5 * np.sin(2 * np.pi * freq * 2.003 * t) * np.exp(-t * 2.4)
         + 0.22 * np.sin(2 * np.pi * freq * 3.01 * t) * np.exp(-t * 4.0) + 0.08 * np.sin(2 * np.pi * freq * 4.02 * t) * np.exp(-t * 6))
    hammer = lowpass(rng.standard_normal(n), 2200) * np.exp(-t * 110) * 0.25
    return (y + hammer) * env(n, 0.003, 0.08, 0.92, 0.25) * vel

def brass(freqs, n, bright=1600):
    t = np.arange(n) / SR; y = np.zeros(n)
    for f in freqs:
        for dt in (-0.005, 0.004): y += 2 * (((t * f * (1 + dt)) % 1)) - 1
    y = lowpass(y, bright); return y * env(n, 0.04, 0.12, 0.8, 0.18) / (len(freqs) * 2)

def sub(freq, n): return np.sin(2 * np.pi * freq * np.arange(n) / SR) * env(n, 0.3, 0.3, 1, 0.6)
def bassline(freq, n):
    t = np.arange(n) / SR; y = tone(freq, n, "saw") * 0.5 + np.sin(2 * np.pi * freq * t)
    return lowpass(y, 240) * env(n, 0.008, 0.08, 0.8, 0.06)

def kick(n=int(0.35 * SR)):
    t = np.arange(n) / SR; f = 46 + 120 * np.exp(-t * 30); ph = np.cumsum(f) / SR
    return np.sin(2 * np.pi * ph) * np.exp(-t * 7) + lowpass(rng.standard_normal(n), 1800) * np.exp(-t * 90) * 0.3
def taiko(n=int(1.0 * SR)):
    t = np.arange(n) / SR; f = 58 + 70 * np.exp(-t * 16); ph = np.cumsum(f) / SR
    return np.sin(2 * np.pi * ph) * np.exp(-t * 4.0) + lowpass(rng.standard_normal(n), 800) * np.exp(-t * 22) * 0.6
def hat(n=int(0.07 * SR), open_=False):
    t = np.arange(n) / SR; return bandpass(rng.standard_normal(n), 5500, 9500) * np.exp(-t * (22 if open_ else 95))
def snare(n=int(0.25 * SR)):
    t = np.arange(n) / SR; return np.sin(2 * np.pi * 185 * t) * np.exp(-t * 26) * 0.6 + bandpass(rng.standard_normal(n), 1400, 6000) * np.exp(-t * 17)
def riser(secs):
    n = int(secs * SR); x = rng.standard_normal(n); y = np.zeros(n); k = 24; step = n // k
    for i in range(k):   # a noise sweep: the lowpass opens as it rises
        fc = 250 + 6000 * (i / (k - 1)) ** 2; sl = slice(i * step, n if i == k - 1 else (i + 1) * step); y[sl] = lowpass(x[sl], fc)
    return y * (np.arange(n) / n) ** 1.6

def score(name, bpm, bars, part, secs_rev=2.0, mix_rev=0.3, gain=0.72, lp_rev=3200):
    beat = 60 / bpm; bar = 4 * beat; n = int(bars * bar * SR); buf = [np.zeros(n), np.zeros(n)]
    part(buf, n, beat, bar)
    L = reverb(seam(buf[0], int(0.4 * SR)), secs_rev, mix_rev, lp_rev); R = reverb(seam(buf[1], int(0.4 * SR)), secs_rev, mix_rev, lp_rev)
    save("music_" + name, L, R, gain=gain)

A2, E3, D3, C3, G2, F2, Bb2 = 110, 164.81, 146.83, 130.81, 98, 87.31, 116.54
A3, C4, D4, E4, F4, G4, A4, B4, C5, D5, E5, F5, G5, A5 = 220, 261.63, 293.66, 329.63, 349.23, 392, 440, 493.88, 523.25, 587.33, 659.25, 698.46, 783.99, 880
Bb3, Eb4, Ab4, Bb4, F3, G3, B3, Db4 = 233.08, 311.13, 415.3, 466.16, 174.61, 196, 246.94, 277.18

def night(buf, n, beat, bar):
    """2 AM: a felt piano over slow strings and a sub, the diner, a decision"""
    prog = [([A3, C4, E4], A2), ([F3, A3, C4], F2), ([C4, E4, G4], C3), ([G3, B3, D4], G2)]
    motif = [(0, 2, 0.9), (1.5, 1, 0.6), (2, 3, 0.7), (3.5, 2, 0.5), (4, 1, 0.8), (5.5, 3, 0.55), (6, 2, 0.6), (7, 4, 0.45)]   # (beat, chord tone idx, velocity) over 2 bars
    for ci, (ch, root) in enumerate(prog):
        at = ci * 2 * bar; L2 = int(2 * bar * SR)
        st = strings(ch, L2, 900, 4.6, 1.6, 0.5); place(st, buf, at, pan=-0.25); place(np.roll(st, 900), buf, at, pan=0.25)
        hi = strings([ch[2] * 2], L2, 1500, 5.4, 2.2, 0.14); place(hi, buf, at, pan=0.35)
        place(sub(root, L2) * 0.5, buf, at)
        tones = ch + [ch[0] * 2, ch[1] * 2]
        for (b, ti, v) in motif:
            if ci % 2 == 1 and b == 7: continue
            place(piano(tones[ti] * 2, int(3.0 * SR), v) * 0.55, buf, at + b * beat, pan=(-0.3 + 0.15 * ti))
        if ci == 3: place(piano(ch[0], int(3.5 * SR), 0.5) * 0.5, buf, at + 6 * beat, pan=-0.4)

def grind(buf, n, beat, bar):
    """the card room: a pulse, hats, a bass ostinato, strings holding the tension under it"""
    prog = [([A3, C4, E4], A2), ([A3, C4, E4], A2), ([F3, A3, C4], F2), ([G3, B3, D4], G2)]
    for ci, (ch, root) in enumerate(prog):
        at = ci * 2 * bar; L2 = int(2 * bar * SR)
        st = strings(ch, L2, 1000, 5.0, 0.9, 0.42); place(st, buf, at, pan=-0.3); place(np.roll(st, 700), buf, at, pan=0.3)
        for b in range(16):   # eighth-note bass, the fifth once a bar
            place(bassline(root * (1.5 if b % 8 == 4 else 1), int(beat * 0.42 * SR)) * (0.7 if b % 2 == 0 else 0.45), buf, at + b * beat / 2, gain=0.55)
        for b in range(8):   # drums over two bars
            if b % 2 == 0: place(kick(), buf, at + b * beat, gain=0.9)
            if b % 4 == 3: place(kick(), buf, at + b * beat + beat / 2, gain=0.55)
            if b % 2 == 1: place(snare(), buf, at + b * beat, gain=0.28)
        for b in range(16): place(hat(open_=(b % 8 == 6)), buf, at + b * beat / 2, pan=0.4, gain=(0.22 if b % 2 else 0.12))
        arp = ch + [ch[0] * 2]
        for k in range(16): place(piano(arp[(k * 2 + k // 4) % 4] * 2, int(beat * 0.6 * SR), 0.45) * 0.35, buf, at + k * beat / 2, pan=(0.5 if k % 2 else -0.5))
    place(riser(2 * beat) * 0.25, buf, n / SR - 2 * beat)

def tension(buf, n, beat, bar):
    """the bubble: a drone, tremolo strings, taiko, a riser into every fourth bar"""
    prog = [([D4, F4, A4], D3), ([Bb3, D4, F4], Bb2), ([D4, F4, A4], D3), ([A3, Db4, E4], A2)]
    t = np.arange(n) / SR; trem = 0.81 + 0.19 * np.sign(np.sin(2 * np.pi * 7.5 * t))
    for ci, (ch, root) in enumerate(prog):
        at = ci * 2 * bar; L2 = int(2 * bar * SR)
        low = strings([ch[0] / 2, ch[2] / 2], L2, 520, 4.2, 1.8, 0.55); place(low, buf, at, pan=-0.2); place(np.roll(low, 1200), buf, at, pan=0.2)
        st = strings(ch, L2, 1100, 6.0, 0.6, 0.38) * trem[:L2]; place(st, buf, at, pan=0.3); place(np.roll(st, 500), buf, at, pan=-0.3)
        place(sub(root / 2, L2) * 0.6, buf, at)
        place(strings([A5], L2, 2600, 6.5, 2.5, 0.07 + 0.02 * ci), buf, at, pan=0.1)   # a pedal, high and thin, getting louder
        for b in range(2):   # taiko on the one and the and-of-three
            place(taiko(), buf, at + b * bar, gain=0.9); place(taiko() * 0.55, buf, at + b * bar + 2.5 * beat, gain=0.9)
        if ci % 2 == 1: place(riser(bar) * 0.22, buf, at + bar)
        if ci == 3: place(taiko() * 0.8, buf, at + bar + 3 * beat, gain=0.9); place(taiko() * 0.8, buf, at + bar + 3.5 * beat, gain=0.9)

def triumph(buf, n, beat, bar):
    """a title: brass stabs, four on the floor, strings up high, a piano running"""
    prog = [([C4, E4, G4], C3), ([G3, B3, D4], G2), ([A3, C4, E4], A2), ([F3, A3, C4], F2)]
    for ci, (ch, root) in enumerate(prog):
        at = ci * 2 * bar; L2 = int(2 * bar * SR)
        st = strings([f * 2 for f in ch], L2, 1700, 5.5, 0.7, 0.4); place(st, buf, at, pan=-0.3); place(np.roll(st, 800), buf, at, pan=0.3)
        place(sub(root, L2) * 0.45, buf, at)
        for b, ln in ((0, 1.5), (2.5, 1.0), (4, 1.5), (6.5, 1.5)): place(brass(ch + [ch[0] / 2], int(ln * beat * SR), 1500) * 0.5, buf, at + b * beat, pan=0.1)
        for b in range(8): place(kick(), buf, at + b * beat, gain=0.8)
        place(taiko(), buf, at, gain=0.7); place(taiko() * 0.6, buf, at + bar, gain=0.7)
        for b in range(16): place(hat(open_=(b % 4 == 2)), buf, at + b * beat / 2, pan=-0.4, gain=(0.18 if b % 2 else 0.1))
        run = ch + [ch[0] * 2, ch[1] * 2, ch[2] * 2]
        for k in range(16): place(piano(run[(k + k // 3) % 6] * 2, int(beat * 0.5 * SR), 0.5) * 0.35, buf, at + k * beat / 2, pan=(0.5 if k % 2 else -0.3))
    place(riser(bar) * 0.3, buf, n / SR - bar)

for name, fn in [("office", office), ("diner", diner), ("cardroom", cardroom), ("casino", casino), ("vegas", vegas), ("tv", tv), ("titan", titan), ("main", main), ("hall", hall), ("home", home)]:
    ambience(name, 20, fn)
score("night", 70, 8, night, 2.8, 0.42, 0.7, 2600)
score("grind", 108, 8, grind, 1.3, 0.22, 0.74)
score("tension", 84, 8, tension, 3.0, 0.45, 0.72, 2400)
score("triumph", 100, 8, triumph, 1.9, 0.3, 0.78)

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
