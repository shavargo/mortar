"""Bake animated 8-direction actor sprite sheets from CC0 3D models with Blender."""

from __future__ import annotations

import argparse
import os
import shutil
import sys
from pathlib import Path

from mortar_art import actors, bake_runner, sheets
from mortar_art.blender import BlenderError, find_blender
from mortar_art.config import REPO_ROOT
from mortar_art.jobs import ART_ROOT

BAKE_ROOT = REPO_ROOT / "tools" / "art" / "out" / "bake"
REVIEW_PATH = REPO_ROOT / "tools" / "art" / "out" / "sheets" / "actors-baked.png"


def parse_args(argv: list[str] | None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--only", help="bake one actor id, e.g. warrior")
    parser.add_argument("--force", action="store_true", help="re-bake actors whose sheets already exist")
    parser.add_argument("--sheet", action="store_true", help="write a review contact sheet to tools/art/out/sheets")
    parser.add_argument("--list-clips", nargs="+", metavar="FILE",
                        help="print the clips and objects in files under tools/art/models")
    parser.add_argument("--rehash", action="store_true",
                        help="record the sha256 of every file under tools/art/models in actors.json")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        if args.rehash:
            print(f"[BAKE] Recorded {actors.record_hashes()} file hashes")
            return 0
        if args.list_clips:
            list_clips(args.list_clips)
            return 0
        manifest = actors.load_manifest()
        selected = select(manifest, args.only)
        failures = bake_all(manifest, selected, args.force)
        if args.sheet:
            write_review(selected)
    except (actors.ManifestError, BlenderError, bake_runner.BakeError) as error:
        print(f"[ERROR] {error}", file=sys.stderr)
        return 1
    return 1 if failures else 0


def select(manifest: actors.Manifest, only: str | None) -> list[actors.ActorSpec]:
    if only is None:
        return list(manifest.actors.values())
    if only not in manifest.actors:
        raise actors.ManifestError(f"no actor '{only}' in actors.json")
    return [manifest.actors[only]]


def is_baked(actor: actors.ActorSpec) -> bool:
    return all((ART_ROOT / "actors" / actor.id / f"{name}.png").is_file() for name in actor.wanted)


def bake_all(manifest: actors.Manifest, selected: list[actors.ActorSpec], force: bool) -> list[str]:
    pending = [actor for actor in selected if force or not is_baked(actor)]
    if not pending:
        print("[BAKE] Everything selected is already baked")
        return []
    blender = find_blender(os.environ)
    failures = []
    for index, actor in enumerate(pending, 1):
        print(f"[BAKE] {index}/{len(pending)} {actor.id}")
        try:
            bake_actor(blender, manifest, actor)
        except (actors.ManifestError, bake_runner.BakeError, OSError, ValueError) as error:
            print(f"[ERROR] {actor.id}: {error}", file=sys.stderr)
            failures.append(actor.id)
    return failures


def bake_actor(blender: Path, manifest: actors.Manifest, actor: actors.ActorSpec) -> None:
    actors.check_files(manifest, actor)
    work = BAKE_ROOT / actor.id
    shutil.rmtree(work, ignore_errors=True)
    bake_runner.run_job(blender, actors.build_job(actor, work), work)
    for path in sheets.pack_actor(work, ART_ROOT / "actors" / actor.id, actor.tint):
        print(f"[BAKE] Wrote {path.relative_to(REPO_ROOT)}")


def list_clips(names: list[str]) -> None:
    paths = [actors.MODELS_ROOT / name for name in names]
    missing = [str(path) for path in paths if not path.is_file()]
    if missing:
        raise actors.ManifestError(f"not found: {', '.join(missing)}")
    log = bake_runner.run_job(find_blender(os.environ), actors.list_job(paths), BAKE_ROOT / "_list")
    for line in bake_runner.prefixed_lines(log, "[CLIP]") + bake_runner.prefixed_lines(log, "[OBJECT]"):
        print(line)


def write_review(selected: list[actors.ActorSpec]) -> None:
    entries = [
        entry for actor in selected for entry in sheets.review_entries(actor.id, ART_ROOT / "actors" / actor.id)
    ]
    if not entries:
        print("[BAKE] No baked sheets to review")
        return
    sheets.write_review(entries, REVIEW_PATH)
    print(f"[BAKE] Wrote {REVIEW_PATH.relative_to(REPO_ROOT)}")


if __name__ == "__main__":
    sys.exit(main())
