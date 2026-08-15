#!/usr/bin/env python3
"""Fail if a raw hex colour literal reappears in the MAUI view XAML.

Views must consume named theme resources from Resources/Styles/Colors.xaml, so
the palette has exactly one home. See issue #80.

The detector deliberately does NOT use `\\b` around the pattern. `#` is not a
word character, so `\\b#[0-9A-Fa-f]{6}\\b` silently matches nothing and any
check built on it passes forever while the code rots. This script proves it can
still see a colour -- on a fixture and on the real palette file -- before it is
willing to report success.

Usage:
    check_no_raw_hex.py [--root REPO_ROOT]
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

HEX_ANY = re.compile(r"#[0-9A-Fa-f]{3,8}")
VIEWS = "app/MyFireNumber/Views"
PALETTE = "app/MyFireNumber/Resources/Styles/Colors.xaml"

FIXTURE = '<Label TextColor="#17352C" BackgroundColor="#FFFFFF" />'


def self_check(palette: Path) -> None:
    """Refuse to report success unless the detector demonstrably still works."""
    found = HEX_ANY.findall(FIXTURE)
    if len(found) != 2:
        sys.exit(f"detector self-check FAILED: fixture yielded {found}")

    if not palette.exists():
        sys.exit(f"detector self-check FAILED: {palette} is missing")
    n = len(HEX_ANY.findall(palette.read_text(encoding="utf-8")))
    if n == 0:
        sys.exit("detector self-check FAILED: no hex found in the palette file")
    print(f"detector self-check ok (fixture=2, {palette.name}={n})")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=".")
    args = ap.parse_args()
    root = Path(args.root).resolve()

    self_check(root / PALETTE)

    offenders: list[str] = []
    scanned = 0
    for path in sorted((root / VIEWS).rglob("*.xaml")):
        scanned += 1
        for i, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            for m in HEX_ANY.finditer(line):
                rel = path.relative_to(root)
                offenders.append(f"{rel}:{i}: {m.group(0)}  |  {line.strip()[:90]}")

    print(f"scanned {scanned} view XAML files under {VIEWS}")
    if offenders:
        print(f"\n{len(offenders)} raw hex colour literal(s) found; use a named "
              f"resource from {PALETTE} instead:\n")
        for o in offenders[:40]:
            print(f"  {o}")
        if len(offenders) > 40:
            print(f"  ... and {len(offenders) - 40} more")
        return 1

    print("no raw hex colour literals in view XAML")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
