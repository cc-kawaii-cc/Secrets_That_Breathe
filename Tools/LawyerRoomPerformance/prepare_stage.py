"""Prepare a read-only saved-scene audit in an isolated Unity project."""
from pathlib import Path
import re,json,shutil,hashlib
root=Path('.').resolve();out=root/'Tools/LawyerRoomPerformance';stage=root/'Tools/CompileChecks/LawyerRoomAuditUnity'
scene=Path('Assets/MainScenes/From_PM/Lawyer Room.unity')
index={}
for p in Path('Assets').rglob('*.meta'):
    with p.open(encoding='utf-8-sig',errors='replace') as f:s=f.read(200)
    if m:=re.search(r'^guid: (\w+)',s,re.M):index[m[1]]=Path(str(p)[:-5])
todo=[scene];seen=set();copied=[];unresolved=set()
skip={'.cs','.dll','.asmdef','.asmref','.mp3','.wav','.ogg','.mp4','.unitypackage','.txt','.md','.pdf','.zip'}
while todo:
    p=todo.pop()
    if p in seen or not p.is_file() or p.suffix.lower() in skip:continue
    seen.add(p)
    text=''
    if p.suffix.lower() in ['.unity','.prefab','.asset','.mat','.controller','.overridecontroller','.anim']:
        raw=p.read_bytes()
        if raw.startswith(b'%YAML'):text=raw.decode('utf-8-sig',errors='replace')
    meta=Path(str(p)+'.meta');mt=meta.read_text(encoding='utf-8-sig') if meta.exists() else ''
    for g in re.findall(r'guid: ([0-9a-f]{32})',text+'\n'+mt):
        if g in index:todo.append(index[g])
        elif not g.startswith('0000000000000000'):unresolved.add(g)
    dest=stage/p;dest.parent.mkdir(exist_ok=True,parents=True);shutil.copy2(p,dest)
    if meta.exists():
        if p.suffix.lower() in ['.fbx','.obj','.dae']:mt=re.sub(r'(generateSecondaryUV:) 1',r'\1 0',mt)
        Path(str(dest)+'.meta').write_text(mt,encoding='utf-8')
    copied.append({'path':p.as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
for d in ['Assets/Editor','Packages','ProjectSettings']:(stage/d).mkdir(exist_ok=True,parents=True)
(stage/'Packages/manifest.json').write_text('{"dependencies":{"com.unity.modules.physics":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}')
shutil.copy2('ProjectSettings/ProjectVersion.txt',stage/'ProjectSettings/ProjectVersion.txt')
shutil.copy2('Assets/SecretsThatBreathe/Scripts/Editor/LawyerRoomPerformanceAudit.cs',stage/'Assets/Editor/LawyerRoomPerformanceAudit.cs')
request={'copied':copied,'unresolvedPackageOrExternalGUIDs':sorted(unresolved),'sourceSceneSHA256':hashlib.sha256(scene.read_bytes()).hexdigest(),
         'skippedScriptsAudioAndVideos':True,'lightmapUVGenerationDisabledInCopiesOnly':True}
(out/'stage-manifest.json').write_text(json.dumps(request,indent=2),encoding='utf-8')
print('Copied',len(copied),'files,',round(sum(p['bytes'] for p in copied)/1048576,2),'MiB')
print('Models',[(p['path'],round(p['bytes']/1048576,2)) for p in copied if Path(p['path']).suffix.lower() in ['.fbx','.obj','.dae','.blend']])
