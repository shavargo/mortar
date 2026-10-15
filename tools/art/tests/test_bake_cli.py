import pytest

import bake
from mortar_art import actors
from mortar_art.bake_runner import BakeError
from mortar_art.blender import BlenderError

DATA = {
    "actors": [
        {"id": "smith", "model": "p/smith.glb", "npc": True, "clips": {"idle": "Idle"}},
        {"id": "healer", "model": "p/healer.glb", "npc": True, "clips": {"idle": "Idle"}},
    ],
    "files": {"p/smith.glb": "0" * 64, "p/healer.glb": "0" * 64},
}


@pytest.fixture
def cli(monkeypatch):
    state = {"baked": [], "fail": set(), "baked_already": set()}
    monkeypatch.setattr(bake.actors, "load_manifest", lambda: actors.parse_manifest(DATA))
    monkeypatch.setattr(bake, "is_baked", lambda actor: actor.id in state["baked_already"])
    monkeypatch.setattr(bake, "find_blender", lambda environ: "blender.exe")

    def bake_actor(blender, manifest, actor):
        if actor.id in state["fail"]:
            raise BakeError("no clip named 'Idle'")
        state["baked"].append(actor.id)

    monkeypatch.setattr(bake, "bake_actor", bake_actor)
    return state


def test_every_actor_is_baked_by_default(cli):
    assert bake.main([]) == 0
    assert cli["baked"] == ["smith", "healer"]


def test_only_bakes_one_actor(cli):
    assert bake.main(["--only", "healer"]) == 0
    assert cli["baked"] == ["healer"]


def test_an_unknown_actor_is_an_error(cli, capsys):
    assert bake.main(["--only", "dragon"]) == 1
    assert "no actor 'dragon'" in capsys.readouterr().err


def test_baked_actors_are_skipped_without_needing_blender(cli, monkeypatch, capsys):
    cli["baked_already"].update({"smith", "healer"})

    def no_blender(environ):
        raise BlenderError("not installed")

    monkeypatch.setattr(bake, "find_blender", no_blender)

    assert bake.main([]) == 0
    assert "already baked" in capsys.readouterr().out


def test_force_rebakes_baked_actors(cli):
    cli["baked_already"].add("smith")

    assert bake.main(["--force"]) == 0
    assert cli["baked"] == ["smith", "healer"]


def test_one_failure_does_not_stop_the_others(cli, capsys):
    cli["fail"].add("smith")

    assert bake.main([]) == 1
    assert cli["baked"] == ["healer"]
    assert "smith: no clip named 'Idle'" in capsys.readouterr().err


def test_rehash_records_hashes_and_bakes_nothing(cli, monkeypatch):
    monkeypatch.setattr(bake.actors, "record_hashes", lambda: 3)

    assert bake.main(["--rehash"]) == 0
    assert cli["baked"] == []
