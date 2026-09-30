# Gauge for Codex · Windows 1.0.0

A native Windows quota tray and optional desktop card, without pet logic or assets. Independent community utility, not an official OpenAI application.

- Opaque tray flyout: quota summary, widget toggle, refresh, updates, settings and exit.
- Separately draggable frosted desktop widget: balanced 58% tint (56–70% adjustable) and independent opaque text; old 16% / 80% styles migrate without moving the card.
- Actual five-hour quota first, weekly quota only in details; missing windows are not invented, duplicates removed and single-window disclosure hidden. Low/exhausted weekly allowance remains visible while collapsed.
- Native taskbar-DPI C + terminal marks, light/dark taskbar variants and vector widget header; no persistent blue mouse-focus outline.
- Default non-topmost, remembered position, distinct hide/exit behavior, Codex-follow startup and four languages.
- Local 60-second quota refresh without model calls, timestamped cache/manual fallback, weekly notify-only updates, verified explicit installation and rollback.

中文：托盘浮窗恢复实色，桌面组件独立保留雾面背景；默认底色浓度 58%，文字不模糊。真实 5 小时额度在主卡片，详情只列其他周期，单周期不重复、不补造数据。修复图标 DPI 清晰度、详情交互和旧设置迁移。默认不置顶，支持跟随启动、隐藏与退出、四语言、每周提醒及手动确认更新。

完整 [中文使用说明](https://github.com/qingtan-labs/GaugeForCodex-Windows/blob/v1.0.0/docs/user-guide.zh-Hans.md) · [图文介绍](https://github.com/qingtan-labs/GaugeForCodex-Windows/blob/v1.0.0/README.zh-Hans.md)。图例均使用示例额度，不含私人账号数据。

## Download

Choose `win-x64` for Intel/AMD Windows or `win-arm64` for ARM Windows. Application ZIPs include .NET 8 Windows Desktop runtime; no SDK is required. `source.zip` is source code, not the runnable app.

Verify SHA-256 against `SHA256SUMS.txt`, extract and open `GaugeForCodex.exe`. Optional `install.ps1` creates user shortcuts. Existing users may choose Check for updates and confirm installation; preferences and normalized quota cache are retained.

## Verification and limitations

110 offline assertions, strict x64/ARM64 builds, WPF interface checks, 36 tray combinations, contrast calculations, update rollback tests, and re-extracted package/resource/architecture/ICO-alpha checks. [Verification scope](https://github.com/qingtan-labs/GaugeForCodex-Windows/blob/v1.0.0/docs/verification-1.0.0.md).

**Not Authenticode signed:** Windows may show an unknown-publisher warning. Do not disable security protections. ARM64 is cross-compiled and package-verified, not physically tested. Background blur depends on Windows transparency/composition; fallback may be opaque. Tray icons follow taskbar theme, but full-window dark-mode selection is not included in 1.0.0. A same-version replacement requires manual download for users already on that version.
