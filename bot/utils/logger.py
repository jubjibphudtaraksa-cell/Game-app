"""
utils/logger.py
----------------
Centralized logging configuration for the bot process.
"""

from __future__ import annotations

import logging
import os


def setup_logging() -> logging.Logger:
    level = os.getenv("LOG_LEVEL", "INFO").upper()
    logging.basicConfig(
        level=level,
        format="%(asctime)s | %(levelname)-8s | %(name)s | %(message)s",
    )
    # Quiet down noisy third-party loggers a bit
    logging.getLogger("discord.http").setLevel(logging.WARNING)
    logging.getLogger("discord.gateway").setLevel(logging.WARNING)
    return logging.getLogger("bot")
