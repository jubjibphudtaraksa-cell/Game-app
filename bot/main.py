
from __future__ import annotations

import asyncio
import os

import discord
from discord.ext import commands
from dotenv import load_dotenv

from bot.utils.logger import setup_logging

load_dotenv()
logger = setup_logging()

DISCORD_TOKEN = os.getenv("DISCORD_TOKEN", "")
GUILD_ID = os.getenv("GUILD_ID", "").strip()

INTENTS = discord.Intents.default()
INTENTS.message_content = False  # not needed - we only use slash commands


class ExtractorBot(commands.Bot):
    def __init__(self) -> None:
        super().__init__(command_prefix="!", intents=INTENTS)

    async def setup_hook(self) -> None:
        await self.load_extension("bot.cogs.extractor")

        if GUILD_ID:
            guild = discord.Object(id=int(GUILD_ID))
            self.tree.copy_global_to(guild=guild)
            synced = await self.tree.sync(guild=guild)
            logger.info(
                "Synced %d command(s) to guild %s", len(synced), GUILD_ID
            )
        else:
            synced = await self.tree.sync()
            logger.info("Synced %d global command(s)", len(synced))

    async def on_ready(self) -> None:
        logger.info("Logged in as %s (ID: %s)", self.user, self.user.id if self.user else "?")


async def main() -> None:
    if not DISCORD_TOKEN:
        raise RuntimeError(
            "DISCORD_TOKEN is not set. Please add it to your .env file."
        )

    bot = ExtractorBot()
    async with bot:
        await bot.start(DISCORD_TOKEN)


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        logger.info("Bot shutting down...")
