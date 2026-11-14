param([Parameter(Mandatory=$true)][string]$CompilerPath)
$ErrorActionPreference='Stop'
$taskOut=Join-Path $PSScriptRoot 'bin\tests'
New-Item -ItemType Directory -Force $taskOut | Out-Null
$taskMod=Join-Path $PSScriptRoot 'mods\Auto_Mapping'
& $CompilerPath /nologo /warnaserror+ "/out:$taskOut\AutoMappingPinChecks.exe" "/resource:$taskMod\assets\aio-landmarks.tsv,QuietMap.AioLandmarks" "$taskMod\src\LandmarkPolicy.cs" "$PSScriptRoot\tests\AutoMappingPinChecks.cs"
if($LASTEXITCODE -ne 0){throw 'Regression check compilation failed'}
& "$taskOut\AutoMappingPinChecks.exe"
if($LASTEXITCODE -ne 0){throw 'Regression checks failed'}
