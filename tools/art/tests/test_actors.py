import copy
import json

import pytest

from mortar_art import actors
from mortar_art.actors import ManifestError

WARRIOR = {
    "id": "warrior",
    "model": "pack/knight.glb",
    "animations": ["pack/moves.glb"],
    "hide": ["Cape"],
    "clips": {"idle": "Idle", "walk": "Walk", "attack": "Chop", "hit": "Hit", "death": "Death"},
    "attackHitSeconds": 0.4,
    "scale": 1.1,
    "facing": 90,
    "tint": [1.0, 0.9, 0.8],
}
SMITH = {"id": "smith", "model": "pack/smith.glb", "npc": True, "clips": {"idle": "Idle"}}


def manifest_data(*entries):
    files = {name: "0" * 64 for entry in entries for name in [entry["model"], *entry.get("animations", [])]}
    return {"actors": [copy.deepcopy(entry) for entry in entries], "files": files}


def test_an_actor_entry_is_parsed():
    warrior = actors.parse_manifest(manifest_data(WARRIOR)).actors["warrior"]

    assert warrior.model == "pack/knight.glb"
    assert warrior.files == ("pack/knight.glb", "pack/moves.glb")
    assert warrior.hide == ("Cape",)
    assert warrior.wanted == ("idle", "walk", "attack", "hit", "death")
    assert warrior.directions == tuple(range(8))
    assert (warrior.scale, warrior.facing, warrior.tint) == (1.1, 90.0, (1.0, 0.9, 0.8))
    assert warrior.attack_hit_seconds == 0.4


def test_npcs_need_only_idle_and_face_south():
    smith = actors.parse_manifest(manifest_data(SMITH)).actors["smith"]

    assert smith.wanted == ("idle",)
    assert smith.directions == (0,)
    assert smith.tint == (1.0, 1.0, 1.0)


def test_a_missing_clip_is_named():
    entry = copy.deepcopy(WARRIOR)
    del entry["clips"]["death"]

    with pytest.raises(ManifestError, match="warrior: clips are missing death"):
        actors.parse_manifest(manifest_data(entry))


def test_unknown_animation_names_are_rejected():
    entry = copy.deepcopy(WARRIOR)
    entry["clips"]["cast"] = "Spell"

    with pytest.raises(ManifestError, match="unknown animations cast"):
        actors.parse_manifest(manifest_data(entry))


def test_fighters_need_an_attack_hit_time():
    entry = copy.deepcopy(WARRIOR)
    del entry["attackHitSeconds"]

    with pytest.raises(ManifestError, match="attackHitSeconds"):
        actors.parse_manifest(manifest_data(entry))


def test_a_tint_needs_three_numbers():
    entry = copy.deepcopy(WARRIOR)
    entry["tint"] = [1.0, 1.0]

    with pytest.raises(ManifestError, match="tint"):
        actors.parse_manifest(manifest_data(entry))


def test_an_actor_cannot_be_listed_twice():
    with pytest.raises(ManifestError, match="listed twice"):
        actors.parse_manifest(manifest_data(SMITH, SMITH))


def test_every_referenced_file_needs_a_hash():
    data = manifest_data(WARRIOR)
    del data["files"]["pack/moves.glb"]

    with pytest.raises(ManifestError, match="no sha256 recorded for pack/moves.glb"):
        actors.parse_manifest(data)


def test_a_missing_manifest_is_reported(tmp_path):
    with pytest.raises(ManifestError, match="not found"):
        actors.load_manifest(tmp_path / "actors.json")


def test_required_ids_come_from_classes_monsters_and_npcs(tmp_path):
    for folder, name in (("classes", "warrior"), ("monsters", "zombie")):
        (tmp_path / folder).mkdir()
        (tmp_path / folder / f"{name}.json").write_text(json.dumps({"id": name, "spriteSet": name}), encoding="utf-8")

    assert actors.required_actor_ids(tmp_path) == {"warrior", "zombie", "smith", "healer", "sage"}


def _models(tmp_path, content=b"model"):
    (tmp_path / "pack").mkdir(parents=True)
    (tmp_path / "pack" / "smith.glb").write_bytes(content)
    (tmp_path / "pack" / "LICENSE.txt").write_text("CC0", encoding="utf-8")
    return tmp_path


def test_files_matching_their_hash_pass(tmp_path):
    root = _models(tmp_path)
    data = manifest_data(SMITH)
    data["files"]["pack/smith.glb"] = actors.sha256_of(root / "pack" / "smith.glb")
    manifest = actors.parse_manifest(data)

    actors.check_files(manifest, manifest.actors["smith"], root)


def test_a_changed_file_is_rejected(tmp_path):
    root = _models(tmp_path)
    manifest = actors.parse_manifest(manifest_data(SMITH))

    with pytest.raises(ManifestError, match="smith: pack/smith.glb does not match its recorded sha256"):
        actors.check_files(manifest, manifest.actors["smith"], root)


def test_a_missing_file_is_rejected(tmp_path):
    manifest = actors.parse_manifest(manifest_data(SMITH))

    with pytest.raises(ManifestError, match="model file pack/smith.glb is missing"):
        actors.check_files(manifest, manifest.actors["smith"], tmp_path)


def test_hashing_skips_licences_and_uses_forward_slashes(tmp_path):
    root = _models(tmp_path)

    assert list(actors.hash_models(root)) == ["pack/smith.glb"]


def test_recording_hashes_keeps_the_actors(tmp_path):
    root = _models(tmp_path / "models")
    path = tmp_path / "actors.json"
    path.write_text(json.dumps({"actors": [SMITH], "files": {}}), encoding="utf-8")

    assert actors.record_hashes(path, root) == 1
    saved = json.loads(path.read_text(encoding="utf-8"))
    assert saved["actors"] == [SMITH]
    assert saved["files"]["pack/smith.glb"] == actors.sha256_of(root / "pack" / "smith.glb")


def test_a_bake_job_has_absolute_paths_and_the_wanted_clips(tmp_path):
    warrior = actors.parse_manifest(manifest_data(WARRIOR)).actors["warrior"]

    job = actors.build_job(warrior, tmp_path / "out", tmp_path)

    assert job["mode"] == "bake"
    assert job["model"] == str(tmp_path / "pack" / "knight.glb")
    assert job["animations"] == [str(tmp_path / "pack" / "moves.glb")]
    assert list(job["clips"]) == ["idle", "walk", "attack", "hit", "death"]
    assert job["directions"] == list(range(8))
    assert (job["scale"], job["facing"], job["attackHitSeconds"]) == (1.1, 90.0, 0.4)
    assert job["hide"] == ["Cape"]
    assert job["maxFrames"] is None
    assert job["out"] == str(tmp_path / "out")


def test_a_bake_job_can_be_narrowed(tmp_path):
    warrior = actors.parse_manifest(manifest_data(WARRIOR)).actors["warrior"]

    job = actors.build_job(warrior, tmp_path, tmp_path, animations=("idle",), directions=(0,), max_frames=1)

    assert job["clips"] == {"idle": "Idle"}
    assert job["directions"] == [0]
    assert job["maxFrames"] == 1


def test_a_list_job_names_the_files(tmp_path):
    assert actors.list_job([tmp_path / "a.glb"]) == {"mode": "list", "files": [str(tmp_path / "a.glb")]}
