import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
ROOT=Path(__file__).resolve().parents[3]
WORK=ROOT/'ArtSource/LowPolyMaleSlowWalk/IdleTalk'
bpy.ops.wm.open_mainfile(filepath=str(WORK/'LowPolyMale_IdleTalk.blend'))
scene=bpy.context.scene
rig=bpy.data.objects['SlowWalkRig']; mesh=bpy.data.objects['Cube']
rig.animation_data.action=bpy.data.actions['IdleTalk']
def points():
 bpy.context.view_layer.update()
 e=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
 return [e.matrix_world@v.co for v in e.data.vertices]
reference={}
for f in [1,37,73,109,145]:
 scene.frame_set(f);reference[f]=points()
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.render.fps=30
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_IdleTalk.fbx'))
meshes=[o for o in scene.objects if o.type=='MESH'];rigs=[o for o in scene.objects if o.type=='ARMATURE']
assert len(meshes)==len(rigs)==1
mesh,rig=meshes[0],rigs[0]
actions=list(bpy.data.actions);print('ACTIONS',[(a.name,list(a.frame_range)) for a in actions]);assert len(actions)==1 and 'IdleTalk' in actions[0].name
rig.animation_data.action=actions[0]
mesh.data.calc_loop_triangles()
assert len(mesh.data.loop_triangles)==964 and len(rig.data.bones)==22
errors=[]
for f,baseline in reference.items():
 scene.frame_set(f+1);actual=points();tree=KDTree(len(actual))
 for i,v in enumerate(actual):tree.insert(v,i)
 tree.balance();errors.append(max(tree.find(v)[2] for v in baseline))
assert max(errors)<.005,errors
scene.frame_set(2);start=points();scene.frame_set(146);end=points()
loop_error=max((a-b).length for a,b in zip(start,end));assert loop_error<.0001
report={'actions':[a.name for a in actions],'frames':list(actions[0].frame_range),'seconds':4.8,'triangles':964,'bones':22,'maximum_export_surface_error':max(errors),'loop_vertex_error':loop_error}
(WORK/'export-validation.json').write_text(json.dumps(report,indent=2))
scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=500;scene.render.resolution_y=620;scene.render.resolution_percentage=100
scene.display.shading.light='STUDIO';scene.display.shading.color_type='SINGLE';scene.display.shading.single_color=(.55,.61,.67);scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
for name,location,frame in [('front',(0,-12,2.1),37),('side',(12,0,2.1),37),('back',(0,12,2.1),109)]:
 scene.frame_set(frame);bpy.ops.object.camera_add(location=location);camera=bpy.context.object
 camera.rotation_euler=(Vector((0,0,2.1))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=4.9;scene.camera=camera
 scene.render.filepath=str(WORK/'Preview'/('export_'+name+'.png'));bpy.ops.render.render(write_still=True)
print('IDLE_EXPORT_VALIDATION',json.dumps(report))

