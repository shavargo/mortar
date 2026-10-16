"""Turn painted item icons into coloured glyph pictures with the world filter's rules."""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image

from . import glyphs
from .config import REPO_ROOT

CONTENT_ITEMS = REPO_ROOT / "content" / "items"
COLUMNS_PER_CELL = 6
ROWS_PER_CELL = 4
ICON_SCALE = 2
SOLID_ALPHA = 0.35
EDGE_THRESHOLD = 0.25
NORMALIZE = 0.5
SATURATION = 0.85

Colour = tuple[float, float, float, float]


def item_cells(items_root: Path = CONTENT_ITEMS) -> dict[str, int]:
    cells: dict[str, int] = {}
    for path in sorted(items_root.glob("*.json")):
        item = json.loads(path.read_text(encoding="utf-8"))
        size = item.get("size", {})
        cells[item["icon"]] = min(int(size.get("width", 1)), int(size.get("height", 1)))
    return cells


def grid_for(cells: int) -> tuple[int, int]:
    if cells < 1:
        raise ValueError("an icon covers at least one cell")
    return COLUMNS_PER_CELL * cells, ROWS_PER_CELL * cells


def cell_colours(icon: Image.Image, columns: int, rows: int) -> list[list[Colour]]:
    small = icon.convert("RGBA").resize((columns, rows), Image.Resampling.BOX)
    pixels = [tuple(channel / 255 for channel in pixel) for pixel in small.get_flattened_data()]
    return [pixels[row * columns : (row + 1) * columns] for row in range(rows)]


def luminance(colour: Colour) -> float:
    return sum(channel * weight for channel, weight in zip(colour[:3], glyphs.LUMA))


def choose_slots(colours: list[list[Colour]]) -> list[list[int]]:
    rows, columns = len(colours), len(colours[0])

    def ink(row: int, column: int) -> float:
        if not (0 <= row < rows and 0 <= column < columns):
            return 0.0
        return luminance(colours[row][column]) * colours[row][column][3]

    slots = []
    for row in range(rows):
        line = []
        for column in range(columns):
            colour = colours[row][column]
            if colour[3] < SOLID_ALPHA:
                line.append(0)
                continue
            gx = (ink(row, column + 1) - ink(row, column - 1)) / 2
            gy = (ink(row - 1, column) - ink(row + 1, column)) / 2
            edge = glyphs.edge_slot(gx, gy, EDGE_THRESHOLD)
            line.append(edge or glyphs.ramp_slot(luminance(colour), 0.0))
        slots.append(line)
    return slots


def tint(colour: Colour) -> tuple[int, int, int]:
    peak = max(colour[0], colour[1], colour[2], 1e-4)
    mixed = [channel + (channel / peak - channel) * NORMALIZE for channel in colour[:3]]
    grey = sum(channel * weight for channel, weight in zip(mixed, glyphs.LUMA))
    red, green, blue = (round(255 * min(1.0, grey + (channel - grey) * SATURATION)) for channel in mixed)
    return red, green, blue


def convert_icon(icon: Image.Image, cells: int, glyph_images: list[Image.Image]) -> Image.Image:
    columns, rows = grid_for(cells)
    colours = cell_colours(icon, columns, rows)
    slots = choose_slots(colours)
    width, height = glyph_images[0].size
    picture = Image.new("RGBA", (columns * width, rows * height), (0, 0, 0, 0))
    for row in range(rows):
        for column in range(columns):
            slot = slots[row][column]
            if slot == 0:
                continue
            glyph = Image.new("RGBA", (width, height), (*tint(colours[row][column]), 255))
            glyph.putalpha(glyph_images[slot])
            picture.paste(glyph, (column * width, row * height))
    return picture


def convert_all(source: Path, target: Path, cells: dict[str, int]) -> list[Path]:
    chars = glyphs.glyph_chars(glyphs.load_font())
    font = glyphs.load_font(ICON_SCALE)
    glyph_images = [glyphs.render_glyph(char, font, ICON_SCALE) for char in chars]
    target.mkdir(parents=True, exist_ok=True)
    written = []
    for path in sorted(source.glob("*.png")):
        with Image.open(path) as icon:
            picture = convert_icon(icon, cells.get(path.stem, 1), glyph_images)
        output = target / path.name
        picture.save(output)
        written.append(output)
    return written
