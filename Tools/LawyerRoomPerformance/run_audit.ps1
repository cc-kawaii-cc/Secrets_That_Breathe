$ErrorActionPreference = 'Stop'
$workspaceRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$auditProject = (Resolve-Path -LiteralPath (Join-Path $workspaceRoot 'Tools/CompileChecks/LawyerRoomAuditUnity')).Path
if (-not $auditProject.StartsWith($workspaceRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected audit project' }
$unityExecutable = 'C:/Program Files/Unity/Hub/Editor/6000.3.8f1/Editor/Unity.exe'
$logPath = Join-Path $PSScriptRoot 'unity-audit.log'
& $unityExecutable -batchmode -nographics -projectPath $auditProject -executeMethod SecretsThatBreathe.Editor.LawyerRoomPerformanceAudit.Batch -logFile $logPath | Out-Null
exit $LASTEXITCODE
