#!/bin/bash
# SOLVER_PLAN P1: run every config in p1/cfgs through realize/solve_full.js (60 s per flop, 400k EV samples), one at a time
# (never alongside Blender: memory), skipping finished ones, so it resumes after a stop.   bash tools/pf/p1_run.sh
cd "$(dirname "$0")/realize" || exit 1
mkdir -p ../p1/out
for f in ../p1/cfgs/*.json; do
  o=../p1/out/$(basename "$f")
  [ -s "$o" ] && continue
  if node solve_full.js "$f" ${P1_SECS:-60} ${P1_EVK:-400} "$o.part" > /dev/null 2>&1; then mv "$o.part" "$o"; echo "done $(basename "$f")"; else echo "FAIL $(basename "$f")"; rm -f "$o.part"; fi
done
echo "P1 RUN COMPLETE: $(ls ../p1/out/*.json | wc -l) outputs"
