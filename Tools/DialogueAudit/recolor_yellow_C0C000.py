from pathlib import Path
import re,json,shutil
p=Path('Assets/Tonpalm/Lawyer Room.unity');before=p.read_bytes();s=before.decode('utf-8').replace('\r\n','\n')
pattern=r'^--- !u!114 &(-?\d+)\n(.*?)(?=^--- !u!|\Z)'
changed=[]
colour='{r: 0.7529412, g: 0.7529412, b: 0, a: 1}'
rgba=192 | (192<<8) | (255<<24)
def recolor(m):
 b=m.group(2)
 if '  m_text:' not in b or not re.search(r'^  m_fontColor: \{r: 1, g: 1, b: 0, a: 1\}',b,re.M):return m.group()
 changed.append(int(m.group(1)))
 b=re.sub(r'^  (m_Color|m_fontColor): .*',lambda c:'  '+c.group(1)+': '+colour,b,flags=re.M)
 b=re.sub(r'(m_fontColor32:\n    serializedVersion: \d+\n    rgba:) \d+',lambda c:c.group(1)+' '+str(rgba),b)
 return '--- !u!114 &'+m.group(1)+'\n'+b
s=re.sub(pattern,recolor,s,flags=re.M|re.S)
assert changed
assert p.read_bytes()==before
shutil.copy2(p,'Tools/DialogueAudit/LawyerRoom-before-C0C000-text.unity.backup')
p.write_text(s,encoding='utf-8')
for i in changed:
 b=re.search(r'^--- !u!114 &'+str(i)+r'\n.*?(?=^--- !u!|\Z)',s,re.M|re.S).group()
 assert 'm_fontColor: '+colour in b
 assert 'rgba: '+str(rgba) in b
Path('Tools/DialogueAudit/yellow-C0C000-validation.json').write_text(json.dumps({'result':'PASS','color':'#C0C000','changedTextComponents':changed,'count':len(changed),'scope':'Lawyer Room scene; TMP vertex and serialized Color32 values'},indent=2),encoding='utf-8')
print('Updated',len(changed),'yellow TMP texts to #C0C000; all other styles and positions unchanged.')
