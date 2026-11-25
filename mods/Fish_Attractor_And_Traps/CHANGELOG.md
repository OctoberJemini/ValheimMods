# Changelog

## 2.0.5 — Wood icon rendering correction

- Use neutral ambient light and disable fog during icon capture, restoring scene settings immediately afterward.
- Render 512-pixel matte wood icons with bilinear filtering; no gameplay or placed-model changes.
- Confirmed in game: the trap and attractor icons display correctly with a wood appearance.


## 2.0.4 — Private icon framing correction

- Copy icon geometry in prefab-local coordinates instead of inherited world coordinates.
- Reject fully transparent rendered images instead of assigning blank menu icons.
- Local test build; visual confirmation pending.


## 2.0.3 — Native icon material

- Fix the unavailable Standard shader introduced in 2.0.2, which left inherited beehive and chest icons visible.
- Use the loaded native Wood item material for isolated trap-shaped icon renders.
- Placed models and gameplay remain unchanged. Visual validation pending.


## 2.0.2 — Hammer-menu icon rendering

- Render the trap and attractor icons using isolated display materials with the native wood textures.
- Avoid the native building shader in the icon camera; render both icons at 256 pixels without cached images.
- Placed models, recipes, bait rules, timing, discovery and shared stocks are unchanged.
- Includes the host message-reader corrections from 2.0.1.
- Compiled and API-checked; visual confirmation in game is still pending.

## 2.0.1 — Host message reader fix

- Fixed repeated end-of-stream errors when a local host publishes fishing discoveries and skill.
- Fixed the same message-cursor issue for local trap requests and catch replies.
- No recipe, timing, skill, bait-loss or stock balance changes.

## 2.0.0 — Fishing progression and shared stocks

- Restricted undiscovered fish to their native biome and bait; rod catches and genuine trap collections unlock cross-biome trapping.
- Added builder-based Fishing skill: 15/12/10-minute attempts and 25/15/5% bait-loss chance at skill 0/50/100.
- Added host-arbitrated local stock within 20m: three catch slots, each recovering 30 minutes after use.
- Persisted depletion independently of traps, plus pending catch receipts and owner profiles.
- Added ownership, stock, competition and timing hover text. No bait is spent while blocked or depleted.
- Removed the short first-catch timer; added retry and ownership-transfer receipt handling.
- Left live-fish attractor behavior unchanged. The attractor retains its original timing, meat input and live-fish cap.

## 1.0.1

- Standardized the package icon background and OCT wordmark placement to match the original OctoberJemini artwork.
- Artwork-only package update; mod DLLs and gameplay behavior are unchanged.


## 1.0.0

- Initial public release with separate Fish Attractor and Fish Trap build pieces.
- Manually loaded raw meat attracts live Perch/Pike.
- Fishing bait determines the trap catch; owner-only production, storage limits and persistent inventories.
- Added OCT package artwork and public source/bug-report links.
