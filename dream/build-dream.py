# Build the Dream beta: the trainer-work app with The Dream in place of the Arena.
#   python3 dream/build-dream.py   → writes app/slick-rng.html from dream/base.html + the dream sources
import pathlib, re
root = pathlib.Path(__file__).resolve().parent
s = (root / 'base.html').read_text(encoding='utf-8')

def rep(old, new, count=1):
    global s
    assert s.count(old) == count, (old[:70], s.count(old))
    s = s.replace(old, new)

rep('family=Press+Start+2P&display=swap', 'family=Press+Start+2P&family=Bodoni+Moda:ital,opsz,wght@0,6..96,500;0,6..96,600;1,6..96,400;1,6..96,500;1,6..96,600&display=swap')
css = (root / 'dream.css').read_text(encoding='utf-8')
i = s.index('</style>'); s = s[:i] + css + '\n' + s[i:]
rep('<div class="arena" id="arena"></div>', '<div class="dream v-title" id="dream"></div>')
import re as _re
s, k = _re.subn(r'(<button class="half h-trainer" id="goTrainer" type="button">)<svg.*?</svg><b>Trainer</b><small>Drill spots · find leaks</small>', lambda m: m.group(1) + (root / 'crown.svg').read_text() + '<b>The Dream</b><small>Cubicle to legend</small>', s, count=1, flags=_re.S); assert k == 1
s, n = re.subn(r'<span class="ver">BETA ([0-9.]+)</span>', r'<span class="ver">BETA \1 · DREAM</span>', s, count=1); assert n == 1
rep('<title>Confirmed Fire Poker</title>', '<title>Confirmed Fire Dream</title>')
js = '\n'.join((root / f).read_text(encoding='utf-8') for f in ['data.js', 'spots.js', 'story.js', 'avatar.js', 'avatar3d.js', 'av3d-head.js', 'av3d-hair.js', 'props.js', 'engine.js', 'modes.js', 'progression.js', 'fashion.js', 'ui.js'])
rep("AR = { st: arLoad(), view: 'hub', tab: 'ranked', period: 'week', run: null, saved: null, last: null };",
    "AR = { st: arLoad(), view: 'hub', tab: 'ranked', period: 'week', run: null, saved: null, last: null };\n" + js +
    "\nDR = { st: drLoad(), view: 'title', tab: 'events', run: null, sc: null, last: null, saved: null, draft: { name: '', look: { ...DR_LOOK_DEFAULT } } };\n"
    "if (DR.st.made) DR.view = 'hub';\nwindow.__dream = { get DR() { return DR; }, drPlayScene, drLib, DR_SCENES, DR_PEOPLE, drSceneEnd, drRender, drAdvance, drUnlocks, drStart, drProgress, drSave, drFinish, drStagesFor, drCoolerRoll, drDefault, drAvailable, drAfford, drPick, drAdd, drRisk, drSngStart, drLgSettle, drModeState, drLgBoard, drSeasonFor, DR_LG_ITEMS, _solve: (gid, seed) => { TR.random = true; window.__trRand = true; applySetup(arSetup(gid, arRng(seed))); }, _pf: () => pfState, _query: (path) => pfQueryAsync(path), _cls: (a, b) => Engine.CLASS_OF[Engine.comboIndex(a, b)], _cname: (c) => Engine.CLASS_NAMES[c], _snap: () => snapSetup(), _apply: (x) => applySetup(x), _cfg: () => pfConfig(), _view: (sp) => { const k = TR.spot; TR.spot = sp; const v = trSpotView(); TR.spot = k; return v; }, _seats: () => T.seats.length }; // for automated play-testing\nwindow.__dreamAv = drAvatarSVG;\nwindow.__dr3 = { dr3Snap, dr3Draw, dr3Init, DR3, dr3Head, dr3Ring, dr3Build, dr3SkinTex, dr3HeadMat };\n")
rep("  if (m !== 'trainer') arLeave();\n  if (m === 'trainer') { trRender(); arRender(); }",
    "  if (m !== 'trainer') { arLeave(); drLeave(); }\n  if (m === 'trainer') { trRender(); arRender(); drRender(); }")
rep("$('goTrainer').onclick = () => showVariants('trainer');", "$('goTrainer').onclick = () => { setVariant('nlh'); setMode('trainer'); hideLaunch(); };")
three = (root / 'vendor' / 'three.min.js').read_text(encoding='utf-8')
assert s.count('<script>\n(() => {') == 1
s = s.replace('<script>\n(() => {', '<script>' + three + '</script>\n<script>\n(() => {', 1)
out = root.parent / 'app' / 'slick-rng.html'
out.write_text(s, encoding='utf-8')
print('built', out, len(s))
