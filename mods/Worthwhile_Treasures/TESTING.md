# Private validation checklist


## Dungeon rewards — new private builds

Offline: 3,748 shared dungeon-name, tier, coin-range, jackpot and patch-probability checks pass. Both DLLs resolve against installed game assemblies. Native Container.Awake was inspected: only the owner generates default loot, and already-generated loot is not generated again.

Live testing remains required:
- Generate fresh burial-chamber, sunken-crypt, troll-cave, frost-cave and Dvergr dungeon chests, including numbered scene instances. Verify biome coin ranges.
- Confirm already-generated and previously emptied chests remain unchanged.
- Run with both mods and Epic Loot: retain food, resources and gear alongside coins; rare patch success adds one item only.
- Check patch generation before/after Eikthyr, full chests, save/reload and multiplayer ownership. Reopening must not retry losing rolls or refill winning rolls.
- Check Ashlands and modded Deep North dungeon treasure variants. Unknown custom containers without the TreasureChest_ prefab prefix are outside automatic coverage.
