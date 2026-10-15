import json

import pytest

from mortar_art import jobs
from mortar_art.config import REPO_ROOT

CONTENT = REPO_ROOT / "content"


def spec():
    return {
        "model": "m",
        "steps": 8,
        "style": {"tile": "tile style", "actor": "actor style", "icon": "icon style"},
        "tiles": [{"theme": "town", "ground": "grass", "variants": 2, "seed": 100, "prompt": "grass"}],
        "actors": [{"id": "warrior", "seed": 200, "prompt": "a warrior"}],
        "icons": [{"id": "gold", "seed": 300, "prompt": "coins"}],
    }


def test_tile_entries_expand_into_variants():
    tiles = jobs.build_jobs(spec(), "tiles")

    assert [job.key for job in tiles] == ["tiles/town/grass-0", "tiles/town/grass-1"]
    assert [job.seed for job in tiles] == [100, 101]
    assert tiles[0].prompt == "grass, tile style"
    assert tiles[0].output == jobs.ART_ROOT / "tiles" / "town" / "grass-0.png"


def test_actor_and_icon_keys_match_the_sprite_catalog():
    keys = [job.key for job in jobs.build_jobs(spec())]

    assert keys == ["tiles/town/grass-0", "tiles/town/grass-1", "actors/warrior", "icons/gold"]


def test_unknown_kinds_are_rejected():
    with pytest.raises(jobs.JobError):
        jobs.build_jobs(spec(), "sounds")


def test_duplicate_keys_are_rejected():
    duplicated = spec()
    duplicated["actors"].append({"id": "warrior", "seed": 1, "prompt": "again"})

    with pytest.raises(jobs.JobError):
        jobs.build_jobs(duplicated)


def test_select_matches_an_entry_or_a_key():
    all_jobs = jobs.build_jobs(spec())

    assert len(jobs.select(all_jobs, "tiles/town/grass")) == 2
    assert len(jobs.select(all_jobs, "tiles/town/grass-1")) == 1
    assert jobs.select(all_jobs, None) == all_jobs


def test_reroll_bumps_the_entry_seed():
    data = spec()

    assert jobs.reroll(data, "tiles/town/grass-1") == 1
    assert data["tiles"][0]["seed"] == 100 + jobs.REROLL_STEP
    with pytest.raises(jobs.JobError):
        jobs.reroll(data, "actors/nobody")


def load(path):
    return json.loads(path.read_text(encoding="utf-8"))


def test_prompts_cover_all_content():
    expected = set()
    for tileset in (CONTENT / "tilesets").glob("*.json"):
        data = load(tileset)
        weights = data.get("variantWeights", {})
        for ground in data["groundTypes"]:
            if ground["id"] != "void":
                for variant in range(len(weights.get(ground["id"], [1]))):
                    expected.add(f"tiles/{tileset.stem}/{ground['id']}-{variant}")
    for folder in ("classes", "monsters"):
        expected |= {f"actors/{load(path)['spriteSet']}" for path in (CONTENT / folder).glob("*.json")}
    expected |= {f"actors/{name}" for name in ("stairs-down", "stairs-up", "smith", "healer", "sage")}
    expected |= {f"icons/{load(path)['icon']}" for path in (CONTENT / "items").glob("*.json")}
    expected.add("icons/gold")

    assert {job.key for job in jobs.build_jobs(jobs.load_spec())} == expected


def test_raised_tiles_are_the_sight_blocking_grounds():
    blocking = set()
    for tileset in (CONTENT / "tilesets").glob("*.json"):
        blocking |= {f"tiles/{tileset.stem}/{ground['id']}" for ground in load(tileset)["groundTypes"] if ground["blocksSight"]}

    tiles = jobs.build_jobs(jobs.load_spec(), "tiles")

    assert {job.entry for job in tiles if job.raised} == blocking
