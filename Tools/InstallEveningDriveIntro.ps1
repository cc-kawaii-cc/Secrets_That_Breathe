$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scenePath = Join-Path $project 'Assets/Nemo_Upgrade/MainScene_2_Evening.unity'
$lawyerPath = Join-Path $project 'Assets/Tonpalm/Lawyer Room.unity'
$original = [IO.File]::ReadAllText($scenePath)
$scene = $original.Replace("`r`n", "`n")
$lawyer = [IO.File]::ReadAllText($lawyerPath).Replace("`r`n", "`n")
if ($scene.Contains('SecretsThatBreathe.EveningDriveIntro')) { throw 'Evening intro is already installed.' }
$sentence = 'หลังจากเข้มได้รับทำคดีจากลุงป้อง แล้วเข้มก็ได้รีบขับรถไปยังที่เกิดเหตุทันที เพื่อที่จะไปตรวจสอบหาหลักฐาน ก่อนที่ความจริงจะถูกลบออกไป...'
$ids = [ordered]@{}
for ($i=1; $i -le 10; $i++) { $ids[[string](710400000+$i)] = [string](730900000+$i) }
$ids['710400035'] = '730900011'
for ($i=1; $i -le 6; $i++) { $ids[[string](710600000+$i)] = [string](730900020+$i) }
foreach ($id in $ids.Values) { if ($scene -match "(?m)^--- !u!\d+ &$id(?:\s|$)") { throw "ID collision $id" } }
$newBlocks = [Collections.Generic.List[string]]::new()
foreach ($oldId in $ids.Keys) {
    $match = [regex]::Match($lawyer, "(?ms)^--- !u!\d+ &$oldId\n.*?(?=^---|\z)")
    if (!$match.Success) { throw "Missing Lawyer UI block $oldId" }
    $block = $match.Value
    foreach ($mapId in $ids.Keys) { $block = $block.Replace('&'+$mapId+"`n", '&'+$ids[$mapId]+"`n").Replace('fileID: '+$mapId+'}', 'fileID: '+$ids[$mapId]+'}') }
    switch ([string]$oldId) {
        '710400001' { $block = $block.Replace('LawyerRoomIntroCanvas','EveningDriveIntroCanvas') }
        '710400002' {
            $block = [regex]::Replace($block, '(?s)  m_Children:.*?  m_Father: \{fileID: \d+\}', "  m_Children:`n  - {fileID: 730900008}`n  - {fileID: 730900022}`n  m_Father: {fileID: 0}")
        }
        '710400006' { $block = $block.Replace('m_BlocksRaycasts: 0','m_BlocksRaycasts: 1') }
        '710400035' {
            $block = @'
--- !u!114 &730900011
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 730900001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 845961d1bc0c4b2192963fd768d2f3e1, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::SecretsThatBreathe.EveningDriveIntro
  screen: {fileID: 730900006}
  storyText: {fileID: 730900024}
  storyGroup: {fileID: 730900026}
  blackDelaySeconds: 2
  secondsPerCharacter: 0.05
  completedTextHoldSeconds: 2
  textFadeOutSeconds: 1
  revealGameSeconds: 2
  movementStartTextFadeProgress: 0.7
  maximumStreamingWaitSeconds: 10
'@ + "`n"
        }
        '710600001' { $block = $block.Replace('Intro2Text','EveningStoryText') }
        '710600002' { $block = $block.Replace('m_AnchorMin: {x: 0.08, y: 0.4}','m_AnchorMin: {x: 0.08, y: 0.3}').Replace('m_AnchorMax: {x: 0.92, y: 0.6}','m_AnchorMax: {x: 0.92, y: 0.7}') }
        '710600004' { $block = [regex]::Replace($block, '(?m)^  m_text:.*$', '  m_text: '+(ConvertTo-Json $sentence -Compress)) }
        '710600006' { $block = $block.Replace('m_Alpha: 1','m_Alpha: 0') }
    }
    $newBlocks.Add($block)
}
$scene = $scene.Replace('--- !u!1660057539 &9223372036854775807', ($newBlocks -join '') + '--- !u!1660057539 &9223372036854775807')
$scene += "  - {fileID: 730900002}`n"
$objects = [regex]::Matches($scene,'(?m)^--- !u!\d+ &(\d+)') | ForEach-Object {$_.Groups[1].Value}
if ($objects | Group-Object | Where-Object Count -gt 1) { throw 'Duplicate scene object IDs' }
$newText = [regex]::Match($scene,'(?ms)^--- !u!114 &730900024\n.*?(?=^---|\z)').Value
$savedSentence = ([regex]::Match($newText,'(?m)^  m_text: (.*)$').Groups[1].Value | ConvertFrom-Json)
if ($savedSentence -cne $sentence) { throw 'Authored sentence mismatch' }
foreach ($reference in [regex]::Matches(($newBlocks -join ''), 'fileID: (730900\d+)')) {
    if ($objects -notcontains $reference.Groups[1].Value) { throw 'Missing intro UI reference' }
}
$work = Join-Path $project 'Tools/EveningIntro'
[IO.Directory]::CreateDirectory($work) | Out-Null
$backup = Join-Path $work ('BeforeIntro-'+(Get-Date -Format yyyyMMdd-HHmmss)+'.unity')
[IO.File]::WriteAllText($backup,$original,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText($scenePath,$scene,[Text.UTF8Encoding]::new($false))
[ordered]@{success=$true;scene=$scenePath;backup=$backup;sentence=$savedSentence;blackDelaySeconds=2;secondsPerCharacter=0.05;font='Kanit-Medium SDF (same as Lawyer Room)';fontSize=40;whiteText=$true;centered=$true;newBlocks=$newBlocks.Count;playModeTested=$false} | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $work 'validation.json') -Encoding utf8
Get-Content (Join-Path $work 'validation.json')
