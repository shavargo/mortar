import json

import pytest
from PIL import Image

from mortar_art import glyph_icons, glyphs

WHITE = (1.0, 1.0, 1.0, 1.0)
CLEAR = (0.0, 0.0, 0.0, 0.0)


@pytest.fixture(scope="module")
def glyph_images():
    chars = glyphs.glyph_chars(glyphs.load_font())
    font = glyphs.load_font(glyph_icons.ICON_SCALE)
    return [glyphs.render_glyph(char, font, glyph_icons.ICON_SCALE) for char in chars]


def test_each_covered_cell_gets_six_by_four_characters():
    assert glyph_icons.grid_for(1) == (6, 4)
    assert glyph_icons.grid_for(2) == (12, 8)


def test_an_icon_covers_at_least_one_cell():
    with pytest.raises(ValueError, match="at least one cell"):
        glyph_icons.grid_for(0)


def test_icons_cover_the_smaller_side_of_their_item(tmp_path):
    (tmp_path / "blade.json").write_text(json.dumps({"icon": "blade", "size": {"width": 1, "height": 3}}))
    (tmp_path / "vest.json").write_text(json.dumps({"icon": "vest", "size": {"width": 2, "height": 3}}))

    assert glyph_icons.item_cells(tmp_path) == {"blade": 1, "vest": 2}


def test_the_real_items_all_have_cells():
    cells = glyph_icons.item_cells()

    assert cells["broad-blade"] == 1
    assert all(value >= 1 for value in cells.values())


def test_a_converted_icon_matches_its_grid_size(glyph_images):
    icon = Image.new("RGBA", (64, 64), (200, 40, 40, 255))

    picture = glyph_icons.convert_icon(icon, 1, glyph_images)

    assert picture.size == (6 * 12, 4 * 20)


def test_a_transparent_icon_converts_to_nothing(glyph_images):
    picture = glyph_icons.convert_icon(Image.new("RGBA", (64, 64), (0, 0, 0, 0)), 1, glyph_images)

    assert picture.getchannel("A").getbbox() is None


def test_glyphs_keep_the_icon_colour_on_a_clear_background(glyph_images):
    picture = glyph_icons.convert_icon(Image.new("RGBA", (64, 64), (220, 60, 30, 255)), 1, glyph_images)
    inked = [pixel for pixel in picture.get_flattened_data() if pixel[3] > 200]

    assert inked
    assert all(red > green > blue for red, green, blue, _ in inked)
    assert any(pixel[3] == 0 for pixel in picture.get_flattened_data())


def test_conversion_is_deterministic(glyph_images):
    icon = Image.new("RGBA", (64, 64), (90, 160, 220, 255))

    first = glyph_icons.convert_icon(icon, 1, glyph_images).tobytes()

    assert glyph_icons.convert_icon(icon, 1, glyph_images).tobytes() == first


def test_an_opaque_edge_beside_clear_space_draws_a_bar():
    colours = [[WHITE, WHITE, WHITE, CLEAR, CLEAR, CLEAR] for _ in range(4)]

    slots = glyph_icons.choose_slots(colours)

    assert slots[1][2] == glyphs.FIRST_EDGE_SLOT + 2
    assert slots[1][1] == glyphs.RAMP_SIZE - 1
    assert slots[1][3] == 0


def test_tint_keeps_hue_and_lifts_dim_colours():
    red, green, blue = glyph_icons.tint((0.4, 0.2, 0.1, 1.0))

    assert red > green > blue
    assert red > round(0.4 * 255)
