# OctoberJemini store artwork

The primary reference is the original **Simple Item Upgrade Preview** artwork. The 1.0.0 and 1.0.1 image pixels are identical. `oct-brand-master.png` is the common background and OCT footer; use it to preserve branding, with each mod illustration above it. `brand-spec.json` records the bounds.

At 256×256, the OCT wordmark occupies x=44, y=169, width=169, height=68. Every corrected icon copies the original footer rows 165–255 exactly. Subjects retain their original illustrations, isolated from their previous backgrounds and composited without redrawing. Keep subjects above row 155 and within the canvas. Export opaque 256×256 PNGs.

On 2026-11-18, ten existing packages received artwork-only patch releases. Their live ZIPs were downloaded after publication and checked against the staged icon bytes and every DLL hash. The original Simple Item Upgrade Preview icon remains unchanged. See `published-2026-11-18.json` for versions and links, and `thunderstore-published-2026-11-18.jpg` for the published listing.

In-game menu/map sprites do not use the OCT wordmark. Portal Atlas is still a local prototype and was not included in these public package releases.
