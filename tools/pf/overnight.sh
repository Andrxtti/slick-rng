#!/bin/bash
# Library pipeline, resumable and mechanical: solve every library table once per pass dir, then build the consensus
# library (consensus.py) from all passes into out_final/. Run it again whenever it was stopped (the 2h background
# limit); it clears stale claims and continues where it was.
#   PASS_DIRS="out_mw out_mw2 out_mw3" bash overnight.sh   -> STATUS.txt gets progress lines, last one "PIPELINE DONE"
# Solve times per table group come from run_mw.sh (SHOVE_SECS / BUBBLE_SECS / SAT_SECS / RING_SECS).
cd "$(dirname "$0")"
PASS_DIRS=${PASS_DIRS:-"out_mw out_mw2 out_mw3"}
FINAL=${FINAL:-out_final}
# the library is the shove/bubble/sat/ring tables (the same globs run_mw.sh walks; cash-* configs are not in the library)
N=$(ls cfgs/shove-*.json cfgs/bubble-*.json cfgs/sat-*.json cfgs/ring-*.json | wc -l)
status() { echo "$(date '+%m-%d %H:%M') $*" >> STATUS.txt; }
run_pass() { # $1 = output dir
  local d=$1; mkdir -p "$d/.claim"
  for c in "$d"/.claim/*/; do [ -d "$c" ] || continue; b=$(basename "$c"); [ -s "$d/$b.json" ] || rm -rf "$c"; done   # stale claims
  local have=$(ls "$d"/*.json 2>/dev/null | wc -l)
  if [ "$have" -ge "$N" ]; then status "$d complete ($have/$N)"; return 0; fi
  status "$d resuming at $have/$N"
  OUTD=$d bash run_mw.sh
  have=$(ls "$d"/*.json 2>/dev/null | wc -l); status "$d now $have/$N"
  [ "$have" -ge "$N" ]
}
for d in $PASS_DIRS; do run_pass "$d" || exit 0; done
rm -rf "$FINAL"; mkdir -p "$FINAL"
python3 consensus.py "$FINAL" $PASS_DIRS > "$FINAL/consensus.log" 2>&1
status "consensus library built in $FINAL: $(tail -1 "$FINAL/consensus.log")"
status "PIPELINE DONE"
