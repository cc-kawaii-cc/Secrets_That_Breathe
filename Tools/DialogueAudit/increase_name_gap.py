from pathlib import Path
import re,json,shutil
p=Path('Assets/Tonpalm/Lawyer Room.unity')
s=p.read_text(encoding='utf-8')
blocks={int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
views=list(re.finditer(r'^  (?:khemDialogue|tonDialogue|visitorDialogue):\n    canvas: \{fileID: (\d+)\}\n    motionRoot: \{fileID: \d+\}\n    speaker: \{fileID: (\d+)\}\n    sentence: \{fileID: \d+\}',s,re.M))
assert len(views)==4
changes=[]
for v in views:
 tmp=blocks[int(v.group(2))][1]
 go=int(re.search(r'm_GameObject: \{fileID: (\d+)',tmp).group(1))
 rid=next(i for i,(k,b) in blocks.items() if k==224 and 'm_GameObject: {fileID: '+str(go)+'}' in b)
 rect=blocks[rid][1]
 old=float(re.search(r'm_AnchoredPosition: \{x: ([^,]+)',rect).group(1))
 size=float(re.search(r'^  m_fontSize: ([\d.]+)',tmp,re.M).group(1))
 # The existing Kanit gap is 2 spaces (13.2 units at font size 30).
 extra=size*.44
 new=round(old-extra,6)
 changes.append({'rect':rid,'beforeX':old,'afterX':new,'extraGap':extra})
shutil.copy2(p,'Tools/DialogueAudit/LawyerRoom-before-extra-name-gap.unity.backup')
for c in changes:
 pattern=r'(^--- !u!224 &'+str(c['rect'])+r'\n.*?)(?=^--- !u!|\Z)'
 s,n=re.subn(pattern,lambda m:re.sub(r'(m_AnchoredPosition: \{x: )[^,]+',lambda _:_.group(1)+str(c['afterX']),m.group(1)),s,flags=re.M|re.S)
 assert n==1
updated={int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
changed={i for i in blocks if blocks[i]!=updated[i]}
assert changed=={c['rect'] for c in changes},changed
for c in changes:
 old=blocks[c['rect']][1];new=updated[c['rect']][1]
 assert re.sub(r'm_AnchoredPosition: \{x: [^,]+','m_AnchoredPosition: {x: SAME',old)==re.sub(r'm_AnchoredPosition: \{x: [^,]+','m_AnchoredPosition: {x: SAME',new)
p.write_text(s,encoding='utf-8')
Path('Tools/DialogueAudit/extra-name-gap.json').write_text(json.dumps({'result':'PASS','changedSpeakerRects':changes,'otherSceneObjectsUnchanged':True},indent=2),encoding='utf-8')
print(json.dumps(changes))
