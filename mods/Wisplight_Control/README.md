# Wisplight Control

Upgrade your Wisplight through Mistlands progression, hide its floating visuals while keeping mist clearing, and replace the hidden wisp's light with a steady white character glow.

## Bug reports and source

[Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) | [Request a feature](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml) | [Browse bug reports](https://github.com/OctoberJemini/ValheimMods/issues) | [Source code](https://github.com/OctoberJemini/ValheimMods/tree/main/mods/Wisplight_Control)

Only the latest release of this mod and Valheim is supported. Update both before reporting a problem. Choose **Wisplight Control** in the report form and provide reproduction steps and relevant logs/screenshots.

## Progression by design

**Upgrades are intentionally tied to Mistlands progression.** Early upgrades require Refined Eitr, while later upgrades require trophies from tougher enemies. This makes the increased mist-clearing range something you earn as you progress, rather than upgrading straight to maximum with stockpiled materials. Crafting the standard Wisplight remains unchanged.

## Crafting and upgrades

Use the Workbench's **Upgrade** tab for an existing Wisplight.

| Level | Cost for this step | Workbench level | Default mist-clearing radius |
| --- | --- | --- | --- |
| Craft / 1 | 1 Wisp + 1 Silver | 1 | 15m |
| 2 | 20 Wisps + 4 Silver + 10 Refined Eitr | 2 | 20m |
| 3 | 20 Wisps + 8 Silver + 25 Refined Eitr + 1 Gjall Trophy | 3 | 25m |
| 4 | 20 Wisps + 16 Silver + 1 Seeker Soldier Trophy | 4 | 30m |

All upgrades consume their ingredients normally. Each row lists that upgrade's cost, not a cumulative total. Later ingredients do not change initial crafting or its discovery requirements. Existing upgraded Wisplights retain their quality; the costs apply to new crafting/upgrade attempts.

## Visibility controls

Equip your Wisplight, then press **F9**, or open your inventory and click the small **eye button** in the upper-right corner of its equipped slot.

- **Open eye / visible:** normal native wisp movement, lighting and audio. Character glow is off.
- **Crossed-out eye / hidden:** wisp visuals and its native light/audio are hidden, while a steady white light follows your character.
- **Mist clearing stays active in both states**, using the equipped Wisplight's quality.

The keyboard and eye button control the same saved preference. The wisp starts hidden for a new character. Unequip the Wisplight to turn both its light and mist clearing off. The eye appears only on your equipped Wisplight and does not equip, move or split the item. Standard InventoryGrid-based equipment slots are supported, including the local Equipment and Quick Slots layout. Controller navigation remains with the normal inventory grid; the eye is a pointer control.

The shortcut is ignored while typing, in menus/inventory, while viewing the map, during teleporting, and while dead or in a cutscene. Clicking the eye works with the inventory open.

## Settings

`BepInEx/config/OctoberJemini.WisplightControl.cfg`

- **Toggle key:** F9 by default.
- **Level 1 radius / Radius per upgrade:** 15m / 5m by default.
- **Character light radius:** 10m by default.
- **Character light brightness:** 1.8 by default, adjustable from 0 to 3. This controls the replacement glow only while the wisp is hidden.

Edit with the game closed, then restart. The character's visibility choice saves separately from configuration. The glow uses one steady white point light, without a visible flame, orbit or flicker. No global fog, bloom, camera exposure or other light sources are changed.

## Installation and compatibility

Requires **BepInExPack Valheim** and its bundled Harmony. Place `WisplightControl.dll` under `BepInEx/plugins/OctoberJemini-WisplightControl`, then restart Valheim.

Disable **On/Off WispToggle** (`com.Azura.wisptoggle`) and **UpgradeableWisplight** (`mtsukn.upgradeablewisplight`) before use; this mod replaces their overlapping functions. Keep overlapping mods disabled in your mod manager. If replacing a privately shared prototype, remove its old WisplightControl.dll before installing through a mod manager, so only one copy loads.

For multiplayer use matching copies on the clients/server and consistent settings. Visibility and range are stored on the existing native wisp's network data. No new item, prefab or RPC is introduced. Wisp torches, Dvergr structures and unmarked vanilla wisps are unchanged.

## Verification and removal

Version **1.0.0** is the initial public release. It compiles with warnings as errors against the installed Valheim assemblies. Twenty-three checks run through the actual native `Piece.Requirement.GetAmount` with the scoped Harmony patch, covering every ingredient/quality combination and unrelated-recipe isolation. Fifteen policy checks cover range, glow and saved visibility behavior. Initial discovery is checked against the installed native code's zero-base-amount handling.

The new recipe's in-game upgrade display/consumption, clickable equipment overlay, lighting transitions and multiplayer still require verification in a running game. Removing this mod restores vanilla's quality cap; higher qualities may be clamped when items reload.

## Credits

Created for OctoberJemini with Codex. Code and OCT store artwork were made with AI assistance. Native Valheim assets remain the property of their owners and are not included in this package.
