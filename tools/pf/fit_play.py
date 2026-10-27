#!/usr/bin/env python3
"""Coordinate search over the playability family constants, scored by hand agreement with the saved GW nodes
(offsuit junk excluded: the course folds those on purpose). Logs every step to fit_log.txt; best set in fit_best.json."""
import json, subprocess, os, re, sys
os.chdir(os.path.dirname(os.path.abspath(__file__)))
D = json.loads(subprocess.check_output(['node', '-e', "console.log(JSON.stringify(require('../../app/engine.js').PLAY_DEFAULT))"]))
FIT = ['sGap1', 'sGap2', 'sGap3', 'sGap4', 'sBroad', 'saWheel', 'saOther', 'pairLo', 'pairMid']
def score(tag, cur):
    env = dict(os.environ, PLAYSET=json.dumps(cur), EXCL='1', CAL_SECS=os.environ.get('CAL_SECS', '240'))
    out = subprocess.run(['./calib.sh', tag, 'A=1'], env=env, capture_output=True, text=True).stdout
    m = re.search(r'([\d.]+)% over (\d+)', out); return float(m.group(1)) if m else -1
log = open('fit_log.txt', 'a'); cur = dict(D)
best = score('fit0', cur); print('base', best, file=log, flush=True)
for rnd in range(2):
    step = 0.03 if rnd == 0 else 0.015
    for k in FIT:
        cand = []
        for sgn in (+1, -1):
            c = dict(cur); c[k] = round(cur[k] + sgn * step, 4)
            cand.append((score(f'fit_{k}_{sgn}', c), c))
        sc, c = max(cand, key=lambda x: x[0])
        print(f'round {rnd} {k}: base {best} +:{cand[0][0]} -:{cand[1][0]}', file=log, flush=True)
        if sc > best + 0.4: best, cur = sc, c; print(f'  accept {k}={cur[k]} -> {best}', file=log, flush=True); json.dump(cur, open('fit_best.json', 'w'))
print('DONE best', best, json.dumps(cur), file=log, flush=True)
