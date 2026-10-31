#!/bin/bash
# P2 pilot: one-pass preflop solves of 8-max 40bb and 25bb, the formula (base) vs the P1 table (REALTAB), then the P0 grid
# and regression checks on each.   bash tools/pf/p2_pilot.sh
cd "$(dirname "$0")"; O=p2/pilot
for d in 40 25; do for v in base tab; do
  o=$O/$v/m8-d$d.json; mkdir -p $O/$v; [ -s $o ] && continue
  if [ $v = tab ]; then export REALTAB=p2/real_table.json; else unset REALTAB; fi
  node solve_full.js cfgs_full/m8-d$d.json 900 60 $o.part >> $O/log.txt 2>&1 && mv $o.part $o
done; done
unset REALTAB
for v in base tab; do node p0_grid.js $O/$v pilot_$v > /dev/null; done
node p0_compare.js pilot_base pilot_tab > $O/compare.txt; tail -14 $O/compare.txt
