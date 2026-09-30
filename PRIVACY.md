# Privacy / 隐私

Gauge for Codex for Windows reads only normalized quota summaries via the local Codex `app-server` stdio interface and `account/rateLimits/read`.
It does not invoke a model or send a prompt. It does not read conversations, credentials, browser cookies or API keys.
The installed Codex component can contact OpenAI with the user's existing local account configuration.

The application stores percentages, window durations, reset timestamps, last successful refresh time, manual values, display/startup preferences and update-check timestamps in `Data/` beside its executable.
A small session identifier (desktop process ID and start time) prevents relaunch after an explicit exit; it is never transmitted.
The app checks process paths to distinguish Codex Desktop from similarly named CLI processes; it does not inspect command lines or window contents.

Weekly update checks and user-requested checks send HTTPS requests to GitHub's releases API for `qingtan-labs/GaugeForCodex-Windows`. GitHub receives the usual network metadata, including IP address, and an application/version User-Agent. No quota, account identifier, conversation, file or telemetry is sent.
Downloads use GitHub Releases and its HTTPS asset CDN after explicit installation confirmation.
There is no developer-operated backend, analytics, advertising or crash-reporting service.

You can disable weekly update checks and automatic startup in Settings. Manual update checking remains available.
Uninstall by exiting the application, disabling automatic startup, removing the installed folder and its shortcuts. Removing `Data/` also removes preferences, normalized quota cache and retained update backups.

中文：本程序只读本机 Codex 的额度汇总，不调用模型、不读取对话或密钥。额度与设置保存在安装目录旁的 `Data/`。每周检查更新及手动检查会访问 GitHub；点击“下载并安装”后才下载发行包。不会发送额度、账号或文件数据，无遥测。设置中可关闭每周检查和自动启动。
