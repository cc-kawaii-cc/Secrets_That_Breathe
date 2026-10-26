"""Bake a rear LED band into the existing UVs. Mesh data is read only."""
from pathlib import Path
import re, json, hashlib
import numpy as np
from PIL import Image, ImageFilter

root=Path(__file__).resolve().parents[2]
work=Path(__file__).resolve().parent
asset=root/'Assets/Model Palm/Porsche V.2'
meshpath=asset/'Meshes/Light_RearOnly.asset'
text=meshpath.read_text(encoding='utf-8-sig')
n=int(re.search(r'm_VertexCount: (\d+)',text)[1])
data=np.frombuffer(bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',text)[1]),dtype='<f4').reshape(n,14)
pos=data[:,:3]; uv=data[:,10:12]
indices=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)',text)[1]),dtype='<u2').reshape(-1,3)
print('Bounds',pos.min(0),pos.max(0))
for lo,hi in [(-.75,-.6),(-.6,-.3),(-.3,.3),(.3,.6),(.6,.75)]:
    p=pos[(pos[:,0]>=lo)&(pos[:,0]<hi)]
    if len(p): print(lo,hi,np.percentile(p[:,1],[0,25,50,75,100]))

size=4096
mask=np.zeros((size,size),np.float32)
coverage=np.zeros((size,size),bool)
# The middle light guide is part of Body, whereas the two end lenses are Light.
# Both use the same unique atlas. Bake both surfaces into one dedicated map.
bt=(asset/'Meshes/Body_HeadlightsSeparated.asset').read_text(encoding='utf-8-sig')
bn=int(re.search(r'm_VertexCount: (\d+)',bt)[1])
bd=np.frombuffer(bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',bt)[1]),dtype='<f4').reshape(bn,14)
bi=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)',bt)[1]),dtype='<u2').reshape(-1,3)
pos=np.concatenate((pos,bd[:,:3])); uv=np.concatenate((uv,bd[:,10:12])); indices=np.concatenate((indices,bi+n))
# Use the existing continuous lens, with a narrow upper light guide and two
# subordinate short segments per side; all coordinates are in original metres.
for tri in indices:
    tp=pos[tri]
    if tp[:,2].min()>-1.78 or tp[:,1].min()>.80 or tp[:,1].max()<.73 or tp[:,0].min()>.75 or tp[:,0].max()<-.75: continue
    pts=uv[tri]*size
    low=np.maximum(np.floor(pts.min(0)-1).astype(int),0)
    high=np.minimum(np.ceil(pts.max(0)+1).astype(int),size-1)
    a,b,c=pts
    den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
    if abs(den)<1e-8: continue
    xx,yy=np.meshgrid(np.arange(low[0],high[0]+1)+.5,np.arange(low[1],high[1]+1)+.5)
    w0=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
    w1=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den
    w2=1-w0-w1
    inside=(w0>=0)&(w1>=0)&(w2>=0)
    p=w0[...,None]*pos[tri[0]]+w1[...,None]*pos[tri[1]]+w2[...,None]*pos[tri[2]]
    x=np.abs(p[...,0]); y=p[...,1]
    # Slightly lower at the outer tips, following the measured lens silhouette.
    center=.780-0.021*np.clip((x-.50)/.25,0,1)**1.5
    band=np.clip((.0065-np.abs(y-center))/.0015,0,1)
    lower=np.clip((.0035-np.abs(y-(center-.017)))/.001,0,1)
    segments=((x>.43)&(x<.53))|((x>.56)&(x<.66))
    rear=p[...,2]<(-2.14+.37*(x/.75)**2)
    strength=np.maximum(band,lower*segments*.65)*inside*rear*(x<.744)
    sl=np.s_[low[1]:high[1]+1,low[0]:high[0]+1]
    mask[sl]=np.maximum(mask[sl],strength)
    coverage[sl]|=inside
# Dilate UV seams by two texels; new texture never changes the source atlas.
im=Image.fromarray(np.uint8(mask[::-1]*255)).filter(ImageFilter.MaxFilter(5))
v=np.asarray(im)/255.
rgb=np.stack((v, v*.008, v*.015),axis=-1)
Image.fromarray(np.uint8(rgb*255)).save(work/'atlas_bake_diagnostic.png')
report={'sourceMeshSHA256':hashlib.sha256(meshpath.read_bytes()).hexdigest(),'vertices':n,'triangles':len(indices),'boundsMin':pos.min(0).tolist(),'boundsMax':pos.max(0).tolist(),'emissionMapSize':size,'litTexels':int((v>0).sum()),'geometryChanged':False}
(work/'texture_validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
