import bpy, os, json
from collections import Counter
root=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
work=os.path.join(root,'Tools','PorscheHeadlights')
def snapshot():
    result={}
    for ob in bpy.context.scene.objects:
        if ob.type!='MESH':continue
        me=ob.data;me.calc_loop_triangles(); uv=me.uv_layers[0].data
        faces=[]
        for tri in me.loop_triangles:
            corners=[]
            for li in tri.loops:
                co=ob.matrix_world@me.vertices[me.loops[li].vertex_index].co
                corners.append(tuple(round(x,5) for x in (*co,*uv[li].uv)))
            faces.append(tuple(sorted(corners)))
        result[ob.name]=dict(triangles=len(faces),corners=Counter(c for f in faces for c in f),bounds=([min((ob.matrix_world@v.co)[a] for v in me.vertices) for a in range(3)],[max((ob.matrix_world@v.co)[a] for v in me.vertices) for a in range(3)]))
    return result
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(root,'Assets','Model Palm','Porsche V.2','PorscheV.2.fbx'))
baseline=snapshot()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(work,'Source','PorscheV2_HeadlightsSeparated.fbx'))
after=snapshot()
assert set(after)==set(baseline)|{'Headlight_Left','Headlight_Right'}
assert sum(x['triangles'] for x in baseline.values())==sum(x['triangles'] for x in after.values())
# Compare total corner samples exactly at five decimal places; triangles in the original were not re-cut.
beforeCounter=sum((x['corners'] for x in baseline.values()),Counter())
afterCounter=sum((x['corners'] for x in after.values()),Counter())
report={'originalTriangles':sum(x['triangles'] for x in baseline.values()),'afterExportTriangles':sum(x['triangles'] for x in after.values()),'positionUvCornerMultisetEqual5Decimals':beforeCounter==afterCounter,'missingCornerSamples':sum((beforeCounter-afterCounter).values()),'addedCornerSamples':sum((afterCounter-beforeCounter).values()),'objects':[{k:v for k,v in dict(name=name,**rec).items() if k!='corners'} for name,rec in after.items()]}
json.dump(report,open(os.path.join(work,'Source','export_validation.json'),'w'),indent=2)
print('PORSCHE_EXPORT_VALIDATED',report['originalTriangles'],report['afterExportTriangles'], report['positionUvCornerMultisetEqual5Decimals'])
