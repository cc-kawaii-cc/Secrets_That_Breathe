"""Make a self-contained Unity prefab, one URP material and native mesh."""
from pathlib import Path
import json,re,uuid,numpy as np
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent;O=R/'Assets/SecretsThatBreathe/Props/ThaiTrafficCone'
def guid(path):
 p=Path(str(path)+'.meta')
 if p.exists():return re.search(r'^guid: ([0-9a-f]+)',p.read_text(),re.M)[1]
 return uuid.uuid5(uuid.NAMESPACE_URL,'SecretsThatBreathe/Cone/'+str(path.relative_to(R))).hex
def meta(path,kind='native',fileid=0):
 g=guid(path);p=Path(str(path)+'.meta')
 if kind=='folder':s=f'fileFormatVersion: 2\nguid: {g}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n'
 elif kind=='prefab':s=f'fileFormatVersion: 2\nguid: {g}\nPrefabImporter:\n  externalObjects: {{}}\n'
 else:s=f'fileFormatVersion: 2\nguid: {g}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: {fileid}\n'
 p.write_text(s+'  userData: \n  assetBundleName: \n  assetBundleVariant: \n');return g
for path in [O,O/'Textures',O/'Meshes']:
 path.mkdir(exist_ok=True);meta(path,'folder')
tex=O/'Textures/ThaiTrafficCone_Worn_BaseColor.png';texguid=guid(tex)
text=(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/Textures/ThaiPolice_BaseColor.png.meta').read_text()
text=re.sub(r'^guid: [a-f0-9]+',f'guid: {texguid}',text,flags=re.M);Path(str(tex)+'.meta').write_text(text)
material=O/'ThaiTrafficCone_Worn.mat';matguid=meta(material,fileid=2100000)
oldtex=re.search(r'^guid: ([a-f0-9]+)',(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/Textures/ThaiPolice_BaseColor.png.meta').read_text(),re.M)[1]
text=(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/ThaiPolice.mat').read_text().replace('ThaiPolice','ThaiTrafficCone_Worn').replace(oldtex,texguid)
text=re.sub(r'(_Smoothness: )[^\n]+',r'\g<1>0.12',text);material.write_text(text)
# Maintain the same authored object/material names for the editable FBX alternative.
model=O/'ThaiTrafficCone_Worn.fbx';modelguid=guid(model)
text=(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/ThaiPolice_LowPoly.fbx.meta').read_text()
text=re.sub(r'^guid: [a-f0-9]+',f'guid: {modelguid}',text,flags=re.M)
oldmat=re.search(r'^guid: ([a-f0-9]+)',(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/ThaiPolice.mat.meta').read_text(),re.M)[1]
text=text.replace('name: ThaiPolice','name: ThaiTrafficCone_Worn').replace(oldmat,matguid);Path(str(model)+'.meta').write_text(text)
d=json.loads((W/'mesh-data.json').read_text());p=np.array(d['positions']);n=np.array(d['normals']);uv=np.array(d['uv']);t=np.array(d['triangles'])
# Blender Z-up to Unity Y-up, with the FBX importer's handedness reflection.
p=p[:,[0,2,1]]*[-1,1,-1];n=n[:,[0,2,1]]*[-1,1,-1];t=t[:,[0,2,1]]
tangent=np.tile([1.,0,0],(len(p),1));tangent-=n*np.sum(tangent*n,axis=1)[:,None]
bad=np.linalg.norm(tangent,axis=1)<1e-5
tangent[bad]=[0,0,1];tangent/=np.linalg.norm(tangent,axis=1)[:,None]
data=np.zeros((len(p),14),dtype='<f4');data[:,:3]=p;data[:,3:6]=n;data[:,6:9]=tangent;data[:,9]=1;data[:,10:12]=uv
text=(R/'Assets/Model Palm/Porsche V.2/Meshes/Headlight_Left.asset').read_text(encoding='utf-8-sig')
text=re.sub(r'(?m)^  m_Name: .+$','  m_Name: ThaiTrafficCone_Worn',text)
text=re.sub(r'(?m)^(\s*)indexCount: \d+$',rf'\g<1>indexCount: {t.size}',text)
text=re.sub(r'(?m)^(\s*)vertexCount: \d+$',rf'\g<1>vertexCount: {len(p)}',text)
text=re.sub(r'm_VertexCount: \d+',f'm_VertexCount: {len(p)}',text)
text=re.sub(r'm_IndexBuffer: [a-f0-9]+','m_IndexBuffer: '+t.astype('<u2').tobytes().hex(),text)
text=re.sub(r'm_DataSize: \d+',f'm_DataSize: {data.nbytes}',text)
text=re.sub(r'_typelessdata: [a-f0-9]+','_typelessdata: '+data.tobytes().hex(),text)
vec=lambda v:'{x: '+format(v[0],'.9g')+', y: '+format(v[1],'.9g')+', z: '+format(v[2],'.9g')+'}'
text=re.sub(r'm_Center: \{[^\n]+\}','m_Center: '+vec((p.min(0)+p.max(0))/2),text)
text=re.sub(r'm_Extent: \{[^\n]+\}','m_Extent: '+vec((p.max(0)-p.min(0))/2),text)
mesh=O/'Meshes/ThaiTrafficCone_Worn.asset';mesh.write_text(text);meshguid=meta(mesh,fileid=4300000)
prefix='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
prefab='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!1 &1000\nGameObject:\n'+prefix+'''  serializedVersion: 6
  m_Component:
  - component: {fileID: 1001}
  - component: {fileID: 1002}
  - component: {fileID: 1003}
  - component: {fileID: 1004}
  m_Layer: 0
  m_Name: ThaiTrafficCone_Worn
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &1001
Transform:
'''+prefix+'''  m_GameObject: {fileID: 1000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!33 &1002
MeshFilter:
'''+prefix+f'  m_GameObject: {{fileID: 1000}}\n  m_Mesh: {{fileID: 4300000, guid: {meshguid}, type: 2}}\n'
# Use the current Unity version's existing MeshRenderer serialization schema.
template=(R/'Assets/Model Palm/Porsche V.2/PorscheV.2.prefab').read_text()
renderer=re.search(r'--- !u!23 &\d+\nMeshRenderer:\n.*?(?=--- !u!|\Z)',template,re.S)[0]
renderer=re.sub(r'--- !u!23 &\d+','--- !u!23 &1003',renderer)
renderer=re.sub(r'm_GameObject: \{fileID: \d+\}','m_GameObject: {fileID: 1000}',renderer)
renderer=re.sub(r'(  m_Materials:\n)(?:  - \{[^\n]+\}\n)+',rf'\g<1>  - {{fileID: 2100000, guid: {matguid}, type: 2}}\n',renderer)
prefab+=renderer
prefab+='--- !u!64 &1004\nMeshCollider:\n'+prefix+f'''  m_GameObject: {{fileID: 1000}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 5
  m_Convex: 1
  m_CookingOptions: 30
  m_Mesh: {{fileID: 4300000, guid: {meshguid}, type: 2}}
'''
prefabpath=O/'ThaiTrafficCone_Worn.prefab';prefabpath.write_text(prefab);prefabguid=meta(prefabpath,'prefab')
# Round-trip the native buffers, not merely their reported counts.
native=mesh.read_text();actual=np.frombuffer(bytes.fromhex(re.search(r'_typelessdata: ([a-f0-9]+)',native)[1]),dtype='<f4').reshape(-1,14)
assert np.max(np.abs(actual[:,:3]-p))<1e-7 and np.max(np.abs(actual[:,3:6]-n))<1e-6
actualt=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([a-f0-9]+)',native)[1]),dtype='<u2').reshape(-1,3)
assert np.array_equal(actualt,t)
assert np.allclose(np.linalg.norm(actual[:,3:6],axis=1),1,atol=1e-5)
assert np.allclose(p.max(0)-p.min(0),[.36,.70,.36],atol=1e-6)
assert len(re.findall(r'^--- !u!1 ',prefab,re.M))==1
(W/'unity-assets.json').write_text(json.dumps({'prefab_guid':prefabguid,'mesh_guid':meshguid,'material_guid':matguid,'texture_guid':texguid,'model_guid':modelguid,'triangles':len(t),'native_mesh_matches_authored_data':True,'native_dimensions_metres':[.36,.7,.36],'collider':'convex MeshCollider; no Rigidbody','live_unity_import_checked':False},indent=2))
print('CONE_PREFAB_READY',prefabpath.relative_to(R))
