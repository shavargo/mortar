from PIL import Image

from mortar_art import imaging

RED = (200, 0, 0, 255)
GREEN = (0, 200, 0, 255)
BLUE = (0, 0, 200, 255)
YELLOW = (200, 200, 0, 255)


def quadrants(size: int = 64) -> Image.Image:
    image = Image.new("RGBA", (size, size))
    half = size // 2
    image.paste(RED, (0, 0, half, half))
    image.paste(GREEN, (half, 0, size, half))
    image.paste(BLUE, (0, half, half, size))
    image.paste(YELLOW, (half, half, size, size))
    return image


def gradient(size: int = 256) -> Image.Image:
    image = Image.new("RGB", (size, size))
    image.putdata([(x, x, x) for _ in range(size) for x in range(size)])
    return image


def test_seamless_edges_wrap():
    result = imaging.make_seamless(gradient())

    assert abs(result.getpixel((0, 128))[0] - result.getpixel((255, 128))[0]) <= 1


def test_seamless_keeps_the_center():
    result = imaging.make_seamless(gradient())

    assert result.getpixel((128, 128)) == (128, 128, 128)


def test_diamond_places_texture_quadrants():
    diamond = imaging.project_diamond(quadrants())

    assert diamond.size == (64, 32)
    assert diamond.getpixel((32, 8))[:3] == RED[:3]
    assert diamond.getpixel((48, 16))[:3] == GREEN[:3]
    assert diamond.getpixel((16, 16))[:3] == BLUE[:3]
    assert diamond.getpixel((32, 24))[:3] == YELLOW[:3]


def test_diamond_corners_are_transparent():
    diamond = imaging.project_diamond(quadrants())

    for corner in [(0, 0), (63, 0), (0, 31), (63, 31)]:
        assert diamond.getpixel(corner)[3] == 0


def test_raised_block_shape():
    block = imaging.raised_block(Image.new("RGB", (64, 64), (120, 120, 120)))

    assert block.size == (64, 64)
    assert block.getpixel((32, 16))[3] == 255
    assert block.getpixel((8, 40))[3] == 255
    assert block.getpixel((56, 40))[3] == 255
    assert block.getpixel((4, 4))[3] == 0
    assert block.getpixel((4, 60))[3] == 0


def test_raised_block_faces_are_shaded():
    block = imaging.raised_block(Image.new("RGB", (64, 64), (120, 120, 120)))

    top = block.getpixel((32, 16))[0]
    left = block.getpixel((8, 40))[0]
    right = block.getpixel((56, 40))[0]
    assert left < right < top


def test_estimate_background_uses_corners():
    image = Image.new("RGB", (64, 64), (250, 0, 250))
    image.paste((10, 10, 10), (16, 16, 48, 48))

    assert imaging.estimate_background(image) == (250, 0, 250)


def test_chroma_key_removes_the_background():
    image = Image.new("RGB", (64, 64), (250, 0, 250))
    image.paste((90, 90, 90), (16, 16, 48, 48))

    keyed = imaging.chroma_key(image, (250, 0, 250))

    assert keyed.getpixel((2, 2))[3] == 0
    assert keyed.getpixel((32, 32))[3] == 255


def test_crop_to_content():
    image = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    image.paste(RED, (10, 20, 30, 50))

    assert imaging.crop_to_content(image).size == (20, 30)


def test_fit_bottom_anchors_with_margin():
    fitted = imaging.fit(Image.new("RGBA", (20, 40), RED), 64, 96, anchor="bottom", margin=4)

    assert fitted.size == (64, 96)
    assert fitted.getpixel((32, 91))[3] == 255
    assert fitted.getpixel((32, 93))[3] == 0
    assert fitted.getpixel((32, 3))[3] == 0


def test_outline_grows_the_silhouette():
    image = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    image.paste(RED, (6, 6, 10, 10))

    outlined = imaging.add_outline(image)

    assert outlined.getpixel((5, 7))[3] == 255
    assert outlined.getpixel((7, 7))[:3] == RED[:3]
    assert outlined.getpixel((2, 2))[3] == 0


def test_contact_sheet_lays_out_a_grid():
    entries = [(f"item-{index}", quadrants()) for index in range(7)]

    sheet = imaging.contact_sheet(entries, cell=64, columns=3)

    assert sheet.size == (3 * 64, 3 * (64 + imaging.LABEL_HEIGHT))
