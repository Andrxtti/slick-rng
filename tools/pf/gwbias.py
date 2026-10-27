import json, re, glob, base64, os
import numpy as np
SC='/tmp/claude-0/-home-claude/c819b688-727f-5a5a-aefc-c9c8a5a8f2a2/'
cl=json.load(open(SC+'scratchpad/cls.json')); N=cl['n']; SZ=np.array(cl['s'],float); IDX={n:i for i,n in enumerate(N)}
def fam(n):
    if len(n)==2: return 'pair'
    s=n[2]=='s'; hi,lo='23456789TJQKA'.index(n[0]),'23456789TJQKA'.index(n[1]); g=hi-lo
    if n[0]=='A': return 'Ax'+('s' if s else 'o')
    if hi>=8 and lo>=8: return 'bway'+('s' if s else 'o')
    return ('sc' if g<=1 else 'sg' if g<=3 else 'sx') if s else 'junk_o'
F=np.array([fam(n) for n in N]); fams=sorted(set(F))
def cat(path,seat):
    k=path.split('-'); nr=sum(1 for t in k if t.startswith('R')); nc=sum(1 for t in k if t=='C')
    if nr>=2: return '3bet+'
    if nc>=1: return 'multiway(open+flat)'
    return {'4':'CO/HJ vs open','5':'BTN vs open','6':'SB vs open','7':'BB vs open','1':'EP vs open','2':'EP vs open','3':'MP vs open'}.get(seat,'other')
acc={}
for f in sorted(glob.glob('gw/gw*.txt')):
    for line in open(f).read().strip().split('\n'):
        key,body=line.split('\t'); st,path,seat=key.split('|'); d=round(float(st))
        if d not in (25,100): continue
        j=json.load(open(f'../../app/lib/chart-{d}bb.json')); nodes={n['p']:n for n in j['nodes']}
        keys=['fold' if t=='F' else 'call' if t=='C' else 'raise' for t in path.split('-')]
        n=nodes.get('|'.join(keys))
        if not n: continue
        na=len(n['a']); S=np.frombuffer(base64.b64decode(n['s']),np.uint8).reshape(169,na)/255
        ours=np.zeros((169,3))
        for jx,a in enumerate(n['a']): ours[:,0 if a=='fold' else 1 if a in('call','check') else 2]+=S[:,jx]
        for tk in body.split():
            h,codes=tk.split(':'); o=[0,0,0]
            for m in re.finditer(r'([arcf])(\d+)',codes): o[0 if m[1]=='f' else 1 if m[1]=='c' else 2]+=int(m[2])/100
            if h not in IDX or sum(o)<0.5: continue
            c=IDX[h]
            if F[c]=='junk_o': continue
            k=(d,cat(path,seat),F[c]); a=acc.setdefault(k,[0,0,0,0])
            w=SZ[c]; a[0]+=w; a[1]+=w*(1-np.minimum(ours[c],o).sum()); a[2]+=w*(ours[c][0]-o[0]); a[3]+=w*((ours[c][1]+ours[c][2])-(o[1]+o[2]))
tot=sum(v[0] for v in acc.values()); err=sum(v[1] for v in acc.values())
print('overall disagreement %.1f%% over %d combos'%(100*err/tot,tot))
rows=sorted(acc.items(),key=lambda kv:-kv[1][1])
print('depth category                 family  share-of-all-error  disagree%  ours-fold minus GW-fold (pts)')
for (d,c,f),v in rows[:22]: print(f'{d:>4} {c:<26} {f:<6} {100*v[1]/err:6.1f}%        {100*v[1]/v[0]:5.1f}%     {100*v[2]/v[0]:+6.1f}')
