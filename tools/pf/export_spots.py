# Build Unity's spots.json from the clean-tree solves in tools/pf/out (labels come from the solved tree itself)
import json, glob, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '../../unity/Assets/Dream/Resources/Dream/spots.json')
lib = {}; stats = {}
def kind(lbl):
    l = lbl.lower()
    return 'fold' if l == 'fold' else 'check' if l == 'check' else 'call' if (l.startswith('call') or l == 'complete') else 'allin' if l.startswith('jam') else 'raise'
for f in sorted(glob.glob(os.path.join(HERE, 'out', '*.json'))):
    if f.endswith('probe.json'): continue
    d = json.load(open(f)); spots = d.get('spots') or []
    if len(spots) < 4: continue
    t = d['snap'].get('t222') or {}
    T = {'u': d['unit'], 'k': 1, 'left': t.get('left'), 'paid': t.get('paid'), 'avg': t.get('avg'), 'icm': d['unit'] == 'icm', 'bbAnte': bool(t.get('bbAnte')), 'n': []}
    if d['unit'] == 'icm':
        st = [float(v) for v in (t.get('stacks') or {}).values()] or [25]
        avg = float(t.get('avg') or (sum(st) / len(st))); left = float(t.get('left') or len(st))
        T['k'] = round(left * avg / 100, 3)
    for n in spots:
        assert len(n['a']) == len(n['v']['opts']) and all(kind(o) == k for o, k in zip(n['v']['opts'], n['a'])), (f, n['a'], n['v']['opts'])
        for r in n['r']: assert len(r[1]) == len(n['a']) and len(r[2]) == len(n['a'])
        T['n'].append({k: n[k] for k in ('h', 'p', 'a', 'w', 'r', 'g', 'v')})
    lib.setdefault(d['gid'], []).append(T)
    stats[d['gid']] = stats.get(d['gid'], 0) + len(spots)
json.dump(lib, open(OUT, 'w'), separators=(',', ':'))
print('tables', {g: len(v) for g, v in lib.items()}, 'spots', stats, 'size', os.path.getsize(OUT) // 1024, 'KB ->', OUT)
