#!/usr/bin/env python3
"""Compose Microsoft Store marketing screenshots from Windows window captures.

Usage: frame_windows_screenshots.py RAW_DIR OUT_DIR [WIDTH HEIGHT]

Every measurement scales from the canvas width, so any 16:9 size works. The default
2560 x 1440 downscales a capture taken at 150% display scaling slightly, which keeps the
app's text sharp; 3840 x 2160 would upscale the same capture and soften it.
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont

RAW = sys.argv[1]
OUT = sys.argv[2]
CANVAS_W, CANVAS_H = (int(sys.argv[3]), int(sys.argv[4])) if len(sys.argv) > 4 else (2560, 1440)
S = CANVAS_W / 2560

os.makedirs(OUT, exist_ok=True)

INK_TOP = (20, 70, 54)
INK_BOTTOM = (7, 27, 21)
CREAM = (247, 246, 239)
MUTED = (181, 210, 195)
ACCENT = (255, 158, 44)
GLOW_GREEN = (54, 148, 112)
RIM = (74, 104, 91)

SLIDES = [
    (
        "01-home.png",
        "Your financial\nfuture, in focus",
        "Net worth, progress, and next steps \u2014 private and on-device.",
    ),
    (
        "02-accounts.png",
        "Every account.\nOne clear picture.",
        "Track investments, property, income, expenses, and debts without giving up your data.",
    ),
    (
        "03-history.png",
        "See progress\nbecome momentum",
        "Monthly check-ins turn your plan into trends you can act on.",
    ),
    (
        "04-calculators.png",
        "14 focused\ncalculators",
        "FIRE, Coast, Barista, 72(t), Roth conversions, debt payoff, and more.",
    ),
    (
        "05-coast-fire.png",
        "Model the life\nyou want",
        "Link your profile, adjust assumptions, and explore your path to Coast FIRE.",
    ),
]

WINDOWS_FONTS = os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Fonts")
APP_FONTS = os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "..", "..", "app", "MyFireNumber", "Resources", "Fonts"
)


def px(value):
    return round(value * S)


def font(size, bold=False):
    """Segoe UI Variable, the Windows 11 system face, falling back to the app's own Open Sans."""
    variable = os.path.join(WINDOWS_FONTS, "SegUIVar.ttf")
    if os.path.exists(variable):
        try:
            face = ImageFont.truetype(variable, size)
            face.set_variation_by_name("Bold Display" if bold else "Regular Display")
            return face
        except (OSError, ValueError):
            pass
    for name in (
        os.path.join(WINDOWS_FONTS, "segoeuib.ttf" if bold else "segoeui.ttf"),
        os.path.join(APP_FONTS, "OpenSans-Semibold.ttf" if bold else "OpenSans-Regular.ttf"),
    ):
        if os.path.exists(name):
            return ImageFont.truetype(name, size)
    return ImageFont.load_default(size=size)


def build_background():
    strip = Image.new("RGB", (1, CANVAS_H))
    pixels = strip.load()
    for y in range(CANVAS_H):
        t = y / max(1, CANVAS_H - 1)
        pixels[0, y] = tuple(round(INK_TOP[c] + (INK_BOTTOM[c] - INK_TOP[c]) * t) for c in range(3))
    canvas = strip.resize((CANVAS_W, CANVAS_H), Image.Resampling.BILINEAR).convert("RGBA")

    # Glows are soft enough to paint at quarter size, which keeps the wide blurs cheap.
    small = (CANVAS_W // 4, CANVAS_H // 4)
    glows = Image.new("RGBA", small, (0, 0, 0, 0))
    for (cx, cy), radius, color, alpha in (
        ((0.90, 0.06), 520, ACCENT, 50),
        ((0.62, 1.04), 780, GLOW_GREEN, 70),
        ((0.02, 0.98), 460, GLOW_GREEN, 34),
    ):
        layer = Image.new("RGBA", small, (0, 0, 0, 0))
        x, y, r = cx * small[0], cy * small[1], px(radius) / 4
        ImageDraw.Draw(layer).ellipse([x - r, y - r, x + r, y + r], fill=(*color, alpha))
        glows.alpha_composite(layer.filter(ImageFilter.GaussianBlur(r / 2)))
    canvas.alpha_composite(glows.resize((CANVAS_W, CANVAS_H), Image.Resampling.BICUBIC))
    return canvas


def rounded_mask(size, radius):
    """Antialiased rounded rectangle; Pillow's own is drawn without smoothing."""
    scale = 4
    mask = Image.new("L", (size[0] * scale, size[1] * scale), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [0, 0, mask.width - 1, mask.height - 1], radius=radius * scale, fill=255
    )
    return mask.resize(size, Image.Resampling.LANCZOS)


def build_window(raw_path):
    shot = Image.open(raw_path).convert("RGBA")
    # The outermost pixels are the translucent system border and the corners outside the
    # window's rounding. Trim the border, flatten, and re-round below.
    edge = 2
    shot = shot.crop((edge, edge, shot.width - edge, shot.height - edge))
    flat = Image.new("RGBA", shot.size, (0, 0, 0, 255))
    flat.alpha_composite(shot)

    ratio = min(px(1440) / flat.width, px(1200) / flat.height)
    size = (round(flat.width * ratio), round(flat.height * ratio))
    flat = flat.resize(size, Image.Resampling.LANCZOS)

    radius, rim = px(13), max(1, px(2))
    flat.putalpha(rounded_mask(size, radius))
    plate_size = (size[0] + rim * 2, size[1] + rim * 2)
    plate = Image.new("RGBA", plate_size, (*RIM, 255))
    plate.putalpha(rounded_mask(plate_size, radius + rim))
    plate.alpha_composite(flat, (rim, rim))
    return plate


def drop_shadow(canvas, box, radius):
    x0, y0, x1, y1 = box
    for offset, spread, blur, alpha in ((px(46), px(6), px(56), 175), (px(10), 0, px(14), 120)):
        layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).rounded_rectangle(
            [x0 - spread, y0 + offset - spread, x1 + spread, y1 + offset + spread],
            radius=radius + spread,
            fill=(0, 0, 0, alpha),
        )
        canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur)))


def wrap(draw, text, face, max_width):
    lines = []
    for paragraph in text.split("\n"):
        line = ""
        for word in paragraph.split():
            candidate = f"{line} {word}".strip()
            if not line or draw.textlength(candidate, font=face) <= max_width:
                line = candidate
            else:
                lines.append(line)
                line = word
        lines.append(line)
    return lines


def fit_headline(draw, max_width):
    """One size for the whole set, small enough that no authored line break has to re-wrap."""
    size = px(108)
    while size > px(64):
        face = font(size, bold=True)
        widest = max(
            draw.textlength(line, font=face) for _, headline, _ in SLIDES for line in headline.split("\n")
        )
        if widest <= max_width:
            break
        size -= 2
    return font(size, bold=True)


def compose(background, raw_path, headline, subhead, output_path):
    canvas = background.copy()
    draw = ImageDraw.Draw(canvas)

    window = build_window(raw_path)
    window_x = CANVAS_W - px(110) - window.width
    window_y = (CANVAS_H - window.height) // 2
    drop_shadow(canvas, (window_x, window_y, window_x + window.width, window_y + window.height), px(15))
    canvas.alpha_composite(window, (window_x, window_y))

    left = px(130)
    text_width = window_x - left - px(84)
    eyebrow_font = font(px(30), bold=True)
    headline_font = fit_headline(draw, text_width)
    subhead_font = font(px(44))
    headline_step = round(headline_font.size * 1.13)
    subhead_step = round(subhead_font.size * 1.38)

    headline_lines = headline.split("\n")
    subhead_lines = wrap(draw, subhead, subhead_font, text_width)
    pill_h = px(62)
    block_h = (
        pill_h + px(58)
        + headline_step * len(headline_lines) + px(30)
        + subhead_step * len(subhead_lines) + px(40)
        + px(11)
    )
    # Sit a little above center: the Store may overlay its own text on the bottom third.
    y = (CANVAS_H - block_h) // 2 - px(70)

    label = "MY FIRE #"
    pill_w = round(draw.textlength(label, font=eyebrow_font)) + px(56)
    draw.rounded_rectangle([left, y, left + pill_w, y + pill_h], radius=pill_h // 2, fill=ACCENT)
    draw.text((left + pill_w // 2, y + pill_h // 2), label, font=eyebrow_font, fill=INK_BOTTOM, anchor="mm")
    y += pill_h + px(58)

    for line in headline_lines:
        draw.text((left, y), line, font=headline_font, fill=CREAM)
        y += headline_step
    y += px(30)

    for line in subhead_lines:
        draw.text((left, y), line, font=subhead_font, fill=MUTED)
        y += subhead_step
    y += px(40)

    draw.rounded_rectangle([left, y, left + px(170), y + px(11)], radius=px(6), fill=ACCENT)

    canvas.convert("RGB").save(output_path, "PNG", optimize=True)


background = build_background()
for name, headline, subhead in SLIDES:
    source = os.path.join(RAW, name)
    if not os.path.exists(source):
        raise FileNotFoundError(source)
    destination = os.path.join(OUT, name)
    compose(background, source, headline, subhead, destination)
    with Image.open(destination) as result:
        print(f"{name}: {result.width}x{result.height}")
