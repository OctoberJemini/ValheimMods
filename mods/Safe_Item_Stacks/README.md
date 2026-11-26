# Safe Item Stacks

**Version 1.0.0 · By OctoberJemini**

More room for supplies without stacking your equipment. Safe Item Stacks raises the stack limits of selected materials and utility supplies while leaving weapons, armor, enchantable equipment, and Dyrnwyn items untouched.

## Supported items

All categories default to **50 per stack**:

- Dragon eggs.
- Completed seal breakers (not sealbreaker fragments).
- Swamp keys.
- Lox, asksvin, and moose saddles.
- Mead bases and barley wine base.

Existing separate items can be combined manually. The mod does not rearrange your inventory. Weight, teleport restrictions, recipes, fermentation time, and output quantities are unchanged.

## Barrels still take one base

Fermenters consume **one base per batch**, leaving the remaining bases in your stack. The mod only adjusts maximum stack sizes and leaves the game's loading logic intact. Saddles likewise retain the game's one-item consumption when attached. Other mods that change these interactions retain their own behavior.

## What does not change

Weapons, armor, rings, belts, wishbones, wisplights, tankards, and other equipment are excluded. **Dyrnwyn and every Dyrnwyn fragment are excluded.** Sealbreaker fragments keep their existing stack limit.

Only the supported item selection is changed. The plugin additionally skips items with durability, multiple quality levels, or a type other than material/miscellaneous. It does not override Epic Loot's identity checks or merge equipment with different stats. It does not undo changes another mod makes to unsupported items. Mods that transform normally ordinary supplies into unique custom items require separate compatibility testing.

## Installation

Install **Safe Item Stacks** using a Thunderstore-compatible mod manager and allow it to install BepInExPack Valheim. For manual installation, close Valheim and merge this ZIP's `BepInEx` directory into your game or profile folder.

The installed DLL is `BepInEx/plugins/OctoberJemini-Safe_Item_Stacks/SafeItemStacks.dll`.

Dependencies: BepInExPack Valheim **5.4.2360**. ItemStacks is optional; supported limits are reapplied after its database refreshes. Built against Valheim **1.0.21**.

## Configuration

After the first launch, edit `BepInEx/config/OctoberJemini.SafeItemStacks.cfg` and restart Valheim. No personal configuration file is bundled in the package.

| Setting under [Stacking] | Default | Applies to |
|---|---:|---|
| StackSize | 50 | Dragon eggs |
| SealBreakerStackSize | 50 | Completed seal breakers |
| MeadBaseStackSize | 50 | Mead bases and barley wine base |
| UtilityStackSize | 50 | Swamp keys and saddles |

Allowed values: 2–1000. Existing higher limits from other mods are preserved for the newly supported items; dragon eggs use the configured StackSize directly. Use matching versions and settings on all multiplayer peers. Settings are not automatically synchronized.

## Relationship to Dragon Egg Stacks

**This is a separate mod, not a renamed update to Dragon Egg Stacks.** The original package remains available for users who only want egg stacking.

Safe Item Stacks uses its own plugin ID, `OctoberJemini.SafeItemStacks`, DLL, and settings file. It does not overwrite or import `OctoberJemini.DragonEggStacks.cfg`.

Safe Item Stacks already includes dragon egg stacking, so only this mod is needed for the full feature set. Disable Dragon Egg Stacks when using this mod to avoid competing egg limits. To preserve a preferred old egg limit, copy its numeric StackSize value into the new config after the first launch. Existing inventories do not need item conversion.

## Updating, removing, and troubleshooting

Close Valheim before replacing DLLs. Keep only one copy of SafeItemStacks.dll. Before uninstalling or lowering limits, back up saves and split oversized stacks to fit the resulting limits.

If items do not combine, restart after configuration changes and check for conflicting stacking mods. Existing stacks are not automatically consolidated. If loading a barrel behaves differently, check mods that change fermenter interactions; this mod does not patch them.

## Validation and support

Compiled with warnings treated as errors. Offline checks cover supported items, equipment/Dyrnwyn exclusions, clone names, and higher existing limits. External game/library references resolve against the build environment. The game's fermenter and saddle code were inspected to confirm one-item consumption. This standalone release has not yet been playtested in game.

[Report an issue](https://github.com/OctoberJemini/ValheimMods/issues) | [Mod source and index](https://github.com/OctoberJemini/ValheimMods)

Include the mod version, Valheim version, reproduction steps, other relevant mods, and relevant BepInEx log excerpts.

## Credits

Created for OctoberJemini with Codex. Shop artwork generated with OpenAI image generation using the established OCT branding. Unofficial Valheim community mod.
