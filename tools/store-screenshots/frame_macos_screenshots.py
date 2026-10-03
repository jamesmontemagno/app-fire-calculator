#!/usr/bin/env python3
"""Compose Mac App Store marketing screenshots from Mac Catalyst captures."""
import os
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

RAW = sys.argv[1]
OUT = sys.argv[2]
CANVAS_W, CANVAS_H = (2880, 1800)

os.makedirs(OUT, exist_ok=True)

INK_TOP = (20, 70, 54)
INK_BOTTOM = (7, 27, 21)
CREAM = (247, 246, 239)
MUTED = (181, 210, 195)
ACCENT = (255, 158, 44)
FRAME = (10, 17, 14)

SLIDES = [
    (
        "01-home.png",
        "Your financial future,\nin focus",
        "Net worth, progress, and next steps — private and on-device.",
    ),
    (
        "02-accounts.png",
        "Every account.\nOne clear picture.",
        "Track assets, income, expenses, and debts without giving up your data.",
    ),
    (
        "03-history.png",
        "See progress\nbecome momentum",
        "Monthly check-ins turn your plan into trends you can act on.",
    ),
    (
        "04-coast-fire.png",
        "Model the life\nyou want",
        "Link your profile, adjust assumptions, and explore your path to Coast FIRE.",
    ),
]


def font(size, bold=False):
    names = (
        [
            "/System/Library/Fonts/SFNSDisplay-Bold.otf",
            "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        ]
        if bold
        else [
            "/System/Library/Fonts/SFNSDisplay.otf",
            "/System/Library/Fonts/Supplemental/Arial.ttf",
        ]
    )
    names.append("/System/Library/Fonts/Helvetica.ttc")
    for name in names:
        if os.path.exists(name):
            try:
                return ImageFont.truetype(name, size)
            except OSError:
                continue
    return ImageFont.load_default()


def gradient():
    strip = Image.new("RGB", (1, CANVAS_H))
    pixels = strip.load()
    for y in range(CANVAS_H):
        t = y / max(1, CANVAS_H - 1)
        pixels[0, y] = tuple(
            round(INK_TOP[channel] + (INK_BOTTOM[channel] - INK_TOP[channel]) * t)
            for channel in range(3)
        )
    return strip.resize((CANVAS_W, CANVAS_H), Image.Resampling.BILINEAR).convert("RGBA")


def add_glow(canvas, center, radius, color):
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    x, y = center
    draw.ellipse(
        [x - radius, y - radius, x + radius, y + radius],
        fill=(*color, 42),
    )
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(radius // 2)))


def crop_capture(image):
    background = Image.new("RGB", image.size, (0, 0, 0))
    bounds = ImageChops.difference(image.convert("RGB"), background).getbbox()
    return image.crop(bounds) if bounds else image


def rounded(image, radius):
    mask = Image.new("L", image.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [0, 0, image.width - 1, image.height - 1],
        radius=radius,
        fill=255,
    )
    result = image.convert("RGBA")
    result.putalpha(mask)
    return result


def wrap(draw, text, text_font, max_width):
    lines = []
    for paragraph in text.split("\n"):
        words = paragraph.split()
        line = ""
        for word in words:
            candidate = f"{line} {word}".strip()
            if not line or draw.textlength(candidate, font=text_font) <= max_width:
                line = candidate
            else:
                lines.append(line)
                line = word
        lines.append(line)
    return lines


def compose(raw_path, headline, subhead, output_path):
    canvas = gradient()
    add_glow(canvas, (2600, 180), 560, ACCENT)
    add_glow(canvas, (1880, 1680), 760, (54, 148, 112))
    draw = ImageDraw.Draw(canvas)

    left = 150
    text_width = 930
    eyebrow_font = font(34, bold=True)
    headline_font = font(112, bold=True)
    subhead_font = font(48)

    draw.rounded_rectangle(
        [left, 145, left + 255, 207],
        radius=31,
        fill=ACCENT,
    )
    draw.text((left + 28, 157), "MY FIRE #", font=eyebrow_font, fill=INK_BOTTOM)

    y = 310
    for line in wrap(draw, headline, headline_font, text_width):
        draw.text((left, y), line, font=headline_font, fill=CREAM)
        y += 128

    y += 42
    for line in wrap(draw, subhead, subhead_font, text_width):
        draw.text((left, y), line, font=subhead_font, fill=MUTED)
        y += 68

    y += 48
    draw.rounded_rectangle([left, y, left + 190, y + 13], radius=7, fill=ACCENT)

    capture = crop_capture(Image.open(raw_path).convert("RGB"))
    max_capture_w = 1540
    max_capture_h = 1210
    ratio = min(max_capture_w / capture.width, max_capture_h / capture.height)
    capture = capture.resize(
        (round(capture.width * ratio), round(capture.height * ratio)),
        Image.Resampling.LANCZOS,
    )

    chrome_h = 72
    bezel = 10
    window_w = capture.width + bezel * 2
    window_h = capture.height + chrome_h + bezel
    window = Image.new("RGBA", (window_w, window_h), (0, 0, 0, 0))
    window_draw = ImageDraw.Draw(window)
    window_draw.rounded_rectangle(
        [0, 0, window_w - 1, window_h - 1],
        radius=30,
        fill=FRAME,
        outline=(83, 117, 101, 255),
        width=2,
    )
    window_draw.rectangle([0, 44, window_w - 1, chrome_h], fill=FRAME)
    for index, color in enumerate(((255, 95, 87), (254, 188, 46), (40, 200, 64))):
        cx = 38 + index * 34
        window_draw.ellipse([cx, 25, cx + 18, 43], fill=color)

    window.alpha_composite(rounded(capture, 18), (bezel, chrome_h))

    window_x = CANVAS_W - window_w - 105
    window_y = (CANVAS_H - window_h) // 2 + 20
    shadow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    shadow_draw.rounded_rectangle(
        [
            window_x - 15,
            window_y + 35,
            window_x + window_w + 15,
            window_y + window_h + 45,
        ],
        radius=44,
        fill=(0, 0, 0, 165),
    )
    canvas.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(42)))
    canvas.alpha_composite(window, (window_x, window_y))

    canvas.convert("RGB").save(output_path, "PNG", optimize=True)


for name, headline, subhead in SLIDES:
    source = os.path.join(RAW, name)
    if not os.path.exists(source):
        raise FileNotFoundError(source)
    destination = os.path.join(OUT, name)
    compose(source, headline, subhead, destination)
    with Image.open(destination) as result:
        print(f"{name}: {result.width}x{result.height}")
