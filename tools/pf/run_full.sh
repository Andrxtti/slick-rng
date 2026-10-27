#!/bin/bash
# Full library: every reachable decision per table, three equal-iteration passes. CFGDIR/PREFIX select the table set.
cd "$(dirname "$0")"
CFGDIR=${CFGDIR:-cfgs_chart}; OUTP=${OUTP:-full}; SECS=${FULL_SECS:-900}
for P in ${PASSES:-1 2 3}; do
  OUTD=${OUTP}_p$P; mkdir -p $OUTD/.claim
  worker() { for f in $CFGDIR/*.json; do b=$(basename $f .json); o=$OUTD/$b.json; [ -s $o ] && continue; mkdir $OUTD/.claim/$b 2>/dev/null || continue
    TI=0; if [ $P -gt 1 ]; then TI=$(node -e "console.log(require('./${OUTP}_p1/$b.json').iters)"); fi
    if TARGET_ITERS=$TI node solve_full.js $f $SECS 60 $o >> $OUTD/log.txt 2>&1; then echo "done $b" >> $OUTD/log.txt; else echo "FAIL $b" >> $OUTD/log.txt; rm -rf $OUTD/.claim/$b; fi; done; }
  worker & worker & wait
done
echo "FULL PIPELINE DONE $(date)" >> ${OUTP}_STATUS.txt
