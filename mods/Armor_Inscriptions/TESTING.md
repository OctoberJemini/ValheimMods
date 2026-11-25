# Private 0.4.0 test checklist

- Log loads Armor Inscriptions 0.4.0 and registers the bench without errors.
- Hildir offers all four patches at 500 coins after Eikthyr.
- Hammer > Crafting contains Inscription Bench; recipe is 20 Wood, 10 Leather Scraps, 5 Deer Hide.
- Placed bench resembles the approved table layout and has correct wood/hide textures.
- Press E at the bench. Choose armor first, select an inscription, check owned/required counts, then Apply Inscription.
- Cancel consumes nothing; duplicate inscriptions are disabled.
- Meadows/Black Forest costs 1; Swamp 2; Mountains 3; Plains 4; Mistlands 5; Ashlands 6; Deep North 7.
- Test 7 patches split across two stacks: exactly 7 should be consumed.
- With only 6 patches, Deep North application must be disabled.
- Move away from or destroy bench while menu is open; no remote application should be possible.
- Check independent effects, unequipping, broken armor, normal combat stamina, and hoe/cultivator coverage.
- Upgrade armor, save/reload, and enchant/disenchant with Epic Loot; inscriptions should persist.
- Existing inscriptions from private 0.1.0 should remain active.
- Multiplayer and controller-only navigation still need testing.

Debug prefab names:
OCT_InscriptionBench
OCT_Inscription_runner
OCT_Inscription_leaper
OCT_Inscription_cultivator
OCT_Inscription_builder

- Browse a long armor list with mouse wheel; equipped armor and duplicate named pieces remain identifiable by slot.
- With no patches, armor and inscriptions remain selectable, while Apply is disabled.
- Existing inscriptions display Already applied; check both traders retain other mods' stock.
- Check three sideways thread bundles and scaled-down tacks in game.


- Native inventory opens alongside recipe panel, with Upgrade tab, patch-badged armor entries and patch material counts.
- Test maximum-quality and equipped armor; both compatible inscriptions should be available.
- Test full inventory, holding Shift, canceling the timer, and removing patches/armor before completion. No duplicate armor or extra patch consumption.
- Applying one inscription removes only its entry; the second remains. Both applied removes both entries.
- Verify another normal workbench still has its normal Craft/Upgrade tabs and recipes.
- Check station animation, success sound, and hide color in game.

- Build Leather Rack and Precision Stitching Tools from Crafting. Check textures, icons, collisions, connection effect and the approved layout.
- Base allows tiers 1â€“3; rack allows 4â€“5; rack+tools allows 6â€“7. Tools alone must not bypass the rack.
- Remove an extension during crafting: no patches consumed, no inscription applied.
- Move outside 5m and verify locked recipe reason. Rebuild within 5m and verify unlock.
- Duplicate rack must not substitute for stitching tools; dismantle and verify materials returned.


## Dungeon rewards — new private builds

Offline: 3,748 shared dungeon-name, tier, coin-range, jackpot and patch-probability checks pass. Both DLLs resolve against installed game assemblies. Native Container.Awake was inspected: only the owner generates default loot, and already-generated loot is not generated again.

Live testing remains required:
- Generate fresh burial-chamber, sunken-crypt, troll-cave, frost-cave and Dvergr dungeon chests, including numbered scene instances. Verify biome coin ranges.
- Confirm already-generated and previously emptied chests remain unchanged.
- Run with both mods and Epic Loot: retain food, resources and gear alongside coins; rare patch success adds one item only.
- Check patch generation before/after Eikthyr, full chests, save/reload and multiplayer ownership. Reopening must not retry losing rolls or refill winning rolls.
- Check Ashlands and modded Deep North dungeon treasure variants. Unknown custom containers without the TreasureChest_ prefab prefix are outside automatic coverage.


## 0.5.0 swimming checks pending

- Verify six Hildir patches, 500 coins after Eikthyr, and other mods stock.
- Apply both to helmets at tiers 1-7; verify counts and extension requirements.
- Snorkel: swim and idle at zero stamina without exhaustion drowning. Remove/break helmet and confirm native behavior returns.
- Tidewalker: compare timed straight swims under identical conditions for 25% increase, no repeated accumulation, and no land/boat speed change.
- Verify normal swim skill gains and combat costs.
- Save/reload, trade and quality-upgrade helmets; retain both inscriptions and Epic Loot metadata.


## 0.5.1 cost checks pending in game

- Deep North body armor: Builder and Cultivator each show/consume 1 patch, but require both extensions.
- Remove an extension during crafting: application must stop without consuming patches.
- Deep North legs/helmet retain 7-patch costs; low-tier body armor stays at 1.
