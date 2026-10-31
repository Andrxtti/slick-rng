#!/bin/bash
# Re-solve every library table under the current multiway model (MW defaults in solver2.js) through the full gate, into
# out_mw/ so the shipped out/ solves are untouched. Two workers, one per core. Each worker claims a table with an atomic
# mkdir, so a restart after a stop just picks up the tables not yet finished (a table with a finished json is skipped).
#   bash run_mw.sh [secs-per-table-overrides via SHOVE_SECS / BUBBLE_SECS / SAT_SECS / RING_SECS]
cd "$(dirname "$0")"
OUTD=${OUTD:-out_mw}; mkdir -p "$OUTD" "$OUTD/.claim"
EVS=${EVS:-60}
worker() {
  for f in $(ls cfgs/shove-*.json; ls cfgs/bubble-*.json; ls cfgs/sat-*.json; ls cfgs/ring-*.json); do
    b=$(basename "$f" .json); o="$OUTD/$b.json"
    [ -s "$o" ] && continue
    mkdir "$OUTD/.claim/$b" 2>/dev/null || continue
    T=${SECS:-300}; case $b in shove-*) T=${SHOVE_SECS:-240};; ring-*) T=${RING_SECS:-480};; bubble-*) T=${BUBBLE_SECS:-$T};; sat-*) T=${SAT_SECS:-$T};; esac
    # ITERS_FROM="out_mw out_mw2": solve at least as many iterations as the smallest of those passes did on this table
    TI=0; if [ -n "$ITERS_FROM" ]; then TI=$(node -e "let m=0;for(const d of process.argv.slice(1)){try{const i=require('./'+d+'/$b.json').iters;m=m?Math.min(m,i):i}catch(e){}}console.log(m)" $ITERS_FROM); fi
    if TARGET_ITERS=$TI node solve_lib.js "$f" "$T" "${EVS}" "$o" >> "$OUTD/log.txt" 2>&1; then echo "done $b" >> "$OUTD/log.txt"; else echo "FAIL $b" >> "$OUTD/log.txt"; rm -rf "$OUTD/.claim/$b"; fi
  done
}
worker & worker & wait
echo ALLDONE >> "$OUTD/log.txt"
