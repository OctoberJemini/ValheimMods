# Fish Attractor & Traps

Two buildable fishing pieces: bring live fish to your dock, or catch fish directly in a manually baited trap.

## Bug reports and source code

[Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) | [Request a feature](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml) | [Source code](https://github.com/OctoberJemini/ValheimMods/tree/main/mods/Fish_Attractor_And_Traps) | [Browse bug reports](https://github.com/OctoberJemini/ValheimMods/issues)

We support only the latest release of this mod and Valheim. Please update both before reporting a problem. Choose **Fish Attractor & Traps** in the report form and include reproduction steps and relevant logs or screenshots.

## Latest update — 2.0.5

Corrects the hammer-menu icons for the fish trap and attractor. The icons retain their actual shapes and native wood texture, with neutral lighting, matte shading and higher-resolution rendering. The result has been visually confirmed in game. Placed models and fishing rules are unchanged. Includes the local-host message-reader fixes from 2.0.1.

## Upgrading from 1.x

**2.0 is a gameplay balance overhaul.** Traps now respect discovery, Fishing skill and local stock. Update the server and all players together; 1.x and 2.x cannot play together. Existing trap pieces and their contents remain, but their old short timers are replaced by a full first attempt under the new rules. Discoveries start fresh: previously owned fish do not count, and buying fish does not bypass progression. Your original attractor keeps its existing behavior and settings.

## Fish Trap — progression and shared fishing grounds (2.0)

Build a wooden cage from the Hammer menu near a Workbench: **20 Wood, 10 Fine Wood, 10 Bronze Nails**. Place it on a low dock or shoreline with water at least **1.5 metres deep within 3 metres**, with its base within 4 metres of the water surface. Open its six-slot inventory to load fishing bait and collect catches. The default stored-fish limit remains 20.

### Discover fish, then bring production home

- Undiscovered species can be trapped in their native biome with the correct bait.
- Catch a species with a fishing rod, or collect a genuine catch from a trap, to unlock that species for trapping in any biome. Suitable water and the correct bait remain necessary.
- Unlocks are personal and saved with your character. The trap uses its builder's discoveries and Fishing skill, regardless of which computer owns the network object.
- Simply buying, trading, possessing, or depositing fish does not unlock a species. Only credited trap production collected through the normal inventory transfer paths qualifies. Old catches are not inferred from item discovery; catch or collect a new fish after installing this version.
- Collecting from traps does **not** grant Fishing skill. Active rod fishing improves trap efficiency.
- The character who collects a fish receives its discovery. Collecting from a friend's trap does not also update that friend's discoveries.

Bait-to-fish matches are read from native fish definitions. Native habitats are read from loaded spawn tables; Tetra uses the Mountains biome because it is a cave location spawn. Modded fish without a recognized habitat need a rod-catch unlock first. Habitat tables refresh as the world loads. Biome eligibility does not attempt to reproduce every natural spawning condition, such as time of day or individual cave placement.

If a bait matches multiple species, only eligible species that fit in storage are considered. The first recognized bait stack in the inventory is used. If it cannot catch anything here, change/remove it to use another bait.

### Bait reference

| Bait | Possible species |
|---|---|
| Fishing Bait | Perch or Pike |
| Mossy Fishing Bait | Trollfish |
| Sticky Fishing Bait | Giant Herring |
| Cold Fishing Bait | Tetra |
| Stingy Fishing Bait | Grouper |
| Heavy Fishing Bait | Tuna or Coral Cod |
| Misty Fishing Bait | Anglerfish or Pufferfish |
| Frosty Fishing Bait | Northern Salmon |
| Hot Fishing Bait | Magmafish |

These are the bait definitions in the game version used to build this release; the mod reads installed definitions at runtime. Each species must also meet the native-biome or personal-discovery rule. Raw meat feeds the attractor, not the trap. No rod, nearby live fish or companion is required for the trap's native-biome catches.

### Fishing skill, timing and bait loss

| Builder's Fishing skill | Time per attempt | Bait lost with no catch |
|---|---:|---:|
| 0 | 15 minutes | 25% |
| 50 | 12 minutes | 15% |
| 100 | 10 minutes | 5% |

Values interpolate between these points. For example, a new character can place a correctly baited trap in a species' native biome, collect its first genuine catch to learn that species, then use that bait back at their home dock. A successful attempt consumes one bait and adds one fish. A failed attempt consumes one bait and adds nothing. Open containers, missing bait, invalid water, ineligible species, full storage, and depleted fishing grounds do not consume bait.

The first attempt uses the full normal interval. Saved pending attempts cannot be rerolled by reopening the trap or changing network owners. A delayed completed attempt is followed by a full interval, with no accumulated catch-up burst. The regular 15-second check and network acknowledgment can add a small delay.

### Competition within 20 metres

Each trap checks actual catch locations within **20 horizontal metres**, including catches made by other players' traps. A local area has **three catch slots**. Each successful catch occupies one slot for **30 minutes**, then that slot recovers. Failed bait attempts do not deplete stock. Catch locations with an elevation difference of 100 metres or more do not compete, separating typical cave interiors from surface water.

Nearby traps share these slots rather than multiplying production indefinitely. Depleted traps pause with **“Fish are scarce - waiting for recovery.”** Rebuilding a trap in the same area does not reset the saved catch history. This is a local competition radius, not a transitive chain: distant traps are not linked solely by a third trap between them.

Hover text shows the builder, last reported Fishing skill, local stock, nearby loaded traps, bait-loss chance and timing. Nearby count includes loaded traps that have checked in recently, even if some are currently paused. Stock is based on recent catches, not the number of traps alone. Several traps in the same immediate area can use the three initial slots faster, but cannot create extra slots; spreading stations beyond the competition radius allows separate fishing grounds. A single skill-0 trap averages roughly three fish per hour before other interruptions; skill 100 averages roughly 5.7. These are long-run estimates, not guaranteed yields.

### Ownership and persistence

The builder is the gameplay owner. The owner must join once with this version so the host can record their fishing profile. While they are offline, traps use their last reported skill and discoveries. Online profiles refresh about every 10 seconds. Rebuilding under another character changes the builder; simply opening the inventory does not.

Production still requires a loaded area. World time controls attempts and recovery; the mod does not keep zones active or produce while the world is closed. The host arbitrates shared stock and persists pending catch receipts before sending them to a trap's network owner.

Back up `BepInEx/config/OctoberJemini.FishTraps/world-<worldUID>.json` and its `.bak` **together with the world save**. This host-side ledger stores depletion, owner snapshots and pending attempts. Copy it when moving hosts. Restore matching saves; mismatched receipt counters or an unreadable ledger pause production rather than guessing. Character discovery uses the `oct_fishtrap_discovered_v1` custom-data entry.

The old “Seconds between catches” and “First catch delay” trap settings are no longer used. Version 2.0 uses a fixed skill curve, 20m competition range, three slots and 30-minute slot recovery. Stored-fish limit and existing attractor settings remain available.

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

Configure the attractor production interval and first-fish delay, trap storage limit, attractor meat capacity, nearby live-fish cap, shared minimum water depth, and additional raw meats. Settings appear after the relevant pieces initialize. Restart after editing configuration.

## Installation and multiplayer

Install with a Thunderstore-compatible mod manager. **BepInExPack Valheim** and **Jotunn** are required. For manual installation, place `FishAttractor.dll` in a folder under `BepInEx/plugins`.

Install matching versions on **every client and the server**. Trap production is coordinated by the host and applied by the piece's network owner while its area is loaded. Returning to an overdue station can produce one fish, followed by the normal interval; there is no accumulated catch-up burst. The mod does not keep unloaded areas active or produce while the world is closed.

If upgrading from the privately shared prototype, remove its old `FishAttractor.dll` before installing through a mod manager, so only one copy loads. Existing pieces retain their prefab IDs and saved data.

## Compatibility and removal

The trap uses a normal container. Companion jobs and storage automation mods may deposit items, remove bait or collect catches from it; this release has no companion exclusion rule. Keep it outside their storage search area if needed.

Version 2.0.0 was built against Valheim 1.0.21 and Jotunn 2.30.2. Compilation and 190 offline policy checks passed. Tests cover habitat policy, discovery eligibility, skill curves, depletion, recovery, receipt replay and serialization. No gameplay or multiplayer test of this redesign has been performed. In-game behavior, including normal/Take All transfers, owner changes, reconnects, host restarts and save recovery, remains unverified. Please back up saves before updating and report issues with logs. Storage automation that bypasses normal inventory transfers does not grant discovery.

Dismantle these custom pieces and collect their contents before uninstalling. Removing the mod while custom pieces remain can cause those pieces to disappear when the world loads.

## Credits

Created for OctoberJemini with Codex. Code and upload artwork were made with AI assistance. In-game models use vanilla Valheim timber assets; game assets are not distributed in this package.
