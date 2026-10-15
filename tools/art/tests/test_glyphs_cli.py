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
