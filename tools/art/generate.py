"""Generate placeholder art for the Mortar Unity sample with Cloudflare Workers AI."""

from __future__ import annotations

import argparse
import io
import os
import sys
from pathlib import Path

from PIL import Image

from mortar_art import imaging, jobs, process
from mortar_art.config import REPO_ROOT, ConfigError, load_credentials
from mortar_art.workers_ai import ArtError, generate_image

SHEETS_ROOT = REPO_ROOT / "tools" / "art" / "out" / "sheets"


def parse_args(argv: list[str] | None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--kind", choices=("all",) + jobs.KINDS, default="all")
    parser.add_argument("--only", help="an entry or key, e.g. actors/warrior or tiles/town/grass")
    parser.add_argument("--reroll", action="store_true", help="bump the seed of --only and regenerate it")
    parser.add_argument("--force", action="store_true", help="regenerate sprites that already exist")
    parser.add_argument("--reprocess", action="store_true", help="rebuild sprites from saved raw images without calling the API")
    parser.add_argument("--sheet", action="store_true", help="write contact sheets to tools/art/out/sheets")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        spec = jobs.load_spec()
        if args.reroll:
            if not args.only:
                raise jobs.JobError("--reroll needs --only")
            jobs.reroll(spec, args.only)
            jobs.save_spec(spec)
        selected = jobs.select(jobs.build_jobs(spec, args.kind), args.only)
        if not selected:
            raise jobs.JobError("no jobs match")
        if args.reprocess:
            reprocess(selected)
        else:
            generate(selected, spec, force=args.force or args.reroll)
        if args.sheet:
            write_sheets(selected)
    except (jobs.JobError, ConfigError, ArtError) as error:
        print(f"[ERROR] {error}", file=sys.stderr)
        return 1
    return 0


def generate(selected: list[jobs.Job], spec: jobs.Spec, force: bool) -> None:
    pending = [job for job in selected if force or not job.output.exists()]
    if not pending:
        print("[ART] Everything selected is already generated")
        return
    credentials = load_credentials(os.environ, REPO_ROOT / ".env")
    for index, job in enumerate(pending, 1):
        print(f"[ART] {index}/{len(pending)} {job.key} (seed {job.seed})")
        data = generate_image(credentials, spec["model"], job.prompt, job.seed, int(spec.get("steps", 8)))
        job.raw.parent.mkdir(parents=True, exist_ok=True)
        job.raw.write_bytes(data)
        save_sprite(job, Image.open(io.BytesIO(data)))


def reprocess(selected: list[jobs.Job]) -> None:
    for job in selected:
        if not job.raw.exists():
            raise jobs.JobError(f"no raw image for {job.key}; generate it first")
        save_sprite(job, load(job.raw))
        print(f"[ART] Reprocessed {job.key}")


def save_sprite(job: jobs.Job, raw: Image.Image) -> None:
    job.output.parent.mkdir(parents=True, exist_ok=True)
    process.for_job(job, raw).save(job.output)


def write_sheets(selected: list[jobs.Job]) -> None:
    SHEETS_ROOT.mkdir(parents=True, exist_ok=True)
    for kind in jobs.KINDS:
        entries = [(job.key, load(job.output)) for job in selected if job.kind == kind and job.output.exists()]
        if entries:
            path = SHEETS_ROOT / f"{kind}.png"
            imaging.contact_sheet(entries).save(path)
            print(f"[ART] Wrote {path.relative_to(REPO_ROOT)}")


def load(path: Path) -> Image.Image:
    with Image.open(path) as image:
        return image.copy()


if __name__ == "__main__":
    sys.exit(main())
