from pathlib import Path
import re
import json
import uuid
root = Path(__file__).resolve().parents[2]
scene = root/'Assets/MainScenes/From_PM/MainScene_2_Evening.unity'
s = scene.read_text(encoding='utf-8-sig')
backup = Path(__file__).parent/'Evening-before-F1-checkpoint.unity.txt'
if not backup.exists(): backup.write_text(s,encoding='utf-8')
assert not re.search(r'&7315000\d+\b',s)
scripts = root/'Assets/SecretsThatBreathe/Scripts'
for name in ['EveningTestingShortcuts','PorschePassengerRide.Testing']:
    meta = scripts/(name+'.cs.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+
                        '\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n'
                        '  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n'
                        '  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
guid = re.search(r'guid: (\w+)',(scripts/'EveningTestingShortcuts.cs.meta').read_text()).group(1)
lawyer = (root/'Assets/MainScenes/From_PM/Lawyer Room.unity').read_text(encoding='utf-8-sig')
def block(text,id):
    return re.search(r'^--- !u!\d+ &'+str(id)+r'\n.*?(?=^---|\Z)',text,re.M|re.S).group()
mapping={711400001:731500001,711400002:731500002,
         2056101521:731500010,2056101522:731500011,2056101523:731500012,2056101524:731500013,
         1011965194:731500020,1011965195:731500021,1011965196:731500022,1011965197:731500023,1011965198:731500024}
blocks=''
for old,new in mapping.items():
    b=block(lawyer,old)
    if old==711400001:
        b=b.replace('  m_Layer:', '  - component: {fileID: 731500003}\n  m_Layer:',1)
    if old==711400002:
        b=re.sub(r'  m_Children:\n.*?(?=  m_Father:)', '  m_Children:\n  - {fileID: 2056101522}\n',b,flags=re.S)
        b=b.replace('m_Father: {fileID: 1676370658}','m_Father: {fileID: 1987101001}')
    if old==2056101521: b=b.replace('TestSkipPhoneHint','TestSkipCarExitHint').replace('m_IsActive: 0','m_IsActive: 1')
    if old==1011965196:
        b=re.sub(r'^  m_text: .*',lambda m:'  m_text: '+json.dumps('[F1] Skip ไปตอนลงจากรถ',ensure_ascii=True),b,flags=re.M)
    b=re.sub(r'&('+ '|'.join(map(str,mapping))+r')\b',lambda m:'&'+str(mapping[int(m[1])]),b)
    b=re.sub(r'fileID: (\d+)',lambda m:'fileID: '+str(mapping.get(int(m[1]),int(m[1]))),b)
    blocks+=b
canvas=block(s,731000003).replace('&731000003','&731500003').replace('m_GameObject: {fileID: 731000001}','m_GameObject: {fileID: 731500001}')
canvas=re.sub(r'm_SortingOrder: \d+','m_SortingOrder: 32700',canvas)
blocks+=canvas
blocks+=f'''--- !u!114 &731500030
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1987101000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::SecretsThatBreathe.EveningTestingShortcuts
  ride: {{fileID: 900260002}}
  enableTestingShortcuts: 1
  testSkipHint: {{fileID: 731500010}}
'''
go=block(s,1987101000)
s=s.replace(go,go.replace('  m_Layer:', '  - component: {fileID: 731500030}\n  m_Layer:',1))
rect=block(s,1987101001)
s=s.replace(rect,rect.replace('  m_Children:\n','  m_Children:\n  - {fileID: 731500002}\n'))
s=s.replace('--- !u!1660057539',blocks+'--- !u!1660057539')
ids=re.findall(r'^--- !u!\d+ &(\d+)',s,re.M)
assert len(ids)==len(set(ids))
assert 'm_Father: {fileID: 1987101001}' in block(s,731500002)
assert 'm_Father: {fileID: 731500002}' in block(s,731500011)
assert 'm_Father: {fileID: 731500011}' in block(s,731500021)
scene.write_text(s,encoding='utf-8')
print('Saved Lawyer Room styled F1 hint under SeatingCanvas/TestingShortcutHints.')
rsp=(root/'Tools/FDRide/VillageAmbienceCompileCheck.rsp').read_text()
rsp=rsp.replace('Tools/FDRide/VillageAmbience.CompileCheck.dll','Tools/FootstepAudio/EveningSkip.CompileCheck.dll')
rsp+='\n"Assets/SecretsThatBreathe/Scripts/PorschePassengerRide.Testing.cs"\n"Assets/SecretsThatBreathe/Scripts/EveningTestingShortcuts.cs"\n'
(Path(__file__).parent/'EveningSkipCompileCheck.rsp').write_text(rsp)
