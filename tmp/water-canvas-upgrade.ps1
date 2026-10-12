$scenePath=(Resolve-Path -LiteralPath 'Assets/Tonpalm/Lawyer Room.unity').Path
$sceneText=[IO.File]::ReadAllText($scenePath)
$blocks=[regex]::Split($sceneText,'(?m)(?=^--- !u!)')
$newIds= @(2119876100..2119876105)+@(2119876110..2119876115)+@(2119876120..2119876122)
foreach($id in $newIds) { if($sceneText -match ('&'+$id+'\b')) { throw "Scene ID already exists: $id" } }
$component=$blocks | Where-Object {$_ -match '^--- !u!114 &2109876101\b'}
$messageValue=[regex]::Match($component,'(?m)^  message: (.*)').Groups[1].Value.TrimEnd("`r")
if(!$messageValue) { throw 'Machine message not found' }
$cloneSourceIds=@(1813318281,1813318282,1776115747,1776115750,1776115749,1776115748)
function Copy-WaterPanel([long]$firstId,[string]$name,[string]$labelName,[string]$textValue,[float]$anchor,[int]$width,[int]$height) {
    $clones=@()
    foreach($sourceId in $cloneSourceIds) {
        $block=$blocks | Where-Object {$_ -match ('^--- !u!\d+ &'+$sourceId+'\b')}
        if(!$block) {throw "Missing template document: $sourceId"}
        $clones+=$block
    }
    $clone=$clones -join ''
    for($i=0;$i -lt $cloneSourceIds.Length;$i++) {
        $clone=[regex]::Replace($clone,'(?<!\d)'+$cloneSourceIds[$i]+'(?!\d)',($firstId+$i).ToString())
    }
    $clone=[regex]::Replace($clone,'(?m)^  - component: \{fileID: 181331828[34]\}\r?\n','')
    $clone=$clone.Replace('m_Name: SitPrompt',"m_Name: $name").Replace('m_Name: PromptText',"m_Name: $labelName")
    $clone=[regex]::Replace($clone,'(?m)^  m_text: .*$',"  m_text: $textValue")
    $clone=$clone.Replace('m_AnchorMin: {x: 0.5, y: 0.25}',"m_AnchorMin: {x: 0.5, y: $anchor}").Replace('m_AnchorMax: {x: 0.5, y: 0.25}',"m_AnchorMax: {x: 0.5, y: $anchor}")
    $clone=$clone.Replace('m_AnchoredPosition: {x: 0, y: -100}','m_AnchoredPosition: {x: 0, y: 0}').Replace('m_SizeDelta: {x: 560, y: 76}',"m_SizeDelta: {x: $width, y: $height}")
    return $clone
}
$newDocs=Copy-WaterPanel 2119876100 'WaterMachinePrompt' 'PromptText' '"[E] สำรวจตู้กดน้ำ"' .25 560 100
$newDocs+=Copy-WaterPanel 2119876110 'WaterMachineMessage' 'MessageText' $messageValue .16 1180 120
for($i=0;$i -lt $blocks.Length;$i++) {
    if($blocks[$i] -match '^--- !u!224 &1676370658\b') {
        $blocks[$i]=$blocks[$i].Replace('  m_Father: {fileID: 0}',"  - {fileID: 2119876101}`n  - {fileID: 2119876111}`n  m_Father: {fileID: 0}")
    }
    if($blocks[$i] -match '^--- !u!1001 &1745743667\b') {
        $blocks[$i]=$blocks[$i].Replace('    m_AddedGameObjects: []',"    m_AddedGameObjects:`n    - targetCorrespondingSourceObject: {fileID: 8388358614469977985, guid: bed8a6f423fd4d444b27a859943f3aeb, type: 3}`n      insertIndex: -1`n      addedObject: {fileID: 2119876121}")
    }
    if($blocks[$i] -match '^--- !u!114 &2109876101\b') {
        $blocks[$i]=[regex]::Replace($blocks[$i],'(?m)^  promptTemplate:.*\r?\n',"  hintPanel: {fileID: 2119876100}`n  messagePanel: {fileID: 2119876110}`n  interactionZone: {fileID: 2119876122}`n  playerBody: {fileID: 0}`n")
        $blocks[$i]=[regex]::Replace($blocks[$i],'(?m)^  (interactionDistance|message):.*\r?\n','')
    }
}
$newDocs+=@'
--- !u!1 &2119876120
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 2119876121}
  - component: {fileID: 2119876122}
  m_Layer: 0
  m_Name: InteractionZone
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &2119876121
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2119876120}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 3.3333333, y: 2.5, z: 3.3333333}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 1745743668}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!65 &2119876122
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2119876120}
  m_Material: {fileID: 0}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 1
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {x: 5, y: 3, z: 5}
  m_Center: {x: 0, y: 1.2, z: 0}
'@
[IO.File]::WriteAllText($scenePath,($blocks -join '')+$newDocs+"`n",[Text.UTF8Encoding]::new($false))
'Persistent Canvas panels and editable trigger zone added.'
