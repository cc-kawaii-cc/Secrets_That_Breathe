from pathlib import Path
import re,json
s=Path('Tools/DialogueAudit/verify_authored_canvases.py').read_text(encoding='utf-8-sig')
exec(s.split('report=[]')[0])
for i,(k,b) in blocks.items():
 if k!=114 or '  m_text:' not in b:continue
 raw=re.search(r'^  m_text: (.*?)(?=\n  m_\w+:)',b,re.M|re.S)
 if not raw:continue
 text=re.sub(r'\\u([0-9a-fA-F]{4})',lambda m:chr(int(m.group(1),16)),raw.group(1))
 color=prop(b,'m_fontColor') or ''
 vals=[float(x) for x in re.findall(r'[rgba]: ([\d.eE+-]+)',color)]
 quoted='\\"' in text
 yellow=len(vals)==4 and vals[0]>.6 and vals[1]>.5 and vals[2]<.6
 if not quoted and not yellow:continue
 go=fid(b,'m_GameObject');rect=rects[go][1];pr=blocks[parents[rects[go][0]]][1]
 print(json.dumps({'id':i,'path':path(go),'text':text,'color':color,'fontSize':prop(b,'m_fontSize'),'font':prop(b,'m_fontAsset'),'labelPosition':prop(rect,'m_AnchoredPosition'),'parentAnchor':prop(pr,'m_AnchorMin'),'parentPosition':prop(pr,'m_AnchoredPosition'),'parentSize':prop(pr,'m_SizeDelta'),'yellow':yellow},ensure_ascii=False))
