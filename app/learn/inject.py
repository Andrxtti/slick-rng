#!/usr/bin/env python3
"""Put the Learn view (learn.html / learn.css / learn.js next to this file) into a solver page, or refresh it.
Usage: python3 inject.py ../slick-rng.html [more pages]. Safe to re-run: everything sits between markers."""
import os, re, sys
here = os.path.dirname(os.path.abspath(__file__))
rd = lambda f: open(os.path.join(here, f), encoding='utf-8').read()
html, css, js = rd('learn.html'), rd('learn.css'), rd('learn.js')
BOOK = ('<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 5.5c2.5-1.2 5-1.2 7 .5v13c-2-1.7-4.5-1.7-7-.5z"/>'
        '<path d="M19 5.5c-2.5-1.2-5-1.2-7 .5v13c2-1.7 4.5-1.7 7-.5z"/></svg>')

def put(s, begin, end, body, anchor, before=True):
    """replace the block between markers, or insert it next to the anchor (first occurrence)"""
    block = begin + body + end
    if begin in s:
        return re.sub(re.escape(begin) + '.*?' + re.escape(end), lambda m: block, s, count=1, flags=re.S)
    i = s.index(anchor)
    return s[:i] + block + s[i:] if before else s[:i + len(anchor)] + block + s[i + len(anchor):]

def once(s, old, new):
    if new in s: return s
    assert s.count(old) == 1, ('anchor not unique', old[:70], s.count(old)); return s.replace(old, new)

for page in sys.argv[1:]:
    s = open(page, encoding='utf-8').read()
    s = put(s, '/* LEARN CSS BEGIN */\n', '/* LEARN CSS END */\n', css + '.lhome-learn { justify-self: center; margin: 2px 0 6px; font-size: 13px; }\n', '</style>')
    s = put(s, '<!-- LEARN BEGIN -->\n', '<!-- LEARN END -->\n', html + '<script>\n' + js + '</script>\n', '<div class="launch" id="launch">')
    s = once(s, '<button id="modeTrainer" aria-pressed="false">Trainer</button></div>',
             '<button id="modeTrainer" aria-pressed="false">Trainer</button><button id="modeLearn" aria-pressed="false" data-learn="start">Learn</button></div>')
    m = re.search(r'(<button type="button" data-tab="trainer">.*?</button>)', s)
    s = once(s, m[1], m[1] + f'\n  <button type="button" data-learn="start">{BOOK}Learn</button>')
    m = re.search(r'(<div class="lscreen" id="lsHome">.*?<div class="lsplit">.*?</div>)', s, flags=re.S)
    s = once(s, m[1], m[1] + '\n    <button type="button" class="lrnlink lhome-learn" data-learn="start">New here? How the solver works ›</button>')
    # the spot's own "How it's built" link next to the solver badge, and the section it opens
    s = once(s, '<span class="pfnote ${r.solved ? \'ok\' : \'\'}">${pfNote(r.solved)}</span>',
             '<span class="pfnote ${r.solved ? \'ok\' : \'\'}">${pfNote(r.solved)}</span><button type="button" class="lrnlink" data-learn="${r.learn || \'solver\'}">How it\'s built</button>')
    s = once(s, "    return { kind: 'action', preflop: true, name: seat.pos, sub,",
             "    const mwSpot = T.seats.filter((x) => x !== seat && !x.folded && x.acts.some((a) => a.k !== 'fold' && a.k !== 'check')).length >= 2;\n"
             "    return { kind: 'action', preflop: true, learn: cfgNow.payoff.type === 'icm' ? 'icm' : mwSpot ? 'multiway' : 'solver', name: seat.pos, sub,")
    open(page, 'w', encoding='utf-8').write(s); print('learn view in', page)
