# Manual Tar Pickup

## Bug reports and source code

[Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) | [Request a feature](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml) | [Browse source and all mods](https://github.com/OctoberJemini/ValheimMods)

Choose **Manual Tar Pickup** in the report form and include the mod version, Valheim version, steps to reproduce, and relevant logs or screenshots.


Reach into tar and retrieve dropped items with your normal interact key, **E** by default.

Automatic pickup continues to ignore items stuck in tar, so you choose what to pull out. Your normal interaction distance and inventory restrictions still apply. If you rebound Interact, use your chosen key.

## Installation

Install with a Thunderstore-compatible mod manager, then restart Valheim.

For manual installation, install BepInExPack for Valheim and place `ManualTarPickup.dll` in `BepInEx/plugins/ManualTarPickup`.

## How it works

The mod bypasses the tar restriction only in dropped-item manual interaction. It uses the game's normal pickup and network ownership handling. It has no configuration or additional controls.

## Compatibility

Built against the installed Valheim 1.0 game assemblies. Other mods that replace dropped-item interaction may affect compatibility. Each player who wants this behavior should install the mod.

Build and game-code checks completed. In-game tar retrieval has not yet been verified.

## Removal

Remove the mod and restart Valheim to restore the standard tar pickup restriction.

## Credits

Created for OctoberJemini with Codex. Icon generated with OpenAI image generation.

## Source and support

[Source and mod index](https://github.com/OctoberJemini/ValheimMods) · [Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) · [Feature request](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml)

Build instructions: [BUILDING.md](../../BUILDING.md).
