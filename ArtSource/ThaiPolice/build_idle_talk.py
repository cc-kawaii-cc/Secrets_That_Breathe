"""Create a police skin on the original IdleTalk FBX without re-exporting its rig.

Both characters use the same 484-vertex base mesh. Only the UV layer is changed;
all skeleton IDs, bind poses, animation curves, object names and transforms survive.
"""
from pathlib import Path
import sys, re, uuid, json, hashlib, copy
import numpy as np

PROJECT = Path(__file__).resolve().parents[2]
sys.path.insert(0, r'C:/Program Files/Blender Foundation/Blender 5.2/5.2/scripts/addons_core/io_scene_fbx')
from io_scene_fbx import parse_fbx, encode_bin

SOURCE = PROJECT/'Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk'
OUT = PROJECT/'Assets/SecretsThatBreathe/Characters/ThaiPolice/IdleTalk'
OUT.mkdir(exist_ok=True)

def child(node, name): return next(e for e in node.elems if e.id == name)
def make_guid(path):
    meta = Path(str(path)+'.meta')
    if meta.exists(): return re.search(r'guid: (\w+)', meta.read_text())[1]
    return uuid.uuid5(uuid.NAMESPACE_URL, 'SecretsThatBreathe/PoliceIdle/'+path.name).hex

root, version = parse_fbx.parse(str(SOURCE/'LowPolyMale_IdleTalk.fbx'))
baseline = copy.deepcopy(root)
objects = child(root, b'Objects')
geometry = next(e for e in objects.elems if e.id == b'Geometry' and e.props[-1] == b'Mesh')
positions = np.array(child(geometry,b'Vertices').props[0]).reshape(-1,3)
indices = child(geometry,b'PolygonVertexIndex').props[0]
uv_layer = child(geometry,b'LayerElementUV')
uv = np.array(child(uv_layer,b'UV').props[0]).reshape(-1,2)
uv_indices = child(uv_layer,b'UVIndex').props[0]
assert len(positions) == 484 and len(uv_indices) == len(indices)
police_root, _ = parse_fbx.parse(str(PROJECT/'Assets/SecretsThatBreathe/Characters/ThaiPolice/ThaiPolice_LowPoly.fbx'))
police_geometry = next(e for e in child(police_root,b'Objects').elems if e.id == b'Geometry' and e.props[-1] == b'Mesh')
assert list(indices) == list(child(police_geometry,b'PolygonVertexIndex').props[0]), 'Different topology: cannot reuse UV correspondence'
# Build unique per-corner UVs using the exact recipe from build_police.py.
new_uv = []
face = []
corners = []
for corner, index in enumerate(indices):
    vertex = index if index >= 0 else -index-1
    face.append(vertex); corners.append(corner)
    if index < 0:
        offset = .01 if positions[face,0].mean() >= 0 else .51
        for c in corners:
            u,v = uv[uv_indices[c]]
            new_uv.extend((u*.48+offset, v*.98+.01))
        face=[]; corners=[]
assert not face
police_uv = child(child(police_geometry,b'LayerElementUV'),b'UV').props[0]
police_indices = child(child(police_geometry,b'LayerElementUV'),b'UVIndex').props[0]
expected = np.array(police_uv).reshape(-1,2)[list(police_indices)]
assert np.max(np.abs(expected - np.array(new_uv).reshape(-1,2))) < 1e-6
uv_data = child(uv_layer,b'UV').props[0]
uv_data[:] = type(uv_data)(uv_data.typecode, new_uv)
uv_indices[:] = type(uv_indices)(uv_indices.typecode, range(len(uv_indices)))

methods = {ord(k):v for k,v in {'B':'add_bool','C':'add_char','Y':'add_int16','I':'add_int32','L':'add_int64','F':'add_float32','D':'add_float64','S':'add_string','R':'add_bytes','f':'add_float32_array','d':'add_float64_array','i':'add_int32_array','l':'add_int64_array','b':'add_bool_array','c':'add_byte_array'}.items()}
def encode(node):
    target=encode_bin.FBXElem(node.id)
    for t,p in zip(node.props_type,node.props):
        if t in (ord('i'),ord('l'),ord('d'),ord('f'),ord('b'),ord('c')): p = list(p)
        getattr(target, methods[t])(p)
    target.elems=[encode(e) for e in node.elems]
    return target

fbx=OUT/'LowPolyMale_IdleTalk.fbx' # Same basename preserves Unity's imported local IDs.
encode_bin.write(str(fbx),encode(root),version)
saved, _ = parse_fbx.parse(str(fbx))
def fingerprint(node, skip_uv=False):
    h=hashlib.sha256(node.id)
    if skip_uv and node.id == b'LayerElementUV': return b'UV'
    for p in node.props: h.update(p.tobytes() if hasattr(p,'tobytes') else repr(p).encode())
    for e in node.elems:h.update(fingerprint(e,skip_uv))
    return h.digest()
def compare(a,b,path=''):
    if a.id == b'LayerElementUV': return
    assert a.id == b.id and len(a.props)==len(b.props) and len(a.elems)==len(b.elems), path
    for index,(x,y) in enumerate(zip(a.props,b.props)):
        assert x == y, (path,a.id,index,repr(x)[:100],repr(y)[:100])
    for x,y in zip(a.elems,b.elems):compare(x,y,path+'/'+a.id.decode(errors='replace'))
compare(child(saved,b'Objects'),child(baseline,b'Objects'))
assert fingerprint(child(saved,b'Connections')) == fingerprint(child(baseline,b'Connections'))
fbx_guid=make_guid(fbx)
meta=(SOURCE/'LowPolyMale_IdleTalk.fbx.meta').read_text()
meta=re.sub(r'^guid: \w+', 'guid: '+fbx_guid,meta,flags=re.M)
meta=meta.replace('366f51e0255644e6b05dcf31e35cbc23','4ebc6d03dd155ee8b222369c4da70fdf')
meta=meta.replace('userData: idle-talk-rig-v1','userData: police-idle-skin-v1')
Path(str(fbx)+'.meta').write_text(meta)
prefab=OUT/'ThaiPolice_IdleTalk.prefab'
prefab_guid=make_guid(prefab)
text=(SOURCE/'LowPolyMale_IdleTalk.prefab').read_text()
text=text.replace('guid: 2f73da8e25aca1c4e91417d8bcde39ac','guid: '+fbx_guid)
text=text.replace('m_Name: LowPolyMale_IdleTalk','m_Name: ThaiPolice_IdleTalk')
prefab.write_text(text)
Path(str(prefab)+'.meta').write_text('fileFormatVersion: 2\nguid: '+prefab_guid+'\nPrefabImporter:\n  externalObjects: {}\n')
Path(str(OUT)+'.meta').write_text('fileFormatVersion: 2\nguid: '+make_guid(OUT)+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n')
face_size=0
triangles=0
for index in indices:
    face_size+=1
    if index<0:
        triangles+=face_size-2
        face_size=0
report={'vertices':484,'triangles':triangles, 'policeUVMatches':True,'allNonUVObjectsAndConnectionsUnchanged':True,'fbxGuid':fbx_guid,'prefabGuid':prefab_guid,'animationController':'original LowPolyMale_IdleTalk.controller'}
(Path(__file__).parent/'idle-talk-build.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
