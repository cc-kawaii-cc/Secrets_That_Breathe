from pathlib import Path
import re,json
exec(Path('Tools/DialogueAudit/verify_authored_canvases.py').read_text(encoding='utf-8-sig').split('report=[]')[0])
mono=[(i,b) for i,(k,b) in blocks.items() if k==114]
items=[]
for i,b in mono:
 if '  m_text:' not in b:continue
 go=fid(b,'m_GameObject')
 if go not in rects:continue
 p=path(go)
 if not p.startswith('SeatingCanvas/'):continue
 m=re.search(r'^  m_text: (.*?)(?=\n  m_\w+:)',b,re.M|re.S)
 raw=m.group(1) if m else ''
 try:t=json.loads(re.sub(r'\n +',' ',raw)) if raw.startswith('"') else raw.strip()
 except:t=raw
 parentid=parents[rects[go][0]];pg=rectgo.get(parentid,0)
 ids={i,go,pg};refs=[]
 for j,other in mono:
  if j==i or fid(other,'m_GameObject') in {go,pg}:continue
  for field,target in re.findall(r'^  ([\w]+): \{fileID: (-?\d+)\}',other,re.M):
   if int(target) in ids:refs.append({'owner':j,'field':field,'target':int(target)})
  # Arrays such as PhoneCallDialogueUI.sentences.
  if re.search(r'^  - \{fileID: '+str(i)+r'\}',other,re.M):refs.append({'owner':j,'field':'array','target':i})
 # Nested view references are not top-level fields.
 for j,other in mono:
  if j!=i and re.search(r'^    (speaker|sentence): \{fileID: '+str(i)+r'\}',other,re.M):refs.append({'owner':j,'field':'dialogue view','target':i})
 items.append({'id':i,'path':p,'text':t,'refs':refs})
Path('Tools/DialogueAudit/text-usage-inventory.json').write_text(json.dumps(items,ensure_ascii=False,indent=2),encoding='utf-8')
print('TOTAL TEXTS',len(items))
for item in items:
 if not item['refs'] or '/Conversation/' in item['path'] or '/Lines/' in item['path']:
  print(json.dumps(item,ensure_ascii=False))
