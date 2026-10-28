"""Isolate Isuzu's red lightbar banks; leave all original car data untouched."""
from pathlib import Path
import json,re,uuid,shutil,hashlib
import numpy as np
from PIL import Image
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent;A=R/'Assets/Model Palm/Police Car1';P=A/'isuzu.prefab'
backup=W/'Backups';backup.mkdir(exist_ok=True)
for f in [P,A/'isuzu.fbx',Path(str(P)+'.meta'),A/'Material/tripo_mat_0f8da5ad.mat']:
 dest=backup/(f.name+'.before')
 if not dest.exists():shutil.copy2(f,dest)
def guid(path):
 meta=Path(str(path)+'.meta')
 if meta.exists():return re.search(r'^guid: ([a-f0-9]+)',meta.read_text(),re.M)[1]
 return uuid.uuid5(uuid.NAMESPACE_URL,'IsuzuSiren/'+str(path.relative_to(R))).hex
def native_meta(path,fileid):
 g=guid(path);Path(str(path)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {g}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: {fileid}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');return g
folder=A/'Meshes';folder.mkdir(exist_ok=True)
if not Path(str(folder)+'.meta').exists():Path(str(folder)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(folder)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
script=R/'Assets/SecretsThatBreathe/Scripts/PoliceSirenLights.cs';scriptguid=guid(script)
if not Path(str(script)+'.meta').exists():Path(str(script)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {scriptguid}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
atlas=A/'Texture/tripo_rgb_b9574266-7dd8-4242-9a7c-6f0907865bc9.jpg'
rgb=np.asarray(Image.open(atlas).convert('RGB').resize((1024,1024),Image.Resampling.LANCZOS),dtype=float)/255
red,green,blue=rgb.transpose(2,0,1)
mask=np.clip((red-np.maximum(green,blue)*1.4-.025)/.12,0,1)*np.clip((red-.12)/.15,0,1)
value=mask*(.30+.70*np.clip(red/.65,0,1))
pixels=np.repeat((value[:,:,None]*255).astype(np.uint8),3,axis=2)
emitmap=A/'Texture/Isuzu_Siren_EmissionMask.png';Image.fromarray(pixels).save(emitmap);emitguid=guid(emitmap)
texmeta=(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/Textures/ThaiPolice_BaseColor.png.meta').read_text()
texmeta=re.sub(r'^guid: [a-f0-9]+',f'guid: {emitguid}',texmeta,flags=re.M);texmeta=texmeta.replace('maxTextureSize: 512','maxTextureSize: 1024');Path(str(emitmap)+'.meta').write_text(texmeta)
materialguids=[]
for bank in ['Left','Right']:
 name='Isuzu_Siren_'+bank;path=A/'Material'/(name+'.mat')
 text=(A/'Material/tripo_mat_0f8da5ad.mat').read_text().replace('m_Name: tripo_mat_0f8da5ad','m_Name: '+name)
 text=text.replace('m_ValidKeywords: []','m_ValidKeywords:\n  - _EMISSION').replace('m_LightmapFlags: 4','m_LightmapFlags: 2')
 text=re.sub(r'(- _EmissionMap:\s+m_Texture: )\{[^\n]+\}',rf'\g<1>{{fileID: 2800000, guid: {emitguid}, type: 3}}',text)
 text=re.sub(r'(- _EmissionColor: )\{[^\n]+\}',r'\g<1>{r: 7, g: 0.025, b: 0.008, a: 1}',text)
 text=re.sub(r'(_Smoothness: )[^\n]+',r'\g<1>0.4',text);path.write_text(text);materialguids.append(native_meta(path,2100000))
# Use Blender's actual FBX triangulation/corner normals as the baseline.
d=json.loads((W/'bar-mesh-source.json').read_text());positions=np.array(d['positions']);normals=np.array(d['normals']);uv=np.array(d['uv']);tri=np.array(d['triangles'])
# Same local coordinates Unity's FBX importer uses; the car children are identity.
p=positions[:,[0,2,1]]*[-1,1,-1];n=normals[:,[0,2,1]]*[-1,1,-1];center=(p[:,0].min()+p[:,0].max())/2
v=[];ns=[];uvs=[];banks=[[],[]];parents=[[],[]];lookup={}
def add_vertex(a):
 point=a[:3];normal=a[3:6];normal=normal/max(np.linalg.norm(normal),1e-10);tex=a[6:]
 key=tuple(np.round(np.r_[point,normal,tex],7))
 if key not in lookup:lookup[key]=len(v);v.append(point);ns.append(normal);uvs.append(tex)
 return lookup[key]
def clip(poly,side):
 out=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  ain=(a[0]-center)*side>=-1e-9;bin=(b[0]-center)*side>=-1e-9
  if ain:out.append(a)
  if ain!=bin:out.append(a+(b-a)*((center-a[0])/(b[0]-a[0])))
 return out
for source_index,t in enumerate(tri):
 poly=[np.r_[p[i],n[i],uv[i]] for i in t]
 for bank,side in enumerate([-1,1]):
  q=clip(poly,side)
  for j in range(1,len(q)-1):
   a,b,c=q[0],q[j],q[j+1]
   if np.linalg.norm(np.cross(b[:3]-a[:3],c[:3]-a[:3]))<1e-11:continue
   banks[bank].append([add_vertex(a),add_vertex(c),add_vertex(b)])
   parents[bank].append(source_index)
v=np.array(v);ns=np.array(ns);uvs=np.array(uvs);t=np.concatenate([np.array(x) for x in banks])
data=np.zeros((len(v),14),dtype='<f4');data[:,:3]=v;data[:,3:6]=ns;data[:,10:12]=uvs
tangent=np.tile([1.,0,0],(len(v),1));tangent-=ns*np.sum(ns*tangent,axis=1)[:,None]
bad=np.linalg.norm(tangent,axis=1)<1e-5;tangent[bad]=[0,0,1];tangent/=np.linalg.norm(tangent,axis=1)[:,None];data[:,6:9]=tangent;data[:,9]=1
mesh=A/'Meshes/Isuzu_Siren_Banks.asset';meshguid=native_meta(mesh,4300000)
text=(R/'Assets/Model Palm/Porsche V.2/Meshes/Headlight_Left.asset').read_text(encoding='utf-8-sig')
text=re.sub(r'(?m)^  m_Name: .+$','  m_Name: Isuzu_Siren_Banks',text)
vec=lambda z:'{x: '+format(z[0],'.9g')+', y: '+format(z[1],'.9g')+', z: '+format(z[2],'.9g')+'}'
sub='  m_SubMeshes:\n';offset=0
for bank in banks:
 ix=np.array(bank);points=v[ix.ravel()];mid=(points.min(0)+points.max(0))/2;ext=(points.max(0)-points.min(0))/2
 sub+=f'''  - serializedVersion: 2
    firstByte: {offset}
    indexCount: {ix.size}
    topology: 0
    baseVertex: 0
    firstVertex: 0
    vertexCount: {len(v)}
    localAABB:
      m_Center: {vec(mid)}
      m_Extent: {vec(ext)}
''';offset+=ix.size*2
text=re.sub(r'  m_SubMeshes:\n.*?(?=  m_Shapes:)',sub,text,flags=re.S)
text=re.sub(r'm_IndexBuffer: [a-f0-9]+','m_IndexBuffer: '+t.astype('<u2').tobytes().hex(),text)
text=re.sub(r'm_VertexCount: \d+',f'm_VertexCount: {len(v)}',text);text=re.sub(r'm_DataSize: \d+',f'm_DataSize: {data.nbytes}',text)
text=re.sub(r'_typelessdata: [a-f0-9]+','_typelessdata: '+data.tobytes().hex(),text)
mid=(v.min(0)+v.max(0))/2;ext=(v.max(0)-v.min(0))/2
# Only the global AABB; submesh AABBs were already calculated independently.
text=re.sub(r'(  m_LocalAABB:\n    m_Center: )\{[^\n]+\}(\n    m_Extent: )\{[^\n]+\}',lambda m:m[1]+vec(mid)+m[2]+vec(ext),text)
mesh.write_text(text)
# Existing blocks remain byte-for-byte except the lightbar bindings and root's new child/component.
prefab=P.read_text();original=prefab
def replaceblock(fid,func):
 global prefab
 match=re.search(r'--- !u!\d+ &'+str(fid)+r'\n.*?(?=--- !u!|\Z)',prefab,re.S)
 assert match,fid
 prefab=prefab[:match.start()]+func(match[0])+prefab[match.end():]
replaceblock(5978638142169209096,lambda s:re.sub(r'm_Mesh: \{[^\n]+\}',f'm_Mesh: {{fileID: 4300000, guid: {meshguid}, type: 2}}',s))
replaceblock(7112178629771985715,lambda s:re.sub(r'(  m_Materials:\n)(?:  - \{[^\n]+\}\n)+',lambda m:m[1]+''.join(f'  - {{fileID: 2100000, guid: {g}, type: 2}}\n' for g in materialguids),s))
def fid(name):return int.from_bytes(hashlib.sha256(('IsuzuSiren/'+name).encode()).digest()[:8],'little')&0x7fffffffffffffff
rootgo=708723478682126092;roottr=6186545251062735584;controller=fid('controller')
owned=[controller]+[fid(bank+suffix) for bank in ['Left','Right'] for suffix in ['go','transform','light']]
for id in owned:prefab=re.sub(r'--- !u!\d+ &'+str(id)+r'\n.*?(?=--- !u!|\Z)','',prefab,flags=re.S)
replaceblock(rootgo,lambda s:s.replace(f'  - component: {{fileID: {controller}}}\n','').replace('  m_Layer:',f'  - component: {{fileID: {controller}}}\n  m_Layer:',1))
def root_children(s):
 for bank in ['Left','Right']:s=s.replace(f'  - {{fileID: {fid(bank+"transform")}}}\n','')
 return s.replace('  m_Father:', ''.join(f'  - {{fileID: {fid(bank+"transform")}}}\n' for bank in ['Left','Right'])+'  m_Father:',1)
replaceblock(roottr,root_children)
common='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
lighttemplate=(R/'Assets/Nemo_Upgrade/Starter Assets/Sample/ThirdPersonController/Playground.unity').read_text()
lighttemplate=re.search(r'--- !u!108 &\d+\nLight:\n.*?(?=--- !u!|\Z)',lighttemplate,re.S)[0]
locations=[]
for bank,sign in [('Left',-1),('Right',1)]:
 go,tr,light=[fid(bank+s) for s in ['go','transform','light']]
 pos=[center+sign*.31,float(v[:,1].max())+.035,float((v[:,2].min()+v[:,2].max())/2)];locations.append(pos)
 rotation='{x: 0.156434465, y: 0, z: 0, w: 0.987688341}' if sign<0 else '{x: 0, y: 0.987688341, z: -0.156434465, w: 0}'
 prefab+=f'--- !u!1 &{go}\nGameObject:\n'+common+f'''  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr}}}
  - component: {{fileID: {light}}}
  m_Layer: 0
  m_Name: Siren_Red_{bank}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tr}
Transform:
'''+common+f'''  m_GameObject: {{fileID: {go}}}
  serializedVersion: 2
  m_LocalRotation: {rotation}
  m_LocalPosition: {vec(pos)}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {roottr}}}
  m_LocalEulerAnglesHint: {{x: 18, y: {0 if sign<0 else 180}, z: 0}}
'''
 l=re.sub(r'--- !u!108 &\d+',f'--- !u!108 &{light}',lighttemplate);l=re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {{fileID: {go}}}',l)
 for key,val in [('m_Type','0'),('m_Intensity','32'),('m_Range','11'),('m_SpotAngle','100'),('m_InnerSpotAngle','55'),('m_BounceIntensity','0'),('m_ColorTemperature','6500')]:l=re.sub(rf'(?m)^  {key}: [^\n]+',f'  {key}: {val}',l)
 l=re.sub(r'(?m)^  m_Color: \{[^\n]+\}','  m_Color: {r: 1, g: 0.015, b: 0.005, a: 1}',l)
 l=l.replace('m_Bias: 0.125','m_Bias: 0.025').replace('m_NormalBias: 0','m_NormalBias: 0.08').replace('m_NearPlane: 0.2','m_NearPlane: 0.03')
 prefab+=l
prefab+=f'--- !u!114 &{controller}\nMonoBehaviour:\n'+common+f'''  m_GameObject: {{fileID: {rootgo}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {scriptguid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::SecretsThatBreathe.PoliceSirenLights
  sirenOn: 1
  pattern: 0
  cyclesPerSecond: 1.6
  emissionColor: {{r: 7, g: 0.025, b: 0.008, a: 1}}
  dimEmission: 0.025
  lensRenderer: {{fileID: 7112178629771985715}}
  leftMaterialIndex: 0
  rightMaterialIndex: 1
  leftLight: {{fileID: {fid('Leftlight')}}}
  rightLight: {{fileID: {fid('Rightlight')}}}
  lightIntensity: 32
  lightRange: 11
  spotAngle: 100
  downwardTilt: 18
  lightColor: {{r: 1, g: 0.015, b: 0.005, a: 1}}
'''
# A rerun owns only these known blocks/bindings. All original other blocks stay intact.
before=(backup/'isuzu.prefab.before').read_text()
allowed={rootgo,roottr,5978638142169209096,7112178629771985715}
def blocks(s):return {int(m[1]):m[0] for m in re.finditer(r'--- !u!\d+ &(\d+)\n.*?(?=--- !u!|\Z)',s,re.S)}
bb,aa=blocks(before),blocks(prefab)
for id,s in bb.items():
 if id not in allowed:assert aa[id]==s,('Unexpected original block modification',id)
assert all(id in aa for id in bb)
assert len(aa)-len(bb)==7
assert hashlib.sha256((A/'isuzu.fbx').read_bytes()).digest()==hashlib.sha256((backup/'isuzu.fbx.before').read_bytes()).digest()
P.write_text(prefab)
np.savez_compressed(W/'siren-mesh.npz',positions=v,normals=ns,uv=uvs,triangles=t,bank_counts=[len(x) for x in banks],source_triangles=np.concatenate(parents))
(W/'build-report.json').write_text(json.dumps({'original_siren_triangles':len(tri),'new_siren_triangles':len(t),'banks_triangles':[len(x) for x in banks],'vertices':len(v),'mask_resolution':1024,'source_fbx_unchanged':True,'original_prefab_blocks_preserved_except_lightbar_and_root_additions':True,'new_objects':2,'real_lights':2,'light_locations_unity':locations,'materials':materialguids,'script_guid':scriptguid,'prefab_guid_preserved':True,'pattern':'RotatingBeacon, 1.6 Hz; AlternatingDoubleFlash optional'},indent=2))
print('ISUZU_SIREN_BUILT',len(tri),'->',len(t),'lightbar triangles')
