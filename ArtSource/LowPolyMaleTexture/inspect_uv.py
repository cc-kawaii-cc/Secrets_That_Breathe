import bpy,json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/LowPolyMaleSlowWalk/LowPolyMale_SlowWalk.blend'))
m=bpy.data.objects['Cube'];u=m.data.uv_layers.active
polys=[]
for p in m.data.polygons:polys.append([[float(x) for x in u.data[i].uv] for i in p.loop_indices])
print('UV_MINMAX',min(x for p in polys for v in p for x in v),max(x for p in polys for v in p for x in v))
(R/'ArtSource/LowPolyMaleTexture/uv-info.json').write_text(json.dumps(polys))
bpy.ops.object.select_all(action='DESELECT');m.select_set(True);bpy.context.view_layer.objects.active=m
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.export_layout(filepath=str(R/'ArtSource/LowPolyMaleTexture/original_uv.svg'),mode='SVG',size=(1024,1024),opacity=.3,export_all=True)
