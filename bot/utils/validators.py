"""
utils/validators.py
--------------------
Small standalone validation helpers used by modals/views.
"""

from __future__ import annotations

from urllib.parse import urlparse


def is_valid_url(value: str) -> bool:
    """
    Return True if `value` looks like a well-formed http(s) URL.

    Checks:
      - starts with http:// or https://
      - has a network location (domain)
    """
    value = value.strip()

    if not value.startswith(("http://", "https://")):
        return False

    try:
        parsed = urlparse(value)
    except ValueError:
        return False

    return bool(parsed.scheme in ("http", "https") and parsed.netloc)
