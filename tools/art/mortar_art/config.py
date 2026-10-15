"""Credentials for Cloudflare Workers AI, read without ever echoing their values."""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass, field
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
REQUIRED_KEYS = ("CF_API_TOKEN", "CF_ACCOUNT_ID")


class ConfigError(Exception):
    pass


@dataclass(frozen=True)
class Credentials:
    account_id: str = field(repr=False)
    api_token: str = field(repr=False)


def parse_env_file(text: str) -> dict[str, str]:
    values: dict[str, str] = {}
    for line in text.splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#") or "=" not in stripped:
            continue
        key, value = stripped.split("=", 1)
        values[key.strip()] = value.strip().strip('"').strip("'")
    return values


def load_credentials(environ: Mapping[str, str], env_file: Path | None) -> Credentials:
    values: dict[str, str] = {}
    if env_file is not None and env_file.is_file():
        values.update(parse_env_file(env_file.read_text(encoding="utf-8")))
    values.update({key: environ[key].strip() for key in REQUIRED_KEYS if environ.get(key, "").strip()})
    missing = [key for key in REQUIRED_KEYS if not values.get(key)]
    if missing:
        raise ConfigError(f"Missing {', '.join(missing)}; set them in the environment or in .env")
    malformed = [key for key in REQUIRED_KEYS if not _is_clean(values[key])]
    if malformed:
        raise ConfigError(f"{', '.join(malformed)} must not contain whitespace or control characters")
    return Credentials(account_id=values["CF_ACCOUNT_ID"], api_token=values["CF_API_TOKEN"])


def _is_clean(value: str) -> bool:
    return all(character.isprintable() and not character.isspace() for character in value)
