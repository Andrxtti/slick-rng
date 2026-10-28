#!/usr/bin/env python3
"""GTO Wizard's realized pot share for the CALLER at 100bb re-raised nodes, from the per-hand EVs (gw/ev100.txt):
   pure calls: share = (EV + cost) / pot;  call/fold mixes: share = cost / pot (indifference).
   Prints per matchup; also compares with our real postflop solves when an out dir is given.
   python3 realize/gwshare.py [outDir]"""
import re, sys, json, os
H = os.path.dirname(os.path.abspath(__file__)); PF = os.path.dirname(H)
EV = {}; ST = {}
for l in open(PF + '/gw/ev100.txt'):
    k, b = l.rstrip('\n').split('\t'); EV[k.split('|')[1]] = {t.split('=')[0]: float(t.split('=')[1]) for t in b.split()}
for l in open(PF + '/gw/gw100.txt'):
    k, b = l.rstrip('\n').split('\t')[:2]; d = {}
    for t in b.split():
        h, c = t.split(':'); c = c.split('@')[0]; d[h] = {m[0]: int(m[1]) / 100 for m in re.findall(r'([arcf])(\d+)', c)}
    ST[k.split('|')[1]] = d
# node: (matchup name, caller name, cost to call, pot after the call)
NODES = {'R2.1-F-F-F-F-F-F-R11.02': ('bb3b_utg', 'UTG', 8.92, 23.54), 'F-F-F-F-F-R2.5-F-R13.12': ('bb3b_btn', 'BTN', 10.62, 27.74),
         'F-F-F-F-R2.2-R8.8-F-F': ('co_btn3b', 'CO', 6.6, 20.1), 'R2.1-F-F-F-F-R8.4-F-F': ('utg_btn3b', 'UTG', 6.3, 19.3),
         'F-F-F-F-R2.2-F-F-R11-R24.2': ('bb_co4b', 'BB', 13.2, 49.9),
         'F-F-F-F-R2.2-F-F': ('bb_co', 'BB', 1.2, 5.9), 'F-F-F-F-F-R2.5-F': ('bb_btn', 'BB', 1.5, 6.5), 'R2.1-F-F-F-F-F-F': ('bb_utg', 'BB', 1.1, 5.7)}
out = {}
for node, (mk, who, cost, pot) in NODES.items():
    sh = {}
    for h, ev in EV[node].items():
        m = ST[node].get(h, {}); c, f, r = m.get('c', 0), m.get('f', 0), m.get('r', 0) + m.get('a', 0)
        if r > 0.05: continue
        if c >= 0.95: sh[h] = round((ev + cost) / pot, 3)
        elif c >= 0.05 and f >= 0.05: sh[h] = round(cost / pot, 3)
    folds = [h for h, m in ST[node].items() if m.get('f', 0) >= 0.95 and h in EV[node]]   # hands that reach the node and fold: call share < cost/pot
    out[mk] = {'caller': who, 'cost': cost, 'pot': pot, 'share': sh, 'folds': folds}
    print(mk, who, f'cost {cost} pot {pot} indifference share {cost/pot:.3f}:', ' '.join(f'{h}={v}' for h, v in sorted(sh.items(), key=lambda x: -x[1])))
json.dump(out, open(H + '/gw_caller_share.json', 'w'))
if len(sys.argv) > 1:
    d = sys.argv[1]; print('\n== ours (real solves) vs GW, caller side ==')
    for mk, o in out.items():
        files = [f for f in os.listdir(d) if f.startswith('a-real-' + mk + '-')]
        if not files: continue
        s = {}; n = {}
        for f in files:
            r = json.load(open(os.path.join(d, f)))
            for fl in r['flops'].values():
                for h, x in fl['classes'][o['caller']].items(): s[h] = s.get(h, 0) + x['ev'] * x['n']; n[h] = n.get(h, 0) + x['n']
        rows = [(h, o['share'][h], s[h] / n[h] / o['pot']) for h in o['share'] if h in n and n[h] > 500]
        rows.sort(key=lambda x: -x[1]); diff = sum(x[2] - x[1] for x in rows) / max(1, len(rows))
        print(mk, f'{len(files)} flops, mean(ours - GW) = {diff:+.3f}:', ' '.join(f'{h} GW {g:.2f} ours {u:.2f}' for h, g, u in rows))
