"""Build four clean standalone FD lamp surfaces. No car texture is used by them."""
from pathlib import Path
import re,json,hashlib,uuid,shutil
from collections import Counter
import numpy as np
from read_fbx import read,faces

root=Path(__file__).resolve().parents[2]; asset=root/'Assets/Model Palm/FD V.2'
work=Path(__file__).resolve().parent/'IndependentLights';work.mkdir(exist_ok=True)
source=asset/'FD.fbx'; prefab=asset/'FD.prefab'
for file in [prefab,source,Path(str(source)+'.meta')]:
    target=work/(file.name+'.before')
    if not target.exists():shutil.copyfile(file,target)
doc=read(source);geometries=[g for g in doc.child('Objects').children if g.name=='Geometry']
body=next(g for g in geometries if len(g.value('Vertices',[]))==13909*3)
points=body.value('Vertices').reshape(-1,3)/100; fs,_=faces(body.value('PolygonVertexIndex'))
triangles=np.array([[f[0],f[k],f[k+1]] for f in fs for k in range(1,len(f)-1)])
bt=points[triangles]

def guid(path,fileid):
    meta=Path(str(path)+'.meta')
    if meta.exists():return re.search(r'guid: ([0-9a-f]+)',meta.read_text())[1]
    value=uuid.uuid4().hex
    meta.write_text(f'fileFormatVersion: 2\nguid: {value}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: {fileid}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8');return value

meshdir=asset/'Meshes';meshdir.mkdir(exist_ok=True)
foldermeta=Path(str(meshdir)+'.meta')
if not foldermeta.exists():foldermeta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')

def ray_depth(xy,front):
    low=xy.min(0);high=xy.max(0)
    keep=(bt[:,:,0].max(1)>=low[0])&(bt[:,:,0].min(1)<=high[0])&(bt[:,:,1].max(1)>=low[1])&(bt[:,:,1].min(1)<=high[1])
    keep&=bt[:,:,2].max(1)>1.5 if front else bt[:,:,2].min(1)<-1.7
    depth=np.full(len(xy),-np.inf if front else np.inf)
    for a,b,c in bt[keep]:
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-12:continue
        wa=((b[1]-c[1])*(xy[:,0]-c[0])+(c[0]-b[0])*(xy[:,1]-c[1]))/den
        wb=((c[1]-a[1])*(xy[:,0]-c[0])+(a[0]-c[0])*(xy[:,1]-c[1]))/den;wc=1-wa-wb
        hit=(wa>=-1e-7)&(wb>=-1e-7)&(wc>=-1e-7)
        z=wa*a[2]+wb*b[2]+wc*c[2]
        if front:depth[hit]=np.maximum(depth[hit],z[hit])
        else:depth[hit]=np.minimum(depth[hit],z[hit])
    return depth

def features(xy,center):
    x,y=(xy-center).T
    return np.stack((np.ones(len(x)),x,y,x*x,x*y,y*y),axis=-1)

def pick_outline(pixels,eye,target,scale):
    """Transfer a traced housing contour from the diagnostic camera to FBX XY."""
    eye=np.asarray(eye);target=np.asarray(target);forward=target-eye;forward/=np.linalg.norm(forward)
    right=np.cross(forward,[0,1,0]);right/=np.linalg.norm(right);up=np.cross(right,forward)
    rel=bt-target;projected=np.stack((rel@right,rel@up),axis=-1)*(760/scale)+[550,380]
    query=np.asarray(pixels,dtype=float);query[:,1]=759-query[:,1];query+=.5
    depth=np.full(len(query),-np.inf);hits=np.zeros((len(query),3))
    for pos,screen in zip(bt,projected):
        a,b,c=screen;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-12:continue
        wa=((b[1]-c[1])*(query[:,0]-c[0])+(c[0]-b[0])*(query[:,1]-c[1]))/den
        wb=((c[1]-a[1])*(query[:,0]-c[0])+(a[0]-c[0])*(query[:,1]-c[1]))/den;wc=1-wa-wb
        p=wa[:,None]*pos[0]+wb[:,None]*pos[1]+wc[:,None]*pos[2]
        score=-((p-target)@forward);keep=(wa>=0)&(wb>=0)&(wc>=0)&(score>depth)
        depth[keep]=score[keep];hits[keep]=p[keep]
    assert np.isfinite(depth).all(),'Housing contour missed the source mesh'
    return hits[:,:2]

def rounded_outline(poly,corner=.0012,spacing=.010):
    """Small corner rounds preserve the marked housing area instead of ovalizing it."""
    poly=np.asarray(poly);result=[]
    for i,p in enumerate(poly):
        prev=poly[i-1];nxt=poly[(i+1)%len(poly)]
        before=p+(prev-p)*min(corner/np.linalg.norm(prev-p),.15)
        after=p+(nxt-p)*min(corner/np.linalg.norm(nxt-p),.15)
        pn=poly[(i+2)%len(poly)]
        next_before=nxt+(p-nxt)*min(corner/np.linalg.norm(p-nxt),.15)
        for t in np.linspace(0,1,4,endpoint=False):result.append((1-t)**2*before+2*(1-t)*t*p+t*t*after)
        for t in np.linspace(0,1,max(1,int(np.linalg.norm(next_before-after)/spacing)),endpoint=False):result.append(after*(1-t)+next_before*t)
    return np.asarray(result)

def emitter(name,outline,front):
    # Smooth a support surface analytically, rather than copying the folded mesh.
    center=outline.mean(0);n=len(outline);sign=1 if front else -1
    sample=np.concatenate([center[None,:]+r*(outline-center) for r in np.linspace(0,1,16)])
    depth=ray_depth(sample,front);valid=np.isfinite(depth)
    assert valid.mean()>.90,(name,float(valid.mean()))
    coeff=np.linalg.lstsq(features(sample[valid],center),depth[valid],rcond=None)[0]
    fit=features(sample[valid],center)@coeff
    offset=max(float(np.max((depth[valid]-fit)*sign)),0)+.0015
    # Four radial rings with a gently domed face, sealed back and thin side wall.
    xy=[center];radial=[0.]
    for ring in range(1,5):
        for v in outline:xy.append(center+(v-center)*ring/4);radial.append(ring/4)
    xy=np.array(xy);radial=np.array(radial)
    direct=ray_depth(xy,front);known=np.isfinite(direct)
    direct[~known]=features(xy[~known],center)@coeff
    z=direct+sign*(.0015+.0005*(1-radial**2))
    p=np.c_[xy,z];t=[]
    def add(a,b,c,direction):
        normal=np.cross(p[b]-p[a],p[c]-p[a])
        if normal@np.asarray(direction)<0:b,c=c,b
        t.append([a,b,c])
    for j in range(n):add(0,1+j,1+(j+1)%n,[0,0,sign])
    for ring in range(1,4):
        a=1+(ring-1)*n;b=1+ring*n
        for j in range(n):
            j2=(j+1)%n;add(a+j,b+j,b+j2,[0,0,sign]);add(a+j,b+j2,a+j2,[0,0,sign])
    # Fit the expanded cover locally; a single globally lifted polynomial would
    # float off this strongly swept housing. Verify interior points of each face.
    front_t=np.array(t,int)
    weights=np.array([[1/3,1/3,1/3],[.6,.2,.2],[.2,.6,.2],[.2,.2,.6],[.5,.5,0],[0,.5,.5],[.5,0,.5]])
    for _ in range(3):
        test=np.einsum('sk,fkj->fsj',weights,p[front_t]);actual=ray_depth(test[:,:,:2].reshape(-1,2),front).reshape(test.shape[:2])
        needed=(actual-test[:,:,2])*sign+.001
        needed[~np.isfinite(needed)]=0;needed=np.maximum(needed.max(1),0)
        if needed.max()<.00005:break
        lift=np.zeros(len(p))
        for k in range(3):np.maximum.at(lift,front_t[:,k],needed)
        p[:,2]+=sign*lift
    offset=float(np.max((p[:,2]-direct)*sign))
    outer=1+3*n;back=len(p)
    backvertices=p.copy();backvertices[:,2]-=sign*.0018
    p=np.vstack((p,backvertices))
    for tri in front_t:t.append([int(tri[0])+back,int(tri[2])+back,int(tri[1])+back])
    for j in range(n):
        j2=(j+1)%n;direction=np.r_[(outline[j]+outline[j2])/2-center,0]
        add(outer+j,back+outer+j,back+outer+j2,direction);add(outer+j,back+outer+j2,outer+j2,direction)
    t=np.asarray(t,int)
    normals=np.zeros_like(p);face=np.cross(p[t[:,1]]-p[t[:,0]],p[t[:,2]]-p[t[:,0]])
    for k in range(3):np.add.at(normals,t[:,k],face)
    normals/=np.maximum(np.linalg.norm(normals,axis=1,keepdims=True),1e-12)
    # Unity's FBX importer reflects source X. Checked against the Porsche mesh.
    pu=p*np.array([-1,1,1]);nu=normals*np.array([-1,1,1]);tu=t[:,[0,2,1]]
    uv=(p[:,:2]-outline.min(0))/(outline.max(0)-outline.min(0))
    tangent=np.tile([-1.,0,0],(len(p),1));tangent-=nu*np.sum(tangent*nu,axis=1,keepdims=True)
    bad=np.linalg.norm(tangent,axis=1)<1e-6
    fallback=np.tile([0.,1,0],(bad.sum(),1));fallback-=nu[bad]*np.sum(fallback*nu[bad],axis=1,keepdims=True);tangent[bad]=fallback
    tangent/=np.linalg.norm(tangent,axis=1,keepdims=True)
    handed=np.sign(np.cross(nu,tangent)[:,1]);handed[handed==0]=1
    data=np.zeros((len(p),14),dtype='<f4');data[:,:3]=pu;data[:,3:6]=nu;data[:,6:9]=tangent;data[:,9]=handed;data[:,10:12]=uv
    template=(root/'Assets/Model Palm/Porsche V.2/Meshes/Headlight_Left.asset').read_text(encoding='utf-8-sig')
    text=re.sub(r'(?m)^  m_Name: .+$',f'  m_Name: {name}',template)
    text=re.sub(r'(?m)^(\s*)indexCount: \d+$',rf'\g<1>indexCount: {tu.size}',text)
    text=re.sub(r'(?m)^(\s*)vertexCount: \d+$',rf'\g<1>vertexCount: {len(p)}',text)
    text=re.sub(r'm_IndexBuffer: [0-9a-f]+',f'm_IndexBuffer: {tu.astype("<u2").tobytes().hex()}',text)
    text=re.sub(r'm_VertexCount: \d+',f'm_VertexCount: {len(p)}',text)
    text=re.sub(r'm_DataSize: \d+',f'm_DataSize: {data.nbytes}',text)
    text=re.sub(r'_typelessdata: [0-9a-f]+',f'_typelessdata: {data.tobytes().hex()}',text)
    mn=pu.min(0);mx=pu.max(0);mid=(mn+mx)/2;ext=(mx-mn)/2
    vec=lambda v:'{x: '+format(v[0],'.9g')+', y: '+format(v[1],'.9g')+', z: '+format(v[2],'.9g')+'}'
    text=re.sub(r'm_Center: \{[^\n]+\}',f'm_Center: {vec(mid)}',text)
    text=re.sub(r'm_Extent: \{[^\n]+\}',f'm_Extent: {vec(ext)}',text)
    path=meshdir/(name+'.asset');path.write_text(text,encoding='utf-8');meshguid=guid(path,4300000)
    edges=Counter(tuple(sorted((int(a),int(b)))) for tri in t for a,b in zip(tri,np.roll(tri,-1)))
    assert all(c==2 for c in edges.values())
    areas=np.linalg.norm(face,axis=1)/2;assert areas.min()>1e-12
    volume=float(np.sum(np.einsum('ij,ij->i',p[t[:,0]],np.cross(p[t[:,1]],p[t[:,2]])))/6);assert volume>0
    np.savez_compressed(work/(name+'.npz'),positions=p,normals=normals,triangles=t,uv=uv)
    return {'name':name,'meshGUID':meshguid,'meshPath':str(path.relative_to(root)).replace('\\','/'),'vertices':len(p),'triangles':len(t),'closedManifold':True,'minTriangleArea':float(areas.min()),'volume':volume,'surfaceFitMaxOffset':offset,'surfaceRayCoverage':float(valid.mean()),'front':front}

lamps=[]
front_trace=pick_outline([[359,268],[451,295],[547,324],[640,352],[696,478],[622,465],[559,455],[517,454],[480,458],[443,465],[410,457],[386,440],[373,412],[363,366]],[-.5,.9,4],[-.55,.65,1.9],.7)
rear_trace=pick_outline([[476,304],[534,310],[608,325],[648,345],[701,392],[698,399],[451,388]],[-.55,1,-4],[-.53,.84,-2],.6)
front_trace[:,0]=np.abs(front_trace[:,0]);rear_trace[:,0]=np.abs(rear_trace[:,0])
(work/'full_lens_outlines.json').write_text(json.dumps({'headlightXY':front_trace.tolist(),'taillightXY':rear_trace.tolist(),'source':'Full housing contour matching user painted regions'},indent=2),encoding='utf-8')
for unityside,sourcesign in [('Left',1),('Right',-1)]:
    outline=rounded_outline(front_trace);outline[:,0]*=sourcesign
    lamps.append(emitter('FD_Headlight_Emission_'+unityside,outline,True))
    poly=rounded_outline(rear_trace)
    poly[:,0]*=sourcesign
    lamps.append(emitter('FD_Taillight_Emission_'+unityside,poly,False))

materialguids={}
for kind,color in [('Headlights',[4,3.85,3.5]),('Taillights',[2.5,.006,.003])]:
    path=asset/f'Material/FD_{kind}_Independent_Emission.mat'
    if path.exists():
        materialguids[kind]=guid(path,2100000)
        continue
    mat=(asset/'Material/tripo_mat_2fc8db08.mat').read_text(encoding='utf-8-sig')
    mat=mat.replace('m_Name: tripo_mat_2fc8db08',f'm_Name: FD_{kind}_Independent_Emission').replace('m_ValidKeywords: []','m_ValidKeywords:\n  - _EMISSION').replace('m_LightmapFlags: 4','m_LightmapFlags: 2')
    mat=re.sub(r'm_Texture: \{[^\n]+\}','m_Texture: {fileID: 0}',mat)
    for prop in ['_BaseColor','_Color']:
        mat=re.sub(r'(- '+prop+r': )\{[^\n]+\}',r'\1{r: 0.006, g: 0.006, b: 0.006, a: 1}',mat)
    mat=re.sub(r'(- _EmissionColor: )\{[^\n]+\}',r'\g<1>{r: '+str(color[0])+', g: '+str(color[1])+', b: '+str(color[2])+', a: 1}',mat)
    path.write_text(mat,encoding='utf-8');materialguids[kind]=guid(path,2100000)

text=prefab.read_text(encoding='utf-8-sig')
# Undo only the prior atlas material binding, keeping every source block intact.
legacy=asset/'Material/FD_Lights_Emission.mat.meta'
if legacy.exists():
    oldguid=re.search(r'guid: ([0-9a-f]+)',legacy.read_text())[1]
    text=text.replace(oldguid,'0325d93ed7798c948be090f5ae6bd3e6')
def fid(label):return int.from_bytes(hashlib.sha256(('FD-independent:'+label).encode()).digest()[:8],'little')&0x7fffffffffffffff
rootid=7600716200271561881
bodyfilter=re.search(r'--- !u!33 &3984807753046274885\n.*?(?=--- !u!|\Z)',text,re.S)[0]
bodyrenderer=re.search(r'--- !u!23 &1753977182908596650\n.*?(?=--- !u!|\Z)',text,re.S)[0]
ownedids={fid(l['name']+suffix) for l in lamps for suffix in ['go','transform','filter','renderer']}
for id in ownedids:text=re.sub(r'--- !u!\d+ &'+str(id)+r'\n.*?(?=--- !u!|\Z)','',text,flags=re.S)
for l in lamps:text=text.replace('  - {fileID: '+str(fid(l['name']+'transform'))+'}\n','')
children=''.join('  - {fileID: '+str(fid(l['name']+'transform'))+'}\n' for l in lamps)
rootblock=re.search(r'--- !u!4 &'+str(rootid)+r'\n.*?(?=--- !u!|\Z)',text,re.S)
replacement=rootblock[0].replace('  m_Father: {fileID: 0}',children+'  m_Father: {fileID: 0}')
text=text[:rootblock.start()]+replacement+text[rootblock.end():]
for l in lamps:
    name=l['name'];go,tr,mf,mr=[fid(name+s) for s in ['go','transform','filter','renderer']]
    game=f'''--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr}}}
  - component: {{fileID: {mf}}}
  - component: {{fileID: {mr}}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tr}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {rootid}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
'''
    fil=bodyfilter.replace('&3984807753046274885','&'+str(mf)).replace('m_GameObject: {fileID: 8705988004067251947}',f'm_GameObject: {{fileID: {go}}}')
    fil=re.sub(r'm_Mesh: \{[^\n]+\}',f'm_Mesh: {{fileID: 4300000, guid: {l["meshGUID"]}, type: 2}}',fil)
    ren=bodyrenderer.replace('&1753977182908596650','&'+str(mr)).replace('m_GameObject: {fileID: 8705988004067251947}',f'm_GameObject: {{fileID: {go}}}')
    mg=materialguids['Headlights' if l['front'] else 'Taillights']
    ren=re.sub(r'  m_Materials:\n(?:  - \{[^\n]+\}\n)+',f'  m_Materials:\n  - {{fileID: 2100000, guid: {mg}, type: 2}}\n',ren)
    ren=ren.replace('m_CastShadows: 1','m_CastShadows: 0').replace('m_ReceiveShadows: 1','m_ReceiveShadows: 0')
    text+=game+fil+ren
prefab.write_text(text,encoding='utf-8')
report={'lamps':lamps,'materials':materialguids,'newTriangles':sum(l['triangles'] for l in lamps),'originalTriangles':30624,'finalTriangles':30624+sum(l['triangles'] for l in lamps),'originalMeshObjects':8,'finalMeshObjects':12,'newTextureCount':0,'usesCarTextures':False,'coordinateConversion':'FBX centimetres to metres and X reflection, triangle winding reversed'}
(work/'build_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('FD_INDEPENDENT_LIGHTS_SAVED',report['newTriangles'],[(l['name'],round(l['surfaceFitMaxOffset'],4)) for l in lamps])
