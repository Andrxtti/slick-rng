#!/usr/bin/env python3
"""Automatic pass/fail checks for the library build; writes build_report.md. Exit 1 on any hard failure."""
import json, glob, os, subprocess, sys, re
os.chdir(os.path.dirname(os.path.abspath(__file__)))
fails = []; lines = ['# Library build report', '']
want = [f'm{f}-d{d}.json' for f in (8, 6) for d in (10, 15, 20, 25, 30, 40, 50, 60, 80, 100)]
have = {os.path.basename(p) for p in glob.glob('../../app/lib/*.json')}
miss = [w for w in want if w not in have]
if miss: fails.append(f'missing tables: {miss}')
tot = 0; lines += ['| table | decision points | flagged hands (weighted) | size MB |', '|---|---|---|---|']
for w in want:
    p = f'../../app/lib/{w}'
    if not os.path.exists(p): continue
    j = json.load(open(p)); ns = j['nodes']; mb = os.path.getsize(p) / 1e6; tot += mb
    wf = sum(n['prob'] * n['nx'] for n in ns) / max(1e-9, sum(n['prob'] * 169 for n in ns))
    lines.append(f'| {w[:-5]} | {len(ns)} | {100*wf:.2f}% | {mb:.2f} |')
    if wf > 0.03: fails.append(f'{w}: {100*wf:.1f}% of weighted hands flagged (limit 3%)')
    if len(ns) < 100: fails.append(f'{w}: only {len(ns)} decision points')
lines += ['', f'Total library size: {tot:.1f} MB']
if tot > 90: fails.append(f'library too large: {tot:.0f} MB')
r = subprocess.run(['python3', 'gwlib.py'], capture_output=True, text=True, env=dict(os.environ, EXCL='1')).stdout
m = re.search(r'mean hand agreement with GTO Wizard: ([\d.]+)% over (\d+) nodes', r)
if m: lines += ['', f'Agreement with the saved GTO Wizard nodes (offsuit junk excluded): {m[1]}% over {m[2]} nodes']; 
if m and float(m[1]) < 78: fails.append(f'GW agreement {m[1]}% below the 78% floor')
if os.path.exists('build_pagetest.py'):
    t = subprocess.run(['python3', 'build_pagetest.py'], capture_output=True, text=True); lines += ['', '## Page test', '```', t.stdout.strip()[-1500:], '```']
    if t.returncode != 0: fails.append('page test failed')
lines += ['', '## Result', 'FAIL: ' + '; '.join(fails) if fails else 'PASS']
open('build_report.md', 'w').write('\n'.join(lines) + '\n'); print('\n'.join(lines[-6:])); sys.exit(1 if fails else 0)
