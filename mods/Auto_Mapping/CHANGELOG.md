# Changelog

## 1.1.8

- Fixed Fuling Temples failing to receive their automatic visited X when walking through parts of the building. Temple variants now use the full loaded exterior radius, capped at 20 metres, instead of halving the smaller spawn-clearance radius.
- Unloaded temples use the full configured visit radius.
- Other landmarks, dungeon-entry rules and service-location exclusions retain existing behaviour.
- The X records a visit; it does not inspect remaining enemies or loot.

## 1.1.7

- Standardized the package icon background and OCT wordmark placement to match the original OctoberJemini artwork.
- Artwork-only package update; mod DLLs and gameplay behavior are unchanged.

## 1.1.6
- Fixed a saved map pin near a landmark preventing that landmark from appearing. Boss markers and unrelated manual pins no longer hide nearby caves.
- Existing matching entrance pins are reused.

## 1.1.5
- Added prominent bug-report, feature-request and source links to the main mod description.
- Documentation-only package update; mod binaries are unchanged.

## 1.1.4

First public release, retaining the version number from local development.

- Automatic portal, dungeon and landmark tracking.
- Portal cleanup on confirmed network destruction.
- Validate nearby saved portal records by prefab and position as well as network ID.
- Configurable visited-location crossing, opacity and icon sizes.
