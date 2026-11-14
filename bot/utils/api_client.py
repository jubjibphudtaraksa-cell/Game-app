"""
utils/api_client.py
--------------------
Thin async HTTP client for talking to the Source Extractor FastAPI service.
"""

from __future__ import annotations

import logging
import os
from dataclasses import dataclass
from typing import Any, Optional, Union

import httpx

logger = logging.getLogger("bot.api_client")

API_BASE_URL = os.getenv("API_BASE_URL", "http://127.0.0.1:8000")
REQUEST_TIMEOUT = float(os.getenv("REQUEST_TIMEOUT", "15"))


@dataclass
class ExtractApiResult:
    success: bool
    type: Optional[str] = None
    filename: Optional[str] = None
    data: Optional[Union[str, dict, list]] = None
    message: Optional[str] = None


async def request_extract(url: str) -> ExtractApiResult:
    """
    Call POST /extract on the API service with the given URL.

    Never raises - all failure modes are normalized into a
    success=False ExtractApiResult so callers can handle uniformly.
    """
    endpoint = f"{API_BASE_URL.rstrip('/')}/extract"

    try:
        async with httpx.AsyncClient(timeout=REQUEST_TIMEOUT) as client:
            response = await client.post(endpoint, json={"url": url})
            response.raise_for_status()
            payload = response.json()
    except httpx.TimeoutException:
        logger.warning("Timeout calling API for URL: %s", url)
        return ExtractApiResult(success=False, message="Sorry")
    except httpx.HTTPStatusError as exc:
        logger.warning(
            "API returned error status %s for URL %s",
            exc.response.status_code,
            url,
        )
        return ExtractApiResult(success=False, message="Sorry")
    except httpx.RequestError as exc:
        logger.error("Could not reach API at %s: %s", endpoint, exc)
        return ExtractApiResult(success=False, message="Sorry")
    except Exception:  # noqa: BLE001
        logger.exception("Unexpected error calling API for URL: %s", url)
        return ExtractApiResult(success=False, message="Sorry")

    if not isinstance(payload, dict):
        logger.error("Unexpected API payload shape for URL %s", url)
        return ExtractApiResult(success=False, message="Sorry")

    return ExtractApiResult(
        success=bool(payload.get("success", False)),
        type=payload.get("type"),
        filename=payload.get("filename"),
        data=payload.get("data"),
        message=payload.get("message"),
    )
