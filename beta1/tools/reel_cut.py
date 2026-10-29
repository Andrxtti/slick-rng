# Cut the highlight reel (BETA1 task 10) from the rendered films: shots pulled by script line (Logs/film_<id>.txt cue
# sheets), title cards, a score made here (drone, impacts, a riser: our own, so no licence question), the stings.
#   python beta1/tools/reel_cut.py   -> C:\Dev\Builds\Beta1\Reel\the_dream_reel_1080p.mp4 and _720p.mp4
import os, subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FILM = r"C:\Dev\Builds\Beta1\Film"
OUT = r"C:\Dev\Builds\Beta1\Reel"
TMP = os.path.join(OUT, "parts")
VOICE = os.path.join(ROOT, "Assets", "Dream", "FilmData", "Voice")
FONT = "C\\:/Windows/Fonts/arialbd.ttf"
os.makedirs(TMP, exist_ok=True)


def narrator_key(text):
    """the game's Voice.Key("narrator", text): FNV-1a over UTF-16 code units, as in tools/voice.py"""
    h = 2166136261; data = ("narrator|" + text).encode("utf-16-le")
    for i in range(0, len(data), 2): h ^= data[i] | (data[i + 1] << 8); h = (h * 16777619) & 0xFFFFFFFF
    return f"{h:08x}"


def cue(scene, line):
    for l in open(os.path.join(ROOT, "Logs", f"film_{scene}.txt"), encoding="utf-8"):
        p = l.split("\t")
        if int(p[0]) == line: return float(p[1])
    raise KeyError((scene, line))


def ff(*args): subprocess.run(["ffmpeg", "-v", "error", "-y", *args], check=True)


# (kind, source, start, duration, sting voice or None, sting offset in the shot)
CUT = [
    ("card", "Everybody has a dream.", 0, 4.0, None, 0),
    ("film", "p_office", cue("p_office", 1) + 0.3, 2.6, None, 0),
    ("film", "p_office", cue("p_office", 7) + 0.2, 2.4, None, 0),
    ("film", "p_diner", cue("p_diner", 1) + 0.4, 2.4, None, 0),
    ("film", "p_diner2", cue("p_diner2", 7) + 0.0, 2.6, "sting_01", 0.3),
    ("film", "p_diner2", cue("p_diner2", 1) + 0.6, 2.6, None, 0),
    ("card", "Some people get a seat.", 0, 3.0, None, 0),
    ("film", "c1", cue("c1", 1) + 0.5, 2.6, None, 0),
    ("film", "c1", cue("c1", 3) + 0.2, 3.2, "sting_08", 0.4),
    ("film", "c1", cue("c1", 4) + 0.1, 3.4, "sting_07", 0.4),
    ("card", "Some people earn one.", 0, 3.0, None, 0),
    ("film", "reel_casino", 0.3, 4.6, None, 0),
    ("film", "reel_vegas", 0.3, 4.6, "sting_03", 0.8),
    ("film", "reel_tv", 1.0, 5.6, "sting_06", 0.5),
    ("film", "reel_titan", 0.3, 4.2, None, 0),
    ("film", "reel_main", 0.5, 6.5, "sting_04", 2.0),
    ("film", "reel_hall", 0.3, 6.4, "sting_05", 1.6),
    ("film", "p_diner2", cue("p_diner2", 4) + 0.4, 3.6, "sting_10", 0.6),
    ("card", "THE DREAM", 0, 6.0, None, 0),
]

parts, t, stings, cuts = [], 0.0, [], []
for i, (kind, src, start, dur, sting, soff) in enumerate(CUT):
    p = os.path.join(TMP, f"{i:02d}.mp4")
    fade = f"fade=t=in:st=0:d=0.25,fade=t=out:st={dur - 0.25:.2f}:d=0.25"
    if kind == "card":
        size = 110 if src == "THE DREAM" else 64
        ff("-f", "lavfi", "-i", f"color=c=black:s=1920x1080:r=30:d={dur}",
           "-vf", f"drawtext=fontfile='{FONT}':text='{src}':fontcolor=0xF2EEE6:fontsize={size}:x=(w-tw)/2:y=(h-th)/2:alpha='min(1,t/0.6)',{fade}",
           "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "16", p)
    else:
        ff("-ss", f"{start:.2f}", "-t", f"{dur:.2f}", "-i", os.path.join(FILM, src + ".mp4"), "-an",
           "-vf", f"scale=1920:1080,fps=30,{fade}", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "16", p)
    parts.append(p)
    if sting: stings.append((os.path.join(VOICE, sting + ".wav"), t + soff))
    if kind == "card":   # the narrator reads the title card (tools/voice.py makes these clips)
        nar = os.path.join(ROOT, "Assets", "Dream", "Resources", "Voice", "Lines", narrator_key(src) + ".wav")
        if os.path.exists(nar): stings.append((nar, t + 0.35))
    cuts.append(t)
    t += dur
total = t
with open(os.path.join(TMP, "list.txt"), "w") as f:
    for p in parts: f.write(f"file '{p}'\n")
video = os.path.join(TMP, "video.mp4")
ff("-f", "concat", "-safe", "0", "-i", os.path.join(TMP, "list.txt"), "-c", "copy", video)

# the score: a low drone under everything, an impact on the act breaks, a riser into the title, a final boom
impact = os.path.join(TMP, "impact.wav")
ff("-f", "lavfi", "-i", "aevalsrc='0.9*sin(2*PI*(48-20*t)*t)*exp(-3*t)+0.25*(random(0)-0.5)*exp(-18*t)':s=44100:d=2.5", "-af", "lowpass=f=900", impact)
drone = os.path.join(TMP, "drone.wav")
ff("-f", "lavfi", "-i", f"aevalsrc='0.16*sin(2*PI*55*t)+0.1*sin(2*PI*82.41*t)+0.06*sin(2*PI*110*t)*(0.5+0.5*sin(2*PI*0.25*t))':s=44100:d={total}",
   "-af", f"lowpass=f=400,tremolo=f=0.5:d=0.3,afade=t=in:d=3,afade=t=out:st={total - 2}:d=2", drone)
riser = os.path.join(TMP, "riser.wav")
ff("-f", "lavfi", "-i", "aevalsrc='0.25*sin(2*PI*(80+60*t*t)*t)*(t/6)+0.08*(random(0)-0.5)*(t/6)':s=44100:d=6", "-af", "highpass=f=60", riser)
hits = [cuts[i] for i, c in enumerate(CUT) if c[0] == "card"] + [cuts[11], cuts[15]]
inputs = ["-i", drone]
filt, mix = [], ["[0]"]
k = 1
for h in hits:
    inputs += ["-i", impact]; filt.append(f"[{k}]adelay={int(h * 1000)}|{int(h * 1000)},volume=0.8[a{k}]"); mix.append(f"[a{k}]"); k += 1
inputs += ["-i", riser]; filt.append(f"[{k}]adelay={int((cuts[-1] - 6) * 1000)}|{int((cuts[-1] - 6) * 1000)}[a{k}]"); mix.append(f"[a{k}]"); k += 1
for path, at in stings:
    inputs += ["-i", path]; filt.append(f"[{k}]aresample=44100,adelay={int(at * 1000)}|{int(at * 1000)},volume=1.6[a{k}]"); mix.append(f"[a{k}]"); k += 1
filt.append("".join(mix) + f"amix=inputs={len(mix)}:normalize=0,alimiter=limit=0.9,atrim=0:{total:.2f}[out]")
audio = os.path.join(TMP, "audio.wav")
ff(*inputs, "-filter_complex", ";".join(filt), "-map", "[out]", "-ac", "2", "-ar", "44100", audio)

final = os.path.join(OUT, "the_dream_reel_1080p.mp4")
ff("-i", video, "-i", audio, "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k",
   "-movflags", "+faststart", "-shortest", final)
ff("-i", final, "-vf", "scale=1280:720", "-c:v", "libx264", "-crf", "24", "-preset", "slow", "-c:a", "aac", "-b:a", "128k",
   "-movflags", "+faststart", os.path.join(OUT, "the_dream_reel_720p.mp4"))
for f in ("the_dream_reel_1080p.mp4", "the_dream_reel_720p.mp4"):
    print(f, f"{os.path.getsize(os.path.join(OUT, f)) / 1e6:.1f} MB")
print(f"reel: {total:.1f} s, {len(CUT)} shots")
