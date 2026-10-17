"""Monospace glyph rendering shared by the world filter's atlas and the glyph icons.

The slot rules must match `GlyphRules` in the Unity package: Unity draws the world with them and
this module draws the item icons with them.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from .config import REPO_ROOT

RESOURCES_ROOT = REPO_ROOT / "unity" / "com.mortar.unity" / "Runtime" / "Resources" / "Mortar"
FONT_PATH = RESOURCES_ROOT / "DejaVuSansMono.ttf"
ATLAS_PATH = RESOURCES_ROOT / "GlyphAtlas.png"

CELL_WIDTH = 6
CELL_HEIGHT = 10
SLOTS = 16
RAMP_SIZE = 12
FIRST_EDGE_SLOT = 12
EDGE_CHARS = ("-", "/", "|", "\\")
RAMP_CANDIDATES = " .`',:;-~^\"=+<>*!ilcoxe%#&$@MW"
LUMA = (0.299, 0.587, 0.114)
SUPERSAMPLE = 4
FONT_SIZE = 10


def load_font(scale: int = 1, path: Path | None = None) -> ImageFont.FreeTypeFont:
    font_path = path or FONT_PATH
    if not font_path.is_file():
        raise FileNotFoundError(f"font not found: {font_path}")
    return ImageFont.truetype(str(font_path), FONT_SIZE * SUPERSAMPLE * scale)


def render_glyph(char: str, font: ImageFont.FreeTypeFont, scale: int = 1) -> Image.Image:
    width, height = CELL_WIDTH * scale, CELL_HEIGHT * scale
    big = Image.new("L", (width * SUPERSAMPLE, height * SUPERSAMPLE), 0)
    ImageDraw.Draw(big).text((big.width / 2, big.height / 2), char, fill=255, font=font, anchor="mm")
    return big.resize((width, height), Image.Resampling.BOX)


def coverage(glyph: Image.Image) -> float:
    pixels = list(glyph.get_flattened_data())
    return sum(pixels) / (255 * len(pixels))


def pick_ramp(coverages: dict[str, float], size: int = RAMP_SIZE) -> list[str]:
    if " " not in coverages or len(coverages) < size:
        raise ValueError(f"a ramp needs a space and at least {size} candidates")
    remaining = sorted((value, char) for char, value in coverages.items() if char != " ")
    densest = remaining[-1][0]
    ramp = [" "]
    for step in range(1, size):
        target = densest * step / (size - 1)
        best = min(remaining, key=lambda item: (abs(item[0] - target), item[1]))
        remaining.remove(best)
        ramp.append(best[1])
    return sorted(ramp, key=lambda char: (coverages[char], char))


def glyph_chars(font: ImageFont.FreeTypeFont) -> list[str]:
    candidates = dict.fromkeys(RAMP_CANDIDATES)
    coverages = {char: coverage(render_glyph(char, font)) for char in candidates}
    return pick_ramp(coverages) + list(EDGE_CHARS)


def build_atlas(chars: list[str], font: ImageFont.FreeTypeFont, scale: int = 1) -> Image.Image:
    if len(chars) != SLOTS:
        raise ValueError(f"an atlas needs {SLOTS} glyphs, got {len(chars)}")
    width = CELL_WIDTH * scale
    atlas = Image.new("RGBA", (width * SLOTS, CELL_HEIGHT * scale), (255, 255, 255, 0))
    for slot, char in enumerate(chars):
        glyph = render_glyph(char, font, scale)
        white = Image.new("RGBA", glyph.size, (255, 255, 255, 0))
        white.putalpha(glyph)
        atlas.paste(white, (slot * width, 0))
    return atlas


def ramp_slot(luminance: float, dark_threshold: float) -> int:
    if luminance < dark_threshold:
        return 0
    span = (luminance - dark_threshold) / max(1e-6, 1.0 - dark_threshold)
    return min(RAMP_SIZE - 1, 1 + int(span * (RAMP_SIZE - 1)))


def edge_slot(gx: float, gy: float, threshold: float) -> int:
    if math.hypot(gx, gy) < threshold:
        return 0
    angle = (math.degrees(math.atan2(gy, gx)) + 90.0) % 180.0
    return FIRST_EDGE_SLOT + round(angle / 45.0) % 4
