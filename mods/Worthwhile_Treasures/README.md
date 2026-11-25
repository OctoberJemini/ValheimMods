# Worthwhile Treasures

Make the climb worth it. Find more coins and saleable valuables in world and dungeon treasure chests, with a rare jackpot that announces itself with a short chime.

## Rewards

| Treasure chest | Normal coins | Jackpot coins |
| --- | ---: | ---: |
| Meadows / Black Forest, burial chambers and troll caves | 25–75 | 250 |
| Swamp and sunken crypts | 50–100 | 400 |
| Mountains and frost caves | 75–150 | 600 |
| Plains | 100–200 | 800 |
| Mistlands, including Dvergr dungeon, town, tower and loose-stone chests | 100–300 | 1,000 |
| Ashlands fortress and stone chests | 150–350 | 1,250 |
| Deep North village chests | 200–400 | 1,500 |

Each eligible chest has a 2% jackpot chance. Jackpot coins replace the normal coin allocation. Every enhanced chest also receives one randomly chosen valuable type: 1–3 amber, 1–3 amber pearls, or one silver necklace. Existing non-coin loot remains. Plain original coins are replaced by the new allocation; items with custom data are preserved.

Rewards are generated once and saved with the chest. Reopening does not reroll or refill it. Jackpot chimes play once on a successful opening, accompanied by a message. Taking everything through another route does not award extra coins.

**Newly generated loot only:** chests whose default contents were generated before installing this mod remain unchanged, even if you never opened them. An unopened chest may already have generated its loot when the area loaded. Newly explored map terrain is not a guarantee that a chest is new. This avoids refilling old looted chests or altering player-stored belongings. Player-built storage and resource crates are excluded. Network prefab identity determines the chest type, so numbered dungeon copies are recognized. All `TreasureChest_` prefab variants are eligible. Known biome chest families determine their own tier, including crypts and caves; unfamiliar modded treasure prefabs use the biome at their horizontal position. Dungeon height does not affect the tier. Arbitrary custom storage prefabs without the treasure prefix and quest containers are not automatically included. The Deep North entry matches this installed setup's loot configuration; the mod does not add locations or chests.

## Epic Loot compatibility

Epic Loot is optional. Its gear tables, rarity chances, progression rules and deferred roll timing are not modified. This mod only replaces plain coins and adds saleable valuables. Armor Inscriptions adds its independent rare patch roll after this reward, preserving the enhanced inventory. Neither mod requires the other. Other treasure mods that replace coins may conflict. If the full reward cannot fit, the original inventory is preserved and the bonus is skipped with a log warning. No items are dropped or deleted to make room.

## Installation and removal

Requires BepInEx and Jotunn 2.30.2 or later. Place `WorthwhileTreasures.dll` in a dedicated folder under `BepInEx/plugins`. Install the same version on server and clients for multiplayer. Close Valheim before changing plugin files. Removing the DLL stops future enhancements; already saved rewards remain ordinary game items.

## Testing status

Compiled against the installed game assemblies; external member references verified. Reward range, target exclusions and jackpot calculations checked offline. Live Mistlands testing confirmed enhanced Dvergr chest contents, including 279 coins with a silver necklace and another reported chest with 169 coins and amber. The logged tower chest retained other loot alongside the reward. Ashlands and Deep North rewards, the jackpot sound, and multiplayer ownership still need live testing. The 1.1.0 dungeon fix was confirmed live on newly generated numbered Dvergr mine chests: 266 coins plus a silver necklace and 112 coins plus two amber, with other loot retained. Fresh early-biome dungeon rewards still need live confirmation. It does not claim exhaustive compatibility with custom dungeon storage prefabs.

## Configuration and troubleshooting

Reward amounts and jackpot odds are fixed in this release. Verbose diagnostics are disabled by default. For troubleshooting, set `VerboseLogging = true` under `[Diagnostics]` in `BepInEx/config/OctoberJemini.WorthwhileTreasures.cfg`, then restart. This logs chest identities, positions and contents; review logs before sharing them.

## Bug reports and feature requests

[Report a bug](https://github.com/OctoberJemini/ValheimMods/issues/new?template=bug_report.yml) | [Request a feature](https://github.com/OctoberJemini/ValheimMods/issues/new?template=feature_request.yml) | [Browse issues](https://github.com/OctoberJemini/ValheimMods/issues)

Name **Worthwhile Treasures** in your report or title if it is not yet available in the form. Include your mod and Valheim versions, other loot mods, single-player or multiplayer, chest type, whether the area existed before installation, and relevant BepInEx log lines. A screenshot of the chest contents helps. Please reproduce on the latest release when possible.

## Credits

OctoberJemini. Icon created with OpenAI image generation using the established OCT artwork references; original OCT footer preserved. Chime synthesized by the plugin, with no external audio asset.
