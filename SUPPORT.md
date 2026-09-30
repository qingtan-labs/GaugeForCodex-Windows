# Support

Use GitHub Issues with app version, Windows version/architecture and a description without credentials or account data.

- **Where is the app?** Open the Windows tray overflow arrow and locate the C + terminal icon. Drag it to the visible tray area if desired. Desktop and Start-menu shortcuts are also available after running `install.ps1`.
- **Close vs exit:** The widget's × only hides the widget. The tray's Exit closes quota UI and stops refresh. Follow mode suppresses relaunch until a new Codex desktop session. Its small guardian remains available for the next session.
- **Quota unavailable:** Open and sign in to Codex Desktop, then choose Refresh quota. Manual entry is available. Cached data is explicitly labeled with its timestamp.
- **No blur:** Windows system transparency effects, high contrast and the graphics/composition environment affect Acrylic. Windows 10 and unsupported configurations use the documented fallback. The app never changes system accessibility or privacy settings.
- **Glass looks white:** Over white content, a neutral translucent card still looks pale. Over colored/dark content it can show the blurred backdrop. Version 1.0.0 migrates the earlier 80% white-looking tint to 58%; Settings permits 56–70%. Text is opaque and is not blurred. Compatibility fallback may remain mostly opaque.
- **Why no five-hour quota/details?** Only periods actually returned by Codex are shown. A single weekly period hides the detail entry; dual five-hour/weekly periods put the five-hour allowance on the card and weekly allowance in details.
- **Theme/language:** The tray follows taskbar light/dark preference. Full-window dark mode is not included in 1.0.0. Settings supports English, Chinese, Japanese and Spanish; the initial language defaults to Chinese on Chinese systems and English elsewhere.
- **Update failed:** Download/verification failures leave the current installation untouched. Copy/verification failures restore backed-up files. Backups and update transactions remain under `Data/Updates/` for manual recovery.

完整中文操作、数据状态及更新说明：[使用指南](docs/user-guide.zh-Hans.md)。
