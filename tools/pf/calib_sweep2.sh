#!/bin/bash
cd "$(dirname "$0")"
./calib.sh s2base A=1
./calib.sh mwplay15 MWPLAY=1.5
./calib.sh mwplay2 MWPLAY=2
./calib.sh mwplayk25 MWPLAYK=0.25
./calib.sh mwplay15pen2 MWPLAY=1.5 MWPEN=2
./calib.sh mwplay15k25pen2 MWPLAY=1.5 MWPLAYK=0.25 MWPEN=2
./calib.sh mwgam15 MWPLAY=1.5 MWGAM=0.15
echo SWEEP2 DONE
