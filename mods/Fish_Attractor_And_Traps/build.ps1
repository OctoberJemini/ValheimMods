param(
 [Parameter(Mandatory=$true)][string]$ManagedPath,
 [Parameter(Mandatory=$true)][string]$BepInExPath,
 [Parameter(Mandatory=$true)][string]$CompilerPath
)
$ErrorActionPreference='Stop'
$refs=@('mscorlib','System','System.Core','netstandard','assembly_valheim','assembly_utils','assembly_guiutils','UnityEngine','UnityEngine.CoreModule','UnityEngine.PhysicsModule','UnityEngine.AssetBundleModule')|ForEach-Object {'/reference:'+(Join-Path $ManagedPath "$_.dll")}
$refs+=@("/reference:$BepInExPath\core\BepInEx.dll","/reference:$BepInExPath\plugins\ValheimModding-Jotunn\Jotunn.dll")
$source=Join-Path $PSScriptRoot 'src'
$dll=Join-Path $PSScriptRoot 'FishAttractor.dll'
& $CompilerPath /nologo /noconfig /target:library /optimize+ /nostdlib+ /warnaserror+ "/out:$dll" @refs "$source\Plugin.cs" "$source\BaitPolicy.cs" "$source\FishTrap.cs" "$source\TrapRules.cs"
if($LASTEXITCODE -ne 0){throw 'Fish Attractor compilation failed'}
