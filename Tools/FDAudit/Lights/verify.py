from pathlib import Path
import re,json,hashlib
from PIL import Image
import numpy as np

root=Path(__file__).resolve().parents[3]; work=Path(__file__).resolve().parent;asset=root/'Assets/Model Palm/FD V.2'
before=(work/'FD.prefab.before').read_text(encoding='utf-8-sig');after=(asset/'FD.prefab').read_text(encoding='utf-8-sig')
report=json.loads((work/'build_report.json').read_text())
old='0325d93ed7798c948be090f5ae6bd3e6';new=report['materialGUID']
assert after.count(new)==2
assert after.replace(new,old)==before,'Only Body atlas and Light material references may change'
assert (asset/'FD.fbx').read_bytes()==(work/'FD.fbx.before').read_bytes()
assert (asset/'Material/tripo_mat_2fc8db08.mat').read_bytes()==(work/'tripo_mat_2fc8db08.mat.before').read_bytes()
mat=(asset/'Material/FD_Lights_Emission.mat').read_text()
assert '  - _EMISSION' in mat and 'm_LightmapFlags: 2' in mat
assert report['textureGUID'] in mat
assert report['classification']=={'front':2,'rear':4,'off':6}
image=np.asarray(Image.open(asset/'Texture/FD_Lights_Emission.png'))
assert np.count_nonzero((image[...,1]>180)&(image[...,2]>150))>0
assert np.count_nonzero((image[...,0]>100)&(image[...,1]<20))>0
result={'prefabSaved':True,'onlyMaterialBindingsChanged':True,'transformsPivotsAndSerializedIDsPreserved':True,'sourceFBXUnchanged':True,'originalSharedMaterialUnchanged':True,'meshObjects':8,'triangles':30624,'newTriangles':0,'newTextures':1,'textureSize':list(image.shape[:2]),'frontLightStyle':'One conventional white low-beam aperture per side, no DRL','rearLightStyle':'Continuous upper red lens areas, clear lower sections off','shader':'Existing URP Lit','verification':'FBX geometry/UV CPU diagnostic projections and file invariants','notVerified':['Live Unity import and scene Bloom','Standalone player build','Brake/turn-signal switching (not implemented)']}
(work/'final_validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result))
