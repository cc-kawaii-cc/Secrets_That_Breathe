from pathlib import Path
import re,json
s=Path('Assets/Tonpalm/Lawyer Room.unity').read_text(encoding='utf-8')
blocks={int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
def prop(b,key):
 m=re.search(r'^  '+re.escape(key)+r': (.*)',b,re.M)
 return m.group(1) if m else None
def fid(b,key):
 return int(re.search(re.escape(key)+r': \{fileID: (-?\d+)',b).group(1))
gos={i:prop(b,'m_Name') for i,(k,b) in blocks.items() if k==1}
rects={fid(b,'m_GameObject'):(i,b) for i,(k,b) in blocks.items() if k==224}
parents={i:fid(b,'m_Father') for i,(k,b) in blocks.items() if k==224}
rectgo={i:fid(b,'m_GameObject') for i,(k,b) in blocks.items() if k==224}
def path(go):
 out=[gos[go]]; r=rects[go][0]
 while parents.get(r):
  r=parents[r];out.append(gos[rectgo[r]])
 return '/'.join(reversed(out))
report=[]
for go,name in gos.items():
 if name not in ['KhemDialogueCanvas','TonDialogueCanvas','UncleDialogueCanvas']: continue
 rootrect=rects[go][0]
 motions=[(i,b) for i,(k,b) in blocks.items() if k==224 and fid(b,'m_Father')==rootrect]
 assert len(motions)==1
 mr,mb=motions[0]
 labels={}
 for i,(k,b) in blocks.items():
  if k!=224 or fid(b,'m_Father')!=mr:continue
  lg=fid(b,'m_GameObject'); n=gos[lg]
  if n not in ['Sentence','Speaker']:continue
  tmp=next(tb for _,(tk,tb) in blocks.items() if tk==114 and 'Unity.TextMeshPro::' in tb and fid(tb,'m_GameObject')==lg)
  labels[n]={'position':prop(b,'m_AnchoredPosition'),'anchorMin':prop(b,'m_AnchorMin'),'anchorMax':prop(b,'m_AnchorMax'),'fontSize':prop(tmp,'m_fontSize'),'color':prop(tmp,'m_fontColor'),'font':prop(tmp,'m_fontAsset')}
 assert set(labels)=={'Sentence','Speaker'}
 report.append({'path':path(go),'anchorMin':prop(mb,'m_AnchorMin'),'anchorMax':prop(mb,'m_AnchorMax'),'labels':labels})
assert len(report)==4
bodies=[r['labels']['Sentence'] for r in report]
assert all(b['position']==bodies[0]['position'] for b in bodies),'White text positions differ'
assert all(r['anchorMin']==report[0]['anchorMin'] for r in report),'Screen anchors differ'
assert all(b['fontSize']=='30' for b in bodies)
assert all(b['color']=='{r: 1, g: 1, b: 1, a: 1}' for b in bodies)
Path('Tools/DialogueAudit/authored-canvas-validation.json').write_text(json.dumps({'result':'PASS','views':report},ensure_ascii=False,indent=2),encoding='utf-8')
print('PASS: 4 saved speaker Canvases; identical white text anchors and local position; original font size 30; white sentence colour.')
