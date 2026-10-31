#!/bin/bash
# Cloud share of a library build: runs the jobs in $1 (from pc_build.py --cloud), two at a time, into bfull_p<pass>/.
# Resumable: finished outputs are skipped, a claim folder stops two workers taking the same job. Relaunch until DONE.
cd "$(dirname "$0")"; JOBS=$1; mkdir -p .claim_cloud
worker() { node -e "for (const j of require('./$JOBS')) console.log(j.id, j.cfg, j.pass, j.table, j.secs, j.evs, j.env.AGGRE, j.env.PLAYREAL, j.env.TARGET_ITERS, j.env.MAXSECS)" |
  while read id cfg P b T EV AG PR TI MX; do
    o=bfull_p$P/$b.json; [ -s $o ] && continue; mkdir -p bfull_p$P; mkdir .claim_cloud/$id 2>/dev/null || continue
    if AGGRE=$AG PLAYREAL=$PR TARGET_ITERS=$TI MAXSECS=$MX node solve_full.js $cfg $T $EV $o.part >> cloud_build.log 2>&1; then mv $o.part $o; echo "done $id" >> cloud_build.log
    else echo "FAIL $id" >> cloud_build.log; rm -rf .claim_cloud/$id $o.part; fi
  done; }
worker & worker & wait; echo "CLOUD DONE $(date +%H:%M)" >> cloud_build.log
