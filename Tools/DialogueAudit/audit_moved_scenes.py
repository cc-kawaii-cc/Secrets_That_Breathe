from pathlib import Path
import re,json,collections,hashlib
scene_paths=[Path('Assets/MainScenes/From_PM/Lawyer Room.unity'),Path('Assets/MainScenes/From_PM/MainScene_2_Evening.unity')]
initial_hashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in scene_paths}
texts={str(p):p.read_text(encoding='utf-8') for p in scene_paths}
requested=set(g for s in texts.values() for g in re.findall(r'guid: ([0-9a-f]{32})',s) if set(g)!={'0'})
index=collections.defaultdict(list)
meta_count=0
for root in [Path('Assets'),Path('Library/PackageCache')]:
 for p in root.rglob('*.meta'):
  try:
   with p.open(encoding='utf-8-sig',errors='replace') as f:head=''.join(f.readline() for _ in range(4))
   m=re.search(r'^guid: ([0-9a-f]{32})',head,re.M)
   if m and m.group(1) in requested:index[m.group(1)].append(str(p)[:-5])
   meta_count+=1
  except OSError:pass
report={'checked_meta_files':meta_count,'unique_external_scene_guids':len(requested),'missing_external_assets':{},'duplicate_referenced_guids':{g:ps for g,ps in index.items() if len(ps)>1},'scenes':{},'scene_files_changed':False}
pattern=r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)'
for path,s in texts.items():
 bs={int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(pattern,s,re.M|re.S)}
 missinglocals=collections.defaultdict(list)
 for i,(k,b) in bs.items():
  for line in b.splitlines():
   if 'guid:' in line:continue
   for ref in re.findall(r'\{fileID: (-?\d+)\}',line):
    if int(ref)!=0 and int(ref) not in bs:missinglocals[ref].append(i)
 guids=set(re.findall(r'guid: ([0-9a-f]{32})',s))
 missing=sorted(g for g in guids if set(g)!={'0'} and re.fullmatch(r'0000000000000000[def]000000000000000',g) is None and g not in index)
 report['missing_external_assets'][path]=missing
 scripts=[]
 for i,(k,b) in bs.items():
  if k!=114:continue
  script=re.search(r'm_Script: \{fileID: (-?\d+), guid: (\w+)',b)
  if script and (int(script.group(1))==0 or script.group(2) not in index):scripts.append({'component':i,'guid':script.group(2)})
 lightmapguids=[]
 for i,(k,b) in bs.items():
  if k==157:lightmapguids=[{'guid':g,'asset':index.get(g,[])} for g in re.findall(r'guid: ([0-9a-f]{32})',b) if set(g)!={'0'}]
 report['scenes'][path]={'blocks':len(bs),'undefined_local_references':dict(missinglocals),'missing_script_references':scripts,'lighting_assets':lightmapguids}
 assert hashlib.sha256(Path(path).read_bytes()).hexdigest()==initial_hashes[path]
Path('Tools/DialogueAudit/moved-scenes-dependency-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
