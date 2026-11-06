import bpy,bmesh,json
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'Tools/GUYOptimization'
config=json.loads((out/'config.json').read_text());result={}
for tag in ['before','after']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if tag=='before':bpy.ops.import_scene.fbx(filepath=str(root/config[2]['path']))
    else:bpy.ops.wm.open_mainfile(filepath=str(out/'Model2/optimized.blend'))
    values=[]
    for o in bpy.context.scene.objects:
        if o.type!='MESH':continue
        bm=bmesh.new();bm.from_mesh(o.data)
        values.append({'name':o.name,'boundary':sum(e.is_boundary for e in bm.edges),'nonmanifold':sum(not e.is_manifold for e in bm.edges),
                       'degenerate':sum(f.calc_area()<1e-12 for f in bm.faces)})
        bm.free()
    result[tag]=values
(out/'topology2.json').write_text(json.dumps(result,indent=2))
