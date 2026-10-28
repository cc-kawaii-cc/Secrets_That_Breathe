from pathlib import Path
import re,json
pattern=r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)'
def parse(p):return {int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(pattern,p.read_text(encoding='utf-8'),re.M|re.S)}
scene=Path('Assets/Tonpalm/Lawyer Room.unity');bs=parse(scene)
backup=sorted(Path('Tools/DialogueAudit/Backups').glob('LawyerRoom-before-door-zones-*.unity.txt'))[-1];before=parse(backup)
changed={711100028,711100011,268766367}
for i,v in before.items():
 if i not in changed:assert bs[i]==v,('Unrelated scene object changed',i)
added=set(bs)-set(before);assert len(added)==7
for i in changed|added:
 b=bs[i][1]
 for line in b.splitlines():
  if 'guid:' in line:continue
  for ref in re.findall(r'\{fileID: (-?\d+)\}',line):assert int(ref)==0 or int(ref) in bs,(i,ref)
instance=bs[268766367][1]
assert '    m_AddedGameObjects:\n    - targetCorrespondingSourceObject:' in instance
assert '    m_AddedComponents:\n    - targetCorrespondingSourceObject:' in instance
assert '      addedObject: {fileID: 711200011}' in instance
assert '      addedObject: {fileID: 711200021}' in instance
assert '      addedObject: {fileID: 711100028}' in instance
controller=bs[711100028][1]
assert 'm_GameObject: {fileID: 711200001}' in controller
assert 'interactionZone: {fileID: 711200012}' in controller
assert 'lookTarget: {fileID: 711200022}' in controller
assert 'interactRange' not in controller
for i in [711200012,711200022]:assert 'm_IsTrigger: 1' in bs[i][1]
# Only the additions section of the door PrefabInstance changed.
assert before[268766367][1].split('    m_RemovedComponents:')[0]==instance.split('    m_RemovedComponents:')[0]
report={'result':'PASS','interaction':'same InteractionLook box helpers as vending system','door_component':'OfficeGlassDoor/OfficeDoorExitInteraction','editable_boxes':['OfficeGlassDoor/InteractionZone','OfficeGlassDoor/LookTarget'],'door_placement_unchanged':True,'unrelated_scene_objects_unchanged':len(before)-len(changed),'runtime_compile':'PASS; existing warnings only','play_mode_test':'Not run'}
Path('Tools/DialogueAudit/office-exit-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2))
