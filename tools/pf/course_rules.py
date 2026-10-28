#!/usr/bin/env python3
"""Course rules applied to a finished library (run after consensus_full.py, before build_gates.py).
Usage: course_rules.py LIBDIR

Rule 1 (Steven, Oct 2026): in a multiway pot the big blind folds offsuit junk. At the big blind's decision after one
open and at least one flat (no 3-bet yet), every offsuit hand without an ace and without two broadway cards has its
call share moved to fold. The low offsuit connectors 54o, 65o, 76o and 87o are left to the solver: the course treats
them as the playable offsuit hands there (their straights don't collide with the openers' cards).
Chip EV calls many of these hands (very good pot odds); the EVs stay as solved, so the hand detail still shows that the
call is close in chips, and Learn > Multiway pots explains the tournament reason for the fold.
Idempotent: re-running changes nothing."""
import json, sys, glob, os, base64
import numpy as np
import subprocess
HERE = os.path.dirname(os.path.abspath(__file__))
N = json.loads(subprocess.run(['node', '-e', "process.stdout.write(JSON.stringify(require('../../app/engine.js').CLASS_NAMES))"], cwd=HERE, capture_output=True, text=True).stdout)   # the library's class order
assert len(N) == 169
KEEP = {'54o', '65o', '76o', '87o'}
JUNK = np.array([nm[-1] == 'o' and nm[0] != 'A' and not (nm[0] in 'TJQK' and nm[1] in 'TJQK') and nm not in KEEP for nm in N])
lib = sys.argv[1]; total = 0
for f in sorted(glob.glob(os.path.join(lib, 'm*-d*.json'))):
    J = json.load(open(f)); bb = len(J['names']) - 1; moved = 0; nodes_hit = 0
    for n in J['nodes']:
        k = n['p'].split('|') if n['p'] else []
        if n['h'] != bb or k.count('raise') != 1 or k.count('call') < 1 or 'allin' in k or 'call' not in n['a'] or 'fold' not in n['a']: continue
        na = len(n['a']); S = np.frombuffer(base64.b64decode(n['s']), np.uint8).reshape(169, na).astype(np.int32).copy()
        ci, fi = n['a'].index('call'), n['a'].index('fold')
        m = JUNK & (S[:, ci] > 0)
        if not m.any(): continue
        S[m, fi] += S[m, ci]; S[m, ci] = 0; S = np.clip(S, 0, 255)
        n['s'] = base64.b64encode(S.astype(np.uint8).tobytes()).decode(); moved += int(m.sum()); nodes_hit += 1
    if moved:
        json.dump(J, open(f, 'w'), separators=(',', ':'))
    total += moved
    print(os.path.basename(f), 'BB multiway junk calls folded:', moved, 'hands in', nodes_hit, 'nodes')
print('total', total)
