param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '..\..'),
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe',
    [switch]$PrepareOnly
)

$ErrorActionPreference = 'Stop'
$duoProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$duoStage = Join-Path $duoProjectRoot 'tmp\IPhoneDuoUnityVerify'
$duoSource = Join-Path $duoProjectRoot 'Assets\SecretsThatBreathe\Props\IPhoneDuo'
$duoTarget = Join-Path $duoStage 'Assets\SecretsThatBreathe\Props\IPhoneDuo'
$duoReport = Join-Path $duoProjectRoot 'ArtSource\IPhoneDuo'
foreach ($duoFolder in @($duoTarget, (Join-Path $duoStage 'Packages'), (Join-Path $duoStage 'ProjectSettings'), $duoReport)) {
    New-Item -ItemType Directory -Path $duoFolder -Force | Out-Null
}
Get-ChildItem -LiteralPath $duoSource -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $duoTarget -Recurse -Force
}
if (Test-Path -LiteralPath ($duoSource + '.meta')) {
    Copy-Item -LiteralPath ($duoSource + '.meta') -Destination ($duoTarget + '.meta') -Force
}
Copy-Item -LiteralPath (Join-Path $duoProjectRoot 'ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $duoStage 'ProjectSettings\ProjectVersion.txt') -Force
Copy-Item -LiteralPath (Join-Path $duoProjectRoot 'ProjectSettings\ProjectSettings.asset') -Destination (Join-Path $duoStage 'ProjectSettings\ProjectSettings.asset') -Force
@'
{"dependencies":{"com.unity.render-pipelines.universal":"17.3.0","com.unity.modules.physics":"1.0.0","com.unity.modules.imageconversion":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}
'@ | Set-Content -LiteralPath (Join-Path $duoStage 'Packages\manifest.json') -Encoding utf8
if ($PrepareOnly) {
    Write-Output "Prepared independent Unity project: $duoStage"
    return
}
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity editor not found: $UnityPath" }
if (-not (Test-Path -LiteralPath (Join-Path $duoTarget 'Models\IPhoneDuo.fbx'))) { throw 'Export Models/IPhoneDuo.fbx before running the build.' }
$env:IPHONE_DUO_REPORT_DIR = $duoReport
$duoLog = Join-Path $duoReport 'Unity_Build.log'
$duoArguments = @('-batchmode', '-projectPath', ('"' + $duoStage + '"'), '-executeMethod', 'SecretsThatBreathe.IPhoneDuo.Editor.IPhoneDuoAssetBuilder.BatchBuild', '-logFile', ('"' + $duoLog + '"'))
$duoProcess = Start-Process -FilePath $UnityPath -ArgumentList $duoArguments -WindowStyle Hidden -PassThru
Write-Output "Unity verification process $($duoProcess.Id); log: $duoLog"
$duoProcess.WaitForExit()
if ($duoProcess.ExitCode -ne 0) { throw "Unity verification failed with exit code $($duoProcess.ExitCode). See $duoLog" }
$duoVerification = Join-Path $duoTarget 'UnityVerification.json'
if (-not (Test-Path -LiteralPath $duoVerification)) { throw 'Unity did not produce a verification report.' }
$duoResult = Get-Content -LiteralPath $duoVerification -Raw | ConvertFrom-Json
if ($duoResult.status -ne 'PASS') { throw 'Unity verification did not pass.' }
# Copy generated assets and their GUIDs without overwriting concurrently refined source art or code.
foreach ($duoGeneratedFolder in @('Materials', 'Prefabs', 'Preview', 'Demo')) {
    $duoGeneratedPath = Join-Path $duoTarget $duoGeneratedFolder
    if (Test-Path -LiteralPath $duoGeneratedPath) {
        Copy-Item -LiteralPath $duoGeneratedPath -Destination $duoSource -Recurse -Force
    }
}
Copy-Item -LiteralPath $duoVerification -Destination (Join-Path $duoSource 'UnityVerification.json') -Force
Get-ChildItem -LiteralPath $duoTarget -Filter '*.meta' -Recurse -Force | ForEach-Object {
    $duoRelativeMeta = [System.IO.Path]::GetRelativePath($duoTarget, $_.FullName)
    $duoMetaDestination = Join-Path $duoSource $duoRelativeMeta
    New-Item -ItemType Directory -Path (Split-Path -Parent $duoMetaDestination) -Force | Out-Null
    Copy-Item -LiteralPath $_.FullName -Destination $duoMetaDestination -Force
}
if (Test-Path -LiteralPath ($duoTarget + '.meta')) {
    Copy-Item -LiteralPath ($duoTarget + '.meta') -Destination ($duoSource + '.meta') -Force
}
Write-Output "Imported verified iPhone DUO assets into $duoSource"
Write-Output "Triangles: $($duoResult.triangles); preview renders: $duoSource\Preview; report: $duoVerification"
