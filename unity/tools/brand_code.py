#!/usr/bin/env python3
"""Make redeem codes for a brand drop.

Writes the codes to a CSV (for the printer) and prints the "codes" block for brand.json, which holds only the
SHA-256 hashes so the codes can't be read out of the game. Codes ignore case, spaces and dashes, exactly as the
game does (Wardrobe.Normalize).

  python brand_code.py --brand drift --item chain:drift_tag --count 500 --out drift_tag_codes.csv
  python brand_code.py --hash "DRIFT-7KQ2-M9XA"        # hash one code you already have
"""
import argparse, csv, hashlib, json, secrets, sys

ALPHABET = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"  # no 0/O or 1/I/L, easy to read off a tag


def normalize(code):
    return "".join(ch for ch in code if ch.isalnum()).upper()


def digest(code):
    return hashlib.sha256(normalize(code).encode("utf-8")).hexdigest()


def make(prefix, groups=2, size=4):
    body = "-".join("".join(secrets.choice(ALPHABET) for _ in range(size)) for _ in range(groups))
    return f"{prefix}-{body}" if prefix else body


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--brand", help="brand key, used as the code prefix")
    ap.add_argument("--item", help="what the codes unlock, as slot:id (e.g. chain:drift_tag)")
    ap.add_argument("--count", type=int, default=100)
    ap.add_argument("--out", help="CSV file for the codes")
    ap.add_argument("--hash", help="just print the hash of one code")
    a = ap.parse_args()

    if a.hash:
        print(digest(a.hash))
        return
    if not a.brand or not a.item or ":" not in a.item:
        ap.error("--brand and --item slot:id are required")

    prefix = normalize(a.brand)[:6]
    codes = set()
    while len(codes) < a.count:
        codes.add(make(prefix))
    codes = sorted(codes)

    if a.out:
        with open(a.out, "w", newline="") as f:
            w = csv.writer(f)
            w.writerow(["code", "unlocks"])
            for c in codes:
                w.writerow([c, a.item])
        print(f"Wrote {len(codes)} codes to {a.out}", file=sys.stderr)
    else:
        for c in codes:
            print(c, file=sys.stderr)

    block = {digest(c): a.item for c in codes}
    print(json.dumps({"codes": block}, indent=1))


if __name__ == "__main__":
    main()
