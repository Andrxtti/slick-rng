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
    if node solve_lib.js "$f" "$T" "${EVS}" "$o" >> "$OUTD/log.txt" 2>&1; then echo "done $b" >> "$OUTD/log.txt"; else echo "FAIL $b" >> "$OUTD/log.txt"; rm -rf "$OUTD/.claim/$b"; fi
  done
}
worker & worker & wait
echo ALLDONE >> "$OUTD/log.txt"
