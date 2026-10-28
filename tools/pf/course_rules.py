#!/usr/bin/env python3
"""Course rules applied to a finished library (run after consensus_full.py, before build_gates.py).
Usage: course_rules.py LIBDIR   (MARGIN=0.15 by default)

Rule 1 (Steven, Oct 2026): in a multiway pot the big blind folds offsuit junk unless the price makes folding a clear
mistake. At the big blind's decision after one open and at least one flat (no 3-bet yet), an offsuit hand without an
ace and without two broadway cards keeps its call only when calling beats folding by at least MARGIN big blinds in
chip EV; otherwise its call share moves to fold. Why a margin: our flop model is kind to junk in multiway pots (it
can't see how often these hands are dominated or bet off the pot), and a tournament stack is worth more than chip EV
says, so a call that is only barely +EV in the model is a fold in a real tournament. Very short stacks (pot odds of
15% or better with little play left) keep their calls; deeper stacks fold them.
Rule 2 (Oct 28): facing a 3-bet or bigger (two or more raises before the decision, no all-in yet), suited junk
(a suited hand whose top card is below an ace, with a gap of three or more and a low card of eight or less) and the
offsuit junk of rule 1, and offsuit aces below ATo, fold unless calling beats folding by at least MARGIN3 (0.5bb). These hands sit at indifference
in the model's 3-bet pots and GTO Wizard folds them; folding costs nothing in chip EV and keeps the ranges clean.
The EVs stay as solved, so the hand detail still shows the call's chip EV. Idempotent: re-running changes nothing."""
import json, sys, glob, os, base64
import numpy as np
import subprocess
HERE = os.path.dirname(os.path.abspath(__file__))
N = json.loads(subprocess.run(['node', '-e', "process.stdout.write(JSON.stringify(require('../../app/engine.js').CLASS_NAMES))"], cwd=HERE, capture_output=True, text=True).stdout)   # the library's class order
assert len(N) == 169
MARGIN = float(os.environ.get('MARGIN', 0.15))
JUNK = np.array([nm[-1] == 'o' and nm[0] != 'A' and not (nm[0] in 'TJQK' and nm[1] in 'TJQK') for nm in N])
R = '23456789TJQKA'
SJUNK = np.array([nm[-1] == 's' and nm[0] != 'A' and R.index(nm[0]) - R.index(nm[1]) >= 3 and R.index(nm[1]) <= R.index('8') for nm in N])
OACE = np.array([nm[-1] == 'o' and nm[0] == 'A' and nm[1] in '23456789' for nm in N])
MARGIN3 = float(os.environ.get('MARGIN3', 0.5))
lib = sys.argv[1]; total = 0
for f in sorted(glob.glob(os.path.join(lib, 'm*-d*.json'))):
    J = json.load(open(f)); bb = len(J['names']) - 1; moved = 0; nodes_hit = 0
    for n in J['nodes']:
        k = n['p'].split('|') if n['p'] else []
        if n['h'] != bb or k.count('raise') != 1 or k.count('call') < 1 or 'allin' in k or 'call' not in n['a'] or 'fold' not in n['a']: continue
        na = len(n['a']); S = np.frombuffer(base64.b64decode(n['s']), np.uint8).reshape(169, na).astype(np.int32).copy()
        ci, fi = n['a'].index('call'), n['a'].index('fold')
        E = np.frombuffer(base64.b64decode(n['e']), np.int16).reshape(169, na) / 100
        m = JUNK & (S[:, ci] > 0) & (E[:, ci] - E[:, fi] < MARGIN)
        if not m.any(): continue
        S[m, fi] += S[m, ci]; S[m, ci] = 0; S = np.clip(S, 0, 255)
        n['s'] = base64.b64encode(S.astype(np.uint8).tobytes()).decode(); moved += int(m.sum()); nodes_hit += 1
    moved3 = 0; nodes3 = 0
    for n in J['nodes']:
        k = n['p'].split('|') if n['p'] else []
        if k.count('raise') < 2 or 'allin' in k or 'call' not in n['a'] or 'fold' not in n['a']: continue
        if k[-1] != 'raise' and not (k[-1] == 'fold' and 'raise' in k[-3:]): continue   # the player is facing the latest raise (or cold folds after it)
        na = len(n['a']); S = np.frombuffer(base64.b64decode(n['s']), np.uint8).reshape(169, na).astype(np.int32).copy()
        ci, fi = n['a'].index('call'), n['a'].index('fold')
        E = np.frombuffer(base64.b64decode(n['e']), np.int16).reshape(169, na) / 100
        m = (JUNK | SJUNK | OACE) & (S[:, ci] > 0) & (E[:, ci] - E[:, fi] < MARGIN3)
        if not m.any(): continue
        S[m, fi] += S[m, ci]; S[m, ci] = 0; S = np.clip(S, 0, 255)
        n['s'] = base64.b64encode(S.astype(np.uint8).tobytes()).decode(); moved3 += int(m.sum()); nodes3 += 1
    if moved or moved3:
        json.dump(J, open(f, 'w'), separators=(',', ':'))
    total += moved + moved3
    print(os.path.basename(f), 'BB multiway junk calls folded:', moved, 'hands in', nodes_hit, 'nodes;', 'vs-3-bet junk calls folded:', moved3, 'hands in', nodes3, 'nodes')
print('total', total)
