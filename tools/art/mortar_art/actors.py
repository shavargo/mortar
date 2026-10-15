"""The actor bake manifest (actors.json): which model, clips and look each actor uses."""

from __future__ import annotations

import hashlib
import json
from collections.abc import Iterable
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from .bake_geometry import ANIMATIONS
from .config import REPO_ROOT

ART_TOOL_ROOT = REPO_ROOT / "tools" / "art"
ACTORS_PATH = ART_TOOL_ROOT / "actors.json"
MODELS_ROOT = ART_TOOL_ROOT / "models"
CONTENT_ROOT = REPO_ROOT / "content"
NPC_IDS = ("smith", "healer", "sage")
LICENCE_NAME = "LICENSE.txt"


class ManifestError(Exception):
    pass


@dataclass(frozen=True)
class ActorSpec:
    id: str
    model: str
    animations: tuple[str, ...]
    hide: tuple[str, ...]
    clips: dict[str, str]
    scale: float = 1.0
    facing: float = 0.0
    tint: tuple[float, float, float] = (1.0, 1.0, 1.0)
    attack_hit_seconds: float = 0.0
    npc: bool = False

    @property
    def wanted(self) -> tuple[str, ...]:
        return ("idle",) if self.npc else ANIMATIONS

    @property
    def files(self) -> tuple[str, ...]:
        return (self.model, *self.animations)

    @property
    def directions(self) -> tuple[int, ...]:
        return (0,) if self.npc else tuple(range(8))


@dataclass(frozen=True)
class Manifest:
    actors: dict[str, ActorSpec]
    files: dict[str, str]


def load_manifest(path: Path = ACTORS_PATH) -> Manifest:
    if not path.is_file():
        raise ManifestError(f"{path.name} not found at {path}")
    return parse_manifest(json.loads(path.read_text(encoding="utf-8")))


def parse_manifest(data: dict[str, Any]) -> Manifest:
    files = {str(name): str(digest) for name, digest in data.get("files", {}).items()}
    parsed: dict[str, ActorSpec] = {}
    for entry in data.get("actors", []):
        actor = _parse_actor(entry)
        if actor.id in parsed:
            raise ManifestError(f"actor '{actor.id}' is listed twice")
        unhashed = [name for name in actor.files if name not in files]
        if unhashed:
            raise ManifestError(f"{actor.id}: no sha256 recorded for {', '.join(unhashed)}")
        parsed[actor.id] = actor
    return Manifest(parsed, files)


def _parse_actor(entry: dict[str, Any]) -> ActorSpec:
    actor_id = str(_require(entry, "id", "an actor entry"))
    npc = bool(entry.get("npc", False))
    clips = {str(name): str(clip) for name, clip in _require(entry, "clips", actor_id).items()}
    missing = [name for name in (("idle",) if npc else ANIMATIONS) if name not in clips]
    if missing:
        raise ManifestError(f"{actor_id}: clips are missing {', '.join(missing)}")
    unknown = sorted(set(clips) - set(ANIMATIONS))
    if unknown:
        raise ManifestError(f"{actor_id}: unknown animations {', '.join(unknown)}")
    hit = float(entry.get("attackHitSeconds", 0.0))
    if not npc and hit <= 0:
        raise ManifestError(f"{actor_id}: attackHitSeconds must be greater than 0")
    tint = tuple(float(value) for value in entry.get("tint", (1.0, 1.0, 1.0)))
    if len(tint) != 3:
        raise ManifestError(f"{actor_id}: tint needs three numbers")
    return ActorSpec(
        id=actor_id,
        model=str(_require(entry, "model", actor_id)),
        animations=_strings(entry.get("animations", [])),
        hide=_strings(entry.get("hide", [])),
        clips=clips,
        scale=float(entry.get("scale", 1.0)),
        facing=float(entry.get("facing", 0.0)),
        tint=(tint[0], tint[1], tint[2]),
        attack_hit_seconds=hit,
        npc=npc,
    )


def _require(entry: dict[str, Any], name: str, owner: str) -> Any:
    if name not in entry:
        raise ManifestError(f"{owner} is missing '{name}'")
    return entry[name]


def _strings(values: Iterable[Any]) -> tuple[str, ...]:
    return tuple(str(value) for value in values)


def required_actor_ids(content_root: Path = CONTENT_ROOT) -> set[str]:
    ids = set(NPC_IDS)
    for folder in ("classes", "monsters"):
        for path in sorted((content_root / folder).glob("*.json")):
            sprite_set = json.loads(path.read_text(encoding="utf-8")).get("spriteSet")
            if sprite_set:
                ids.add(str(sprite_set))
    return ids


def sha256_of(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check_files(manifest: Manifest, actor: ActorSpec, models_root: Path = MODELS_ROOT) -> None:
    for name in actor.files:
        path = models_root / name
        if not path.is_file():
            raise ManifestError(f"{actor.id}: model file {name} is missing")
        if sha256_of(path) != manifest.files[name]:
            raise ManifestError(f"{actor.id}: {name} does not match its recorded sha256")


def hash_models(models_root: Path = MODELS_ROOT) -> dict[str, str]:
    return {
        path.relative_to(models_root).as_posix(): sha256_of(path)
        for path in sorted(models_root.rglob("*"))
        if path.is_file() and path.name != LICENCE_NAME
    }


def record_hashes(path: Path = ACTORS_PATH, models_root: Path = MODELS_ROOT) -> int:
    data = json.loads(path.read_text(encoding="utf-8"))
    data["files"] = hash_models(models_root)
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    return len(data["files"])


def build_job(
    actor: ActorSpec,
    out_dir: Path,
    models_root: Path = MODELS_ROOT,
    *,
    animations: tuple[str, ...] | None = None,
    directions: tuple[int, ...] | None = None,
    max_frames: int | None = None,
) -> dict[str, Any]:
    return {
        "mode": "bake",
        "actor": actor.id,
        "model": str(models_root / actor.model),
        "animations": [str(models_root / name) for name in actor.animations],
        "hide": list(actor.hide),
        "clips": {name: actor.clips[name] for name in (animations or actor.wanted)},
        "attackHitSeconds": actor.attack_hit_seconds,
        "scale": actor.scale,
        "facing": actor.facing,
        "directions": list(directions if directions is not None else actor.directions),
        "maxFrames": max_frames,
        "out": str(out_dir),
    }


def list_job(paths: list[Path]) -> dict[str, Any]:
    return {"mode": "list", "files": [str(path) for path in paths]}
