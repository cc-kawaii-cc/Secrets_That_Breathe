import bpy, json, os, sys
from mathutils import Vector
from collections import defaultdict

root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
out = os.path.join(root, 'Tools', 'PorscheHeadlights')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(root, 'Assets', 'Model Palm', 'Porsche V.2', 'PorscheV.2.fbx'))
report = []
for ob in bpy.context.scene.objects:
    rec = dict(name=ob.name, type=ob.type, parent=ob.parent.name if ob.parent else None,
               matrix=[list(r) for r in ob.matrix_world], dimensions=list(ob.dimensions))
    if ob.type == 'MESH':
        me = ob.data
        rec.update(vertices=len(me.vertices), faces=len(me.polygons), triangles=sum(len(p.vertices)-2 for p in me.polygons),
                   materials=[m.name if m else None for m in me.materials], uv_layers=[u.name for u in me.uv_layers])
        adj=defaultdict(list)
        for e in me.edges:
            a,b=e.vertices; adj[a].append(b); adj[b].append(a)
        remaining=set(range(len(me.vertices))); comps=[]
        while remaining:
            seed=next(iter(remaining)); todo=[seed]; remaining.remove(seed); ids=[]
            while todo:
                v=todo.pop(); ids.append(v)
                for n in adj[v]:
                    if n in remaining: remaining.remove(n); todo.append(n)
            pts=[ob.matrix_world@me.vertices[i].co for i in ids]
            idset=set(ids); polys=[p for p in me.polygons if p.vertices[0] in idset]
            mn=[min(p[k] for p in pts) for k in range(3)]; mx=[max(p[k] for p in pts) for k in range(3)]
            comps.append(dict(verts=len(ids),faces=len(polys),min=mn,max=mx,materials=sorted(set(p.material_index for p in polys)), polygon_indices=[p.index for p in polys]))
        rec['components']=sorted(comps,key=lambda c:-c['faces'])
    report.append(rec)
with open(os.path.join(out,'inspection.json'),'w',encoding='utf-8') as f: json.dump(report,f,ensure_ascii=False,indent=2)
print('PORSCHE_INSPECTION_COMPLETE')
for rec in report:
    print(rec['name'],rec.get('vertices'),rec.get('faces'), rec.get('dimensions'), rec.get('materials'))
    for c in rec.get('components',[])[:15]: print(' ',{k:v for k,v in c.items() if k!='polygon_indices'})
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'PorscheV2_Inspection.blend'))
