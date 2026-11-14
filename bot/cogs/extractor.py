

from __future__ import annotations

import logging

import discord
from discord import app_commands
from discord.ext import commands

from bot.views.extractor_view import ExtractorView

logger = logging.getLogger("bot.cogs.extractor")


class ExtractorCog(commands.Cog):
    """Cog housing the /show command and its embed + view."""

    def __init__(self, bot: commands.Bot) -> None:
        self.bot = bot

    @app_commands.command(
        name="show", description="เปิดหน้าต่างดึงข้อมูลจากลิงก์เว็บไซต์หรือ API"
    )
    async def show(self, interaction: discord.Interaction) -> None:
        try:
            embed = discord.Embed(
                title="Source Extractor",
                description="กดปุ่มด้านล่างเพื่อกรอกลิงก์เว็บไซต์หรือ API",
                color=discord.Color.blurple(),
            )
            view = ExtractorView()
            await interaction.response.send_message(embed=embed, view=view)
        except Exception:  # noqa: BLE001
            logger.exception("Failed to send /show embed")
            if not interaction.response.is_done():
                await interaction.response.send_message(
                    "❌ Sorry\nไม่สามารถดึงข้อมูลจากลิงก์นี้ได้",
                    ephemeral=True,
                )


async def setup(bot: commands.Bot) -> None:
    await bot.add_cog(ExtractorCog(bot))
