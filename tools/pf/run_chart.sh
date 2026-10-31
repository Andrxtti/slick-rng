#!/bin/bash
# Chart library: 5 depths, 8-max, chip EV; three equal-iteration passes through the same consensus gate.
cd "$(dirname "$0")"
for P in 1 2 3; do
  OUTD=out_chart$P; mkdir -p $OUTD/.claim
  worker() { for f in cfgs_chart/chart-*.json; do b=$(basename $f .json); o=$OUTD/$b.json; [ -s $o ] && continue; mkdir $OUTD/.claim/$b 2>/dev/null || continue
    TI=0; if [ $P -gt 1 ]; then TI=$(node -e "console.log(require('./out_chart1/$b.json').iters)"); fi
    if TARGET_ITERS=$TI node solve_lib.js $f ${CHART_SECS:-900} 60 $o >> $OUTD/log.txt 2>&1; then echo "done $b" >> $OUTD/log.txt; else echo "FAIL $b" >> $OUTD/log.txt; rm -rf $OUTD/.claim/$b; fi; done; }
  worker & worker & wait
done
echo "CHART PIPELINE DONE $(date)" >> out_chart_STATUS.txt
