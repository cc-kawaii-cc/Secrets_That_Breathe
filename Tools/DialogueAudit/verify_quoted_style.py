from pathlib import Path
import re,json
exec(Path('Tools/DialogueAudit/verify_authored_canvases.py').read_text(encoding='utf-8-sig').split('report=[]')[0])
items=[]
for i,(k,b) in blocks.items():
 if k!=114 or '  m_text:' not in b: continue
 match=re.search(r'^  m_text: (.*?)(?=\n  m_\w+:)',b,re.M|re.S)
 if not match or '\\"' not in match.group(1):continue
 go=fid(b,'m_GameObject');rect=rects[go][1];parent=blocks[parents[rects[go][0]]][1]
 items.append({'path':path(go),'fontSize':prop(b,'m_fontSize'),'color':prop(b,'m_fontColor'),'font':prop(b,'m_fontAsset'), 'anchor':prop(parent,'m_AnchorMin'),'position':prop(parent,'m_AnchoredPosition'),'size':prop(parent,'m_SizeDelta'),'labelPosition':prop(rect,'m_AnchoredPosition')})
assert len(items)>=6, len(items)
reference=next(i for i in items if '/ReturnToDeskMessage/' in i['path'])
for item in items:
 for key in ['fontSize','color','font','anchor','position','size','labelPosition']:
  if key=='color' and any(part in item['path'] for part in ['/05_ThoughtFirst/', '/06_ThoughtSecond/', '/07_ThoughtThird/', '/QuotedThoughtCanvas/']):
   assert item[key]=='{r: 1, g: 1, b: 1, a: 1}',(item['path'],item[key])
   continue
  assert item[key]==reference[key],(item['path'],key,item[key],reference[key])
assert any('/QuotedThoughtCanvas/' in i['path'] for i in items),'Missing saved yellow thought Canvas'
assert reference['fontSize']=='25'
assert reference['color']=='{r: 0.7529412, g: 0.7529412, b: 0, a: 1}'
Path('Tools/DialogueAudit/quoted-style-validation.json').write_text(json.dumps({'result':'PASS','items':items},ensure_ascii=False,indent=2),encoding='utf-8')
print('PASS:',len(items),'quoted source/display texts retain size 25 and bottom position; objectives are yellow and money thoughts are white.')
