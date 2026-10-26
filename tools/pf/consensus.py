# Consensus library: merge independent solves of the same tables so a hand is only dealt when the solves agree on it.
#   python3 consensus.py <outdir> <pass1dir> <pass2dir> [<pass3dir> ...]
# For every table (json present in the first pass dir): spots are the union over passes of the dealt spots, keyed by
# (hero seat, action path, action list). A dealt hand survives when no pass that has a verdict on that node disagrees
# with its best action and at least one OTHER pass agrees. A pass's verdict on a node is its 'verdicts' entry (every
# candidate node, written by solve_lib.js) or, for older outputs, the dealt spot's own 'g' string; '.' (that pass saw a
# mix or an inconsistency) counts as a disagreement. Spots left with fewer than 5 hands, or that became one-answer
# spots, are dropped. Prints one line per table.
import json, os, sys, glob

MIN_ROWS = 5


def load(path):
    d = json.load(open(path))
    V = {}
    for h, p, a, g in d.get('verdicts', []): V[(h, tuple(p), tuple(a))] = g
    for sp in d['spots']:
        k = (sp['h'], tuple(sp['p']), tuple(sp['a']))
        V.setdefault(k, sp['g'])
    return d, V


def merge(table, passes, outdir):
    docs = [load(p) for p in passes]
    spots = {}  # key -> {'meta': spot dict (first seen), 'rows': {cls: (pass idx, row)}}
    for i, (d, V) in enumerate(docs):
        for sp in d['spots']:
            k = (sp['h'], tuple(sp['p']), tuple(sp['a']))
            e = spots.setdefault(k, {'meta': sp, 'rows': {}})
            for r in sp['r']: e['rows'][r[0]] = (i, r)   # later passes win when several dealt the same hand
    gid = docs[0][0].get('gid')
    out, stats = [], {'spots_in': len(spots), 'rows_in': 0, 'rows_out': 0, 'disagree': 0, 'unverified': 0, 'spots_thin': 0, 'spots_flat': 0}
    for k, e in spots.items():
        keep = []
        for c, (i, r) in e['rows'].items():
            stats['rows_in'] += 1
            best = r[1].index(max(r[1]))
            agree = disagree = 0
            for j, (d, V) in enumerate(docs):
                g = V.get(k)
                if g is None: continue
                if g[c] == str(best): agree += 1 if j != i else 0
                else: disagree += 1
            if disagree: stats['disagree'] += 1; continue
            if agree < 1: stats['unverified'] += 1; continue
            keep.append(r)
        if len(keep) < MIN_ROWS: stats['spots_thin'] += 1; continue
        tops = [r[1].index(max(r[1])) for r in keep if max(r[1]) >= 0.95]
        if gid != 'sat' and tops and max(tops.count(x) for x in set(tops)) > 0.85 * len(keep): stats['spots_flat'] += 1; continue
        keep.sort(key=lambda r: max(r[2]) - sorted(r[2])[-2] if len(r[2]) > 1 else 0)
        if len(keep) > 30: keep = [keep[round(i * (len(keep) - 1) / 29)] for i in range(30)]
        stats['rows_out'] += len(keep)
        sp = dict(e['meta']); sp['r'] = keep; out.append(sp)
    res = dict(docs[-1][0]); res.pop('verdicts', None); res['spots'] = out
    res['consensus'] = {'passes': [os.path.basename(os.path.dirname(p)) or p for p in passes], **stats, 'spots_out': len(out)}
    json.dump(res, open(os.path.join(outdir, table), 'w'))
    s = stats
    print(f"{table:18s} spots {s['spots_in']:3d} -> {len(out):3d}  hands {s['rows_in']:4d} -> {s['rows_out']:4d}  (dropped: {s['disagree']} disagreed, {s['unverified']} unverified, {s['spots_thin']} thin spots, {s['spots_flat']} one-answer spots)")
    return stats


if __name__ == '__main__':
    outdir, dirs = sys.argv[1], sys.argv[2:]
    os.makedirs(outdir, exist_ok=True)
    tot = {}
    for f in sorted(glob.glob(os.path.join(dirs[0], '*.json'))):
        table = os.path.basename(f)
        paths = [os.path.join(d, table) for d in dirs if os.path.exists(os.path.join(d, table))]
        if len(paths) < 2: print(f'{table:18s} skipped: only {len(paths)} pass'); continue
        for k, v in merge(table, paths, outdir).items(): tot[k] = tot.get(k, 0) + v
    print('total', tot)
