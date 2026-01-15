# Puppeteer 1.5 by veloxcity

*RimWorld meets Twitch - Let your viewers control colonists live!*

[![RimWorld 1.5](https://img.shields.io/badge/RimWorld-1.5-blue.svg)](https://rimworldgame.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

This is an updated fork of [Puppeteer](https://github.com/pardeike/Puppeteer) by Andreas Pardeike, updated for **RimWorld 1.5**.

## What is Puppeteer?

Puppeteer allows streamers to give control of specific colonists to their Twitch viewers. Viewers can:

- View their colonist's stats, health, and inventory
- Customize appearance and clothing
- Set schedules and work priorities
- Issue commands and interact with the map
- All through a web browser in near real-time!

## Requirements

- RimWorld 1.5
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
- [TwitchToolkit](https://steamcommunity.com/sharedfiles/filedetails/?id=1718525787) (for Twitch integration)

## Installation

1. Download the latest release
2. Extract to your RimWorld `Mods` folder
3. Enable in mod list (load after Harmony and TwitchToolkit)

## For Streamers

1. Install the mod and start RimWorld
2. Go to https://puppeteer.rimworld.live
3. Log in with Twitch
4. Go to Settings → Streamer and create a game token
5. Download the token file and place it in:
   ```
   %AppData%\..\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config
   ```
6. Configure your stream info and put your game online
7. Right-click colonists in the colonist bar to assign viewers

## For Viewers

1. Go to https://puppeteer.rimworld.live
2. Log in with Twitch
3. Wait in the Lobby for a streamer's game
4. Once assigned a colonist, you can start controlling them!

## Respawn System

Puppeteer includes a respawn system for viewer-caused deaths:

- **Tickets**: Given at game start, consumed on respawn
- **Portal**: Place in your base for respawns to work
- **Cooldown**: Player interactions start a cooldown - deaths during cooldown are permanent
- **No tickets or portal = permanent death**

## Changes from Original

- Updated for RimWorld 1.5 API changes
- Fixed compatibility issues with new game systems
- Various bug fixes and improvements

## Credits

- **Original Author**: [Andreas Pardeike](https://github.com/pardeike)
- **1.5 Update**: [veloxcity](https://github.com/veloxcity)

## Links

- [Original Puppeteer](https://github.com/pardeike/Puppeteer)
- [Puppeteer Website](https://puppeteer.rimworld.live)
- [Discord](https://discord.gg/mG5D923)

## License

MIT License - See [LICENSE](LICENSE) for details.
