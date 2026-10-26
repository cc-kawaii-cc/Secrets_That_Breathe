"""Diagnostic projection using real FBX triangles/UVs and the saved emission map."""
from pathlib import Path
import re
import numpy as np
from PIL import Image
import render_audit as audit

emission=np.asarray(Image.open(audit.asset/'Texture/FD_Lights_Emission.png').convert('RGB'))/255.
material=(audit.asset/'Material/FD_Lights_Emission.mat').read_text()
emission_hdr=float(re.search(r'_EmissionColor: \{r: ([0-9.]+)',material)[1])
source=Path(audit.__file__).read_text(encoding='utf-8')
start=source.index('def normalize('); end=source.index("if __name__=='__main__':")
code=source[start:end]
code=code.replace("('TEXTURE ONLY',base)","('DIAGNOSTIC DIFFUSE + EMISSION / NO BLOOM',base)")
code=code.replace("base[sl][visible]=color[visible]", """if name=='Light' or (name=='Body' and textured):
            euv=wa[...,None]*uv[0]+wb[...,None]*uv[1]+wc[...,None]*uv[2]
            ex=np.clip((euv[...,0]*emission.shape[1]).astype(int),0,emission.shape[1]-1)
            ey=np.clip(((1-euv[...,1])*emission.shape[0]).astype(int),0,emission.shape[0]-1)
            light=emission[ey,ex]**2.2*4
            color=np.clip(color+light,0,1)
        base[sl][visible]=color[visible]""")
# Avoid shadowing the directional-light vector with the emission samples.
code=code.replace('light=emission[ey,ex]**2.2*4','emitted=emission[ey,ex]**2.2*emission_hdr').replace('color+light','color+emitted')
namespace=dict(audit.__dict__); namespace['emission']=emission;namespace['emission_hdr']=emission_hdr; namespace['work']=audit.work/'Lights'
exec(code,namespace)
if __name__=='__main__':
    namespace['render']('front_emission',(3.0,1.7,5.2),(0,.7,.4),3.8)
    namespace['render']('rear_emission',(-2.8,1.6,-5.2),(0,.75,-.5),3.8)
    namespace['render']('front_lamp_close',(-.5,.9,4),(-.55,.65,1.9),.7)
    namespace['render']('rear_lamp_close',(-.55,1,-4),(-.53,.84,-2),.6)
    print('FD_EMISSION_PREVIEW_COMPLETE')
