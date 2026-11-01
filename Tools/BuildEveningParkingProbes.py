"""Keep FD probes at its authored parking spot. Does not bake lighting."""
from pathlib import Path
import re
import json
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
scene = ROOT/'Assets/MainScenes/From_PM/MainScene_2_Evening.unity'
s = scene.read_text(encoding='utf-8-sig')
before = s
folder = ROOT/'Tools/EveningLighting'
backup = folder/'before-parking-only-probes.unity.txt'
if not backup.exists(): backup.write_text(s, encoding='utf-8')
def block(id):
    return re.search(r'^--- !u!\d+ &'+str(id)+r'\n.*?(?=^---|\Z)', s, re.M|re.S).group()
def vector(line):
    return np.array([float(v) for v in re.search(r'\{x: ([^,]+), y: ([^,]+), z: ([^}]+)', line).groups()])
ride = block(900260002)
end = vector(re.search(r'  endPosition: .*', ride).group())
route = re.search(r'  intermediateRoutePoints:.*?(?=  cornerRadius:)',ride,re.S).group()
corners = re.findall(r'  - \{x: [^\n]+',route)
previous = vector(corners[-1] if corners else re.search(r'  startPosition: .*',ride).group())
forward = end-previous
forward[1] = 0
forward /= np.linalg.norm(forward)
right = np.cross([0,1,0],forward)
probes = [forward*length+right*width+np.array([0,height,0])
          for length in (-4,-1.35,1.35,4)
          for width in (-2.5,0,2.5) for height in (.3,1.2,2.5)]
group = block(731200003)
old_count = len(re.findall(r'^  - \{x:',group,re.M))
vectors = ''.join('  - {x: %.4f, y: %.4f, z: %.4f}\n'%tuple(p) for p in probes)
new_group = re.sub(r'  m_SourcePositions:\n.*?(?=  m_Dering:)', '  m_SourcePositions:\n'+vectors,group,flags=re.S)
s = s.replace(group,new_group)
transform = block(731200002)
new_transform = re.sub(r'  m_LocalPosition: .*', '  m_LocalPosition: {x: %s, y: %s, z: %s}'%tuple(end), transform)
s = s.replace(transform,new_transform)
go = block(731200001)
s = s.replace(go,go.replace('FD_Route_LightProbes','FD_Parking_LightProbes'))
# Confirm all other scene objects, including crime/vegetation probes, are untouched.
def without_owned(text):
    for id in (731200001,731200002,731200003):
        text = re.sub(r'^--- !u!\d+ &'+str(id)+r'\n.*?(?=^---|\Z)','',text,flags=re.M|re.S)
    return text
assert without_owned(s) == without_owned(before)
assert len(probes) == 36 and np.linalg.matrix_rank(np.array(probes)-probes[0]) == 3
scene.write_text(s,encoding='utf-8')
# The separate sparse-route builder owns the cabin probes along the road.
report = dict(probeCount=36,coverage='parking only',center=end.tolist(),
              lengthMetres=8,widthMetres=5,heightAboveRoadMetres=[.3,1.2,2.5],
              stationaryRoot='FD_Parking_LightProbes',baked=False,unityRenderChecked=False)
(folder/'parking-probes-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(f'FD probes: {old_count} -> 36. Other scene objects unchanged.')
print(json.dumps(report))
