"""Full headlamp / upper rear lens emission on original car UVs, no extra mesh."""
from pathlib import Path
import re,json,uuid,hashlib,shutil
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
from read_fbx import read,faces

root=Path(__file__).resolve().parents[2];asset=root/'Assets/Model Palm/FD V.2';work=Path(__file__).resolve().parent/'FullLensTexture';work.mkdir(exist_ok=True)
prefab=asset/'FD.prefab'
if not (work/'FD.prefab.before').exists():shutil.copyfile(prefab,work/'FD.prefab.before')
atlaspath=asset/'Texture/tripo_rgb_0e51d11d-ea82-4e43-90cb-07f3a9a32623.jpg'
atlas=np.asarray(Image.open(atlaspath).convert('RGB'))/255.;size=len(atlas)
front_poly=[[359,268],[451,295],[547,324],[640,352],[696,478],[622,465],[559,455],[517,454],[480,458],[443,465],[410,457],[386,440],[373,412],[363,366]]
rear_poly=[[476,304],[534,310],[608,325],[648,345],[701,392],[698,399],[451,388]]
def region(poly,eye,target,scale):
    mask=Image.new('L',(1100,760));ImageDraw.Draw(mask).polygon(poly,fill=255)
    mask=np.asarray(mask.filter(ImageFilter.GaussianBlur(.7)))/255.
    eye=np.asarray(eye);target=np.asarray(target);f=target-eye;f/=np.linalg.norm(f)
    right=np.cross(f,[0,1,0]);right/=np.linalg.norm(right);up=np.cross(right,f)
    return mask,target,right,up,scale,f
regions=[region(front_poly,[-.5,.9,4],[-.55,.65,1.9],.7),region(rear_poly,[-.55,1,-4],[-.53,.84,-2],.6)]
doc=read(asset/'FD.fbx');objects=doc.child('Objects').children
def visibility_depth(target,right,up,scale,forward,mirror):
    depth=np.full((760,1100),-np.inf,np.float32)
    for g in objects:
        if g.name!='Geometry' or len(g.value('Vertices',[])) not in [13909*3,920*3]:continue
        positions=g.value('Vertices').reshape(-1,3)/100;positions=positions*np.array([mirror,1,1]);polys,_=faces(g.value('PolygonVertexIndex'))
        rel=positions-target;screen=np.stack((rel@right,rel@up),axis=-1)*(760/scale)+[550,380];distance=-(rel@forward)
        for face in polys:
            for k in range(1,len(face)-1):
                ids=[face[0],face[k],face[k+1]];a,b,c=screen[ids]
                lo=np.maximum(np.floor(screen[ids].min(0)).astype(int),0);hi=np.minimum(np.ceil(screen[ids].max(0)).astype(int),[1099,759])
                if np.any(lo>hi):continue
                den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
                if abs(den)<1e-10:continue
                xx,yy=np.meshgrid(np.arange(lo[0],hi[0]+1)+.5,np.arange(lo[1],hi[1]+1)+.5)
                wa=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
                wb=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;wc=1-wa-wb
                inside=(wa>=0)&(wb>=0)&(wc>=0);zz=wa*distance[ids[0]]+wb*distance[ids[1]]+wc*distance[ids[2]]
                sl=np.s_[lo[1]:hi[1]+1,lo[0]:hi[0]+1];old=depth[sl];old[inside]=np.maximum(old[inside],zz[inside])
    # Baking texels and diagnostic pixels have different sample centres. The
    # nearest visible surface in a 3x3 neighbourhood tolerates that subpixel shift.
    padded=np.pad(depth,1,constant_values=-np.inf)
    return np.maximum.reduce([padded[y:y+760,x:x+1100] for y in range(3) for x in range(3)])[::-1]
depths=[[visibility_depth(target,right,up,scale,f,mirror) for mirror in [1,-1]] for mask,target,right,up,scale,f in regions]
rejected_hidden=0;rejected_backfacing=0
output=np.zeros((size,size,3),np.float32)
allowed=np.zeros((size,size),bool)
for g in objects:
    if g.name!='Geometry' or len(g.value('Vertices',[])) not in [13909*3,920*3]:continue
    p=g.value('Vertices').reshape(-1,3)/100;fs,cs=faces(g.value('PolygonVertexIndex'));ul=g.child('LayerElementUV');uv=ul.value('UV').reshape(-1,2);ui=ul.value('UVIndex')
    ml=g.child('LayerElementMaterial');mats=ml.value('Materials') if ml else None
    for fi,(f,corners) in enumerate(zip(fs,cs)):
        if len(p)==13909 and mats[fi]!=0:continue
        fp=p[f]
        if fp[:,1].min()>1.03 or fp[:,1].max()<.50 or (fp[:,2].max()<1.5 and fp[:,2].min()>-1.8):continue
        for k in range(1,len(f)-1):
            ids=np.array([f[0],f[k],f[k+1]]);ci=np.array([corners[0],corners[k],corners[k+1]])
            tex=uv[ui[ci]]*size;a,b,c=tex;lo=np.maximum(np.floor(tex.min(0)).astype(int),0);hi=np.minimum(np.ceil(tex.max(0)).astype(int),size-1)
            den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
            if abs(den)<1e-10:continue
            xx,yy=np.meshgrid(np.arange(lo[0],hi[0]+1)+.5,np.arange(lo[1],hi[1]+1)+.5)
            wa=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
            wb=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;wc=1-wa-wb;inside=(wa>=0)&(wb>=0)&(wc>=0)
            point=wa[...,None]*p[ids[0]]+wb[...,None]*p[ids[1]]+wc[...,None]*p[ids[2]]
            mirrored=point.copy();mirrored[...,0]=-np.abs(mirrored[...,0])
            original=atlas[size-1-np.clip(yy.astype(int),0,size-1),np.clip(xx.astype(int),0,size-1)]
            luminance=original.mean(-1)
            emitted=np.zeros((*wa.shape,3))
            face_normal=np.cross(p[ids[1]]-p[ids[0]],p[ids[2]]-p[ids[0]])
            face_normal/=max(np.linalg.norm(face_normal),1e-12)
            for idx,(mask,target,right,up,scale,forward) in enumerate(regions):
                rel=mirrored-target;sx=(rel@right)*(760/scale)+550;sy=759-((rel@up)*(760/scale)+380)
                valid=(sx>=0)&(sx<1100)&(sy>=0)&(sy<760)&inside
                valid&=point[...,2]>1.5 if idx==0 else point[...,2]<-1.8
                ix=np.clip(sx.astype(int),0,1099);iy=np.clip(sy.astype(int),0,759)
                nearest=np.where(point[...,0]>0,depths[idx][1][iy,ix],depths[idx][0][iy,ix])
                point_depth=-(rel@forward)
                on_surface=np.abs(point_depth-nearest)<.003
                normal=np.broadcast_to(face_normal,point.shape).copy();normal[...,0]*=np.where(point[...,0]>0,-1,1)
                outward=(normal@(-forward))>.015
                footprint=mask[iy,ix]>.01
                rejected_hidden+=int((valid&footprint&~on_surface).sum())
                rejected_backfacing+=int((valid&footprint&~outward).sum())
                valid&=on_surface&outward
                amount=mask[np.clip(sy.astype(int),0,759),np.clip(sx.astype(int),0,1099)]*valid
                # Modulation preserves lens/reflector details instead of painting
                # a flat opaque glow over the stock texture.
                strength=.35+.65*luminance if idx==0 else .66+.25*luminance
                color=np.array([1.,.98,.94]) if idx==0 else np.array([1.,.008,.004])
                emitted=np.maximum(emitted,(amount*strength)[...,None]*color)
                sl=np.s_[lo[1]:hi[1]+1,lo[0]:hi[0]+1];allowed[sl]|=valid
            sl=np.s_[lo[1]:hi[1]+1,lo[0]:hi[0]+1];output[sl]=np.maximum(output[sl],emitted)
image=Image.fromarray(np.uint8(np.clip(output[::-1],0,1)*255)).filter(ImageFilter.MaxFilter(3))
# Prevent the padding step itself from bleeding into hidden/back-facing UV faces.
pixels=np.asarray(image).copy();pixels[~allowed[::-1]]=0;image=Image.fromarray(pixels)
texpath=asset/'Texture/FD_Lights_Emission.png';image.save(texpath)
def restore_meta(path,fileid=None):
    meta=Path(str(path)+'.meta');old=root/'Tools/FDAudit/Lights/LegacyAtlas'/meta.name
    if not meta.exists() and old.exists():shutil.copyfile(old,meta)
    if not meta.exists():
        value=uuid.uuid4().hex
        if fileid is None:
            text=Path(str(atlaspath)+'.meta').read_text();text=re.sub(r'(?m)^guid: [0-9a-f]+$',f'guid: {value}',text)
            text=re.sub(r'(?m)^(\s*)maxTextureSize: \d+$',r'\g<1>maxTextureSize: 4096',text);text=re.sub(r'(?m)^(\s*)textureCompression: \d+$',r'\g<1>textureCompression: 0',text)
        else:text=f'fileFormatVersion: 2\nguid: {value}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: {fileid}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
        meta.write_text(text,encoding='utf-8')
    return re.search(r'guid: ([0-9a-f]+)',meta.read_text())[1]
texguid=restore_meta(texpath);matpath=asset/'Material/FD_Lights_Emission.mat';matguid=restore_meta(matpath,2100000)
mat=matpath.read_text() if matpath.exists() else (asset/'Material/tripo_mat_2fc8db08.mat').read_text()
mat=mat.replace('m_Name: tripo_mat_2fc8db08','m_Name: FD_Lights_Emission').replace('m_ValidKeywords: []','m_ValidKeywords:\n  - _EMISSION').replace('m_LightmapFlags: 4','m_LightmapFlags: 2')
mat,n=re.subn(r'(- _EmissionMap:\s*\n\s*m_Texture: )\{[^\n]+\}',rf'\g<1>{{fileID: 2800000, guid: {texguid}, type: 3}}',mat);assert n==1
if not matpath.exists():mat=re.sub(r'(- _EmissionColor: )\{[^\n]+\}',r'\1{r: 3, g: 3, b: 3, a: 1}',mat)
matpath.write_text(mat,encoding='utf-8')
text=prefab.read_text();fid=lambda label:int.from_bytes(hashlib.sha256(('FD-independent:'+label).encode()).digest()[:8],'little')&0x7fffffffffffffff
names=['FD_'+kind+'_Emission_'+side for kind in ['Headlight','Taillight'] for side in ['Left','Right']]
for name in names:
    for suffix in ['go','transform','filter','renderer']:
        id=fid(name+suffix);text=re.sub(r'--- !u!\d+ &'+str(id)+r'\n.*?(?=--- !u!|\Z)','',text,flags=re.S)
    text=text.replace('  - {fileID: '+str(fid(name+'transform'))+'}\n','')
for renderer in [1753977182908596650,8835136615230305043]:
    block=re.search(r'--- !u!23 &'+str(renderer)+r'\n.*?(?=--- !u!|\Z)',text,re.S);assert block
    replacement,n=re.subn(r'(m_Materials:\s*\n\s*- \{fileID: 2100000, guid: )[0-9a-f]+(, type: 2\})',rf'\g<1>{matguid}\2',block[0],count=1);assert n==1
    text=text[:block.start()]+replacement+text[block.end():]
prefab.write_text(text,encoding='utf-8')
report={'method':'Texture-based emission on original UVs','headlights':'Full marked headlamp lens, white','taillights':'Full marked upper lens, red; lower clear area excluded','materialGUID':matguid,'textureGUID':texguid,'meshObjects':8,'triangles':30624,'extraEmitterObjects':0,'baseColorTextureUnchanged':True,'frontTrace':front_poly,'rearTrace':rear_poly,'emissionHDR':[3,3,3]}
report.update(visibilityDepthTest=True,outwardFacingOnly=True,hiddenTexelSamplesRejected=rejected_hidden,backFacingTexelSamplesRejected=rejected_backfacing,depthToleranceMetres=.003,UVPaddingRestricted=True)
(work/'build_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('FD_FULL_LENS_TEXTURE_SAVED',np.count_nonzero(np.asarray(image)[...,0]))
