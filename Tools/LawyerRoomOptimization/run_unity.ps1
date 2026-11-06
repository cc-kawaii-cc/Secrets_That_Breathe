$ErrorActionPreference = 'Stop'
$workspaceRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$auditProject = (Resolve-Path -LiteralPath (Join-Path $workspaceRoot 'Tools/CompileChecks/LawyerRoomAuditUnity')).Path
if (-not $auditProject.StartsWith($workspaceRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected project path' }
$logPath = Join-Path $PSScriptRoot 'unity-build.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.8f1/Editor/Unity.exe' -batchmode -nographics -projectPath $auditProject -executeMethod BuildOptimizedLawyerChair.Run -logFile $logPath | Out-Null
exit $LASTEXITCODE
