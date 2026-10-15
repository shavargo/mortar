import json
import subprocess
from pathlib import Path

import pytest

from mortar_art import bake_runner
from mortar_art.bake_runner import BakeError


def test_blender_runs_headless_with_factory_settings_and_the_job():
    command = bake_runner.blender_command(Path("blender.exe"), Path("job.json"))

    assert command[:5] == ["blender.exe", "--background", "--factory-startup", "--python-exit-code", "2"]
    assert command[5:7] == ["--python", str(bake_runner.BAKE_SCRIPT)]
    assert command[7:] == ["--", "job.json"]


def _fake_blender(monkeypatch, output, code):
    def run(command, stdout, stderr, timeout, check):
        stdout.write(output)
        return subprocess.CompletedProcess(command, code)

    monkeypatch.setattr(bake_runner.subprocess, "run", run)


def test_a_successful_job_returns_the_log_and_writes_the_job(monkeypatch, tmp_path):
    _fake_blender(monkeypatch, "[CLIP] a.glb: Idle (1.00s)\n", 0)

    log = bake_runner.run_job(Path("blender.exe"), {"mode": "list", "files": []}, tmp_path)

    assert bake_runner.prefixed_lines(log, "[CLIP]") == ["[CLIP] a.glb: Idle (1.00s)"]
    assert json.loads((tmp_path / "job.json").read_text(encoding="utf-8")) == {"mode": "list", "files": []}


def test_bake_errors_are_reported_with_the_log_path(monkeypatch, tmp_path):
    _fake_blender(monkeypatch, "noise\n[BAKE-ERROR] no clip named 'Idle'\n", 2)

    with pytest.raises(BakeError, match="no clip named 'Idle'") as error:
        bake_runner.run_job(Path("blender.exe"), {"mode": "list", "files": []}, tmp_path)
    assert "blender.log" in str(error.value)


def test_a_crash_without_a_bake_error_reports_the_exit_code():
    message = bake_runner.failure_message("Segmentation fault\n", 11, Path("blender.log"))

    assert message.startswith("Blender exited with code 11")


def test_timeouts_are_reported(monkeypatch, tmp_path):
    def run(command, stdout, stderr, timeout, check):
        raise subprocess.TimeoutExpired(command, timeout)

    monkeypatch.setattr(bake_runner.subprocess, "run", run)

    with pytest.raises(BakeError, match="timed out"):
        bake_runner.run_job(Path("blender.exe"), {"mode": "list", "files": []}, tmp_path)
