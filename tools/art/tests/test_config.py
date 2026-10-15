import pytest

from mortar_art.config import ConfigError, Credentials, load_credentials, parse_env_file


def test_parse_env_file_handles_comments_and_quotes():
    text = '# comment\n\nCF_API_TOKEN="abc"\nCF_ACCOUNT_ID = xyz \nOTHER=1\n'

    assert parse_env_file(text) == {"CF_API_TOKEN": "abc", "CF_ACCOUNT_ID": "xyz", "OTHER": "1"}


def test_environment_overrides_the_file(tmp_path):
    env_file = tmp_path / ".env"
    env_file.write_text("CF_API_TOKEN=file-token\nCF_ACCOUNT_ID=file-account\n", encoding="utf-8")

    credentials = load_credentials({"CF_API_TOKEN": "env-token"}, env_file)

    assert credentials.api_token == "env-token"
    assert credentials.account_id == "file-account"


def test_missing_keys_are_named_without_values(tmp_path):
    env_file = tmp_path / ".env"
    env_file.write_text("CF_API_TOKEN=abc123\n", encoding="utf-8")

    with pytest.raises(ConfigError) as raised:
        load_credentials({}, env_file)

    assert "CF_ACCOUNT_ID" in str(raised.value)
    assert "abc123" not in str(raised.value)


def test_environment_values_are_stripped():
    credentials = load_credentials({"CF_API_TOKEN": "FAKE-TOKEN-123\n", "CF_ACCOUNT_ID": " FAKE-ACCOUNT-123\r"}, None)

    assert credentials.api_token == "FAKE-TOKEN-123"
    assert credentials.account_id == "FAKE-ACCOUNT-123"


@pytest.mark.parametrize("token", ["FAKE TOKEN-123", "FAKE-TOKEN\n123", "FAKE-TOKEN\x00123"])
def test_values_with_inner_whitespace_or_control_characters_are_rejected_by_key(token):
    with pytest.raises(ConfigError) as raised:
        load_credentials({"CF_API_TOKEN": token, "CF_ACCOUNT_ID": "FAKE-ACCOUNT-123"}, None)

    message = str(raised.value)
    assert "CF_API_TOKEN" in message
    assert "FAKE" not in message
    assert "123" not in message


def test_file_values_are_validated_too(tmp_path):
    env_file = tmp_path / ".env"
    env_file.write_text("CF_API_TOKEN=FAKE-TOKEN-123\nCF_ACCOUNT_ID=FAKE ACCOUNT\n", encoding="utf-8")

    with pytest.raises(ConfigError) as raised:
        load_credentials({}, env_file)

    assert "CF_ACCOUNT_ID" in str(raised.value)
    assert "FAKE" not in str(raised.value)


def test_repr_hides_values():
    credentials = Credentials(account_id="acct-123", api_token="secret-token")

    assert "acct-123" not in repr(credentials)
    assert "secret-token" not in repr(credentials)
