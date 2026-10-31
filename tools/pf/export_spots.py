# Build Unity's spots.json from the clean-tree solves in tools/pf/out (labels come from the solved tree itself)
import json, glob, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '../../unity/Assets/Dream/Resources/Dream/spots.json')
lib = {}; stats = {}; dropped = {'spots': 0}
NAMES = json.loads(os.popen("node -e \"process.stdout.write(JSON.stringify(require('" + HERE + "/../../app/engine.js').CLASS_NAMES))\"").read())
LIBDIR = os.environ.get('LIBDIR', 'out')   # LIBDIR=out_final exports the consensus library
MWFLOP = json.loads(os.popen('LIBDIR=' + LIBDIR + ' node ' + HERE + '/mwflop.js').read())
# hand classes ranked by all-in equity against a random hand (0 = AA); 60 and beyond is junk
RANK = json.loads(os.popen("node -e \"const E=require('" + HERE + "/../../app/engine.js');const S=new Array(169).fill(0);for(let i=0;i<E.NUM_COMBOS;i++)S[E.CLASS_OF[i]]++;const q=[];for(let c=0;c<169;c++){let s=0,w=0;for(let o=0;o<169;o++){s+=E.EQ169[c*169+o]*S[o];w+=S[o]}q.push(s/w)}const idx=[...Array(169).keys()].sort((a,b)=>q[b]-q[a]);const r=new Array(169);idx.forEach((c,i)=>r[c]=i);process.stdout.write(JSON.stringify(r))\"").read())
VALUE = {NAMES.index(h) for h in ('AA', 'KK', 'QQ', 'JJ', 'TT', 'AKs', 'AKo', 'AQs', 'AQo')}
def kind(lbl):
    l = lbl.lower()
    return 'fold' if l == 'fold' else 'check' if l == 'check' else 'call' if (l.startswith('call') or l == 'complete') else 'allin' if l.startswith('jam') else 'raise'
for f in sorted(glob.glob(os.path.join(HERE, LIBDIR, '*.json'))):
    if f.endswith('probe.json') or os.path.basename(f).startswith('ref'): continue
    d = json.load(open(f)); spots = d.get('spots') or []
    if 'gid' not in d: continue   # fit/probe outputs (mw100_*.json and the like) share the folder
    if len(spots) < (2 if d['gid'] == 'sat' else 4): continue
    t = d['snap'].get('t222') or {}
    T = {'u': d['unit'], 'k': 1, 'left': t.get('left'), 'paid': t.get('paid'), 'avg': t.get('avg'), 'icm': d['unit'] == 'icm', 'bbAnte': bool(t.get('bbAnte')), 'n': []}
    if d['unit'] == 'icm':
        st = [float(v) for v in (t.get('stacks') or {}).values()] or [25]
        avg = float(t.get('avg') or (sum(st) / len(st))); left = float(t.get('left') or len(st))
        T['k'] = round(left * avg / 100, 3)
    for n in spots:
        # short-stack flats from a seat that doesn't close the action: the course teaches shove-or-fold at 12bb and
        # under, and that is where our flop model is weakest, so those hands are never quizzed
        hero = n['v']['seats'][n['h']]
        eff = min(hero[1], max([x[1] for i, x in enumerate(n['v']['seats']) if i != n['h'] and not x[3]] or [hero[1]]))
        # multiway spots that reach a flop (two or more opponents in, and some line below the hero's decision sees a
        # flop) depend on the multiway postflop model (fitted to GTO Wizard chip EV at 100bb/25bb and to the course's
        # multiway charts, see MULTIWAY_NOTES.md). Two hand families there follow the course over the model and are
        # never quizzed: offsuit junk flats (the course folds them in MTTs; our blind realization is too kind to them
        # three-way) and TT+/AQ+ flats at 15bb or less with a jam on the menu (short-stack play jams them).
        if os.path.basename(f) + '|' + ','.join(n['p']) in MWFLOP and 'call' in n['a']:
            ci, jam = n['a'].index('call'), 'allin' in n['a']
            keep = [r for r in n['r'] if not (r[2].index(max(r[2])) == ci and ((NAMES[r[0]].endswith('o') and RANK[r[0]] >= 60) or (r[0] in VALUE and jam and eff <= 15 + 1e-9)))]
            dropped['multiway junk flat / short strong flat'] = dropped.get('multiway junk flat / short strong flat', 0) + len(n['r']) - len(keep); n['r'] = keep
        def drop(kind, why):
            if kind not in n['a']: return
            ki = n['a'].index(kind); keep = [r for r in n['r'] if r[2].index(max(r[2])) != ki]
            dropped[why] = dropped.get(why, 0) + len(n['r']) - len(keep); n['r'] = keep
        if n['v']['cat'] == 'Facing a raise':
            # short flats from a seat that doesn't close the action (the course: shove-or-fold, don't flat pairs with
            # the blinds behind), and SB flats up to 30bb: disputed lines where our flop model is weakest
            if hero[0] != 'BB' and eff <= 16 + 1e-9: drop('call', 'short flat')
            if hero[0] == 'SB' and eff <= 30 + 1e-9: drop('call', 'SB flat')
            # blind re-jams above 25bb when a small 3-bet is on the menu: our model leans to the jam there
            if hero[0] in ('SB', 'BB') and eff > 25 and 'raise' in n['a']: drop('allin', 'blind jam over 25bb')
        if n['v']['cat'] in ('Facing a raise', 'Facing a 3-bet or more') and 'raise' in n['a'] and eff <= 35 + 1e-9:
            # big pairs and big aces with a small re-raise on the menu at 35bb or less: the small raise (inducing, getting
            # stacks in later) is a real option that our flop model undervalues, so "jam" is never taught for them
            ai = n['a'].index('allin') if 'allin' in n['a'] else -1
            if ai >= 0:
                keep = [r for r in n['r'] if not (r[0] in VALUE and r[2].index(max(r[2])) == ai)]
                dropped['value jam vs small raise'] = dropped.get('value jam vs small raise', 0) + len(n['r']) - len(keep); n['r'] = keep
        if n['v']['cat'] == 'Facing a 3-bet or more' and eff <= 35 + 1e-9:
            # flatting a 3-bet with 35bb or less (stack-to-pot under ~2): the course plays these jam-or-fold, and our
            # flop model (no stack-off) flatters calling there
            drop('call', 'short 3-bet flat')
        if len(n['r']) < (8 if not n['p'] or all(k == 'fold' for k in n['p']) else 5): dropped['spots'] += 1; continue
        assert len(n['a']) == len(n['v']['opts']) and all(kind(o) == k for o, k in zip(n['v']['opts'], n['a'])), (f, n['a'], n['v']['opts'])
        for r in n['r']: assert len(r[1]) == len(n['a']) and len(r[2]) == len(n['a'])
        T['n'].append({k: n[k] for k in ('h', 'p', 'a', 'w', 'r', 'g', 'v')})
    lib.setdefault(d['gid'], []).append(T)
    stats[d['gid']] = stats.get(d['gid'], 0) + len(T['n'])
# cash games are not in the story; if anything ever deals one, it plays the chip-EV ring tables
if 'cash' not in lib and 'ring' in lib: lib['cash'] = lib['ring']
for g in ('ring', 'bubble', 'shove', 'sat'): assert lib.get(g), 'no tables for ' + g
json.dump(lib, open(OUT, 'w'), separators=(',', ':'))
print('dropped', dropped, 'tables', {g: len(v) for g, v in lib.items()}, 'spots', stats, 'size', os.path.getsize(OUT) // 1024, 'KB ->', OUT)
