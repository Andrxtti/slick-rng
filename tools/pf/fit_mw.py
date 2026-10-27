#!/usr/bin/env python3
"""Fit per-class multiway playability corrections to the saved GW nodes (multiway = an open plus at least one flat).
Each iteration: solve 100bb + 25bb with the current corrections, measure per class (GW continue - ours continue) over the
multiway nodes, nudge the class's correction. Offsuit junk is never moved (the course folds it). Logs to fit_mw_log.txt."""
import json, subprocess, os, re, glob, base64, sys
import numpy as np
os.chdir(os.path.dirname(os.path.abspath(__file__)))
SC = '/tmp/claude-0/-home-claude/c819b688-727f-5a5a-aefc-c9c8a5a8f2a2/scratchpad/'
cl = json.load(open(SC + 'cls.json')); N = cl['n']; SZ = np.array(cl['s'], float); IDX = {n: i for i, n in enumerate(N)}
JUNK = np.array([n[-1] == 'o' and len(n) == 3 and n[0] != 'A' and not (n[0] in 'TJQK' and n[1] in 'TJQK') for n in N])
GW = []   # (depth, path, seat, {class: continue prob})
for f in sorted(glob.glob('gw/gw*.txt')):
    for line in open(f).read().strip().split('\n'):
        key, body = line.split('\t'); st, path, seat = key.split('|'); d = round(float(st))
        if d not in (25, 100) or 'C' not in path.split('-'): continue
        g = {}
        for tk in body.split():
            h, codes = tk.split(':'); o = [0, 0, 0]
            for m in re.finditer(r'([arcf])(\d+)', codes): o[0 if m[1] == 'f' else 1 if m[1] == 'c' else 2] += int(m[2]) / 100
            if h in IDX and sum(o) > 0.5: g[IDX[h]] = 1 - o[0]
        GW.append((d, path, g))
def ours_cont(d, path, tag):
    j = json.load(open(f'calib/{tag}_{d}.json')); nodes = {n['p']: n for n in j['nodes']}
    keys = ['fold' if t == 'F' else 'call' if t == 'C' else 'raise' for t in path.split('-')]; n = nodes.get('|'.join(keys))
    if not n: return None
    na = len(n['a']); S = np.frombuffer(base64.b64decode(n['s']), np.uint8).reshape(169, na) / 255
    return 1 - S[:, n['a'].index('fold')] if 'fold' in n['a'] else np.ones(169)
def score(tag, env_extra):
    env = dict(os.environ, CAL_SECS=os.environ.get('CAL_SECS', '240'), **env_extra)
    return subprocess.run(['./calib.sh', tag, 'A=1'], env=env, capture_output=True, text=True).stdout.strip()
log = open('fit_mw_log.txt', 'a'); delta = np.zeros(169); gain = float(os.environ.get('GAIN', 0.5)); lim = 0.25
if os.path.exists('fit_mw_best.json') and os.environ.get('RESUME'): delta = np.array(json.load(open('fit_mw_best.json')))
for it in range(int(os.environ.get('ITERS', 8))):
    json.dump(delta.tolist(), open(f'calib/mw_delta_{it}.json', 'w')); tag = f'mw{it}'
    out = score(tag, {'PLAYMW': f'calib/mw_delta_{it}.json', 'EXCL': '1'})
    diff = np.zeros(169); cnt = np.zeros(169); mw_err = []; hold_err = []
    for ni, (d, path, g) in enumerate(GW):
        oc = ours_cont(d, path, tag)
        if oc is None: continue
        held = ni % 3 == 0     # every third node is held out of the fit and only scored
        for c, gc in g.items():
            if JUNK[c]: continue
            if held: hold_err.append(abs(gc - oc[c]) * SZ[c]); continue
            diff[c] += gc - oc[c]; cnt[c] += 1; mw_err.append(abs(gc - oc[c]) * SZ[c])
    m = np.where(cnt > 0, diff / np.maximum(cnt, 1), 0)
    print(f'iter {it}: {out} | multiway mean |continue error| {np.sum(mw_err)/sum(SZ[c] for _ in [0] for c in [0]) if False else np.mean(mw_err)/np.mean(SZ):.3f}  mean signed (GW-ours) {np.mean(m[cnt>0]):+.3f} | HELD-OUT nodes mean |error| {np.mean(hold_err)/np.mean(SZ):.3f}', file=log, flush=True)
    delta = np.clip(delta + gain * m * np.minimum(cnt, 6) / 6, -lim, lim)
    json.dump(delta.tolist(), open('fit_mw_best.json', 'w'))
print('DONE', file=log, flush=True)
