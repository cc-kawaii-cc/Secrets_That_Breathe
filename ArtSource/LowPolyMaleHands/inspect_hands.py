import bpy, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
W=Path(__file__).resolve().parent
SOURCE=ROOT/'Assets/Nemo_Upgrade/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
report=[]
for o in meshes:
 p=[o.matrix_world@v.co for v in o.data.vertices]
 o.data.calc_loop_triangles()
 report.append(dict(name=o.name,mesh=o.data.name,parent=o.parent.name if o.parent else None,vertices=len(p),triangles=len(o.data.loop_triangles),bounds=[[min(v[i] for v in p),max(v[i] for v in p)] for i in range(3)],matrix=[list(row) for row in o.matrix_world],materials=[m.name for m in o.data.materials],modifiers=[m.type for m in o.modifiers],uv=[u.name for u in o.data.uv_layers],coords=[list(v) for v in p],faces=[list(f.vertices) for f in o.data.polygons]))
(W/'baseline.json').write_text(json.dumps(report,indent=2))
print('AUDIT',json.dumps([{k:v for k,v in r.items() if k not in ('coords','faces')} for r in report]))
scene=bpy.context.scene
scene.render.engine='BLENDER_WORKBENCH'
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.display.shading.light='STUDIO';scene.display.shading.color_type='SINGLE';scene.display.shading.single_color=(.6,.6,.6)
scene.display.shading.show_shadows=False;scene.display.shading.show_cavity=True
scene.world=bpy.data.worlds.new('InspectionWorld');scene.world.color=(.12,.12,.12)
def render(name,target,offset,scale):
 bpy.ops.object.camera_add(location=Vector(target)+Vector(offset));c=bpy.context.object
 c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=scale;scene.camera=c
 scene.render.filepath=str(W/(name+'.png'));bpy.ops.render.render(write_still=True)
render('before_overview',(0,0,2.1),(0,-12,1),5.6)
render('before_hand_front',(1.27,-.07,2.16),(0,-5,0),.72)
render('before_hand_palm',(1.27,-.07,2.16),(-5,0,-2),.72)
render('before_hand_back',(1.27,-.07,2.16),(5,1,2),.72)
bpy.ops.wm.save_as_mainfile(filepath=str(W/'Original_Inspection.blend'))
