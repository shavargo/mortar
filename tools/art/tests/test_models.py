from pathlib import Path

from PIL import Image

from mortar_art import actors
from mortar_art import bake_geometry as geo
from mortar_art.config import REPO_ROOT

ART_ACTORS = REPO_ROOT / "unity" / "Mortar.Sample" / "Assets" / "Art" / "actors"


def test_every_actor_in_content_has_a_bake_entry():
    manifest = actors.load_manifest()

    assert sorted(actors.required_actor_ids() - set(manifest.actors)) == []


def test_human_actors_fit_inside_the_frame_with_headroom():
    """AnimatedHuman is taller than KayKit; oversized scale clips the head."""
    for actor_id in ("warrior", "smith", "healer", "sage"):
        path = ART_ACTORS / actor_id / "idle.png"
        with Image.open(path) as sheet:
            frame = sheet.convert("RGBA").crop((0, 0, geo.FRAME_WIDTH, geo.FRAME_HEIGHT))
        box = frame.getchannel("A").getbbox()
        assert box is not None, actor_id
        top, bottom = box[1], box[3]
        assert top >= 4, f"{actor_id} head is clipped (top={top})"
        assert bottom <= geo.FRAME_HEIGHT - 4, f"{actor_id} feet are clipped (bottom={bottom})"
        assert bottom - top >= geo.FRAME_HEIGHT // 2, f"{actor_id} figure is too short"


def test_committed_models_match_their_recorded_hashes():
    manifest = actors.load_manifest()

    assert actors.hash_models() == manifest.files


def test_every_model_pack_has_a_licence():
    packs = [path for path in actors.MODELS_ROOT.iterdir() if path.is_dir()]

    assert packs
    assert [pack.name for pack in packs if not (pack / actors.LICENCE_NAME).is_file()] == []
