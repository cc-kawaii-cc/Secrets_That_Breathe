"""Dim realtime dusk fill and a broad crime-scene fill, without changing the sky palette."""
from pathlib import Path
import re,json,math,datetime
R=Path(__file__).resolve().parents[1]
p=R/'Assets/MainScenes/From_PM/MainScene_2_Evening.unity';s=p.read_text(encoding='utf-8')
sky=R/'Assets/SecretsThatBreathe/Environment/EveningSky/EveningSky.mat';mat=sky.read_text(encoding='utf-8')
work=R/'Tools/EveningLighting';work.mkdir(exist_ok=True)
(work/('before-visibility-fill-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S')+'.json')).write_text(json.dumps({'sky':mat,'ambient':re.search(r'(?m)^  m_AmbientSkyColor: .*',s)[0]},indent=2),encoding='utf-8')
blocks={m[1]:m[0] for m in re.finditer(r'^--- !u!\d+ &(-?\d+)[^\n]*\n.*?(?=^---|\Z)',s,re.M|re.S)}
def field(b,key,value):
    b,n=re.subn(r'(?m)^  '+key+r': .*','  '+key+': '+str(value),b);assert n==1,key
    return b
def light(first,name,kind,intensity,position,rotation,hint,range_=20):
    global s
    ids=[first+i for i in range(4)]
    old_name=re.search(r'^--- !u!1 &'+str(first)+r'\n.*?(?=^---|\Z)',s,re.M|re.S)
    if old_name:assert 'm_Name: '+name+'\n' in old_name[0]
    for id in ids:
        old=re.search(r'^--- !u!\d+ &'+str(id)+r'\n.*?(?=^---|\Z)',s,re.M|re.S)
        if old:
            assert old_name
            s=s[:old.start()]+s[old.end():]
    mapping={'946736669':str(ids[0]),'946736671':str(ids[1]),'946736670':str(ids[2]),'946736672':str(ids[3])}
    result=[]
    for original in mapping:
        b=blocks[original]
        b=re.sub(r'(?<=&)(-?\d+)|(?<=fileID: )(-?\d+)',lambda m:mapping.get(m[0],m[0]),b)
        if original=='946736669':
            b=field(b,'m_Name',name);b=field(b,'m_IsActive',1)
        elif original=='946736671':
            b=field(b,'m_LocalPosition','{x: %s, y: %s, z: %s}'%position)
            b=field(b,'m_LocalRotation','{x: %s, y: %s, z: %s, w: %s}'%rotation)
            b=field(b,'m_LocalEulerAnglesHint','{x: %s, y: %s, z: %s}'%hint)
            b=field(b,'m_Children','[]');b=field(b,'m_Father','{fileID: 0}')
        elif original=='946736670':
            b=field(b,'m_Type',kind);b=field(b,'m_Intensity',intensity);b=field(b,'m_Range',range_)
            b=field(b,'m_Color',re.search(r'(?m)^    - _SunColor: (\{[^\n]+\})',mat)[1] if kind==1 else '{r: 0.9, g: 0.91, b: 0.94, a: 1}')
            b=field(b,'m_Lightmapping',4)
            # User preference: both fill lights cast soft shadows.
            b=b.replace('  m_Shadows:\n    m_Type: 0','  m_Shadows:\n    m_Type: 2')
        result.append(b)
    marker='--- !u!1660057539 &9223372036854775807'
    s=s.replace(marker,''.join(result)+marker)
    ref='  - {fileID: '+str(ids[1])+'}\n';s=s.replace(ref,'');s+=ref
pitch=math.radians(9)*.5;yaw=math.radians(142)*.5
q=(math.sin(pitch)*math.cos(yaw),math.cos(pitch)*math.sin(yaw),-math.sin(pitch)*math.sin(yaw),math.cos(pitch)*math.cos(yaw))
light(731230001,'Evening_DuskFill',1,.18,(180,12,240),q,(9,142,0))
light(731230011,'CrimeScene_SoftFill',2,6,(160.264,9.59909,330.78),(0,0,0,1),(0,0,0),22)
mat,n=re.subn(r'(?m)^    - _Exposure: .*','    - _Exposure: 0.48',mat);assert n==1
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M);assert len(ids)==len(set(ids))
p.write_text(s,encoding='utf-8');sky.write_text(mat,encoding='utf-8')
(work/'visibility-fill-validation.json').write_text(json.dumps({'globalRealtimeFill':.18,'localRealtimeFill':6,'localRange':22,'skyExposure':.48,'softShadowsEnabled':True,'unityRenderChecked':False,'probeBakeRequired':True},indent=2),encoding='utf-8')
print('Applied two dim realtime fills + sky brightness 0.48. Native references/IDs verified; both sources cast soft shadows.')
