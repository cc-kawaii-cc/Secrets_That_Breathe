"""Sparse probes for the moving cabin; the dense parking group stays unchanged."""
from pathlib import Path
import re
import math
import json
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SCENE = ROOT/'Assets/MainScenes/From_PM/MainScene_2_Evening.unity'
IDS = (731200011,731200012,731200013)
s = SCENE.read_text(encoding='utf-8-sig')
folder = ROOT/'Tools/EveningLighting'
backup = folder/'before-sparse-route-probes.unity.txt'
if not backup.exists(): backup.write_text(s,encoding='utf-8')
def block(id):
    match = re.search(r'^--- !u!\d+ &'+str(id)+r'\n.*?(?=^---|\Z)',s,re.M|re.S)
    return match.group() if match else None
def vector(text):
    return np.array([float(v) for v in re.search(r'\{x: ([^,]+), y: ([^,]+), z: ([^}]+)',text).groups()])
ride = block(900260002)
start = vector(re.search(r'  startPosition: .*',ride).group())
end = vector(re.search(r'  endPosition: .*',ride).group())
corner_text = re.search(r'  intermediateRoutePoints:.*?(?=  cornerRadius:)',ride,re.S).group()
corners = [vector(t) for t in re.findall(r'  - \{x: [^\n]+',corner_text)]
radius = float(re.search(r'  cornerRadius: ([^\n]+)',ride)[1])
points = [start]+corners+[end]
samples = []
def line(a,b):
    direction = b-a
    distance = np.linalg.norm(direction)
    if distance < 1e-5: return
    for t in np.linspace(0,1,max(2,math.ceil(distance/20)+1)):
        samples.append((a*(1-t)+b*t,direction/distance))
last = start
for i in range(1,len(points)-1):
    incoming,outgoing = points[i]-points[i-1],points[i+1]-points[i]
    trim = min(radius,np.linalg.norm(incoming)*.45,np.linalg.norm(outgoing)*.45)
    entry = points[i]-incoming/np.linalg.norm(incoming)*trim
    exit = points[i]+outgoing/np.linalg.norm(outgoing)*trim
    line(last,entry)
    for t in np.linspace(0,1,max(3,math.ceil(trim*2/4)+1)):
        p=(1-t)**2*entry+2*(1-t)*t*points[i]+t*t*exit
        d=(1-t)*(points[i]-entry)+t*(exit-points[i]); d/=np.linalg.norm(d)
        samples.append((p,d))
    last = exit
line(last,end)
first_dir = (points[1]-start)/np.linalg.norm(points[1]-start)
samples.append((start-first_dir*3,first_dir))
probes = {}
for p,d in samples:
    right=np.cross([0,1,0],d); right/=np.linalg.norm(right)
    for side in (-2.5,2.5):
        for height in (.3,2.5):
            local=p+right*side+np.array([0,height,0])-start
            probes[tuple(round(float(v),4) for v in local)]=None
assert len(probes)<150 and np.linalg.matrix_rank(np.array(list(probes))-next(iter(probes)))==3
vectors=''.join('  - {x: %.4f, y: %.4f, z: %.4f}\n'%p for p in probes)
blocks=''.join(block(id) for id in (731200001,731200002,731200003))
blocks=blocks.replace('FD_Parking_LightProbes','FD_Route_Sparse_LightProbes')
for old,new in zip((731200001,731200002,731200003),IDS): blocks=blocks.replace(str(old),str(new))
blocks=re.sub(r'  m_LocalPosition: .*','  m_LocalPosition: {x: %s, y: %s, z: %s}'%tuple(start),blocks)
blocks=re.sub(r'  m_SourcePositions:\n.*?(?=  m_Dering:)','  m_SourcePositions:\n'+vectors,blocks,flags=re.S)
parking_before=[block(id) for id in (731200001,731200002,731200003)]
for id in IDS:
    existing=block(id)
    if existing:
        assert 'FD_Route_Sparse_LightProbes' in s
        s=s.replace(existing,'')
rootref=f'  - {{fileID: {IDS[1]}}}\n'
s=s.replace(rootref,'')
marker='--- !u!1660057539 &9223372036854775807'
assert s.count(marker)==1
s=s.replace(marker,blocks+marker)
s+=rootref
assert parking_before==[block(id) for id in (731200001,731200002,731200003)]
ids=re.findall(r'^--- !u!\d+ &(\d+)',s,re.M); assert len(ids)==len(set(ids))
SCENE.write_text(s,encoding='utf-8')
report=dict(routeProbeCount=len(probes),parkingProbeCount=36,totalFDProbes=len(probes)+36,
            straightSpacingMaximumMetres=20,cornerSpacingApproxMetres=4,
            routeHeightAboveRoadMetres=[.3,2.5],routeLateralOffsetsMetres=[-2.5,2.5],
            baked=False,unityRenderChecked=False)
(folder/'route-probes-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
