# Table setups (stacks, blinds, antes, ICM payouts) for every library table, from the solved library's snapshots
import asyncio, json, glob, os, sys
from playwright.async_api import async_playwright
SP = '/tmp/claude-0/-home-claude/c819b688-727f-5a5a-aefc-c9c8a5a8f2a2/scratchpad'
OUT = os.path.join(os.path.dirname(__file__), 'cfgs')
async def main():
    async with async_playwright() as p:
        b = await p.chromium.launch(); pg = await b.new_page(); errs = []
        pg.on('pageerror', lambda e: errs.append(str(e)))
        await pg.add_init_script("window.__pfHold = true;")
        await pg.goto('file://' + SP + '/dream.html'); await pg.wait_for_timeout(1500)
        await pg.click('#goTrainer'); await pg.wait_for_timeout(300)
        n = 0
        for f in sorted(glob.glob(SP + '/lib4/*.json')):
            d = json.load(open(f))
            if d.get('skip') or not d.get('snap'): continue
            cfg = await pg.evaluate("(s) => { __dream._apply(s); const c = __dream._cfg(); return JSON.parse(JSON.stringify(c)); }", d['snap'])
            name = os.path.basename(f)
            json.dump({'gid': d['gid'], 'unit': d['unit'], 'snap': d['snap'], 'cfg': cfg}, open(os.path.join(OUT, name), 'w'))
            n += 1
        print('tables', n, 'errors', errs[:3]); await b.close()
asyncio.run(main())
