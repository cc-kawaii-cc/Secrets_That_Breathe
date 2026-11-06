"""Decode the existing standalone Unity chair mesh without changing it."""
from pathlib import Path
import re,json,hashlib,shutil
import numpy as np
out=Path('Tools/LawyerRoomOptimization');out.mkdir(exist_ok=True,parents=True)
path=Path('Assets/SecretsThatBreathe/Props/SmoothedChairs/Chair_5e546eeaa124d9c4fac7f82be9fd2043_4579454407841743219_Smooth60.asset')
backup=out/'Backups'/path;backup.parent.mkdir(parents=True,exist_ok=True)
if not backup.exists():shutil.copy2(path,backup);shutil.copy2(Path(str(path)+'.meta'),Path(str(backup)+'.meta'))
assert path.read_bytes()==backup.read_bytes(),'Chair changed since backup'
s=path.read_text(encoding='utf-8-sig');data=bytes.fromhex(re.search(r'^\s+_typelessdata: (\w+)',s,re.M)[1])
count=int(re.search(r'^\s+m_VertexCount: (\d+)',s,re.M)[1]);stride=len(data)//count
channels=[tuple(map(int,m)) for m in re.findall(r'- stream: (\d+)\s+offset: (\d+)\s+format: (\d+)\s+dimension: (\d+)',s)]
arrays={}
for index,name in [(0,'positions'),(1,'normals'),(4,'uv'),(5,'uv2')]:
    stream,offset,fmt,dim=channels[index];assert stream==0 and fmt==0 and dim>0
    arrays[name]=np.ndarray((count,dim),dtype='<f4',buffer=data,offset=offset,strides=(stride,4)).copy()
indices=bytes.fromhex(re.search(r'^\s+m_IndexBuffer: (\w+)',s,re.M)[1])
arrays['indices']=np.frombuffer(indices,dtype='<u4').copy()
np.savez(out/'source.npz',**arrays)
manifest={'source':path.as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
          'metaSHA256':hashlib.sha256(Path(str(path)+'.meta').read_bytes()).hexdigest(),
          'name':re.search(r'^  m_Name: (.*)',s,re.M)[1],'vertices':count,'triangles':len(arrays['indices'])//3,
          'backup':backup.as_posix()}
manifest['protectedFiles']={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in [Path('Assets/MainScenes/From_PM/Lawyer Room.unity'),
    Path('Assets/All model nine/NINE/Perfeb/Office_1/Perfeb_Ofice/chair.prefab'),Path('Assets/SecretsThatBreathe/Scripts/Editor/ChairSmoothShadingSetup.cs')]}
(out/'source.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Decoded exact current chair:',manifest['triangles'],'triangles;',count,'vertices; stride',stride)
