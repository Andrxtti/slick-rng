// GTO Wizard page extractor (runs in the browser pane). Returns one tab-separated line in the gw/*.txt format:
//   depth|preflop_actions|seat<TAB>hand:a12r34c56f0 ...   plus a second field with EVs: hand=ev ...
// Actions come from the action buttons (.sab_btn: name + color); each cell's strategy from its gradient bands.
(async () => {
  for (let i = 0; i < 60; i++) { if (document.querySelectorAll('.sab_btn').length && [...document.querySelectorAll('.ra_table_cell')].some((c) => c.style.backgroundImage)) break; await new Promise((r) => setTimeout(r, 500)); }
  const url = new URL(location.href), q = url.searchParams;
  const depth = q.get('depth'), path = q.get('preflop_actions') || '', spot = q.get('history_spot');
  const btns = [...document.querySelectorAll('.sab_btn')].map((b) => {
    const back = b.querySelector('.sab_btn_back'), name = (b.querySelector('.sab_btn_name_bet_big') || {}).textContent || '';
    return { name: name.trim(), color: back && back.style.background.replace(/\s+/g, ''), fre: (b.querySelector('.sab_btn_fre') || {}).textContent };
  });
  const code = (nm) => /^Allin/i.test(nm) ? 'a' : /^Raise/i.test(nm) ? 'r' : /^Call/i.test(nm) ? 'c' : /^Check/i.test(nm) ? 'c' : /^Fold/i.test(nm) ? 'f' : '?';
  const byColor = {}; for (const b of btns) byColor[b.color] = code(b.name);
  const out = [], evs = [], seen = new Set();
  for (const cell of document.querySelectorAll('.ra_table_cell')) {
    const hand = (cell.id || '').split('_')[1]; if (!hand || seen.has(hand)) continue; seen.add(hand);
    const bg = cell.style.backgroundImage || '', sz = cell.style.backgroundSize || '';
    const cols = [...bg.matchAll(/rgb\([^)]*\)/g)].map((m) => m[0].replace(/\s+/g, '')).filter((_, i) => i % 2 === 0);
    const bands = [...sz.matchAll(/([\d.]+)%\s+([\d.]+)%/g)], sizes = bands.map((m) => +m[1]), wt = bands.length ? Math.max(...bands.map((m) => +m[2])) : 100;
    const tot = {}; let prev = 0;
    // bands are cumulative widths, painted in order; the last (100%) band is the base
    const pairs = cols.map((c, i) => [c, sizes[i]]).sort((x, y) => x[1] - y[1]);
    for (const [c, s] of pairs) { const k = byColor[c] || '?'; tot[k] = (tot[k] || 0) + (s - prev); prev = s; }
    if (!cols.length) { const bc = getComputedStyle(cell).backgroundColor.replace(/\s+/g, ''); if (byColor[bc]) tot[byColor[bc]] = 100; else if (cell.classList.contains('rtc_folded')) tot.f = 100; else continue; }
    const str = Object.entries(tot).filter(([k, v]) => v > 0.05).map(([k, v]) => k + Math.round(v)).join('');
    out.push(hand + ':' + str + (wt < 99.5 ? '@' + Math.round(wt) : ''));
    const v = cell.querySelector('.rtc_value'); if (v) evs.push(hand + '=' + v.textContent.trim());
  }
  return { key: `${depth}|${path}|${spot}`, actions: btns.map((b) => b.name + ' ' + b.fre).join(', '), line: `${depth}|${path}|${spot}\t${out.join(' ')}`, ev: evs.join(' '), n: out.length };
})()
