from PIL import Image

import glyphs as cli
from mortar_art import glyphs


def test_atlas_writes_the_atlas(tmp_path, monkeypatch, capsys):
    target = tmp_path / "GlyphAtlas.png"
    monkeypatch.setattr(glyphs, "ATLAS_PATH", target)
    monkeypatch.setattr(cli, "REPO_ROOT", tmp_path)

    assert cli.main(["atlas"]) == 0

    with Image.open(target) as atlas:
        assert atlas.size == (glyphs.CELL_WIDTH * glyphs.SLOTS, glyphs.CELL_HEIGHT)
    assert "[GLYPHS] Wrote GlyphAtlas.png" in capsys.readouterr().out


def test_a_missing_font_fails_with_an_error(tmp_path, monkeypatch, capsys):
    monkeypatch.setattr(glyphs, "FONT_PATH", tmp_path / "missing.ttf")
    monkeypatch.setattr(glyphs, "ATLAS_PATH", tmp_path / "GlyphAtlas.png")

    assert cli.main(["atlas"]) == 1
    assert "[ERROR] font not found" in capsys.readouterr().err


def test_icons_writes_glyph_icons_and_a_sheet(tmp_path, monkeypatch):
    art = tmp_path / "Art"
    (art / "icons").mkdir(parents=True)
    Image.new("RGBA", (64, 64), (200, 200, 40, 255)).save(art / "icons" / "lamp.png")
    monkeypatch.setattr(cli, "ART_ROOT", art)
    monkeypatch.setattr(cli, "SHEET_PATH", tmp_path / "sheets" / "glyph-icons.png")
    monkeypatch.setattr(cli, "REPO_ROOT", tmp_path)

    assert cli.main(["icons"]) == 0

    with Image.open(art / "glyph-icons" / "lamp.png") as picture:
        assert picture.size == (72, 80)
    assert (tmp_path / "sheets" / "glyph-icons.png").is_file()

