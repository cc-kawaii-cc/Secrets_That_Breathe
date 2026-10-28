import bpy,bmesh,json,hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent;OUT=R/'Assets/SecretsThatBreathe/Characters/ThaiPolice'
bpy.ops.wm.open_mainfile(filepath=str(W/'ThaiPolice_LowPoly.blend'))
m=bpy.data.objects['ThaiPolice_Body'];reference=[m.matrix_world@v.co for v in m.data.vertices]
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(OUT/'ThaiPolice_LowPoly.fbx'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(meshes)==1
m=meshes[0];m.data.calc_loop_triangles();assert len(m.data.vertices)==484;assert len(m.data.loop_triangles)==964
positions=[m.matrix_world@v.co for v in m.data.vertices];height=max(p.z for p in positions)-min(p.z for p in positions)
assert abs(height-1.8)<1e-5
tree=KDTree(len(positions))
for i,p in enumerate(positions):tree.insert(p,i)
tree.balance();error=max(tree.find(p)[2] for p in reference);assert error<1e-5
bm=bmesh.new();bm.from_mesh(m.data);assert all(e.is_manifold for e in bm.edges);assert all(f.calc_area()>1e-10 for f in bm.faces);bm.free()
assert not [o for o in bpy.context.scene.objects if o.type=='ARMATURE']
assert len(m.data.materials)==1 and m.data.materials[0].name=='ThaiPolice'
assert len(m.data.uv_layers)==1
image=bpy.data.images.load(str(OUT/'Textures/ThaiPolice_BaseColor.png'));assert tuple(image.size)==(512,512)
mat=bpy.data.materials.new('Validation_ThaiPolice');mat.use_nodes=True;tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image
bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.85;mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);m.data.materials.clear();m.data.materials.append(mat)
s=bpy.context.scene;s.render.engine='BLENDER_WORKBENCH';s.display.shading.color_type='TEXTURE';s.display.shading.light='FLAT';s.display.shading.show_shadows=False;s.display.shading.show_cavity=False
s.world=bpy.data.worlds.new('ValidationWorld');s.world.color=(.08,.08,.08);s.display.shading.background_type='WORLD';s.view_settings.view_transform='Standard';s.view_settings.look='None'
s.render.resolution_x=650;s.render.resolution_y=850;s.render.resolution_percentage=100
for name,loc in [('front',(0,-6,1)),('three_quarter',(3,-6,1.3)),('back',(0,6,1))]:
 bpy.ops.object.camera_add(location=loc);cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.1;s.camera=cam;s.render.filepath=str(W/('export_'+name+'.png'));bpy.ops.render.render(write_still=True)
baseline=json.loads((W/'build-report.json').read_text())
assert hashlib.sha256((R/'Assets/Nemo_Upgrade/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx').read_bytes()).hexdigest()==baseline['source_sha256']
report={'reimported_objects':1,'vertices':484,'triangles':964,'height_metres':height,'export_surface_error':error,'texture_size':[512,512],'materials':1,'closed_manifold':True,'source_unchanged':True,'unity_editor_visual_check':False,'preview_lighting':'Flat albedo; actual exported mesh and atlas'}
(W/'validation.json').write_text(json.dumps(report,indent=2));print('POLICE_VALIDATION',json.dumps(report))
