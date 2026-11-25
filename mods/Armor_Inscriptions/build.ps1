param(
 [string]$ManagedPath='C:/Program Files (x86)/Steam/steamapps/common/Valheim/valheim_Data/Managed',
 [string]$BepInExPath='C:/Users/che/AppData/Roaming/Thunderstore Mod Manager/DataFolder/Valheim/profiles/October Jemini 1.0 (Valheim)/BepInEx',
 [string]$CompilerPath='C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
)
$ErrorActionPreference='Stop'
$ManagedPath=(Resolve-Path $ManagedPath).Path
$BepInExPath=(Resolve-Path $BepInExPath).Path
$refs=@('mscorlib','System','System.Core','netstandard','assembly_valheim','assembly_utils','assembly_guiutils','UnityEngine','UnityEngine.CoreModule','UnityEngine.PhysicsModule','UnityEngine.TextRenderingModule','UnityEngine.UIModule','UnityEngine.ImageConversionModule','UnityEngine.InputLegacyModule','Unity.TextMeshPro','UnityEngine.UI','UnityEngine.AssetBundleModule','Newtonsoft.Json','SoftReferenceableAssets')|ForEach-Object {'/reference:'+(Join-Path $ManagedPath "$_.dll")}
$refs+=@("/reference:$BepInExPath\core\BepInEx.dll","/reference:$BepInExPath\core\0Harmony.dll","/reference:$BepInExPath\plugins\ValheimModding-Jotunn\Jotunn.dll")
$resources=@('runner','leaper','cultivator','builder','snorkel','tidewalker','bench','rack','stitching')|ForEach-Object {"/resource:$(Join-Path $PSScriptRoot "assets\$_.png"),patch.$_.png"}
$sources=@(Get-ChildItem (Join-Path $PSScriptRoot 'src') -Filter '*.cs'|ForEach-Object FullName)
& $CompilerPath /nologo /noconfig /target:library /optimize+ /nostdlib+ /warnaserror+ /nowarn:1701 "/out:$PSScriptRoot\ArmorInscriptions.dll" @refs @resources @sources
if($LASTEXITCODE -ne 0){throw 'Armor Inscriptions build failed'}
