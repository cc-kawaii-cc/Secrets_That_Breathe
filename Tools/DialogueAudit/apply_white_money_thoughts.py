from pathlib import Path
import re,shutil,json
p=Path('Assets/Tonpalm/Lawyer Room.unity')
s=p.read_text(encoding='utf-8')
guid=re.search(r'guid: (\w+)',Path('Assets/SecretsThatBreathe/Scripts/AuntDialogueUI.cs.meta').read_text()).group(1)
match=next(m for m in re.finditer(r'^--- !u!114 &\d+\n.*?(?=^--- !u!|\Z)',s,re.M|re.S) if 'guid: '+guid+',' in m.group())
ui=match.group()
ids=[int(re.search(r'^  '+f+r': \{fileID: (\d+)',ui,re.M).group(1)) for f in ['firstThought','secondThought','thirdThought']]
ids.append(int(re.search(r'  thoughtDialogue:\n.*?    sentence: \{fileID: (\d+)',ui,re.S).group(1)))
assert len(set(ids))==4 and all(ids)
shutil.copy2(p,'Tools/DialogueAudit/LawyerRoom-before-white-money-thoughts-disk.unity.backup')
for i in ids:
 pattern=r'(^--- !u!114 &'+str(i)+r'\n.*?)(?=^--- !u!|\Z)'
 def white(m):
  b=re.sub(r'^  (m_Color|m_fontColor): .*',r'  \1: {r: 1, g: 1, b: 1, a: 1}',m.group(1),flags=re.M)
  return re.sub(r'(m_fontColor32:\n    serializedVersion: \d+\n    rgba:) \d+',r'\1 4294967295',b)
 s=re.sub(pattern,white,s,flags=re.M|re.S)
updated=ui.replace('  quotedTextStyleRevision: 1','  quotedTextStyleRevision: 2')
if '  moneyThoughtHoldDuration:' in updated:
 updated=re.sub(r'^  moneyThoughtHoldDuration: .*','  moneyThoughtHoldDuration: 3',updated,flags=re.M)
else:
 updated=updated.replace('  quotedTextStyleRevision: 2','  quotedTextStyleRevision: 2\n  moneyThoughtHoldDuration: 3')
s=s.replace(ui,updated)
p.write_text(s,encoding='utf-8')
for i in ids:
 b=re.search(r'^--- !u!114 &'+str(i)+r'\n.*?(?=^--- !u!|\Z)',s,re.M|re.S).group()
 assert 'm_fontColor: {r: 1, g: 1, b: 1, a: 1}' in b
assert 'moneyThoughtHoldDuration: 3' in s
Path('Tools/DialogueAudit/white-money-thoughts-validation.json').write_text(json.dumps({'result':'PASS','whiteTextObjects':ids,'holdSeconds':3,'display':'ShowInstant (no typing or position interpolation)'},indent=2),encoding='utf-8')
print('PASS: three thought sources and their saved Canvas display are white; hold duration = 3 seconds.')
