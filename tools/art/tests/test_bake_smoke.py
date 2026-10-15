import os

import pytest
from PIL import Image

from mortar_art import actors, bake_runner
from mortar_art import bake_geometry as geo
from mortar_art.blender import BlenderError, find_blender


def _blender():
    try:
        return find_blender(os.environ)
    except BlenderError:
        return None


BLENDER = _blender()


@pytest.mark.skipif(BLENDER is None, reason="Blender is not installed")
def test_the_warrior_idle_bakes_with_real_transparency_and_feet_on_the_pivot(tmp_path):
    manifest = actors.load_manifest()
    warrior = manifest.actors["warrior"]
    actors.check_files(manifest, warrior)
    job = actors.build_job(warrior, tmp_path, animations=("idle",), directions=(0,), max_frames=1)
    job["hide"] = [*job["hide"], "1H_Sword", "Badge_Shield"]

    bake_runner.run_job(BLENDER, job, tmp_path)

    with Image.open(tmp_path / "idle" / "0" / "00.png") as image:
        frame = image.convert("RGBA")
    width, height = frame.size
    alpha = frame.getchannel("A")
    assert (width, height) == geo.render_size()
    for corner in ((0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1)):
        assert alpha.getpixel(corner) == 0
    box = alpha.getbbox()
    assert box is not None
    feet_row = height - geo.PIVOT_FROM_BOTTOM * geo.RENDER_SCALE
    assert feet_row + 4 * geo.RENDER_SCALE <= box[3] <= feet_row + 14 * geo.RENDER_SCALE
