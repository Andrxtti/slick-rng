# vcfr: heads-up postflop vector CFR core (WebAssembly)

Exact full-traversal Discounted CFR for heads-up postflop spots: both ranges as vectors, every turn/river
card enumerated, exact card removal at showdown, all-in runouts averaged over every remaining card.
The JS side (`VecSolver` / `VecSolverW` in the engine) builds the tree and the ranges and reads results.

Build (clang 18, no libc):

    clang --target=wasm32 -O3 -msimd128 -nostdlib -fno-builtin -Wl,--no-entry -Wl,--export-dynamic \
      -Wl,--initial-memory=2097152 -Wl,--max-memory=4294967296 -o vcfr.wasm vcfr.c

The engine embeds the module as base64 (`VCFR_WASM`). Verified against the JS VecSolver (same values and
exploitability) and against an independent best-response checker on river and turn spots.
