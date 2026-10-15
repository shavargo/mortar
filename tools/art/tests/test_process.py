from PIL import Image

from mortar_art import process


def figure_on_magenta():
    image = Image.new("RGB", (512, 512), (250, 0, 250))
    image.paste((90, 80, 70), (200, 100, 312, 450))
    return image


def test_tiles_have_flat_and_raised_sizes():
    texture = Image.new("RGB", (512, 512), (100, 90, 80))

    assert process.tile(texture, raised=False).size == (64, 32)
    assert process.tile(texture, raised=True).size == (64, 64)


def test_actors_are_cut_out_and_stand_on_the_pivot():
    sprite = process.actor(figure_on_magenta())

    assert sprite.size == (64, 96)
    assert sprite.getpixel((0, 0))[3] == 0
    assert sprite.getpixel((32, 90))[3] == 255
    assert all(sprite.getpixel((x, 94))[3] == 0 for x in range(64))


def test_icons_are_square_and_centered():
    sprite = process.icon(figure_on_magenta())

    assert sprite.size == (64, 64)
    assert sprite.getpixel((32, 32))[3] == 255
    assert sprite.getpixel((0, 0))[3] == 0
