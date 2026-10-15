from pathlib import Path

import pytest

from mortar_art.blender import PINNED_VERSION, BlenderError, find_blender


def _install(root: Path, version: str) -> Path:
    exe = root / "Blender Foundation" / f"Blender {version}" / "blender.exe"
    exe.parent.mkdir(parents=True)
    exe.write_bytes(b"")
    return exe


def test_the_blender_variable_wins(tmp_path):
    custom = tmp_path / "custom" / "blender.exe"
    custom.parent.mkdir()
    custom.write_bytes(b"")
    _install(tmp_path, PINNED_VERSION)

    assert find_blender({"BLENDER": str(custom), "ProgramFiles": str(tmp_path)}) == custom


def test_the_blender_variable_must_name_a_file(tmp_path):
    with pytest.raises(BlenderError, match="not a file"):
        find_blender({"BLENDER": str(tmp_path / "missing.exe")})


def test_the_pinned_install_is_found(tmp_path):
    exe = _install(tmp_path, PINNED_VERSION)

    assert find_blender({"ProgramFiles": str(tmp_path)}) == exe


def test_other_versions_are_named_but_not_used(tmp_path):
    _install(tmp_path, "5.2")

    with pytest.raises(BlenderError, match=r"found Blender 5\.2") as error:
        find_blender({"ProgramFiles": str(tmp_path)})
    assert f"BlenderFoundation.Blender.LTS.{PINNED_VERSION}" in str(error.value)


def test_a_missing_install_says_how_to_fix_it(tmp_path):
    with pytest.raises(BlenderError, match="set BLENDER"):
        find_blender({"ProgramFiles": str(tmp_path)})


def test_no_program_files_still_explains(tmp_path):
    with pytest.raises(BlenderError, match="set BLENDER"):
        find_blender({})
