from pathlib import Path
import json,re
exec(Path('Tools/DialogueAudit/verify_authored_canvases.py').read_text(encoding='utf-8-sig').split('report=[]')[0])
print('Disabled TMP labels:')
for i,(k,b) in blocks.items():
 if k==114 and '  m_text:' in b and prop(b,'m_Enabled')=='0':
  go=fid(b,'m_GameObject')
  if go in rects:print(i,path(go))
print('Other text outside SeatingCanvas:')
for i,(k,b) in blocks.items():
 if k==114 and '  m_text:' in b:
  go=fid(b,'m_GameObject')
  if go in rects and not path(go).startswith('SeatingCanvas/'):print(path(go))
print('Phone dialogue arrays:')
guid=re.search(r'guid: (\w+)',Path('Assets/SecretsThatBreathe/Scripts/PhoneCallDialogueUI.cs.meta').read_text()).group(1)
b=next(b for _,(k,b) in blocks.items() if k==114 and 'guid: '+guid+',' in b)
print(re.search(r'^  sentences:.*?(?=^  ringThought:)',b,re.M|re.S).group())
