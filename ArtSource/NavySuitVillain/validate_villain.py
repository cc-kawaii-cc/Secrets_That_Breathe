import bpy,bmesh,json,re,hashlib,numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent;O=R/'Assets/SecretsThatBreathe/Characters/NavySuitVillain'
expected=json.loads((W/'mesh-data.json').read_text());build=json.loads((W/'build-report.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(O/'NavySuitVillain_LowPoly.fbx'))
objects={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'};assert set(objects)=={'Body','Sunglasses'}
report={'meshes':{},'export_triangles':0,'texture':[512,512],'native_meshes_checked':True,'static_game_pose':True,'unity_editor_visual_check':False}
for name,o in objects.items():
 d=expected[name];p=[o.matrix_world@v.co for v in o.data.vertices];tree=KDTree(len(p))
 for i,v in enumerate(p):tree.insert(v,i)
 tree.balance();error=max(tree.find(Vector(v))[2] for v in d['positions']);assert error<1e-5,(name,error)
 o.data.calc_loop_triangles();assert len(o.data.loop_triangles)==len(d['triangles'])
 assert len(o.data.uv_layers)==1 and len(o.data.materials)==1
 bm=bmesh.new();bm.from_mesh(o.data);assert all(e.is_manifold for e in bm.edges);assert all(f.calc_area()>1e-11 for f in bm.faces);assert bm.calc_volume(signed=True)>0;bm.free()
 native=(O/'Meshes'/('NavySuitVillain_'+name+'.asset')).read_text();v=np.frombuffer(bytes.fromhex(re.search(r'_typelessdata: ([a-f0-9]+)',native)[1]),dtype='<f4').reshape(-1,14);t=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([a-f0-9]+)',native)[1]),dtype='<u2').reshape(-1,3)
 xyz=np.array(d['positions'])[:,[0,2,1]]*[-1,1,-1];assert np.allclose(v[:,:3],xyz,atol=1e-7)
 assert np.array_equal(t,np.array(d['triangles'])[:,[0,2,1]])
 assert np.allclose(np.linalg.norm(v[:,3:6],axis=1),1,atol=1e-5)
 report['meshes'][name]={'triangles':len(t),'surface_error_metres':error,'closed_parts':True,'native_vertices':len(v)};report['export_triangles']+=len(t)
body=objects['Body'];world=[body.matrix_world@v.co for v in body.data.vertices];height=max(v.z for v in world)-min(v.z for v in world);assert abs(height-1.85)<1e-5
assert report['export_triangles']==1072
report['height_metres']=height
assert hashlib.sha256((R/'Assets/Nemo_Upgrade/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx').read_bytes()).hexdigest()==build['source_sha256']
report['original_base_mesh_unchanged']=True
image=bpy.data.images.load(str(O/'Textures/NavySuitVillain_BaseColor.png'));assert tuple(image.size)==(512,512)
for name,o in objects.items():
 mat=o.data.materials[0];mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF')
 if name=='Body':
  tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 else:mat.diffuse_color=(.008,.010,.014,1);bs.inputs['Base Color'].default_value=(.008,.010,.014,1)
s=bpy.context.scene;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='FLAT';s.display.shading.color_type='TEXTURE';s.display.shading.show_specular_highlight=False;s.world=bpy.data.worlds.new('ExportReview');s.world.color=(.13,.13,.13);s.display.shading.background_type='WORLD';s.view_settings.view_transform='Standard';s.view_settings.look='None';s.render.resolution_x=700;s.render.resolution_y=900;s.render.resolution_percentage=100
for name,eye,target,scale in [('front',(0,-6,1),(0,0,.92),2.1),('three_quarter',(3,-6,1.3),(0,0,.92),2.1),('side',(6,0,1),(0,0,.92),2.1),('face',(1.5,-4,1.9),(0,0,1.67),.46)]:
 bpy.ops.object.camera_add(location=eye);c=bpy.context.object;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=scale;s.camera=c;s.render.filepath=str(W/('export_'+name+'.png'));bpy.ops.render.render(write_still=True)
prefab=(O/'NavySuitVillain.prefab').read_text();assert 'm_Name: Sunglasses' in prefab and 'CapsuleCollider:' in prefab
ids={int(m[1]) for m in re.finditer(r'^--- !u!\d+ &(\d+)',prefab,re.M)}
for v in re.findall(r'\{fileID: (\d+)\}',prefab):assert int(v)==0 or int(v) in ids
(W/'validation.json').write_text(json.dumps(report,indent=2));print('VILLAIN_VALIDATION',json.dumps(report))
