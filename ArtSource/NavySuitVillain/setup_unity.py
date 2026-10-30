"""Write only the new navy-suit NPC's assets and two-part static prefab."""
from pathlib import Path
import json,re,uuid,numpy as np
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent;O=R/'Assets/SecretsThatBreathe/Characters/NavySuitVillain'
def guid(path):
 meta=Path(str(path)+'.meta')
 if meta.exists():return re.search(r'^guid: ([a-f0-9]+)',meta.read_text(),re.M)[1]
 return uuid.uuid5(uuid.NAMESPACE_URL,'SecretsThatBreathe/NavySuitVillain/'+str(path.relative_to(R))).hex
def meta(path,kind='native',fileid=0):
 g=guid(path)
 if kind=='folder':s=f'fileFormatVersion: 2\nguid: {g}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n'
 elif kind=='prefab':s=f'fileFormatVersion: 2\nguid: {g}\nPrefabImporter:\n  externalObjects: {{}}\n'
 else:s=f'fileFormatVersion: 2\nguid: {g}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: {fileid}\n'
 Path(str(path)+'.meta').write_text(s+'  userData: \n  assetBundleName: \n  assetBundleVariant: \n');return g
for folder in [O,O/'Textures',O/'Meshes']:folder.mkdir(exist_ok=True);meta(folder,'folder')
tex=O/'Textures/NavySuitVillain_BaseColor.png';texguid=guid(tex)
texmeta=(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/Textures/ThaiPolice_BaseColor.png.meta').read_text()
texmeta=re.sub(r'^guid: [a-f0-9]+',f'guid: {texguid}',texmeta,flags=re.M);Path(str(tex)+'.meta').write_text(texmeta)
template=(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/ThaiPolice.mat').read_text()
oldtexture=re.search(r'^guid: ([a-f0-9]+)',(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/Textures/ThaiPolice_BaseColor.png.meta').read_text(),re.M)[1]
materials={}
for name in ['NavySuitVillain','SunglassesBlack']:
 path=O/(name+'.mat');g=meta(path,fileid=2100000);text=template.replace('ThaiPolice',name)
 if name=='NavySuitVillain':text=text.replace(oldtexture,texguid)
 else:
  text=re.sub(r'm_Texture: \{[^\n]+\}','m_Texture: {fileID: 0}',text)
  text=re.sub(r'(- _(BaseColor|Color): )\{[^\n]+\}',r'\g<1>{r: 0.008, g: 0.010, b: 0.014, a: 1}',text)
  text=re.sub(r'(_Smoothness: )[^\n]+',r'\g<1>0.68',text)
 path.write_text(text);materials[name]=g
model=O/'NavySuitVillain_LowPoly.fbx';modelguid=guid(model)
fmeta=(R/'Assets/Nemo_Upgrade/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx.meta').read_text()
fmeta=re.sub(r'^guid: [a-f0-9]+',f'guid: {modelguid}',fmeta,flags=re.M)
mapping='  externalObjects:\n'+''.join(f'  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: {name}\n    second: {{fileID: 2100000, guid: {g}, type: 2}}\n' for name,g in materials.items())
fmeta=fmeta.replace('  externalObjects: {}\n',mapping)
for key in ['importAnimation','animationType','importCameras','importLights','importPhysicalCameras']:fmeta=re.sub(rf'(^  +{key}: )[^\n]+',r'\g<1>0',fmeta,flags=re.M)
Path(str(model)+'.meta').write_text(fmeta)
mesh_guids={};report={}
for name,d in json.loads((W/'mesh-data.json').read_text()).items():
 p=np.array(d['positions'])[:,[0,2,1]]*[-1,1,-1];n=np.array(d['normals'])[:,[0,2,1]]*[-1,1,-1];uv=np.array(d['uv']);t=np.array(d['triangles'])[:,[0,2,1]]
 tangent=np.tile([1.,0,0],(len(p),1));tangent-=n*np.sum(n*tangent,axis=1)[:,None];bad=np.linalg.norm(tangent,axis=1)<1e-5;tangent[bad]=[0,0,1];tangent/=np.linalg.norm(tangent,axis=1)[:,None]
 data=np.zeros((len(p),14),dtype='<f4');data[:,:3]=p;data[:,3:6]=n;data[:,6:9]=tangent;data[:,9]=1;data[:,10:12]=uv
 text=(R/'Assets/Model Palm/Porsche V.2/Meshes/Headlight_Left.asset').read_text(encoding='utf-8-sig')
 text=re.sub(r'(?m)^  m_Name: .+$','  m_Name: NavySuitVillain_'+name,text)
 text=re.sub(r'(?m)^(\s*)indexCount: \d+$',rf'\g<1>indexCount: {t.size}',text);text=re.sub(r'(?m)^(\s*)vertexCount: \d+$',rf'\g<1>vertexCount: {len(p)}',text)
 text=re.sub(r'm_VertexCount: \d+',f'm_VertexCount: {len(p)}',text);text=re.sub(r'm_DataSize: \d+',f'm_DataSize: {data.nbytes}',text)
 text=re.sub(r'm_IndexBuffer: [a-f0-9]+','m_IndexBuffer: '+t.astype('<u2').tobytes().hex(),text);text=re.sub(r'_typelessdata: [a-f0-9]+','_typelessdata: '+data.tobytes().hex(),text)
 vec=lambda v:'{x: '+format(v[0],'.9g')+', y: '+format(v[1],'.9g')+', z: '+format(v[2],'.9g')+'}'
 text=re.sub(r'm_Center: \{[^\n]+\}','m_Center: '+vec((p.min(0)+p.max(0))/2),text);text=re.sub(r'm_Extent: \{[^\n]+\}','m_Extent: '+vec((p.max(0)-p.min(0))/2),text)
 path=O/'Meshes'/('NavySuitVillain_'+name+'.asset');path.write_text(text);mesh_guids[name]=meta(path,fileid=4300000)
 assert len(data.tobytes())==len(p)*56
 report[name]={'render_vertices':len(p),'triangles':len(t),'mesh_guid':mesh_guids[name]}
common='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
def go(id,name,components):
 return f'--- !u!1 &{id}\nGameObject:\n'+common+'  serializedVersion: 6\n  m_Component:\n'+''.join(f'  - component: {{fileID: {c}}}\n' for c in components)+f'  m_Layer: 0\n  m_Name: {name}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n'
def tr(id,go,parent,children):
 return f'--- !u!4 &{id}\nTransform:\n'+common+f'  m_GameObject: {{fileID: {go}}}\n  serializedVersion: 2\n  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}\n  m_LocalPosition: {{x: 0, y: 0, z: 0}}\n  m_LocalScale: {{x: 1, y: 1, z: 1}}\n  m_ConstrainProportionsScale: 0\n'+('  m_Children:\n'+''.join(f'  - {{fileID: {c}}}\n' for c in children) if children else '  m_Children: []\n')+f'  m_Father: {{fileID: {parent}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}\n'
prefab='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+go(1000,'NavySuitVillain',[1001,1002])+tr(1001,1000,0,[2001,3001])
prefab+='--- !u!136 &1002\nCapsuleCollider:\n'+common+'''  m_GameObject: {fileID: 1000}
  m_Material: {fileID: 0}
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
  serializedVersion: 2
  m_Radius: 0.22
  m_Height: 1.85
  m_Direction: 1
  m_Center: {x: 0, y: 0.925, z: 0}
'''
renderer_template=re.search(r'--- !u!23 &\d+\nMeshRenderer:\n.*?(?=--- !u!|\Z)',(R/'Assets/MainScenes/Prefeb/Villain.prefab').read_text(),re.S)[0]
for name,base in [('Body',2000),('Sunglasses',3000)]:
 prefab+=go(base,name,[base+1,base+2,base+3])+tr(base+1,base,1001,[])
 prefab+=f'--- !u!33 &{base+2}\nMeshFilter:\n'+common+f'  m_GameObject: {{fileID: {base}}}\n  m_Mesh: {{fileID: 4300000, guid: {mesh_guids[name]}, type: 2}}\n'
 renderer=re.sub(r'--- !u!23 &\d+',f'--- !u!23 &{base+3}',renderer_template);renderer=re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {{fileID: {base}}}',renderer)
 matguid=materials['NavySuitVillain' if name=='Body' else 'SunglassesBlack']
 renderer=re.sub(r'(  m_Materials:\n)(?:  - \{[^\n]+\}\n)+',rf'\g<1>  - {{fileID: 2100000, guid: {matguid}, type: 2}}\n',renderer)
 prefab+=renderer
path=O/'NavySuitVillain.prefab';path.write_text(prefab);prefabguid=meta(path,'prefab')
(W/'unity-assets.json').write_text(json.dumps({'prefab_guid':prefabguid,'model_guid':modelguid,'materials':materials,'meshes':report,'collider':'capsule 1.85 m','static_standing_pose':True},indent=2))
print('NAVY_SUIT_PREFAB_READY')
