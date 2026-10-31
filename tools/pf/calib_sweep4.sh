#!/bin/bash
# stack-off at low SPR (PA) with the 3-bet composition knobs (Oct 28). Waits for sweep 3. Results in calib_log4.txt
cd "$(dirname "$0")"
until grep -q "SWEEP3 DONE" calib_log3.txt 2>/dev/null; do sleep 15; done
run() { ./calib.sh "$@" >> calib_log4.txt 2>&1; }
run pa3 PA=3
run pa5 PA=5
run pa3pair12 PA=3 PAIRSPAN=12
run pa5pair12pre1 PA=5 PAIRSPAN=12 PRE=1
run pa3pair20 PA=3 PAIRSPAN=20
echo SWEEP4 DONE >> calib_log4.txt
