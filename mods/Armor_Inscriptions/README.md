# Armor Inscriptions

**Earn your quality of life through progression.** Invest in a tailoring bench, buy embroidered patches from Hildir, and turn your armor into equipment that makes building, planting and travelling more comfortable. As you move into higher-tier armor, more patches and specialized bench extensions make each new set an investment. Epic Loot is optional.

Run with your weapon ready instead of carrying a hammer just to avoid stamina costs. Armor Inscriptions is designed for players who enjoy stamina convenience but want to earn it through coins, materials and equipment progression.

## Balance philosophy

All six patches become available after Eikthyr. Each costs 500 coins. Builder and Cultivator each consume one matching patch at every armor tier. Runner, Leaper, Snorkel and Tidewalker consume one to seven matching patches by armor tier. Both leg or helmet inscriptions on Deep North gear therefore cost 7,000 coins in patches; both body inscriptions cost 1,000 coins. Bench progression still follows the armor tier. Stockpiling is allowed.

Inscriptions belong to the armor you apply them to. Ordinary quality upgrades preserve them; replacing that armor means investing in the new piece. Earlier inscribed armor can still be kept as a travel or work outfit. Bench extensions gate applications to later armor tiers without weakening inscriptions you have already earned.

These are powerful convenience rewards. Free running and jumping also help with escape, positioning and conserving stamina during combat, even though attacks, blocks and dodges keep their normal costs. That benefit is intentional. The balance comes from acquiring and maintaining your equipment progression, rather than requiring you to swap to a building tool to use the reward.

## Build the bench

Find **Inscription Bench** in the hammer's **Crafting** category. Build near a workbench using:

- 20 Wood

- 10 Leather Scraps

- 5 Deer Hide

Materials are recovered when the bench is dismantled. The bench uses native wood/hide/tool materials and meshes assembled into a plank table with legs, braces, lower shelf, leather mat, a rear shelf with boxes, stacked and draped hides, sideways linen bundles, small tacks, needle and flint tool. The build-menu icon is embedded artwork. The furniture has undergone in-game visual review; the latest cosmetic refinements remain subject to testing.

## Buy patches

All six patches cost **500 coins each**, available from Hildir after **Eikthyr** has been defeated in the world. Later bosses do not change the price. Stockpiling is allowed.

| Patch | Armor | Effect while equipped |
| --- | --- | --- |
| Runner | Legs | Running costs no stamina |
| Leaper | Legs | Normal jumping costs no stamina |
| Builder | Body | Hammer placement, removal and repairs cost no stamina |
| Cultivator | Body | Hoe and cultivator actions cost no stamina |
| Snorkel | Helmet | Swimming costs no stamina; prevents exhaustion drowning |
| Tidewalker | Helmet | Swim 25% faster |

## Armor-tier application costs

**Builder and Cultivator always require one matching patch each**, including on Deep North armor.

Runner, Leaper, Snorkel and Tidewalker require these **matching patches**:

| Armor tier | Patches | Purchase value |
| --- | ---: | ---: |
| Meadows / Black Forest | 1 | 500 coins |
| Swamp | 2 | 1,000 coins |
| Mountains | 3 | 1,500 coins |
| Plains | 4 | 2,000 coins |
| Mistlands | 5 | 2,500 coins |
| Ashlands | 6 | 3,000 coins |
| Deep North | 7 | 3,500 coins |

The armor's prefab determines its tier, not its quality or the tool you hold. Each inscription is a separate purchase/application: putting Builder and Cultivator on a Deep North chest requires 1 of each patch and both bench extensions. The installed Deep North heavy, medium and mage chest/leg pieces and helmets are explicitly mapped. Unknown modded armor is blocked instead of silently assigned a cheap tier; optional prefab=count mappings (1–7) can be added to the config.

## Applying inscriptions

1. Carry patches and the intended armor in your player inventory.

2. Press **E / Use** at the Inscription Bench.

3. Select an armor/inscription entry in the native workbench Upgrade menu. Each eligible armor has up to two entries, marked with a patch badge.

4. Review the patch materials and click **Apply Inscription**. The normal crafting timer and station animation run. Completion consumes the patches and removes that inscription entry; the original armor stays equipped or in its inventory slot.

The menu lists icons, names, quality, equipped status and patch counts. It supports patches split across stacks. Insufficient quantity, duplicate inscriptions and unmapped tiers are disabled. The bench must remain within 4 metres and accessible through ward permissions when confirming. Leaving, destroying the bench, dying or teleporting closes the menu.

Using a patch directly from inventory now directs you to the bench. Application consumes the required quantity once. Canceling consumes nothing. Existing 0.1.0 inscriptions remain valid without a surcharge.

The armor tooltip gains an **Inscriptions** section. These are saved item properties, not visible additions to the character's armor model. Both compatible inscriptions may coexist. Ordinary quality upgrades preserve inscriptions. Replacing armor requires applying new patches; the old armor retains its inscriptions if worn again or traded. There is no transfer/refund feature.

## Gameplay and compatibility

Effects apply only to equipped, nonbroken armor. Running and jumping benefits work regardless of held tool, including during combat. Attacks, blocking and dodging retain normal stamina costs. Sneaking, grappling and encumbrance are unchanged. Snorkel removes swimming stamina use and exhaustion drowning while equipped; it does not provide general damage immunity. Tidewalker multiplies swimming movement speed by 1.25, alongside existing movement modifiers, without affecting land speed. Swimming skill gains remain normal. Materials, durability and Eitr costs are unchanged.

Interaction with doors/containers while holding the standard hammer, hoe or cultivator is included without requiring a patch.

Epic Loot is not required. Its rolls and sockets are separate from inscription data. Applying patches does not change quality, durability, crafter data or enchantments. Native quality upgrades explicitly carry inscriptions to the replacement item. Other mods' custom equipment-conversion systems are not universally supported.

### Rare treasure finds

Newly generated world and dungeon treasure chests can contain **one random patch** after Eikthyr has been defeated in that world. The six patch types have equal chances. The roll is independent of gold jackpots and Epic Loot gear; Hildir remains the reliable source.

| Chest tier | Chance of one patch |
| --- | ---: |
| Meadows / Black Forest (including burial chambers and troll caves) | 1% |
| Swamp / sunken crypts | 2% |
| Mountains / frost caves | 3% |
| Plains | 4% |
| Mistlands / Dvergr dungeons | 5% |
| Ashlands | 6% |
| Deep North | 7% |

All `TreasureChest_` prefab variants are eligible. Network prefab identity handles numbered dungeon copies. Known chest families determine their tier; unfamiliar modded treasure prefabs use the biome at their horizontal position. Arbitrary custom storage prefabs without the treasure prefix and quest containers are not automatically included.

Each chest gets only one roll when its default loot generates. Existing chests do not refill, reopening never rerolls, and chests generated before Eikthyr do not gain a later roll. Player-built storage and resource crates are excluded. Full inventories keep their original contents and skip the patch instead of dropping or deleting anything. Patches stack to 20 per type.

Worthwhile Treasures is optional. When both mods are installed, patches roll after the coin update; neither mod changes Epic Loot's deferred gear-roll timing.

## Requirements

Requires BepInEx and Jotunn 2.30.2 or later. Install the same version on multiplayer clients and server.

Disable BasilPanda-NoStamCosts and tonsit-InteractWhileBuilding; their overlapping features are replaced here. Their settings can remain. The DLL and nine icons are packaged together; no external image files are required at runtime.

Config: BepInEx/config/OctoberJemini.ArmorInscriptions.cfg

Optional armor mapping example: SomeModChest=6,SomeModLegs=6

Use consistent tier overrides across multiplayer clients.

## Validation and limits

The current build passed 2,123 policy checks and 116 installed-assembly compatibility/scope checks. Checks cover fixed pricing, armor tiers, extension progression, duplicate protection and split-stack consumption planning.

Earlier builds were confirmed loading the bench and both extensions in game, and furniture has been visually reviewed. Full save/reload, quality-upgrade, Epic Loot, interrupted-crafting, multiplayer and controller checks remain on the test checklist. The dungeon expansion additionally passed 3,748 shared classification, reward-range and probability checks. New chest drops and the two-mod combination still require live testing. This is an early release; applying inscriptions, rare chest drops and the listed compatibility scenarios still need complete live validation.

## Tailoring extensions

Build separately under Hammer > Crafting, with a workbench nearby. Place each extension within 5 metres of the Inscription Bench. Duplicate copies do not stack.

| Extension | Materials | Unlocks |
|---|---|---|
| Leather Rack | 10 Fine Wood, 6 Deer Hide, 2 Lox Pelt, 6 Linen Thread | Plains and Mistlands armor |
| Precision Stitching Tools | 8 Fine Wood, 10 Linen Thread, 4 Asksvin Hide, 2 Flametal | Ashlands and Deep North armor, together with Leather Rack |

The basic bench handles Meadows through Mountains. Extension requirements use the original armor tier, even when Builder or Cultivator costs only one patch. Missing extensions are shown in the recipe details; application stays disabled until they are nearby. The exact extensions are checked again when the timer finishes. Dismantling returns construction materials. Existing inscriptions keep working even without the bench or its extensions.

## Bug reports and feature requests

[Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) | [Request a feature](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml)

Include **Armor Inscriptions**, your mod/game versions, armor prefab and tier, active extensions, patch quantities, other mods, and relevant BepInEx logs. Describe what happened before and after applying the inscription.

## Credits

OctoberJemini. Store icon created with OpenAI image generation using the established OCT branding.

## Swimming inscriptions and fixed work-patch costs

Snorkel and Tidewalker can coexist on one helmet. Snorkel removes swimming stamina use and exhaustion drowning; Tidewalker increases swimming speed by 25%. Both effects require an equipped, unbroken helmet. They retain 1-7 matching-patch armor-tier costs and existing bench-extension requirements, and share the existing rare chest drop roll with the other four types.

Builder and Cultivator cost one matching patch each at any supported armor tier. Bench-extension requirements still follow the original armor tier. Previously applied inscriptions remain valid; earlier spending is not refunded.

The new swimming behavior still needs complete live gameplay validation, including equip/unequip, breakage and save/reload. Automated checks passed; this remains an early release.


## 0.5.2 compatibility fix

Fixes the startup conflict with Impactful Skills that prevented inscription hooks, including Hildir patch stock, from loading. Tidewalker now applies after swimming speed modifiers, preserving skill-based swimming speed. Startup and Hildir stock were verified in game with Impactful Skills installed. Full swimming gameplay validation remains pending.
