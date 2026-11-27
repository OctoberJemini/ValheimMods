# Release process

Store releases and source synchronization are separate actions. Both must be completed before reporting that everything is pushed.

## Source first

1. Build and test locally with the installed game and dependencies. Do not upload game DLLs.
2. Review and commit the mod source, required artwork, manifest, README and changelog. Update the root mod index and issue forms when appropriate.
3. Push to GitHub and verify the remote commit. Do not sweep private experiments or unrelated working changes into a public commit.
4. Keep the tested ZIP unchanged. Record its SHA256 and the test results. Packaging validation does not prove that a DLL was compiled from a particular commit; the maintainer must confirm that association during the local build/test step.

## One-time connections

Add repository secrets at https://github.com/OctoberJemini/ValheimMods/settings/secrets/actions:

- `THUNDERSTORE_TOKEN`: Thunderstore > Settings > Teams > OctoberJemini > Service Accounts > Add service account. Copy the once-displayed token directly to GitHub's secret field.
- `NEXUSMODS_API_KEY`: Nexus https://www.nexusmods.com/settings/api-keys > generate personal API key. Copy directly to the GitHub secret field.

Never put tokens in chat, source files, logs, or workflow inputs. The workflow receives them only for publishing. Validation does not require store credentials.

The existing secret names `THUNDERSTORETOKEN` and `NEXUSTOKEN` are also accepted as fallbacks; no renaming is necessary. When keys exist, validation makes read-only connection checks and discards account response data without logging it.

Nexus upload automation updates an existing file lineage. Create each Nexus mod page and initial main file first, including requirements and installation instructions. In `tools/release/catalog.json`, record the **API Unique Mod ID** and **API File ID** shown under the main file's **More actions > Advanced** panel. The API mod ID is not the short game-specific number in the page URL, and the API File ID is not an individual file-version ID. Until mapped, Nexus publishing fails before either store upload. This workflow does not create Nexus pages, upload screenshots or keep long-form page descriptions synchronized.

Portal Atlas Map is connected to [Nexus page 5026](https://www.nexusmods.com/valheim/mods/5026), API Unique Mod ID `15749645079458`, API File ID `8459433`. Version 0.2.12 is already live on both stores; use it only for validation, not another upload.

The initial catalog covers Armor Inscriptions, Worthwhile Treasures, Portal Atlas Map, Fish Attractor & Traps, and Andvaranaut Enchanting. Add other mods with their actual source directory, expected DLL and categories before enabling them in the workflow choices. Linked external source repositories need their own source/tag strategy.

## Stage a tested package

Use a tag such as `Armor_Inscriptions/v0.5.2` at the tested source commit. Never move an existing release tag.

```powershell
git tag Armor_Inscriptions/v0.5.2 <tested-commit>
git push origin Armor_Inscriptions/v0.5.2
Get-FileHash -Algorithm SHA256 outputs/OctoberJemini-Armor_Inscriptions-0.5.2.zip
gh release create Armor_Inscriptions/v0.5.2 --verify-tag --draft --title 'Armor Inscriptions 0.5.2' --notes-file <release-notes-file> outputs/OctoberJemini-Armor_Inscriptions-0.5.2.zip
```

A draft GitHub release holds the exact ZIP for the pipeline. The workflow does not automatically make the GitHub release public. The maintainer can publish it after distribution succeeds. Existing Thunderstore releases can be mirrored to Nexus by selecting only `nexus`.

## Run the pipeline

GitHub > Actions > **Publish tested mod** > Run workflow, using the **main** branch:

1. Select the mod, version, exact ZIP SHA256 and `validate` destination.
2. Confirm the validation run passes. It checks the ZIP hash, tagged manifest/docs/icon, archive paths and expected plugin binary. It does not rebuild or change the package.
3. Once publishing is authorized, run again with `thunderstore`, `nexus` or `both`.
4. Verify the live listings and record the result. Use the same version and ZIP on both stores.

Publishing is manual only: a source push, tag or pull request never publishes a store release. Actions are pinned to commits; Thunderstore's official CLI is pinned to 0.2.4. GitHub requires `contents: write` to access draft release assets; the workflow uses that temporary job token only to download the package and never modifies source or releases. It does not access the desktop or launch Valheim.

## Partial failure

Store uploads are not an atomic transaction. Thunderstore runs first; if Nexus fails afterward, check its file list before retrying **only Nexus**. Do not blindly rerun both destinations or re-upload a successful version. Likewise check the destination after a network timeout: a failed response does not prove that nothing was published. Existing versions are not automatically deleted or archived.

## References

- https://github.com/Nexus-Mods/upload-action
- https://github.com/Nexus-Mods/API-Example
- https://github.com/thunderstore-io/thunderstore-cli

## Linked No Fog Destinations releases

Use **Publish tested No Fog Destinations** for the separately maintained `OctoberJemini/EpicLootNoFogDestinations` repository. Publish its immutable `v<version>` GitHub tag/release with `Epic_Loot_No_Fog_Destinations-<version>.zip` attached, then run the workflow with the exact ZIP SHA256 and `validate`. After validation succeeds and store publication is authorized, use `thunderstore`. The workflow checks package documentation, manifest, artwork and attribution against that tagged source and records the DLL checksum. It uses the existing hub repository secret; source pushes do not publish. Update the hub index after store publication succeeds.
