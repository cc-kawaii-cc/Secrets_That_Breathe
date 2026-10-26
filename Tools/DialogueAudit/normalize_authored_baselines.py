from pathlib import Path
import re,shutil
p=Path('Assets/Tonpalm/Lawyer Room.unity')
s=p.read_text(encoding='utf-8')
blocks={int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
views=list(re.finditer(r'^  (?:khemDialogue|tonDialogue|visitorDialogue):\n    canvas: \{fileID: \d+\}\n    motionRoot: \{fileID: (\d+)\}\n    speaker: \{fileID: (\d+)\}\n    sentence: \{fileID: (\d+)\}(?:\n    layoutRevision: \d+)?',s,re.M))
assert len(views)==4
rect_ids=[]
for v in views:
 for tmpid in [int(v.group(2)),int(v.group(3))]:
  tmp=blocks[tmpid][1]
  go=int(re.search(r'm_GameObject: \{fileID: (\d+)',tmp).group(1))
  rid=next(i for i,(k,b) in blocks.items() if k==224 and re.search(r'm_GameObject: \{fileID: '+str(go)+r'\}',b))
  rect_ids.append(rid)
shutil.copy2(p,'Tools/DialogueAudit/LawyerRoom-before-baseline-normalization.unity.backup')
for rid in rect_ids:
 pat=r'(^--- !u!224 &'+str(rid)+r'\n.*?)(?=^--- !u!|\Z)'
 s=re.sub(pat,lambda m:re.sub(r'(m_AnchoredPosition: \{x: [^,]+, y: )[^}]+',r'\g<1>33',m.group(1)),s,flags=re.M|re.S)
for v in views:
 tid=int(v.group(3))
 pat=r'(^--- !u!114 &'+str(tid)+r'\n.*?)(?=^--- !u!|\Z)'
 def white(m):
  b=re.sub(r'^  (m_Color|m_fontColor): .*',r'  \1: {r: 1, g: 1, b: 1, a: 1}',m.group(1),flags=re.M)
  b=re.sub(r'(m_fontColor32:\n    serializedVersion: \d+\n    rgba:) \d+',r'\1 4294967295',b)
  return b
 s=re.sub(pat,white,s,flags=re.M|re.S)
s=re.sub(r'(^  (?:khemDialogue|tonDialogue|visitorDialogue):\n    canvas: \{fileID: \d+\}\n    motionRoot: \{fileID: \d+\}\n    speaker: \{fileID: \d+\}\n    sentence: \{fileID: \d+\})(?:\n    layoutRevision: \d+)?',r'\1\n    layoutRevision: 2',s,flags=re.M)
p.write_text(s,encoding='utf-8')
print('Saved 4 authored Canvas views with a common text origin and font baseline.')
