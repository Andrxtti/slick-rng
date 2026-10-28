#!/usr/bin/env python3
"""First public release (v1): the NLH solver and Learn only. PLO and the Arena trainer stay in the code for later
releases but have no way in: the launch screen goes straight to Cash | MTT, the header and phone tab bar lose the
Trainer button, and the PLO card is gone. Usage: python3 v1.py ../slick-rng.html  (safe to re-run)."""
import sys
for page in sys.argv[1:]:
    s = open(page, encoding='utf-8').read()
    if '/* V1 BEGIN */' not in s:
        css = ('/* V1 BEGIN */\nbody.v1 #modeTrainer, body.v1 .tabbar [data-tab="trainer"], body.v1 #lsBack, body.v1 #goPlo, body.v1 #goTrainer { display: none !important; }\n'
               'body.v1 #lsGame .lhead .wordmark span { color: var(--hero); }\n/* V1 END */\n')
        i = s.index('</style>'); s = s[:i] + css + s[i:]
        a = "function showLaunch(which) {\n"
        assert s.count(a) == 1; s = s.replace(a, a + "  if (window.__v1 && which !== 'game') { which = 'game'; if (S.variant !== 'nlh') setVariant('nlh'); }   // v1: NLH solver only\n")
        a = "showLaunch('home');\n})();"
        assert s.count(a) == 1
        boot = ("// v1: the NLH solver and Learn only (PLO and the Arena trainer come in later releases)\n"
                "window.__v1 = true; document.body.classList.add('v1');\n"
                "$('lsGTitle').innerHTML = 'CONFIRMED <span>FIRE</span> POKER'; { const st = $('lsGame').querySelector('.subtag'); if (st) st.textContent = 'NLH solver · pick your game'; }\n"
                "$('lsGame').querySelector('.lsplit').insertAdjacentHTML('afterend', '<button type=\"button\" class=\"lrnlink lhome-learn\" data-learn=\"start\">New here? How the solver works ›</button>');\n")
        s = s.replace(a, boot + a)
        open(page, 'w', encoding='utf-8').write(s); print('v1 applied to', page)
    else: print('v1 already in', page)
