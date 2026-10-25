#!/bin/bash
# Refit scan for the multiway flop model: solve the 100bb 8-max tree under several MW settings, two at a time (two
# cores), then summarize every run against the 18 charts with mwsum.js.
#   bash mwfit.sh <secs> "play,gam,r,pen,leak" "play,gam,r,pen,leak" ...     (tags: fit_<play>_<gam>_<r>_<pen>_<leak>)
# pen and leak may be left off (default 1 and 0). A setting of "off" solves with the engine's original multiway
# settlement (MW=0) as the control.
cd "$(dirname "$0")"
SECS=${1:-450}; shift
run() {
  local c=$1
  if [ "$c" = off ]; then MW=0 EVSECS=${EVSECS:-60} node mw.js "$SECS" fit_off > out/mw100_fit_off.log 2>&1
  else IFS=, read -r p g r pen leak <<< "$c"; pen=${pen:-1}; leak=${leak:-0}
    MW=1 MWPLAY=$p MWGAM=$g MWR=$r MWPEN=$pen MWLEAK=$leak EVSECS=${EVSECS:-60} node mw.js "$SECS" "fit_${p}_${g}_${r}_${pen}_${leak}" > "out/mw100_fit_${p}_${g}_${r}_${pen}_${leak}.log" 2>&1; fi
}
while [ $# -gt 0 ]; do
  a=$1; shift; b=$1; [ -n "$b" ] && shift
  run "$a" & [ -n "$b" ] && run "$b" & wait
  b=
done
node mwsum.js out/mw100_fit_*.json
