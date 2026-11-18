param(
 [Parameter(Mandatory=$true)][string]$ManagedPath,
 [Parameter(Mandatory=$true)][string]$BepInExPath,
 [Parameter(Mandatory=$true)][string]$CompilerPath
)
$ErrorActionPreference='Stop'
$refs=@('mscorlib','System','System.Core','netstandard','assembly_valheim','assembly_utils','assembly_guiutils','UnityEngine','UnityEngine.CoreModule','UnityEngine.ParticleSystemModule','UnityEngine.AudioModule','UnityEngine.InputLegacyModule','UnityEngine.UI','UnityEngine.UIModule') | ForEach-Object { '/reference:'+(Join-Path $ManagedPath "$_.dll") }
$refs+=@("/reference:$BepInExPath\core\BepInEx.dll","/reference:$BepInExPath\core\0Harmony.dll")
$sources=@(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
& $CompilerPath /nologo /noconfig /target:library /optimize+ /nostdlib+ /warnaserror+ "/out:$PSScriptRoot\WisplightControl.dll" @refs @sources
if($LASTEXITCODE -ne 0){throw 'Wisplight Control compilation failed'}
