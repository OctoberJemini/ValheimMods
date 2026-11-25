# Nexus listing preparation — Portal Atlas Map 0.2.12

Status: published on Nexus on 25 November 2026: https://www.nexusmods.com/valheim/mods/5026

## Release connection

- Public Valheim page ID: `5026` (game ID `3667`).
- Nexus API Unique Mod ID: `15749645079458`.
- Nexus API File ID: `8459433` (main-file lineage; not a file-version ID).
- IDs were copied from the uploaded main file's **More actions > Advanced** panel.
- Version `0.2.12` is already live on both stores. Do not upload it again.
- The approved OCT icon is the thumbnail; all four reviewed gameplay screenshots are in the gallery.
- Jotunn is linked as a Nexus requirement and the official BepInExPack Valheim page as an external requirement.
- Generative AI content and media are disclosed. Mod rewards were not enabled.
- Future releases use **Publish tested mod** with a new tested version and checksum; a Git push alone does not publish.

## Listing fields

- Name: **Portal Atlas Map**
- Author: **OctoberJemini**
- Version: **0.2.12**
- Summary: **Your next adventure is a map click away. Buy Haldor's Atlas, raise glowing stone pillars at your outposts, and carry your portal network wherever you roam. Search, name and favourite destinations—without juggling portal tags. Earn the freedom to travel while keeping Valheim's item restrictions intact.**
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
3. Record the assigned API Unique Mod ID and API File ID in `tools/release/catalog.json` under `Portal_Atlas_Map`. Atlas is now mapped using the IDs above.
4. Commit and push that mapping. Do not guess IDs.
5. Future file updates use the manual **Publish tested mod** GitHub Actions workflow. Select `nexus` when the same version is already on Thunderstore. A normal Git push does not publish.

See [the shared release guide](../../../docs/RELEASING.md). Do not re-upload 0.2.12 as a new Nexus version if the initial main-file upload already created it.

Nexus policy reference reviewed during preparation: https://help.nexusmods.com/article/180-ai-policy-changes-2026
