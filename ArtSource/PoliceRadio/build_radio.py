import bpy
from pathlib import Path
from mathutils import Vector
R=Path.cwd();A=R/'Assets/SecretsThatBreathe/Characters/ThaiPolice/Radio';W=R/'ArtSource/PoliceRadio'
bpy.ops.wm.read_factory_settings(use_empty=True)
colors=[(.028,.032,.034,1),(.10,.115,.12,1),(.19,.31,.15,1),(.28,.30,.31,1),(.36,.045,.025,1),(.025,.028,.030,1),(.025,.028,.030,1),(.025,.028,.030,1)]
image=bpy.data.images.new('RadioPalette',width=8,height=8);image.pixels=[c for y in range(8) for color in colors for c in color];image.filepath_raw=str(A/'RadioPalette.png');image.file_format='PNG';image.save()
mat=bpy.data.materials.new('PoliceRadio');mat.use_nodes=True;tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.75
objects=[]
def color(o,index):
 o.data.materials.append(mat)
 uv=o.data.uv_layers.active or o.data.uv_layers.new()
 for v in uv.data:v.uv=((index+.5)/8,.5)
 objects.append(o)
def box(name,loc,scale,index,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('Small moulded edge','BEVEL');m.width=bevel;m.segments=1;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 color(o,index);return o
# Blender -Y faces out from the uniform; Z is up.
box('Body',(0,0,0),(.066,.038,.112),0,.004)
box('SpeakerPanel',(0,-.020,.013),(.050,.004,.051),1,.001)
for i in range(5):box('Grille',(0,-.023,.002+i*.007),(.040,.002,.002),3)
box('Display',(0,-.021,-.032),(.038,.003,.017),2,.001)
box('PushToTalk',(-.035,0,.015),(.007,.020,.030),4,.001)
box('BeltClip',(0,.025,.005),(.024,.014,.067),1,.002)
for x,radius,depth,z in [(-.020,.005,.105,.107),(.019,.009,.015,.063)]:
 bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=radius,depth=depth,location=(x,0,z));color(bpy.context.object,0)
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name='PoliceRadio';o.data.calc_loop_triangles();print('RADIO_TRIANGLES',len(o.data.loop_triangles))
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.export_scene.fbx(filepath=str(A/'PoliceRadio.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=str(W/'PoliceRadio.blend'))
