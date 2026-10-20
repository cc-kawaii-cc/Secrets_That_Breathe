import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/Porsche992GTS'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Existing_Inspection.blend'))
ob=bpy.data.objects['Body'];bm=bmesh.new();bm.from_mesh(ob.data);seen=set();parts=[]
for v in bm.verts:
    if v in seen:continue
    todo=[v];component=[];seen.add(v)
    while todo:
        a=todo.pop();component.append(a)
        for e in a.link_edges:
            n=e.other_vert(a)
            if n not in seen:seen.add(n);todo.append(n)
    points=[ob.matrix_world@a.co for a in component];parts.append({'verts':len(points),'min':[min(p[i] for p in points) for i in range(3)],'max':[max(p[i] for p in points) for i in range(3)]})
data={'components':sorted(parts,key=lambda p:-p['verts']),'materials':[]}
for i,m in enumerate(ob.data.materials):
    points=[ob.matrix_world@ob.data.vertices[vi].co for p in ob.data.polygons if p.material_index==i for vi in p.vertices]
    if points:data['materials'].append({'name':m.name,'index':i,'faces':sum(p.material_index==i for p in ob.data.polygons),'min':[min(p[k] for p in points) for k in range(3)],'max':[max(p[k] for p in points) for k in range(3)]})
(OUT/'body_components.json').write_text(json.dumps(data,indent=2));bm.free()
