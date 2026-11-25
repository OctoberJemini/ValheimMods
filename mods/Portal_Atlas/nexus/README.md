# Nexus listing preparation — Portal Atlas Map 0.2.12

Status: prepared in Git; not yet submitted or published on Nexus.

## Listing fields

- Name: **Portal Atlas Map**
- Author: **OctoberJemini**
- Version: **0.2.12**
- Summary: **Buy Haldor's Portal Atlas Map to travel to portals and orb-crowned stone pillars. Search destinations, save favourites and name your places.**
- Description: [description.md](description.md).
- Main thumbnail: [the approved OCT icon](../icon.png), unchanged. Use the complete square artwork; do not crop off OCT branding.
- Proposed category: Gameplay (select the closest current Valheim category in the form).
- Suggested discovery terms: portals, fast travel, map, building, quality of life.
- Requirements: BepInExPack Valheim 5.4.2360 and Jotunn 2.30.2 / compatible newer version. Jotunn's Nexus page is mod **1138**. BepInExPack's Nexus page is mod **3605**; check its available version before linking it as the installer source. The official Thunderstore dependency URL is in the description.
- AI disclosure: code, artwork and listing text use AI assistance; some artwork is generated. Apply the corresponding Nexus disclosure fields rather than marking the package AI-free. The four gameplay screenshots are real captures.
- Permissions: retain existing project rights; do not grant a new blanket license. Upload/fork/training and donation preferences are not configured by this preparation.

## Reviewed gallery order

| Order | Image | Caption / use |
| --- | --- | --- |
| Cover | [icon.png](../icon.png) | Approved OCT promotional thumbnail, not an in-game screenshot. |
| 1 | [atlas-travel-picker.png](../screenshots/atlas-travel-picker.png) | Search by name or biome, favourite destinations and click a destination to travel. Lead gameplay image. |
| 2 | [haldor-atlas-shop.png](../screenshots/haldor-atlas-shop.png) | Haldor sells Portal Atlas Map for 5,000 coins by default. It is greyed out because this character has 1,911 coins. Other shop stock is not provided by Atlas. |
| 3 | [atlas-hotbar.png](../screenshots/atlas-hotbar.png) | The Atlas in hotbar slot 8. Use its slot key to open it while carried. A narrow detail image; not a listing cover. |
| 4 | [ordinary-map-pillar-markers.png](../screenshots/ordinary-map-pillar-markers.png) | Passive pillar markers on the ordinary map. Open the Atlas to travel. Other pins/HUD content may come from the modded setup. |

All four screenshots were visually reviewed and retained without generation, retouching or upscaling. Their capture version is not recorded; they document UI behavior still described by 0.2.12, not proof of a fresh 0.2.12 gameplay session. No clean in-game close-up of the new orb crown was found in the Atlas screenshot folder. Do not substitute design renders or the older wisp-pillar concept as current gameplay. Add a genuine current pillar shot later.

## Initial main file

- File name: `OctoberJemini-Portal_Atlas_Map-0.2.12.zip`.
- File display name: **Portal Atlas Map 0.2.12**.
- File category: **Main**.
- Short file description: **Portal Atlas Map 0.2.12: orb-crowned pillars with blue sparks, local light, a quiet hum and weathered stone. Includes the portable destination picker.**
- Use the exact tested Thunderstore ZIP, not a rebuilt DLL or the private-test ZIP.
- Package SHA256: `a2aeb28991cb7e890208c35ba236c8a64a47792e5d1bd3cf539121758016df6e`.
- Plugin SHA256: `aa6c33d784c90f2fb68063ca7c41b0cda681c5afeeba641f9cdc25194a7b3479`.
- Runtime source commit: `25bbda2f9f670989caa715710547be13327839b0` (no Atlas runtime changes in this Nexus preparation).
- Release notes: the 0.2.12 section of [CHANGELOG.md](../CHANGELOG.md).

The ZIP's manifest, README, changelog and icon were validated against committed source, and the ZIP contains only the expected plugin and package files. Its existing README includes historical wisp wording followed by the current orb-crown section; the Nexus description consistently describes the current crown. Preserve the tested archive for this release; clean that historical wording during a future package update.

## Upload and pipeline handoff

1. Create the initial Nexus Valheim mod page with these fields, approved icon and gallery. Keep it unpublished until the page and initial file are ready.
2. Add the initial main file and requirements, and review Nexus permission/disclosure settings.
3. Record the assigned mod ID and API file/group ID in `tools/release/catalog.json` under `Portal_Atlas_Map`. These remain null until Nexus assigns real IDs.
4. Commit and push that mapping. Do not guess IDs.
5. Future file updates use the manual **Publish tested mod** GitHub Actions workflow. Select `nexus` when the same version is already on Thunderstore. A normal Git push does not publish.

See [the shared release guide](../../../docs/RELEASING.md). Do not re-upload 0.2.12 as a new Nexus version if the initial main-file upload already created it.

Nexus policy reference reviewed during preparation: https://help.nexusmods.com/article/180-ai-policy-changes-2026
