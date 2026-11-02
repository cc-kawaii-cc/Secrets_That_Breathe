import bpy
from pathlib import Path
R=Path.cwd();A=R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/GunAndPush';W=R/'ArtSource/PoliceGunPush'
bpy.ops.wm.read_factory_settings(use_empty=True)
# Dark polymer holster with the handgun kept inside; grip and rear slide visible.
image=bpy.data.images.new('HolsterPalette',width=4,height=4)
colors=[(.023,.027,.030,1),(.065,.075,.080,1),(.135,.145,.150,1),(.030,.024,.019,1)]
image.pixels=[c for y in range(4) for color in colors for c in color];image.filepath_raw=str(A/'HolsterPalette.png');image.file_format='PNG';image.save()
mat=bpy.data.materials.new('PoliceHolsteredGun');mat.use_nodes=True;node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;mat.node_tree.links.new(node.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.8
objects=[]
def box(name,loc,size,index,bevel=0,angle=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('Moulded corners','BEVEL');m.width=bevel;m.segments=1;bpy.ops.object.modifier_apply(modifier=m.name)
 o.rotation_euler.y=angle;o.data.materials.append(mat)
 uv=o.data.uv_layers.active or o.data.uv_layers.new()
 for loop in uv.data:loop.uv=((index+.5)/4,.5)
 objects.append(o)
# Blender -Y faces out, Z-up. The grip projects above the enclosing holster.
box('Holster',(0,0,-.048),(.060,.048,.125),0,.006)
box('BeltLoop',(0,.034,-.015),(.050,.028,.072),3,.003)
box('RetentionStrap',(0,-.028,.002),(.047,.009,.020),1,.001)
box('PistolGrip',(0,0,.047),(.038,.035,.077),1,.003,angle=-.20)
box('PistolBackplate',(0,.008,.085),(.042,.040,.015),2,.002)
for z in [.025,.038,.051,.064]:box('GripGroove',(0,-.020,z),(.032,.002,.002),0)
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name='PoliceHolsteredGun';bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.calc_loop_triangles();print('HOLSTER_TRIANGLES',len(o.data.loop_triangles))
bpy.ops.export_scene.fbx(filepath=str(A/'PoliceHolsteredGun.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=str(W/'PoliceHolsteredGun.blend'))
