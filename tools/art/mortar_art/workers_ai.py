"""Minimal Cloudflare Workers AI client for text-to-image models."""

from __future__ import annotations

import base64
import binascii
import http.client
import json
import urllib.error
import urllib.request
from collections.abc import Callable
from typing import Any

from .config import Credentials

API_BASE = "https://api.cloudflare.com/client/v4/accounts"
MAX_STEPS = 8
MAX_ERROR_LENGTH = 200


class ArtError(Exception):
    pass


def build_request(credentials: Credentials, model: str, prompt: str, steps: int) -> urllib.request.Request:
    if not 1 <= steps <= MAX_STEPS:
        raise ArtError(f"steps must be between 1 and {MAX_STEPS}")
    body = json.dumps({"prompt": prompt, "steps": steps}).encode("utf-8")
    return urllib.request.Request(
        f"{API_BASE}/{credentials.account_id}/ai/run/{model}",
        data=body,
        method="POST",
        headers={"Authorization": f"Bearer {credentials.api_token}", "Content-Type": "application/json"},
    )


def generate_image(
    credentials: Credentials,
    model: str,
    prompt: str,
    steps: int = MAX_STEPS,
    timeout: float = 120.0,
    opener: Callable[..., Any] = urllib.request.urlopen,
) -> bytes:
    try:
        request = build_request(credentials, model, prompt, steps)
        with opener(request, timeout=timeout) as response:
            payload = json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as error:
        message = f"Workers AI returned HTTP {error.code}" + _http_error_detail(error, credentials)
    except (urllib.error.URLError, OSError, http.client.HTTPException) as error:
        message = f"Workers AI request failed ({type(error).__name__})"
    except json.JSONDecodeError:
        message = "Workers AI returned invalid JSON"
    except ValueError as error:
        message = f"Workers AI request was rejected before sending ({type(error).__name__})"
    else:
        return _decode_image(payload, credentials)
    raise ArtError(message) from None


def _http_error_detail(error: urllib.error.HTTPError, credentials: Credentials) -> str:
    try:
        payload = json.loads(error.read().decode("utf-8"))
    except (OSError, ValueError, http.client.HTTPException):
        payload = None
    finally:
        error.close()
    return _first_error(payload, credentials)


def _redact(text: str, credentials: Credentials) -> str:
    for secret in sorted((credentials.api_token, credentials.account_id), key=len, reverse=True):
        if secret:
            text = text.replace(secret, "[redacted]")
    return text


def _decode_image(payload: Any, credentials: Credentials) -> bytes:
    result = payload.get("result") if isinstance(payload, dict) else None
    image = result.get("image") if isinstance(result, dict) else None
    if not isinstance(image, str):
        raise ArtError("Workers AI returned no image" + _first_error(payload, credentials))
    try:
        return base64.b64decode(image, validate=True)
    except binascii.Error:
        raise ArtError("Workers AI returned an invalid image") from None


def _first_error(payload: Any, credentials: Credentials) -> str:
    errors = payload.get("errors") if isinstance(payload, dict) else None
    if isinstance(errors, list) and errors and isinstance(errors[0], dict):
        message = _redact(str(errors[0].get("message", "")), credentials)
        return f": {message[:MAX_ERROR_LENGTH]}"
    return ""
