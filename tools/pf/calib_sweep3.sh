#!/bin/bash
# 3-bet composition sweep (Oct 28): playability fade knobs. Results in calib_log3.txt
cd "$(dirname "$0")"
run() { ./calib.sh "$@" >> calib_log3.txt 2>&1; }
run s3base A=1
run pre1 PRE=1
run pair12 PAIRSPAN=12
run pre1pair12 PRE=1 PAIRSPAN=12
run pspan3 PSPAN=3
run gre0 GRE=0
run pre2pair12 PRE=2 PAIRSPAN=12
run pre1pair12io12 PRE=1 PAIRSPAN=12 IOSPAN=12
echo SWEEP3 DONE >> calib_log3.txt
