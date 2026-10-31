#!/usr/bin/env python3
"""Copy the frozen per-class pot corrections (model_pot.json) into app/engine.js, so the page's own solver (spots
outside the library) plays the same model as the library. Run after fit_pot.py FREEZE=1, then rebuild the page."""
import json, re, os
os.chdir(os.path.dirname(os.path.abspath(__file__)))
d = json.load(open('model_pot.json')); p = '../../app/engine.js'; s = open(p).read()
fmt = lambda a: '[' + ','.join(('%.3f' % x).rstrip('0').rstrip('.') if abs(x) > 5e-4 else '0' for x in a) + ']'
body = f"PLAY_RE.set({fmt(d['re'])});\nPLAY_HU.set({fmt(d['hu'])});\n"
s2 = re.sub(r'(// FITTED_POT_BEGIN[^\n]*\n).*?(// FITTED_POT_END)', lambda m: m[1] + body + m[2], s, flags=re.S)
assert s2 != s or body in s; open(p, 'w').write(s2); print('embedded', sum(abs(x) > 5e-4 for x in d['re'] + d['hu']), 'nonzero values')
