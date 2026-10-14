$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$sourceRoot = Join-Path $projectRoot 'Assets/GUY/Model/MEMEE'
$targetRoot = Join-Path $projectRoot 'Assets/SecretsThatBreathe/Props/MamaEmpty'
if (-not $targetRoot.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar)) { throw 'Target outside project' }
if (Test-Path -LiteralPath $targetRoot) { throw 'Target already exists' }
$guidMap = @{}
foreach ($kind in @('Materials','Textures')) {
    $dest = Join-Path $targetRoot $kind
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $sourceRoot $kind) -File) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $dest $file.Name)
        if ($file.Extension -eq '.meta') {
            $oldGuid = [regex]::Match([IO.File]::ReadAllText($file.FullName), '(?m)^guid: ([a-f0-9]{32})').Groups[1].Value
            if (-not $oldGuid) { throw "Missing GUID: $($file.Name)" }
            $guidMap[$oldGuid] = [guid]::NewGuid().ToString('N')
        }
    }
}
foreach ($kind in @('Models','Prefabs')) { New-Item -ItemType Directory -Path (Join-Path $targetRoot $kind) -Force | Out-Null }
foreach ($name in @('MEMEEE_Empty.fbx','MEMEEE_Empty.fbx.meta','MEMEEE_Empty.prefab','MEMEEE_Empty.prefab.meta')) {
    $kind = if ($name -like '*.fbx*') { 'Models' } else { 'Prefabs' }
    $sourcePath = (Resolve-Path -LiteralPath (Join-Path $sourceRoot $name)).Path
    if (-not $sourcePath.StartsWith($sourceRoot + [IO.Path]::DirectorySeparatorChar)) { throw 'Move outside source folder' }
    Move-Item -LiteralPath $sourcePath -Destination (Join-Path (Join-Path $targetRoot $kind) $name)
}
foreach ($file in Get-ChildItem -LiteralPath $targetRoot -Recurse -File) {
    if ($file.Extension -in @('.meta','.mat','.prefab')) {
        $content = [IO.File]::ReadAllText($file.FullName)
        foreach ($oldGuid in $guidMap.Keys) { $content = $content.Replace($oldGuid, $guidMap[$oldGuid]) }
        [IO.File]::WriteAllText($file.FullName, $content)
        foreach ($oldGuid in $guidMap.Keys) {
            if ($content.Contains($oldGuid)) { throw "Shared reference remains: $($file.FullName)" }
        }
    }
}
foreach ($folder in @($targetRoot, (Join-Path $targetRoot 'Models'), (Join-Path $targetRoot 'Prefabs'), (Join-Path $targetRoot 'Materials'), (Join-Path $targetRoot 'Textures'))) {
    [IO.File]::WriteAllText($folder + '.meta', "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n")
}
$guidMap | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $projectRoot 'ArtSource/MamaEmpty/independent_asset_guids.json')
Write-Output "Moved model and prefab; duplicated $($guidMap.Count) materials/textures with independent GUIDs and remapped references."
