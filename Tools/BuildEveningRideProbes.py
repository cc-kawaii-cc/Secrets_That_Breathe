"""Place stationary baked-light probes along FD's authored route (no bake)."""
from pathlib import Path
import re, json, math
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SCENE = ROOT/'Assets/MainScenes/From_PM/MainScene_2_Evening.unity'
IDS = (731200001, 731200002, 731200003)
s = SCENE.read_text(encoding='utf-8')
ride = re.search(r'^--- !u!114 &900260002\n.*?(?=^---|\Z)', s, re.M|re.S)[0]
def vector(text):
    return np.array([float(x) for x in re.search(r'\{x: ([^,]+), y: ([^,]+), z: ([^}]+)', text).groups()])
start = vector(re.search(r'  startPosition: .*',ride)[0])
end = vector(re.search(r'  endPosition: .*',ride)[0])
corner_text = re.search(r'  intermediateRoutePoints:.*?(?=  cornerRadius:)',ride,re.S)[0]
corners = [vector(t) for t in re.findall(r'  - \{x: [^\n]+', corner_text)]
radius = float(re.search(r'  cornerRadius: ([^\n]+)',ride)[1])
points=[start]+corners+[end]
samples=[]
def line(a,b,spacing=6):
    for t in np.linspace(0,1,max(2,math.ceil(np.linalg.norm(b-a)/spacing)+1)):
        samples.append((a*(1-t)+b*t,(b-a)/np.linalg.norm(b-a)))
last=start
for i in range(1,len(points)-1):
    incoming=points[i]-points[i-1];outgoing=points[i+1]-points[i]
    trim=min(radius,np.linalg.norm(incoming)*.45,np.linalg.norm(outgoing)*.45)
    entry=points[i]-incoming/np.linalg.norm(incoming)*trim
    exit=points[i]+outgoing/np.linalg.norm(outgoing)*trim
    line(last,entry)
    for t in np.linspace(0,1,max(5,math.ceil(trim*2/2)+1)):
        p=(1-t)**2*entry+2*(1-t)*t*points[i]+t*t*exit
        d=(1-t)*(points[i]-entry)+t*(exit-points[i]);d/=np.linalg.norm(d)
        samples.append((p,d))
    last=exit
line(last,end)
first_dir=(points[1]-start)/np.linalg.norm(points[1]-start)
last_dir=(end-points[-2])/np.linalg.norm(end-points[-2])
samples.extend([(start-first_dir*5,first_dir),(end+last_dir*5,last_dir)])
probes={}
for p,d in samples:
    right=np.cross([0,1,0],d);right/=np.linalg.norm(right)
    for side in (-2.5,0,2.5):
        for height in (.3,1.2,2.5):
            local=p+right*side+np.array([0,height,0])-start
            probes[tuple(round(float(x),4) for x in local)]=None
assert len(probes)>=100 and len(set(p[1] for p in probes))>=3
vectors=''.join('  - {x: %.4f, y: %.4f, z: %.4f}\n'%p for p in probes)
block=f'''--- !u!1 &{IDS[0]}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {IDS[1]}}}
  - component: {{fileID: {IDS[2]}}}
  m_Layer: 0
  m_Name: FD_Route_LightProbes
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{IDS[1]}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {IDS[0]}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {start[0]}, y: {start[1]}, z: {start[2]}}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!220 &{IDS[2]}
LightProbeGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {IDS[0]}}}
  m_Enabled: 1
  m_SourcePositions:
{vectors}  m_Dering: 1
'''
for id in IDS:
    existing=re.search(r'^--- !u!\d+ &'+str(id)+r'\n.*?(?=^---|\Z)',s,re.M|re.S)
    if existing:
        assert 'm_Name: FD_Route_LightProbes' in s
        s=s[:existing.start()]+s[existing.end():]
marker='--- !u!1660057539 &9223372036854775807'
assert s.count(marker)==1
s=s.replace(marker,block+marker)
rootref=f'  - {{fileID: {IDS[1]}}}\n'
s=s.replace(rootref,'')
s+=rootref
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M);assert len(ids)==len(set(ids))
SCENE.write_text(s,encoding='utf-8')
report={'probeCount':len(probes),'spacingStraightMetres':6,'spacingCornerMetres':2,
        'heightAboveRoadMetres':[.3,1.2,2.5],'lateralOffsetsMetres':[-2.5,0,2.5],
        'routeStart':start.tolist(),'routeEnd':end.tolist(),'stationaryRoot':'FD_Route_LightProbes',
        'baked':False,'unityRenderChecked':False}
folder=ROOT/'Tools/EveningLighting';folder.mkdir(exist_ok=True)
(folder/'route-probes-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
