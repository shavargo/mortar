import copy

import pytest

import generate
from mortar_art import jobs

SPEC = {
    "model": "m",
    "steps": 8,
    "style": {"tile": "tile style", "actor": "actor style", "icon": "icon style"},
    "tiles": [{"theme": "town", "ground": "grass", "variants": 3, "seed": 100, "prompt": "grass"}],
    "actors": [{"id": "warrior", "seed": 200, "prompt": "a warrior"}],
    "icons": [{"id": "gold", "seed": 300, "prompt": "coins"}],
}


@pytest.fixture
def run(monkeypatch):
    state = {"spec": copy.deepcopy(SPEC), "saved": [], "generated": []}
    monkeypatch.setattr(generate.jobs, "load_spec", lambda: state["spec"])
    monkeypatch.setattr(generate.jobs, "save_spec", lambda spec: state["saved"].append(copy.deepcopy(spec)))
    monkeypatch.setattr(generate, "generate", lambda selected, spec, force: state["generated"].append((selected, force)))

    def main(*argv):
        state["exit"] = generate.main(list(argv))
        return state

    return main


def test_rerolling_one_variant_regenerates_every_variant_of_the_entry(run):
    state = run("--only", "tiles/town/grass-1", "--reroll")

    selected, force = state["generated"][0]
    assert state["exit"] == 0
    assert force is True
    assert [job.key for job in selected] == ["tiles/town/grass-0", "tiles/town/grass-1", "tiles/town/grass-2"]
    assert [job.seed for job in selected] == [1100, 1101, 1102]
    assert state["saved"][0]["tiles"][0]["seed"] == 100 + jobs.REROLL_STEP


def test_reroll_with_an_empty_selection_saves_nothing(run, capsys):
    state = run("--kind", "tiles", "--only", "actors/warrior", "--reroll")

    assert state["exit"] == 1
    assert state["saved"] == []
    assert state["generated"] == []
    assert state["spec"] == SPEC
    assert "no jobs match" in capsys.readouterr().err


def test_without_reroll_only_the_named_variant_is_generated(run):
    state = run("--only", "tiles/town/grass-1")

    selected, force = state["generated"][0]
    assert [job.key for job in selected] == ["tiles/town/grass-1"]
    assert force is False
    assert state["saved"] == []
