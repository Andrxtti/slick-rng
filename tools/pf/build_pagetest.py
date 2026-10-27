#!/usr/bin/env python3
"""Load the real Solver page over http for every (format, depth) in the library: it must reach the pre-solved library
(not the live solver), render the first-in chart, step one action, and raise no page errors. Exit 1 on any failure.
LIBDIR=<dir with m8-d100.json ...> to test another folder (default app/lib)."""
import asyncio, os, subprocess, sys, tempfile, time, shutil
from playwright.async_api import async_playwright
APP = os.path.abspath('../../app'); LIBDIR = os.path.abspath(os.environ.get('LIBDIR', APP + '/lib'))
tmp = tempfile.mkdtemp(); os.symlink(APP + '/slick-rng.html', tmp + '/slick-rng.html'); os.symlink(LIBDIR, tmp + '/lib')
srv = subprocess.Popen(['python3', '-m', 'http.server', '8124', '--directory', tmp], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL); time.sleep(1)
combos = sorted({(f[1], int(f.split('-d')[1][:-5])) for f in os.listdir(LIBDIR) if f.startswith('m') and f.endswith('.json')}, key=lambda x: (x[0], x[1]))
fails = []
async def one(b, fmt, d):
    pg = await b.new_page(viewport={'width': 1300, 'height': 1000}); errs = []
    pg.on('pageerror', lambda e: errs.append(str(e)))
    await pg.goto('http://localhost:8124/slick-rng.html'); await pg.wait_for_timeout(1200)
    await pg.click('#goSolver'); await pg.click('#goNlh'); await pg.click('#goMtt'); await pg.wait_for_timeout(500)
    await pg.select_option('#spFormat', fmt); await pg.wait_for_timeout(300)
    await pg.select_option('#spStack', str(d)); await pg.wait_for_timeout(2500)
    want = f'pre-solved · {d} bb · {fmt}-max'
    txt = lambda: pg.evaluate("document.body.innerText.replace(/\\s+/g,' ')")
    t = await txt(); ok = want in t
    cells = await pg.evaluate("document.querySelectorAll('.cell, .hcell, [data-hand], .mx td').length")
    if ok:
        await pg.click('button:has-text(\"Fold\")'); await pg.wait_for_timeout(800); t2 = await txt(); ok = want in t2
    await pg.close()
    if not ok or errs: fails.append(f'{fmt}-max {d}bb: ' + ('not pre-solved' if not ok else '') + (' errors: ' + '; '.join(errs[:2]) if errs else ''))
    print(f'{fmt}-max {d:>3}bb', 'OK' if ok and not errs else 'FAIL', 'cells', cells)
async def main():
    async with async_playwright() as p:
        b = await p.chromium.launch()
        for fmt, d in combos: await one(b, fmt, d)
        await b.close()
asyncio.run(main()); srv.terminate(); shutil.rmtree(tmp, ignore_errors=True)
print('page test:', 'PASS' if not fails else 'FAIL ' + ' | '.join(fails)); sys.exit(1 if fails else 0)
