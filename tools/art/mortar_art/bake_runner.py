"""Run Blender headless for one bake or clip-listing job and collect its errors."""

from __future__ import annotations

import json
import subprocess
from pathlib import Path
from typing import Any

from .config import REPO_ROOT

BAKE_SCRIPT = REPO_ROOT / "tools" / "art" / "blender" / "bake_actor.py"
TIMEOUT_SECONDS = 3600
ERROR_PREFIX = "[BAKE-ERROR] "


class BakeError(Exception):
    pass


def blender_command(blender: Path, job_path: Path) -> list[str]:
    return [
        str(blender), "--background", "--factory-startup", "--python-exit-code", "2",
        "--python", str(BAKE_SCRIPT), "--", str(job_path),
    ]


def run_job(blender: Path, job: dict[str, Any], work_dir: Path) -> str:
    work_dir.mkdir(parents=True, exist_ok=True)
    job_path = work_dir / "job.json"
    log_path = work_dir / "blender.log"
    job_path.write_text(json.dumps(job, indent=2) + "\n", encoding="utf-8")
    with log_path.open("w", encoding="utf-8", errors="replace") as log:
        try:
            result = subprocess.run(
                blender_command(blender, job_path), stdout=log, stderr=subprocess.STDOUT,
                timeout=TIMEOUT_SECONDS, check=False,
            )
        except subprocess.TimeoutExpired as error:
            raise BakeError(f"Blender timed out after {TIMEOUT_SECONDS}s (log: {log_path})") from error
    text = log_path.read_text(encoding="utf-8", errors="replace")
    if result.returncode != 0:
        raise BakeError(failure_message(text, result.returncode, log_path))
    return text


def failure_message(log_text: str, code: int, log_path: Path) -> str:
    errors = [line[len(ERROR_PREFIX):].strip() for line in prefixed_lines(log_text, ERROR_PREFIX)]
    detail = "; ".join(errors) if errors else f"Blender exited with code {code}"
    return f"{detail} (log: {log_path})"


def prefixed_lines(log_text: str, prefix: str) -> list[str]:
    return [line for line in log_text.splitlines() if line.startswith(prefix)]
