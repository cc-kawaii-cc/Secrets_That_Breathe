import bpy,json
from pathlib import Path
r=Path.cwd();p=r/'Assets/SecretsThatBreathe/Environment/BananaTree_Unity6_URP/Assets/BananaTree'
bpy.ops.wm.open_mainfile(filepath=str(p/'Source/BananaTree_source.blend'))
report={}
for o in bpy.data.objects:
 if o.type=='MESH':
  o.data.calc_loop_triangles();report[o.name]={'triangles':len(o.data.loop_triangles),'dimensions':list(o.dimensions),'materials':[m.name for m in o.data.materials],'uv':len(o.data.uv_layers),'color':list(o.data.color_attributes.keys())}
(r/'ArtSource/BananaTree/existing-model.json').write_text(json.dumps(report,indent=2))
print('MODEL',json.dumps(report))
