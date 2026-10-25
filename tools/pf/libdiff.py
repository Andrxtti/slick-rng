# Library diff: match spots by (hero seat, action path) and hands by class, never by list position. Reports the dealt
# hands two solves share and how many of them change their best action (and how many of those by at least the gate gap).
#   python3 libdiff.py <a.json> <b.json> [label]
import json, sys, subprocess, collections
names = json.loads(subprocess.check_output(['node', '-e', "process.stdout.write(JSON.stringify(require('/home/claude/slick-rng/app/engine.js').CLASS_NAMES))"]))
def index(path):
    d = json.load(open(path)); out = {}
    for n in d['spots']:
        key = (n['h'], tuple(n['p']))
        out[key] = {'a': n['a'], 'cat': n['v']['cat'], 'hands': {r[0]: (n['a'][r[2].index(max(r[2]))], r) for r in n['r']}}
    return d, out
def compare(pa, pb, label):
    da, A = index(pa); db, B = index(pb)
    gap = 0.02 if da.get('unit') == 'icm' else 0.12
    spots_common = [k for k in A if k in B and A[k]['a'] == B[k]['a']]
    hands_a = sum(len(v['hands']) for v in A.values()); hands_b = sum(len(v['hands']) for v in B.values())
    common = flips = clear = 0; trans = collections.Counter(); byhand = collections.Counter(); ex = []
    for k in spots_common:
        ha, hb = A[k]['hands'], B[k]['hands']
        for c in ha:
            if c not in hb: continue
            common += 1
            if ha[c][0] != hb[c][0]:
                flips += 1; r = ha[c][1]; i1 = A[k]['a'].index(ha[c][0]); i2 = A[k]['a'].index(hb[c][0])
                if r[2][i1] - r[2][i2] >= gap: clear += 1
                trans[(A[k]['cat'], hb[c][0], ha[c][0])] += 1; byhand[names[c]] += 1
                if len(ex) < 6: ex.append(f"{A[k]['cat']} hero seat {k[0]} {names[c]}: {hb[c][0]} -> {ha[c][0]} (evs {[round(x,2) for x in r[2]]})")
    print(f"{label}: spots {len(A)} vs {len(B)} ({len(spots_common)} matched); dealt hands {hands_a} vs {hands_b}, {common} in both;")
    print(f"   same hand, different best action: {flips} ({100*flips/max(common,1):.1f}%), with a gate-size margin: {clear}")
    for (cat, o, n), v in trans.most_common(6): print(f"     {v:4d} {cat:22s} {o:>6s} -> {n}")
    for e in ex: print('     e.g.', e)
if __name__ == '__main__':
    compare(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else 'diff')
