"""Reproject the actual Unity lens meshes and their atlas for mask inspection."""
from pathlib import Path
import re, json
import numpy as np
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
work=Path(__file__).resolve().parent/'EmissionMask'
work.mkdir(exist_ok=True)
asset=root/'Assets/Model Palm/Porsche V.2'
atlas=np.asarray(Image.open(asset/'Texture/tripo_rgb_62867c90-c4b3-4b96-b7c1-0cf4f20ce93e.jpg').convert('RGB'))

def mesh(side):
    text=(asset/f'Meshes/Headlight_{side}.asset').read_text(encoding='utf-8-sig')
    count=int(re.search(r'm_VertexCount: (\d+)',text)[1])
    data=np.frombuffer(bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',text)[1]),dtype='<f4').reshape(count,14)
    tri=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)',text)[1]),dtype='<u2').reshape(-1,3)
    return data[:,:3],data[:,10:12],tri

def basis(p):
    origin=(p.min(0)+p.max(0))/2
    _,vectors=np.linalg.eigh(np.cov((p-origin).T))
    up=vectors[:,-1]; up*=np.sign(up[1])
    right=vectors[:,1]; right*=np.sign(right[0])
    normal=np.cross(right,up); normal*=np.sign(normal[2])
    return origin,right,up,normal

def raster(side,emission=None):
    p,uv,tri=mesh(side); origin,right,up,normal=basis(p)
    q=np.stack(((p-origin)@right,(p-origin)@up,(p-origin)@normal),axis=-1)
    low=q[:,:2].min(0); high=q[:,:2].max(0)
    width,height=700,1000
    xy=(q[:,:2]-low)/(high-low)*[width-60,height-60]+30
    out=np.full((height,width,3),22,np.uint8); depth=np.full((height,width),-1e9)
    position=np.zeros((height,width,3),np.float32); uvmap=np.zeros((height,width,2),np.float32)
    for t in tri:
        a,b,c=xy[t]; lo=np.maximum(np.floor(xy[t].min(0)).astype(int),0); hi=np.minimum(np.ceil(xy[t].max(0)).astype(int),[width-1,height-1])
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-10: continue
        x,y=np.meshgrid(np.arange(lo[0],hi[0]+1)+.5,np.arange(lo[1],hi[1]+1)+.5)
        w0=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den
        w1=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den; w2=1-w0-w1
        z=w0*q[t[0],2]+w1*q[t[1],2]+w2*q[t[2],2]
        sl=np.s_[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
        keep=(w0>=0)&(w1>=0)&(w2>=0)&(z>depth[sl])
        tex=w0[...,None]*uv[t[0]]+w1[...,None]*uv[t[1]]+w2[...,None]*uv[t[2]]
        tx=np.clip((tex[...,0]*atlas.shape[1]).astype(int),0,atlas.shape[1]-1)
        ty=np.clip(((1-tex[...,1])*atlas.shape[0]).astype(int),0,atlas.shape[0]-1)
        color=atlas[ty,tx] if emission is None else emission[ty,tx]
        out[sl][keep]=color[keep]; depth[sl][keep]=z[keep]; uvmap[sl][keep]=tex[keep]
        pos=w0[...,None]*p[t[0]]+w1[...,None]*p[t[1]]+w2[...,None]*p[t[2]]
        position[sl][keep]=pos[keep]
    im=Image.fromarray(out[::-1]); im.save(work/(f'{side}_'+('base.png' if emission is None else 'emission.png')))
    if emission is None:
        np.savez_compressed(work/f'{side}_projection.npz',position=position,uv=uvmap,depth=depth,origin=origin,right=right,up=up,normal=normal,low=low,high=high)
    return im

if __name__=='__main__':
    for side in ['Left','Right']: raster(side)
    print('UV_INSPECTION_COMPLETE')
