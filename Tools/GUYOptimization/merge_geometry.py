"""Patch only changed Geometry contents into original FBX; retain IDs/hierarchy/materials."""
from pathlib import Path
import sys,json,struct,hashlib,shutil,os
import numpy as np
sys.path.insert(0,str(Path('Tools/FDAudit').resolve()))
from read_fbx import read
OUT=Path(os.environ.get('GUY_OPT_OUTPUT','Tools/GUYOptimization'));config=json.loads((OUT/'config.json').read_text())

def source(path):
    raw=Path(path).read_bytes();doc=read(path);version=struct.unpack_from('<I',raw,23)[0];wide=version>=7500
    fmt='<QQQB' if wide else '<IIIB';header=25 if wide else 13
    def attach(n,offset):
        end,count,length,nlen=struct.unpack_from(fmt,raw,offset)
        begin=offset+header+nlen;n.rawprops=raw[begin:begin+length];n.count=count
        cursor=begin+length
        for child in n.children:cursor=attach(child,cursor)
        n.tail=raw[cursor:end];return end
    cursor=27
    for child in doc.children:cursor=attach(child,cursor)
    return doc,raw[:27],raw[cursor:],fmt,header
def write(path,doc,prefix,footer,fmt,header):
    def encode(n,start):
        name=n.name.encode();cursor=start+header+len(name)+len(n.rawprops);children=[]
        for child in n.children:
            chunk=encode(child,cursor);children.append(chunk);cursor+=len(chunk)
        cursor+=len(n.tail)
        return struct.pack(fmt,cursor,n.count,len(n.rawprops),len(name))+name+n.rawprops+b''.join(children)+n.tail
    data=[prefix];cursor=len(prefix)
    for n in doc.children:
        chunk=encode(n,cursor);data.append(chunk);cursor+=len(chunk)
    Path(path).write_bytes(b''.join(data)+footer)
def mappings(doc):
    objects=doc.child('Objects').children;models={n.props[0]:n for n in objects if n.name=='Model'}
    mats={n.props[0]:n.props[1].split('\0')[0] for n in objects if n.name=='Material'}
    con=[n.props for n in doc.child('Connections').children]
    result={}
    for n in objects:
        if n.name!='Geometry' or n.props[-1]!='Mesh':continue
        model=next(models[c[2]] for c in con if c[0]=='OO' and c[1]==n.props[0] and c[2] in models)
        name=model.props[1].split('\0')[0]
        slots=[mats[c[1]] for c in con if c[0]=='OO' and c[2]==model.props[0] and c[1] in mats]
        result[name]=(n,slots)
    return result
results=[]
for index,item in enumerate(config):
    folder=OUT/('Model'+str(index));r=json.loads((folder/'result.json').read_text());assert r['done']
    p=Path(item['path']); backup=OUT/'Backups'/p
    backup.parent.mkdir(parents=True,exist_ok=True)
    if not backup.exists():shutil.copy2(p,backup);shutil.copy2(Path(str(p)+'.meta'),Path(str(backup)+'.meta'))
    assert p.read_bytes()==backup.read_bytes(),'Source changed since backup: '+str(p)
    doc,prefix,footer,fmt,header=source(backup);optimized,_,_,fmt2,_=source(folder/'optimized.fbx');assert fmt==fmt2
    orig=mappings(doc);new=mappings(optimized);assert orig.keys()==new.keys()
    changed={m['name'] for m in r['meshes'] if m['changed']}
    for name in changed:
        a,slots=orig[name];b,newslots=new[name]
        assert slots==newslots,(name,slots,newslots)
        av=a.value('Vertices').reshape(-1,3);bv=b.value('Vertices').reshape(-1,3)
        diagonal=np.linalg.norm(av.max(0)-av.min(0));error=max(np.max(np.abs(av.max(0)-bv.max(0))),np.max(np.abs(av.min(0)-bv.min(0))))
        assert error<=diagonal*.003,(name,error,diagonal)
        assert np.isfinite(bv).all()
        oldUV=[n for n in a.children if n.name=='LayerElementUV'];newUV=[n for n in b.children if n.name=='LayerElementUV']
        assert len(oldUV)==len(newUV),(name,'UV count')
        a.children=b.children  # keep the original Geometry identity and every non-geometry node
    output=folder/'optimized-preserved.fbx';write(output,doc,prefix,footer,fmt,header)
    check=read(output);updated=mappings(check)
    triangles=sum(len(n.value('PolygonVertexIndex'))-2*int(np.count_nonzero(n.value('PolygonVertexIndex')<0)) for n,_ in updated.values())
    assert triangles==r['after'],(triangles,r['after'])
    # Verify original non-Geometry properties/IDs remained byte-identical after serialization.
    parsed,_,_,_,_=source(output)
    def signatures(n,path=''):
        key=path+'/'+n.name+('/'+str(n.props[0]) if n.name in ['Model','Geometry','Material','Texture','Video'] and n.props else '')
        result=[(key,getattr(n,'rawprops',b''))]
        if not (n.name=='Geometry' and n.props[-1]=='Mesh'):
            for j,c in enumerate(n.children):result+=signatures(c,key+':'+str(j))
        return result
    assert signatures(doc)==signatures(parsed)
    originaldoc=source(backup)[0];assert signatures(originaldoc)==signatures(parsed)
    results.append({'path':item['path'],'originalSHA256':hashlib.sha256(backup.read_bytes()).hexdigest(),
                    'optimizedSHA256':hashlib.sha256(output.read_bytes()).hexdigest(),
                    'beforeTriangles':r['before'],'afterTriangles':triangles,'beforeBytes':backup.stat().st_size,
                    'afterBytes':output.stat().st_size,'geometryIDsPreserved':True,'allNonGeometryPropertiesPreserved':True,
                    'changedMeshes':sorted(changed),'output':str(output),'backup':str(backup)})
    print(p.name,r['before'],'->',triangles,flush=True)
(OUT/'merge-report.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
