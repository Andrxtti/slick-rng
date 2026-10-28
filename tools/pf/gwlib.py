#!/usr/bin/env python3
"""Compare the shipped library (app/lib) with the GTO Wizard nodes saved in gw/*.txt (local only).
Hand agreement = combo-weighted share of the fold/call/raise mix two sources have in common, over hands GW lists at the node."""
import json, re, sys, glob, base64
import numpy as np
SC = '/tmp/claude-0/-home-claude/c819b688-727f-5a5a-aefc-c9c8a5a8f2a2/scratchpad/'
cl = json.load(open(SC + 'cls.json')); NAMES = cl['n']; SIZE = np.array(cl['s'], float); IDX = {n: i for i, n in enumerate(NAMES)}
LIB = {}
OVR = {}   # depth -> path to a solve_full output (calibration runs); default is the shipped library
def lib(d):
    if d not in LIB:
        j = json.load(open(OVR.get(d) or f'../../app/lib/m8-d{d}.json')); LIB[d] = {n['p']: n for n in j['nodes']}
    return LIB[d]
import os
QUIET = bool(os.environ.get('QUIET'))
EXCL = np.array([nm[-1] == 'o' and len(nm) == 3 and nm[0] != 'A' and not (nm[0] in 'TJQK' and nm[1] in 'TJQK') for nm in NAMES])   # offsuit junk: the course folds these, so they are not scored against GW
for a in sys.argv[1:]:
    d0, p0 = a.split('='); OVR[int(d0)] = p0
rows = []
for f in sorted(glob.glob('gw/gw*.txt')):
    for line in open(f).read().strip().split('\n'):
        key, body = line.split('\t'); st, path, seat = key.split('|'); d = round(float(st))
        toks = path.split('-'); keys = ['fold' if t == 'F' else 'call' if t == 'C' else 'raise' for t in toks]
        if OVR and d not in OVR: continue
        try: nodes = lib(d)
        except Exception: continue
        n = nodes.get('|'.join(keys))
        if n is None: rows.append((d, path, None, None, None, None)); continue
        na = len(n['a']); S = np.frombuffer(base64.b64decode(n['s']), np.uint8).reshape(169, na) / 255
        X = np.unpackbits(np.frombuffer(base64.b64decode(n['x']), np.uint8))[:169].astype(bool) if 'x' in n else np.zeros(169, bool)
        # jams are scored apart from smaller raises when our node offers both (JAMSEP=0 merges them, the old metric)
        sep = os.environ.get('JAMSEP', '1') != '0' and 'allin' in n['a'] and any(a not in ('fold', 'call', 'check', 'allin') for a in n['a'])
        ours = np.zeros((169, 4))
        for j, a in enumerate(n['a']): ours[:, 0 if a == 'fold' else 1 if a in ('call', 'check') else 3 if (a == 'allin' and sep) else 2] += S[:, j]
        gw = np.full((169, 4), np.nan)
        for tk in body.split():
            h, codes = tk.split(':'); o = [0, 0, 0, 0]
            for m in re.finditer(r'([arcf])(\d+)', codes): o[0 if m[1] == 'f' else 1 if m[1] == 'c' else 3 if (m[1] == 'a' and sep) else 2] += int(m[2]) / 100
            if h in IDX: gw[IDX[h]] = o
        has = ~np.isnan(gw).any(1) & (np.nan_to_num(gw).sum(1) > 0.5) & ~(EXCL if os.environ.get('EXCL') else np.zeros(169, bool))
        ag = (np.minimum(ours, np.nan_to_num(gw)).sum(1) * SIZE)[has].sum() / SIZE[has].sum()
        vf = ((np.minimum(ours, np.nan_to_num(gw)).sum(1) < 0.7) & has)
        wo = (SIZE * (1 - ours[:, 0]))[has].sum() / SIZE[has].sum(); wg = (SIZE * (1 - gw[:, 0]))[has].sum() / SIZE[has].sum()
        rows.append((d, path, ag, wo, wg, [NAMES[i] for i in np.where(vf)[0]][:8], int((X & has).sum())))
tot = []
for r in rows:
    if r[2] is None:
        if not QUIET: print(f'{r[0]:>4}bb {r[1]:<40} not in library')
        continue
    tot.append(r[2])
    if not QUIET: print(f'{r[0]:>4}bb {r[1]:<40} agree {100*r[2]:5.1f}%  width ours {100*r[3]:4.0f} GW {100*r[4]:4.0f}  flagged-in-GW-hands {r[6]}  big misses: {" ".join(r[5])}')
print(('' if QUIET else '\n')+'mean hand agreement with GTO Wizard: %.1f%% over %d nodes' % (100 * np.mean(tot), len(tot)))
