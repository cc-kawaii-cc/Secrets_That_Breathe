import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]; W=ROOT/'ArtSource/LowPolyMaleTexture'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/LowPolyMaleSlowWalk/LowPolyMale_SlowWalk.blend'))
s=bpy.context.scene;m=bpy.data.objects['Cube'];r=bpy.data.objects['SlowWalkRig'];r.data.pose_position='REST'
p=[m.matrix_world@v.co for v in m.data.vertices]
report={'bounds':[[min(v[i] for v in p),max(v[i] for v in p)] for i in range(3)],'UV_layers':[(u.name,len(u.data)) for u in m.data.uv_layers],'materials':[x.name for x in m.data.materials],'vertices':[list(v) for v in p],'faces':[list(f.vertices) for f in m.data.polygons]}
(W/'mesh-info.json').write_text(json.dumps(report))
print('MESH_INFO',str({k:v for k,v in report.items() if k not in ['vertices','faces']}))
s.render.engine='BLENDER_WORKBENCH';s.render.resolution_x=768;s.render.resolution_y=1024;s.render.resolution_percentage=100
s.display.shading.light='STUDIO';s.display.shading.color_type='SINGLE';s.display.shading.single_color=(.55,.55,.55)
s.display.shading.show_shadows=False;s.display.shading.show_cavity=True
s.display.shading.background_type='WORLD';s.world.color=(.16,.16,.16)
for name,loc in [('front',(0,-12,2.0)),('back',(0,12,2.0))]:
 bpy.ops.object.camera_add(location=loc);cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,2.0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=4.8;s.camera=cam
 s.render.filepath=str(W/(name+'_template.png'));bpy.ops.render.render(write_still=True)
