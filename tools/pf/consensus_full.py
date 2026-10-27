#!/usr/bin/env python3
"""Merge three full-library passes: per table, per node, per hand. Usage: consensus_full.py OUTDIR PASSDIR1 PASSDIR2 PASSDIR3
A hand's strategy ships (mean of the three solves) only if every action frequency agrees within TOL across all three;
otherwise the hand is flagged 'x' and the page shows it as unverified instead of presenting it as solved."""
import json, sys, os, base64, glob
import numpy as np
TOL = float(os.environ.get('TOL', 0.12))
out, *passes = sys.argv[1:]
os.makedirs(out, exist_ok=True)
stat = {}
for f in sorted(glob.glob(passes[0] + '/*.json')):
    b = os.path.basename(f)
    ds = []
    for p in passes:
        try: ds.append(json.load(open(os.path.join(p, b))))
        except Exception: ds = None; break
    if not ds: print('skip', b); continue
    idx = [{n['p']: n for n in d['nodes']} for d in ds]
    nodes = []; tot = flag = dropped = 0
    for n0 in ds[0]['nodes']:
        ns = [ix.get(n0['p']) for ix in idx]
        if any(x is None or x['a'] != n0['a'] for x in ns): dropped += 1; continue
        na = len(n0['a'])
        S = np.stack([np.frombuffer(base64.b64decode(x['s']), np.uint8).reshape(169, na).astype(float) / 255 for x in ns])
        E = np.stack([np.frombuffer(base64.b64decode(x['e']), np.int16).reshape(169, na).astype(float) for x in ns])
        R = np.frombuffer(base64.b64decode(n0['r']), np.uint8)
        spread = (S.max(0) - S.min(0)).max(1)
        bad = (spread > TOL) & (R > 25)          # only hands that actually reach this node count
        m = S.mean(0); m8 = np.round(m * 255).astype(np.uint8)
        em = np.round(E.mean(0)).astype(np.int16)
        w = (R > 25).sum(); tot += w; flag += int(bad.sum())
        nodes.append({'p': n0['p'], 'h': n0['h'], 'a': n0['a'], 'to': n0['to'], 'lab': n0['lab'], 'prob': n0['prob'],
                      's': base64.b64encode(m8.tobytes()).decode(), 'r': n0['r'], 'e': base64.b64encode(em.tobytes()).decode(),
                      'x': base64.b64encode(np.packbits(bad.astype(np.uint8)).tobytes()).decode(), 'nx': int(bad.sum())})
    d0 = ds[0]
    json.dump({'file': d0['file'], 'names': d0['names'], 'stacks': d0['stacks'], 'ante': d0['ante'], 'iters': [d['iters'] for d in ds], 'nodes': nodes}, open(os.path.join(out, b), 'w'), separators=(',', ':'))
    stat[b] = (len(nodes), dropped, round(100 * flag / max(1, tot), 2), round(os.path.getsize(os.path.join(out, b)) / 1e6, 2))
    print(b, 'nodes', len(nodes), 'dropped', dropped, 'flagged hands %', stat[b][2], 'MB', stat[b][3])
