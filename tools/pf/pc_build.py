#!/usr/bin/env python3
"""Emit the library build as jobs, so the PC worker and the cloud run exactly what build_all.sh would.
   python3 pc_build.py <tag> <targets_dir> <out_jobs.json> [--cloud N]
Every table x 3 passes. Each pass runs to the same iteration target: the iterations of <targets_dir>/<table>.json
(a pass-1 output of the reference build), so the passes are comparable whatever machine runs them. SECS is short and
MAXSECS long: the target, not the clock, ends the solve. Model env is explicit on every job (no defaults).
--cloud N: write the first N jobs (6-max passes 1-2 first) to <out_jobs>.cloud.json, the rest to <out_jobs>."""
import json, os, sys
tag, tdir, out = sys.argv[1:4]; cloud_n = int(sys.argv[sys.argv.index('--cloud') + 1]) if '--cloud' in sys.argv else 0
MODEL = {'AGGRE': '1', 'PLAYREAL': 'none'}
H = os.path.dirname(os.path.abspath(__file__))
tables = sorted(f[:-5] for f in os.listdir(os.path.join(H, 'cfgs_full')) if f.endswith('.json'))
jobs = []
for P in (1, 2, 3):
    for b in tables:
        it = json.load(open(os.path.join(tdir, b + '.json')))['iters']
        jobs.append({'id': f'0{tag}-p{P}-{b}', 'cwd': 'tools/pf', 'cfg': f'cfgs_full/{b}.json', 'secs': 300, 'evs': 60,
                     'env': dict(MODEL, TARGET_ITERS=str(it), MAXSECS='7200'), 'pass': P, 'table': b})
cl = sorted([j for j in jobs if j['table'].startswith('m6') and j['pass'] < 3], key=lambda j: j['id'])[:cloud_n] if cloud_n else []
ids = {j['id'] for j in cl}
json.dump([j for j in jobs if j['id'] not in ids], open(out, 'w'), indent=0)
if cl: json.dump(cl, open(out.replace('.json', '.cloud.json'), 'w'), indent=0)
print(f'{len(jobs)} jobs: {len(jobs) - len(cl)} for the PC -> {out}' + (f', {len(cl)} for the cloud' if cl else ''))
