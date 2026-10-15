"""Locate the Blender executable used for the actor bake."""

from __future__ import annotations

from collections.abc import Mapping
from pathlib import Path

PINNED_VERSION = "4.5"
VENDOR_FOLDER = "Blender Foundation"


class BlenderError(Exception):
    pass


def find_blender(environ: Mapping[str, str]) -> Path:
    override = environ.get("BLENDER", "").strip()
    if override:
        path = Path(override)
        if not path.is_file():
            raise BlenderError(f"BLENDER is set to {path}, which is not a file")
        return path
    program_files = environ.get("ProgramFiles", "").strip()
    if not program_files:
        raise BlenderError(_not_found("no ProgramFiles folder"))
    vendor = Path(program_files) / VENDOR_FOLDER
    pinned = vendor / f"Blender {PINNED_VERSION}" / "blender.exe"
    if pinned.is_file():
        return pinned
    found = sorted(path.parent.name for path in vendor.glob("Blender */blender.exe"))
    raise BlenderError(_not_found(f"found {', '.join(found)}" if found else f"nothing under {vendor}"))


def _not_found(detail: str) -> str:
    return (
        f"Blender {PINNED_VERSION} not found ({detail}); install it with "
        f"'winget install BlenderFoundation.Blender.LTS.{PINNED_VERSION}' or set BLENDER to blender.exe"
    )
