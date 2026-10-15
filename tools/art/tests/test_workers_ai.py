import base64
import io
import json
import urllib.error

import pytest

from mortar_art.config import Credentials
from mortar_art.workers_ai import ArtError, build_request, generate_image

CREDENTIALS = Credentials(account_id="acct-123", api_token="secret-token")
MODEL = "@cf/black-forest-labs/flux-1-schnell"


def opener_returning(payload, captured=None):
    def opener(request, timeout):
        if captured is not None:
            captured.append(request)
        return io.BytesIO(json.dumps(payload).encode("utf-8"))

    return opener


def test_request_targets_the_model_with_a_bearer_token():
    request = build_request(CREDENTIALS, MODEL, "a stone floor", 7, 8)

    assert request.full_url.endswith("/accounts/acct-123/ai/run/" + MODEL)
    assert request.get_header("Authorization") == "Bearer secret-token"
    assert json.loads(request.data) == {"prompt": "a stone floor", "steps": 8, "seed": 7}


def test_decodes_the_returned_image():
    encoded = base64.b64encode(b"jpeg-bytes").decode("ascii")
    captured = []

    data = generate_image(CREDENTIALS, MODEL, "p", 1, opener=opener_returning({"success": True, "result": {"image": encoded}}, captured))

    assert data == b"jpeg-bytes"
    assert len(captured) == 1


def test_http_errors_hide_credentials():
    def failing(request, timeout):
        raise urllib.error.HTTPError(request.full_url, 401, "Unauthorized", None, None)

    with pytest.raises(ArtError) as raised:
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=failing)

    message = str(raised.value)
    assert "401" in message
    assert "secret-token" not in message
    assert "acct-123" not in message
    assert raised.value.__cause__ is None


def test_missing_image_is_an_error():
    with pytest.raises(ArtError):
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=opener_returning({"success": False, "errors": [{"message": "bad prompt"}]}))
