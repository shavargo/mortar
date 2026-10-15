"""Build the glyph atlas for Mortar's ASCII world filter."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from mortar_art import glyphs
from mortar_art.config import REPO_ROOT


def parse_args(argv: list[str] | None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("atlas", help="render the 16-slot glyph atlas into the Unity package")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        if args.command == "atlas":
            write_atlas(glyphs.ATLAS_PATH)
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


if __name__ == "__main__":
    sys.exit(main())
