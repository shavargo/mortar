"""Turn raw generated stills into sprites sized for the Unity importer."""

from __future__ import annotations

from PIL import Image

from . import imaging
from .jobs import Job

WORK_SIZE = 256
ACTOR_WIDTH = 64
ACTOR_HEIGHT = 96
ACTOR_FOOT_MARGIN = 4
ICON_SIZE = 64
ICON_MARGIN = 2


def tile(raw: Image.Image, raised: bool) -> Image.Image:
    texture = imaging.make_seamless(raw.convert("RGB").resize((WORK_SIZE, WORK_SIZE), Image.Resampling.LANCZOS))
    return imaging.raised_block(texture, 64, 64) if raised else imaging.project_diamond(texture, 64, 32)


def actor(raw: Image.Image) -> Image.Image:
    fitted = imaging.fit(_cutout(raw), ACTOR_WIDTH, ACTOR_HEIGHT, anchor="bottom", margin=ACTOR_FOOT_MARGIN)
    return imaging.add_outline(fitted)


def icon(raw: Image.Image) -> Image.Image:
    return imaging.add_outline(imaging.fit(_cutout(raw), ICON_SIZE, ICON_SIZE, margin=ICON_MARGIN))


def for_job(job: Job, raw: Image.Image) -> Image.Image:
    if job.kind == "tiles":
        return tile(raw, job.raised)
    return actor(raw) if job.kind == "actors" else icon(raw)


def _cutout(raw: Image.Image) -> Image.Image:
    small = raw.convert("RGB").resize((WORK_SIZE, WORK_SIZE), Image.Resampling.LANCZOS)
    keyed = imaging.chroma_key(small, imaging.estimate_background(small))
    return imaging.crop_to_content(keyed)
