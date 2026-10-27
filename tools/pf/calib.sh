#!/bin/bash
# calib.sh TAG [ENV=VAL ...]: solve 100bb and 25bb tables with model overrides, score against the saved GTO Wizard nodes
cd "$(dirname "$0")"; TAG=$1; shift; mkdir -p calib
for S in 100 25; do env "$@" node solve_full.js cfgs_chart/chart-${S}bb.json ${CAL_SECS:-300} 5 calib/${TAG}_$S.json > /dev/null 2>&1 & done; wait
echo "$TAG $* : $(QUIET=1 python3 gwlib.py 100=calib/${TAG}_100.json 25=calib/${TAG}_25.json)"
