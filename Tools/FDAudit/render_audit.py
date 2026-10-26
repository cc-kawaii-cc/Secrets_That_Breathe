from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
from read_fbx import read,faces

root=Path(__file__).resolve().parents[2]; work=Path(__file__).resolve().parent
asset=root/'Assets/Model Palm/FD V.2'
doc=read(asset/'FD.fbx')
objects=doc.child('Objects').children
connections=[c.props for c in doc.child('Connections').children]
models={n.props[0]:n for n in objects if n.name=='Model'}
atlas=np.asarray(Image.open(asset/'Texture/tripo_rgb_0e51d11d-ea82-4e43-90cb-07f3a9a32623.jpg').convert('RGB'))/255.
triangles=[]
def layer_values(layer,key,components,indices):
    data=layer.value(key).reshape(-1,components)
    ref=layer.value('ReferenceInformationType')
    if ref=='IndexToDirect':
        ids=layer.value(key+'Index'); return data[ids[indices]]
    return data[indices]
for geometry in objects:
    if geometry.name!='Geometry' or geometry.props[-1]!='Mesh': continue
    modelid=next(c[2] for c in connections if c[0]=='OO' and c[1]==geometry.props[0] and c[2] in models)
    name=models[modelid].props[1].split('\0')[0]
    p=geometry.value('Vertices').reshape(-1,3)/100
    fs,cs=faces(geometry.value('PolygonVertexIndex'))
    nl=geometry.child('LayerElementNormal'); ul=geometry.child('LayerElementUV'); ml=geometry.child('LayerElementMaterial')
    mats=ml.value('Materials') if ml else None
    for fi,(f,corners) in enumerate(zip(fs,cs)):
        for k in range(1,len(f)-1):
            ids=np.array([f[0],f[k],f[k+1]]); ci=np.array([corners[0],corners[k],corners[k+1]])
            points=p[ids]; face=np.cross(points[1]-points[0],points[2]-points[0]); length=np.linalg.norm(face)
            if length<1e-12: continue
            face/=length
            normals=layer_values(nl,'Normals',3,ci) if nl else np.tile(face,(3,1))
            uv=layer_values(ul,'UV',2,ci) if ul else None
            mi=int(mats[fi if ml.value('MappingInformationType')=='ByPolygon' else 0]) if mats is not None else 0
            textured=ul is not None and (name!='Body' or mi==0)
            triangles.append((name,points,normals,uv,face,textured))

def normalize(v):return v/np.maximum(np.linalg.norm(v,axis=-1,keepdims=True),1e-12)
def render(tag,eye,target,scale,width=1100,height=760):
    eye=np.array(eye); target=np.array(target); forward=normalize(target-eye); right=normalize(np.cross(forward,[0,1,0])); up=np.cross(right,forward)
    depth=np.full((height,width),-np.inf); base=np.full((height,width,3),.09); smooth=base.copy(); flat=base.copy(); wire=base.copy()
    light=normalize(np.array([-.3,1,.6])); view=normalize(eye-target); halfway=normalize(light+view)
    for name,p,n,uv,fn,textured in triangles:
        rel=p-target; xy=np.stack((rel@right,rel@up),axis=-1)*(height/scale)+[width/2,height/2]; z=-(rel@forward)
        low=np.maximum(np.floor(xy.min(0)).astype(int),0); high=np.minimum(np.ceil(xy.max(0)).astype(int),[width-1,height-1])
        if np.any(low>high):continue
        a,b,c=xy; den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-10:continue
        xx,yy=np.meshgrid(np.arange(low[0],high[0]+1)+.5,np.arange(low[1],high[1]+1)+.5)
        wa=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
        wb=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den; wc=1-wa-wb
        zz=wa*z[0]+wb*z[1]+wc*z[2]; sl=np.s_[low[1]:high[1]+1,low[0]:high[0]+1]
        visible=(wa>=0)&(wb>=0)&(wc>=0)&(zz>depth[sl])
        if not visible.any():continue
        depth[sl][visible]=zz[visible]
        nn=normalize(wa[...,None]*n[0]+wb[...,None]*n[1]+wc[...,None]*n[2])
        diffuse=.18+.65*np.maximum(nn@light,0); spec=np.maximum(nn@halfway,0)**48*.45
        fs=.18+.65*max(fn@light,0)+max(fn@halfway,0)**48*.45
        color=np.full((*wa.shape,3),[.18,.23,.28] if name=='Window' else [.65,.65,.65])
        if textured:
            tex=wa[...,None]*uv[0]+wb[...,None]*uv[1]+wc[...,None]*uv[2]
            tx=np.clip((tex[...,0]*atlas.shape[1]).astype(int),0,atlas.shape[1]-1); ty=np.clip(((1-tex[...,1])*atlas.shape[0]).astype(int),0,atlas.shape[0]-1)
            color=atlas[ty,tx]
        base[sl][visible]=color[visible]
        smooth[sl][visible]=np.repeat((diffuse+spec)[...,None],3,axis=-1)[visible]*.74
        flat[sl][visible]=fs*.74
        edge=np.minimum(np.minimum(wa,wb),wc)<.035
        wcimg=np.repeat(diffuse[...,None],3,axis=-1)*.74; wcimg[edge]=[.06,.20,.06]; wire[sl][visible]=wcimg[visible]
    images=[]
    for title,data in [('TEXTURE ONLY',base),('SOURCE NORMALS / NO TEXTURE',smooth),('FACE NORMALS / NO TEXTURE',flat),('TRIANGULATION',wire)]:
        im=Image.fromarray(np.uint8(np.clip(data[::-1],0,1)*255)); draw=ImageDraw.Draw(im);draw.rectangle((0,0,width,28),fill=(15,15,15));draw.text((12,8),title,fill='white');images.append(im)
    overview=Image.new('RGB',(width*2,height*2));
    for j,im in enumerate(images): overview.paste(im,((j%2)*width,(j//2)*height))
    overview.save(work/f'{tag}_comparison.png')
    images[1].save(work/f'{tag}_solid.png'); images[0].save(work/f'{tag}_texture.png')

if __name__=='__main__':
    render('front',(3.8,2.8,5.5),(0,.65,0),4.9)
    render('rear',(-3.8,2.6,-5.5),(0,.65,0),4.9)
    render('hood',(2.2,3.2,4.5),(0,.8,1.0),2.25)
    print('FD_READONLY_RENDER_COMPLETE')
