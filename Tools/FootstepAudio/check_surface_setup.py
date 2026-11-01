from pathlib import Path
import re
root = Path(__file__).resolve().parents[2]
scene = root / 'Assets/MainScenes/From_PM/MainScene_2_Evening.unity'
s = scene.read_text(encoding='utf-8')
# The step component owns volume; keep the source at unity gain.
a = s.index('--- !u!82 &731400002')
b = s.index('--- !u!', a+5)
s = s[:a] + s[a:b].replace('m_Volume: 0.2', 'm_Volume: 1') + s[b:]
scene.write_text(s, encoding='utf-8')
component = re.search(r'--- !u!114 &731400001\n.*?(?=--- !u!)', s, re.S).group()
for field, count in [('roadClips',12), ('terrainClips',16)]:
    refs = re.search(r'  '+field+r':\n((?:  - .*\n)+)', component).group(1)
    assert refs.count('fileID: 8300000') == count
    for guid in re.findall(r'guid: (\w+)', refs):
        assert any(guid in p.read_text() for p in (root/'Assets/SecretsThatBreathe/Audio/Footsteps').rglob('*.wav.meta'))
assert s.count('addedObject: {fileID: 731400001}') == 1
assert s.count('addedObject: {fileID: 731400002}') == 1
rsp = (root/'Tools/FDRide/VillageAmbienceCompileCheck.rsp').read_text()
lines = [l for l in rsp.splitlines() if not l.startswith('"Assets/')]
lines[0] = '-out:"Tools/FootstepAudio/Footsteps.CompileCheck.dll"'
lines += ['"Assets/SecretsThatBreathe/Scripts/PlayerFootsteps.cs"',
          '"Assets/Nemo_Upgrade/Starter Assets/Runtime/FirstPersonController/Scripts/FirstPersonController.cs"']
(Path(__file__).parent/'FootstepsCompileCheck.rsp').write_text('\n'.join(lines))
print('Scene clip GUIDs and prefab component attachments verified.')
