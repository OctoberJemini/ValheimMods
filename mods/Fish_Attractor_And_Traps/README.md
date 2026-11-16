# Fish Attractor & Traps

Two buildable fishing pieces: bring live fish to your dock, or catch fish directly in a manually baited trap.

## Bug reports and source code

[Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) | [Request a feature](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml) | [Source code](https://github.com/OctoberJemini/ValheimMods/tree/main/mods/Fish_Attractor_And_Traps) | [Browse bug reports](https://github.com/OctoberJemini/ValheimMods/issues)

We support only the latest release of this mod and Valheim. Please update both before reporting a problem. Choose **Fish Attractor & Traps** in the report form and include reproduction steps and relevant logs or screenshots.

## Fish Trap

Build a slatted wooden cage from the Hammer menu near a Workbench: **20 Wood, 10 Fine Wood, 10 Bronze Nails**.

Place it on a low dock or shoreline with water at least **1.5 metres deep within 3 metres**. The trap's base must be within 4 metres of the water surface. Press **E** to open its six-slot inventory, manually transfer fishing bait into it, and collect the fish from the same window.

- First catch after about **one minute**; subsequent catches every **five minutes**. A 15-second check interval can add a small delay.
- **One fishing bait per fish**, with a default limit of **20 stored fish** across all species.
- Pauses while open, without bait or suitable water, at the stored-fish limit, or without inventory room. Failed catches do not consume bait.
- No fishing rod, companion or nearby live fish required. The trap creates fish items directly in storage.
- If you load several bait types, the first bait stack in its inventory is used.

Fish types follow the installed game's bait definitions:

| Fishing bait | Fish |
| --- | --- |
| Fishing Bait | Perch or Pike |
| Mossy Fishing Bait | Trollfish |
| Sticky Fishing Bait | Giant Herring |
| Cold Fishing Bait | Tetra |
| Stingy Fishing Bait | Grouper |
| Heavy Fishing Bait | Tuna or Coral Cod |
| Misty Fishing Bait | Anglerfish or Pufferfish |
| Frosty Fishing Bait | Northern Salmon |
| Hot Fishing Bait | Magmafish |

Species depend on bait, with **no biome restriction**. Raw meat feeds the attractor; it is not fishing bait for the trap. Inventory contents save normally and drop when the trap is destroyed.

## Fish Attractor

Build a wooden post with a hanging chum basket: **10 Wood, 2 Resin**, at a Workbench.

Place it on a low dock or shoreline beside water at least **1.5 metres deep within roughly 4–8 metres**. The post base must be within 4 metres of the water surface; the hanging basket adjusts to the water height.

Carry raw meat and press **E** to add one piece. The hover text shows which meat will be added. To choose a specific meat, point at the attractor and use that raw-meat item from your hotbar or inventory.

- Holds **20 pieces of meat**.
- First live fish after about **one minute**, then one every **five minutes**, with up to 15 seconds of check delay.
- Creates ordinary **Perch and Pike** in nearby water, even where natural spawns are sparse.
- Pauses at **three nearby fish within 25 metres**. Nearby attractors share that fish count.
- Meat is consumed only after a fish spawns. Unused meat is returned as its original type when dismantled or destroyed.

Accepts raw boar, deer, neck tail, wolf, lox, hare, chicken, serpent, seeker, asksvin, volture and raw fish. Cooked food is excluded. Additional modded raw-meat prefab IDs can be configured.

The attractor creates live fish; you or a companion still need normal fishing equipment to catch them. It does not change companion AI.

## Settings

`BepInEx/config/OctoberJemini.FishAttractor.cfg`

Configure the two production intervals, first-fish delays, trap storage limit, attractor meat capacity, nearby live-fish cap, shared minimum water depth, and additional raw meats. Settings appear after the relevant pieces initialize. Restart after editing configuration.

## Installation and multiplayer

Install with a Thunderstore-compatible mod manager. **BepInExPack Valheim** and **Jotunn** are required. For manual installation, place `FishAttractor.dll` in a folder under `BepInEx/plugins`.

Install matching versions on **every client and the server**. Production runs only on the piece's network owner while its area is loaded. Returning to an overdue station can produce one fish, followed by the normal interval; there is no accumulated catch-up burst. The mod does not keep unloaded areas active or produce while the world is closed.

If upgrading from the privately shared prototype, remove its old `FishAttractor.dll` before installing through a mod manager, so only one copy loads. Existing pieces retain their prefab IDs and saved data.

## Compatibility and removal

The trap uses a normal container. Companion jobs and storage automation mods may deposit items, remove bait or collect catches from it; this release has no companion exclusion rule. Keep it outside their storage search area if needed.

Built against Valheim 1.0 assemblies and Jotunn 2.30.2. Compilation and 51 policy checks passed, and both pieces registered successfully in the local game log. Full multiplayer, ownership transfer and production/save-reload behavior have not yet been verified in-game.

Dismantle these custom pieces and collect their contents before uninstalling. Removing the mod while custom pieces remain can cause those pieces to disappear when the world loads.

## Credits

Created for OctoberJemini with Codex. Code and upload artwork were made with AI assistance. In-game models use vanilla Valheim timber assets; game assets are not distributed in this package.
