param([string]$Runtime='win-x64')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version=[IO.File]::ReadAllText((Join-Path $root 'VERSION')).Trim()
$zip=Join-Path $root ('dist\GaugeForCodex-Windows-'+$version+'-'+$Runtime+'.zip')
$verify=Join-Path $root ('artifacts\extract-'+$Runtime+'-'+[Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $zip -DestinationPath $verify
& (Join-Path $root 'scripts\apply-update.ps1') -InstallDir (Join-Path $root ('artifacts\verify-target-'+$Runtime)) -PackageDir $verify -ValidateOnly
if(Get-ChildItem -LiteralPath $verify -Directory -Recurse | Where-Object{$_.Name -eq 'Data'}){throw 'Published package contains user data.'}
foreach($notice in @('DotNet-Runtime-LICENSE.txt','DotNet-Runtime-THIRD-PARTY-NOTICES.txt','DotNet-WindowsDesktop-LICENSE.txt')){
    if(-not (Test-Path -LiteralPath (Join-Path $verify ('Notices\'+$notice)) -PathType Leaf)){throw ('Missing runtime license/notice: '+$notice)}
}
$stream=[IO.File]::OpenRead((Join-Path $verify 'GaugeForCodex.exe')); $reader=New-Object IO.BinaryReader($stream)
try{ $stream.Position=0x3c; $pe=$reader.ReadInt32(); $stream.Position=$pe; if($reader.ReadUInt32() -ne 0x4550){throw 'Invalid PE header'}; $machine=$reader.ReadUInt16(); $expected=if($Runtime -eq 'win-arm64'){0xaa64}else{0x8664}; if($machine -ne $expected){throw 'Wrong architecture'} }finally{$reader.Dispose();$stream.Dispose()}
Add-Type -AssemblyName System.Drawing
foreach($name in @('app.ico','tray.ico','tray-light.ico')){
    $bytes=[IO.File]::ReadAllBytes((Join-Path $verify ('Assets\'+$name)))
    $count=[BitConverter]::ToUInt16($bytes,4); $expectedCount=if($name -eq 'app.ico'){8}else{11}; if($count -ne $expectedCount){throw 'Incomplete icon size set'}
    for($index=0;$index -lt $count;$index++){
        $entry=6+$index*16; $length=[BitConverter]::ToUInt32($bytes,$entry+8);$offset=[BitConverter]::ToUInt32($bytes,$entry+12)
        $imageStream=New-Object IO.MemoryStream(,$bytes[$offset..($offset+$length-1)])
        $image=New-Object Drawing.Bitmap($imageStream)
        try{
            $maxAlpha=0; $minAlpha=255; $step=[Math]::Max(1,[int]($image.Width/16))
            for($x=0;$x -lt $image.Width;$x+=$step){for($y=0;$y -lt $image.Height;$y+=$step){$alpha=$image.GetPixel($x,$y).A;$maxAlpha=[Math]::Max($maxAlpha,$alpha);$minAlpha=[Math]::Min($minAlpha,$alpha)}}
            if($minAlpha -ne 0 -or $maxAlpha -lt 100){throw 'Invalid icon transparency/visibility'}
        }finally{$image.Dispose();$imageStream.Dispose()}
    }
}
Write-Output "PASS: $Runtime ZIP extracted, runtime licenses/notices, required resources, PE architecture, 30 icon frames and alpha channels"
Write-Output ('Verified extraction: '+$verify)
