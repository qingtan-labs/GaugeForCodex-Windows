$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$test=Join-Path $root ('artifacts\transaction-test-'+[Guid]::NewGuid().ToString('N'))
$package=Join-Path $test 'Package'; $install=Join-Path $test 'Installed'
if(-not $test.StartsWith($root+'\artifacts\',[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe test fixture root.'}
$names=@('GaugeForCodex.exe','GaugeForCodex.dll','GaugeForCodex.runtimeconfig.json','Assets\app.ico','Assets\tray.ico','Assets\tray-light.ico','Assets\app-icon.png','apply-update.ps1','product-id.txt')
New-Item -ItemType Directory -Force -Path (Join-Path $package 'Assets'),(Join-Path $install 'Assets'),(Join-Path $install 'Data') | Out-Null
foreach($name in $names){[IO.File]::WriteAllText((Join-Path $package $name),'new-'+$name);[IO.File]::WriteAllText((Join-Path $install $name),'old-'+$name)}
[IO.File]::WriteAllText((Join-Path $package 'product-id.txt'),'QingtanLabs.GaugeForCodex.Windows|0.1.0')
$settings=Join-Path $install 'Data\settings.txt'; [IO.File]::WriteAllText($settings,'keep-user-settings')
$helper=Join-Path $root 'scripts\apply-update.ps1'
& $helper -InstallDir $install -PackageDir $package -NoRestart
foreach($name in $names){if((Get-FileHash -LiteralPath (Join-Path $package $name)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $install $name)).Hash){throw 'Successful transaction resource mismatch.'}}
if([IO.File]::ReadAllText($settings) -ne 'keep-user-settings'){throw 'Data was changed.'}
Write-Output 'PASS: verified transaction replaces package resources and preserves Data'
foreach($name in $names){[IO.File]::WriteAllText((Join-Path $install $name),'old-'+$name)}
& $helper -InstallDir $install -PackageDir $package -NoRestart -FailureAfterCopies 1
foreach($name in $names){if([IO.File]::ReadAllText((Join-Path $install $name)) -ne 'old-'+$name){throw 'Rollback did not restore a resource.'}}
if([IO.File]::ReadAllText($settings) -ne 'keep-user-settings'){throw 'Rollback changed Data.'}
$status=[IO.File]::ReadAllText((Join-Path $install 'Data\update-result.json')) | ConvertFrom-Json
if($status.status -ne 'rollback'){throw 'Missing rollback result.'}
Write-Output 'PASS: injected mid-copy failure restores previous resources and preserves Data'
