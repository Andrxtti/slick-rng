#!/bin/bash
# re-solve every library table with the clean tree; safe to run two copies at once (one per core)
cd "$(dirname "$0")"
SECS=${SECS:-300}; EVS=${EVS:-60}
for f in $(ls cfgs/shove-*.json; ls cfgs/bubble-*.json; ls cfgs/sat-*.json; ls cfgs/ring-*.json); do
  b=$(basename $f .json); o=out/$b.json
  [ -s $o ] && continue
  mkdir out/$b.lock 2>/dev/null || continue
  T=$SECS; case $b in shove-*) T=${SHOVE_SECS:-240};; ring-*) T=${RING_SECS:-480};; esac
  node solve_lib.js $f $T $EVS $o 2>>out/log.txt || echo "FAIL $b" >> out/log.txt
done
echo ALLDONE >> out/log.txt
