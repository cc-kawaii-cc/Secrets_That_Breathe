from pathlib import Path
import re,json,shutil
p=Path('Assets/Tonpalm/Lawyer Room.unity');before=p.read_bytes();s=before.decode('utf-8').replace('\r\n','\n')
old=Path('Tools/DialogueAudit/LawyerRoom-before-unused-text-cleanup-disk.unity.backup').read_text(encoding='utf-8')
pattern=r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)'
def parse(text):return {int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(pattern,text,re.M|re.S)}
a=parse(old);b=parse(s)
def fid(body,key):return int(re.search(re.escape(key)+r': \{fileID: (-?\d+)',body).group(1))
def nm(body):
 m=re.search(r'^  m_Name: (.*)',body,re.M);return m.group(1) if m else ''
def guid(name):return re.search(r'guid: (\w+)',Path('Assets/SecretsThatBreathe/Scripts/'+name+'.cs.meta').read_text()).group(1)
gos={i:nm(body) for i,(k,body) in a.items() if k==1}
rects={fid(body,'m_GameObject'):i for i,(k,body) in a.items() if k==224}
rectgo={i:fid(body,'m_GameObject') for i,(k,body) in a.items() if k==224}
parents={i:fid(body,'m_Father') for i,(k,body) in a.items() if k==224}
def path(g):
 out=[gos[g]];r=rects[g]
 while parents.get(r):r=parents[r];out.append(gos[rectgo[r]])
 return '/'.join(reversed(out))
lookup={path(g):g for g in rects}
report=json.loads(Path('Tools/DialogueAudit/unused-text-cleanup-validation.json').read_text(encoding='utf-8'))
roots=[lookup[q] for q in report['removedRoots']]
restoredgo=set(roots);again=True
while again:
 again=False
 for g,r in rects.items():
  if parents[r] and rectgo.get(parents[r]) in restoredgo and g not in restoredgo:restoredgo.add(g);again=True
restore={i for i,(k,body) in a.items() if i in restoredgo or (k!=1 and 'm_GameObject:' in body and fid(body,'m_GameObject') in restoredgo)}
assert len(restoredgo)==18
assert not (restore & b.keys()),'Some deleted IDs already exist; inspect before merging.'
changed={}
# Restore only fields changed by the cleanup, retaining all later scene work.
phoneid=next(i for i,(k,body) in a.items() if k==114 and 'guid: '+guid('PhoneCallDialogueUI')+',' in body)
assert phoneid in b
phone=b[phoneid][1]
for field in ['conversationPanel','kemText','tonText']:
 value=fid(a[phoneid][1],field)
 phone=re.sub(r'^  '+field+r': .*','  '+field+': {fileID: '+str(value)+'}',phone,flags=re.M)
phone=re.sub(r'^  unusedTextCleanupRevision: .*\n','',phone,flags=re.M);changed[phoneid]=phone
itemcount=0
for i,(k,body) in a.items():
 if k!=114 or 'guid: '+guid('ItemInspectionUI')+',' not in body:continue
 held=fid(body,'heldPrompt')
 if held not in restore:continue
 assert i in b
 current=re.sub(r'^  heldPrompt: .*','  heldPrompt: {fileID: '+str(held)+'}',b[i][1],flags=re.M)
 current=re.sub(r'^  heldPanel: .*\n','',current,flags=re.M)
 changed[i]=current;itemcount+=1
assert itemcount==8
visitorid=next(i for i,(k,body) in a.items() if k==114 and 'guid: '+guid('AuntDialogueUI')+',' in body)
oldview=re.search(r'^  thoughtDialogue:\n((?:    .*\n)+)',a[visitorid][1],re.M).group(1)
speaker=fid(oldview,'speaker')
assert speaker in restore
changed[visitorid]=re.sub(r'(^  thoughtDialogue:\n.*?^    speaker: )\{fileID: -?\d+\}',lambda m:m.group(1)+'{fileID: '+str(speaker)+'}',b[visitorid][1],flags=re.M|re.S)
parentids={parents[rects[g]] for g in roots}
for parent in parentids:
 assert parent in b,('Original UI parent is missing',parent)
 oldchildren=re.search(r'^  m_Children:(.*?)(?=^  m_Father:)',a[parent][1],re.M|re.S).group(1)
 oldids=[int(x) for x in re.findall(r'fileID: (-?\d+)',oldchildren)]
 current=changed.get(parent,b[parent][1]);cm=re.search(r'^  m_Children:(.*?)(?=^  m_Father:)',current,re.M|re.S)
 children=[int(x) for x in re.findall(r'fileID: (-?\d+)',cm.group(1))]
 for idx,child in enumerate(oldids):
  if child not in restore or child in children:continue
  nxt=next((v for v in oldids[idx+1:] if v in children),None)
  if nxt is not None:children.insert(children.index(nxt),child)
  else:children.append(child)
 newlist='  m_Children:\n'+''.join('  - {fileID: '+str(v)+'}\n' for v in children)
 changed[parent]=current[:cm.start()]+newlist+current[cm.end():]
chunks=list(re.finditer(pattern,s,re.M|re.S))
out=s[:chunks[0].start()]+''.join('--- !u!'+m.group(1)+' &'+m.group(2)+'\n'+changed.get(int(m.group(2)),m.group(3)) for m in chunks)
out=out.rstrip()+'\n'+''.join('--- !u!'+str(a[i][0])+' &'+str(i)+'\n'+a[i][1] for i in a if i in restore)
after=parse(out)
assert all(after[i]==a[i] for i in restore),'Restored objects differ from backup'
assert all(after[i]==b[i] for i in b if i not in changed),'Unrelated scene block changed'
assert set(b).issubset(after),'Later scene objects were lost'
assert p.read_bytes()==before,'Scene changed while restoring; retry.'
shutil.copy2(p,'Tools/DialogueAudit/LawyerRoom-before-undo-cleanup.unity.backup')
p.write_text(out,encoding='utf-8')
Path('Tools/DialogueAudit/undo-unused-cleanup-validation.json').write_text(json.dumps({'result':'PASS','restoredGameObjects':len(restoredgo),'restoredTextComponents':14,'restoredBlocks':len(restore),'inspectionReferencesRestored':itemcount,'laterSceneBlocksPreserved':len(b)-len(changed),'automaticCleanupRemoved':True},indent=2),encoding='utf-8')
print('Restored 18 GameObjects / 14 TMP texts and their old references. Preserved all unrelated current scene objects.')
