from pathlib import Path
import re,json,uuid,datetime
pat=r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)'
def parse(s):return {int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(pat,s,re.M|re.S)}
p=Path('Assets/MainScenes/From_PM/MainScene_2_Evening.unity');raw=p.read_bytes();s=p.read_text(encoding='utf-8');bs=parse(s);src=parse(Path('Assets/MainScenes/From_PM/Lawyer Room.unity').read_text(encoding='utf-8'))
assert '::SecretsThatBreathe.EveningPoliceEncounter\n' not in s

def prop(b,key):
 m=re.search(r'^  '+re.escape(key)+r': (.*)',b,re.M);return m.group(1) if m else None

def fid(b,key):return int(re.search(r'^  '+re.escape(key)+r': \{fileID: (-?\d+)',b,re.M).group(1))
def field(b,key,val):
 b,n=re.subn(r'^  '+re.escape(key)+r':[^\n]*',lambda m:'  '+key+': '+val,b,flags=re.M);assert n==1,key;return b

def text(b,value):
 b,n=re.subn(r'^  m_text:.*?(?=^  m_isRightToLeft:)',lambda m:'  m_text: '+json.dumps(value,ensure_ascii=True)+'\n',b,flags=re.M|re.S);assert n==1;return b

def scriptguid(name):
 m=Path('Assets/SecretsThatBreathe/Scripts/'+name+'.cs.meta')
 if not m.exists():m.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
 return re.search(r'^guid: (\w+)',m.read_text(),re.M).group(1)
nextid=842000000;new={}
def add(k,b):
 global nextid
 nextid+=1;assert nextid not in bs;new[nextid]=(k,b);return nextid

def children(rect,refs):
 k,b=new[rect];b=re.sub(r'^  m_Children:(?: \[\]|\n(?:  - \{fileID:.*\n)*\s*(?=  m_Father:))',lambda m:'  m_Children:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in refs).rstrip(),b,flags=re.M)
 # Use a bounded replacement for both empty and non-empty lists.
 b=re.sub(r'  m_Children:.*?(?=\n  m_Father:)',lambda m:'  m_Children:'+(' []' if not refs else '\n'+''.join(f'  - {{fileID: {i}}}\n' for i in refs).rstrip()),b,flags=re.S)
 new[rect]=(k,b)

def componentlist(go,ids):
 k,b=new[go];b=re.sub(r'  m_Component:\n(?:  - component:.*\n)+','  m_Component:\n'+''.join(f'  - component: {{fileID: {i}}}\n' for i in ids),b);new[go]=(k,b)

def group(name,parent):
 go=add(1,src[711100011][1]);r=field(field(src[711100012][1],'m_GameObject',f'{{fileID: {go}}}'),'m_Father',f'{{fileID: {parent}}}');rect=add(224,r)
 new[go]=(1,field(new[go][1],'m_Name',name));componentlist(go,[rect]);children(rect,[]);return go,rect

transforms={fid(b,'m_GameObject'):(i,b) for i,(k,b) in src.items() if k in [4,224] and re.search(r'^  m_GameObject:',b,re.M)}

def clone(go,parent,name):
 included={go};todo=[go]
 while todo:
  current=todo.pop();rid=transforms[current][0]
  for g,(r,b) in transforms.items():
   if fid(b,'m_Father')==rid and g not in included:included.add(g);todo.append(g)
 ids=[i for i,(k,b) in src.items() if (k==1 and i in included) or (k!=1 and re.search(r'^  m_GameObject: \{fileID: (-?\d+)\}',b,re.M) and fid(b,'m_GameObject') in included)]
 mapping={i:add(src[i][0],src[i][1]) for i in ids}
 for old,i in mapping.items():
  k,b=src[old];b=re.sub(r'\{fileID: (-?\d+)\}',lambda m:'{fileID: '+str(mapping.get(int(m.group(1)),int(m.group(1))))+'}',b);new[i]=(k,b)
 root=mapping[go];rect=mapping[transforms[go][0]];new[root]=(1,field(new[root][1],'m_Name',name));new[rect]=(224,field(new[rect][1],'m_Father',f'{{fileID: {parent}}}'))
 return root,rect,mapping

def mono(go,name,body):
 b=field(src[274621871][1],'m_GameObject',f'{{fileID: {go}}}');b=re.sub(r'guid: 1b9a119f382d4ed48dae4d7102e821fe','guid: '+scriptguid(name),b);b=field(b,'m_EditorClassIdentifier','Assembly-CSharp::SecretsThatBreathe.'+name);return add(114,b+body)

def colour(label,c):
 k,b=new[label];value='{r: '+str(c[0])+', g: '+str(c[1])+', b: '+str(c[2])+', a: 1}';b=field(field(b,'m_Color',value),'m_fontColor',value)
 rgba=round(c[0]*255)|(round(c[1]*255)<<8)|(round(c[2]*255)<<16)|(255<<24);b=re.sub(r'(m_fontColor32:\n    serializedVersion: 2\n    rgba:) \d+',lambda m:m.group(1)+' '+str(rgba),b);new[label]=(k,b)

root,rr=group('PoliceEncounter',1987101001);spoken,sr=group('Lines',rr);narration,nr=group('NarrationLines',rr);choicegroup,cr=group('Choices',rr)
rootchildren=[sr,nr,cr];spokenrects=[];narrationrects=[];lines={}
texts={
'intro1':'ขอโทษนะครับ','intro2':'พื้นที่นี้อนุญาติให้เข้าแค่เจ้าหน้าที่นะครับ',
'lieReply':'พอดีผมได้รับอนุญาติจากตำรวจที่สถานีมาแล้วน่ะครับ เพราะฉะนั้นขอเข้าไปดูหน่อยนะครับ',
'lawyerReply':'ผมเป็นทนายของฝั่งเหยื่อในคดีนี้ พอดีเห็นว่ายังพึ่งเกิดเหตุได้ไม่นาน เลยอยากจะมาดูด้วยตาตนเองหน่อยน่ะครับ',
'refused1':'ยังไงก็ไม่อนุญาติครับ','refused2':'คุณเห็นไหมว่าเจ้าหน้าที่ทำงานกันอยู่','refused3':'กรุณาถอยออกไปและอย่ารบกวนการทำงานของเจ้าหน้าที่ด้วยครับ',
'sunglassesReply':'แล้วนั่นเจ้าหน้าที่ด้วยไหมครับ','evidence1':'ขอโทษที่รบกวนครับ','evidence2':'แต่ผมมีสิทธิ์เก็บหลักฐานในที่เกิดเหตุเพื่อปกป้องสิทธ์ของลูกความผม',
'final1':'แต่ตามกฏหมายคุณไม่มีสิทธิ์บังคับให้ผมเปิดพื้นที่ให้คุณตามอำเภอใจ','final2':'ถ้าอยากได้สิทธิ์เข้าถึงพื้นที่ ไปคุยกับหัวหน้าผมที่สถานี','final3':'ระหว่างนี้กรุณาถอยไปครับ',
'radioNarration':'(ตำรวจวิทยุกลับไปที่สถานีเพื่อเช็คว่าจริงไหม)','threatNarration':'(ตำรวจไม่พูดอะไร เอามือนึงจับปืนและอีกมือดันตัวเข้มให้ถอยออกไป)'}
for n,(key,value) in enumerate(texts.items(),1):
 parent=nr if key.endswith('Narration') else sr
 go,r,m=clone(1580522195,parent,f'{n:02d}_{key}');label=m[1879748197];new[label]=(114,text(new[label][1],value));lines[key]=label
 (narrationrects if parent==nr else spokenrects).append(r)
children(sr,spokenrects);children(nr,narrationrects)
views={}
for name,canvas,motion,speaker,sentence,speakerName,c in [
 ('PoliceDialogueCanvas',1344492686,1051314041,822551971,2029957246,'ตำรวจเลว',(.6754716,.22292897,.15166242)),
 ('KhemDialogueCanvas',1868462039,622051536,1476477200,1433489185,'เข้ม',(.25,.7,1))]:
 go,r,m=clone(fid(src[canvas][1],'m_GameObject'),rr,name);rootchildren.append(r);new[go]=(1,field(new[go][1],'m_IsActive','0'))
 new[m[speaker]]=(114,text(new[m[speaker]][1],speakerName));colour(m[speaker],c);new[m[sentence]]=(114,text(new[m[sentence]][1],''));colour(m[sentence],(1,1,1))
 views[name]={'canvas':m[canvas],'motionRoot':m[motion],'speaker':m[speaker],'sentence':m[sentence]}
# Narration uses the objective's bottom position, white text and no speaker.
go,r,m=clone(1469328448,rr,'NarrationCanvas');rootchildren.append(r);label=m[1927969891];colour(label,(1,1,1));new[label]=(114,text(new[label][1],''))
canvas=add(223,field(src[1344492686][1],'m_GameObject',f'{{fileID: {go}}}'));componentlist(go,[int(i) for i in re.findall(r'component: \{fileID: (\d+)\}',new[go][1])]+[canvas])
views['NarrationCanvas']={'canvas':canvas,'motionRoot':r,'speaker':0,'sentence':label}
# Two authored choice sets, each with independent editable Labels.
choice_menus=[];choice_refs=[]
for n,captions in enumerate([['<color=#FF3333>(โกหก)</color> '+texts['lieReply'],texts['lawyerReply']],[texts['sunglassesReply'],'ขอโทษที่รบกวนครับ '+texts['evidence2']]],1):
 go,r,m=clone(1437111846,cr,'ChoiceSet'+str(n));choice_menus.append(r);choice_refs.append(m[1437111848])
 for key,val in [('m_AnchorMin','{x: 0.5, y: 0.32}'),('m_AnchorMax','{x: 0.5, y: 0.32}'),('m_SizeDelta','{x: 1500, y: 360}')]:new[r]=(224,field(new[r][1],key,val))
 cb=src[1437111848][1]
 for side,caption,y in [('accept',captions[0],100),('refuse',captions[1],-100)]:
  lab=int(re.search(r'^  '+side+r'Label: \{fileID: (\d+)',cb,re.M).group(1));button=int(re.search(r'^  '+side+r'Button: \{fileID: (\d+)',cb,re.M).group(1));bg=fid(src[button][1],'m_GameObject');br=transforms[bg][0]
  new[m[bg]]=(1,field(new[m[bg]][1],'m_Name','Choice1' if side=='accept' else 'Choice2'))
  for key,val in [('m_AnchoredPosition',f'{{x: 0, y: {y}}}'),('m_SizeDelta','{x: 1460, y: 150}')]:new[m[br]]=(224,field(new[m[br]][1],key,val))
  new[m[lab]]=(114,text(new[m[lab]][1],caption));colour(m[lab],(1,1,1));new[m[lab]]=(114,field(new[m[lab]][1],'m_isRichText','1'))
children(cr,choice_menus)
# Saved objective and boarding prompt.
go,r,m=clone(1469328448,rr,'ReturnToCarObjective');rootchildren.append(r);objective=m[1927969891];new[objective]=(114,text(new[objective][1],'"กลับไปที่รถ"'))
go,r,m=clone(1813318281,rr,'BoardCarPrompt');rootchildren.append(r);prompt=m[1776115748];new[prompt]=(114,text(new[prompt][1],'[F] ขึ้นรถ'))
# Two saved audio sources.
audioids=[]
for name,guid,volume in [('JumpscareAudio','b3ec5f2d9a9c5c34db350a7de763a20e',.8),('DialogueTypingAudio','a64cd7f25e0305446a846e879daefa6c',.35)]:
 go,r,m=clone(700200000,rr,name);rootchildren.append(r);source=m[700200002];new[source]=(82,field(field(field(new[source][1],'m_Resource',f'{{fileID: 8300000, guid: {guid}, type: 3}}'),'m_Volume',str(volume)),'Loop','0' if name=='JumpscareAudio' else '1'));audioids.append(source)
# Scene-owned prefab references; no changes to either character prefab.
stripped=[]
for k,source,prefab,instance in [(4,767316361547216777,'bca392387fe1513cbb040163148918ab',1752053072),(4,1001,'45d1ac6644ce526ab190f1b9c84d77a6',2122602504),(114,731600102,'bca392387fe1513cbb040163148918ab',1752053072)]:
 i=add(k,('Transform' if k==4 else 'MonoBehaviour')+f':\n  m_CorrespondingSourceObject: {{fileID: {source}, guid: {prefab}, type: 3}}\n  m_PrefabInstance: {{fileID: {instance}}}\n  m_PrefabAsset: {{fileID: 0}}\n'+('  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: f6694cd6a14f4e1aa75c374e360aec3b, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: Assembly-CSharp::SecretsThatBreathe.PoliceJumpscareRange\n' if k==114 else ''));stripped.append(i)
police,man,rangeid=stripped
pose=mono(root,'PoliceEncounterGestures',f'  policeRoot: {{fileID: {police}}}\n  blendSpeed: 5\n  radioHandOffset: {{x: -0.18, y: 1.5, z: 0.13}}\n  gunHandOffset: {{x: 0.22, y: 0.92, z: 0.05}}\n  pushingHandOffset: {{x: -0.18, y: 1.3, z: 0.68}}\n')
body=f'  range: {{fileID: {rangeid}}}\n  policeRoot: {{fileID: {police}}}\n  sunglassesMan: {{fileID: {man}}}\n  ride: {{fileID: 900260002}}\n  gestures: {{fileID: {pose}}}\n  scareAudio: {{fileID: {audioids[0]}}}\n  turnSeconds: 0.18\n  zoomStartTurnFraction: 0.65\n  zoomSeconds: 0.6\n  scareFieldOfView: 30\n  dialogueStartDelay: 0.5\n  focusSeconds: 0.35\n  sunglassesFieldOfView: 35\n  cameraReturnSeconds: 0.6\n'
for field_,view in [('policeDialogue','PoliceDialogueCanvas'),('khemDialogue','KhemDialogueCanvas'),('narrationDialogue','NarrationCanvas')]:
 body+='  '+field_+':\n'+''.join(f'    {key}: {{fileID: {i}}}\n' for key,i in views[view].items())+'    normalizeFontBaseline: '+('0' if field_=='narrationDialogue' else '1')+'\n    layoutRevision: 2\n'
body+=f'  presentation:\n    secondsPerCharacter: 0.05\n    completedLineHold: 1\n    scrollSpeed: 18\n    typingAudio: {{fileID: {audioids[1]}}}\n'
for field_,keys in [('introduction',['intro1','intro2']),('refusedReplies',['refused1','refused2','refused3']),('evidenceReplies',['evidence1','evidence2']),('finalPoliceReplies',['final1','final2','final3'])]:body+='  '+field_+':\n'+''.join(f'  - {{fileID: {lines[key]}}}\n' for key in keys)
for key in ['lieReply','lawyerReply','radioNarration','sunglassesReply','threatNarration']:body+=f'  {key}: {{fileID: {lines[key]}}}\n'
body+=f'  firstChoices: {{fileID: {choice_refs[0]}}}\n  secondChoices: {{fileID: {choice_refs[1]}}}\n  narrationHoldSeconds: 3\n  returnObjective: {{fileID: {objective}}}\n  boardingPrompt: {{fileID: {prompt}}}\n  returnObjectiveDelay: 1\n  boardingDistance: 2\n  pushDistance: 0.4\n  pushSeconds: 0.3\n  onReadyToChangeScene:\n    m_PersistentCalls:\n      m_Calls: []\n'
controller=mono(root,'EveningPoliceEncounter',body);componentlist(root,[rr,controller,pose]);children(rr,rootchildren)
# Saved EventSystem beneath SeatingCanvas. The existing choice code assigns default actions.
go,r=group('DialogueEventSystem',1987101001)
base=field(src[274621871][1],'m_GameObject',f'{{fileID: {go}}}')
es=add(114,field(re.sub(r'guid: 1b9a119f382d4ed48dae4d7102e821fe','guid: 76c392e42b5098c458856cdf6ecaaaa1',base),'m_EditorClassIdentifier','UnityEngine.UI::UnityEngine.EventSystems.EventSystem')+'  m_FirstSelected: {fileID: 0}\n  m_sendNavigationEvents: 1\n  m_DragThreshold: 10\n')
module=add(114,field(re.sub(r'guid: 1b9a119f382d4ed48dae4d7102e821fe','guid: 01614664b831546d2ae94a42149d80ac',base),'m_EditorClassIdentifier','Unity.InputSystem::UnityEngine.InputSystem.UI.InputSystemUIInputModule')+'  m_ActionsAsset: {fileID: 0}\n')
componentlist(go,[r,es,module])
parent=1987101001;old=bs[parent][1];updated=old.replace('  m_Father:',f'  - {{fileID: {rr}}}\n  - {{fileID: {r}}}\n  m_Father:',1)
s2=s.replace(old,updated)+''.join(f'--- !u!{k} &{i}'+(' stripped' if i in stripped else '')+f'\n{b}' for i,(k,b) in new.items())
after=parse(s2)
for i,v in bs.items():
 if i!=parent:assert after[i]==v,('Existing object changed',i)
for i,(k,b) in new.items():
 for line in b.splitlines():
  if 'guid:' in line:continue
  for value in re.findall(r'\{fileID: (-?\d+)\}',line):assert int(value)==0 or int(value) in after,(i,value)
backup=Path('Tools/EveningPoliceEncounter')/('before-encounter-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S')+'.unity.txt');backup.write_bytes(raw)
assert p.read_bytes()==raw;p.write_text(s2,encoding='utf-8',newline='\n')
report={'controller':controller,'root':root,'rootRect':rr,'views':views,'lines':lines,'choices':choice_refs,'returnObjective':objective,'boardingPrompt':prompt,'newBlocks':len(new),'stripped':stripped}
Path('Tools/EveningPoliceEncounter/ui-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('PASS:',len(new),'new serialized blocks; all UI under SeatingCanvas. Existing scene changed only its canvas child list.')
