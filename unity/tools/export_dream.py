# Export The Dream's data and pre-solved spot library from the web build into JSON for Unity.
#   python3 unity/tools/export_dream.py <dream.html wrapped for phones> <out dir>
# Writes dream_data.json (events, people, story, items...) and spots.json (tables -> situations -> hands,
# with the table view for each situation: seats, positions, stacks, bets, pot, option labels).
import asyncio, json, sys
from playwright.async_api import async_playwright

SRC, OUT = sys.argv[1], sys.argv[2]

# calendar-era tuning that the Unity build adds on top of the web data
FLAGS = {
    'nightly': {'turbo': True}, 'tilt11': {'turbo': True},
    'deep': {'reentry': True}, 'tilt55': {'reentry': True}, 'classic': {'reentry': True}, 'ring400': {'reentry': True},
    'ring600': {'reentry': True}, 'ring1700': {'reentry': True}, 'b1500': {'reentry': True}, 'b3000': {'reentry': True},
    'tilt215': {'reentry': True}, 'b10k': {'reentry': True}, 'rpt3500': {'reentry': True}, 'glo10k': {'reentry': True},
    'tilt530': {'reentry': True}, 'rpt10k': {'reentry': True}, 'private': {'req': 5}, 't25k': {'reentry': True},
    't100k': {'reentry': True}, 'tilt2100': {'reentry': True}, 't250k': {'req': 5}, 'legends': {'req': 7},
}
FREEROLL = {'id': 'freeroll', 'ch': 1, 'kind': 'mtt', 'online': True, 'turbo': True, 'name': 'CF Tilt Comeback Freeroll', 'venue': 'cftilt',
            'buyin': 0, 'free': True, 'field': 4000, 'blurb': 'Free to enter when your roll is running low. Four thousand runners and real money at the top.'}

JS = r"""async () => {
  const D = window.__dream, data = D._data(), lib = D.drLib();
  const strip = (o) => JSON.parse(JSON.stringify(o, (k, v) => (typeof v === 'function' ? '__fn' : v)));
  const out = { data: strip(data), classes: [], payouts: {} };
  for (let c = 0; c < 169; c++) out.classes.push({ name: D._cname(c), combos: [] });
  for (let a = 1; a < 52; a++) for (let b = 0; b < a; b++) out.classes[D._cls(a, b)].combos.push([a, b]);
  // payouts per $1 of buy-in for every field size in use
  const fields = new Set([...data.DR_EVENTS.map((e) => e.field), data.DR_DAILY.field, 4000, 160, ...Object.values(data.DR_SNG).map((x) => x.field)].filter(Boolean));
  for (const f of fields) { const p = D._payouts(f, 1); out.payouts[f] = { paid: p.paid, amounts: p.amounts.map((x) => +x.toFixed(6)) }; }
  // spots: every table with the view of every situation
  const spots = {};
  for (const [gid, tables] of Object.entries(lib)) {
    spots[gid] = [];
    for (const t of tables) {
      D._apply(t.s);
      const tt = t.s.t222 || {}, T = { u: t.u || 'bb', k: t.k || 1, left: tt.left || null, paid: tt.paid || null, avg: tt.avg || null, icm: !!tt.icm, bbAnte: !!tt.bbAnte, n: [] };
      for (const n of t.n) {
        if (n.bad) continue;
        const v = D._view({ hero: n.h, path: n.p.map((k) => ({ k })), hands: [], seats: [], res: { actions: n.a.map((k) => ({ k })), strat: [], ev: [] }, cls: 0 });
        T.n.push({ h: n.h, p: n.p, a: n.a, w: n.w, r: n.r, g: n.g || null,
          v: { seats: v.seats.map((x) => [x.pos, +(+x.stack).toFixed(2), +(+x.put).toFixed(2), x.folded ? 1 : 0, x.acts.join(' · ')]), pot: +(+v.pot).toFixed(2), opts: v.opts.map((o) => o.t), cat: D._trCat(v) } });
      }
      spots[gid].push(T);
    }
  }
  out.spots = spots;
  return out;
}"""

async def main():
    async with async_playwright() as p:
        b = await p.chromium.launch(); pg = await b.new_page()
        errs = []; pg.on('pageerror', lambda e: errs.append(str(e)))
        await pg.goto('file://' + SRC); await pg.wait_for_timeout(1200)
        await pg.click('#goTrainer'); await pg.wait_for_timeout(300)
        r = await pg.evaluate(JS)
        await b.close()
    d = r['data']
    for e in d['DR_EVENTS']:
        e.update(FLAGS.get(e['id'], {}))
    d['DR_EVENTS'].append(FREEROLL)
    data = {'data': d, 'classes': r['classes'], 'payouts': r['payouts']}
    json.dump(data, open(OUT + '/dream_data.json', 'w'), separators=(',', ':'))
    json.dump(r['spots'], open(OUT + '/spots.json', 'w'), separators=(',', ':'))
    print('tables', {g: (len(t), sum(len(x['n']) for x in t)) for g, t in r['spots'].items()}, 'errors', errs[:3])

asyncio.run(main())
