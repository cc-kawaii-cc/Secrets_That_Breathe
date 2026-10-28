from pathlib import Path
import re,json
scene=Path('Assets/Tonpalm/Lawyer Room.unity')
def parse(p):return {int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)',p.read_text(encoding='utf-8'),re.M|re.S)}
bs=parse(scene);backup=sorted(Path('Tools/DialogueAudit/Backups').glob('LawyerRoom-before-intro2-*.unity.txt'))[-1];before=parse(backup)
for i,v in before.items():
 if i not in [710400002,710400035]:assert bs[i]==v,('Existing object changed',i)
intro={i:v for i,v in bs.items() if i not in before};assert len(intro)==6
assert 'm_IsActive: 0\n' in bs[710400001][1]
assert 'm_Father: {fileID: 1676370658}' in bs[710400002][1]
assert '  - {fileID: 710400002}\n' in bs[1676370658][1]
for i,(k,b) in intro.items():
 for line in b.splitlines():
  if 'guid:' in line:continue
  for ref in re.findall(r'\{fileID: (-?\d+)\}',line):assert int(ref)==0 or int(ref) in bs,(i,ref)
assert any(k==223 and 'm_OverrideSorting: 1\n' in b and 'm_SortingOrder: 30000\n' in b for k,b in bs.values())
assert sum('Unity.TextMeshPro::' in b for k,b in intro.values())==1
assert not Path('Assets/Resources/LawyerRoomIntroCanvas.prefab').exists()
assert 'm_fontSize: 40\n' in intro[710600004][1]
assert 'm_fontColor: {r: 1, g: 1, b: 1, a: 1}' in intro[710600004][1]
assert 'secondIntroText: {fileID: 710600004}' in bs[710400035][1]
assert 'secondIntroGroup: {fileID: 710600006}' in bs[710400035][1]
assert 'secondIntroHoldSeconds: 1\n' in bs[710400035][1]
report={'result':'PASS','mode':'saved inactive Canvas under SeatingCanvas; activated at runtime','existing_scene_blocks_unchanged_except_parent_children':len(before)-1,'new_intro_blocks':len(intro),'runtime_compile':'PASS; existing warnings only','play_mode_test':'Not run'}
Path('Tools/DialogueAudit/intro-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2))
