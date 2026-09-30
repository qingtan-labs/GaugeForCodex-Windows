param([string]$Dotnet='dotnet',[ValidateSet('win-x64','win-arm64')][string]$Runtime='win-x64')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version=[IO.File]::ReadAllText((Join-Path $root 'VERSION')).Trim()
$dist=Join-Path $root 'dist'
$package=Join-Path $root ('artifacts\package-'+$Runtime)
New-Item -ItemType Directory -Force -Path $dist,$package | Out-Null
& $Dotnet run --project (Join-Path $root 'tests\GaugeForCodex.Tests.csproj') -c Release -p:TreatWarningsAsErrors=true
if($LASTEXITCODE -ne 0){throw 'Self-tests failed.'}
& (Join-Path $root 'tests\update-transaction.ps1')
& $Dotnet publish (Join-Path $root 'src\GaugeForCodex.csproj') -c Release -r $Runtime --self-contained true -p:TreatWarningsAsErrors=true -o $package
if($LASTEXITCODE -ne 0){throw 'Strict publish failed.'}
$configuration=Get-Content -LiteralPath (Join-Path $package 'GaugeForCodex.runtimeconfig.json') -Raw | ConvertFrom-Json
$nugetRoot=if($env:NUGET_PACKAGES){$env:NUGET_PACKAGES}else{Join-Path $env:USERPROFILE '.nuget\packages'}
$notices=Join-Path $package 'Notices'
New-Item -ItemType Directory -Force -Path $notices | Out-Null
foreach($component in @(
    @{Framework='Microsoft.NETCore.App';Package=('microsoft.netcore.app.runtime.'+$Runtime);Prefix='DotNet-Runtime'},
    @{Framework='Microsoft.WindowsDesktop.App';Package=('microsoft.windowsdesktop.app.runtime.'+$Runtime);Prefix='DotNet-WindowsDesktop'}
)){
    $framework=$configuration.runtimeOptions.includedFrameworks | Where-Object name -eq $component.Framework | Select-Object -First 1
    if(-not $framework){throw 'Missing runtime version for license selection.'}
    $packRoot=Join-Path $nugetRoot ($component.Package+'\'+$framework.version)
    $license=Get-ChildItem -LiteralPath $packRoot -File | Where-Object Name -Match '^LICENSE(\.TXT)?$' | Select-Object -First 1
    if(-not $license){throw ('Missing runtime license: '+$component.Framework)}
    Copy-Item -LiteralPath $license.FullName -Destination (Join-Path $notices ($component.Prefix+'-LICENSE.txt')) -Force
    $thirdParty=Join-Path $packRoot 'THIRD-PARTY-NOTICES.TXT'
    if(Test-Path -LiteralPath $thirdParty){Copy-Item -LiteralPath $thirdParty -Destination (Join-Path $notices ($component.Prefix+'-THIRD-PARTY-NOTICES.txt')) -Force}
}
Copy-Item -LiteralPath (Join-Path $root 'scripts\install.ps1') -Destination $package -Force
foreach($name in @('LICENSE','NOTICE.md','PRIVACY.md','SUPPORT.md')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $package -Force}
[IO.File]::WriteAllText((Join-Path $package 'product-id.txt'),('QingtanLabs.GaugeForCodex.Windows|'+$version))
& (Join-Path $root 'scripts\apply-update.ps1') -InstallDir (Join-Path $root ('artifacts\verification-'+$Runtime)) -PackageDir $package -ValidateOnly
if($LASTEXITCODE -ne 0){throw 'Package validation failed.'}
$zip=Join-Path $dist ('GaugeForCodex-Windows-'+$version+'-'+$Runtime+'.zip')
$items=@(Get-ChildItem -LiteralPath $package | Where-Object{$_.Name -ne 'Data'} | Select-Object -ExpandProperty FullName)
Compress-Archive -LiteralPath $items -DestinationPath $zip -Force -CompressionLevel Optimal
$lines=@(Get-ChildItem -LiteralPath $dist -Filter ('GaugeForCodex-Windows-'+$version+'-*.zip') | Sort-Object Name | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_.Name })
[IO.File]::WriteAllText((Join-Path $dist 'SHA256SUMS.txt'),($lines -join "`n")+"`n")
Write-Output "Packaged: $zip"
