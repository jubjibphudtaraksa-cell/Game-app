"""
modals/url_modal.py
--------------------
Modal shown when the user clicks the "ใส่ลิงก์" button. Collects a URL,
validates it, calls the extraction API, and DMs the result file back
to the user.
"""

from __future__ import annotations

import json
import logging
import os
import tempfile
import uuid
from pathlib import Path

import discord

from bot.utils.api_client import request_extract
from bot.utils.validators import is_valid_url

logger = logging.getLogger("bot.modals.url_modal")


class UrlModal(discord.ui.Modal, title="Source Extractor"):
    """Modal collecting a single URL from the user."""

    url_input: discord.ui.TextInput = discord.ui.TextInput(
        label="URL",
        placeholder="https://example.com",
        required=True,
        style=discord.TextStyle.short,
        max_length=2000,
    )

    async def on_submit(self, interaction: discord.Interaction) -> None:
        url = self.url_input.value.strip()

        # --- Basic validation ---
        if not is_valid_url(url):
            await interaction.response.send_message(
                "❌ กรุณาใส่ URL ที่ถูกต้อง",
                ephemeral=True,
            )
            return

        # Acknowledge immediately; extraction + DM may take a moment.
        await interaction.response.send_message(
            "⏳ กำลังดึงข้อมูล...", ephemeral=True
        )

        try:
            result = await request_extract(url)
        except Exception:  # noqa: BLE001 - defensive, request_extract shouldn't raise
            logger.exception("Unhandled error requesting extraction for %s", url)
            await interaction.followup.send(
                "❌ Sorry\nไม่สามารถดึงข้อมูลจากลิงก์นี้ได้",
                ephemeral=True,
            )
            return

        if not result.success:
            await interaction.followup.send(
                "❌ Sorry\nไม่สามารถดึงข้อมูลจากลิงก์นี้ได้",
                ephemeral=True,
            )
            return

        await self._deliver_via_dm(interaction, result)

    async def _deliver_via_dm(self, interaction: discord.Interaction, result) -> None:
        """Write a temp file for the extracted data and DM it to the user."""
        filename = result.filename or "output.txt"
        tmp_path: Path | None = None

        try:
            tmp_dir = Path(tempfile.gettempdir())
            unique_name = f"{uuid.uuid4().hex}_{filename}"
            tmp_path = tmp_dir / unique_name

            if result.type == "api":
                content = json.dumps(result.data, ensure_ascii=False, indent=2)
            else:
                content = str(result.data)

            tmp_path.write_text(content, encoding="utf-8")

            discord_file = discord.File(tmp_path, filename=filename)

            try:
                await interaction.user.send(
                    "✅ ดึงข้อมูลสำเร็จ",
                    file=discord_file,
                )
            except discord.Forbidden:
                logger.info(
                    "Cannot DM user %s - DMs closed", interaction.user.id
                )
                await interaction.followup.send(
                    "❌ ไม่สามารถส่ง DM ได้\nกรุณาเปิดข้อความส่วนตัวก่อน",
                    ephemeral=True,
                )
                return
            except discord.HTTPException:
                logger.exception(
                    "Failed to send DM to user %s", interaction.user.id
                )
                await interaction.followup.send(
                    "❌ Sorry\nไม่สามารถดึงข้อมูลจากลิงก์นี้ได้",
                    ephemeral=True,
                )
                return

            await interaction.followup.send(
                "✅ ส่งข้อมูลไปที่ข้อความส่วนตัวของคุณแล้ว",
                ephemeral=True,
            )

        except Exception:  # noqa: BLE001
            logger.exception("Failed preparing/sending file for URL result")
            await interaction.followup.send(
                "❌ Sorry\nไม่สามารถดึงข้อมูลจากลิงก์นี้ได้",
                ephemeral=True,
            )
        finally:
            # Always clean up the temp file, even if sending failed.
            if tmp_path is not None:
                try:
                    if tmp_path.exists():
                        os.remove(tmp_path)
                except OSError:
                    logger.warning("Could not remove temp file %s", tmp_path)

    async def on_error(
        self, interaction: discord.Interaction, error: Exception
    ) -> None:
        logger.exception("Unhandled modal error: %s", error)
        if interaction.response.is_done():
            await interaction.followup.send(
                "❌ Sorry\nไม่สามารถดึงข้อมูลจากลิงก์นี้ได้",
                ephemeral=True,
            )
        else:
            await interaction.response.send_message(
                "❌ Sorry\nไม่สามารถดึงข้อมูลจากลิงก์นี้ได้",
                ephemeral=True,
            )
