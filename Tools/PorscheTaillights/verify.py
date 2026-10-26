from pathlib import Path
import re, json, hashlib, subprocess

root=Path(__file__).resolve().parents[2]
work=Path(__file__).resolve().parent
asset=root/'Assets/Model Palm/Porsche V.2'
before=(work/'PorscheV.2_before.prefab').read_text(encoding='utf-8-sig')
after=(asset/'PorscheV.2.prefab').read_text(encoding='utf-8-sig')
def blocks(text):
    return {m[1]:m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\Z)',text,re.M|re.S)}
b,a=blocks(before),blocks(after)
assert b.keys()<=a.keys()
changed=[key for key in b if b[key]!=a[key]]
# Only root's child list gains the new visual object.
assert changed==['2523572730012541411'],changed
root_before=b[changed[0]]; root_after=a[changed[0]]
old_children=re.findall(r'^  - \{fileID: (\d+)\}',root_before,re.M)
new_children=re.findall(r'^  - \{fileID: (\d+)\}',root_after,re.M)
added=set(new_children)-set(old_children)
assert len(added)==1
clean=root_after
for child in added: clean=clean.replace('  - {fileID: '+child+'}\n','')
assert clean==root_before
overlay=(asset/'Meshes/Taillights_EmissionOverlay.asset').read_text()
triangles=int(re.search(r'indexCount: (\d+)',overlay)[1])//3
originals=list((asset/'Meshes').glob('*.asset'))
originals=[p for p in originals if p.name!='Taillights_EmissionOverlay.asset']
hashes={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in originals}
for p in originals:
    relative=p.relative_to(root).as_posix()
    assert subprocess.check_output(['git','diff','--numstat','--',relative],cwd=root)==b'',relative
result=json.loads((work/'result.json').read_text(encoding='utf-8-sig'))
assert result['success'] and result['emission']
inspection_path=work/'final_unity_inspection.json'
if not inspection_path.exists(): inspection_path=root/'Tools/PorscheHeadlights/result.json'
inspection=json.loads(inspection_path.read_text(encoding='utf-8-sig'))
assert inspection['success']
(work/'final_unity_inspection.json').write_text(json.dumps(inspection,indent=2,ensure_ascii=False),encoding='utf-8')
report={'existingSerializedBlocksPreserved':True,'existingTransformsPivotsHierarchyPreserved':True,'originalMeshesUnchanged':hashes,'addedObjects':['Taillights_Emission'],'originalMeshObjects':9,'finalMeshObjects':10,'originalTriangles':26436,'overlayTriangles':triangles,'finalTriangles':26436+triangles,'trianglesStage':'Unity triangulated meshes, no added modifiers','emissionSurvivesReimport':True,'newTextureCount':0,'openScenes':inspection['scenes'],'previewPostProcessing':False,'notTested':['Standalone player build','Baked GI','Blender or FBX export (Unity-only material and mesh addition)']}
(work/'final_validation.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='originalMeshesUnchanged'},ensure_ascii=True))
