"""Generation jobs built from prompts.json; keys match the Unity sprite catalog."""

from __future__ import annotations

import json
from collections.abc import Callable
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from .config import REPO_ROOT

ART_ROOT = REPO_ROOT / "unity" / "Mortar.Sample" / "Assets" / "Art"
RAW_ROOT = REPO_ROOT / "tools" / "art" / "out" / "raw"
PROMPTS_PATH = REPO_ROOT / "tools" / "art" / "prompts.json"
KINDS = ("tiles", "actors", "icons")
REROLL_STEP = 1000

Spec = dict[str, Any]


class JobError(Exception):
    pass


@dataclass(frozen=True)
class Job:
    kind: str
    entry: str
    key: str
    prompt: str
    seed: int
    raised: bool = False

    @property
    def output(self) -> Path:
        return ART_ROOT / f"{self.key}.png"

    @property
    def raw(self) -> Path:
        return RAW_ROOT / f"{self.key}.jpg"


def load_spec(path: Path = PROMPTS_PATH) -> Spec:
    return json.loads(path.read_text(encoding="utf-8"))


def save_spec(spec: Spec, path: Path = PROMPTS_PATH) -> None:
    path.write_text(json.dumps(spec, indent=2) + "\n", encoding="utf-8")


def build_jobs(spec: Spec, kind: str = "all") -> list[Job]:
    kinds = KINDS if kind == "all" else (kind,)
    if any(name not in KINDS for name in kinds):
        raise JobError(f"unknown kind '{kind}'")
    built = [job for name in kinds for job in _BUILDERS[name](spec)]
    keys = [job.key for job in built]
    duplicates = sorted({key for key in keys if keys.count(key) > 1})
    if duplicates:
        raise JobError(f"duplicate keys: {', '.join(duplicates)}")
    return built


def select(all_jobs: list[Job], only: str | None) -> list[Job]:
    return all_jobs if only is None else [job for job in all_jobs if only in (job.entry, job.key)]


def reroll(spec: Spec, only: str) -> int:
    matches = {job.entry for job in select(build_jobs(spec), only)}
    if not matches:
        raise JobError(f"nothing matches '{only}'")
    bumped = 0
    for section in KINDS:
        for entry in spec.get(section, []):
            if _entry_name(section, entry) in matches:
                entry["seed"] = int(entry["seed"]) + REROLL_STEP
                bumped += 1
    return bumped


def _tile_jobs(spec: Spec) -> list[Job]:
    style = _style(spec, "tile")
    built = []
    for entry in spec.get("tiles", []):
        name = _entry_name("tiles", entry)
        for variant in range(int(_field(entry, "variants"))):
            built.append(
                Job("tiles", name, f"{name}-{variant}", f"{_field(entry, 'prompt')}, {style}",
                    int(_field(entry, "seed")) + variant, bool(entry.get("raised", False)))
            )
    return built


def _named_jobs(spec: Spec, section: str, style_key: str) -> list[Job]:
    style = _style(spec, style_key)
    return [
        Job(section, _entry_name(section, entry), _entry_name(section, entry),
            f"{_field(entry, 'prompt')}, {style}", int(_field(entry, "seed")))
        for entry in spec.get(section, [])
    ]


def _entry_name(section: str, entry: dict[str, Any]) -> str:
    if section == "tiles":
        return f"tiles/{_field(entry, 'theme')}/{_field(entry, 'ground')}"
    return f"{section}/{_field(entry, 'id')}"


def _field(entry: dict[str, Any], name: str) -> Any:
    if name not in entry:
        raise JobError(f"prompt entry {entry!r} is missing '{name}'")
    return entry[name]


def _style(spec: Spec, key: str) -> str:
    styles = spec.get("style", {})
    if key not in styles:
        raise JobError(f"prompts.json has no '{key}' style")
    return styles[key]


_BUILDERS: dict[str, Callable[[Spec], list[Job]]] = {
    "tiles": _tile_jobs,
    "actors": lambda spec: _named_jobs(spec, "actors", "actor"),
    "icons": lambda spec: _named_jobs(spec, "icons", "icon"),
}
