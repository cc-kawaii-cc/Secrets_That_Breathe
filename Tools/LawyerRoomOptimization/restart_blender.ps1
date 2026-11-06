$ErrorActionPreference = 'Stop'
Get-CimInstance Win32_Process -Filter "Name='blender.exe'" | ForEach-Object {
    if ($_.CommandLine -and $_.CommandLine.Contains('LawyerRoomOptimization\optimize_chair.py')) {
        Stop-Process -Id $_.ProcessId -Force
    }
}
