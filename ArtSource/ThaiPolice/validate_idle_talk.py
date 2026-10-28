from pathlib import Path
import bpy,json
from mathutils import Vector
R=Path(__file__).resolve().parents[2]; W=Path(__file__).parent
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/IdleTalk/LowPolyMale_IdleTalk.fbx'))
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
assert len(mesh.data.vertices)==484 and mesh.find_armature()==rig
mesh.data.calc_loop_triangles();assert len(mesh.data.loop_triangles)==964
mat=bpy.data.materials.new('ThaiPolice');mat.use_nodes=True
nodes=mat.node_tree.nodes;tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/Textures/ThaiPolice_BaseColor.png'))
nodes.active=tex;mat.node_tree.links.new(tex.outputs['Color'],nodes.get('Principled BSDF').inputs['Base Color'])
mesh.data.materials.clear();mesh.data.materials.append(mat)
s=bpy.context.scene;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_shadows=True
s.display.shading.background_type='WORLD';s.world=bpy.data.worlds.new('Preview');s.world.color=(.07,.07,.07)
s.view_settings.view_transform='Standard';s.render.resolution_x=600;s.render.resolution_y=800;s.render.resolution_percentage=100
assert rig.animation_data and rig.animation_data.action
action=rig.animation_data.action
start,end=action.frame_range
def surface(frame):
 s.frame_set(int(frame));dg=bpy.context.evaluated_depsgraph_get();ev=mesh.evaluated_get(dg)
 return [ev.matrix_world@v.co for v in ev.data.vertices]
a=surface(start);b=surface(start+(end-start)*.35)
moving=sum((v-w).length>.001 for v,w in zip(a,b));assert moving>20
for name,frame,side in [('front',start+(end-start)*.35,-1),('back',start+(end-start)*.7,1)]:
 points=surface(frame);lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)));center=(lo+hi)*.5;height=hi.z-lo.z
 bpy.ops.object.camera_add(location=center+Vector((height*.5,side*height*2,height*.1)))
 cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=height*1.2;s.camera=cam
 s.render.filepath=str(W/('idle-police-'+name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(W/'ThaiPolice_IdleTalk.blend'))
(W/'idle-talk-validation.json').write_text(json.dumps({'vertices':484,'triangles':964,'bones':len(rig.data.bones),'movingVertices':moving,'action':action.name,'frameRange':list(action.frame_range),'unityPlayModeTested':False},indent=2))
print('POLICE_IDLE_VALIDATED',moving)
