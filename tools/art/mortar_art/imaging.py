"""Image operations that turn generated stills into isometric sprites."""

from __future__ import annotations

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter

TRANSPARENT = (0, 0, 0, 0)
LEFT_FACE_BRIGHTNESS = 0.55
RIGHT_FACE_BRIGHTNESS = 0.75
LABEL_HEIGHT = 16


def make_seamless(image: Image.Image, blend: int = 48) -> Image.Image:
    rgb = image.convert("RGB")
    width, height = rgb.size
    shifted = ImageChops.offset(rgb, width // 2, height // 2)
    return Image.composite(rgb, shifted, _edge_mask(width, height, blend))


def project_diamond(texture: Image.Image, width: int = 64, height: int = 32) -> Image.Image:
    if width != height * 2:
        raise ValueError("diamonds must be twice as wide as they are tall")
    square = _square(texture, width)
    half = width / 2
    diamond = _affine(square, (width, height), (1, 2, -half, -1, 2, half))
    mask = Image.new("L", (width, height), 0)
    ImageDraw.Draw(mask).polygon(
        [(half, 0), (width - 1, height / 2), (half, height - 1), (0, height / 2)], fill=255
    )
    diamond.putalpha(ImageChops.multiply(diamond.getchannel("A"), mask))
    return diamond


def raised_block(texture: Image.Image, width: int = 64, height: int = 64) -> Image.Image:
    if width != height:
        raise ValueError("raised blocks must be square")
    square = _square(texture, width)
    left = _affine(square, (width, height), (2, 0, 0, -1, 2, -width / 2))
    right = _affine(square, (width, height), (2, 0, -width, 1, 2, -1.5 * width))
    block = Image.new("RGBA", (width, height), TRANSPARENT)
    block.alpha_composite(_shade(left, LEFT_FACE_BRIGHTNESS))
    block.alpha_composite(_shade(right, RIGHT_FACE_BRIGHTNESS))
    block.alpha_composite(project_diamond(square, width, width // 2))
    return block


def estimate_background(image: Image.Image, patch: int = 8) -> tuple[int, ...]:
    rgb = image.convert("RGB")
    width, height = rgb.size
    boxes = [
        (0, 0, patch, patch),
        (width - patch, 0, width, patch),
        (0, height - patch, patch, height),
        (width - patch, height - patch, width, height),
    ]
    pixels = [pixel for box in boxes for pixel in rgb.crop(box).get_flattened_data()]
    return tuple(sum(pixel[channel] for pixel in pixels) // len(pixels) for channel in range(3))


def chroma_key(
    image: Image.Image, key: tuple[int, ...], tolerance: float = 80.0, softness: float = 0.5
) -> Image.Image:
    rgba = image.convert("RGBA")
    solid = tolerance * (1 + softness)
    alphas = []
    for red, green, blue, alpha in rgba.get_flattened_data():
        distance = ((red - key[0]) ** 2 + (green - key[1]) ** 2 + (blue - key[2]) ** 2) ** 0.5
        if distance <= tolerance:
            alphas.append(0)
        elif distance >= solid:
            alphas.append(alpha)
        else:
            alphas.append(int(alpha * (distance - tolerance) / (solid - tolerance)))
    mask = Image.new("L", rgba.size)
    mask.putdata(alphas)
    rgba.putalpha(mask)
    return rgba


def crop_to_content(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    box = rgba.getchannel("A").getbbox()
    return rgba.crop(box) if box else rgba


def fit(
    image: Image.Image, width: int, height: int, anchor: str = "center", margin: int = 1
) -> Image.Image:
    if anchor not in ("center", "bottom"):
        raise ValueError(f"unknown anchor '{anchor}'")
    rgba = image.convert("RGBA")
    scale = min((width - 2 * margin) / rgba.width, (height - 2 * margin) / rgba.height)
    size = (max(1, round(rgba.width * scale)), max(1, round(rgba.height * scale)))
    resized = rgba.resize(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (width, height), TRANSPARENT)
    x = (width - size[0]) // 2
    y = height - margin - size[1] if anchor == "bottom" else (height - size[1]) // 2
    canvas.alpha_composite(resized, (x, y))
    return canvas


def add_outline(image: Image.Image, color: tuple[int, int, int, int] = (20, 14, 10, 255)) -> Image.Image:
    rgba = image.convert("RGBA")
    solid = rgba.getchannel("A").point(lambda alpha: 255 if alpha > 32 else 0)
    outline = Image.new("RGBA", rgba.size, color)
    outline.putalpha(solid.filter(ImageFilter.MaxFilter(3)))
    outline.alpha_composite(rgba)
    return outline


def contact_sheet(
    entries: list[tuple[str, Image.Image]], cell: int = 128, columns: int = 6
) -> Image.Image:
    rows = max(1, -(-len(entries) // columns))
    sheet = Image.new("RGB", (columns * cell, rows * (cell + LABEL_HEIGHT)), (40, 40, 40))
    draw = ImageDraw.Draw(sheet)
    for index, (label, image) in enumerate(entries):
        x = (index % columns) * cell
        y = (index // columns) * (cell + LABEL_HEIGHT)
        thumbnail = fit(image, cell, cell, margin=4)
        sheet.paste(thumbnail, (x, y), thumbnail)
        draw.text((x + 4, y + cell + 2), label[-24:], fill=(220, 220, 220))
    return sheet


def _square(texture: Image.Image, size: int) -> Image.Image:
    return texture.convert("RGBA").resize((size, size), Image.Resampling.LANCZOS)


def _affine(image: Image.Image, size: tuple[int, int], data: tuple[float, ...]) -> Image.Image:
    return image.transform(
        size, Image.Transform.AFFINE, data, resample=Image.Resampling.BILINEAR, fillcolor=TRANSPARENT
    )


def _shade(face: Image.Image, brightness: float) -> Image.Image:
    shaded = ImageEnhance.Brightness(face.convert("RGB")).enhance(brightness)
    shaded.putalpha(face.getchannel("A"))
    return shaded


def _edge_mask(width: int, height: int, blend: int) -> Image.Image:
    mask = Image.new("L", (width, height))
    mask.putdata(
        [
            min(255, 255 * min(x, y, width - 1 - x, height - 1 - y) // blend)
            for y in range(height)
            for x in range(width)
        ]
    )
    return mask
