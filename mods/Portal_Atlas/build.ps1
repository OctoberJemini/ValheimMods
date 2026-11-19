param(
 [string]$ManagedPath='C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed',
 [string]$BepInExPath='C:\Users\che\AppData\Roaming\Thunderstore Mod Manager\DataFolder\Valheim\profiles\October Jemini 1.0 (Valheim)\BepInEx',
 [string]$CompilerPath=(Join-Path $PSScriptRoot '..\..\work\tools\roslyn\tasks\net472\csc.exe')
)
$ErrorActionPreference='Stop'
$refs=@('mscorlib','System','System.Core','netstandard','assembly_valheim','assembly_utils','assembly_guiutils','Splatform','UnityEngine','UnityEngine.CoreModule','UnityEngine.AssetBundleModule','UnityEngine.ImageConversionModule','UnityEngine.PhysicsModule','UnityEngine.ParticleSystemModule','UnityEngine.InputLegacyModule','UnityEngine.UI','UnityEngine.UIModule','UnityEngine.TextRenderingModule','UnityEngine.JSONSerializeModule','Unity.TextMeshPro') | ForEach-Object { '/reference:'+(Join-Path $ManagedPath "$_.dll") }
$refs+=@("/reference:$BepInExPath\core\BepInEx.dll","/reference:$BepInExPath\core\0Harmony.dll","/reference:$BepInExPath\plugins\ValheimModding-Jotunn\Jotunn.dll")
$sources=@(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
$resources=@(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'assets') -Filter '*.png' | ForEach-Object { '/resource:'+ $_.FullName+',PortalAtlas.'+$_.Name })
$resources+='/resource:'+(Join-Path $PSScriptRoot 'model/waystone.mesh')+',PortalAtlas.waystone.mesh'
& $CompilerPath /nologo /noconfig /target:library /optimize+ /nostdlib+ /warnaserror+ "/out:$PSScriptRoot\PortalAtlas.dll" @refs @resources @sources
if($LASTEXITCODE -ne 0){throw 'Portal Atlas compilation failed'}
