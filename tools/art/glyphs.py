"""Build the glyph atlas for Mortar's ASCII world filter and glyph versions of the item icons."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

from mortar_art import glyph_icons, glyphs
from mortar_art.config import REPO_ROOT
from mortar_art.imaging import contact_sheet
from mortar_art.jobs import ART_ROOT

SHEET_PATH = REPO_ROOT / "tools" / "art" / "out" / "sheets" / "glyph-icons.png"


def parse_args(argv: list[str] | None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("atlas", help="render the 16-slot glyph atlas into the Unity package")
    commands.add_parser("icons", help="convert Assets/Art/icons into Assets/Art/glyph-icons")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        if args.command == "atlas":
            write_atlas(glyphs.ATLAS_PATH)
        else:
            write_icons(ART_ROOT, SHEET_PATH)
    except (OSError, ValueError) as error:
        print(f"[ERROR] {error}", file=sys.stderr)
        return 1
    return 0


def write_atlas(path: Path) -> None:
    font = glyphs.load_font()
    chars = glyphs.glyph_chars(font)
    path.parent.mkdir(parents=True, exist_ok=True)
    glyphs.build_atlas(chars, font).save(path)
    print(f"[GLYPHS] Ramp {''.join(chars[: glyphs.RAMP_SIZE])!r}")
    print(f"[GLYPHS] Wrote {path.relative_to(REPO_ROOT).as_posix()}")


def write_icons(art_root: Path, sheet_path: Path) -> None:
    written = glyph_icons.convert_all(art_root / "icons", art_root / "glyph-icons", glyph_icons.item_cells())
    for path in written:
        print(f"[GLYPHS] Wrote {path.relative_to(REPO_ROOT).as_posix()}")
    if not written:
        print("[GLYPHS] No icons to convert")
        return
    entries = []
    for path in written:
        with Image.open(path) as picture:
            entries.append((path.stem, picture.copy()))
    sheet_path.parent.mkdir(parents=True, exist_ok=True)
    contact_sheet(entries).save(sheet_path)
    print(f"[GLYPHS] Wrote {sheet_path.relative_to(REPO_ROOT).as_posix()}")


if __name__ == "__main__":
    sys.exit(main())
