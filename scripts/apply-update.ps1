param(
    [Parameter(Mandatory=$true)][string]$InstallDir,
    [Parameter(Mandatory=$true)][string]$PackageDir,
    [int]$ParentId = 0,
    [ValidateSet('--follow','--background')][string]$RestartMode = '--background',
    [switch]$ValidateOnly,
    [switch]$NoRestart,
    [int]$FailureAfterCopies = 0
)
$ErrorActionPreference = 'Stop'
$install = [IO.Path]::GetFullPath($InstallDir).TrimEnd('\')
$package = [IO.Path]::GetFullPath($PackageDir).TrimEnd('\')
if ($install -eq [IO.Path]::GetPathRoot($install).TrimEnd('\') -or $install -eq $package) { throw 'Unsafe installation root.' }
$exe = Join-Path $install 'GaugeForCodex.exe'
$data = Join-Path $install 'Data'
$marker = Join-Path $data 'update-in-progress.json'
$result = Join-Path $data 'update-result.json'
$identity = Join-Path $package 'product-id.txt'
if (-not (Test-Path -LiteralPath $identity)) { throw 'Missing package identity.' }
$product = [IO.File]::ReadAllText($identity).Trim()
if ($product -notmatch '^QingtanLabs\.GaugeForCodex\.Windows\|\d+\.\d+\.\d+$') { throw 'Incorrect product identity.' }
foreach ($required in @('GaugeForCodex.exe','GaugeForCodex.dll','GaugeForCodex.runtimeconfig.json','Assets\app.ico','Assets\tray.ico','Assets\tray-light.ico','Assets\app-icon.png','apply-update.ps1')) {
    if (-not (Test-Path -LiteralPath (Join-Path $package $required) -PathType Leaf)) { throw "Missing package resource: $required" }
}
$files = @(Get-ChildItem -LiteralPath $package -Recurse -File)
foreach ($file in $files) {
    $relative = $file.FullName.Substring($package.Length + 1)
    $target = [IO.Path]::GetFullPath((Join-Path $install $relative))
    if (-not $target.StartsWith($install + '\',[StringComparison]::OrdinalIgnoreCase) -or $relative -match '^Data([\\/]|$)') { throw 'Package may not replace user data.' }
}
if ($ValidateOnly) { Write-Output 'PASS: package identity, required files, target containment, data exclusion'; exit 0 }
New-Item -ItemType Directory -Force -Path $data | Out-Null
$backup = Join-Path $data ('Updates\Backup-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $backup | Out-Null
$originals = @{}
try {
    if ($ParentId -gt 0) {
        $parent = Get-Process -Id $ParentId -ErrorAction SilentlyContinue
        if ($parent) { $null = $parent.WaitForExit(30000) }
    }
    # Stop only this installation's card/supervisor, never a similarly named application elsewhere.
    Get-Process -Name GaugeForCodex -ErrorAction SilentlyContinue | ForEach-Object {
        if ($_.Path -and [IO.Path]::GetFullPath($_.Path).Equals($exe,[StringComparison]::OrdinalIgnoreCase)) { Stop-Process -Id $_.Id -Force }
    }
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($package.Length + 1)
        $target = Join-Path $install $relative
        $existed = Test-Path -LiteralPath $target -PathType Leaf
        if ($existed) {
            $saved = Join-Path $backup $relative
            New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($saved)) | Out-Null
            Copy-Item -LiteralPath $target -Destination $saved
        }
        $originals[$relative] = $existed
    }
    $copied = 0
    foreach ($file in $files) {
        $target = Join-Path $install ($file.FullName.Substring($package.Length + 1))
        New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($target)) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
        $copied++
        if ($FailureAfterCopies -gt 0 -and $copied -eq $FailureAfterCopies) { throw 'Injected transaction test failure.' }
    }
    # Validate copied resources before declaring the transaction successful.
    foreach ($file in $files) {
        $target = Join-Path $install ($file.FullName.Substring($package.Length + 1))
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash) { throw 'Installed resource checksum mismatch.' }
    }
    [IO.File]::WriteAllText($result,(@{ status='installed'; version=$product.Split('|')[1] } | ConvertTo-Json))
} catch {
    $failure = $_
    foreach ($relative in $originals.Keys) {
        $target = [IO.Path]::GetFullPath((Join-Path $install $relative))
        if (-not $target.StartsWith($install + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe rollback target.' }
        if ($originals[$relative]) { Copy-Item -LiteralPath (Join-Path $backup $relative) -Destination $target -Force }
        elseif (Test-Path -LiteralPath $target -PathType Leaf) { Remove-Item -LiteralPath $target -Force }
    }
    [IO.File]::WriteAllText($result,'{"status":"rollback"}')
    Write-Warning 'Installation failed; previous files restored.'
} finally {
    if (Test-Path -LiteralPath $marker -PathType Leaf) { Remove-Item -LiteralPath $marker -Force }
    if (-not $NoRestart -and (Test-Path -LiteralPath $exe)) {
        Start-Process -FilePath $exe -ArgumentList @('--watch') -WorkingDirectory $install -WindowStyle Hidden
        Start-Process -FilePath $exe -ArgumentList @($RestartMode) -WorkingDirectory $install -WindowStyle Hidden
    }
}
