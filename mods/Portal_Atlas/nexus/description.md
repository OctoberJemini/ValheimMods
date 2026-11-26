# Portal Atlas Map

**Adventure farther. Get home faster.**

Your next destination should be a choice, not a hunt for the right portal tag. Buy the Portal Atlas Map from Haldor and carry your travel network with you: open the Atlas, find a destination, and go.

Search by name or biome, favourite the places you return to, and build stone Atlas pillars at your outposts. Whether you're heading home with supplies or meeting friends across the world, your destinations are together in one familiar map view.

## What you get

- **Portable travel:** carry the Atlas and open it from your inventory or hotbar.
- **A searchable destination list:** find normal portals and Atlas pillars by name or biome.
- **Favourites and naming:** star regular stops and give destinations meaningful names.
- **Portals without tag pairing:** walk into a normal portal to choose a destination, even without owning the Atlas item.
- **Buildable Atlas pillars:** weathered stone, carved details, a Dvergr orb crown, blue sparks, local lighting and a quiet hum. The lit front triangle marks the arrival side.
- **Separate map controls:** your ordinary map remains your ordinary map; Atlas travel happens in the Atlas view.

## Getting started

1. Install the requirements below and Portal Atlas Map, then restart Valheim.
2. Visit **Haldor** and buy the map for **5,000 coins** by default. This is a one-time item purchase, not a per-trip charge.
3. Carry it in your inventory. Right-click it, or put it in a numbered hotbar slot and press that slot's key. It does not need to be equipped.
4. Search for a destination, then click its name or red map marker to travel. **Travel is immediate; there is no confirmation button.**
5. Use the star to favourite a stop and the pencil to rename it.

Without the item, walk into a normal portal to open the picker. Stay beside that portal while choosing a destination. Buying the Atlas adds remote access and unlocks pillar construction.

## Build your own destination

Owning an Atlas unlocks its pillar in the hammer's **Misc** category for that character. Each pillar costs:

- **20 Stone**
- **1 Wisp**
- **1 Surtling Core**

No workbench, fuel or matching portal name is required. Rotate the pillar so the glowing front triangle faces a clear arrival area. Pillars receive automatic names and can be renamed. Dismantling a pillar removes its destination.

The orb crown is decorative. It does not clear mist or create a ward. Its light reaches approximately three metres and its spatial hum fades within six metres. Existing pillars receive the updated appearance when loaded.

## Travel rules and balance

The 5,000-coin Atlas buys convenience, not unrestricted cargo transport. Native item teleportability and applicable portal/world restrictions still apply. Portals that natively permit all items retain that permission.

All normal portals and Atlas pillars in the current world can be listed, including undiscovered destinations. This does not reveal unexplored terrain. Travel stays within the current world.

Portal Atlas changes normal portal entry to the destination picker. Vanilla matching-tag pairing is not used while the mod is active. Atlas display names preserve the underlying vanilla portal tags for use if you remove the mod later.

## Requirements and installation

Required dependencies for this release:

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) — package version **5.4.2360**.
- [Jotunn, the Valheim Library](https://www.nexusmods.com/valheim/mods/1138) — **2.30.2** or a compatible newer release.

**Manual installation:** close Valheim, install the dependencies, and copy the ZIP's `BepInEx` folder into the Valheim installation folder, merging folders when prompted. Keep only one copy of `PortalAtlas.dll`; do not install a second copy alongside an existing Thunderstore installation. Restart the game afterward.

If you use a separate mod-manager profile, install into that profile's BepInEx folder instead. The identical release is also available on [Thunderstore](https://thunderstore.io/c/valheim/p/OctoberJemini/Portal_Atlas_Map/).

For multiplayer, the host/dedicated server and every client need the mod and its dependencies. Keep versions consistent. **Version 0.2.12 has automated checks and local in-game visual testing; broader multiplayer and dedicated-server validation remain incomplete.**

Epic Loot and Auto Mapping are not required. [Auto Mapping](https://thunderstore.io/c/valheim/p/OctoberJemini/Auto_Mapping/) is an optional companion for automatically recording discoveries. Screenshots were captured in a modded game; unrelated inventory items, HUD elements and world pins are not included with Portal Atlas.

## Settings, backups and removal

Haldor's Atlas price is configurable and defaults to **5,000 coins**. The first launch creates `BepInEx/config/OctoberJemini.PortalAtlas.cfg`. Under `[Trader]`, set `AtlasPrice` to any whole-number price from **1 to 100,000 coins**.

```ini
[Trader]
AtlasPrice = 5000
```

**No configuration file is required to use the mod.** If the file or the `AtlasPrice` setting is missing, the mod uses **5,000 coins** and recreates the missing configuration on the next launch. Close the game before editing or deleting configuration.

Back up your character, world and `BepInEx/config/OctoberJemini.PortalAtlas/` folder before updates or moving hosts. That folder stores world labels/counters and per-character/world favourites. Copy it with your world when moving to another host.

Before uninstalling, dismantle Atlas pillars and remove Atlas map items, save, and close the game. Then remove the plugin. Ordinary portals remain and return to vanilla pairing using their underlying tags.

## Need help?

[Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) · [Request a feature](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml) · [Source and full guide](https://github.com/OctoberJemini/ValheimMods/tree/main/mods/Portal_Atlas)

Please update both the mod and Valheim before reporting a problem. Include your mod version, single-player/hosted/dedicated setup, reproduction steps, relevant other mods, and a short BepInEx log excerpt. Remove credentials and personal details before posting logs.

If the map is missing from Haldor, check that Portal Atlas and Jotunn loaded. If the pillar is missing, obtain an Atlas first and check the hammer's Misc category. If travel is blocked, check the in-game message and your carried items; the ordinary M map does not initiate travel.

## Credits and AI disclosure

Created and maintained by **OctoberJemini**, with **OpenAI Codex** used extensively for programming, troubleshooting, testing support and documentation. Original icons and texture artwork include AI-generated or AI-assisted material. This listing's text was prepared with AI assistance and reviewed against the mod source and release documentation.

The gameplay gallery uses actual in-game screenshots, not generated gameplay images. The OCT thumbnail is promotional artwork. Native game effects and materials are referenced from the installed game; Valheim game binaries are not distributed in this package. This project is not affiliated with Iron Gate.
