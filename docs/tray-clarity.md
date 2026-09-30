# Tray clarity refinement

The taskbar entry remains a C + terminal mark, without an outer quota ring,
gradient, shadow or miniature application tile. The C follows the same circular
arc and round-ended terminal strokes as the application icon. Each frame is
hinted independently, with a three-pixel C at 16px and a 3.5px C at 24px, keeping
the brand's outline rather than replacing it with an angular block letter.

`scripts/tray-artwork.cjs` is the editable per-size master. `build-icons.cjs`
generates 11 exact PNG-backed frames per tray ICO: 16, 20, 24, 28, 32, 36,
40, 48, 64, 128 and 256 px. The SVG files are generated 32px previews, not the
source used to downsample all smaller sizes.

`TrayArtwork.TaskbarPixels()` uses the primary taskbar's DPI and Windows'
small-icon metric ([window DPI](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow),
[DPI-specific metrics](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getsystemmetricsfordpi)). Initial load, theme changes, display-setting changes and
update reminders all use that size. A minute-level safety check detects a
missed DPI change. The app never replaces a 16px icon with a 32px badge canvas.

Light taskbars use dark ink; dark taskbars use near-white ink. An available
update adds a compact amber dot, with a separated edge. Icon handles and
previous tray resources are released on replacement.

## Validation

- Strict x64 compilation and the existing quota/update regression suite.
- App-native test covers 36 combinations: nine taskbar-size frames (16–64px),
  light/dark ink and normal/update states, plus 20 consecutive badge redraws.
- Exact frame dimensions, visible opaque pixels and transparent backgrounds.
- The gallery uses the actual ICO frames, not differently resized SVGs.
- Gallery ink contrast is at least 7:1 on its documented light/dark surfaces.

The gallery is an asset audit, not a screenshot of the user's taskbar. Physical
monitor changes and Windows Explorer's rendering are not exhaustively tested.
