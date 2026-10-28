import bpy,json,re
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent;A=R/'Assets/Model Palm/Police Car1'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(A/'isuzu.fbx'))
# Resolve the materials/textures assigned in the existing Unity prefab.
metas={re.search(r'^guid: ([a-f0-9]+)',p.read_text(),re.M)[1]:Path(str(p)[:-5]) for p in A.rglob('*.meta') if re.search(r'^guid: ([a-f0-9]+)',p.read_text(),re.M)}
for m in bpy.data.materials:
 path=A/'Material'/(m.name+'.mat')
 if not path.exists():continue
 text=path.read_text();base=re.search(r'- _BaseMap:\s+m_Texture: \{fileID: 2800000, guid: ([a-f0-9]+)',text)
 m.use_nodes=True;n=m.node_tree.nodes;bsdf=n.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.65
 if base and base[1] in metas:
  tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(metas[base[1]]));m.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
info=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 p=[o.matrix_world@v.co for v in o.data.vertices];o.data.calc_loop_triangles();info.append({'name':o.name,'vertices':len(p),'triangles':len(o.data.loop_triangles),'bounds':[[min(v[i] for v in p),max(v[i] for v in p)] for i in range(3)]})
(W/'inspection.json').write_text(json.dumps(info,indent=2))
bar=bpy.data.objects['police'];p=[bar.matrix_world@v.co for v in bar.data.vertices]
bar.data.calc_loop_triangles();p2=[];n2=[];uv2=[];t2=[]
for triangle in bar.data.loop_triangles:
 row=[]
 for li in triangle.loops:
  vi=bar.data.loops[li].vertex_index;row.append(len(p2));p2.append(list(bar.matrix_world@bar.data.vertices[vi].co));n2.append(list((bar.matrix_world.to_3x3()@bar.data.corner_normals[li].vector).normalized()));uv2.append(list(bar.data.uv_layers.active.data[li].uv))
 t2.append(row)
(W/'bar-mesh-source.json').write_text(json.dumps({'positions':p2,'normals':n2,'uv':uv2,'triangles':t2}))
(W/'bar-geometry.json').write_text(json.dumps({'coords':[list(v) for v in p],'faces':[list(f.vertices) for f in bar.data.polygons],'uv':[[list(bar.data.uv_layers.active.data[li].uv) for li in f.loop_indices] for f in bar.data.polygons]},indent=2))
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.color_type='TEXTURE';scene.display.shading.light='STUDIO';scene.display.shading.show_specular_highlight=False;scene.display.shading.show_cavity=True
scene.world=bpy.data.worlds.new('Audit');scene.world.color=(.15,.15,.15);scene.display.shading.background_type='WORLD';scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.render.resolution_x=1100;scene.render.resolution_y=850;scene.render.resolution_percentage=100
def render(name,eye,target,scale,baronly=False):
 for o in bpy.context.scene.objects:
  if o.type=='MESH':o.hide_render=baronly and o!=bar
 bpy.ops.object.camera_add(location=eye);c=bpy.context.object;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=scale;scene.camera=c;scene.render.filepath=str(W/(name+'.png'));bpy.ops.render.render(write_still=True)
render('before_car',(6,-7,4),(0,0,1),6.5)
center=sum(p,Vector())/len(p)
render('before_bar',center+Vector((1.1,-1.7,.9)),center,1.18,True)
render('before_bar_top',center+Vector((0,0,3)),center,1.18,True)
for o in bpy.context.scene.objects:
 if o.type=='MESH':o.hide_render=False
bpy.ops.wm.save_as_mainfile(filepath=str(W/'Isuzu_Inspection.blend'))
print('ISUZU_AUDIT',json.dumps(info))
