from pathlib import Path
import re,json,shutil
p=Path('Assets/Tonpalm/Lawyer Room.unity'); original=p.read_bytes(); s=original.decode('utf-8').replace('\r\n','\n')
chunks=list(re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S))
blocks={int(m.group(2)):(int(m.group(1)),m.group(3)) for m in chunks}
def fid(b,key): return int(re.search(re.escape(key)+r': \{fileID: (-?\d+)',b).group(1))
def name(b):
 m=re.search(r'^  m_Name: (.*)',b,re.M);return m.group(1) if m else ''
def guid(file):return re.search(r'guid: (\w+)',Path(file).read_text()).group(1)
seqid=next(i for i,(k,b) in blocks.items() if k==114 and 'guid: '+guid('Assets/SecretsThatBreathe/Scripts/DrawerSearchScareSequence.cs.meta')+',' in b)
seq=blocks[seqid][1]
if re.search(r'exitObjectiveText: \{fileID: [1-9]',seq):
 print('Exit objective already saved by Unity.');raise SystemExit(0)
search=next(b for _,(k,b) in blocks.items() if k==114 and 'guid: '+guid('Assets/SecretsThatBreathe/Scripts/DocumentSearchSequence.cs.meta')+',' in b)
tmpid=fid(search,'returnToDeskMessage');labelgo=fid(blocks[tmpid][1],'m_GameObject')
rect_by_go={fid(b,'m_GameObject'):i for i,(k,b) in blocks.items() if k==224}
parentrect=fid(blocks[rect_by_go[labelgo]][1],'m_Father'); parentgo=fid(blocks[parentrect][1],'m_GameObject')
mealgo=next(i for i,(k,b) in blocks.items() if k==1 and name(b)=='MealAndSearchUI');mealrect=rect_by_go[mealgo]
go_ids={parentgo,labelgo}
clone_ids={i for i,(k,b) in blocks.items() if i in go_ids or (k!=1 and 'm_GameObject:' in b and fid(b,'m_GameObject') in go_ids)}
assert tmpid in clone_ids and parentrect in clone_ids
mapping={};candidate=700100000
for i in sorted(clone_ids):
 while candidate in blocks:candidate+=1
 mapping[i]=candidate;candidate+=1
parts=[]
for old in sorted(clone_ids):
 k,b=blocks[old]
 b=re.sub(r'fileID: (-?\d+)',lambda m:'fileID: '+str(mapping.get(int(m.group(1)),int(m.group(1)))),b)
 if old==parentgo:
  b=re.sub(r'^  m_Name: .*','  m_Name: ExitObjective',b,flags=re.M)
  b=re.sub(r'^  m_IsActive: .*','  m_IsActive: 0',b,flags=re.M)
 if old==parentrect:b=re.sub(r'm_Father: \{fileID: -?\d+\}','m_Father: {fileID: '+str(mealrect)+'}',b)
 if old==tmpid:
  b=re.sub(r'^  m_text: .*?(?=\n  m_\w+:)',lambda m:'  m_text: '+json.dumps('"ไปที่ประตูทางออก"',ensure_ascii=True),b,flags=re.M|re.S)
 parts.append('--- !u!'+str(k)+' &'+str(mapping[old])+'\n'+b)
meal=blocks[mealrect][1]
newmeal=meal.replace('  m_Father:', '  - {fileID: '+str(mapping[parentrect])+'}\n  m_Father:',1)
assert newmeal!=meal
s=s.replace(meal,newmeal,1)
newseq=seq.replace('  auntDialogueText:', '  exitObjectiveText: {fileID: '+str(mapping[tmpid])+'}\n  exitObjectiveDelay: 3\n  auntDialogueText:',1)
assert newseq!=seq
s=s.replace(seq,newseq,1)
s=s.rstrip()+'\n'+''.join(parts)
assert p.read_bytes()==original,'Scene changed during patch; retry against current file.'
shutil.copy2(p,'Tools/DialogueAudit/LawyerRoom-before-exit-objective-disk.unity.backup')
p.write_text(s,encoding='utf-8')
print('Saved ExitObjective Canvas panel and linked it to DrawerSearchScareSequence. Delay=3s.')
