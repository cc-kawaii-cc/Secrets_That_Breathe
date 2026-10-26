"""Diagnostic render of independent lamp meshes together with the unchanged car."""
from pathlib import Path
import numpy as np
import render_audit as audit

work=audit.work/'IndependentLights'
for path in work.glob('FD_*_Emission_*.npz'):
    data=np.load(path);p=data['positions'];n=data['normals'];ids=data['triangles'];uv=data['uv']
    for t in ids:
        fn=np.cross(p[t[1]]-p[t[0]],p[t[2]]-p[t[0]]);fn/=np.linalg.norm(fn)
        audit.triangles.append((path.stem,p[t],n[t],uv[t],fn,False))
source=Path(audit.__file__).read_text(encoding='utf-8')
code=source[source.index('def normalize('):source.index("if __name__=='__main__':")]
code=code.replace("base[sl][visible]=color[visible]", """if name.startswith('FD_Headlight_Emission_'):
            color[:]=[1.,.97,.9]
        elif name.startswith('FD_Taillight_Emission_'):
            color[:]=[1.,.015,.008]
        base[sl][visible]=color[visible]""")
code=code.replace("('TEXTURE ONLY',base)","('INDEPENDENT EMITTER GEOMETRY / NO BLOOM',base)")
namespace=dict(audit.__dict__);namespace['work']=work
exec(code,namespace)
if __name__=='__main__':
    namespace['render']('front',(3,1.7,5.2),(0,.7,.4),3.8)
    namespace['render']('rear',(-2.8,1.6,-5.2),(0,.75,-.5),3.8)
    namespace['render']('front_close',(-.5,.9,4),(-.55,.65,1.9),.7)
    namespace['render']('rear_close',(-.55,1,-4),(-.53,.84,-2),.6)
    print('FD_INDEPENDENT_PREVIEWS_COMPLETE')
