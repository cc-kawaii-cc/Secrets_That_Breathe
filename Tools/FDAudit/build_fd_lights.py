"""Selective conventional headlamps and red rear bulbs; preserve FD geometry."""
from pathlib import Path
import sys,re,json,uuid,shutil,hashlib
import numpy as np
from PIL import Image,ImageFilter
from read_fbx import read,faces

root=Path(__file__).resolve().parents[2]; asset=root/'Assets/Model Palm/FD V.2'
work=Path(__file__).resolve().parent/'Lights'; work.mkdir(exist_ok=True)
source=asset/'FD.fbx'; prefab=asset/'FD.prefab'
for p in [source,prefab,asset/'Material/tripo_mat_2fc8db08.mat']:
    if not (work/(p.name+'.before')).exists(): shutil.copyfile(p,work/(p.name+'.before'))
doc=read(source)
g=next(g for g in doc.child('Objects').children if g.name=='Geometry' and len(g.value('Vertices',[]))==920*3)
p=g.value('Vertices').reshape(-1,3)/100; fs,cs=faces(g.value('PolygonVertexIndex'))
ul=g.child('LayerElementUV'); uv=ul.value('UV').reshape(-1,2); uvindex=ul.value('UVIndex')
adj=[set() for _ in p]
for f in fs:
    for a,b in zip(f,f[1:]+f[:1]): adj[a].add(b);adj[b].add(a)
remaining=set(range(len(p))); components=[]; vertex_group=np.zeros(len(p),int)
while remaining:
    todo=[remaining.pop()]; ids=[]
    while todo:
        i=todo.pop();ids.append(i)
        for j in adj[i]&remaining:remaining.remove(j);todo.append(j)
    group=len(components); vertex_group[ids]=group
    q=p[ids]; mn=q.min(0);mx=q.max(0);center=(mn+mx)/2; radius=(mx-mn)/2
    # Conventional low beam: only the large outer reflector bulb on each side.
    kind='rear' if center[2]<-1.8 else ('front' if center[2]>1.65 and abs(center[0])>.60 else 'off')
    components.append({'id':group,'kind':kind,'vertices':len(ids),'min':mn.tolist(),'max':mx.tolist(),'center':center,'radius':radius})

size=4096; output=np.zeros((size,size,3),np.float32); count={'front':0,'rear':0,'off':0}; conflict=0
for f,corners in zip(fs,cs):
    rec=components[vertex_group[f[0]]]; kind=rec['kind']
    if kind!='front':continue
    center=rec['center']; radius=rec['radius']
    for k in range(1,len(f)-1):
        ids=np.array([f[0],f[k],f[k+1]]); ci=np.array([corners[0],corners[k],corners[k+1]])
        tex=uv[uvindex[ci]]*size; a,b,c=tex
        lo=np.maximum(np.floor(tex.min(0)).astype(int),0);hi=np.minimum(np.ceil(tex.max(0)).astype(int),size-1)
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-10:continue
        xx,yy=np.meshgrid(np.arange(lo[0],hi[0]+1)+.5,np.arange(lo[1],hi[1]+1)+.5)
        wa=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
        wb=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;wc=1-wa-wb
        inside=(wa>=0)&(wb>=0)&(wc>=0)
        point=wa[...,None]*p[ids[0]]+wb[...,None]*p[ids[1]]+wc[...,None]*p[ids[2]]
        local=(point-center)/radius
        # Compact bulb apertures, not glowing outlines/DRLs or entire housings.
        disk=np.sqrt(local[...,0]**2+local[...,1]**2)
        edge=np.clip((.88-disk)/.14,0,1)
        frontface=np.clip(((local[...,2] if kind=='front' else -local[...,2])-.05)/.30,0,1)
        strength=edge*frontface*inside
        color=np.array([1.,.97,.88]) if kind=='front' else np.array([.75,.015,.008])
        sl=np.s_[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
        old=output[sl]; emitted=strength[...,None]*color
        # Check white and red apertures do not address the same texels.
        if kind=='front':conflict+=int(((old[...,0]>.1)&(old[...,1]<.05)&(strength>.1)).sum())
        else:conflict+=int(((old[...,1]>.1)&(strength>.1)).sum())
        output[sl]=np.maximum(old,emitted)
# The original opaque Body includes the outer glass, hiding the separate bulbs.
# Project the same compact apertures onto that visible glass as well.
bg=next(g for g in doc.child('Objects').children if g.name=='Geometry' and len(g.value('Vertices',[]))==13909*3)
bp=bg.value('Vertices').reshape(-1,3)/100; bfs,bcs=faces(bg.value('PolygonVertexIndex'))
bul=bg.child('LayerElementUV'); buv=bul.value('UV').reshape(-1,2); bui=bul.value('UVIndex')
bm=bg.child('LayerElementMaterial').value('Materials')
atlas=np.asarray(Image.open(asset/'Texture/tripo_rgb_0e51d11d-ea82-4e43-90cb-07f3a9a32623.jpg').convert('RGB'))/255.
for fi,(f,corners) in enumerate(zip(bfs,bcs)):
    if bm[fi]!=0:continue
    fp=bp[f]
    if fp[:,1].min()>.95 or fp[:,1].max()<.56 or (fp[:,2].min()>-1.8 and fp[:,2].max()<1.6):continue
    for k in range(1,len(f)-1):
        ids=np.array([f[0],f[k],f[k+1]]);ci=np.array([corners[0],corners[k],corners[k+1]])
        tex=buv[bui[ci]]*size;a,b,c=tex
        lo=np.maximum(np.floor(tex.min(0)).astype(int),0);hi=np.minimum(np.ceil(tex.max(0)).astype(int),size-1)
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-10:continue
        xx,yy=np.meshgrid(np.arange(lo[0],hi[0]+1)+.5,np.arange(lo[1],hi[1]+1)+.5)
        wa=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
        wb=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;wc=1-wa-wb
        inside=(wa>=0)&(wb>=0)&(wc>=0)
        point=wa[...,None]*bp[ids[0]]+wb[...,None]*bp[ids[1]]+wc[...,None]*bp[ids[2]]
        emitted=np.zeros((*wa.shape,3))
        for rec in components:
            kind=rec['kind']
            if kind!='front':continue
            center=rec['center'];radius=rec['radius'];local=(point-center)/radius
            disk=np.sqrt(local[...,0]**2+local[...,1]**2)
            aperture=np.clip((.85-disk)/.13,0,1)*inside
            aperture*=point[...,2]>1.6 if kind=='front' else point[...,2]<-1.8
            color=np.array([1.,.97,.88]) if kind=='front' else np.array([.75,.015,.008])
            emitted=np.maximum(emitted,aperture[...,None]*color)
        # Rear running lights illuminate the original red lens area, leaving
        # the lower clear reverse/indicator section dark.
        # Clean continuous upper red lens. The diffuse atlas contains mottled
        # shadows, so its pixel brightness must not break the running light up.
        outline=np.array([[.530,.886],[.560,.885],[.605,.866],[.649,.832],[.627,.828],[.538,.849]])
        qx=np.abs(point[...,0]);qy=point[...,1];red_mask=np.zeros(wa.shape,bool)
        for aa,bb in zip(outline,np.roll(outline,-1,axis=0)):
            crossing=((aa[1]>qy)!=(bb[1]>qy))&(qx<(bb[0]-aa[0])*(qy-aa[1])/(bb[1]-aa[1]+1e-15)+aa[0])
            red_mask^=crossing
        rear_region=(point[...,2]<-1.8)&(np.abs(point[...,0])>.40)&(np.abs(point[...,0])<.81)&(point[...,1]>.79)&(point[...,1]<1.0)
        emitted=np.maximum(emitted,(red_mask*rear_region*inside)[...,None]*[.75,.015,.008])
        sl=np.s_[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
        output[sl]=np.maximum(output[sl],emitted)
assert conflict==0,f'Conflicting front/rear UVs: {conflict}'
result=np.uint8(np.clip(output[::-1],0,1)*255)
result=np.asarray(Image.fromarray(result).filter(ImageFilter.MaxFilter(3)))
texpath=asset/'Texture/FD_Lights_Emission.png';Image.fromarray(result).save(texpath)
def guid_for(path,template=None):
    meta=Path(str(path)+'.meta')
    if meta.exists():return re.search(r'guid: ([0-9a-f]+)',meta.read_text())[1]
    guid=uuid.uuid4().hex
    if template:
        text=Path(template).read_text(encoding='utf-8-sig');text=re.sub(r'(?m)^guid: [0-9a-f]+$',f'guid: {guid}',text)
        text=re.sub(r'(?m)^(\s*)maxTextureSize: \d+$',r'\g<1>maxTextureSize: 4096',text)
        text=re.sub(r'(?m)^(\s*)textureCompression: \d+$',r'\g<1>textureCompression: 0',text)
    else:text=f'fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    meta.write_text(text,encoding='utf-8');return guid
texguid=guid_for(texpath,str(asset/'Texture/tripo_rgb_0e51d11d-ea82-4e43-90cb-07f3a9a32623.jpg')+'.meta')
matpath=asset/'Material/FD_Lights_Emission.mat';matguid=guid_for(matpath)
mat=(asset/'Material/tripo_mat_2fc8db08.mat').read_text(encoding='utf-8-sig')
mat=mat.replace('m_Name: tripo_mat_2fc8db08','m_Name: FD_Lights_Emission')
mat=mat.replace('m_ValidKeywords: []','m_ValidKeywords:\n  - _EMISSION').replace('m_LightmapFlags: 4','m_LightmapFlags: 2')
mat,n=re.subn(r'(- _EmissionMap:\s*\n\s*m_Texture: )\{fileID: 0\}',rf'\g<1>{{fileID: 2800000, guid: {texguid}, type: 3}}',mat);assert n==1
mat,n=re.subn(r'(- _EmissionColor: )\{[^\n]+\}',r'\1{r: 4, g: 4, b: 4, a: 1}',mat);assert n==1
matpath.write_text(mat,encoding='utf-8')
text=prefab.read_text(encoding='utf-8-sig')
match=re.search(r'--- !u!23 &8835136615230305043\n.*?(?=--- !u!|\Z)',text,re.S);assert match
block=match[0];new,n=re.subn(r'(m_Materials:\s*\n\s*- \{fileID: 2100000, guid: )[0-9a-f]+(, type: 2\})',rf'\g<1>{matguid}\2',block);assert n==1
text=text[:match.start()]+new+text[match.end():];prefab.write_text(text,encoding='utf-8')
text=prefab.read_text(encoding='utf-8-sig')
# The Body's atlas slot gets the same selective map; all other slots stay intact.
blocks=list(re.finditer(r'--- !u!23 &\d+\n.*?(?=--- !u!|\Z)',text,re.S))
body_filter=re.search(r'--- !u!33 &\d+\n(?:(?!--- !u!).)*?m_Mesh: \{fileID: 2959467708922500008,',text,re.S)
assert body_filter
body_id=re.search(r'm_GameObject: \{fileID: (\d+)\}',body_filter[0])[1]
target=next(b for b in blocks if f'm_GameObject: {{fileID: {body_id}}}' in b[0])
replacement,n=re.subn(r'(m_Materials:\s*\n\s*- \{fileID: 2100000, guid: )[0-9a-f]+(, type: 2\})',rf'\g<1>{matguid}\2',target[0],count=1);assert n==1
text=text[:target.start()]+replacement+text[target.end():];prefab.write_text(text,encoding='utf-8')
for rec in components:
    count[rec['kind']]+=1;rec['center']=rec['center'].tolist();rec['radius']=rec['radius'].tolist()
report={'source':'Assets/Model Palm/FD V.2/FD.prefab','material':matpath.name,'texture':texpath.name,'textureGUID':texguid,'materialGUID':matguid,'components':components,'classification':count,'UVConflicts':conflict,'emissivePixels':int((result[...,0]>0).sum()),'DRL':False,'meshObjects':8,'triangles':30624,'geometryChanged':False}
(work/'build_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('FD_LIGHTS_SAVED',count,report['emissivePixels'])
