param([string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\GaugeForCodex-Windows'), [switch]$NoLaunch)
$ErrorActionPreference = 'Stop'
$source = $PSScriptRoot
$install = [IO.Path]::GetFullPath($InstallDir).TrimEnd('\')
if ($install -eq [IO.Path]::GetPathRoot($install).TrimEnd('\')) { throw 'Do not install at a drive root.' }
if (-not (Test-Path -LiteralPath (Join-Path $source 'GaugeForCodex.exe'))) { throw 'Run this script from the extracted application ZIP.' }
if (-not (Test-Path -LiteralPath (Join-Path $source 'product-id.txt'))) { throw 'Package identity is missing.' }
New-Item -ItemType Directory -Force -Path $install | Out-Null
if (-not $source.Equals($install,[StringComparison]::OrdinalIgnoreCase)) {
    Get-Process -Name GaugeForCodex -ErrorAction SilentlyContinue | ForEach-Object {
        if ($_.Path -and $_.Path.Equals((Join-Path $install 'GaugeForCodex.exe'),[StringComparison]::OrdinalIgnoreCase)) { Stop-Process -Id $_.Id -Force }
    }
    Get-ChildItem -LiteralPath $source | Where-Object { $_.Name -ne 'Data' } | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $install -Recurse -Force }
}
$shell = New-Object -ComObject WScript.Shell
foreach ($folder in @([Environment]::GetFolderPath('Desktop'),[Environment]::GetFolderPath('Programs'))) {
    $link = $shell.CreateShortcut((Join-Path $folder 'Gauge for Codex (Windows).lnk'))
    $link.TargetPath = Join-Path $install 'GaugeForCodex.exe'; $link.WorkingDirectory = $install
    $link.Description = 'Gauge for Codex · Windows'; $link.IconLocation = (Join-Path $install 'GaugeForCodex.exe') + ',0'; $link.Save()
}
$startup = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Startup')) 'Gauge for Codex Windows.lnk'))
$startup.TargetPath = Join-Path $install 'GaugeForCodex.exe'; $startup.Arguments = '--watch'; $startup.WorkingDirectory = $install
$startup.IconLocation = (Join-Path $install 'GaugeForCodex.exe') + ',0'; $startup.Save()
if (-not $NoLaunch) {
    Start-Process -FilePath (Join-Path $install 'GaugeForCodex.exe') -ArgumentList @('--watch') -WorkingDirectory $install -WindowStyle Hidden
    Start-Process -FilePath (Join-Path $install 'GaugeForCodex.exe') -WorkingDirectory $install -WindowStyle Hidden
}
Write-Output "Installed: $install"
