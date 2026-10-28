#!/usr/bin/env python3
"""Write tools/pf/model_real.json from realize/mult_L*.json and embed the tables in app/engine.js (FITTED_REAL markers).
   python3 realize/embed.py            # builds model_real.json from mult_L1/L2/L3.json (missing levels stay at 1) and embeds
"""
import json, os, re
H = os.path.dirname(os.path.abspath(__file__)); PF = os.path.dirname(H); ROOT = os.path.dirname(os.path.dirname(PF))
model = {}
for l in (1, 2, 3):
    f = os.path.join(H, f'mult_L{l}.json')
    if os.path.exists(f): model[f'l{l}'] = json.load(open(f))['mult']
json.dump(model, open(os.path.join(PF, 'model_real.json'), 'w'))
fmt = lambda v: '[' + ','.join(('1' if abs(x - 1) < 5e-4 else f'{x:.3f}'.rstrip('0').rstrip('.')) for x in v) + ']'
body = ''.join(f"REAL_MULT[{l}].set({fmt(model[f'l{l}'])});\n" for l in (1, 2, 3) if f'l{l}' in model)
p = os.path.join(ROOT, 'app', 'engine.js'); s = open(p).read()
s2 = re.sub(r'(// FITTED_REAL_BEGIN[^\n]*\n).*?(// FITTED_REAL_END)', lambda m: m[1] + body + m[2], s, flags=re.S)
assert s2 != s or body == '', 'markers not found'
open(p, 'w').write(s2)
print('embedded levels', sorted(model), 'into app/engine.js; model_real.json written')
