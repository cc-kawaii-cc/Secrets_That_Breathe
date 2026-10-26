from pathlib import Path
import re,json,shutil
p=Path('Assets/Tonpalm/Lawyer Room.unity');before=p.read_bytes();s=before.decode('utf-8').replace('\r\n','\n')
blocks={int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
def fid(b,key):return int(re.search(re.escape(key)+r': \{fileID: (-?\d+)',b).group(1))
def guid(path):return re.search(r'guid: (\w+)',Path(path).read_text()).group(1)
uis=[]
for script in ['PhoneCallDialogueUI','AuntDialogueUI']:
 g=guid('Assets/SecretsThatBreathe/Scripts/'+script+'.cs.meta')
 i=next(i for i,(k,b) in blocks.items() if k==114 and 'guid: '+g+',' in b)
 uis.append((script,i,blocks[i][1]))
if all(re.search(r'typingAudio: \{fileID: [1-9]',b) for _,_,b in uis):
 print('AudioSources already saved by Unity.');raise SystemExit(0)
clip=guid('Assets/video-game-text-330163.mp3.meta')
template=next(b for _,(k,b) in blocks.items() if k==82 and re.search(r'^  panLevelCustomCurve:\n.*?      value: 0\n',b,re.M|re.S))
rectbygo={fid(b,'m_GameObject'):i for i,(k,b) in blocks.items() if k==224}
nextid=700200000
parts=[]
for script,uiid,ui in uis:
 while any(i in blocks for i in range(nextid,nextid+3)):nextid+=3
 go,tr,aud=range(nextid,nextid+3);nextid+=3
 parent=rectbygo[fid(ui,'m_GameObject')]
 parts.append(f'''--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr}}}
  - component: {{fileID: {aud}}}
  m_Layer: 0
  m_Name: DialogueTypingAudio
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tr}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {parent}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
''')
 audio=re.sub(r'm_GameObject: \{fileID: \d+\}','m_GameObject: {fileID: '+str(go)+'}',template)
 for key,val in [('m_PlayOnAwake','0'),('m_Volume','0.35'),('Loop','1'),('Mute','0')]:
  audio=re.sub(r'^  '+key+r': .*','  '+key+': '+val,audio,flags=re.M)
 for key in ['m_audioClip','m_Resource']:
  audio=re.sub(r'^  '+key+r': .*','  '+key+': {fileID: 8300000, guid: '+clip+', type: 3}',audio,flags=re.M)
 parts.append('--- !u!82 &'+str(aud)+'\n'+audio)
 oldparent=blocks[parent][1];newparent=oldparent.replace('  m_Father:','  - {fileID: '+str(tr)+'}\n  m_Father:',1)
 s=s.replace(oldparent,newparent,1)
 newui=re.sub(r'(^  presentation:\n.*?^    scrollSpeed: [^\n]+)',lambda m:m.group(1)+'\n    typingAudio: {fileID: '+str(aud)+'}',ui,flags=re.M|re.S)
 assert newui!=ui
 if script=='AuntDialogueUI':
  newui=re.sub(r'^  visitorName: .*',lambda m:'  visitorName: '+json.dumps('ลุงป้อง',ensure_ascii=True)+'\n  typingAudioSetupRevision: 1',newui,flags=re.M)
  sid=int(re.search(r'  visitorDialogue:\n.*?    speaker: \{fileID: (\d+)',ui,re.S).group(1))
  oldname=blocks[sid][1]
  newname=re.sub(r'^  m_text: .*?(?=\n  m_\w+:)',lambda m:'  m_text: '+json.dumps('ลุงป้อง',ensure_ascii=True),oldname,flags=re.M|re.S)
  s=s.replace(oldname,newname,1)
 s=s.replace(ui,newui,1)
s=s.rstrip()+'\n'+''.join(parts)
assert p.read_bytes()==before,'Scene changed during patch; retry.'
shutil.copy2(p,'Tools/DialogueAudit/LawyerRoom-before-dialogue-audio-disk.unity.backup')
p.write_text(s,encoding='utf-8')
print('Saved both typing AudioSources and ลุงป้อง speaker on Canvas.')
