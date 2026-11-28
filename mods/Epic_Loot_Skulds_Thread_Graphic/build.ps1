param(
 [Parameter(Mandatory=$true)][string]$ManagedPath,
 [Parameter(Mandatory=$true)][string]$BepInExPath,
 [Parameter(Mandatory=$true)][string]$CompilerPath
)
$ErrorActionPreference='Stop'
$refs=@('mscorlib','System','System.Core','netstandard','assembly_valheim','UnityEngine','UnityEngine.CoreModule','UnityEngine.ImageConversionModule') | ForEach-Object {'/reference:'+(Join-Path $ManagedPath "$_.dll")}
$refs+=@("/reference:$BepInExPath/core/BepInEx.dll","/reference:$BepInExPath/core/0Harmony.dll","/reference:$BepInExPath/plugins/ValheimModding-Jotunn/Jotunn.dll")
$out=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force $out | Out-Null
& $CompilerPath /nologo /target:library /nostdlib+ /warnaserror+ "/out:$out/SkuldsThreadRope.dll" "/resource:$PSScriptRoot/rope.png,rope.png" @refs "$PSScriptRoot/src/Plugin.cs"
if($LASTEXITCODE -ne 0){throw 'Rope prototype build failed'}
Write-Host "Built $out/SkuldsThreadRope.dll. Not installed or published."
