param(
 [string]$ManagedPath='C:/Program Files (x86)/Steam/steamapps/common/Valheim/valheim_Data/Managed',
 [string]$BepInExPath='C:/Users/che/AppData/Roaming/Thunderstore Mod Manager/DataFolder/Valheim/profiles/October Jemini 1.0 (Valheim)/BepInEx',
 [string]$CompilerPath='C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
)
$ErrorActionPreference='Stop'
& "$PSScriptRoot/build.ps1" -ManagedPath $ManagedPath -BepInExPath $BepInExPath -CompilerPath $CompilerPath
$stage=Join-Path $PSScriptRoot 'test-output'
New-Item -ItemType Directory -Force $stage | Out-Null
& $CompilerPath /nologo /warnaserror+ "/out:$stage\PolicyChecks.exe" "$PSScriptRoot\src\Rules.cs" "$PSScriptRoot\src\ExtensionRules.cs" "$PSScriptRoot\src\ArmorTiers.cs" "$PSScriptRoot\tests\PolicyChecks.cs"
if($LASTEXITCODE -ne 0){throw 'Policy check compilation failed'}
& "$stage/PolicyChecks.exe"
if($LASTEXITCODE -ne 0){throw 'Policy checks failed'}
$cecil=(Resolve-Path "$BepInExPath/core/Mono.Cecil.dll").Path
Copy-Item $cecil "$stage/Mono.Cecil.dll" -Force
& $CompilerPath /nologo /warnaserror+ "/reference:$cecil" "/out:$stage\AuditBuild.exe" "$PSScriptRoot\tests\AuditBuild.cs"
if($LASTEXITCODE -ne 0){throw 'Audit compilation failed'}
& "$stage/AuditBuild.exe" "$PSScriptRoot/ArmorInscriptions.dll" "$ManagedPath/assembly_valheim.dll" "$BepInExPath/plugins/RandyKnapp-EpicLoot/EpicLoot.dll"
if($LASTEXITCODE -ne 0){throw 'Audit failed'}
