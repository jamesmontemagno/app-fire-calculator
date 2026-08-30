#!/usr/bin/env python3
"""Compose App Store marketing screenshots from raw device captures.

Each output is exactly the size App Store Connect requires, with a branded
gradient background, a headline, a subhead, and the raw capture drawn inside a
rounded device frame with a bezel and drop shadow.
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont

RAW = sys.argv[1]
OUT = sys.argv[2]
CANVAS_W, CANVAS_H = (int(sys.argv[3]), int(sys.argv[4])) if len(sys.argv) > 4 else (1242, 2688)

os.makedirs(OUT, exist_ok=True)

# Brand palette pulled from the app's own dark surface and accent tokens.
INK = (14, 47, 36)
INK_DEEP = (8, 30, 23)
CREAM = (247, 246, 239)
ACCENT = (255, 158, 44)
MUTED = (168, 199, 184)

SLIDES = [
    ("01-home.png", "Your whole plan,\nat a glance", "Net worth, progress, and next steps on one private dashboard."),
    ("02-accounts.png", "Track every\naccount", "Assets, income, expenses, and debts \u2014 all stored on your device."),
    ("03-history.png", "Watch your\nnet worth grow", "Monthly check-ins build trends you actually own."),
    ("04-calculators.png", "14 focused\ncalculators", "FIRE, Coast, Barista, 72(t), Roth conversions, debt payoff, and more."),
    ("05-coast-fire.png", "Project every\nscenario", "Interactive charts, saved plans, and Excel exports."),
]


def font(size, bold=False):
    names = (
        ["/System/Library/Fonts/SFNSDisplay-Bold.otf", "/System/Library/Fonts/Supplemental/Arial Bold.ttf"]
        if bold else
        ["/System/Library/Fonts/SFNSDisplay.otf", "/System/Library/Fonts/Supplemental/Arial.ttf"]
    )
    names.append("/System/Library/Fonts/Helvetica.ttc")
    for name in names:
        if os.path.exists(name):
            try:
                return ImageFont.truetype(name, size)
            except OSError:
                continue
    return ImageFont.load_default(size)


def gradient(width, height):
    """Vertical brand gradient, drawn once per row then resized for smoothness."""
    strip = Image.new("RGB", (1, height))
    px = strip.load()
    for y in range(height):
        t = y / max(1, height - 1)
        px[0, y] = tuple(round(INK[i] + (INK_DEEP[i] - INK[i]) * t) for i in range(3))
    return strip.resize((width, height), Image.BILINEAR)


def rounded(image, radius):
    mask = Image.new("L", image.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, image.size[0] - 1, image.size[1] - 1],
                                           radius=radius, fill=255)
    out = image.convert("RGBA")
    out.putalpha(mask)
    return out


def wrap(draw, text, fnt, max_width):
    lines = []
    for paragraph in text.split("\n"):
        words, line = paragraph.split(), ""
        for word in words:
            trial = f"{line} {word}".strip()
            if draw.textlength(trial, font=fnt) <= max_width or not line:
                line = trial
            else:
                lines.append(line)
                line = word
        lines.append(line)
    return lines


def compose(raw_path, headline, subhead, out_path):
    canvas = gradient(CANVAS_W, CANVAS_H).convert("RGBA")
    draw = ImageDraw.Draw(canvas)

    scale = CANVAS_W / 1320
    margin = int(96 * scale)
    head_font = font(int(96 * scale), bold=True)
    sub_font = font(int(46 * scale), bold=False)

    # --- Text block ---
    y = int(150 * scale)
    for line in wrap(draw, headline, head_font, CANVAS_W - margin * 2):
        draw.text((margin, y), line, font=head_font, fill=CREAM)
        y += int(head_font.size * 1.16)

    y += int(24 * scale)
    for line in wrap(draw, subhead, sub_font, CANVAS_W - margin * 2):
        draw.text((margin, y), line, font=sub_font, fill=MUTED)
        y += int(sub_font.size * 1.34)

    # Accent rule ties the text block to the brand's orange.
    y += int(34 * scale)
    draw.rounded_rectangle([margin, y, margin + int(150 * scale), y + int(9 * scale)],
                           radius=int(5 * scale), fill=ACCENT)
    text_bottom = y + int(70 * scale)

    # --- Device frame ---
    shot = Image.open(raw_path).convert("RGB")
    avail_h = CANVAS_H - text_bottom - int(70 * scale)
    avail_w = CANVAS_W - margin * 2
    ratio = min(avail_w / shot.width, avail_h / shot.height)
    target = (max(1, int(shot.width * ratio)), max(1, int(shot.height * ratio)))
    shot = shot.resize(target, Image.LANCZOS)

    corner = int(58 * ratio * (shot.width / target[0] or 1))
    corner = max(12, int(62 * ratio))
    screen = rounded(shot, corner)

    bezel = max(4, int(10 * scale))
    frame = Image.new("RGBA", (screen.width + bezel * 2, screen.height + bezel * 2), (0, 0, 0, 0))
    ImageDraw.Draw(frame).rounded_rectangle(
        [0, 0, frame.width - 1, frame.height - 1],
        radius=corner + bezel, fill=(4, 20, 15, 255))
    frame.alpha_composite(screen, (bezel, bezel))

    fx = (CANVAS_W - frame.width) // 2
    fy = text_bottom

    # Soft drop shadow so the frame lifts off the gradient.
    shadow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle(
        [fx, fy + int(20 * scale), fx + frame.width, fy + frame.height + int(20 * scale)],
        radius=corner + bezel, fill=(0, 0, 0, 130))
    canvas.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(int(34 * scale))))
    canvas.alpha_composite(frame, (fx, fy))

    canvas.convert("RGB").save(out_path, "PNG")
    return out_path


for index, (name, headline, subhead) in enumerate(SLIDES, start=1):
    src = os.path.join(RAW, name)
    if not os.path.exists(src):
        print(f"skip missing {name}")
        continue
    dest = os.path.join(OUT, name)
    compose(src, headline, subhead, dest)
    with Image.open(dest) as check:
        print(f"{name}: {check.size[0]}x{check.size[1]}")
