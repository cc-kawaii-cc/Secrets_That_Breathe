$ErrorActionPreference = 'Stop'
$path = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Assets/Nemo_Upgrade/MainScene_2_Evening.unity'))
$original = [IO.File]::ReadAllText($path)
$script:scene = $original.Replace("`r`n", "`n")
function Block($id) {
    $m = [regex]::Match($script:scene, "(?ms)^--- !u!\d+ &$id(?: stripped)?\n.*?(?=^---|\z)")
    if (!$m.Success) { throw "Missing object $id" }; return $m.Value
}
function ReplaceBlock($id, $replacement) { $old = Block $id; $script:scene = $script:scene.Replace($old, $replacement) }
foreach($id in 900260001,900260002,900260003,900260004) { if($scene -match "(?m)^--- !u!\d+ &$id(?:\s|$)"){throw "ID conflict $id"} }
$ride = Block 1116060303
$fd = Block 606606217
if($ride -notmatch 'm_Enabled: 1' -or $fd -notmatch 'value: FD\n' -or $fd -notmatch 'm_AddedComponents: \[\]'){throw 'Unexpected car setup'}
$backup = Join-Path $PSScriptRoot ('BeforeFD-' + (Get-Date -Format yyyyMMdd-HHmmss) + '.unity')
[IO.File]::WriteAllText($backup, $original, [Text.UTF8Encoding]::new($false))
ReplaceBlock 1116060303 ($ride.Replace('m_Enabled: 1','m_Enabled: 0').Replace('playOnStart: 1','playOnStart: 0'))
$porsche = Block 1116060300
$activeOverride = @'
    - target: {fileID: 203749215052050788, guid: dc0beb625b5239c439aa5d6edc2aa3d8, type: 3}
      propertyPath: m_IsActive
      value: 0
      objectReference: {fileID: 0}
'@
$activePattern='(- target: \{fileID: 203749215052050788, guid: dc0beb625b5239c439aa5d6edc2aa3d8, type: 3\}\n      propertyPath: m_IsActive\n      value: )[^\n]+'
if($porsche -match $activePattern){$porsche=[regex]::Replace($porsche,$activePattern,'${1}0')}
else {$porsche=$porsche.Replace('    m_RemovedComponents:', $activeOverride + "`n    m_RemovedComponents:")}
ReplaceBlock 1116060300 $porsche
$start = [regex]::Match($ride, 'startPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}')
if(!$start.Success){throw 'Missing route'}
$axes=@('x','y','z')
for($i=0;$i -lt 3;$i++) {
    $pattern='(propertyPath: m_LocalPosition\.'+$axes[$i]+'\n      value: )[^\n]+'
    $fd=[regex]::Replace($fd,$pattern,'${1}'+$start.Groups[$i+1].Value)
}
$additions=@'
    - targetCorrespondingSourceObject: {fileID: 7600716200271561881, guid: 7c2d424fd0e654742ac13850a6a08ea8, type: 3}
      insertIndex: -1
      addedObject: {fileID: 900260004}
    m_AddedComponents:
    - targetCorrespondingSourceObject: {fileID: 2078690010631970122, guid: 7c2d424fd0e654742ac13850a6a08ea8, type: 3}
      insertIndex: -1
      addedObject: {fileID: 900260002}
'@
ReplaceBlock 606606217 ($fd.Replace('    m_AddedComponents: []',$additions))
$newRide=$ride.Replace('&1116060303','&900260002').Replace('m_GameObject: {fileID: 1116060301}','m_GameObject: {fileID: 900260001}').Replace('driverSeat: {fileID: 2048486615}','driverSeat: {fileID: 900260004}')
$seatObject=(Block 2048486614).Replace('&2048486614','&900260003').Replace('component: {fileID: 2048486615}','component: {fileID: 900260004}')
$seatTransform=(Block 2048486615).Replace('&2048486615','&900260004').Replace('m_GameObject: {fileID: 2048486614}','m_GameObject: {fileID: 900260003}').Replace('m_Father: {fileID: 1116060302}','m_Father: {fileID: 606606218}')
# FD steering-wheel bounds (FBX cm, reflected X on Unity import), eyes 55cm behind wheel.
$seatTransform=[regex]::Replace($seatTransform,'m_LocalPosition: \{[^}]+\}','m_LocalPosition: {x: -0.3143174, y: 1.13, z: -0.0620665}')
$fdObject=@'
--- !u!1 &900260001 stripped
GameObject:
  m_CorrespondingSourceObject: {fileID: 2078690010631970122, guid: 7c2d424fd0e654742ac13850a6a08ea8, type: 3}
  m_PrefabInstance: {fileID: 606606217}
  m_PrefabAsset: {fileID: 0}
'@
$scene=$scene.Replace('--- !u!1660057539 &9223372036854775807',$fdObject+"`n"+$newRide+$seatObject+$seatTransform+'--- !u!1660057539 &9223372036854775807')
$blocks=[regex]::Matches($scene,'(?ms)^--- !u!\d+ &([^\n]+)\n.*?(?=^---|\z)')
$duplicates=$blocks | ForEach-Object {$_.Groups[1].Value} | Group-Object | Where-Object Count -gt 1
if($duplicates){throw 'Duplicate IDs'}
$rides=@($blocks | Where-Object {$_.Value.Contains('Assembly-CSharp::SecretsThatBreathe.PorschePassengerRide')})
$enabled=@($rides | Where-Object {$_.Value -match 'm_Enabled: 1\n'})
if($enabled.Count -ne 1 -or $enabled[0].Value -notmatch 'm_GameObject: \{fileID: 900260001\}'){throw 'Invalid ride owner'}
[IO.File]::WriteAllText($path,$scene,[Text.UTF8Encoding]::new($false))
$report=[ordered]@{success=$true;backup=$backup;car='FD';seatLocalPosition=@{x=-0.3143174;y=1.13;z=-0.0620665};raisedMetres=0.1;enabledRides=$enabled.Count;originalPorscheInactive=$true;routeAndPlayerReferencesCopied=$true;playModeTested=$false}
$report | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $PSScriptRoot 'validation.json') -Encoding utf8
$report | ConvertTo-Json -Depth 4
