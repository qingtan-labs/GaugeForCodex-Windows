# 1.0.0 interface review / 界面审查

## Design contract

- Product-family alignment: C + terminal monogram from the project's macOS design vocabulary. No copied OpenAI artwork.
- Windows application tile: 448 px rounded square on a 512 px canvas, 112 px corners; the quota radius falls from 179 to 146 px and line width from 18 to 12 px. Removed the redundant bottom progress decoration.
- Tray: no outer quota ring; 11 native ICO sizes (16–256px), 36 small-frame theme/badge combinations. System taskbar preference selects the ink and update reminders preserve the native DPI size.
- Flyout: opaque neutral surface, no blur. Desktop card: 58% default frosting, adjustable within 56–70%, deep opaque text, no foreground blur or neon glow. 304 DIP width and 22 DIP quota; used percentage and reset date remain separate. Non-topmost unless explicitly pinned.
- Disclosure: actual five-hour main card and only other periods in details, deduplication and weekly warnings. Single/unavailable data hides disclosure and closes previously open details; directional chevron and keyboard actions remain.
- Operations: tray-left summary; tray-right full menu; toggle hides/shows widget; close hides only; Exit stops refresh but leaves the minimal next-session guardian. No new model calls or telemetry.

## Checks performed

- Strict warnings-as-errors build.
- Automated WPF smoke: non-topmost windows, widget toggle, hide keeps tray, expansion increases height, collapse restores height, disclosure label changes, unavailable data cannot expand.
- Actual WPF layout rendering with synthetic quota in English, Chinese, Japanese and Spanish; metric widths checked for overlap. Cached, unknown and low-quota examples rendered.
- Visual inspection of collapsed/expanded cards, flyout, Spanish text, and icon scale galleries.
- `scripts/audit-design.cjs`: normal text >=4.5:1 on the matte preview; desktop inks >=4.5:1 over minimum tint on black; tray >=7:1 on light/dark audit surfaces. These calculations are not blanket WCAG certification.
- Multi-size ICO transparency/resource checks, regression tests and update rollback tests.
- Native blur-behind accepted on this Windows 11 host, with documented compatibility fallbacks. No custom screenshot/capture code ships. Detailed verification and limits: [1.0.0 scope](verification-1.0.0.md).

## Limits

There is no supported computer-use runtime in this session. UI states were driven through the app's own smoke harness, not automated real pointer or keyboard input. Physical multi-monitor drag and every display-scaling setting have not been exhaustively tested. Native ARM64 desktop testing remains pending.

The documentation pictures are WPF offscreen renders using sample values, not private account data or desktop screenshots. Gallery surfaces are illustrative; they do not reproduce Windows DWM background blur. Actual Acrylic depends on Windows version, transparency preferences and composition. Older/unsupported environments use a high-opacity fallback for legibility. High contrast uses system colors for primary text; this does not claim full assistive-technology certification.
