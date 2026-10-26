"""Build a selective emission atlas from the actual lamp UVs, without mesh edits."""
from pathlib import Path
import re, json, hashlib, uuid, shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from inspect_emission_uv import mesh, basis, raster, atlas, asset, work

# Coordinates on the real lens reprojections (700 x 1000). These windows exclude
# the outer glass rim and housing. Only bright neutral pixels inside the DRL
# windows emit; the central projector aperture gets a separate controlled mask.
windows={
    'Left':{
        'drl':[
            [(310,197),(317,271),(254,281),(209,330),(187,430),(187,545),(207,645),(247,710),(241,749),(193,736),(151,644),(128,552),(127,423),(143,332),(179,257),(240,217)],
            [(533,268),(575,283),(624,351),(652,440),(658,570),(639,684),(592,752),(539,798),(488,818),(477,776),(539,726),(590,642),(608,541),(600,436),(570,354),(536,323)]],
        'projector':[(337,403),(454,439),(476,565),(447,600),(338,571),(312,486)]},
    'Right':{
        'drl':[
            [(180,259),(235,277),(191,329),(153,405),(129,492),(124,574),(148,672),(185,720),(252,749),(259,802),(216,796),(150,754),(106,691),(83,600),(85,480),(111,378),(147,295)],
            [(450,183),(505,199),(566,231),(602,291),(620,385),(613,479),(593,589),(563,688),(526,720),(505,691),(534,608),(555,513),(574,404),(569,313),(534,272),(477,249),(443,246)]],
        'projector':[(294,413),(411,375),(438,423),(413,550),(385,580),(292,598),(271,546)]}
}

size=atlas.shape[0]
assert atlas.shape[:2]==(4096,4096)
mask=np.zeros((size,size),np.float32)
coverage=np.zeros((size,size),bool)
DRL_LINEAR_STRENGTH=.35
for side,regions in windows.items():
    p,uv,tri=mesh(side); origin,right,up,normal=basis(p)
    q=np.stack(((p-origin)@right,(p-origin)@up),axis=-1)
    xy=(q-q.min(0))/(q.max(0)-q.min(0))*[640,940]+30
    drl=Image.new('L',(700,1000)); draw=ImageDraw.Draw(drl)
    for poly in regions['drl']: draw.polygon(poly,fill=255)
    aperture=Image.new('L',(700,1000)); ImageDraw.Draw(aperture).polygon(regions['projector'],fill=255)
    drl=np.asarray(drl.filter(ImageFilter.GaussianBlur(1.5)))/255.
    aperture=np.asarray(aperture.filter(ImageFilter.GaussianBlur(2)))/255.
    for t in tri:
        pts=uv[t]*size; a,b,c=pts
        lo=np.maximum(np.floor(pts.min(0)).astype(int),0); hi=np.minimum(np.ceil(pts.max(0)).astype(int),size-1)
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-10: continue
        xx,yy=np.meshgrid(np.arange(lo[0],hi[0]+1)+.5,np.arange(lo[1],hi[1]+1)+.5)
        w0=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
        w1=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den; w2=1-w0-w1
        inside=(w0>=0)&(w1>=0)&(w2>=0)
        imagexy=w0[...,None]*xy[t[0]]+w1[...,None]*xy[t[1]]+w2[...,None]*xy[t[2]]
        sx=np.clip(imagexy[...,0].astype(int),0,699)
        sy=np.clip((999-imagexy[...,1]).astype(int),0,999)
        # atlas is top-down whereas Unity UV0 is bottom-up.
        rgb=atlas[size-1-np.clip(yy.astype(int),0,size-1),np.clip(xx.astype(int),0,size-1)].astype(float)/255.
        neutral=np.clip(1-(rgb.max(-1)-rgb.min(-1))/.38,0,1)
        luminance=rgb.mean(-1)
        bright=np.clip((luminance-.48)/.24,0,1)
        drl_srgb=drl[sy,sx]*bright*neutral
        # The emission texture is sRGB. Attenuate in linear light so 35% means
        # optical intensity, rather than multiplying encoded pixel values.
        drl_linear=np.where(drl_srgb<=.04045,drl_srgb/12.92,((drl_srgb+.055)/1.055)**2.4)*DRL_LINEAR_STRENGTH
        drl_dimmed=np.where(drl_linear<=.0031308,drl_linear*12.92,1.055*drl_linear**(1/2.4)-.055)
        value=np.maximum(drl_dimmed,aperture[sy,sx]*(.8+.2*luminance))
        sl=np.s_[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
        mask[sl]=np.maximum(mask[sl],value*inside); coverage[sl]|=inside

# Two pixels of padding prevent a dark mip seam inside an emissive feature.
gray=Image.fromarray(np.uint8(np.clip(mask[::-1],0,1)*255)).filter(ImageFilter.MaxFilter(3))
rgb=np.repeat(np.asarray(gray)[...,None],3,axis=-1)
target=asset/'Texture/Headlights_EmissionMask.png'
Image.fromarray(rgb).save(target)
meta=target.with_name(target.name+'.meta')
if meta.exists(): guid=re.search(r'guid: ([0-9a-f]+)',meta.read_text())[1]
else:
    guid=uuid.uuid4().hex
    original_meta=(asset/'Texture/tripo_rgb_62867c90-c4b3-4b96-b7c1-0cf4f20ce93e.jpg.meta').read_text(encoding='utf-8-sig')
    original_meta=re.sub(r'(?m)^guid: [0-9a-f]+$',f'guid: {guid}',original_meta)
    original_meta=re.sub(r'(?m)^(\s*)maxTextureSize: \d+$',r'\g<1>maxTextureSize: 4096',original_meta)
    original_meta=re.sub(r'(?m)^(\s*)textureCompression: \d+$',r'\g<1>textureCompression: 0',original_meta)
    meta.write_text(original_meta,encoding='utf-8')
material=asset/'Material/Headlights.mat'
backup=work/'Headlights_before_selective_emission.mat'
if not backup.exists(): shutil.copyfile(material,backup)
text=material.read_text(encoding='utf-8-sig')
text,n=re.subn(r'(- _EmissionMap:\s*\n\s*m_Texture: \{fileID: 2800000, guid: )[0-9a-f]+(, type: 3\})',rf'\g<1>{guid}\2',text)
assert n==1
# HDR 3 gives clear white DRLs/projector while the black map keeps glass dark.
text,n=re.subn(r'(- _EmissionColor: )\{[^\n]+\}',r'\1{r: 3, g: 3, b: 3, a: 1}',text)
assert n==1
material.write_text(text,encoding='utf-8')
for side in windows: raster(side,rgb)
report={'material':str(material.relative_to(asset)),'emissionMap':target.name,'emissionMapGUID':guid,'size':list(rgb.shape[:2]),'emissivePixels':int(np.count_nonzero(rgb[:,:,0])),'geometryChanged':False,'baseMapChanged':False,'HDR':[3,3,3],'drlLinearStrength':DRL_LINEAR_STRENGTH,'projectorStrengthUnchanged':True,'regions':windows}
(work/'mask_validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('SELECTIVE_EMISSION_SAVED',report['emissivePixels'])
