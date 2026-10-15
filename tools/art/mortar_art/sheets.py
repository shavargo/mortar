"""Turn baked frames into per-animation sprite sheets with JSON metadata for Unity."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from PIL import Image

from . import bake_geometry as geo
from .imaging import contact_sheet

Tint = tuple[float, float, float]
NO_TINT: Tint = (1.0, 1.0, 1.0)
REVIEW_ANIMATIONS = ("idle", "attack")


def finish_frame(render: Image.Image, tint: Tint = NO_TINT) -> Image.Image:
    expected = geo.render_size()
    if render.size != expected:
        raise ValueError(f"expected a {expected[0]}x{expected[1]} render, got {render.width}x{render.height}")
    premultiplied = render.convert("RGBA").convert("RGBa")
    small = premultiplied.resize((geo.FRAME_WIDTH, geo.FRAME_HEIGHT), Image.Resampling.BOX)
    return apply_tint(small.convert("RGBA"), tint)


def apply_tint(frame: Image.Image, tint: Tint) -> Image.Image:
    if tint == NO_TINT:
        return frame
    red, green, blue, alpha = frame.convert("RGBA").split()
    scaled = [
        band.point(lambda value, factor=factor: min(255, round(value * factor)))
        for band, factor in zip((red, green, blue), tint)
    ]
    return Image.merge("RGBA", (*scaled, alpha))


def pack_sheet(rows: list[list[Image.Image]]) -> Image.Image:
    if not rows or not rows[0]:
        raise ValueError("a sheet needs at least one frame")
    count = len(rows[0])
    if any(len(row) != count for row in rows):
        raise ValueError("every direction needs the same number of frames")
    sheet = Image.new("RGBA", (count * geo.FRAME_WIDTH, len(rows) * geo.FRAME_HEIGHT), (0, 0, 0, 0))
    for row_index, row in enumerate(rows):
        for column, frame in enumerate(row):
            if frame.size != (geo.FRAME_WIDTH, geo.FRAME_HEIGHT):
                raise ValueError(f"frames must be {geo.FRAME_WIDTH}x{geo.FRAME_HEIGHT}")
            sheet.paste(frame, (column * geo.FRAME_WIDTH, row_index * geo.FRAME_HEIGHT))
    return sheet


def sheet_info(frames: int, directions: int, loop: bool, hit_frame: int) -> dict[str, Any]:
    return {
        "frames": frames,
        "directions": directions,
        "fps": geo.SAMPLE_FPS,
        "loop": loop,
        "hitFrame": hit_frame,
        "frameWidth": geo.FRAME_WIDTH,
        "frameHeight": geo.FRAME_HEIGHT,
        "pivotX": geo.PIVOT_X,
        "pivotY": geo.PIVOT_FROM_BOTTOM,
    }


def write_sheet(sheet: Image.Image, info: dict[str, Any], png_path: Path) -> None:
    png_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(png_path)
    png_path.with_suffix(".json").write_text(json.dumps(info, indent=2) + "\n", encoding="utf-8")


def pack_actor(bake_dir: Path, art_dir: Path, tint: Tint) -> list[Path]:
    summary = json.loads((bake_dir / "frames.json").read_text(encoding="utf-8"))
    written = []
    for animation, meta in summary["animations"].items():
        rows = [
            [finish_frame(_load(bake_dir / animation / str(direction) / f"{index:02d}.png"), tint)
             for index in range(int(meta["frames"]))]
            for direction in summary["directions"]
        ]
        path = art_dir / f"{animation}.png"
        info = sheet_info(int(meta["frames"]), len(rows), animation in geo.LOOPING, int(meta["hitFrame"]))
        write_sheet(pack_sheet(rows), info, path)
        written.append(path)
    return written


def frame_at(sheet: Image.Image, row: int, index: int) -> Image.Image:
    left, top = index * geo.FRAME_WIDTH, row * geo.FRAME_HEIGHT
    return sheet.crop((left, top, left + geo.FRAME_WIDTH, top + geo.FRAME_HEIGHT))


def review_entries(actor_id: str, art_dir: Path) -> list[tuple[str, Image.Image]]:
    entries = []
    for animation in REVIEW_ANIMATIONS:
        png = art_dir / f"{animation}.png"
        if not png.is_file():
            continue
        info = json.loads(png.with_suffix(".json").read_text(encoding="utf-8"))
        index = int(info["hitFrame"]) if animation == "attack" else 0
        sheet = _load(png)
        for row in range(int(info["directions"])):
            entries.append((f"{actor_id} {animation} {row}", frame_at(sheet, row, index)))
    return entries


def write_review(entries: list[tuple[str, Image.Image]], path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    contact_sheet(entries, cell=geo.FRAME_WIDTH, columns=8).save(path)


def _load(path: Path) -> Image.Image:
    with Image.open(path) as image:
        return image.convert("RGBA")
