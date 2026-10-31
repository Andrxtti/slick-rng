#!/usr/bin/env python3
"""Fit per-class playability corrections by pot type to the saved GTO Wizard nodes (local only, gw/).
  re: re-raised pots (3-bet / squeeze / 4-bet flops). Signal: GW raise share - ours, at 100bb nodes facing one raise
      (at 25bb nearly every raise is a jam, where the flop model plays no part).
  hu: heads-up single-raised pots. Signal: GW call share - ours, at nodes with no flat before the actor.
Each iteration solves 100bb + 25bb (calib.sh) with the current corrections and nudges every class toward GW.
Every third node is held out of the fit and only scored, so an improvement has to generalize. Offsuit junk is never
moved (the course folds it). Extra model settings for the solves come from the environment (e.g. PRE=1).
Writes calib/pot_<it>.json each iteration, logs to fit_pot_log.txt, and freezes the best held-out iteration
into model_pot.json when FREEZE=1."""
import json, subprocess, os, re, glob, base64
import numpy as np
os.chdir(os.path.dirname(os.path.abspath(__file__)))
SC = '/tmp/claude-0/-home-claude/c819b688-727f-5a5a-aefc-c9c8a5a8f2a2/scratchpad/'
cl = json.load(open(SC + 'cls.json')); N = cl['n']; SZ = np.array(cl['s'], float); IDX = {n: i for i, n in enumerate(N)}
JUNK = np.array([n[-1] == 'o' and len(n) == 3 and n[0] != 'A' and not (n[0] in 'TJQK' and n[1] in 'TJQK') for n in N])
GW = []   # (depth, path, gw[169,4] fold/call/raise/jam, has[169])
for f in sorted(glob.glob('gw/gw*.txt')):
    for line in open(f).read().strip().split('\n'):
        key, body = line.split('\t'); st, path, seat = key.split('|'); d = round(float(st))
        if d not in (25, 100): continue
        g = np.zeros((169, 4)); has = np.zeros(169, bool)
        for tk in body.split():
            h, codes = tk.split(':')
            if h not in IDX: continue
            i = IDX[h]
            for m in re.finditer(r'([arcf])(\d+)', codes): g[i, 'fcra'.index(m[1])] += int(m[2]) / 100
            has[i] = g[i].sum() > 0.5
        GW.append((d, path, g, has & ~JUNK))
def ours(d, path, tag):
    j = json.load(open(f'calib/{tag}_{d}.json')); nodes = {n['p']: n for n in j['nodes']}
    keys = ['fold' if t == 'F' else 'call' if t == 'C' else 'raise' for t in path.split('-')]; n = nodes.get('|'.join(keys))
    if not n: return None, False
    S = np.frombuffer(base64.b64decode(n['s']), np.uint8).reshape(169, len(n['a'])) / 255
    o = np.zeros((169, 4)); sep = 'allin' in n['a'] and 'raise' in n['a']
    for j2, a in enumerate(n['a']): o[:, 0 if a == 'fold' else 1 if a in ('call', 'check') else 3 if (a == 'allin' and sep) else 2] += S[:, j2]
    return o, sep
def agree(o, g, has, sep):
    if not sep: g = np.c_[g[:, :2], g[:, 2] + g[:, 3], np.zeros(169)]
    return (np.minimum(o, g).sum(1) * SZ)[has].sum() / SZ[has].sum()
def solve(tag, potfile):
    env = dict(os.environ, CAL_SECS=os.environ.get('CAL_SECS', '240'), PLAYPOT=potfile, EXCL='1')
    return subprocess.run(['./calib.sh', tag, 'A=1'], env=env, capture_output=True, text=True).stdout.strip()
log = open('fit_pot_log.txt', 'a')
re_d = np.zeros(169); hu_d = np.zeros(169); g0 = float(os.environ.get('GAIN', 0.3)); lim = float(os.environ.get('LIM', 0.3))
print(f'--- fit start, env extras: { {k: v for k, v in os.environ.items() if k in ("PRE", "PA", "PAIRSPAN", "PSPAN", "GRE", "IOSPAN")} }', file=log, flush=True)
best = None
for it in range(int(os.environ.get('ITERS', 8))):
    pf = f'calib/pot_{it}.json'; json.dump({'re': re_d.tolist(), 'hu': hu_d.tolist()}, open(pf, 'w')); tag = f'pot{it}'
    out = solve(tag, pf)
    sre = np.zeros(169); cre = np.zeros(169); shu = np.zeros(169); chu = np.zeros(169); fit_ag = []; hold_ag = []
    for ni, (d, path, g, has) in enumerate(GW):
        o, sep = ours(d, path, tag)
        if o is None: continue
        (hold_ag if ni % 3 == 0 else fit_ag).append(agree(o, g, has, sep))
        if ni % 3 == 0: continue
        toks = path.split('-'); nR = sum(t.startswith('R') for t in toks)
        if d == 100 and nR == 1:
            dr = (g[:, 2] + g[:, 3]) - (o[:, 2] + o[:, 3]); sre[has] += dr[has]; cre[has] += 1
        if 'C' not in toks and nR == 1:
            dc = g[:, 1] - o[:, 1]; shu[has] += dc[has]; chu[has] += 1
    ho = float(np.mean(hold_ag)); fa = float(np.mean(fit_ag))
    print(f'iter {it}: {out} | fit nodes {100*fa:.1f}% | HELD-OUT nodes {100*ho:.1f}% | |re| {np.abs(re_d).mean():.3f} |hu| {np.abs(hu_d).mean():.3f}', file=log, flush=True)
    if best is None or ho > best[0]: best = (ho, it)
    gain = g0 * 0.85 ** it
    re_d = np.clip(re_d + gain * np.where(cre > 0, sre / np.maximum(cre, 1), 0) * np.minimum(cre, 4) / 4, -lim, lim)
    hu_d = np.clip(hu_d + gain * np.where(chu > 0, shu / np.maximum(chu, 1), 0) * np.minimum(chu, 4) / 4, -lim, lim)
print(f'best held-out iteration {best[1]} ({100*best[0]:.1f}%)', file=log, flush=True)
if os.environ.get('FREEZE'):
    json.dump(json.load(open(f'calib/pot_{best[1]}.json')), open('model_pot.json', 'w'))
    print(f'froze model_pot.json from iteration {best[1]}', file=log, flush=True)
print('DONE', file=log, flush=True)
