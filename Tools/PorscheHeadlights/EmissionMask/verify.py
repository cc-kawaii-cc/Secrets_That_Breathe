from pathlib import Path
import re,json,hashlib,subprocess
from PIL import Image

r=Path(__file__).resolve().parents[3]
a=r/'Assets/Model Palm/Porsche V.2'
w=Path(__file__).resolve().parent
m=(a/'Material/Headlights.mat').read_text(encoding='utf-8-sig')
b=(w/'Headlights_before_selective_emission.mat').read_text(encoding='utf-8-sig')
def unchanged(s):
    s=re.sub(r'(- _EmissionMap:\s*\n\s*m_Texture: )\{[^\n]+\}',r'\1MAP',s)
    return re.sub(r'(- _EmissionColor: )\{[^\n]+\}',r'\1VALUE',s)
assert unchanged(m)==unchanged(b)
guid=re.search(r'guid: ([0-9a-f]+)',(a/'Texture/Headlights_EmissionMask.png.meta').read_text())[1]
assert re.search(r'- _EmissionMap:\s*\n\s*m_Texture: \{fileID: 2800000, guid: '+guid,m)
prefab=(a/'PorscheV.2.prefab').read_text()
assert prefab.count('guid: 6443a3699d0cb974f9006120c85fcb9e, type: 2')==2
hashes={}; tris=0
for side in ['Left','Right']:
    p=a/f'Meshes/Headlight_{side}.asset'; raw=p.read_bytes()
    subprocess.run(['git','diff','--exit-code','HEAD','--',p.relative_to(r).as_posix()],cwd=r,check=True,capture_output=True)
    hashes[side]=hashlib.sha256(raw).hexdigest()
    tris+=int(re.search(rb'indexCount: (\d+)',raw)[1])//3
report={'materialPropertiesChanged':['EmissionMap','EmissionColor'],'baseMapAndAllOtherMaterialPropertiesPreserved':True,'bothPrefabBindingsVerified':True,'headlightTriangles':tris,'meshSHA256':hashes,'meshGitDiffEmpty':True,'textureSize':Image.open(a/'Texture/Headlights_EmissionMask.png').size,'emissionKeywordEnabled':'  - _EMISSION' in m,'bakedEmissiveFlagPreserved':'m_LightmapFlags: 2' in m,'verification':'Actual mesh UV reprojection; no screen control; live Unity Bloom not verified'}
(w/'final_validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
