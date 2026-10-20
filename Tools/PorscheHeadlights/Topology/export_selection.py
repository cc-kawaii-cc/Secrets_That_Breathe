import bpy, os, json
OUT=os.path.dirname(os.path.abspath(__file__)); BASE=os.path.dirname(OUT)
bpy.ops.wm.open_mainfile(filepath=os.path.join(BASE,'PorscheV2_Inspection.blend'))
ob=bpy.data.objects['Body'];me=ob.data;uv=me.uv_layers[0]
with open(os.path.join(OUT,'uv_analysis.json')) as f:r=json.load(f)
groups=[]
for side,n,sign in [('Left',99,1),('Right',100,-1)]:
    c=next(c for c in r['layers'][1]['islands'] if c['faces']==n and sign*c['min'][0]>.5)
    ids=sorted(c['polygon_indices']);corners=[];seen=set()
    for pi in ids:
        p=me.polygons[pi]
        for li in p.loop_indices:
            pt=ob.matrix_world@me.vertices[me.loops[li].vertex_index].co; tex=uv.data[li].uv
            vals=(-pt.x,pt.z,-pt.y,tex.x,tex.y)
            if vals not in seen:
                seen.add(vals);corners.append(dict(zip(['x','y','z','u','v'],vals)))
    groups.append(dict(side=side,expectedTriangles=sum(len(me.polygons[i].vertices)-2 for i in ids),corners=corners,blenderPolygonIndices=ids))
with open(os.path.join(BASE,'lens_selection.json'),'w') as f:json.dump({'groups':groups},f,indent=2)
print([(g['side'],len(g['blenderPolygonIndices']),g['expectedTriangles'],len(g['corners'])) for g in groups])
