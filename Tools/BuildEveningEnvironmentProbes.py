"""Extend probe coverage to the crime scene and foliage without changing static flags."""
from pathlib import Path
import re, json, datetime
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
PATH=ROOT/'Assets/MainScenes/From_PM/MainScene_2_Evening.unity'
s=PATH.read_text(encoding='utf-8')
work=ROOT/'Tools/EveningLighting';work.mkdir(exist_ok=True)
backup=work/('before-environment-probes-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S')+'.unity.txt')
backup.write_text(s,encoding='utf-8')
blocks={m[2]:(int(m[1]),m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^---|\Z)',s,re.M|re.S)}
def vec(text,key,default):
    m=re.search(re.escape(key)+r': \{x: ([^,]+), y: ([^,]+), z: ([^}]+)',text)
    return np.array([float(v) for v in m.groups()]) if m else np.array(default,dtype=float)
def matrix(p,q,scale):
    x,y,z,w=q
    r=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
                [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
                [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
    m=np.eye(4);m[:3,:3]=r@np.diag(scale);m[:3,3]=p;return m
def quaternion(b):
    m=re.search(r'm_LocalRotation: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)',b)
    return [float(v) for v in m.groups()] if m else [0,0,0,1]
cache={'0':np.eye(4)}
def world(id):
    if id in cache:return cache[id]
    kind,b=blocks[id]
    if ' stripped\n' in b:
        instance=re.search(r'm_PrefabInstance: \{fileID: (\d+)',b)[1]
        target=re.search(r'm_CorrespondingSourceObject: \{fileID: (-?\d+)',b)[1]
        result=instance_matrix(blocks[instance][1],target)
    else:
        parent=re.search(r'm_Father: \{fileID: (\d+)',b)[1]
        result=world(parent)@matrix(vec(b,'m_LocalPosition',[0,0,0]),quaternion(b),vec(b,'m_LocalScale',[1,1,1]))
    cache[id]=result;return result
def overrides(b,target,key,default):
    result=list(default)
    for axis in range(len(default)):
        name='xyzw'[axis]
        m=re.search(r'target: \{fileID: '+target+r',[^\n]+\n      propertyPath: '+key+r'\.'+name+r'\n      value: ([^\n]+)',b)
        if m:result[axis]=float(m[1])
    return result
def instance_matrix(b,target):
    parent=re.search(r'm_TransformParent: \{fileID: (\d+)',b)[1]
    return world(parent)@matrix(overrides(b,target,'m_LocalPosition',[0,0,0]),
        overrides(b,target,'m_LocalRotation',[0,0,0,1]),overrides(b,target,'m_LocalScale',[1,1,1]))
targets=[]
for id,(kind,b) in blocks.items():
    if kind!=1001:continue
    name=re.search(r'propertyPath: m_Name\n      value: ([^\n]+)',b)
    if not name:continue
    name=name[1]
    if not any(k in name for k in ['_Bush','MangoTree','ThaiPolice_IdleTalk','ThaiTrafficCone_Worn']) and name!='isuzu':continue
    target=re.search(r'target: \{fileID: (-?\d+),[^\n]+\n      propertyPath: m_LocalPosition\.x',b)
    assert target,(id,name)
    m=instance_matrix(b,target[1]);targets.append((name,m[:3,3],m))
crime=[t for t in targets if t[0].startswith(('ThaiPolice','ThaiTrafficCone')) or t[0]=='isuzu']
plants=[t for t in targets if '_Bush' in t[0] or 'MangoTree' in t[0]]
assert len(crime)>=4 and len(plants)>0
def add(probes,p):probes[tuple(round(float(v),3) for v in p)]=None
crime_probes={}
# A rectangular volume keeps cones, the moving skeleton and the approach inside
# the probe hull, rather than extrapolating from the vehicle's last waypoint.
positions=np.array([p for _,p,_ in crime]);lo=positions.min(axis=0);hi=positions.max(axis=0)
base=float(np.median(positions[:,1]))
for x in np.linspace(lo[0]-3,hi[0]+3,max(3,int(np.ceil((hi[0]-lo[0]+6)/3))+1)):
    for z in np.linspace(lo[2]-3,hi[2]+3,max(3,int(np.ceil((hi[2]-lo[2]+6)/3))+1)):
        for h in (.3,1.3,2.8):add(crime_probes,[x,base+h,z])
plant_probes={}
for name,p,m in plants:
    tree='MangoTree' in name
    radius=2.5 if tree else 1.0
    heights=(.4,2.5,5,8) if tree else (.25,1.25,2.5)
    # Sample around, rather than inside, the trunk. Heights are world metres.
    for dx,dz in [(-radius,0),(radius,0),(0,-radius),(0,radius)]:
        for h in heights:add(plant_probes,p+[dx,h,dz])
def group(name,first,probes):
    global s
    ids=(first,first+1,first+2)
    owned='m_Name: '+name in s
    for id in ids:
        old=re.search(r'^--- !u!\d+ &'+str(id)+r'\n.*?(?=^---|\Z)',s,re.M|re.S)
        if old:
            assert owned
            s=s[:old.start()]+s[old.end():]
    header=f'''--- !u!1 &{ids[0]}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {ids[1]}}}
  - component: {{fileID: {ids[2]}}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{ids[1]}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ids[0]}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!220 &{ids[2]}
LightProbeGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ids[0]}}}
  m_Enabled: 1
  m_SourcePositions:
'''
    body=''.join('  - {x: %.3f, y: %.3f, z: %.3f}\n'%p for p in probes)
    marker='--- !u!1660057539 &9223372036854775807'
    s=s.replace(marker,header+body+'  m_Dering: 1\n'+marker)
    ref=f'  - {{fileID: {ids[1]}}}\n';s=s.replace(ref,'');s+=ref
group('CrimeScene_LightProbes',731210001,crime_probes)
group('Vegetation_LightProbes',731220001,plant_probes)
# Keep the authored sky and warm fog. A dim neutral ambient floor prevents
# completely unlit dynamic objects without adding emission to their materials.
old_ambient=re.search(r'(?m)^  m_AmbientSkyColor: .*',s)[0]
s=re.sub(r'(?m)^  m_AmbientSkyColor: .*','  m_AmbientSkyColor: {r: 0.20, g: 0.21, b: 0.23, a: 1}',s)
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M);assert len(ids)==len(set(ids))
PATH.write_text(s,encoding='utf-8')
report={'backup':str(backup.relative_to(ROOT)),'crimeObjects':len(crime),'vegetationObjects':len(plants),
        'crimeProbeCount':len(crime_probes),'vegetationProbeCount':len(plant_probes),
        'crimeBounds':[lo.tolist(),hi.tolist()],'oldAmbient':old_ambient,
        'objects':[{'name':n,'worldPosition':p.tolist()} for n,p,_ in targets],
        'baked':False,'unityRenderChecked':False,'skyAndFogUnchanged':True}
(work/'environment-probes-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='objects'},ensure_ascii=True))
