"""One-time additions of authored CanvasGroups and the boarding script meta."""
import re, uuid
from pathlib import Path

p = Path('Assets/MainScenes/From_PM/MainScene_2_Evening.unity')
s = p.read_text(encoding='utf-8-sig')
for go, ident in [(842000199,842000264),(842000222,842000265)]:
    assert f'&{ident}\n' not in s
    pattern = rf'(^--- !u!1 &{go}\n.*?)(  m_Layer:)'
    s, n = re.subn(pattern, lambda m:m[1]+f'  - component: {{fileID: {ident}}}\n'+m[2], s, count=1, flags=re.M|re.S)
    assert n == 1
    s += f'''--- !u!225 &{ident}
CanvasGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_Alpha: 1
  m_Interactable: 1
  m_BlocksRaycasts: 1
  m_IgnoreParentGroups: 0
'''
p.write_text(s, encoding='utf-8', newline='\n')
meta = Path('Assets/SecretsThatBreathe/Scripts/PorschePassengerRide.Boarding.cs.meta')
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
print('Saved both choice CanvasGroups and boarding script metadata.')
