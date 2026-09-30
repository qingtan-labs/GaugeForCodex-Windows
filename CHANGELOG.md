# Changelog

## 1.0.0 — 2026-09-30

- Match tray and vector widget-header marks to the application's circular C + terminal identity.
- Use exact taskbar-DPI icon frames for initial load, display/theme changes and update badges; add 28/36/40px frames.
- Use thinner native blur-behind glass with dynamic API probing, documented Acrylic/opaque fallbacks, and respect Windows transparency/high-contrast settings.
- Restore an opaque non-glass tray flyout; use a balanced 58% desktop scrim with deeper inks, migrating both overly thin and white-looking styles while preserving position and balanced custom values.
- Increase small-label size/weight, use opaque dark text, and verify widget text contrast over the dimmest composited background.
- Prefer an actual five-hour window on the main card; show only additional windows in details and warn about low/exhausted weekly quota.
- Remove duplicate quota windows and hide disclosure for single-window/unavailable states, including live transitions.
- Remove persistent blue mouse-focus borders while retaining neutral keyboard focus indicators.
- Add 110 data/security/localization/readability tests, 36 native tray combinations, disclosure/vector/glass checks and updated illustrated guides.
