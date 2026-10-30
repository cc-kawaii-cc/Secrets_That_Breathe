"""Bake registered front/back artwork to a unique low resolution UV atlas."""
import bpy,bmesh,json,math,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent
OUT=R/'Assets/SecretsThatBreathe/Characters/NavySuitVillain'
SOURCE=R/'Assets/Nemo_Upgrade/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx'
source_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(SOURCE))
m=next(o for o in bpy.context.scene.objects if o.type=='MESH');m.name='NavySuitVillain_Body';m.data.name='NavySuitVillain_Body'
s=bpy.context.scene
# Unmirror both UV halves so medals and the nameplate can differ left/right.
layer=m.data.uv_layers.active
for p in m.data.polygons:
 x=sum(m.data.vertices[v].co.x for v in p.vertices)/len(p.vertices)
 for li in p.loop_indices:
  co=layer.data[li].uv;co.x=co.x*.48+(.01 if x>=0 else .51);co.y=co.y*.98+.01
mat=bpy.data.materials.new('NavySuitVillain_ProjectionBake');mat.use_nodes=True;n=mat.node_tree.nodes;n.clear();links=mat.node_tree.links
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
source=bpy.data.images.load(str(W/'NavySuitVillain_Projection.png'))
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
hand=mathnode('MULTIPLY',mathnode('GREATER_THAN',mathnode('ABSOLUTE',pos.outputs['X'],0),1.10),mathnode('LESS_THAN',pos.outputs['Z'],2.32))
edge=mathnode('MULTIPLY',mathnode('LESS_THAN',mathnode('ABSOLUTE',normal.outputs['Y'],0),.5),mathnode('LESS_THAN',pos.outputs['Z'],3.55))
fixmask=mathnode('MULTIPLY',neutral,mathnode('MAXIMUM',mathnode('MAXIMUM',shoe,hand),edge))
fill=n.new('ShaderNodeMixRGB');links.new(hand,fill.inputs[0]);fill.inputs[1].default_value=(.018,.028,.058,1);fill.inputs[2].default_value=(.29,.145,.075,1)
fillshoe=n.new('ShaderNodeMixRGB');links.new(shoe,fillshoe.inputs[0]);links.new(fill.outputs[0],fillshoe.inputs[1]);fillshoe.inputs[2].default_value=(.006,.006,.006,1)
finished=n.new('ShaderNodeMixRGB');links.new(fixmask,finished.inputs[0]);links.new(clean.outputs[0],finished.inputs[1]);links.new(fillshoe.outputs[0],finished.inputs[2]);links.new(finished.outputs[0],emit.inputs['Color'])
target=bpy.data.images.new('NavySuitVillain_BaseColor',width=512,height=512,alpha=False);target.generated_color=(.2,.16,.11,1)
active=n.new('ShaderNodeTexImage');active.image=target;n.active=active
m.data.materials.clear();m.data.materials.append(mat)
for p in m.data.polygons:p.material_index=0
bpy.context.view_layer.objects.active=m;m.select_set(True)
s.render.engine='CYCLES';s.cycles.device='CPU';s.cycles.samples=1;s.render.bake.margin=3;s.render.bake.use_clear=False
bpy.ops.object.bake(type='EMIT')
target.filepath_raw=str(OUT/'Textures/NavySuitVillain_BaseColor.png');target.file_format='PNG';target.save()
final=bpy.data.materials.new('NavySuitVillain');final.use_nodes=True
bsdf=final.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.85
tex=final.node_tree.nodes.new('ShaderNodeTexImage');tex.image=target;final.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
m.data.materials.clear();m.data.materials.append(final)
# Opaque clipped-rectangle sunglasses: actual side arms and bridge, not just paint.
gv=[];gf=[]
def prism(points,depth):
 offset=len(gv);gv.extend(points);gv.extend([(x,y+depth,z) for x,y,z in points]);count=len(points)
 gf.append([offset+i for i in range(count)][::-1]);gf.append([offset+count+i for i in range(count)])
 for j in range(count):gf.append([offset+j,offset+(j+1)%count,offset+(j+1)%count+count,offset+j+count])
outline=[(.025,4.012),(.041,4.023),(.171,4.018),(.191,4.000),(.181,3.944),(.163,3.927),(.042,3.932),(.024,3.949)]
for sign in [1,-1]:
 prism([(sign*x,-.300+.28*x,z) for x,z in outline],.012)
 # Four-sided continuous arm section with an ear-end bend.
 path=[Vector((sign*.186,-.242,3.998)),Vector((sign*.207,.028,3.982)),Vector((sign*.204,.108,3.942))]
 ids=[]
 for q in path:
  row=[]
  for dx,dz in [(-.006,-.007),(.006,-.007),(.006,.007),(-.006,.007)]:
   row.append(len(gv));gv.append(tuple(q+Vector((dx,0,dz))))
  ids.append(row)
 gf.append(ids[0][::-1]);gf.append(ids[-1])
 for a,b in zip(ids,ids[1:]):
  for j in range(4):gf.append([a[j],a[(j+1)%4],b[(j+1)%4],b[j]])
prism([(-.029,-.297,3.986),(.029,-.297,3.986),(.029,-.297,4.005),(-.029,-.297,4.005)],.014)
glmesh=bpy.data.meshes.new('Sunglasses_Black');glmesh.from_pydata(gv,[],gf);glmesh.update()
bm=bmesh.new();bm.from_mesh(glmesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(glmesh);bm.free()
glasses=bpy.data.objects.new('Sunglasses',glmesh);s.collection.objects.link(glasses)
# Fit lenses to the painted eye line while retaining the ear contact points.
for vertex in glmesh.vertices:vertex.co.z-=.055*max(0,min(1,(.02-vertex.co.y)/.22))
guv=glmesh.uv_layers.new(name='UVMap')
for p in glmesh.polygons:
 for li in p.loop_indices:
  co=glmesh.vertices[glmesh.loops[li].vertex_index].co;guv.data[li].uv=(.5+co.x,.5+(co.z-3.98))
black=bpy.data.materials.new('SunglassesBlack');black.use_nodes=True;black.diffuse_color=(.008,.010,.014,1);black.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.008,.010,.014,1);black.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.32;glmesh.materials.append(black)
# Use the existing authored relaxed standing pose on a private copy. The game
# mesh is baked, while a hidden original rig remains editable in the .blend.
with bpy.data.libraries.load(str(R/'ArtSource/LowPolyMaleSlowWalk/IdleTalk/LowPolyMale_IdleTalk.blend'),link=False) as (src,dst):dst.objects=['SlowWalkRig','Cube']
rig,authored=dst.objects
for obj in [rig,authored]:s.collection.objects.link(obj)
assert len(authored.data.vertices)==len(m.data.vertices)
assert [tuple(p.vertices) for p in authored.data.polygons]==[tuple(p.vertices) for p in m.data.polygons]
for old,newuv in zip(authored.data.uv_layers.active.data,m.data.uv_layers.active.data):old.uv=newuv.uv
authored.data.materials.clear();authored.data.materials.append(final)
rig.data.pose_position='POSE';s.frame_set(1);bpy.context.view_layer.update()
skin=rig.matrix_world@rig.pose.bones['Head'].matrix@rig.data.bones['Head'].matrix_local.inverted()@rig.matrix_world.inverted()
glmesh.transform(skin)
dg=bpy.context.evaluated_depsgraph_get();posed=bpy.data.meshes.new_from_object(authored.evaluated_get(dg),depsgraph=dg)
bpy.data.objects.remove(m,do_unlink=True)
m=bpy.data.objects.new('Body',posed);m.matrix_world=authored.matrix_world.copy();s.collection.objects.link(m)
authored.name='AuthoringBody_DoNotExport';rig.name='AuthoringRig_DoNotExport';authored.hide_render=True;rig.hide_render=True;authored.hide_set(True);rig.hide_set(True)
height=max((m.matrix_world@v.co).z for v in m.data.vertices)-min((m.matrix_world@v.co).z for v in m.data.vertices)
scale=1.85/height;matrix=Matrix.Scale(scale,4)@m.matrix_world
m.data.transform(matrix);m.matrix_world=Matrix.Identity(4)
glmesh.transform(Matrix.Scale(scale,4)@glasses.matrix_world);glasses.matrix_world=Matrix.Identity(4)
minz=min(v.co.z for v in m.data.vertices)
for vtx in m.data.vertices:vtx.co.z-=minz
for vtx in glmesh.vertices:vtx.co.z-=minz
m.data.update();m.data.calc_loop_triangles()
assert len(m.data.loop_triangles)==964 and len(m.data.vertices)==484
root=bpy.data.objects.new('NavySuitVillain',None);s.collection.objects.link(root)
for obj in [m,glasses]:obj.parent=root
bpy.ops.object.select_all(action='DESELECT')
for obj in [root,m,glasses]:obj.select_set(True)
bpy.context.view_layer.objects.active=root
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash
bpy.ops.wm.save_as_mainfile(filepath=str(W/'NavySuitVillain_LowPoly.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'NavySuitVillain_LowPoly.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
meshdata={}
for obj in [m,glasses]:
 me=obj.data;me.calc_loop_triangles();positions=[];normals=[];uvs=[];triangles=[];lookup={}
 for t in me.loop_triangles:
  row=[]
  for li in t.loops:
   pos=me.vertices[me.loops[li].vertex_index].co;no=me.corner_normals[li].vector;uv=me.uv_layers.active.data[li].uv
   key=tuple(round(v,7) for v in (*pos,*no,*uv))
   if key not in lookup:lookup[key]=len(positions);positions.append(list(pos));normals.append(list(no));uvs.append(list(uv))
   row.append(lookup[key])
  triangles.append(row)
 meshdata[obj.name]={'positions':positions,'normals':normals,'uv':uvs,'triangles':triangles}
(W/'mesh-data.json').write_text(json.dumps(meshdata))
s.view_settings.view_transform='Standard';s.view_settings.look='None';s.view_settings.exposure=0;s.view_settings.gamma=1
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_shadows=True;s.display.shading.show_cavity=False;s.display.shading.show_specular_highlight=False
s.display.shading.background_type='WORLD';s.world=bpy.data.worlds.new('PreviewWorld');s.world.color=(.055,.055,.055)
s.render.resolution_x=650;s.render.resolution_y=850;s.render.resolution_percentage=100
for name,loc in [('front',(0,-6,1)),('three_quarter',(3,-6,1.3)),('back',(0,6,1)),('side',(6,0,1))]:
 bpy.ops.object.camera_add(location=loc);cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.1;s.camera=cam;s.render.filepath=str(W/('preview_'+name+'.png'));bpy.ops.render.render(write_still=True)
s.display.shading.light='FLAT'
bpy.ops.object.camera_add(location=(0,-6,1));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.1;s.camera=cam;s.render.filepath=str(W/'albedo_front.png');bpy.ops.render.render(write_still=True)
glmesh.calc_loop_triangles()
(W/'build-report.json').write_text(json.dumps({'model':str(OUT/'NavySuitVillain_LowPoly.fbx'),'objects':2,'body_vertices':484,'body_triangles':964,'sunglasses_triangles':len(glmesh.loop_triangles),'triangles':964+len(glmesh.loop_triangles),'height_metres':1.85,'texture':[512,512],'materials':2,'unique_left_right_uv':True,'source_sha256':source_hash,'source_unchanged':True,'rigged_game_export':False,'pose':'Baked relaxed standing pose; editable source rig hidden in .blend'},indent=2))
print('NAVY_SUIT_VILLAIN_BUILD_COMPLETE')
