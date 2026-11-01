"""Give Evening footsteps a dedicated child for local audio effects."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
path = root / 'Assets/MainScenes/From_PM/MainScene_2_Evening.unity'
s = path.read_text(encoding='utf-8-sig')
backup = Path(__file__).parent / 'Evening-before-footstep-audio-child.unity.txt'
if not backup.exists(): backup.write_text(s, encoding='utf-8')
go_id, transform_id, filter_id = 731400003, 731400004, 731400005
assert not re.search(r'&73140000[345]\b', s)
prefab = re.search(r'--- !u!1001 &1444286042083808156\n.*?(?=--- !u!)', s, re.S).group()
component_entry = ('    - targetCorrespondingSourceObject: {fileID: 8919096523114816503, guid: 7c4383362bb47e94bae5dcaad61a5a4f, type: 3}\n'
                   '      insertIndex: -1\n      addedObject: {fileID: 731400002}\n')
assert component_entry in prefab
child_entry = ('    - targetCorrespondingSourceObject: {fileID: 8110190888927263053, guid: 7c4383362bb47e94bae5dcaad61a5a4f, type: 3}\n'
               f'      insertIndex: -1\n      addedObject: {{fileID: {transform_id}}}\n')
new_prefab = prefab.replace(component_entry, '').replace('    m_AddedComponents:', child_entry+'    m_AddedComponents:')
s = s.replace(prefab, new_prefab)
audio = re.search(r'--- !u!82 &731400002\n.*?(?=--- !u!)', s, re.S).group()
assert 'm_GameObject: {fileID: 1987102022}' in audio
s = s.replace(audio, audio.replace('m_GameObject: {fileID: 1987102022}', f'm_GameObject: {{fileID: {go_id}}}'))
lawyer = (root/'Assets/MainScenes/From_PM/Lawyer Room.unity').read_text(encoding='utf-8-sig')
reverb = re.search(r'--- !u!164 &483891627\n.*?(?=--- !u!)', lawyer, re.S).group()
reverb = reverb.replace('&483891627', f'&{filter_id}').replace('m_GameObject: {fileID: 483891624}', f'm_GameObject: {{fileID: {go_id}}}')
# Ready to enable in Inspector; preserve the outdoor sound until configured.
reverb = reverb.replace('m_Enabled: 1', 'm_Enabled: 0')
child = f'''--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {transform_id}}}
  - component: {{fileID: 731400002}}
  - component: {{fileID: {filter_id}}}
  m_Layer: 0
  m_Name: FootstepAudio
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{transform_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 1987102023}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
'''
s = s.replace('--- !u!1660057539', child+reverb+'--- !u!1660057539')
ids = re.findall(r'^--- !u!\d+ &(\d+)', s, re.M)
assert len(ids) == len(set(ids))
assert s.count('audioSource: {fileID: 731400002}') == 1
assert s.count('addedObject: {fileID: 731400002}') == 0
assert s.count(f'addedObject: {{fileID: {transform_id}}}') == 1
path.write_text(s, encoding='utf-8')
print('FootstepAudio child attached to PlayerCapsule; existing source, clips and volume preserved.')
print('Added disabled Audio Reverb Filter with the Lawyer Room Room preset for Inspector editing.')
