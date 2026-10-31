#!/bin/bash
# cloud share of The Dream re-solve (4.5 model): the jobs in $1 (json list), two at a time, into out_v45_p3/
cd "$(dirname "$0")"; JOBS=$1; mkdir -p out_v45_p3/.claim
worker() { node -e "for (const j of require('$JOBS')) console.log(j.id.replace(/^dream-p3-/,''), j.secs, j.env.TARGET_ITERS)" | while read b T TI; do
  o=out_v45_p3/$b.json; [ -s $o ] && continue; mkdir out_v45_p3/.claim/$b 2>/dev/null || continue
  TARGET_ITERS=$TI node solve_lib.js cfgs/$b.json $T 60 $o >> out_v45_p3/log.txt 2>&1 && echo "done $b" >> out_v45_p3/log.txt || { echo "FAIL $b" >> out_v45_p3/log.txt; rm -rf out_v45_p3/.claim/$b; }
done; }
worker & worker & wait; echo "CLOUD DREAM DONE $(ls out_v45_p3/*.json | wc -l)" >> out_v45_p3/log.txt
