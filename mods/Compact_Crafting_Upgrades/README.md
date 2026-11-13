# Compact Crafting Upgrades

Fit your forge upgrades into a smaller workshop. Compact Crafting Upgrades reduces the minimum spacing between crafting-station upgrades to **0.5 meters** by default.

Works with forge, workbench, black forge, and other pieces that use Valheim's standard station-extension spacing check. Choose a spacing from **0 to 2 meters**. Zero removes that spacing check; ordinary collision and placement restrictions still apply.

Crafting-station connection distances, build ranges, upgrade levels, recipes, and resource costs are unchanged. An upgrade still needs to be within its station's normal connection distance to provide a bonus.

## Installation

Install through a Thunderstore-compatible mod manager with BepInEx, or copy the included `plugins` folder into your profile's `BepInEx` folder. Restart Valheim.

**Disable CraftingStationTweakz before using this mod.** The loader marks that combination incompatible to avoid conflicting spacing behavior. Other mods that change station-upgrade placement may also conflict.

## Settings

After the first launch, edit `BepInEx/config/OctoberJemini.CompactCraftingUpgrades.cfg` with the game closed, then launch again. A compatible in-game configuration manager can also change the values during play.

| Section / setting | Default | Effect |
| --- | --- | --- |
| General / Enabled | true | Enable the custom spacing check |
| General / MinimumUpgradeSpacing | 0.5 | Minimum spacing in meters, from 0 to 2 |

Disabling or uninstalling the mod restores normal placement checks. This mod does not move existing pieces or add custom pieces to world saves.

## Compatibility and validation

Initial release. Compiled against the installed Valheim assemblies and BepInEx 5. The current game's station-extension method and placement call were inspected. Package structure and icon dimensions were checked. Full in-game placement and multiplayer verification remain pending.

The change runs on the building player's client. Settings are local and are not synchronized or enforced by a server. Use server-approved settings in multiplayer.

## Credits

An independent implementation by OctoberJemini. Code and icon created with AI assistance. No game binaries or third-party mod binaries are bundled. Source code is included in `source/`.

## Source and support

[Source and mod index](https://github.com/OctoberJemini/ValheimMods) · [Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) · [Feature request](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml)

Build instructions: [BUILDING.md](../../BUILDING.md).
