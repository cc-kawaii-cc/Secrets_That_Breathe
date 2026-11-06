$ErrorActionPreference = 'Stop'
$workspaceRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$checkProject = (Resolve-Path -LiteralPath (Join-Path $workspaceRoot 'Tools/CompileChecks/GUYOptimizationUnity')).Path
if (-not $checkProject.StartsWith($workspaceRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected project path' }
# Stop only our isolated check, if a sandbox attempt stalled during licensing.
Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | ForEach-Object {
    if ($_.CommandLine -and $_.CommandLine.Contains('GUYOptimizationUnity')) {
        Stop-Process -Id $_.ProcessId -Force
    }
}
$unityExecutable = 'C:/Program Files/Unity/Hub/Editor/6000.3.8f1/Editor/Unity.exe'
$logPath = Join-Path $PSScriptRoot 'unity-check-host.log'
& $unityExecutable -batchmode -nographics -projectPath $checkProject -executeMethod GUYMeshCheck.Run -logFile $logPath | Out-Null
exit $LASTEXITCODE
