param(
 [string]$ManagedPath='C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed',
 [string]$BepInExPath='C:\Users\che\AppData\Roaming\Thunderstore Mod Manager\DataFolder\Valheim\profiles\October Jemini 1.0 (Valheim)\BepInEx',
 [Parameter(Mandatory=$true)][string]$CompilerPath
)
$ErrorActionPreference='Stop'
& "$PSScriptRoot\..\build.ps1" -ManagedPath $ManagedPath -BepInExPath $BepInExPath -CompilerPath $CompilerPath
& "$PSScriptRoot\IntegrationChecks.ps1" -ManagedPath $ManagedPath -BepInExPath $BepInExPath
& $CompilerPath /nologo /target:exe "/out:$PSScriptRoot\DomainChecks.exe" "$PSScriptRoot\DomainChecks.cs" "$PSScriptRoot\..\src\AtlasDomain.cs" "$PSScriptRoot\..\src\FavouriteStore.cs"
if($LASTEXITCODE -ne 0){throw 'Domain check compilation failed'}
& "$PSScriptRoot\DomainChecks.exe"
if($LASTEXITCODE -ne 0){throw 'Domain checks failed'}
& $CompilerPath /nologo /target:exe "/out:$PSScriptRoot\NativeChecks.exe" "/reference:$ManagedPath\netstandard.dll" "/reference:$ManagedPath\assembly_valheim.dll" "/reference:$ManagedPath\assembly_utils.dll" "/reference:$ManagedPath\UnityEngine.CoreModule.dll" "$PSScriptRoot\NativeChecks.cs"
if($LASTEXITCODE -ne 0){throw 'Native check compilation failed'}
$env:ATLAS_GAME_MANAGED=$ManagedPath
$env:ATLAS_BEP_CORE=Join-Path $BepInExPath 'core'
$env:ATLAS_JOTUNN=Join-Path $BepInExPath 'plugins\ValheimModding-Jotunn'
dotnet "$PSScriptRoot\NativeChecks.exe"
if($LASTEXITCODE -ne 0){throw 'Native checks failed'}
