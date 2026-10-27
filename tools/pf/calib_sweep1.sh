#!/bin/bash
cd "$(dirname "$0")"
./calib.sh base A=1
./calib.sh io11 IO=1.1
./calib.sh io14 IO=1.4
./calib.sh ppos15 PPOS=1.5
./calib.sh rlow78 RLOW=0.78
./calib.sh pneg3 PNEG=3
./calib.sh mwpen2 MWPEN=2
echo SWEEP1 DONE
