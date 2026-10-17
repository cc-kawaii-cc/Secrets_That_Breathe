"""Bake the appearance projection into the model's original mirrored UVs; no FBX edits."""
import bpy,bmesh,json,hashlib
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];W=R/'ArtSource/LowPolyMaleTexture';OUT=R/'Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/Textures';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/LowPolyMaleSlowWalk/LowPolyMale_SlowWalk.blend'))
s=bpy.context.scene;m=bpy.data.objects['Cube'];rig=bpy.data.objects['SlowWalkRig'];rig.data.pose_position='REST'
mat=bpy.data.materials.new('ElderOfficeWorker_ProjectionBake');mat.use_nodes=True;n=mat.node_tree.nodes;n.clear();links=mat.node_tree.links
out=n.new('ShaderNodeOutputMaterial');emit=n.new('ShaderNodeEmission');emit.inputs['Strength'].default_value=1;links.new(emit.outputs[0],out.inputs['Surface'])
g=n.new('ShaderNodeNewGeometry');pos=n.new('ShaderNodeSeparateXYZ');normal=n.new('ShaderNodeSeparateXYZ');links.new(g.outputs['Position'],pos.inputs[0]);links.new(g.outputs['Normal'],normal.inputs[0])
def math(op,a,b):
 t=n.new('ShaderNodeMath');t.operation=op
 for i,v in enumerate([a,b]):
  if isinstance(v,(float,int)):t.inputs[i].default_value=v
  else:links.new(v,t.inputs[i])
 return t.outputs[0]
v=math('ADD',math('MULTIPLY',pos.outputs['Z'],1/4.8),.5-2/4.8)
frontu=math('ADD',math('MULTIPLY',pos.outputs['X'],.97/7.2),.25)
backu=math('ADD',math('MULTIPLY',pos.outputs['X'],-.97/7.2),.75)
source=bpy.data.images.load(str(W/'ElderWorker_Projection.png'))
def texture(u):
 combine=n.new('ShaderNodeCombineXYZ');links.new(u,combine.inputs[0]);links.new(v,combine.inputs[1]);tex=n.new('ShaderNodeTexImage');tex.image=source;tex.extension='EXTEND';links.new(combine.outputs[0],tex.inputs['Vector']);return tex.outputs['Color']
f=texture(frontu);b=texture(backu)
amount=n.new('ShaderNodeMapRange');amount.clamp=True;amount.interpolation_type='SMOOTHERSTEP';links.new(normal.outputs['Y'],amount.inputs['Value']);amount.inputs['From Min'].default_value=-.2;amount.inputs['From Max'].default_value=.2
mix=n.new('ShaderNodeMixRGB');links.new(amount.outputs[0],mix.inputs[0]);links.new(f,mix.inputs[1]);links.new(b,mix.inputs[2]);links.new(mix.outputs[0],emit.inputs['Color'])
# Original UVs overlap only mirrored left/right halves. Bake one half, so no write conflicts.
target=bpy.data.images.new('ElderOfficeWorker_BaseColor',width=512,height=512,alpha=False);target.generated_color=(.23,.19,.15,1)
active=n.new('ShaderNodeTexImage');active.image=target;n.active=active
copy=m.copy();copy.data=m.data.copy();copy.name='BakeOnly_RightHalf';s.collection.objects.link(copy)
for modifier in list(copy.modifiers):copy.modifiers.remove(modifier)
copy.data.materials.clear();copy.data.materials.append(mat)
for p in copy.data.polygons:p.material_index=0
bm=bmesh.new();bm.from_mesh(copy.data);bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().x<-.000001],context='FACES');bm.to_mesh(copy.data);bm.free()
bpy.ops.object.select_all(action='DESELECT');copy.select_set(True);bpy.context.view_layer.objects.active=copy
s.render.engine='CYCLES';s.cycles.device='CPU';s.cycles.samples=1;s.render.bake.margin=8;s.render.bake.use_clear=False
bpy.ops.object.bake(type='EMIT')
target.filepath_raw=str(OUT/'ElderOfficeWorker_BaseColor.png');target.file_format='PNG';target.save()
bpy.data.objects.remove(copy,do_unlink=True)
# Real mesh material used for proof renders and editable source. Geometry/rig/UV untouched.
final=bpy.data.materials.new('ElderOfficeWorker');final.use_nodes=True
bsdf=final.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.92
tex=final.node_tree.nodes.new('ShaderNodeTexImage');tex.image=target;final.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
m.data.materials.clear();m.data.materials.append(final)
for p in m.data.polygons:p.material_index=0
rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['SlowWalk_InPlace'];s.frame_set(1)
# Preview with texture color and neutral studio lighting (actual geometry).
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_shadows=True;s.display.shading.show_cavity=False
s.display.shading.background_type='WORLD';s.world.color=(.07,.07,.07)
s.render.resolution_x=650;s.render.resolution_y=850;s.render.resolution_percentage=100
for name,loc in [('front',(0,-12,2.1)),('three_quarter',(7,-12,2.8)),('side',(12,0,2.1)),('back',(0,12,2.1))]:
 bpy.ops.object.camera_add(location=loc);cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,2.1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=4.9;s.camera=cam;s.render.filepath=str(W/('textured_'+name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(W/'LowPolyMale_Textured.blend'))
print('BAKE_DONE',str(target.filepath_raw))
