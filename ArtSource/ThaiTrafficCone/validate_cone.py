import bpy,bmesh,json,re
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
import numpy as np
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent;O=R/'Assets/SecretsThatBreathe/Props/ThaiTrafficCone'
bpy.ops.wm.open_mainfile(filepath=str(W/'ThaiTrafficCone_Worn.blend'))
o=bpy.data.objects['ThaiTrafficCone_Worn'];expected=[o.matrix_world@v.co for v in o.data.vertices]
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(O/'ThaiTrafficCone_Worn.fbx'))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(objects)==1
o=objects[0];me=o.data;me.calc_loop_triangles();assert len(me.loop_triangles)==288
p=[o.matrix_world@v.co for v in me.vertices];tree=KDTree(len(p))
for i,v in enumerate(p):tree.insert(v,i)
tree.balance();error=max(tree.find(v)[2] for v in expected);assert error<1e-6
bounds=np.array([[min(v[i] for v in p),max(v[i] for v in p)] for i in range(3)])
assert np.allclose(bounds[:,1]-bounds[:,0],[.36,.36,.7],atol=1e-6)
bm=bmesh.new();bm.from_mesh(me);assert all(e.is_manifold for e in bm.edges);assert all(f.calc_area()>1e-10 for f in bm.faces);assert bm.calc_volume(signed=True)>0;bm.free()
assert len(me.materials)==1 and me.materials[0].name=='ThaiTrafficCone_Worn'
image=bpy.data.images.load(str(O/'Textures/ThaiTrafficCone_Worn_BaseColor.png'));assert tuple(image.size)==(512,512)
# Check native vertex positions, winding, normals and UVs against the authored export.
text=(O/'Meshes/ThaiTrafficCone_Worn.asset').read_text()
v=np.frombuffer(bytes.fromhex(re.search(r'_typelessdata: ([a-f0-9]+)',text)[1]),dtype='<f4').reshape(-1,14)
t=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([a-f0-9]+)',text)[1]),dtype='<u2').reshape(-1,3)
blenderpos=v[:,[0,2,1]]*[-1,-1,1]
native_error=max(tree.find(Vector(row))[2] for row in blenderpos);assert native_error<1e-6
face=np.cross(v[t[:,1],:3]-v[t[:,0],:3],v[t[:,2],:3]-v[t[:,0],:3])
normal=v[t,3:6].mean(1);assert np.all(np.sum(face*normal,axis=1)>0)
assert np.all((v[:,10:12]>=0)&(v[:,10:12]<=1))
# Verify actual through-openings from the reimported geometry by a centre-axis ray.
hit,loc,n,index=o.ray_cast(o.matrix_world.inverted()@Vector((0,0,.8)),o.matrix_world.inverted().to_3x3()@Vector((0,0,-1)))
assert not hit,'Centre passage unexpectedly capped'
mat=me.materials[0];mat.use_nodes=True;bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.88
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
s=bpy.context.scene;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_specular_highlight=False
s.view_settings.view_transform='Standard';s.view_settings.look='None';s.world=bpy.data.worlds.new('ExportReview');s.world.color=(.18,.18,.18);s.display.shading.background_type='WORLD'
s.render.resolution_x=800;s.render.resolution_y=900;s.render.resolution_percentage=100
for tag,eye in [('front',(.9,-1.5,1)),('back',(-1,1.3,.85))]:
 bpy.ops.object.camera_add(location=eye);c=bpy.context.object;c.rotation_euler=(Vector((0,0,.35))-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=.94;s.camera=c;s.render.filepath=str(W/('export_'+tag+'.png'));bpy.ops.render.render(write_still=True)
report={'fbx_objects':1,'fbx_triangles':len(me.loop_triangles),'fbx_vertices':len(me.vertices),'export_surface_error_metres':error,'native_mesh_surface_error_metres':native_error,'native_winding_matches_normals':True,'centre_open_through':True,'closed_manifold_shell':True,'dimensions_blender_metres':list(bounds[:,1]-bounds[:,0]),'texture':[512,512],'materials':1,'live_unity_import_or_collision_test':False}
(W/'validation.json').write_text(json.dumps(report,indent=2));print('CONE_VALIDATION',json.dumps(report))
