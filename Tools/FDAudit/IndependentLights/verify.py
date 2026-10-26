from pathlib import Path
import re,json,hashlib
import numpy as np

root=Path(__file__).resolve().parents[3];work=Path(__file__).resolve().parent;asset=root/'Assets/Model Palm/FD V.2'
report=json.loads((work/'build_report.json').read_text())
text=(asset/'FD.prefab').read_text(encoding='utf-8-sig')
baseline=(root/'Tools/FDAudit/Lights/FD.prefab.before').read_text(encoding='utf-8-sig')
def blocks(t):return {m[1]:m[0] for m in re.finditer(r'^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\Z)',t,re.S|re.M)}
b=blocks(baseline);a=blocks(text);assert b.keys()<=a.keys()
changed=[key for key in b if b[key]!=a[key]];assert changed==['7600716200271561881'],changed
rootblock=a[changed[0]]
for transform in re.findall(r'--- !u!4 &(\d+)\n',text):
    if transform not in b:rootblock=rootblock.replace('  - {fileID: '+transform+'}\n','')
assert rootblock==b[changed[0]]
assert len(a)-len(b)==16
assert (asset/'FD.fbx').read_bytes()==(work/'FD.fbx.before').read_bytes()
assert (asset/'FD.fbx.meta').read_bytes()==(work/'FD.fbx.meta.before').read_bytes()
checks=[]
for lamp in report['lamps']:
    path=root/lamp['meshPath'];mesh=path.read_text();n=int(re.search(r'm_VertexCount: (\d+)',mesh)[1])
    data=np.frombuffer(bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',mesh)[1]),dtype='<f4').reshape(n,14)
    indices=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)',mesh)[1]),dtype='<u2')
    assert indices.max()<n and len(indices)==lamp['triangles']*3
    assert np.isfinite(data).all()
    assert np.allclose(np.linalg.norm(data[:,3:6],axis=1),1,atol=1e-5)
    assert np.allclose(np.sum(data[:,3:6]*data[:,6:9],axis=1),0,atol=1e-5)
    src=np.load(work/(lamp['name']+'.npz'))
    assert np.allclose(data[:,:3],src['positions']*[-1,1,1])
    assert lamp['closedManifold'] and lamp['volume']>0
    assert lamp['meshGUID'] in text
    checks.append({'name':lamp['name'],'vertices':n,'triangles':len(indices)//3,'closedManifold':True,'minimumFaceArea':lamp['minTriangleArea'],'surfaceOffset':lamp['surfaceFitMaxOffset']})
for kind,guid in report['materials'].items():
    path=asset/f'Material/FD_{kind}_Independent_Emission.mat';mat=path.read_text()
    assert not re.search(r'm_Texture: \{fileID: (?!0\})',mat),'No textures may be bound'
    assert '  - _EMISSION' in mat and 'm_LightmapFlags: 2' in mat
    assert text.count(guid)==2
result={'allOriginalObjectComponentBlocksPreserved':True,'onlyOriginalRootChildListChanged':True,'originalSourceAndImporterUnchanged':True,'textureReferencesOnNewMaterials':0,'newMaterialCount':2,'newEmitterObjectCount':4,'newTriangles':report['newTriangles'],'finalTriangles':report['finalTriangles'],'finalMeshObjects':report['finalMeshObjects'],'triangleStage':'Unity native meshes; no additional modifiers','geometryChecks':checks,'UV0':'Generated planar lamp coordinates; no texture dependency','UV1':'No lightmap unwrap; dynamic lamps','nativeMeshFormat':'Copied from valid Unity 6000.3 mesh serialization layout','verification':'Geometry topology/normal/tangent checks, prefab/file invariants and CPU rendering of actual generated meshes','notVerified':['Live Unity import and Bloom','Standalone build','Blender/FBX export','Brake or turn-signal animation']}
(work/'final_validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='geometryChecks'}))
