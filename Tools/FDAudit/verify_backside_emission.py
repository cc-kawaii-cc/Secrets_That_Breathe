"""Dark emission-only before/after projection from the reported wheel-arch angle."""
from pathlib import Path
import numpy as np
from PIL import Image
import render_audit as audit

work=audit.work/'FullLensTexture'
source=Path(audit.__file__).read_text(encoding='utf-8')
code=source[source.index('def normalize('):source.index("if __name__=='__main__':")]
code=code.replace('base[sl][visible]=color[visible]',"""color[:]=0
        if name=='Light' or (name=='Body' and textured):
            e=wa[...,None]*uv[0]+wb[...,None]*uv[1]+wc[...,None]*uv[2]
            ex=np.clip((e[...,0]*4096).astype(int),0,4095);ey=np.clip(((1-e[...,1])*4096).astype(int),0,4095)
            color=np.clip(emission[ey,ex]**2.2*3,0,1)
        base[sl][visible]=color[visible]""")
code=code.replace("('TEXTURE ONLY',base)","('EMISSION ONLY / SIDE CHECK',base)")
namespace=dict(audit.__dict__);namespace['work']=work
exec(code,namespace)
for tag,path in [('before',work/'Emission_before_backside_fix.png'),('after',audit.asset/'Texture/FD_Lights_Emission.png')]:
    namespace['emission']=np.asarray(Image.open(path).convert('RGB'))/255.
    namespace['render']('side_'+tag,(-3.2,.74,1.5),(-.65,.63,1.55),1.4)
print('FD_BACKSIDE_BEFORE_AFTER_COMPLETE')
