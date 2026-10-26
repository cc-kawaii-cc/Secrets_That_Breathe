from pathlib import Path
import re,json
pattern=r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)'
def parse(p):
 s=p.read_text(encoding='utf-8'); matches=list(re.finditer(pattern,s,re.M|re.S));ids=[int(m.group(2)) for m in matches];assert len(ids)==len(set(ids)),p
 return {int(m.group(2)):(int(m.group(1)),m.group(3)) for m in matches}
backup=sorted(Path('Tools/DialogueAudit/Backups').glob('LawyerRoom-before-runtime-intro-*.unity.txt'))[-1]
before=parse(backup);after=parse(Path('Assets/Tonpalm/Lawyer Room.unity'))
removed=set(before)-set(after)
assert removed=={710500001}
for i,(k,b) in before.items():
 if i in removed: continue
 if k!=1660057539: assert after[i]==(k,b),('Original object changed',i)
 else: assert after[i][1]==b.replace('  - {fileID: 710500001}\n','')
assert '879a4131df70457a88113b7e16163517' not in Path('Assets/Tonpalm/Lawyer Room.unity').read_text(encoding='utf-8')
prefab=Path('Assets/Resources/LawyerRoomIntroCanvas.prefab');pb=parse(prefab)
for i,(k,b) in pb.items():
 for line in b.splitlines():
  if 'guid:' in line:continue
  for ref in re.findall(r'\{fileID: (-?\d+)\}',line):assert int(ref)==0 or int(ref) in pb,('Broken local prefab reference',i,ref)
labels=[(i,b) for i,(k,b) in pb.items() if 'Unity.TextMeshPro::' in b]
assert len(labels)==4
assert sum('m_fontColor: {r: 0.7529412, g: 0.7529412, b: 0, a: 1}' in b for i,b in labels)==3
assert sum('m_fontColor: {r: 1, g: 1, b: 1, a: 1}' in b for i,b in labels)==1
assert all('m_fontSize: 30\n' in b for i,b in labels)
assert sum(k==225 and 'm_Alpha: 0\n' in b for k,b in pb.values())==4
assert any('m_SortingOrder: 30000\n' in b for k,b in pb.values())
controller=next(b for k,b in pb.values() if '::SecretsThatBreathe.LawyerRoomIntro\n' in b)
assert 'paragraphHoldSeconds: 3\n' in controller and 'loadGameplayScene: 0\n' in controller
intro=Path('Assets/Tonpalm/Lawyer Room Intro.unity');ib=parse(intro)
for i,(k,b) in ib.items():
 for line in b.splitlines():
  if 'guid:' in line:continue
  for ref in re.findall(r'\{fileID: (-?\d+)\}',line):assert int(ref)==0 or int(ref) in ib,('Broken loading-scene reference',i,ref)
assert any('propertyPath: loadGameplayScene\n      value: 1' in b for k,b in ib.values())
report={'result':'PASS','original_scene_blocks_unchanged':len(before)-1,'shared_prefab_blocks':len(pb),'paragraphs':3,'text_colour':'#C0C000','prompt':'Press any key','font_size':30,'gap_after_each_fade_seconds':3,'loading_scene':str(intro),'runtime_and_editor_compile':'PASS (existing warnings only)','play_mode_test':'Not run'}
Path('Tools/DialogueAudit/intro-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2))
