# 1.0.0 verification scope

This release freezes the accepted Windows tray/card design. It does not claim every previously proposed feature is implemented.

## Automated gates

- 110 offline assertions: quota parsing, five-hour preference, distinct additional periods, conservative deduplication, low weekly warnings, version/URL/checksum/ZIP safety, aligned translations, weekly checks and preference migration.
- Strict self-contained Release builds with warnings treated as errors, for `win-x64` and `win-arm64`.
- App-native WPF harness: opaque flyout with no glass frame; independent non-blurred foreground; default desktop alpha 147/255; disclosure, single-window transitions, four locales, cached/unknown states and hide-to-tray.
- 36 physical tray size/theme/update combinations and repeated badge-refresh checks.
- Calculated widget text contrast >=4.5:1 at minimum 56% neutral tint over RGB black. This is a color-model check, not full accessibility certification.
- Transaction tests preserve user data and restore files after an injected mid-copy failure.
- Both app ZIPs re-extracted: product identity, .NET runtime/Desktop licenses and runtime third-party notices from the exact NuGet runtime versions, required resources, x64/ARM64 PE type, 30 combined ICO frames with alpha, no `Data/`.
- Source ZIP comes from the release commit, excluding runtime data, build outputs and credentials. SHA-256 identifies all downloadable ZIPs.

## Limitations

- No Authenticode signature; do not disable Windows protections.
- ARM64 is cross-compiled, not physically tested on ARM64 hardware.
- The host accepted native blur-behind. A successful API result and offscreen renders do not prove appearance on every desktop/driver/remote session.
- Dynamic accent probing is a compatibility path, not a stable public API contract; documented Acrylic and opaque fallback remain.
- Tray artwork follows taskbar theme. Full-window system/light/dark selection is not implemented; panels are neutral light.
- Manual language selection is supported; initial Chinese/English choice follows system language, not continuous language following.
- Missing quota periods/timestamps are never fabricated. Cached values are not live values.
- Update checks notify only; a same-number replacement does not count as a higher-version update.

README previews contain synthetic quota data, not personal desktop captures or evidence of physical mouse/keyboard/multi-monitor validation.
