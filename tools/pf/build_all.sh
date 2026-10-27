#!/bin/bash
# One complete library build, resumable. Stages: freeze model -> 3 solve passes -> consensus -> gates -> report.
# Each launch runs up to DEADLINE_MIN minutes (background jobs are capped at 2h) and exits; relaunch until it prints
# "BUILD COMPLETE". Re-running after a model change = delete bfull_p*, app/lib and model_mw.json, run again.
cd "$(dirname "$0")"
DEADLINE_MIN=${DEADLINE_MIN:-105}; T0=$(date +%s); ST=build_state.txt; touch $ST
log() { echo "$(date '+%m-%d %H:%M') $*" | tee -a $ST; }
left() { echo $(( DEADLINE_MIN*60 - ($(date +%s) - T0) )); }
# ---- stage 0: wait for the multiway fit, freeze the best correction
if [ ! -s model_mw.json ]; then
  while ! grep -q DONE fit_mw_log.txt 2>/dev/null; do sleep 30; [ $(left) -lt 60 ] && { log "waiting for fit"; exit 0; }; done
  python3 - <<'P'
import re, json
best = None
for l in open('fit_mw_log.txt'):
    m = re.match(r'iter (\d+):.*both ([\d.]+)%.*HELD-OUT nodes mean \|error\| ([\d.]+)', l)
    if m: it, sc, ho = int(m[1]), float(m[2]), float(m[3]); key = (sc - 100 * ho)   # reward agreement, punish held-out error
    else: continue
    if best is None or key > best[0]: best = (key, it, sc, ho)
print('freeze iteration', best[1], 'score', best[2], 'held-out err', best[3])
json.dump(json.load(open(f'calib/mw_delta_{best[1]}.json')), open('model_mw.json', 'w'))
open('build_state.txt', 'a').write(f'frozen model_mw.json from fit iteration {best[1]} (score {best[2]}, held-out error {best[3]})\n')
P
fi
# ---- stage 1: three passes over all tables
for P in 1 2 3; do
  OUTD=bfull_p$P; mkdir -p $OUTD/.claim
  worker() { for f in cfgs_full/*.json; do b=$(basename $f .json); o=$OUTD/$b.json; [ -s $o ] && continue
    [ $(left) -lt 1000 ] && return    # not enough time left for one more table in this launch
    mkdir $OUTD/.claim/$b 2>/dev/null || continue
    d=${b##*-d}; if [ $d -le 25 ]; then SECS=600; else SECS=900; fi
    TI=0; [ $P -gt 1 ] && TI=$(node -e "console.log(require('./bfull_p1/$b.json').iters)")
    if TARGET_ITERS=$TI node solve_full.js $f $SECS 60 $o >> $OUTD/log.txt 2>&1; then echo "done $b" >> $OUTD/log.txt; else echo "FAIL $b" >> $OUTD/log.txt; rm -rf $OUTD/.claim/$b; fi; done; }
  worker & worker & wait
  n=$(ls $OUTD/*.json 2>/dev/null | wc -l); log "pass $P: $n/20 tables"
  [ $n -lt 20 ] && { log "deadline reached; relaunch to continue"; exit 0; }
done
# ---- stage 2: consensus into the app library, then gates
rm -rf ../../app/lib; python3 consensus_full.py ../../app/lib bfull_p1 bfull_p2 bfull_p3 > consensus_build.txt 2>&1
python3 build_gates.py && log "BUILD COMPLETE" || log "GATES FAILED (see build_report.md)"
