# Security

Report vulnerabilities privately through GitHub's private vulnerability reporting when available; otherwise contact the maintainer without disclosing tokens or personal files in a public issue.

- Update URLs are restricted to HTTPS assets in this project's exact GitHub repository. Stable, newer semantic versions only; drafts and prereleases are rejected.
- SHA-256 is verified before extraction. Filenames, response sizes and extraction sizes are bounded. ZIP path traversal and user-data replacement are rejected.
- The updater preserves settings, backs up replaced files, verifies copied hashes and restores previous files on a copy/verification failure.
- SHA-256 confirms release-asset integrity, not publisher identity. The release is **not Authenticode signed**; no certificate is bundled. Never disable SmartScreen, antivirus or Windows security settings to install it.
- The desktop/guardian IPC is restricted to the same Windows user and accepts fixed commands only. It cannot execute arbitrary paths or shell commands.
- The initial installer is per-user and does not require administrator privileges in a user-writable directory. For an existing installation, use the app's verified updater rather than overwriting a running copy.

Windows 11 22H2+ uses documented DWM Desktop Acrylic. When transparency is disabled, high contrast is enabled or DWM declines the effect, the app uses an opaque/transparent fallback. Windows 10 uses a translucent fallback without live blur.
