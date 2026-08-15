#!/usr/bin/env python3
"""
Emit a manifest of every colour-valued XAML attribute under app/MyFireNumber/Views/,
resolved to concrete light/dark hex values.

The point of this tool is to prove that migrating inline hex literals to named theme
resources is a *pure* refactor: run it before the migration and after, and the two
manifests must be byte-identical.

Each record is

    <file>|<element-path>|<attribute>|<light>|<dark>

The element path carries sibling indexes, so it stays stable while attribute *values*
change but shifts immediately if elements are added, removed, or reordered -- which is
exactly the accident this guards against.

Values that are not colours are dropped. A `{StaticResource}` pointing at something that
is not a colour (a Style, a converter, a thickness) resolves to nothing and is skipped,
identically on both sides of the comparison.

Usage:
    theme_color_manifest.py [--root REPO_ROOT] [--out FILE]
"""

from __future__ import annotations

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

VIEWS_SUBDIR = Path("app/MyFireNumber/Views")
PALETTE_FILES = [
    Path("app/MyFireNumber/Resources/Styles/Colors.xaml"),
]

# Only these attributes are read as colours. A whitelist rather than a value-shaped guess:
# "Transparent" is a colour on Background but a plain word on Text, and guessing from the
# value alone pulls in HorizontalOptions="Center" and friends.
COLOR_ATTRIBUTES = {
    "BackgroundColor", "BarBackgroundColor", "BarTextColor", "BorderColor", "Color",
    "CursorColor", "DisabledColor", "ForegroundColor", "IconColor", "MaximumTrackColor",
    "MinimumTrackColor", "PlaceholderColor", "PointColor", "ProgressColor",
    "SelectionColor", "ShadowColor", "TextColor", "ThumbColor", "TitleColor",
    "TextDecorationColor", "UnselectedColor", "TabBarBackgroundColor",
    "TabBarForegroundColor", "TabBarTitleColor", "TabBarUnselectedColor",
    "TabBarDisabledColor", "LineColor", "IndicatorColor", "SelectedIndicatorColor",
}
# Brush-typed properties. Kept separate only for readability; both are resolved the same way.
BRUSH_ATTRIBUTES = {"Background", "Stroke", "Fill"}

HEX_RE = re.compile(r"^#(?:[0-9A-Fa-f]{3,8})$")
RESOURCE_RE = re.compile(r"^\{(?:Static|Dynamic)Resource\s+([^}]+)\}$")
APPTHEME_RE = re.compile(r"^\{AppThemeBinding\s+(.*)\}$", re.DOTALL)
# Light=<value>, Dark=<value> where <value> is a bare token or a nested {…} extension.
APPTHEME_ARG_RE = re.compile(r"(Light|Dark|Default)\s*=\s*(\{[^{}]*\}|[^,}]+)")


def normalize_hex(value: str) -> str | None:
    """Normalise a XAML hex colour to upper-case #RRGGBB or #AARRGGBB."""
    if not HEX_RE.match(value):
        return None
    digits = value[1:].upper()
    if len(digits) in (3, 4):  # #RGB / #ARGB shorthand
        return "#" + "".join(c * 2 for c in digits)
    if len(digits) in (6, 8):
        return "#" + digits
    return None


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1] if "}" in tag else tag


def load_palette(root: Path) -> dict[str, str]:
    """Build key -> hex from the palette dictionaries, resolving StaticResource chains."""
    raw: dict[str, str] = {}
    for rel in PALETTE_FILES:
        path = root / rel
        if not path.exists():
            continue
        for element in ET.parse(path).getroot().iter():
            if local_name(element.tag) != "Color":
                continue
            key = next((v for k, v in element.attrib.items() if local_name(k) == "Key"), None)
            if key and element.text:
                raw[key] = element.text.strip()

    resolved: dict[str, str] = {}

    def resolve(key: str, seen: frozenset[str]) -> str | None:
        if key in resolved:
            return resolved[key]
        if key in seen or key not in raw:
            return None
        value = raw[key]
        hex_value = normalize_hex(value)
        if hex_value is None:
            match = RESOURCE_RE.match(value)
            if match:
                hex_value = resolve(match.group(1).strip(), seen | {key})
            else:
                # A named colour such as "White". Kept verbatim; it compares identically
                # on both sides because the migration never rewrites these.
                hex_value = value.strip().upper()
        if hex_value is not None:
            resolved[key] = hex_value
        return hex_value

    for key in raw:
        resolve(key, frozenset())
    return resolved


def resolve_scalar(value: str, palette: dict[str, str]) -> str | None:
    """Resolve one non-AppThemeBinding value to a colour, or None if it isn't one."""
    value = value.strip()
    hex_value = normalize_hex(value)
    if hex_value:
        return hex_value
    match = RESOURCE_RE.match(value)
    if match:
        return palette.get(match.group(1).strip())
    if value.startswith("{"):
        return None  # Binding, converter, or another markup extension.
    if re.fullmatch(r"[A-Za-z]+", value):
        # A named colour such as Transparent/White. Never rewritten by the migration,
        # so it compares identically; kept so those attributes stay in the manifest.
        return value.upper()
    return None


def is_color_attribute(element: ET.Element, name: str) -> bool:
    """True when this attribute on this element carries a colour."""
    short = local_name(name).rsplit(".", 1)[-1]
    if short in COLOR_ATTRIBUTES or short in BRUSH_ATTRIBUTES:
        return True
    # <Setter Property="TextColor" Value="#FFFFFF" /> -- the value is a colour only
    # when the targeted property is one. This is the DataTrigger/VisualState shape.
    if short == "Value" and local_name(element.tag) == "Setter":
        target = next(
            (v for k, v in element.attrib.items() if local_name(k) == "Property"),
            "",
        ).rsplit(".", 1)[-1]
        return target in COLOR_ATTRIBUTES or target in BRUSH_ATTRIBUTES
    return False


def resolve_attribute(value: str, palette: dict[str, str]) -> tuple[str, str] | None:
    """Resolve an attribute value to (light, dark), or None if it is not a colour."""
    value = value.strip()
    theme = APPTHEME_RE.match(value)
    if theme:
        args = {m.group(1): m.group(2).strip() for m in APPTHEME_ARG_RE.finditer(theme.group(1))}
        default = args.get("Default")
        light_raw = args.get("Light", default)
        dark_raw = args.get("Dark", default)
        if light_raw is None or dark_raw is None:
            return None
        light = resolve_scalar(light_raw, palette)
        dark = resolve_scalar(dark_raw, palette)
        if light is None or dark is None:
            return None
        return light, dark

    scalar = resolve_scalar(value, palette)
    return None if scalar is None else (scalar, scalar)


def walk(element: ET.Element, path: str, palette: dict[str, str], records: list[str], rel: str) -> None:
    for name in sorted(element.attrib):
        if not is_color_attribute(element, name):
            continue
        resolved = resolve_attribute(element.attrib[name], palette)
        if resolved is None:
            continue
        light, dark = resolved
        records.append(f"{rel}|{path}|{local_name(name)}|{light}|{dark}")

    counts: dict[str, int] = {}
    for child in element:
        tag = local_name(child.tag)
        index = counts.get(tag, 0)
        counts[tag] = index + 1
        walk(child, f"{path}/{tag}[{index}]", palette, records, rel)


def build_manifest(root: Path) -> list[str]:
    palette = load_palette(root)
    records: list[str] = []
    for path in sorted((root / VIEWS_SUBDIR).rglob("*.xaml")):
        rel = path.relative_to(root).as_posix()
        node = ET.parse(path).getroot()
        walk(node, f"{local_name(node.tag)}[0]", palette, records, rel)
    return records


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".", help="Repository root")
    parser.add_argument("--out", help="Write the manifest here instead of stdout")
    args = parser.parse_args()

    records = build_manifest(Path(args.root).resolve())
    text = "\n".join(records) + "\n"

    if args.out:
        Path(args.out).write_text(text, encoding="utf-8")
        print(f"{len(records)} colour attributes -> {args.out}", file=sys.stderr)
    else:
        sys.stdout.write(text)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
