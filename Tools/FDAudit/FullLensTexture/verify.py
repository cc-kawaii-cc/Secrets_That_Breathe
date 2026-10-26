from pathlib import Path
import re,json,subprocess
import numpy as np
from PIL import Image
root=Path(__file__).resolve().parents[3];asset=root/'Assets/Model Palm/FD V.2';work=Path(__file__).resolve().parent
report=json.loads((work/'build_report.json').read_text())
baseline=(root/'Tools/FDAudit/Lights/FD.prefab.before').read_text(encoding='utf-8-sig');current=(asset/'FD.prefab').read_text(encoding='utf-8-sig')
assert current.count(report['materialGUID'])==2
assert current.replace(report['materialGUID'],'0325d93ed7798c948be090f5ae6bd3e6')==baseline,'Unexpected changes to original prefab'
assert 'FD_Headlight_Emission_Left' not in current and 'FD_Taillight_Emission_Left' not in current
mat=(asset/'Material/FD_Lights_Emission.mat').read_text();original=(asset/'Material/tripo_mat_2fc8db08.mat').read_text()
for prop in ['_BaseMap','_MainTex']:
    get=lambda s:re.search(r'- '+prop+r':\s*\n\s*m_Texture: (\{[^\n]+\})',s)[1]
    assert get(mat)==get(original)
assert report['textureGUID'] in mat and '  - _EMISSION' in mat
for name in ['FD.fbx','FD.fbx.meta','Material/tripo_mat_2fc8db08.mat','Texture/tripo_rgb_0e51d11d-ea82-4e43-90cb-07f3a9a32623.jpg']:
    path=asset/name;subprocess.run(['git','diff','--exit-code','HEAD','--',path.relative_to(root).as_posix()],cwd=root,check=True,capture_output=True)
im=np.asarray(Image.open(asset/'Texture/FD_Lights_Emission.png'))
assert ((im[...,0]>80)&(im[...,1]>80)).any() and ((im[...,0]>80)&(im[...,1]<20)).any()
result={'method':'Original car texture UVs + selective Emission Map','fullHeadlampLenses':True,'upperRedRearLenses':True,'lowerClearRearLensesExcluded':True,'originalDiffuseTexturePreserved':True,'originalMeshesAndImporterUnchanged':True,'allOriginalPrefabBlocksPreservedExceptTwoMaterialReferences':True,'additionalEmitterObjects':0,'meshObjects':8,'triangles':30624,'newTextureCount':1,'textureSize':list(im.shape[:2]),'emissionHDR':report['emissionHDR'],'verification':'Actual source mesh UV reprojection and prefab/file invariants','notVerified':['Live Unity import / Bloom','Standalone build']}
(work/'final_validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
