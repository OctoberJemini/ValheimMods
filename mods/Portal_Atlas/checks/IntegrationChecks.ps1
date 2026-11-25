param(
 [string]$ManagedPath='C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed',
 [string]$BepInExPath='C:\Users\che\AppData\Roaming\Thunderstore Mod Manager\DataFolder\Valheim\profiles\October Jemini 1.0 (Valheim)\BepInEx'
)
$ErrorActionPreference='Stop'
Add-Type -Path "$BepInExPath\core\Mono.Cecil.dll"
$game=[Mono.Cecil.AssemblyDefinition]::ReadAssembly("$ManagedPath\assembly_valheim.dll")
$mod=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot '..\PortalAtlas.dll'))
$checks=0
function Check([bool]$ok,[string]$message){if(-not $ok){throw $message};$script:checks++}
$plugin=$mod.MainModule.Types|Where-Object Name -eq 'Plugin'
foreach($patch in @($mod.MainModule.Types | ForEach-Object { $_.NestedTypes })){
 $attribute=$patch.CustomAttributes|Where-Object {$_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch'}
 if(-not $attribute){continue}
 $args=$attribute.ConstructorArguments
 $typeName=$args[0].Value.FullName
 $methodName=$args[1].Value
 $target=$game.MainModule.Types|Where-Object FullName -eq $typeName
 $candidates=@($target.Methods|Where-Object Name -eq $methodName)
 Check ($candidates.Count -gt 0) "Missing Harmony target $typeName.$methodName"
 if($args.Count -eq 3){
  $signature=@($args[2].Value|ForEach-Object {$_.Value.FullName}) -join ','
  $candidates=@($candidates|Where-Object {(@($_.Parameters|ForEach-Object {$_.ParameterType.FullName}) -join ',') -eq $signature})
 }
 Check ($candidates.Count -eq 1) "Ambiguous Harmony target $typeName.$methodName"
 $method=$candidates[0]
 foreach($hook in @($patch.Methods|Where-Object Name -match '^Prefix$|^Postfix$')){
  foreach($p in $hook.Parameters){
   if($p.Name.StartsWith('__')){continue}
   Check (@($method.Parameters|Where-Object Name -eq $p.Name).Count -eq 1) "Missing Harmony injected argument $typeName.$methodName/$($p.Name)"
  }
 }
}
$table=$game.MainModule.Types|Where-Object Name -eq 'PieceTable'
Check (@($table.Fields|Where-Object Name -eq 'm_availablePiecesByCategory').Count -eq 1) 'Missing piece category list'
$rpc=$game.MainModule.Types|Where-Object Name -eq 'ZRoutedRpc'
Check (@($rpc.Methods|Where-Object Name -eq 'GetServerPeerID').Count -eq 1) 'Missing server peer API'
foreach($name in @('atlas','pillar','pillar-pin','star-on','star-off','rename','search','travel','close')){
 Check (@($mod.MainModule.Resources|Where-Object Name -eq "PortalAtlas.$name.png").Count -eq 1) "Missing packaged icon $name"
}
Check (@(($game.MainModule.Types|Where-Object Name -eq 'Minimap').Fields|Where-Object Name -eq 'm_pinNamePrefab').Count -eq 1) 'Native map-label prefab must exist'
Check (@(($game.MainModule.Types|Where-Object Name -eq 'Minimap').Methods|Where-Object { $_.Name -eq 'IsExplored' -and $_.Parameters.Count -eq 1 -and $_.Parameters[0].ParameterType.FullName -eq 'UnityEngine.Vector3' }).Count -eq 1) 'Passive ordinary-map pillar pins require native exploration checks'
$inventory=$game.MainModule.Types|Where-Object Name -eq 'InventoryGui'
$right=($inventory.Methods|Where-Object Name -eq 'OnRightClickItem').Body.Instructions
Check (@($right|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'Humanoid::UseItem'}).Count -gt 0) 'Inventory right click must route through patched UseItem'
$player=$game.MainModule.Types|Where-Object Name -eq 'Player'
$hotbar=($player.Methods|Where-Object Name -eq 'UseHotbarItem').Body.Instructions
Check (@($hotbar|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'Humanoid::UseItem'}).Count -gt 0) 'Hotbar activation must route through patched UseItem'
Check (@($mod.MainModule.Types|Where-Object Name -eq 'AtlasPillar').Count -eq 1) 'Missing pillar network component'
$ui=$mod.MainModule.Types|Where-Object Name -eq 'AtlasUi'
Check (@($mod.MainModule.Resources|Where-Object Name -eq 'PortalAtlas.waystone.mesh').Count -eq 1) 'Approved waystone geometry must be embedded'
$visual=$mod.MainModule.Types|Where-Object Name -eq 'AtlasPillarVisual'
$build=($visual.Methods|Where-Object Name -eq 'Build').Body.Instructions
Check (@($build|Where-Object {$_.Operand -is [string] -and $_.Operand -eq 'stone_wall_2x1'}).Count -eq 1) 'Waystone must reuse native stone material'
$crown=$mod.MainModule.Types|Where-Object Name -eq 'AtlasOrbCrown'
$orb=($crown.Methods|Where-Object Name -eq 'Build').Body.Instructions
$copy=($crown.Methods|Where-Object Name -eq 'CopyMesh').Body.Instructions
Check (@($orb|Where-Object {$_.Operand -is [string] -and $_.Operand -eq 'dverger_demister'}).Count -eq 1) 'Crown must reuse the native Dvergr demister'
Check (@($orb|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'get_identity'}).Count -gt 0) 'Crown must be level in pillar-local space'
Check (@(($orb+$copy)|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'Instantiate'}).Count -eq 0) 'Do not clone gameplay or particle hierarchies'
Check (@($orb|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'set_outputAudioMixerGroup'}).Count -gt 0) 'Hum must respect the native sound mixer'
Check (@($orb|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'set_spatialBlend'}).Count -gt 0) 'Hum must be positional'
foreach($asset in @('waystone-painted','waystone-masonry')){Check (@($mod.MainModule.Resources|Where-Object Name -eq "PortalAtlas.$asset.png").Count -eq 1) "Missing approved-style material $asset"}
Check (@($build|Where-Object {$_.Operand -is [string] -and $_.Operand -eq '_EmissionColor'}).Count -gt 0) 'Windows and base must use emissive orange material'
$click=($ui.Methods|Where-Object Name -eq 'TravelTo').Body.Instructions
Check (@($click|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'AtlasUi::Open\('}).Count -eq 0) 'A map-marker click must not open Atlas from the ordinary map'
$render=($ui.Methods|Where-Object Name -eq 'RenderAboveMap').Body.Instructions
Check (@($render|Where-Object {$_.Operand -and $_.Operand.ToString() -eq 'System.Boolean OctoberJemini.PortalAtlas.AtlasUi::active'}).Count -gt 0) 'Atlas rendering must check explicit Atlas activation'


Check (($visual.Methods|Where-Object Name -eq 'Build').Body.ExceptionHandlers.Count -gt 0) 'Optional orb visual failure must not stop piece registration'
$icons=$mod.MainModule.Types|Where-Object Name -eq 'Icons'
$style=($icons.Methods|Where-Object Name -eq 'MatchNativePortal').Body.Instructions
Check (@($style|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'NativePinStyle::Build'}).Count -eq 1) 'Pillar map style must sample actual native portal artwork'
Check (($icons.Methods|Where-Object Name -eq 'MatchNativePortal').Body.ExceptionHandlers.Count -gt 0) 'Optional map styling errors must keep a working fallback icon'
Check (@($build|Where-Object {$_.Operand -is [string] -and $_.Operand -eq 'snappoint'}).Count -eq 1) 'Waystone build must explicitly remove inherited snap points'
$snapping=($game.MainModule.Types|Where-Object Name -eq 'Player').Methods|Where-Object Name -eq 'FindClosestSnapPoints'
Check (@($snapping.Body.Instructions|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'Piece::GetSnapPoints'}).Count -gt 0) 'Installed native snapping must use piece snap points'
$rename=$mod.MainModule.Types|Where-Object Name -eq 'AtlasRenameDialog'
$open=($rename.Methods|Where-Object Name -eq 'Open').Body.Instructions
Check (@($open|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'get_onSubmit'}).Count -eq 1) 'Atlas rename must bind explicit input submit'
$callbacks=@($rename.Methods)+@($rename.NestedTypes|ForEach-Object {$_.Methods})
Check (@($callbacks|ForEach-Object {$_.Body.Instructions}|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'TextInput::OnEnter'}).Count -eq 1) 'Atlas Enter callback must invoke native commit exactly once'
$textInput=$game.MainModule.Types|Where-Object Name -eq 'TextInput'
foreach($methodName in @('OnInput','OnEnter')){
 Check (@(($textInput.Methods|Where-Object Name -eq $methodName).Body.Instructions|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'TextInput::setText'}).Count -eq 1) 'Native keyboard and button actions must share the same commit path'
}
$commit=($textInput.Methods|Where-Object Name -eq 'setText').Body.Instructions
Check (@($commit|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'TextReceiver::SetText'}).Count -eq 1) 'Native commit must dispatch to the registered receiver'
Check (@($commit|Where-Object {$_.OpCode.Name -eq 'stfld' -and $_.Operand.ToString() -match 'm_queuedSign'}).Count -eq 1) 'Native commit must clear the receiver to prevent duplicate rename submissions'
$network=$mod.MainModule.Types|Where-Object Name -eq 'AtlasNetwork'
Check (@(($network.Methods|Where-Object Name -eq 'Tick').Body.Instructions|Where-Object {$_.Operand -and $_.Operand.ToString() -match 'AtlasNetwork::RequestList'}).Count -eq 1) 'Rename acknowledgements must refresh the catalogue even with the Atlas map closed'
"Passed $checks installed-game API, Harmony target, activation-route and embedded asset checks."
