# Release process

1. Run the offline tests and strict build. Exercise the Windows UI smoke command with synthetic data: `GaugeForCodex.exe --self-test-ui <output-directory>`.
2. Review the two layout previews. They use synthetic quota values and do not capture the desktop. Verify actual Desktop Acrylic separately on a Windows 11 desktop. Do not commit actual desktop captures that include personal background.
3. Run `scripts/build-release.ps1 -Runtime win-x64` and `-Runtime win-arm64`.
4. Extract both ZIPs into new verification directories. Validate product identity, resource presence, PE machine type and application/tray icon entries. Never include `Data/`.
5. After committing the reviewed tree, run `scripts/build-source.ps1` to archive committed source only and regenerate `SHA256SUMS.txt` for this version's ZIPs.
6. Commit source, tag `v<version>` (currently `v1.0.0`), upload packages/source ZIP/checksums to a draft Release, then publish only after verification. Retain .NET runtime license notices.
7. Verify the public latest-release API and a downloaded release checksum. Update the English and Chinese `project-showcase` entries.

Initial builds are unsigned. Do not claim Authenticode trust or instructions to disable Windows security. ARM64 cross-build/resource verification is not a physical ARM64 runtime test.

Normal updates must increase the version: the application only offers higher versions. An explicitly requested same-number replacement requires recording its source/checksums and explaining manual download for same-version users. Never overwrite immutable releases or rewrite source history without explicit authorization. Delete obsolete releases/tags only within the approved scope after replacement downloads are verified.
