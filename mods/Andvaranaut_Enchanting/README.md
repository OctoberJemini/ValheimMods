# Andvaranaut Enchanting

**Keep the treasure finder. Make the ring your own.**

Andvaranaut deserves a place in your build, not just a trip out of your inventory when treasure is nearby. **Andvaranaut Enchanting** lets you enchant, disenchant and re-enchant Epic Loot's treasure-finding ring through the normal crafting interface, while its signature ability stays permanently attached.

Keep a useful roll, strip a disappointing one, or invest in a higher rarity when your enchanting setup allows it. Your ring can grow with your character without sacrificing what makes Andvaranaut special.

## Features

- Enchant and disenchant **Andvaranaut** with Epic Loot's normal actions.
- Keep its native **Treasure finder** after rolled enchantments are removed.
- See a permanent unique-ability note in the tooltip.
- Start with a plain ring: an untouched stock ring no longer stays purple merely because of its original special marker.
- Use the usual material costs, station requirements, unlocked rarities and random-effect rules.

This is an Epic Loot compatibility mod, not a new ring recipe, free upgrade system, stronger detector or extra equipment slot. It affects Andvaranaut only. Wishbone and other equipment keep their normal rules.

## How to use it

1. Obtain Andvaranaut through your existing Epic Loot setup.
2. Select it in Epic Loot's enchanting interface.
3. Choose an unlocked rarity and pay the normal cost.
4. Equip the ring to use its rolled character bonuses.
5. To try again, disenchant it and then enchant it at an available rarity. The native finder remains.

**Disenchanting still removes rolled magic.** The permanent finder does not protect ordinary rolls from an action that intentionally removes them.

## Finder versus enchanted bonuses

The native finder keeps its original range, target rules and activation behavior. Normally you equip the ring to activate it. With the separate **Auto Wishbone & Andvaranaut** mod, native detection can work while the ring is carried.

Rolled character bonuses still follow Epic Loot's equipment rules, including supported extra equipment slots. Carrying the ring does not grant those bonuses. Item-specific effects such as reduced weight can still affect the carried item itself.

## Existing rings and removal

Conversion is automatic. The mod removes only Epic Loot's special Andvaranaut magic marker, and only after verifying that the native finder exists separately.

An otherwise untouched stock ring becomes unenchanted. Already modified rings retain other rolls and associated rarity, sockets, set/legendary identity and augmentation/tempering metadata during conversion. Quality, durability and unrelated custom item data are not changed. Deliberately disenchanting afterward follows Epic Loot's normal behavior.

Back up your character before first installation. Conversion changes saved enchantment data. The original magic data is retained once in the item's `oct_andvaranaut_original_magic_v1` custom-data field for recovery investigation. Removing the plugin does not automatically undo conversion; there is no automatic restore command.

## Installation and compatibility

Install with Thunderstore Mod Manager or r2modman for automatic dependencies. For manual installation, install BepInExPack Valheim, Jotunn and Epic Loot, then copy this package's `BepInEx` folder into Valheim. Restart after installing or updating. Avoid duplicate copies of `AndvaranautEnchanting.dll`.

**Multiplayer:** install the same version on the server and every player. Matching versions are enforced. No configuration file is required.

Built against Epic Loot 0.15.6 and Jotunn 2.30.2. Mods replacing Andvaranaut or modifying Epic Loot eligibility, item data or enchanting interfaces may need compatibility checks.

## Validation

Local playtesting confirmed rare enchanting, disenchanting back to plain, and enchanting at the tested red tier, with the unique ability retained and no reported duplicate tooltip effects. Offline checks cover marker-removal policy and installed API compatibility.

Dedicated servers, every rarity, save/reload after conversion, and all existing socket or third-party item combinations have not been exhaustively tested.

## Bug reports and feature requests

[Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) | [Request a feature](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml) | [Browse issues](https://github.com/OctoberJemini/ValheimMods/issues)

We support the latest release of this mod and Valheim. Choose **Andvaranaut Enchanting** in the form, or name it in the title if the option is not yet available. Include Valheim, Epic Loot and mod versions, relevant equipment mods, reproduction steps and the relevant BepInEx log error. Mention whether the ring was stock, enchanted or socketed, and whether you were in single-player or multiplayer. Before-and-after tooltip screenshots help.

## Credits

Created for OctoberJemini with Codex. Requires **Epic Loot by RandyKnapp** and **Jotunn**. This is an independent compatibility mod, not an official Epic Loot release. Store artwork created with OpenAI image generation using the established OCT branding.

[OctoberJemini mod source and support](https://github.com/OctoberJemini/ValheimMods)
