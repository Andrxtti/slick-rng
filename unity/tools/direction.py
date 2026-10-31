# The shot list: how every line of the story is filmed.   python3 direction.py  -> Assets/Dream/Resources/Dream/direction.json
# One entry per line, in order, as (speaker, direction). The speaker is checked against the script when the game
# loads, so a line added or removed in the script leaves the rest of the scene's direction alone.
#
# direction = shot[^][:act][>react][@cue][~music][!mood]      every part optional; "" = the engine's own choice
#   shot   ext (the city outside, then the room)  wide (both of you, from out front)  two (a medium two-shot from the side)
#          cu (the speaker, forty degrees off the eye-line)  ots (over the listener's shoulder)  push (a long slow push-in)
#          ecu (the eyes)  low (from the floor: the hero, the trophy)  high (from above: small in a big room)  hands (an insert)
#   ^      the camera is on you, not on the other person (narration is on the other person unless you say so)
#   act    a Mixamo clip for the speaker (in narration: for you)      react  a clip for the listener (in narration: for them)
#          Idle, Breathing Idle, Talking, Standing Arguing, Agreeing, Shaking Head No, Head Nod Yes, Thinking, Looking Around,
#          Shrugging, Angry Gesture, Pointing, Clapping, Texting While Standing, Shaking Hands 2, Victory, Cheering, Fist Pump,
#          Defeated, Sad Idle, Sitting Idle, Sitting Talking, Sitting Disbelief, Sitting Clap, Sit To Stand, Stand To Sit, Walking
#   cue    a sting as the line lands: sting (dark hit) swell (warm) bell (the diner door) crowd (applause) flash (cameras) hush (a room going quiet) chips tick text
#   music  the score under the line: night grind tension triumph none
#   mood   the face and body: win cash bust cooler think tense quiet
import json, os, re
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Dream", "Resources", "Dream", "direction.json")

S = {}
def scene(id, *lines): S[id] = list(lines)

# ------------------------------------------------------------ prologue
scene("p_office",
    ("n",     "ext~night"),                                   # Brickell going pink
    ("derek", "ots:Pointing"),
    ("you",   "cu:Talking"),
    ("derek", "cu:Standing Arguing"),
    ("you",   "ecu:Head Nod Yes"),                            # "No problem, Lou." the smallest nod
    ("derek", "ots:Pointing"),
    ("n",     "wide^:Thinking@swell"),                        # you were nine
    ("n",     "push^:Thinking"),
    ("you",   "ecu:Thinking@sting"))                          # the lanyard
scene("p_diner",
    ("n",     "ext~night"),
    ("slick", "two:Talking"),                                 # the stranger, first seen in a two-shot at the counter
    ("you",   "cu:Shrugging"),
    ("slick", "ots:Talking"),
    ("you",   "cu:Talking"),
    ("slick", "push:Talking~none"),                           # the question: the score drops out
    ("n",     "hands@tick~tension"),                          # the red envelope
    ("slick", "cu:Pointing"),
    ("you",   "cu:Thinking"),
    ("slick", "push:Talking@swell"),                          # I sat on that stool once
    ("slick", "cu:Pointing>Head Nod Yes"),                    # three things
    ("you",   "ots:Talking"),
    ("slick", "ecu:Talking@sting"),                           # People call me Slick
    ("n",     "wide^:Looking Around@bell"),                   # gone before the bell stops
    ("n",     "ecu^:Thinking@swell~night"))                   # the line in pen
scene("p_seat",
    ("n",     "ext~grind"),                                   # the card room, Saturday late morning
    ("rosa",  "two:Talking"),
    ("you",   "cu:Talking"),                                  # somebody bought me a seat
    ("rosa",  "ots:Pointing>Head Nod Yes"),                   # table nine, seat four
    ("n",     "push^:Looking Around@hush~tension"))           # hands shaking anyway
scene("p_after",
    ("derek", "ots:Pointing"),
    ("n",     "cu^:Thinking"),
    ("derek", "cu:Angry Gesture@tick"),
    ("n",     "push^:Thinking~night"))                        # your mind never leaves table nine
scene("p_offer",
    ("n",     "wide^:Texting While Standing@text~night"),     # the phone buzzes at home
    ("slick", ""), ("slick", ""), ("slick", ""),
    ("n",     "push^:Thinking@sting~none"))                   # decision time, in silence
scene("p_quit",
    ("you",   ""),                                            # the text to Lou
    ("n",     "low^:Fist Pump@swell~grind"),                  # never felt lighter
    ("slick", ""), ("slick", ""), ("mom", ""))
scene("p_keep",
    ("n",     "high^:Thinking~night"),
    ("n",     "cu^:Texting While Standing"),
    ("slick", ""))

# ------------------------------------------------------------ chapter openers
scene("c1",
    ("n",     "ext~grind"),
    ("rosa",  "two:Pointing"),
    ("lou",   "ots:Talking>Head Nod Yes"),
    ("trent", "cu:Shrugging@tick"),
    ("n",     "push:Looking Around"),                         # Mustich, sized up
    ("n",     "wide^@swell"))
scene("trentbeat",
    ("n",     "wide^:Looking Around@hush~tension"),           # heads-up, the room on its chairs
    ("n",     "wide^:Victory@crowd~triumph!win"),
    ("trent", "cu:Shrugging>Fist Pump"),
    ("lou",   "ots:Head Nod Yes@swell"),
    ("rosa",  "two:Clapping>Victory@crowd"))
scene("c2",
    ("slick", "ext~grind"),
    ("n",     "wide^:Looking Around"),
    ("L:moneybaker", "two:Talking>Head Nod Yes"),
    ("trent", "cu:Pointing@tick"),
    ("n",     "wide^@swell"))
scene("c3",
    ("n",     "ext~grind"),
    ("slick", "two:Pointing"),
    ("n",     "cu^:Looking Around@hush~tension"),             # the man in dark glasses
    ("ghost", "ecu:Talking@sting"),
    ("L:hellbent", "ots:Shrugging"),
    ("n",     "low^@swell~grind"))
scene("c4",
    ("n",     "ext@flash~grind"),
    ("host",  "two:Pointing"),
    ("you",   "push:Talking@swell"),                          # People insist on calling it luck
    ("host",  "cu:Clapping@flash"),
    ("n",     "wide^:Looking Around"),
    ("slick", "ots:Pointing>Head Nod Yes"),
    ("L:kfoxglove", "cu:Shaking Head No"),
    ("mom",   ""),
    ("n",     "wide^@swell"))
scene("c5",
    ("n",     "ext~grind"),
    ("n",     "wide^:Looking Around~tension"),
    ("trent", "two:Shrugging"),
    ("you",   "cu:Talking"),
    ("L:loophole", "ots:Thinking"),
    ("slick", "push:Pointing>Head Nod Yes@swell"),
    ("mom",   ""),
    ("n",     "low^@swell~grind"))
scene("c6",
    ("n",     "ext@crowd~grind"),
    ("n",     "wide^:Looking Around"),
    ("L:mizrockie", "cu:Pointing@sting"),
    ("ghost", "ecu:Talking~tension"),
    ("mom",   ""),
    ("slick", "push:Talking@swell~grind"),
    ("n",     "low^@swell"))
scene("c7",
    ("n",     "ext@swell~triumph"),
    ("n",     "wide^:Looking Around"),
    ("ghost", "low:Pointing@sting~tension"),
    ("slick", "two:Talking>Head Nod Yes@swell~night"),
    ("you",   "cu:Head Nod Yes"),
    ("n",     "push^@swell"))

# ------------------------------------------------------------ the lessons (the first bust of each chapter)
scene("lesson1",
    ("n",     "high^:Sad Idle@hush~night"),
    ("slick", "two:Talking"),
    ("you",   "cu:Shaking Head No"),
    ("slick", "push:Pointing@sting"),
    ("slick", "cu:Pointing>Head Nod Yes@swell"))
scene("lesson2",
    ("n",     "wide^:Looking Around~night"),
    ("slick", "ots:Talking"),
    ("you",   "cu:Shrugging"),
    ("slick", "push:Talking@sting"),
    ("slick", "cu:Pointing>Head Nod Yes@swell"))
scene("lesson3",
    ("n",     "high^:Sad Idle@hush~tension"),
    ("slick", "ots:Talking"),
    ("you",   "cu:Standing Arguing"),
    ("slick", "push:Pointing@sting~night"),
    ("slick", "two:Talking>Head Nod Yes@swell"))
scene("lesson4",
    ("n",     "wide^:Looking Around~night"),
    ("slick", "cu:Talking"),
    ("you",   "cu:Shrugging"),
    ("slick", "push:Pointing@sting"),
    ("slick", "two:Pointing>Head Nod Yes@swell"))
scene("lesson5",
    ("n",     "wide^:Thinking~tension"),
    ("slick", "ots:Shrugging"),
    ("you",   "cu:Shaking Head No"),
    ("slick", "push:Pointing@sting~night"),
    ("slick", "two:Talking>Head Nod Yes@swell"))

# ------------------------------------------------------------ milestones
scene("firstwin",
    ("slick", "two:Clapping>Victory@crowd~triumph"),
    ("slick", "cu:Pointing"),
    ("n",     "hands^@swell"))                                # the sunglasses
scene("ring1",
    ("n",     "hands^:Fist Pump@chips~triumph"),
    ("L:moneybaker", "two:Pointing>Head Nod Yes@swell"),
    ("slick", ""))
scene("bracelet1",
    ("n",     "low^:Victory@crowd~triumph!win"),
    ("L:hellbent", "cu:Shrugging@tick"),
    ("mom", ""), ("slick", ""))
scene("tour1",
    ("host",  "two:Pointing@flash~triumph"),
    ("L:kfoxglove", "cu:Pointing>Head Nod Yes"))
scene("titan1",
    ("n",     "low^:Fist Pump@crowd~triumph!win"),
    ("trent", "two:Shaking Hands 2>Shaking Hands 2@swell"),
    ("you",   "cu:Shrugging"),
    ("trent", "cu:Pointing@tick"))
scene("champ",
    ("n",     "wide^:Victory@crowd~triumph!win"),
    ("ghost", "two:Shaking Hands 2>Shaking Hands 2@hush~none"),   # Good game, kid. the room quiet
    ("n",     "ecu@swell"),                                   # the glasses come off
    ("ghost", "cu:Pointing~tension"),
    ("derek", ""),
    ("n",     "cu^:Texting While Standing@tick"),
    ("slick", "push:Talking@swell~triumph"),
    ("you",   "low:Head Nod Yes@sting"))                      # Let them.
scene("badbeat",
    ("n",     "high^:Sad Idle@hush~tension"),
    ("you",   "cu:Standing Arguing"),
    ("slick", "ots:Talking>Shaking Head No"),
    ("slick", "push:Pointing@swell~night"),
    ("trent", ""),
    ("you",   "cu:Shaking Head No"),
    ("slick", "two:Head Nod Yes>Head Nod Yes@swell"))
scene("broke",
    ("n",     "high^:Sad Idle@hush~tension"),
    ("slick", "ots:Talking"),
    ("you",   "cu:Shaking Head No"),
    ("slick", "push:Talking@swell~night"),
    ("slick", "two:Pointing~none"))
scene("broke_n",
    ("n",     "high^:Sad Idle@hush~tension"),
    ("slick", "ots:Talking"),
    ("you",   "cu:Shaking Head No"),
    ("slick", "push:Talking@swell~night"),
    ("slick", "two:Pointing~none"))
scene("broke_told",
    ("n",     "high^:Sad Idle@hush~tension"),
    ("slick", "cu:Shaking Head No@sting"),                    # I told you
    ("you",   "ecu:Defeated"),
    ("slick", "ots:Pointing"),
    ("slick", "push:Talking~night"))
scene("slick_back",
    ("n",     "cu^:Texting While Standing~night"),
    ("you", ""), ("slick", ""), ("slick", ""))
scene("lou_no",
    ("derek", "ots:Shaking Head No@sting~tension"),
    ("n",     "high^:Sad Idle"),
    ("slick", ""))
scene("lou_no2",
    ("derek", "ots:Shaking Head No@sting~tension"),
    ("n",     "high^:Sad Idle"))
scene("classic_day1",
    ("n",     "wide^:Fist Pump@chips~grind"),                 # bags and tags
    ("rosa",  "two:Pointing"),
    ("trent", "cu:Shrugging@tick"),
    ("slick", ""))
scene("classic_mustich",
    ("n",     "high^:Sad Idle~tension"),                      # your finish
    ("n",     "wide:Fist Pump@hush"),                         # the final hand, from the rail
    ("n",     "low:Victory@crowd~triumph"),                   # the trophy over his head
    ("trent", "cu:Pointing>Clapping"),
    ("you",   "cu:Shaking Head No"),
    ("lou",   "ots:Talking>Head Nod Yes@swell~night"))
scene("classic_shark",
    ("n",     "high^:Sad Idle~tension"),
    ("n",     "wide:Thinking@hush"),
    ("n",     "low:Victory@crowd~triumph"),                   # thirty years
    ("lou",   "cu:Cheering>Clapping"),
    ("you",   "cu:Clapping"),
    ("rosa",  "two:Clapping@swell~night"))
scene("classic_other",
    ("n",     "high^:Sad Idle~tension"),
    ("n",     "wide:Fist Pump@hush"),
    ("n",     "low:Victory@crowd~triumph"),
    ("",      "cu:Victory>Clapping"),                         # the winner: whoever it was this month
    ("you",   "cu:Clapping"),
    ("rosa",  "two:Clapping@swell~night"))
# the final table: the speaker in the middle is whoever's there, so it matches any speaker
scene("ft_intro",
    ("n",     "wide^:Looking Around@hush~tension"),
    ("",      "cu:Pointing"),
    ("you",   "push:Head Nod Yes@sting"))
scene("ft_won",
    ("n",     "low^:Victory@crowd~triumph"),
    ("",      "two:Shaking Hands 2>Shaking Hands 2"),
    ("you",   "cu:Head Nod Yes@swell"))
scene("ft_lost",
    ("n",     "high^:Defeated@hush~tension"),
    ("",      "cu:Fist Pump"),
    ("you",   "cu:Shaking Head No~night"))
scene("rehire",
    ("derek", "ots:Pointing@sting~tension"),
    ("n",     "high^:Sad Idle"))
scene("quit2",
    ("you",   "low:Pointing@sting~grind"),
    ("derek", "cu:Shrugging"),
    ("n",     "wide^@swell"))
scene("quit250",
    ("mom",   ""), ("mom", ""),
    ("you",   "push^:Head Nod Yes@sting"),
    ("n",     "wide^:Looking Around@swell~triumph"),
    ("slick", ""))
scene("derek", ("derek", ""))
scene("derek_job", ("derek", ""))
for i in range(1, 5): scene(f"david{i}", ("mom", ""), ("mom", ""))

# ------------------------------------------------------------ the walk away from the table (the text is built at runtime)
scene("after_win",    ("n", "wide@crowd"), ("you", "low:Victory"))
scene("after_cash",   ("n", "wide@chips"), ("you", "cu:Fist Pump"))
scene("after_bust",   ("n", "high@hush"),  ("you", "cu:Defeated"))
scene("after_cooler", ("n", "high@hush~tension"), ("you", "ecu:Shaking Head No"))

# ------------------------------------------------------------ parse and write
SHOTS = {"ext", "wide", "two", "cu", "ots", "push", "ecu", "low", "high", "hands"}
CUES = {"sting", "swell", "bell", "crowd", "flash", "hush", "chips", "tick", "text", "win", "bust", "cut"}
MUSIC = {"night", "grind", "tension", "triumph", "none"}
RX = re.compile(r"^(?P<shot>[a-z]*)(?P<on>\^)?(?::(?P<act>[^>@~!]+))?(?:>(?P<react>[^@~!]+))?(?:@(?P<cue>[a-z]+))?(?:~(?P<music>[a-z]+))?(?:!(?P<mood>[a-z]+))?$")
def parse(sp, d):
    m = RX.match(d.strip()); assert m, f"bad direction {d!r} for {sp}"
    e = {"sp": sp}
    g = m.groupdict()
    if g["shot"]: assert g["shot"] in SHOTS, g["shot"]; e["shot"] = g["shot"]
    if g["on"]: e["on"] = "you"
    if g["act"]: e["act"] = g["act"].strip()
    if g["react"]: e["react"] = g["react"].strip()
    if g["cue"]: assert g["cue"] in CUES, g["cue"]; e["cue"] = g["cue"]
    if g["music"]: assert g["music"] in MUSIC, g["music"]; e["music"] = g["music"]
    if g["mood"]: e["mood"] = g["mood"]
    return e
out = {sid: [parse(sp, d) for sp, d in lines] for sid, lines in S.items()}
json.dump(out, open(OUT, "w"), indent=0, separators=(",", ":"))
n = sum(len(v) for v in out.values()); shots = sum(1 for v in out.values() for e in v if "shot" in e)
print(f"{len(out)} scenes, {n} lines, {shots} with a shot -> {os.path.relpath(OUT)}")
