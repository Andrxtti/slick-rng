#!/usr/bin/env python3
"""Range strings (hand:weight) from one of our library solves: the reach of a player's hands at a node, times one action.
   python3 realize/librange.py <lib.json> <path|keys> <seat> [action]   e.g. "fold|fold|fold|fold|fold|raise|fold" 7 raise"""
import json, sys, base64, numpy as np
SC = '/tmp/claude-0/-home-claude/c819b688-727f-5a5a-aefc-c9c8a5a8f2a2/scratchpad/'
cl = json.load(open(SC + 'cls.json')); N = cl['n']
J = json.load(open(sys.argv[1])); nodes = {n['p']: n for n in J['nodes']}
def S(n): return np.frombuffer(base64.b64decode(n['s']), np.uint8).reshape(169, len(n['a'])) / 255
def reach(path, seat):
    keys = path.split('|') if path else []; r = np.ones(169)
    for i in range(len(keys)):
        p = '|'.join(keys[:i]); n = nodes[p]
        if n['h'] != seat: continue
        j = n['a'].index(keys[i]); r = r * S(n)[:, j]
    return r
path, seat = sys.argv[2], int(sys.argv[3]); r = reach(path, seat)
if len(sys.argv) > 4:
    n = nodes[path]; assert n['h'] == seat; a = sys.argv[4]; j = n['a'].index(a); r = r * S(n)[:, j]
print(','.join(f'{N[i]}:{r[i]:.3f}' for i in range(169) if r[i] > 0.004))
