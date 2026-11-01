"""Save footstep audio and surface clip references on the Evening scene player."""
from pathlib import Path
import re
ROOT = Path(__file__).resolve().parents[2]
scene = ROOT / 'Assets/MainScenes/From_PM/MainScene_2_Evening.unity'
s = scene.read_text(encoding='utf-8-sig')
backup = Path(__file__).parent / 'Evening-before-surface-footsteps.unity.txt'
if not backup.exists(): backup.write_text(s, encoding='utf-8')
assert 'ccd6f8cd8bdc4ba197adac606a9c1565' not in s, 'Footsteps already installed; inspect before reinstalling'
component_id, audio_id = 731400001, 731400002
assert not re.search(r'&73140000[12]\b', s)
prefab = re.search(r'--- !u!1001 &1444286042083808156\n.*?(?=--- !u!)', s, re.S).group()
target = '8919096523114816503'
added = ''.join(f'    - targetCorrespondingSourceObject: {{fileID: {target}, guid: 7c4383362bb47e94bae5dcaad61a5a4f, type: 3}}\n      insertIndex: -1\n      addedObject: {{fileID: {i}}}\n' for i in (component_id, audio_id))
new_prefab = prefab.replace('  m_SourcePrefab:', added + '  m_SourcePrefab:')
assert new_prefab != prefab
s = s.replace(prefab, new_prefab)
lawyer = (ROOT / 'Assets/MainScenes/From_PM/Lawyer Room.unity').read_text(encoding='utf-8-sig')
audio = re.search(r'--- !u!82 &483891626\n.*?(?=--- !u!)', lawyer, re.S).group()
audio = audio.replace('&483891626', f'&{audio_id}')
audio = re.sub(r'm_GameObject: \{fileID: \d+\}', 'm_GameObject: {fileID: 1987102022}', audio)
audio = re.sub(r'm_PlayOnAwake: \d+', 'm_PlayOnAwake: 0', audio)
audio = re.sub(r'm_Volume: [0-9.]+', 'm_Volume: 1', audio)
audio = re.sub(r'm_audioClip: .*', 'm_audioClip: {fileID: 0}', audio)
def clips(folder, prefix, count):
    refs = []
    for i in range(1, count+1):
        meta = ROOT / f'Assets/SecretsThatBreathe/Audio/Footsteps/{folder}/{prefix}_{i:02d}.wav.meta'
        guid = re.search(r'guid: (\w+)', meta.read_text()).group(1)
        refs.append(f'  - {{fileID: 8300000, guid: {guid}, type: 3}}\n')
    return ''.join(refs)
road = clips('Surfaces/Concrete', 'Concrete_Footstep', 12)
terrain = clips('Surfaces/Glass', 'Glass_Footstep', 16)
component = f'''--- !u!114 &{component_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1987102022}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: ccd6f8cd8bdc4ba197adac606a9c1565, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::SecretsThatBreathe.PlayerFootsteps
  audioSource: {{fileID: {audio_id}}}
  footstepClips:
{road}  roadClips:
{road}  terrainClips:
{terrain}  surfaceLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  concreteClipsRevision: 1
  cadenceSettingsRevision: 1
  volume: 0.4
  walkStepDistance: 1.2
  sprintStepDistance: 0.9
  firstStepDistance: 0.2
  pitchRange: {{x: 0.95, y: 1.05}}
'''
# New components are attached to the existing stripped player GameObject via prefab overrides.
marker = '--- !u!1660057539'
assert marker in s
s = s.replace(marker, audio + component + marker)
ids = re.findall(r'^--- !u!\d+ &(\d+)', s, re.M)
assert len(ids) == len(set(ids))
assert len(re.findall(r'guid: ccd6f8cd8bdc4ba197adac606a9c1565', s)) == 1
scene.write_text(s, encoding='utf-8')
print('Saved Evening player footsteps: 12 road clips, 16 terrain clips, dedicated AudioSource.')
