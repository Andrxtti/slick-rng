# Build the Arena (Punch-Out) beta from base.html with the shared solved-hand library.
# usage: python3 dream/build-arena.py OUT.html
import sys, pathlib
root = pathlib.Path(__file__).parent
s = (root / 'base.html').read_text(encoding='utf-8')
lib = (root / 'spots.js').read_text(encoding='utf-8') + '\n' + (root / 'arena-lib.js').read_text(encoding='utf-8') + '\n'
def rep(a, b):
    global s
    assert s.count(a) == 1, a[:70]
    s = s.replace(a, b)
rep('async function arTable() {\n  const run = AR.run; if (!run) return;\n  run.state = \'loading\'; run.spot = null; run.t0 = performance.now(); run.roundStart = run.total; arRender();\n',
    lib + 'async function arTable() {\n  const run = AR.run; if (!run) return;\n  run.state = \'loading\'; run.spot = null; run.t0 = performance.now(); run.roundStart = run.total; arRender();\n'
    + "  const lib = arLibTable(run);\n  if (lib) {\n    window.__pfHold = true; applySetup(lib.s); run.lib = lib; run.seenN = new Set(); run.libSeats = withHist([], () => T.seats.map((x) => ({ id: x.id, pos: x.pos })));\n"
    + "    await sleep(400); if (AR.run !== run) return;\n    run.state = 'bell'; arRender(); arSfx('bell'); await sleep(900); if (AR.run !== run) return; arHand(); return;\n  }\n  run.lib = null; window.__pfHold = false;\n")
rep("  run.state = 'dealing'; arRender();\n  const sp = await arDeal(run.rng); if (AR.run !== run) return;",
    "  run.state = 'dealing'; arRender();\n  if (run.lib) { await sleep(300); if (AR.run !== run) return; run.spot = arLibDeal(run); run.state = 'play'; run.shown = performance.now(); arRender(); return; }\n  const sp = await arDeal(run.rng); if (AR.run !== run) return;")
rep("  if (AR.saved) { applySetup(AR.saved); AR.saved = null; }", "  window.__pfHold = false;\n  if (AR.saved) { applySetup(AR.saved); AR.saved = null; }")
out = pathlib.Path(sys.argv[1]); out.write_text(s, encoding='utf-8'); print('built', out, len(s))
