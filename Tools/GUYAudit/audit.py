"""Read-only FBX/import/dependency audit. Writes reports only to Tools/GUYAudit."""
from pathlib import Path
import sys, re, json, csv, hashlib, collections, time
import numpy as np
from PIL import Image
sys.path.insert(0, str(Path('Tools/FDAudit').resolve()))
from read_fbx import read

ROOT=Path('Assets/GUY'); OUT=Path('Tools/GUYAudit'); OUT.mkdir(exist_ok=True,parents=True)
def load(p): return p.read_text(encoding='utf-8-sig',errors='replace') if p.exists() else ''
def settings(p):
    text=load(Path(str(p)+'.meta'))
    fields=['isReadable','meshCompression','addColliders','generateSecondaryUV','importAnimation','animationType','importBlendShapes','importCameras','importLights','normalImportMode','tangentImportMode','generateMeshLods','mipmapEnabled','streamingMipmaps','maxTextureSize','textureCompression']
    return {k: int(m[1]) for k in fields if (m:=re.search(r'^\s+'+k+r': (-?\d+)\s*$',text,re.M))}
def guid(p):
    m=re.search(r'^guid: (\w+)',load(Path(str(p)+'.meta')),re.M)
    return m[1] if m else None
paths={}
for meta in Path('Assets').rglob('*.meta'):
    with meta.open(encoding='utf-8-sig',errors='replace') as f: header=f.read(200)
    m=re.search(r'^guid: (\w+)',header,re.M)
    if m: paths[m[1]]=Path(str(meta)[:-5])

models=[]; errors=[]
for p in sorted(ROOT.rglob('*.fbx')):
    try:
        start=time.perf_counter(); doc=read(p); objects=doc.child('Objects').children
        geometry=[n for n in objects if n.name=='Geometry' and n.props[-1]=='Mesh']
        con=doc.child('Connections'); connections=con.children if con else []
        mats=[n for n in objects if n.name=='Material']; meshrows=[]
        for mesh in geometry:
            v=mesh.value('Vertices',np.array([])); idx=mesh.value('PolygonVertexIndex',np.array([]))
            polygons=int(np.count_nonzero(idx<0)); corners=len(idx)
            instances=sum(1 for n in connections if n.props[0]=='OO' and n.props[1]==mesh.props[0])
            meshrows.append({'name':mesh.props[1].split('\0')[0],'vertices':len(v)//3,'polygons':polygons,'triangles':corners-2*polygons,'corners':corners,'instances':instances})
        videos=[n for n in objects if n.name=='Video']; textures=[n for n in objects if n.name=='Texture']
        row={'path':p.as_posix(),'MB':round(p.stat().st_size/1048576,3),
             'meshes':len(geometry),'modelNodes':sum(n.name=='Model' for n in objects),
             'materials':len(mats),'vertices':sum(m['vertices'] for m in meshrows),
             'triangles':sum(m['triangles'] for m in meshrows),
             'instancedTriangles':sum(m['triangles']*max(1,m['instances']) for m in meshrows),
             'corners':sum(m['corners'] for m in meshrows),
             'sourceAnimationCurves':sum(n.name=='AnimationCurve' for n in objects),
             'sourceAnimationKeys':sum(len(n.value('KeyTime',[])) for n in objects if n.name=='AnimationCurve'),
             'sourceSkinClusters':sum(n.name=='Deformer' and n.props[-1]=='Cluster' for n in objects),
             'embeddedMB':round(sum(len(n.value('Content',b'')) for n in videos)/1048576,3),
             'texturePaths':list(dict.fromkeys(str(n.value('RelativeFilename',n.value('FileName',n.value('Filename','')))) for n in videos+textures)),
             'settings':settings(p),'meshDetails':meshrows,
             'inspectionSeconds':round(time.perf_counter()-start,3)}
        models.append(row)
        print(p.name, row['triangles'], 'triangles,',row['meshes'],'meshes',flush=True)
        del doc, objects, geometry
    except Exception as exc:
        errors.append({'path':p.as_posix(),'error':str(exc)})

deps_cache={}
def direct(p):
    key=p.as_posix()
    if key in deps_cache:return deps_cache[key]
    text=load(p) if p.suffix.lower() in ['.mat','.prefab','.unity','.asset'] and p.stat().st_size<15000000 else ''
    text+='\n'+load(Path(str(p)+'.meta'))
    deps_cache[key]={paths[g] for g in re.findall(r'guid: ([0-9a-f]{32})',text) if g in paths and paths[g]!=p and paths[g].is_file()}
    return deps_cache[key]
def dependencies(p):
    seen=set(); todo=[p]
    while todo:
        item=todo.pop()
        for dep in direct(item):
            if dep not in seen:
                seen.add(dep)
                if dep.suffix.lower() in ['.mat','.prefab','.fbx']:todo.append(dep)
    seen.discard(p)
    return seen

textures=[]; texture_paths={p for p in ROOT.rglob('*') if p.suffix.lower() in ['.png','.jpg','.jpeg','.tga','.psd','.tif','.tiff']}
all_assets=models+[{'path':p.as_posix()} for p in ROOT.rglob('*.prefab')]
for row in all_assets:
    deps=dependencies(Path(row['path'])); row['dependencies']=sorted(p.as_posix() for p in deps)
    texture_paths.update(p for p in deps if p.suffix.lower() in ['.png','.jpg','.jpeg','.tga','.psd','.tif','.tiff'])
for p in sorted(texture_paths):
    try:
        with Image.open(p) as im:w,h=im.size;mode=im.mode
        s=settings(p); cap=s.get('maxTextureSize',2048); ratio=min(1,cap/max(w,h)); ew,eh=max(1,round(w*ratio)),max(1,round(h*ratio))
        textures.append({'path':p.as_posix(),'width':w,'height':h,'mode':mode,'MB':round(p.stat().st_size/1048576,3),
                         'effectiveWidthUpperBound':ew,'effectiveHeightUpperBound':eh,
                         'rgba32MipMiBUpperBound':round(ew*eh*4*(4/3 if s.get('mipmapEnabled',1) else 1)/1048576,2),
                         'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'settings':s})
    except Exception as exc:errors.append({'path':p.as_posix(),'error':str(exc)})
modelmap={r['path']:r for r in models}; texmap={r['path']:r for r in textures}
prefabs=[]
for asset in all_assets[len(models):]:
    p=Path(asset['path']);s=load(p);deps=asset['dependencies']
    fbx=[d for d in deps if d.lower().endswith('.fbx')]
    # Package assets are resolved separately in report.py; this is not a missing-file verdict.
    missing=[g for g in set(re.findall(r'guid: ([0-9a-f]{32})',s)) if g not in paths and not g.startswith('0000000000000000')]
    prefabs.append({**asset,'sourceModels':fbx,'uniqueSourceTrianglesNotInstanceCount':sum(modelmap[d]['triangles'] for d in fbx if d in modelmap),
                    'GameObjects':len(re.findall(r'^GameObject:',s,re.M)),
                    'MeshColliders':len(re.findall(r'^MeshCollider:',s,re.M)),
                    'Renderers':len(re.findall(r'^(?:MeshRenderer|SkinnedMeshRenderer):',s,re.M)),
                    'LODGroups':len(re.findall(r'^LODGroup:',s,re.M)),
                    'directMeshReferences':len(re.findall(r'^  m_Mesh: \{fileID: (?!0\})',s,re.M)),
                    'missingGUIDs':missing})
for row in models+prefabs:
    deps=row['dependencies']; row['externalMaterialCount']=sum(d.endswith('.mat') for d in deps)
    row['textureCount']=sum(d in texmap for d in deps)
    row['textureRGBA32MipMiBUpperBound']=round(sum(texmap[d]['rgba32MipMiBUpperBound'] for d in deps if d in texmap),2)

duplicates=collections.defaultdict(list)
for t in textures:duplicates[t['sha256']].append(t['path'])
duplicates=[v for v in duplicates.values() if len(v)>1]
logs=[]
logpaths=list(Path('Logs').glob('*.log'))
editor=Path.home()/'AppData/Local/Unity/Editor/Editor.log'
if editor.exists():logpaths.append(editor)
for p in logpaths:
    for line in load(p).splitlines():
        if 'Assets/GUY/' in line and ('seconds' in line or 'error' in line.lower() or 'warning' in line.lower()):logs.append({'log':str(p),'line':line[:2000]})
summary={'folder':ROOT.as_posix(),'models':len(models),'prefabs':len(prefabs),'texturesInspectedIncludingDependencies':len(textures),
         'sourceFBXMiB':round(sum(r['MB'] for r in models),2),'totalSourceTriangles':sum(r['triangles'] for r in models),
         'generateSecondaryUV':sum(r['settings'].get('generateSecondaryUV')==1 for r in models),
         'readableMeshes':sum(r['settings'].get('isReadable')==1 for r in models),
         'importAnimationEnabled':sum(r['settings'].get('importAnimation')==1 for r in models),
         'modelsWithAnimationCurves':sum(r['sourceAnimationCurves']>0 for r in models),
         'automaticMeshColliders':sum(r['settings'].get('addColliders')==1 for r in models),
         'prefabsWithMeshColliders':sum(r['MeshColliders']>0 for r in prefabs),
         'prefabsWithLODs':sum(r['LODGroups']>0 for r in prefabs),
         'duplicateTextureGroups':len(duplicates),'errors':errors}
report={'summary':summary,'models':models,'prefabs':prefabs,'textures':textures,'duplicateTextures':duplicates,'importLogEvidence':logs,
        'limitations':['Triangles are source FBX geometry triangulation estimates (polygon corners minus 2 per polygon), before Unity splitting/optimization.',
                       'FBX inspectionSeconds measures this Python inspection only, not Unity load/import time.',
                       'Texture RGBA32+mip sizes are uncompressed upper bounds using default maxTextureSize; platform override/compression can change runtime memory.',
                       'Prefab unique-source triangle counts are dependency totals, not rendered instance totals. No runtime profiler or visual geometry review was performed.']}
(OUT/'audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
def csvwrite(name,rows,fields):
    with (OUT/name).open('w',encoding='utf-8-sig',newline='') as f:
        writer=csv.DictWriter(f,fieldnames=fields,extrasaction='ignore');writer.writeheader();writer.writerows(rows)
csvwrite('models.csv',[{**r,**r['settings']} for r in sorted(models,key=lambda r:r['triangles'],reverse=True)],['path','MB','triangles','instancedTriangles','vertices','meshes','modelNodes','materials','externalMaterialCount','textureCount','textureRGBA32MipMiBUpperBound','generateSecondaryUV','isReadable','meshCompression','importAnimation','sourceAnimationCurves','sourceAnimationKeys','addColliders','embeddedMB'])
csvwrite('prefabs.csv',prefabs,['path','GameObjects','Renderers','MeshColliders','LODGroups','directMeshReferences','uniqueSourceTrianglesNotInstanceCount','externalMaterialCount','textureCount','textureRGBA32MipMiBUpperBound','sourceModels','missingGUIDs'])
csvwrite('textures.csv',[{**r,**r['settings']} for r in textures],['path','width','height','MB','maxTextureSize','textureCompression','isReadable','streamingMipmaps','mipmapEnabled','rgba32MipMiBUpperBound'])
print(json.dumps(summary,ensure_ascii=False,indent=2));print('TOP:',json.dumps([{k:r[k] for k in ['path','triangles','meshes','materials','MB']} for r in sorted(models,key=lambda r:r['triangles'],reverse=True)[:12]],ensure_ascii=False))
