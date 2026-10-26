"""Read-only binary FBX inspection. Emits reports only under Tools/FDAudit."""
from pathlib import Path
import struct,zlib,json
import numpy as np

class Node:
    def __init__(self,name,props,children): self.name,self.props,self.children=name,props,children
    def child(self,name): return next((n for n in self.children if n.name==name),None)
    def value(self,name,default=None):
        n=self.child(name); return n.props[0] if n and n.props else default

def read(path):
    data=Path(path).read_bytes(); version=struct.unpack_from('<I',data,23)[0]; wide=version>=7500; header=25 if wide else 13
    def prop(offset):
        kind=chr(data[offset]); offset+=1
        types={'Y':'h','C':'?','I':'i','F':'f','D':'d','L':'q'}
        if kind in types:
            fmt='<'+types[kind]; return struct.unpack_from(fmt,data,offset)[0],offset+struct.calcsize(fmt)
        if kind in 'SR':
            n=struct.unpack_from('<I',data,offset)[0]; offset+=4; v=data[offset:offset+n]
            return (v.decode('utf-8','replace') if kind=='S' else v),offset+n
        if kind in 'fdlibc':
            n,encoded,length=struct.unpack_from('<III',data,offset); offset+=12
            raw=data[offset:offset+length]; raw=zlib.decompress(raw) if encoded else raw
            dtype={'f':'<f4','d':'<f8','l':'<i8','i':'<i4','b':'u1','c':'u1'}[kind]
            v=np.frombuffer(raw,dtype=dtype); assert len(v)==n
            return v,offset+length
        raise ValueError(kind)
    def node(offset):
        fmt='<QQQB' if wide else '<IIIB'; end,count,length,nlen=struct.unpack_from(fmt,data,offset)
        if end==0: return None,offset+header
        cursor=offset+header; name=data[cursor:cursor+nlen].decode(); cursor+=nlen; props=[]
        for i in range(count): value,cursor=prop(cursor); props.append(value)
        children=[]
        while cursor<end:
            child,cursor=node(cursor)
            if child is None: break
            children.append(child)
        assert cursor==end,(name,cursor,end)
        return Node(name,props,children),end
    nodes=[]; cursor=27
    while cursor<len(data):
        n,cursor=node(cursor)
        if n is None: break
        nodes.append(n)
    return Node('ROOT',[],nodes)

def properties(node):
    p=node.child('Properties70')
    return {n.props[0]:n.props[4:] for n in p.children} if p else {}

def faces(indices):
    result=[]; current=[]; corners=[]; cc=[]
    for i,value in enumerate(indices):
        current.append(int(-value-1 if value<0 else value)); cc.append(i)
        if value<0: result.append(current); corners.append(cc); current=[]; cc=[]
    assert not current
    return result,corners

def inspect(path):
    doc=read(path); objects=doc.child('Objects').children
    con=doc.child('Connections'); connections=[n.props for n in con.children]
    models={n.props[0]:n for n in objects if n.name=='Model'}
    materials={n.props[0]:n for n in objects if n.name=='Material'}
    output=[]
    for n in objects:
        if n.name!='Geometry' or n.props[-1]!='Mesh': continue
        p=n.value('Vertices').reshape(-1,3); fs,cs=faces(n.value('PolygonVertexIndex'))
        modelids=[c[2] for c in connections if c[0]=='OO' and c[1]==n.props[0] and c[2] in models]
        model=models[modelids[0]] if modelids else None
        mats=[materials[c[1]].props[1].split('\x00')[0] for c in connections if c[0]=='OO' and model and c[2]==model.props[0] and c[1] in materials]
        output.append({'id':n.props[0],'name':model.props[1].split('\x00')[0] if model else n.props[1].split('\x00')[0],
            'vertices':len(p),'polygons':len(fs),'triangles':sum(len(f)-2 for f in fs),'boundsMin':p.min(0).tolist(),'boundsMax':p.max(0).tolist(),
            'materials':mats,'modelProperties':properties(model) if model else {},
            'layers':{k:[{'name':c.name,'props':c.props if not any(isinstance(x,np.ndarray) for x in c.props) else [f'array({len(c.props[0])})']} for c in layer.children] for k in ['LayerElementNormal','LayerElementUV','LayerElementMaterial','LayerElementSmoothing'] if (layer:=n.child(k))}})
    return doc,output

if __name__=='__main__':
    root=Path(__file__).resolve().parents[2]
    for folder,file,tag in [('FD V.2','FD.fbx','FDV2'),('FD','FD2.fbx','FD_original')]:
        doc,out=inspect(root/'Assets/Model Palm'/folder/file)
        report={'source':folder+'/'+file,'globalSettings':properties(doc.child('GlobalSettings')),'meshes':out,'totalTriangles':sum(o['triangles'] for o in out)}
        (Path(__file__).parent/(tag+'_inspection.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
        print(tag,report['totalTriangles'])
        for o in out: print(o['name'],o['vertices'],o['triangles'],o['materials'],o['boundsMin'],o['boundsMax'],o['modelProperties'])
