# Contributing

Use Windows and .NET SDK 8.0 or later capable of targeting .NET 8. Build with warnings as errors.

```powershell
dotnet run --project tests/GaugeForCodex.Tests.csproj -c Release -p:TreatWarningsAsErrors=true
dotnet build src/GaugeForCodex.csproj -c Release -p:TreatWarningsAsErrors=true
```

Keep the app native, dependency-light and local-first. Do not add telemetry, credential scraping, pet logic or shell parsing of untrusted quota payloads.
Keep English, Simplified Chinese, Japanese and Spanish strings and placeholders aligned.
Add regression tests for quota response changes, update scheduling and update security.
Check tray access, focus dismissal, widget toggling, drag persistence, manual exit suppression, Codex-session transitions and update rollback on Windows.
Do not commit build products, Data directories, account data, credentials or personal desktop captures.
For icon changes, edit the SVG masters and rebuild with Node.js + `sharp`: `node scripts/build-icons.cjs`.
Release binaries are produced by `scripts/build-release.ps1`; see `docs/release-process.md`.
