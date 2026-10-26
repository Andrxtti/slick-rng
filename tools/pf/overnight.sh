#!/bin/bash
# Overnight library pipeline, resumable and mechanical: pass 1 (out_mw) -> pass 2 (out_mw2) -> consensus diff.
# Run it again whenever it was stopped (the 2h background limit); it clears stale claims and continues where it was.
#   bash overnight.sh          -> appends progress lines to STATUS.txt, ends with "PIPELINE DONE" when everything is finished
cd "$(dirname "$0")"
N=$(ls cfgs/*.json | wc -l)
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
run_pass out_mw || exit 0
run_pass out_mw2 || exit 0
if [ ! -s CONSENSUS.txt ]; then
  for f in out_mw/*.json; do b=$(basename "$f"); python3 libdiff.py "$f" "out_mw2/$b" "$b" >> CONSENSUS.txt 2>&1; done
  status "consensus written: CONSENSUS.txt ($(wc -l < CONSENSUS.txt) lines)"
fi
status "PIPELINE DONE"
