from pathlib import Path
import re,json,shutil
p=Path('Assets/Tonpalm/Lawyer Room.unity');before=p.read_bytes();s=before.decode('utf-8').replace('\r\n','\n')
blocks={int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
def fid(b,key):return int(re.search(re.escape(key)+r': \{fileID: (-?\d+)',b).group(1))
def text(b):
 raw=re.search(r'^  m_text: (.*?)(?=\n  m_\w+:)',b,re.M|re.S).group(1)
 return json.loads(re.sub(r'\n +',' ',raw)) if raw.startswith('"') else raw.strip()
guid=re.search(r'guid: (\w+)',Path('Assets/SecretsThatBreathe/Scripts/PhoneCallDialogueUI.cs.meta').read_text()).group(1)
uiid=next(i for i,(k,b) in blocks.items() if k==114 and 'guid: '+guid+',' in b);ui=blocks[uiid][1]
if re.search(r'^  tonRequestSentence: \{fileID: [1-9]',ui,re.M):
 print('Request Canvas already saved by Unity.');raise SystemExit(0)
arr=re.search(r'^  sentences:\n((?:  - \{fileID: \d+\}\n)+)',ui,re.M)
ids=[int(i) for i in re.findall(r'fileID: (\d+)',arr.group(1))]
assert len(ids)==11,len(ids)
phrase='ฝากหาให้พี่ด้วยนะเข้ม';old=text(blocks[ids[9]][1]);split=old.index(phrase);prefix=old[:split].rstrip()
view=re.search(r'^  tonDialogue:\n((?:    .*\n)+)',ui,re.M).group(1)
canvasid=fid(view,'canvas');rootgo=fid(blocks[canvasid][1],'m_GameObject')
rectbygo={fid(b,'m_GameObject'):i for i,(k,b) in blocks.items() if k==224};rootrect=rectbygo[rootgo];parent=fid(blocks[rootrect][1],'m_Father')
selectedgo={rootgo};changed=True
while changed:
 changed=False
 selectedrect={rectbygo[g] for g in selectedgo}
 for i,(k,b) in blocks.items():
  if k==224 and fid(b,'m_Father') in selectedrect:
   g=fid(b,'m_GameObject')
   if g not in selectedgo:selectedgo.add(g);changed=True
cloneids={i for i,(k,b) in blocks.items() if i in selectedgo or (k!=1 and 'm_GameObject:' in b and fid(b,'m_GameObject') in selectedgo)}
assert len(selectedgo)==4,selectedgo
mapping={};n=700300000
for i in sorted(cloneids):
 while n in blocks:n+=1
 mapping[i]=n;n+=1
sentenceid=fid(view,'sentence');speakerid=fid(view,'speaker');motionid=fid(view,'motionRoot')
parts=[]
for i in sorted(cloneids):
 k,b=blocks[i]
 b=re.sub(r'fileID: (-?\d+)',lambda m:'fileID: '+str(mapping.get(int(m.group(1)),int(m.group(1)))),b)
 if i==rootgo:
  b=re.sub(r'^  m_Name: .*','  m_Name: TonRequestDialogueCanvas',b,flags=re.M)
  b=re.sub(r'^  m_IsActive: .*','  m_IsActive: 0',b,flags=re.M)
 if i==sentenceid:b=re.sub(r'^  m_text: .*?(?=\n  m_\w+:)',lambda m:'  m_text: '+json.dumps(phrase,ensure_ascii=True),b,flags=re.M|re.S)
 parts.append('--- !u!'+str(k)+' &'+str(mapping[i])+'\n'+b)
newids=ids[:10]+[mapping[sentenceid]]+ids[10:]
newui=ui[:arr.start(1)]+''.join('  - {fileID: '+str(i)+'}\n' for i in newids)+ui[arr.end(1):]
newui=re.sub(r'^  tonRequestDialogue:\n(?:    .*\n)+','',newui,flags=re.M)
newui=re.sub(r'^  tonRequestSentence: .*\n','',newui,flags=re.M)
newview='  tonRequestDialogue:\n    canvas: {fileID: '+str(mapping[canvasid])+'}\n    motionRoot: {fileID: '+str(mapping[motionid])+'}\n    speaker: {fileID: '+str(mapping[speakerid])+'}\n    sentence: {fileID: '+str(mapping[sentenceid])+'}\n    normalizeFontBaseline: 1\n    layoutRevision: 2\n  tonRequestSentence: {fileID: '+str(mapping[sentenceid])+'}\n'
newui=newui.replace('  presentation:',newview+'  presentation:',1)
s=s.replace(ui,newui,1)
oldb=blocks[ids[9]][1];newb=re.sub(r'^  m_text: .*?(?=\n  m_\w+:)',lambda m:'  m_text: '+json.dumps(prefix,ensure_ascii=True),oldb,flags=re.M|re.S);s=s.replace(oldb,newb,1)
pb=blocks[parent][1];npb=pb.replace('  m_Father:','  - {fileID: '+str(mapping[rootrect])+'}\n  m_Father:',1);s=s.replace(pb,npb,1)
s=s.rstrip()+'\n'+''.join(parts)
assert p.read_bytes()==before,'Scene changed during patch; retry.'
shutil.copy2(p,'Tools/DialogueAudit/LawyerRoom-before-split-ton-request-disk.unity.backup')
p.write_text(s,encoding='utf-8')
print('Saved independent TonRequestDialogueCanvas; first call now has 12 sentences, keeping the final Khem reply.')
