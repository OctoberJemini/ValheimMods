# Auto Mapping

## Bug reports and source code

[Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) | [Request a feature](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml) | [Browse source and all mods](https://github.com/OctoberJemini/ValheimMods)

Choose **Auto Mapping** in the report form. **We support only the latest release of the mod and Valheim; please update both before reporting a bug.** Include steps to reproduce and relevant logs or screenshots.


Automatically mark the portals, dungeons and landmarks you discover while exploring Valheim.

- Portal markers follow their names and disappear when destruction is confirmed by the game.
- Nearby portal checks reject old records that refer to an unrelated object or position.
- Discovered dungeons and outdoor landmarks stay on your map between sessions.
- Entered locations can be crossed off and faded. The cross marks a visit, not a guarantee that every enemy or chest has been cleared.
- Includes recognition for More World Locations AIO locations when that mod is installed.
- Adjust portal and landmark icon sizes and visited-marker opacity.

## Temple visit fix in 1.1.8

Fuling Temple variants now use their full exterior footprint (up to 20 metres) when detecting a visit. Walking through the building should add the normal faded X without requiring you to stand on a tiny spot at its centre. Existing markers and saved exploration records are retained. Revisit a previously affected temple to trigger its automatic visit mark; this update does not retroactively guess which locations you entered.

## Installation

Install through a Thunderstore-compatible mod manager and restart Valheim. For manual installation, install BepInExPack for Valheim, then put `QuietMap.dll` in `BepInEx/plugins/OctoberJemini-Auto_Mapping`.

The plugin retains its internal QuietMap name for compatibility with existing settings and saved markers. If you already have a local QuietMap installation, replace that DLL rather than keeping two copies.

Disable Digi's Auto Map Pins, AutoPOI and DWMP before using Auto Mapping. Auto Mapping detects these older trackers and stays inactive to prevent duplicate markers. They are not required.

## Settings and saved markers

Settings are generated in `BepInEx/config/OctoberJemini.QuietMap.cfg`. Marker records are stored per world and character in `BepInEx/config/QuietMap/`.

Portal and location tracking can be enabled independently. Cross off visited locations controls automatic visit marks. Additional dungeon prefab names can be supplied in the configuration.

## Portal cleanup

When the game processes a portal's network destruction, its tracked marker is removed and saved. Travelling away and unloading an area does not count as destruction. Older stale portal records are checked again when you are nearby in a loaded area.

## Compatibility and validation

The temple fix builds against the installed Valheim game and passes 19 recognition, saved-pin and visit-detection regression checks. In-game confirmation of the updated temple radius remains pending. Other mods that manage the same map markers may conflict.

## Credits

Created for OctoberJemini with Codex. Icon created with OpenAI image generation.

## Source and support

[Source and mod index](https://github.com/OctoberJemini/ValheimMods) · [Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) · [Feature request](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml)

Build instructions: [BUILDING.md](../../BUILDING.md).
