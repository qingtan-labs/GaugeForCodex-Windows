param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version=[IO.File]::ReadAllText((Join-Path $root 'VERSION')).Trim()
$dist=Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$gitOptions=@('-C',$root,'-c',('safe.directory='+$root.Replace('\','/')))
if (@(& git @gitOptions status --porcelain).Count -ne 0) { throw 'Commit the reviewed source before archiving.' }
$zip=Join-Path $dist ('GaugeForCodex-Windows-'+$version+'-source.zip')
& git @gitOptions archive --format=zip --output=$zip HEAD
if ($LASTEXITCODE -ne 0) { throw 'Source archive failed.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[IO.Compression.ZipFile]::OpenRead($zip)
try {
    foreach($entry in $archive.Entries) {
        if($entry.FullName -match '(^|/)(Data|bin|obj|artifacts|dist|\.git)(/|$)') { throw 'Private/build data in source archive.' }
    }
    $versionEntry=$archive.GetEntry('VERSION')
    if(-not $versionEntry -or -not $archive.GetEntry('src/GaugeForCodex.csproj') -or -not $archive.GetEntry('LICENSE')) { throw 'Incomplete source archive.' }
    $reader=New-Object IO.StreamReader($versionEntry.Open())
    try { if($reader.ReadToEnd().Trim() -ne $version) { throw 'Source version mismatch.' } } finally { $reader.Dispose() }
} finally { $archive.Dispose() }
$lines=@(Get-ChildItem -LiteralPath $dist -Filter ('GaugeForCodex-Windows-'+$version+'-*.zip') | Sort-Object Name | ForEach-Object {
    (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_.Name
})
[IO.File]::WriteAllText((Join-Path $dist 'SHA256SUMS.txt'),($lines -join "`n")+"`n")
Write-Output ('PASS: committed source ZIP excludes user/build data; checksums regenerated: '+$zip)
