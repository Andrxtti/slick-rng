# Stingers for the shot list: short one-shots that land with a line.   python3 stings.py <out dir>
#   sfx_sting   a dark hit with a tail (the line that changes things, the Ghost)
#   sfx_swell   a warm two-second string swell (a lesson landing, a goal)
#   sfx_bell    the bell over the diner door
#   sfx_crowd   applause rising and settling (a win, a bracelet)
#   sfx_flash   camera shutters and flashes (the feature table, the cover)
#   sfx_hush    a room going quiet: a murmur that drops out (the cooler, the all-in)
import numpy as np, os, sys, wave
SR = 22050
out = sys.argv[1] if len(sys.argv) > 1 else "audio"; os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(23)
def save(name, L, R=None, gain=0.8):
    R = L if R is None else R
    m = max(1e-9, max(np.abs(L).max(), np.abs(R).max())); L = L / m * gain; R = R / m * gain
    pcm = (np.clip(np.stack([L, R], 1), -1, 1) * 32767).astype("<i2")
    with wave.open(os.path.join(out, "sfx_" + name + ".wav"), "wb") as w: w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    print(name, round(len(L) / SR, 2), "s")
def noise(n, color=1.0):
    w = rng.standard_normal(n); S = np.fft.rfft(w); f = np.fft.rfftfreq(n, 1 / SR); f[0] = f[1]; S /= f ** (color / 2); y = np.fft.irfft(S, n); return y / (y.std() + 1e-9)
def lowpass(x, fc):
    a = 1 - np.exp(-2 * np.pi * fc / SR); y = np.empty_like(x); acc = 0.0
    for i in range(len(x)): acc += a * (x[i] - acc); y[i] = acc
    return y
def env(n, a, d, s, r):
    t = np.linspace(0, n / SR, n); e = np.ones(n) * s
    A, D, R = int(a * SR), int(d * SR), int(r * SR)
    if A > 0: e[:A] = np.linspace(0, 1, A)
    if D > 0: e[A:A + D] = np.linspace(1, s, min(D, max(0, n - A)))
    if R > 0: e[-R:] *= np.linspace(1, 0, min(R, n))
    return e
def sine(f, n, detune=0.0):
    t = np.arange(n) / SR; return np.sin(2 * np.pi * f * (1 + detune) * t)
def reverb(x, secs=2.0, mix=0.35):
    ir = noise(int(secs * SR), 0.5) * np.exp(-np.linspace(0, 6, int(secs * SR))); ir = lowpass(ir, 3000)
    y = np.convolve(x, ir)[:len(x)] ; y = y / (np.abs(y).max() + 1e-9) * np.abs(x).max()
    return x * (1 - mix) + y * mix
def strings(freqs, n, attack=0.6):
    y = np.zeros(n)
    for f in freqs:
        for dt in (-0.004, 0, 0.005): y += sine(f, n, dt) * (1 + 0.2 * sine(5.0, n))
    y = lowpass(y, 1400); return y * env(n, attack, 0.2, 0.9, 0.5)

# a dark hit: sub thump, a detuned minor cluster, a long tail
n = int(2.4 * SR)
thump = sine(48, n) * env(n, 0.002, 0.25, 0.0, 0.1) + 0.5 * sine(36, n) * env(n, 0.002, 0.4, 0.0, 0.1)
cluster = (sine(110, n, 0) + sine(130.81, n, 0.003) + sine(164.81, n, -0.002)) * env(n, 0.01, 0.9, 0.25, 1.2)
hit = lowpass(noise(int(0.08 * SR), 0.7), 2500) * env(int(0.08 * SR), 0.001, 0.05, 0, 0.02); hitb = np.zeros(n); hitb[:len(hit)] = hit
save("sting", reverb(thump + 0.35 * lowpass(cluster, 900) + 0.6 * hitb, 2.6, 0.45))
# a warm swell: a major-ish pad rising then easing
n = int(2.6 * SR); sw = strings([196, 246.94, 293.66, 392], n, 1.1) * env(n, 1.1, 0.3, 0.8, 1.0)
save("swell", reverb(sw, 2.2, 0.4), np.roll(reverb(sw, 2.2, 0.4), 260), 0.6)
# the bell over the door: two partials, a bright ping that rings down
n = int(1.6 * SR); b = (sine(2093, n) + 0.6 * sine(2093 * 2.76, n) + 0.3 * sine(2093 * 5.4, n)) * np.exp(-np.linspace(0, 7, n)); b2 = np.roll(b * 0.7, int(0.11 * SR)); b2[:int(0.11 * SR)] = 0
save("bell", reverb(b + b2, 1.2, 0.3), gain=0.5)
# applause: many short claps, density rising then settling
n = int(3.0 * SR); crowd = np.zeros(n); t = np.linspace(0, 1, n); dens = (np.sin(np.pi * np.minimum(1, t * 1.4)) ** 1.5) * 0.9 + 0.1
for i in range(1400):
    at = int(rng.random() ** 0.8 * (n - 400)); 
    if rng.random() > dens[at]: continue
    ln = int(SR * (0.004 + rng.random() * 0.006)); clap = noise(ln, 0.3) * np.exp(-np.linspace(0, 5, ln)); crowd[at:at + ln] += clap * (0.4 + 0.6 * rng.random())
crowd = np.convolve(crowd, np.ones(3) / 3, "same") ; crowd = crowd - lowpass(crowd, 300)
save("crowd", reverb(crowd * env(n, 0.05, 0.2, 1, 0.9), 1.6, 0.4), gain=0.6)
# camera flashes: shutter clicks with a bright transient, a small burst of them
n = int(1.4 * SR); fl = np.zeros(n)
for k in range(9):
    at = int((0.03 + k * 0.11 + rng.random() * 0.06) * SR); ln = int(0.03 * SR)
    fl[at:at + ln] += (noise(ln, 0.2) * np.exp(-np.linspace(0, 9, ln))) * (0.5 + 0.5 * rng.random())
    fl[at:at + ln // 2] += sine(5200 + 400 * k, ln // 2) * np.exp(-np.linspace(0, 12, ln // 2)) * 0.3
save("flash", fl, np.roll(fl, 90), 0.5)
# a room going quiet: a murmur, then it drops out and leaves a thin ring
n = int(2.2 * SR); m = lowpass(noise(n, 1.1), 900) * env(n, 0.05, 0.1, 0.9, 0.05)
cut = int(0.9 * SR); m[cut:] *= np.exp(-np.linspace(0, 40, n - cut)); ring = sine(880, n) * 0.0
ring = np.zeros(n); ring[cut:] = sine(1760, n - cut) * np.exp(-np.linspace(0, 4, n - cut)) * 0.08
save("hush", m + ring, gain=0.5)
print("ok")
