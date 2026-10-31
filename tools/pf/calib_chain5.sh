#!/bin/bash
# stop sweep 3 after gre0, then the stack-off runs (Oct 28). Results in calib_log4.txt
cd "$(dirname "$0")"
until grep -q "^gre0" calib_log3.txt 2>/dev/null; do sleep 10; done
p=$(pgrep -f "calib_sweep3"); [ -n "$p" ] && kill $p; sleep 1; pkill -f "solve_full.js cfgs_chart"; sleep 2
echo "sweep 3 stopped after gre0" >> calib_log3.txt
run() { ./calib.sh "$@" >> calib_log4.txt 2>&1; }
run pa3 PA=3
run pa5 PA=5
echo SWEEP4 DONE >> calib_log4.txt
