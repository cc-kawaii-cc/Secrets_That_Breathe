"""Remove only the discarded evening encounter; retain the editable proximity ring."""
from pathlib import Path
import re,json,zipfile
ROOT=Path(__file__).resolve().parents[2]
WORK=Path(__file__).parent
SCENE=ROOT/'Assets/MainScenes/From_PM/MainScene_2_Evening.unity'
POLICE=ROOT/'Assets/SecretsThatBreathe/Characters/ThaiPolice/IdleTalk/ThaiPolice_IdleTalk.prefab'
SUIT=ROOT/'Assets/SecretsThatBreathe/Characters/NavySuitVillain/NavySuitVillain.prefab'
scripts=ROOT/'Assets/SecretsThatBreathe/Scripts'
names=['EveningPoliceDialogue','PoliceJumpscareSequence','SuitEncounterLook','PorschePassengerRide.Boarding',
       'Editor/EveningPoliceDialogueSetup','Editor/PoliceConversationRegressionCheck']
files=[scripts/(n+suffix) for n in names for suffix in ['.cs','.cs.meta']]
snapshot=WORK/'before-removal.zip'
if not snapshot.exists():
    with zipfile.ZipFile(snapshot,'w',compression=zipfile.ZIP_DEFLATED) as archive:
        for p in [SCENE,POLICE,SUIT,*files]:
            if p.exists(): archive.write(p,p.relative_to(ROOT).as_posix())
        for folder in ['Tools/EveningPoliceDialogue','Tools/CompileChecks/PoliceSentenceSplit']:
            for p in (ROOT/folder).glob('*'):
                if p.is_file() and p.suffix in ['.py','.cs','.rsp','.json','.txt']: archive.write(p,p.relative_to(ROOT).as_posix())
guids=[re.search(r'guid: (\w+)',(scripts/(n+'.cs.meta')).read_text())[1] for n in names]
def blocks(s):
    return {int(m[2]):m.group() for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^---|\Z)',s,re.M|re.S)}
s=SCENE.read_text(encoding='utf-8-sig');doc=blocks(s)
root=next(i for i,b in doc.items() if '  m_Name: PoliceEncounter\n' in b)
def subtree(go):
    ids={go}
    components=[int(v) for v in re.findall(r'  - component: \{fileID: (-?\d+)',doc[go])]
    ids.update(components)
    rect=next(id for id in components if doc[id].startswith(('--- !u!224 ','--- !u!4 ')))
    children=re.search(r'  m_Children:(.*?)(?=  m_Father:)',doc[rect],re.S)[1]
    for child in re.findall(r'fileID: (-?\d+)',children):
        owner=int(re.search(r'm_GameObject: \{fileID: (\d+)',doc[int(child)])[1])
        ids.update(subtree(owner))
    return ids
removed=subtree(root)
for id in removed: s=s.replace(doc[id],'')
for id in removed:
    s=re.sub(r'^  - \{fileID: '+str(id)+r'\}\n','',s,flags=re.M)
# Remove encounter-script/source overrides, including explicit UI binding.
for entry in re.findall(r'^    - target: .*?\n.*?(?=^    - target:|^    m_RemovedComponents:)',s,re.M|re.S):
    if re.search(r'target: \{fileID: (731600103|731600104|731600201),',entry): s=s.replace(entry,'')
assert 'PoliceEncounter\n' not in s
assert not any(f'guid: {g},' in s for g in guids)
SCENE.write_text(s,encoding='utf-8')
def remove_prefab_components(path,ids):
    text=path.read_text(encoding='utf-8-sig');parts=blocks(text)
    for id in ids:
        assert id in parts
        text=text.replace(parts[id],'')
        text=text.replace(f'  - component: {{fileID: {id}}}\n','')
    assert not any(f'guid: {g},' in text for g in guids)
    path.write_text(text,encoding='utf-8')
remove_prefab_components(POLICE,[731600103,731600104])
remove_prefab_components(SUIT,[731600201])
for p in files:
    assert p.resolve().is_relative_to(ROOT)
    p.unlink()
for rel in ['Tools/FootstepAudio/PoliceScareCompileCheck.rsp']:
    p=ROOT/rel
    if p.exists(): p.unlink()
report={'removedSceneObjectsAndComponents':len(removed),'removedScripts':names,
        'kept':['PoliceJumpscareRange','NPC models and animation assets','colliders','original drive/exit','F1/F2 testing shortcuts']}
(WORK/'removal-report.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
