from pathlib import Path
import re,json,collections
files={'before_delete':'Tools/DialogueAudit/LawyerRoom-before-unused-text-cleanup-disk.unity.backup','after_delete':'Tools/DialogueAudit/LawyerRoom-before-undo-cleanup.unity.backup','before_colour_rollback':'Tools/DialogueAudit/LawyerRoom-before-rollback-to-C0C000-20261027-021214.unity.backup','now':'Assets/Tonpalm/Lawyer Room.unity'}
def parse(file):
 s=Path(file).read_text(encoding='utf-8')
 return {int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
def prop(b,k):
 m=re.search(r'^  '+re.escape(k)+r': (.*)',b,re.M);return m.group(1) if m else None
def fid(b,k):
 m=re.search(re.escape(k)+r': \{fileID: (-?\d+)',b);return int(m.group(1)) if m else 0
def desc(d,i):
 k,b=d[i];g=fid(b,'m_GameObject');return prop(d[g][1],'m_Name') if g in d else str(i)
states={k:parse(v) for k,v in files.items()}
for an,bn in [('before_delete','after_delete'),('after_delete','before_colour_rollback'),('before_colour_rollback','now'),('before_delete','now')]:
 a,b=states[an],states[bn];print('\nCOMPARE',an,'->',bn)
 print('New types',dict(collections.Counter(b[i][0] for i in b.keys()-a.keys())),'Removed types',dict(collections.Counter(a[i][0] for i in a.keys()-b.keys())))
 lights=[];rects=[]
 for i in a.keys()&b.keys():
  k,x=a[i];_,y=b[i]
  if x==y:continue
  if k==108:
   fields={f:[prop(x,f),prop(y,f)] for f in ['m_Enabled','m_Intensity','m_Range','m_Lightmapping','m_Type','m_Color'] if prop(x,f)!=prop(y,f)}
   if fields:lights.append({'name':desc(b,i),'id':i,'fields':fields})
  if k==224:
   fields={f:[prop(x,f),prop(y,f)] for f in ['m_AnchoredPosition','m_AnchorMin','m_AnchorMax','m_SizeDelta','m_LocalScale'] if prop(x,f)!=prop(y,f)}
   if fields:rects.append({'name':desc(b,i),'id':i,'fields':fields})
 print('LIGHTS',json.dumps(lights,ensure_ascii=False))
 print('RECT CHANGES',len(rects),json.dumps(rects[:18],ensure_ascii=False))
 added=[prop(b[i][1],'m_Name') for i in b.keys()-a.keys() if b[i][0]==1]
 print('NEW GO NAMES',json.dumps(added[:35],ensure_ascii=False))
 if an=='before_delete' and bn=='now':print('EXACT SAME BLOCKS',a==b)
Path('Tools/DialogueAudit/lighting-and-layout-cause-audit.json').write_text(json.dumps({'files':files,'currentMatchesBeforeDelete':states['before_delete']==states['now']},indent=2),encoding='utf-8')
