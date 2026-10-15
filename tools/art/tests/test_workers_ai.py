import base64
import http.client
import io
import json
import urllib.error

import pytest

from mortar_art.config import Credentials
from mortar_art.workers_ai import ArtError, build_request, generate_image

CREDENTIALS = Credentials(account_id="acct-123", api_token="secret-token")
FAKE_TOKEN = "FAKE-TOKEN-123"
MODEL = "@cf/black-forest-labs/flux-1-schnell"


def opener_returning(payload, captured=None):
    def opener(request, timeout):
        if captured is not None:
            captured.append(request)
        return io.BytesIO(json.dumps(payload).encode("utf-8"))

    return opener


def opener_raising(error):
    def opener(request, timeout):
        raise error

    return opener


def assert_hides_credentials(raised, *secrets):
    error = raised.value
    assert error.__cause__ is None
    assert error.__suppress_context__ is True
    chain = []
    current = error
    while current is not None:
        chain.append(str(current))
        current = current.__context__
    for secret in secrets:
        assert all(secret not in text for text in chain)


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

    assert "401" in str(raised.value)
    assert_hides_credentials(raised, "secret-token", "acct-123")


def test_http_errors_report_the_first_cloudflare_error():
    body = json.dumps({"errors": [{"message": "bad input"}], "success": False}).encode("utf-8")
    closed = []

    def failing(request, timeout):
        error = urllib.error.HTTPError(request.full_url, 400, "Bad Request", None, io.BytesIO(body))
        closed.append(error)
        raise error

    with pytest.raises(ArtError) as raised:
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=failing)

    message = str(raised.value)
    assert "400" in message
    assert "bad input" in message
    assert "https://" not in message
    assert closed[0].fp is None or closed[0].fp.closed
    assert_hides_credentials(raised, "secret-token", "acct-123")


def test_cloudflare_error_messages_are_redacted():
    body = json.dumps({"errors": [{"message": "account acct-123 rejected secret-token"}]}).encode("utf-8")

    def failing(request, timeout):
        raise urllib.error.HTTPError(request.full_url, 403, "Forbidden", None, io.BytesIO(body))

    with pytest.raises(ArtError) as raised:
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=failing)

    assert "403" in str(raised.value)
    assert_hides_credentials(raised, "secret-token", "acct-123")


def test_redaction_happens_before_truncation():
    message = "x" * 189 + " secret-token"
    body = json.dumps({"errors": [{"message": message}]}).encode("utf-8")

    def failing(request, timeout):
        raise urllib.error.HTTPError(request.full_url, 400, "Bad Request", None, io.BytesIO(body))

    with pytest.raises(ArtError) as raised:
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=failing)

    assert "secret-tok" not in str(raised.value)


def test_successful_responses_without_an_image_redact_errors():
    payload = {"success": False, "errors": [{"message": "acct-123 cannot use secret-token"}]}

    with pytest.raises(ArtError) as raised:
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=opener_returning(payload))

    assert "secret-token" not in str(raised.value)
    assert "acct-123" not in str(raised.value)


def test_connection_failures_name_only_the_error_type():
    with pytest.raises(ArtError) as raised:
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=opener_raising(urllib.error.URLError("no route to acct-123")))

    assert "URLError" in str(raised.value)
    assert_hides_credentials(raised, "acct-123")


@pytest.mark.parametrize(
    "error",
    [ValueError(f"Invalid header value b'Bearer {FAKE_TOKEN}\\n'"), http.client.InvalidURL(f"URL can't contain control characters: {FAKE_TOKEN}")],
)
def test_client_errors_hide_the_token_and_url(error):
    credentials = Credentials(account_id="FAKE-ACCOUNT-123", api_token=FAKE_TOKEN)

    with pytest.raises(ArtError) as raised:
        generate_image(credentials, MODEL, "p", 1, opener=opener_raising(error))

    assert_hides_credentials(raised, FAKE_TOKEN, "FAKE-ACCOUNT-123", "https://")


def test_invalid_json_is_an_error():
    def opener(request, timeout):
        return io.BytesIO(b"not json")

    with pytest.raises(ArtError) as raised:
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=opener)

    assert "invalid JSON" in str(raised.value)


def test_missing_image_is_an_error():
    with pytest.raises(ArtError):
        generate_image(CREDENTIALS, MODEL, "p", 1, opener=opener_returning({"success": False, "errors": [{"message": "bad prompt"}]}))
