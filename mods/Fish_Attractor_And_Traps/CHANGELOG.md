# Changelog

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
