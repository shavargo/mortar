import json

import pytest
from PIL import Image

from mortar_art import bake_geometry as geo
from mortar_art import sheets

CLEAR = (0, 0, 0, 0)


def render(color=CLEAR):
    return Image.new("RGBA", geo.render_size(), color)


def frame(color):
    return Image.new("RGBA", (geo.FRAME_WIDTH, geo.FRAME_HEIGHT), color)


def test_finishing_halves_the_render():
    finished = sheets.finish_frame(render((10, 20, 30, 255)))

    assert finished.size == (geo.FRAME_WIDTH, geo.FRAME_HEIGHT)
    assert finished.getpixel((5, 5)) == (10, 20, 30, 255)


def test_finishing_keeps_edge_colours_instead_of_darkening_them():
    source = render()
    source.putpixel((0, 0), (255, 0, 0, 255))
    source.putpixel((0, 1), (255, 0, 0, 255))

    red, green, blue, alpha = sheets.finish_frame(source).getpixel((0, 0))

    assert alpha == pytest.approx(128, abs=1)
    assert red >= 250 and green == 0 and blue == 0


def test_finishing_rejects_the_wrong_render_size():
    with pytest.raises(ValueError, match="256x384"):
        sheets.finish_frame(Image.new("RGBA", (100, 100)))


def test_a_tint_scales_colour_and_keeps_alpha():
    tinted = sheets.apply_tint(frame((200, 100, 50, 128)), (0.5, 2.0, 1.0))

    assert tinted.getpixel((0, 0)) == (100, 200, 50, 128)


def test_a_sheet_has_a_row_per_direction_and_a_column_per_frame():
    rows = [[frame((255, 0, 0, 255)), frame((0, 255, 0, 255))], [frame((0, 0, 255, 255)), frame((9, 9, 9, 255))]]

    sheet = sheets.pack_sheet(rows)

    assert sheet.size == (2 * geo.FRAME_WIDTH, 2 * geo.FRAME_HEIGHT)
    assert sheet.getpixel((geo.FRAME_WIDTH + 1, 1)) == (0, 255, 0, 255)
    assert sheet.getpixel((1, geo.FRAME_HEIGHT + 1)) == (0, 0, 255, 255)


def test_rows_must_have_the_same_length():
    with pytest.raises(ValueError, match="same number of frames"):
        sheets.pack_sheet([[frame(CLEAR)], [frame(CLEAR), frame(CLEAR)]])


def test_sheet_info_matches_the_unity_reader():
    assert sheets.sheet_info(6, 8, False, 3) == {
        "frames": 6,
        "directions": 8,
        "fps": geo.SAMPLE_FPS,
        "loop": False,
        "hitFrame": 3,
        "frameWidth": geo.FRAME_WIDTH,
        "frameHeight": geo.FRAME_HEIGHT,
        "pivotX": geo.PIVOT_X,
        "pivotY": geo.PIVOT_FROM_BOTTOM,
    }


def _fake_bake(bake_dir, directions, animations):
    bake_dir.mkdir(parents=True)
    summary = {"directions": directions, "animations": {}}
    for name, (count, hit) in animations.items():
        summary["animations"][name] = {"frames": count, "hitFrame": hit}
        for direction in directions:
            for index in range(count):
                path = bake_dir / name / str(direction) / f"{index:02d}.png"
                path.parent.mkdir(parents=True, exist_ok=True)
                render((direction * 40, index * 40, 0, 255)).save(path)
    (bake_dir / "frames.json").write_text(json.dumps(summary), encoding="utf-8")


def test_packing_an_actor_writes_a_sheet_and_sidecar_per_animation(tmp_path):
    _fake_bake(tmp_path / "bake", [0, 1], {"idle": (2, 0), "attack": (3, 1)})

    written = sheets.pack_actor(tmp_path / "bake", tmp_path / "art", (1.0, 1.0, 1.0))

    assert sorted(path.name for path in written) == ["attack.png", "idle.png"]
    with Image.open(tmp_path / "art" / "attack.png") as sheet:
        assert sheet.size == (3 * geo.FRAME_WIDTH, 2 * geo.FRAME_HEIGHT)
        assert sheet.getpixel((2 * geo.FRAME_WIDTH + 1, geo.FRAME_HEIGHT + 1)) == (40, 80, 0, 255)
    attack = json.loads((tmp_path / "art" / "attack.json").read_text(encoding="utf-8"))
    idle = json.loads((tmp_path / "art" / "idle.json").read_text(encoding="utf-8"))
    assert (attack["loop"], attack["hitFrame"], attack["directions"]) == (False, 1, 2)
    assert idle["loop"] is True


def test_review_shows_idle_and_the_attack_hit_frame_for_each_direction(tmp_path):
    _fake_bake(tmp_path / "bake", [0, 1], {"idle": (2, 0), "attack": (3, 2)})
    sheets.pack_actor(tmp_path / "bake", tmp_path / "art", (1.0, 1.0, 1.0))

    entries = sheets.review_entries("warrior", tmp_path / "art")

    assert [label for label, _ in entries] == [
        "warrior idle 0", "warrior idle 1", "warrior attack 0", "warrior attack 1",
    ]
    assert entries[3][1].getpixel((1, 1)) == (40, 80, 0, 255)


def test_writing_the_review_saves_a_contact_sheet(tmp_path):
    path = tmp_path / "review.png"

    sheets.write_review([("warrior idle 0", frame((255, 0, 0, 255)))], path)

    assert path.is_file()
