$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$root=Join-Path $project 'Assets/SecretsThatBreathe/Props/RoadBlood'
if(Test-Path (Join-Path $root 'Textures/RoadBlood_Atlas.png')){throw 'These Prefabs now use the baked texture atlas. Restore the BeforeAtlas backup before rebuilding the old geometry.'}
$work=Join-Path $project 'Tools/RoadBlood'
foreach($folder in 'Meshes','Materials','Prefabs'){[IO.Directory]::CreateDirectory((Join-Path $root $folder))|Out-Null}
[IO.Directory]::CreateDirectory($work)|Out-Null
$encoding=[Text.UTF8Encoding]::new($false)
$culture=[Globalization.CultureInfo]::InvariantCulture
function WriteText($path,$text){[IO.File]::WriteAllText($path,$text,$script:encoding)}
function Number($n){return ([double]$n).ToString('0.########',$script:culture)}
function Meta($path,$guid,$importer,$fileId=0){
    $extra=if($fileId){"  mainObjectFileID: $fileId`n"}else{''}
    WriteText ($path+'.meta') "fileFormatVersion: 2`nguid: $guid`n${importer}:`n  externalObjects: {}`n${extra}  userData:`n  assetBundleName:`n  assetBundleVariant:`n"
}
$materialGuid='d671e43c82a2497ea9c52e03a9bde144'
$materialPath=Join-Path $root 'Materials/RoadBlood_DarkRed.mat'
$material=@'
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: RoadBlood_DarkRed
  m_Shader: {fileID: 4800000, guid: 8d621fd11da24b94b8c3173de45a2abc, type: 3}
  m_Parent: {fileID: 0}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: 2980
  stringTagMap:
    RenderType: Transparent
  disabledShaderPasses: []
  m_LockedProperties:
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats:
    - _Opacity: 0.94
    - _Patchiness: 0.25
    - _NoiseScale: 48
    m_Colors:
    - _BloodColor: {r: 0.42, g: 0.016, b: 0.025, a: 1}
    - _DenseColor: {r: 0.28, g: 0.005, b: 0.013, a: 1}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
'@
if((Test-Path $materialPath) -and [IO.File]::ReadAllText($materialPath) -cne ($material+"`n")){throw 'Output already exists with edits; do not overwrite edited materials.'}
WriteText $materialPath ($material+"`n")
Meta $materialPath $materialGuid NativeFormatImporter 2100000
$meshTemplate=[IO.File]::ReadAllText((Join-Path $project 'Assets/Model Palm/Porsche V.2/Meshes/Headlight_Left.asset')).Replace("`r`n","`n")
$prefabTemplate=[IO.File]::ReadAllText((Join-Path $project 'Assets/Champ&Kichzz/Prefab/Mark/Blood_Mark.prefab')).Replace("`r`n","`n")
function GetPrefabBlock($id){return [regex]::Match($script:prefabTemplate,"(?ms)^--- !u!\d+ &$id\n.*?(?=^---|\z)").Value}
function NewGeometry(){
    $script:vertices=[Collections.Generic.List[object]]::new()
    $script:indices=[Collections.Generic.List[uint16]]::new()
}
function Vertex([double]$x,[double]$z,[double]$r,[double]$phase){
    $alpha=if($r -ge .999){0}elseif($r -ge .92){.48}elseif($r -ge .7){.90}else{1}
    $density=[Math]::Min(1,[Math]::Max(0,.96-$r*.52+.06*[Math]::Sin($phase)))
    $script:vertices.Add([pscustomobject]@{x=$x;y=.003;z=$z;density=$density;alpha=$alpha;r=$r;phase=$phase})
}
function Blob([double]$cx,[double]$cz,[double]$rx,[double]$rz,[double]$seed,[int]$segments=48){
    $first=$script:vertices.Count
    Vertex $cx $cz 0 $seed
    $rings=if($segments -le 16){@(.76,1.0)}else{@(.42,.76,.93,1.0)}
    foreach($radius in $rings){
        for($i=0;$i -lt $segments;$i++){
            $angle=$i*2*[Math]::PI/$segments
            $edge=1+.10*[Math]::Sin(3*$angle+$seed)+.055*[Math]::Sin(7*$angle+$seed*1.7)+.035*[Math]::Sin(13*$angle+$seed*.4)
            Vertex ($cx+$rx*$radius*$edge*[Math]::Cos($angle)) ($cz+$rz*$radius*$edge*[Math]::Sin($angle)) $radius ($angle+$seed)
        }
    }
    for($i=0;$i -lt $segments;$i++){
        $next=($i+1)%$segments
        foreach($index in @($first,($first+1+$next),($first+1+$i))){$script:indices.Add([uint16]$index)}
        for($ring=0;$ring -lt $rings.Count-1;$ring++){
            $inner=$first+1+$ring*$segments; $outer=$inner+$segments
            foreach($index in @(($inner+$i),($outer+$next),($outer+$i),($inner+$i),($inner+$next),($outer+$next))){$script:indices.Add([uint16]$index)}
        }
    }
}
function SaveMesh($name,$guid){
    $vstream=[IO.MemoryStream]::new();$writer=[IO.BinaryWriter]::new($vstream)
    $mins=@([double]::PositiveInfinity,[double]::PositiveInfinity,[double]::PositiveInfinity)
    $maxs=@([double]::NegativeInfinity,[double]::NegativeInfinity,[double]::NegativeInfinity)
    foreach($v in $script:vertices){
        $coords=@($v.x,$v.y,$v.z)
        for($axis=0;$axis -lt 3;$axis++){$writer.Write([single]$coords[$axis]);$mins[$axis]=[Math]::Min($mins[$axis],$coords[$axis]);$maxs[$axis]=[Math]::Max($maxs[$axis],$coords[$axis])}
        foreach($n in @(0,1,0)){$writer.Write([single]$n)}
        foreach($b in @([byte][Math]::Round($v.density*255),[byte]255,[byte]255,[byte][Math]::Round($v.alpha*255))){$writer.Write([byte]$b)}
        $writer.Write([single]$v.r);$writer.Write([single]($v.phase/(2*[Math]::PI)))
    }
    $writer.Flush();$vbytes=$vstream.ToArray();$writer.Dispose();$vstream.Dispose()
    $istream=[IO.MemoryStream]::new();$writer=[IO.BinaryWriter]::new($istream)
    foreach($index in $script:indices){$writer.Write([uint16]$index)}
    $writer.Flush();$ibytes=$istream.ToArray();$writer.Dispose();$istream.Dispose()
    if($vbytes.Length -ne $script:vertices.Count*36){throw 'Vertex stride mismatch'}
    if($ibytes.Length -ne $script:indices.Count*2){throw 'Index size mismatch'}
    $minimumArea=[double]::PositiveInfinity
    for($i=0;$i -lt $script:indices.Count;$i+=3){
        $a=$script:vertices[$script:indices[$i]];$b=$script:vertices[$script:indices[$i+1]];$c=$script:vertices[$script:indices[$i+2]]
        $area=($b.z-$a.z)*($c.x-$a.x)-($b.x-$a.x)*($c.z-$a.z)
        if($area -le 0){throw "Degenerate or reversed triangle in $name"};$minimumArea=[Math]::Min($minimumArea,$area)
    }
    $channels=[Text.StringBuilder]::new()
    for($i=0;$i -lt 14;$i++){
        $offset=0;$format=0;$dimension=0
        switch($i){0{$dimension=3};1{$offset=12;$dimension=3};3{$offset=24;$format=2;$dimension=4};4{$offset=28;$dimension=2}}
        [void]$channels.Append("    - stream: 0`n      offset: $offset`n      format: $format`n      dimension: $dimension`n")
    }
    $data="  m_VertexData:`n    serializedVersion: 3`n    m_VertexCount: $($script:vertices.Count)`n    m_Channels:`n$channels    m_DataSize: $($vbytes.Length)`n    _typelessdata: $([Convert]::ToHexString($vbytes).ToLowerInvariant())`n"
    $mesh=$script:meshTemplate.Replace('m_Name: Headlight_Left',"m_Name: $name")
    $mesh=[regex]::Replace($mesh,'(?ms)^  m_VertexData:.*?(?=^  m_CompressedMesh:)',$data)
    $mesh=[regex]::Replace($mesh,'(?m)^  m_IndexBuffer:.*$', '  m_IndexBuffer: '+[Convert]::ToHexString($ibytes).ToLowerInvariant())
    $mesh=[regex]::Replace($mesh,'(?m)^    indexCount:.*$',"    indexCount: $($script:indices.Count)")
    $mesh=[regex]::Replace($mesh,'(?m)^    vertexCount:.*$',"    vertexCount: $($script:vertices.Count)")
    $center=@();$extent=@();for($i=0;$i -lt 3;$i++){$center+=($mins[$i]+$maxs[$i])*.5;$extent+=[Math]::Max(.003,($maxs[$i]-$mins[$i])*.5)}
    $c="{x: $(Number $center[0]), y: $(Number $center[1]), z: $(Number $center[2])}"
    $e="{x: $(Number $extent[0]), y: $(Number $extent[1]), z: $(Number $extent[2])}"
    $mesh=[regex]::Replace($mesh,'(?m)^(\s*m_Center:) [^\n]+', '${1} '+$c)
    $mesh=[regex]::Replace($mesh,'(?m)^(\s*m_Extent:) [^\n]+', '${1} '+$e)
    $meshPath=Join-Path $script:root "Meshes/$name.asset"
    WriteText $meshPath $mesh; Meta $meshPath $guid NativeFormatImporter 4300000
    return [pscustomobject]@{name=$name;vertices=$script:vertices.Count;triangles=$script:indices.Count/3;width=$maxs[0]-$mins[0];depth=$maxs[2]-$mins[2];minimumTriangleArea=$minimumArea}
}
function SavePrefab($name,$meshGuid,$prefabGuid){
    $go=GetPrefabBlock '8653148305672007211';$transform=GetPrefabBlock '3885263544005871302';$filter=GetPrefabBlock '9089920773845016593';$renderer=GetPrefabBlock '7325716427944746690'
    $go=[regex]::Replace($go,'(?s)  m_Component:.*?  m_Layer: \d+',"  m_Component:`n  - component: {fileID: 400000}`n  - component: {fileID: 330000}`n  - component: {fileID: 230000}`n  m_Layer: 0")
    $go=$go.Replace('m_Name: Blood_Mark',"m_Name: $name")
    $transform=[regex]::Replace($transform,'m_LocalRotation: \{[^}]+\}','m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}')
    $transform=[regex]::Replace($transform,'m_LocalPosition: \{[^}]+\}','m_LocalPosition: {x: 0, y: 0, z: 0}')
    $transform=[regex]::Replace($transform,'m_LocalScale: \{[^}]+\}','m_LocalScale: {x: 1, y: 1, z: 1}')
    $transform=$transform.Replace('m_LocalEulerAnglesHint: {x: 90, y: 0, z: 0}','m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}')
    $filter=[regex]::Replace($filter,'m_Mesh: \{[^}]+\}',"m_Mesh: {fileID: 4300000, guid: $meshGuid, type: 2}")
    $renderer=$renderer.Replace('m_CastShadows: 1','m_CastShadows: 0').Replace('m_ReceiveShadows: 1','m_ReceiveShadows: 0').Replace('m_LightProbeUsage: 1','m_LightProbeUsage: 0').Replace('m_ReflectionProbeUsage: 1','m_ReflectionProbeUsage: 0').Replace('m_ScaleInLightmap: 1','m_ScaleInLightmap: 0').Replace('7d55be77aa5c7844780de852f332fe98',$script:materialGuid)
    $prefab="%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n"+$go+$transform+$filter+$renderer
    $map=@{'8653148305672007211'='100100';'3885263544005871302'='400000';'9089920773845016593'='330000';'7325716427944746690'='230000'}
    foreach($id in $map.Keys){$prefab=$prefab.Replace($id,$map[$id])}
    if($prefab -match 'Collider|MonoBehaviour'){throw 'Unexpected gameplay or physics components'}
    $path=Join-Path $script:root "Prefabs/$name.prefab"
    WriteText $path $prefab;Meta $path $prefabGuid PrefabImporter
}
$random=[Random]::new(81372)
$results=@()
NewGeometry
Blob 0 0 .38 .27 1.8 64
Blob -.27 -.06 .13 .10 6.7 36
Blob .25 .10 .16 .11 3.2 36
for($i=0;$i -lt 13;$i++){
    $angle=$random.NextDouble()*2*[Math]::PI;$distance=.43+$random.NextDouble()*.17;$size=.008+$random.NextDouble()*.018
    Blob ([Math]::Cos($angle)*$distance) ([Math]::Sin($angle)*$distance*.8) $size ($size*(.65+$random.NextDouble()*.6)) ($i+4.7) 16
}
$results+=SaveMesh RoadBlood_Pool 'e20dfe61bc9242cb865459155833eb26'
SavePrefab RoadBlood_Pool 'e20dfe61bc9242cb865459155833eb26' '946ce8de4b2e44299496b60d62b760ce'
NewGeometry
Blob 0 0 .36 .055 2.6 64
Blob -.23 .024 .16 .04 7.4 40
for($i=0;$i -lt 8;$i++){$size=.006+$random.NextDouble()*.013;Blob (-.50+$random.NextDouble()) (-.10+$random.NextDouble()*.2) $size ($size*.7) ($i+8.2) 16}
$results+=SaveMesh RoadBlood_Smear 'da9885ab024a49fea0e8626573727e25'
SavePrefab RoadBlood_Smear 'da9885ab024a49fea0e8626573727e25' 'fbda7013206b4878bec03175e7661b08'
NewGeometry
for($i=0;$i -lt 18;$i++){
    $size=.006+$random.NextDouble()*.022;$x=($random.NextDouble()-.5)*.38;$z=-.45+$i*.052+($random.NextDouble()-.5)*.025
    Blob $x $z $size ($size*(.75+$random.NextDouble()*.8)) ($i+10.5) 16
}
$results+=SaveMesh RoadBlood_Drops 'beb64b4d172741d2a8b0b76429696cfa'
SavePrefab RoadBlood_Drops 'beb64b4d172741d2a8b0b76429696cfa' 'ae871596a437436e8ce6f0d6f137752c'
$report=[ordered]@{success=$true;assetRoot=$root;usesImageTextures=$false;runtimeScripts=0;colliders=0;groundOffsetMetres=.003;meshes=$results}
$report|ConvertTo-Json -Depth 5|Set-Content (Join-Path $work 'geometry-validation.json') -Encoding utf8
$report|ConvertTo-Json -Depth 5
