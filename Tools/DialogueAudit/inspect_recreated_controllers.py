from pathlib import Path
import re,json
s=Path('Tools/DialogueAudit/investigate_lighting_layout.py').read_text(encoding='utf-8-sig');exec(s.split('for an,bn in')[0])
a=states['before_delete'];b=states['after_delete']
known={}
for p in Path('Assets/SecretsThatBreathe/Scripts').rglob('*.cs.meta'):
 g=re.search(r'guid: (\w+)',p.read_text(encoding='utf-8'))
 if g:known[g.group(1)]=p.stem
print('Old controller components no longer present:')
for i in a.keys()-b.keys():
 k,body=a[i]
 if k!=114:continue
 g=fid(body,'m_GameObject')
 if g not in b:continue
 m=re.search(r'm_Script: \{fileID: \d+, guid: (\w+)',body)
 if m:print(desc(a,i),known.get(m.group(1),m.group(1)))
print('New lights:')
for i in b.keys()-a.keys():
 if b[i][0]==108:print(desc(b,i),{key:prop(b[i][1],key) for key in ['m_Intensity','m_Enabled','m_Range']})
