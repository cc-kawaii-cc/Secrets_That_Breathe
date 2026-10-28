"""Bake registered front/back artwork to a unique low resolution UV atlas."""
import bpy,json,math,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent
OUT=R/'Assets/SecretsThatBreathe/Characters/ThaiPolice'
SOURCE=R/'Assets/Nemo_Upgrade/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx'
source_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(SOURCE))
m=next(o for o in bpy.context.scene.objects if o.type=='MESH');m.name='ThaiPolice_Body';m.data.name='ThaiPolice_Body'
s=bpy.context.scene
# Unmirror both UV halves so medals and the nameplate can differ left/right.
layer=m.data.uv_layers.active
for p in m.data.polygons:
 x=sum(m.data.vertices[v].co.x for v in p.vertices)/len(p.vertices)
 for li in p.loop_indices:
  co=layer.data[li].uv;co.x=co.x*.48+(.01 if x>=0 else .51);co.y=co.y*.98+.01
mat=bpy.data.materials.new('ThaiPolice_ProjectionBake');mat.use_nodes=True;n=mat.node_tree.nodes;n.clear();links=mat.node_tree.links
out=n.new('ShaderNodeOutputMaterial');emit=n.new('ShaderNodeEmission');emit.inputs['Strength'].default_value=1;links.new(emit.outputs[0],out.inputs['Surface'])
g=n.new('ShaderNodeNewGeometry');pos=n.new('ShaderNodeSeparateXYZ');normal=n.new('ShaderNodeSeparateXYZ');links.new(g.outputs['Position'],pos.inputs[0]);links.new(g.outputs['Normal'],normal.inputs[0])
def mathnode(op,a,b):
 t=n.new('ShaderNodeMath');t.operation=op
 for i,v in enumerate([a,b]):
  if isinstance(v,(float,int)):t.inputs[i].default_value=v
  else:links.new(v,t.inputs[i])
 return t.outputs[0]
v=mathnode('ADD',mathnode('MULTIPLY',pos.outputs['Z'],1/4.8),.5-2/4.8)
frontu=mathnode('ADD',mathnode('MULTIPLY',pos.outputs['X'],.97/7.2),.25)
backu=mathnode('ADD',mathnode('MULTIPLY',pos.outputs['X'],-.97/7.2),.75)
source=bpy.data.images.load(str(W/'ThaiPolice_Projection.png'))
def texture(u):
 combine=n.new('ShaderNodeCombineXYZ');links.new(u,combine.inputs[0]);links.new(v,combine.inputs[1]);tex=n.new('ShaderNodeTexImage');tex.image=source;tex.extension='EXTEND';links.new(combine.outputs[0],tex.inputs['Vector']);return tex.outputs['Color']
f=texture(frontu);b=texture(backu)
amount=n.new('ShaderNodeMapRange');amount.clamp=True;amount.interpolation_type='SMOOTHERSTEP';links.new(normal.outputs['Y'],amount.inputs['Value']);amount.inputs['From Min'].default_value=-.2;amount.inputs['From Max'].default_value=.2
mix=n.new('ShaderNodeMixRGB');links.new(amount.outputs[0],mix.inputs[0]);links.new(f,mix.inputs[1]);links.new(b,mix.inputs[2])
# Complete the unphotographed head sides with a simple tapered crew cut, keeping
# facial details on the front and avoiding gray backdrop / stretched eyes.
hairline=mathnode('SUBTRACT',3.98,mathnode('MULTIPLY',mathnode('MAXIMUM',pos.outputs['Y'],0),.55))
fade=n.new('ShaderNodeMapRange');fade.clamp=True;fade.interpolation_type='SMOOTHERSTEP';links.new(mathnode('SUBTRACT',pos.outputs['Z'],hairline),fade.inputs['Value']);fade.inputs['From Min'].default_value=-.055;fade.inputs['From Max'].default_value=.035
sidecolor=n.new('ShaderNodeMixRGB');links.new(fade.outputs[0],sidecolor.inputs[0]);sidecolor.inputs[1].default_value=(.29,.145,.075,1);sidecolor.inputs[2].default_value=(.009,.008,.007,1)
sideamount=n.new('ShaderNodeMapRange');sideamount.clamp=True;sideamount.interpolation_type='SMOOTHERSTEP';links.new(mathnode('ABSOLUTE',normal.outputs['X'],0),sideamount.inputs['Value']);sideamount.inputs['From Min'].default_value=.4;sideamount.inputs['From Max'].default_value=.75
mask=mathnode('MULTIPLY',sideamount.outputs[0],mathnode('GREATER_THAN',pos.outputs['Z'],3.65))
clean=n.new('ShaderNodeMixRGB');links.new(mask,clean.inputs[0]);links.new(mix.outputs[0],clean.inputs[1]);links.new(sidecolor.outputs[0],clean.inputs[2])
# Extend clothing/skin/shoe colors over gray samples at grazing silhouettes.
rgb=n.new('ShaderNodeSeparateXYZ');links.new(clean.outputs[0],rgb.inputs[0])
mx=mathnode('MAXIMUM',mathnode('MAXIMUM',rgb.outputs[0],rgb.outputs[1]),rgb.outputs[2]);mn=mathnode('MINIMUM',mathnode('MINIMUM',rgb.outputs[0],rgb.outputs[1]),rgb.outputs[2])
neutral=mathnode('MULTIPLY',mathnode('LESS_THAN',mathnode('SUBTRACT',mx,mn),.04),mathnode('GREATER_THAN',mx,.10))
shoe=mathnode('LESS_THAN',pos.outputs['Z'],.32)
hand=mathnode('MULTIPLY',mathnode('GREATER_THAN',mathnode('ABSOLUTE',pos.outputs['X'],0),1.10),mathnode('LESS_THAN',pos.outputs['Z'],2.4))
edge=mathnode('MULTIPLY',mathnode('LESS_THAN',mathnode('ABSOLUTE',normal.outputs['Y'],0),.5),mathnode('LESS_THAN',pos.outputs['Z'],3.55))
fixmask=mathnode('MULTIPLY',neutral,mathnode('MAXIMUM',mathnode('MAXIMUM',shoe,hand),edge))
fill=n.new('ShaderNodeMixRGB');links.new(hand,fill.inputs[0]);fill.inputs[1].default_value=(.19,.145,.105,1);fill.inputs[2].default_value=(.29,.145,.075,1)
fillshoe=n.new('ShaderNodeMixRGB');links.new(shoe,fillshoe.inputs[0]);links.new(fill.outputs[0],fillshoe.inputs[1]);fillshoe.inputs[2].default_value=(.006,.006,.006,1)
finished=n.new('ShaderNodeMixRGB');links.new(fixmask,finished.inputs[0]);links.new(clean.outputs[0],finished.inputs[1]);links.new(fillshoe.outputs[0],finished.inputs[2]);links.new(finished.outputs[0],emit.inputs['Color'])
target=bpy.data.images.new('ThaiPolice_BaseColor',width=512,height=512,alpha=False);target.generated_color=(.2,.16,.11,1)
active=n.new('ShaderNodeTexImage');active.image=target;n.active=active
m.data.materials.clear();m.data.materials.append(mat)
for p in m.data.polygons:p.material_index=0
bpy.context.view_layer.objects.active=m;m.select_set(True)
s.render.engine='CYCLES';s.cycles.device='CPU';s.cycles.samples=1;s.render.bake.margin=3;s.render.bake.use_clear=False
bpy.ops.object.bake(type='EMIT')
target.filepath_raw=str(OUT/'Textures/ThaiPolice_BaseColor.png');target.file_format='PNG';target.save()
final=bpy.data.materials.new('ThaiPolice');final.use_nodes=True
bsdf=final.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.85
tex=final.node_tree.nodes.new('ShaderNodeTexImage');tex.image=target;final.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
m.data.materials.clear();m.data.materials.append(final)
height=max((m.matrix_world@v.co).z for v in m.data.vertices)-min((m.matrix_world@v.co).z for v in m.data.vertices)
scale=1.8/height;matrix=Matrix.Scale(scale,4)@m.matrix_world
m.data.transform(matrix);m.matrix_world=Matrix.Identity(4)
minz=min(v.co.z for v in m.data.vertices)
for vtx in m.data.vertices:vtx.co.z-=minz
m.data.update();m.data.calc_loop_triangles()
assert len(m.data.loop_triangles)==964 and len(m.data.vertices)==484
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash
bpy.ops.wm.save_as_mainfile(filepath=str(W/'ThaiPolice_LowPoly.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'ThaiPolice_LowPoly.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
s.view_settings.view_transform='Standard';s.view_settings.look='None';s.view_settings.exposure=0;s.view_settings.gamma=1
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_shadows=True;s.display.shading.show_cavity=False;s.display.shading.show_specular_highlight=False
s.display.shading.background_type='WORLD';s.world=bpy.data.worlds.new('PreviewWorld');s.world.color=(.055,.055,.055)
s.render.resolution_x=650;s.render.resolution_y=850;s.render.resolution_percentage=100
for name,loc in [('front',(0,-6,1)),('three_quarter',(3,-6,1.3)),('back',(0,6,1)),('side',(6,0,1))]:
 bpy.ops.object.camera_add(location=loc);cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.1;s.camera=cam;s.render.filepath=str(W/('preview_'+name+'.png'));bpy.ops.render.render(write_still=True)
s.display.shading.light='FLAT'
bpy.ops.object.camera_add(location=(0,-6,1));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.1;s.camera=cam;s.render.filepath=str(W/'albedo_front.png');bpy.ops.render.render(write_still=True)
(W/'build-report.json').write_text(json.dumps({'model':str(OUT/'ThaiPolice_LowPoly.fbx'),'objects':1,'vertices':484,'triangles':964,'height_metres':1.8,'texture':[512,512],'materials':1,'unique_left_right_uv':True,'source_sha256':source_hash,'source_unchanged':True,'rigged':False},indent=2))
print('POLICE_BUILD_COMPLETE')
