param(
 [Parameter(Mandatory=$true)][ValidateSet('Building_Pieces','Dragon_Egg_Stacks','Auto_Mapping','Manual_Tar_Pickup','Auto_Wishbone_Andvaranaut','Compact_Crafting_Upgrades')][string]$Mod,
 [Parameter(Mandatory=$true)][string]$ManagedPath,
 [Parameter(Mandatory=$true)][string]$BepInExPath,
 [Parameter(Mandatory=$true)][string]$CompilerPath
)
$ErrorActionPreference='Stop'
$ManagedPath=(Resolve-Path -LiteralPath $ManagedPath).Path
$BepInExPath=(Resolve-Path -LiteralPath $BepInExPath).Path
$CompilerPath=(Resolve-Path -LiteralPath $CompilerPath).Path
$taskRoot=Join-Path $PSScriptRoot "mods\$Mod"
$taskOutput=Join-Path $PSScriptRoot "bin\$Mod"
New-Item -ItemType Directory -Force $taskOutput | Out-Null
$taskProjects=switch($Mod){
 'Building_Pieces' {@('TowerStairs','RoofCorners45','InvertedStoneRamp')}
 'Dragon_Egg_Stacks' {@('DragonEggStacks')}
 'Auto_Mapping' {@('QuietMap')}
 'Manual_Tar_Pickup' {@('ManualTarPickup')}
 'Auto_Wishbone_Andvaranaut' {@('InventoryFinders')}
 'Compact_Crafting_Upgrades' {@('CompactCraftingUpgrades')}
}
foreach($taskProject in $taskProjects){
 $taskSource=if($Mod -eq 'Building_Pieces'){Join-Path $taskRoot "$taskProject\src"}else{Join-Path $taskRoot 'src'}
 $taskRefs=@('mscorlib','System','System.Core','netstandard','assembly_valheim','UnityEngine','UnityEngine.CoreModule')
 if($Mod -eq 'Building_Pieces'){$taskRefs+=@('UnityEngine.PhysicsModule','UnityEngine.AssetBundleModule')}
 if($Mod -eq 'Auto_Mapping'){$taskRefs+=@('assembly_utils','assembly_guiutils','UnityEngine.ImageConversionModule','Unity.TextMeshPro','UnityEngine.UI','Newtonsoft.Json','Splatform')}
 $taskArguments=@('/nologo','/noconfig','/target:library','/optimize+','/nostdlib+','/warnaserror+',"/out:$taskOutput\$taskProject.dll")
 $taskArguments+=@($taskRefs|ForEach-Object {"/reference:$(Join-Path $ManagedPath "$_.dll")"})
 $taskArguments+="/reference:$(Join-Path $BepInExPath 'core\BepInEx.dll')"
 if($Mod -eq 'Building_Pieces'){$taskArguments+="/reference:$(Join-Path $BepInExPath 'plugins\ValheimModding-Jotunn\Jotunn.dll')"}
 else{$taskArguments+="/reference:$(Join-Path $BepInExPath 'core\0Harmony.dll')"}
 if($taskProject -eq 'TowerStairs'){$taskArguments+=@(Get-ChildItem -LiteralPath (Join-Path $taskSource 'icons') -Filter '*.png'|ForEach-Object {"/resource:$($_.FullName),$($_.Name)"})}
 if($taskProject -eq 'InvertedStoneRamp'){$taskArguments+="/resource:$(Join-Path $taskSource 'icon.png'),inverted-ramp-icon.png"}
 if($Mod -eq 'Auto_Mapping'){
  $taskArguments+="/resource:$(Join-Path $taskRoot 'assets\aio-landmarks.tsv'),QuietMap.AioLandmarks"
 }
 $taskArguments+=@(Get-ChildItem -LiteralPath $taskSource -Filter '*.cs'|ForEach-Object {$_.FullName})
 & $CompilerPath @taskArguments
 if($LASTEXITCODE -ne 0){throw "Compilation failed: $taskProject"}
 Write-Output "Built $taskProject -> $taskOutput"
}
