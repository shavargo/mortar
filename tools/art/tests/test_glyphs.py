import re

import pytest

from mortar_art import glyphs
from mortar_art.config import REPO_ROOT

SETTINGS_PATH = REPO_ROOT / "unity" / "com.backwoods.mortar" / "Runtime" / "Ascii" / "AsciiSettings.cs"


@pytest.fixture(scope="module")
def font():
    return glyphs.load_font()


def test_a_glyph_fills_one_cell(font):
    assert glyphs.render_glyph("@", font).size == (glyphs.CELL_WIDTH, glyphs.CELL_HEIGHT)
    assert glyphs.render_glyph("@", glyphs.load_font(2), 2).size == (2 * glyphs.CELL_WIDTH, 2 * glyphs.CELL_HEIGHT)


def test_space_has_no_ink_and_at_sign_has_plenty(font):
    assert glyphs.coverage(glyphs.render_glyph(" ", font)) == 0.0
    assert glyphs.coverage(glyphs.render_glyph("@", font)) > 0.3


def test_the_ramp_is_ordered_by_ink_coverage(font):
    chars = glyphs.glyph_chars(font)
    ramp = chars[: glyphs.RAMP_SIZE]
    inks = [glyphs.coverage(glyphs.render_glyph(char, font)) for char in ramp]

    assert ramp[0] == " "
    assert len(set(ramp)) == glyphs.RAMP_SIZE
    assert inks == sorted(inks)
    assert inks[-1] > 0.3


def test_the_last_four_slots_are_the_edge_glyphs(font):
    chars = glyphs.glyph_chars(font)

    assert len(chars) == glyphs.SLOTS
    assert chars[glyphs.FIRST_EDGE_SLOT :] == ["-", "/", "|", "\\"]


def test_pick_ramp_spreads_choices_across_coverage():
    coverages = {" ": 0.0, **{chr(97 + index): index / 20 for index in range(1, 21)}}

    ramp = glyphs.pick_ramp(coverages, size=5)

    assert ramp == [" ", "f", "k", "p", "u"]


def test_pick_ramp_needs_a_space_and_enough_candidates():
    with pytest.raises(ValueError, match="space"):
        glyphs.pick_ramp({"a": 0.1, "b": 0.2}, size=2)


def test_the_atlas_is_sixteen_cells_wide_with_a_blank_first_slot(font):
    atlas = glyphs.build_atlas(glyphs.glyph_chars(font), font)
    alpha = atlas.getchannel("A")

    assert atlas.size == (glyphs.CELL_WIDTH * glyphs.SLOTS, glyphs.CELL_HEIGHT)
    assert alpha.crop((0, 0, glyphs.CELL_WIDTH, glyphs.CELL_HEIGHT)).getbbox() is None
    assert atlas.getpixel((glyphs.CELL_WIDTH * 11 + 3, 5))[:3] == (255, 255, 255)


def test_the_bar_glyph_is_a_vertical_line(font):
    atlas = glyphs.build_atlas(glyphs.glyph_chars(font), font)
    left = glyphs.CELL_WIDTH * (glyphs.FIRST_EDGE_SLOT + 2)
    cell = atlas.getchannel("A").crop((left, 0, left + glyphs.CELL_WIDTH, glyphs.CELL_HEIGHT))
    columns = [sum(cell.getpixel((x, y)) for y in range(glyphs.CELL_HEIGHT)) for x in range(glyphs.CELL_WIDTH)]

    assert (columns[2] + columns[3]) > 0.6 * sum(columns)


def test_the_atlas_is_deterministic(font):
    chars = glyphs.glyph_chars(font)

    assert glyphs.build_atlas(chars, font).tobytes() == glyphs.build_atlas(chars, font).tobytes()


def test_an_atlas_needs_sixteen_glyphs(font):
    with pytest.raises(ValueError, match="16 glyphs"):
        glyphs.build_atlas([" ", "@"], font)


def test_a_missing_font_is_a_clear_error(tmp_path):
    with pytest.raises(FileNotFoundError, match="font not found"):
        glyphs.load_font(path=tmp_path / "missing.ttf")


@pytest.mark.parametrize(
    ("luminance", "expected"),
    [(0.0, 0), (0.059, 0), (0.06, 1), (0.5, 6), (0.99, 11), (1.0, 11)],
)
def test_ramp_slots_follow_brightness_above_the_dark_threshold(luminance, expected):
    assert glyphs.ramp_slot(luminance, 0.06) == expected


@pytest.mark.parametrize(
    ("gx", "gy", "expected"),
    [(-0.5, 0.0, 14), (0.5, 0.0, 14), (0.0, -0.5, 12), (-0.4, -0.4, 15), (0.4, -0.4, 13), (0.1, 0.1, 0)],
)
def test_edge_slots_run_across_the_gradient(gx, gy, expected):
    assert glyphs.edge_slot(gx, gy, 0.25) == expected


def test_slot_constants_match_the_unity_settings():
    if not SETTINGS_PATH.is_file():
        pytest.skip("AsciiSettings.cs arrives in Task 2")
    text = SETTINGS_PATH.read_text(encoding="utf-8")

    def constant(name: str) -> int:
        match = re.search(rf"public const int {name} = (\d+);", text)
        assert match, f"{name} not found in AsciiSettings.cs"
        return int(match.group(1))

    assert constant("CellWidth") == glyphs.CELL_WIDTH
    assert constant("CellHeight") == glyphs.CELL_HEIGHT
    assert constant("Slots") == glyphs.SLOTS
    assert constant("RampSize") == glyphs.RAMP_SIZE
    assert constant("FirstEdgeSlot") == glyphs.FIRST_EDGE_SLOT
