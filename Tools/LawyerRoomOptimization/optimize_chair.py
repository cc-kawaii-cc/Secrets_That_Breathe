import bpy,bmesh,math,json
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
root=Path(__file__).resolve().parents[2];out=root/'Tools/LawyerRoomOptimization'
data=np.load(out/'source.npz');manifest=json.loads((out/'source.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True)
mesh=bpy.data.meshes.new('Exact saved Unity chair');mesh.from_pydata(data['positions'].tolist(),[],data['indices'].reshape(-1,3).tolist());mesh.update()
for key in ['uv','uv2']:
    layer=mesh.uv_layers.new(name=key)
    layer.data.foreach_set('uv',data[key][data['indices']].astype(np.float32).ravel())
for face in mesh.polygons:face.use_smooth=True
mesh.normals_split_custom_set_from_vertices(data['normals'].tolist())
o=bpy.data.objects.new('Chair',mesh);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True)
o.scale=(100,100,100);o.rotation_euler=(0,math.pi/2,0);bpy.context.view_layer.update()
material=bpy.data.materials.new('Neutral chair inspection');material.diffuse_color=(.15,.17,.19,1);mesh.materials.append(material)
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Audit');scene.world.color=(.18,.18,.18)
scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
sh=scene.display.shading;sh.light='STUDIO';sh.color_type='MATERIAL';sh.show_cavity=True;sh.cavity_type='BOTH';sh.show_shadows=True
points=[o.matrix_world@Vector(c) for c in o.bound_box];lo=Vector([min(v[i] for v in points) for i in range(3)]);hi=Vector([max(v[i] for v in points) for i in range(3)])
center=(lo+hi)/2;radius=(hi-lo).length/2
camera=bpy.data.objects.new('AuditCamera',bpy.data.cameras.new('AuditCamera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=radius*2.45
def render(tag):
    for i,d in enumerate([(1,-1,.6),(-1,1,.5),(0,-1,.15),(1,0,.15),(0,0,-1)]):
        camera.location=center+Vector(d).normalized()*radius*4;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(out/f'{tag}{i}.png');bpy.ops.render.render(write_still=True)
render('before')
original=mesh;original.use_fake_user=True;dg=bpy.context.evaluated_depsgraph_get();original_bvh=BVHTree.FromObject(o,dg)
original_points=[v.co.copy() for i,v in enumerate(original.vertices) if i%max(1,len(original.vertices)//16000)==0]
bounds=np.array(data['positions']);lo0=bounds.min(0);hi0=bounds.max(0);diag=float(np.linalg.norm(hi0-lo0))
trials=[]
for budget in [8000,10000,12000,16000]:
    if o.data!=original:previous=o.data;o.data=original;bpy.data.meshes.remove(previous)
    o.data=original.copy()
    # Reconnect only coincident vertices from the old normal/UV splits; keep UVs on corners.
    bm=bmesh.new();bm.from_mesh(o.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=diag*1e-7)
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=diag*1e-7)
    bm.to_mesh(o.data);bm.free();o.data.update()
    mod=o.modifiers.new('Preserve planar detail','DECIMATE');mod.decimate_type='DISSOLVE';mod.angle_limit=math.radians(.35);mod.delimit={'MATERIAL','UV','SHARP'}
    bpy.ops.object.modifier_apply(modifier=mod.name)
    count=sum(len(f.vertices)-2 for f in o.data.polygons)
    if count>budget:
        mod=o.modifiers.new('Chair triangle budget','DECIMATE');mod.decimate_type='COLLAPSE';mod.ratio=budget/count;mod.use_collapse_triangulate=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # Restore the 60-degree smooth/crease policy used by the original chair asset.
    bm=bmesh.new();bm.from_mesh(o.data)
    for face in bm.faces:face.smooth=True
    for edge in bm.edges:edge.smooth=edge.is_manifold and edge.calc_face_angle(0)<math.radians(60)
    bm.to_mesh(o.data);bm.free();o.data.update();bpy.context.view_layer.update()
    new_bvh=BVHTree.FromObject(o,bpy.context.evaluated_depsgraph_get());samples=[v.co.copy() for v in o.data.vertices]
    errors=[]
    for tree,points in [(original_bvh,samples),(new_bvh,original_points)]:
        for p in points:
            hit=tree.find_nearest(p);errors.append(hit[3] if hit[0] is not None else float('inf'))
    pos=np.array([tuple(v.co) for v in o.data.vertices]);bound_error=float(max(np.max(np.abs(pos.min(0)-lo0)),np.max(np.abs(pos.max(0)-hi0))))
    triangles=sum(len(f.vertices)-2 for f in o.data.polygons)
    trial={'budget':budget,'triangles':triangles,'surfaceErrorRatio':max(errors)/diag,'boundsErrorRatio':bound_error/diag};trials.append(trial)
    print('CHAIR_TRIAL',trial,flush=True)
    if max(errors)<=diag*.0025 and bound_error<=diag*.0025:break
else:raise RuntimeError('No chair candidate passed the shape tolerance')
# Sample the authored Smooth60 normals from the old surface. Recalculating
# normals on long simplified triangles created wavy highlights on the cushions.
transferred=[]
for face in o.data.polygons:
    for li in face.loop_indices:
        point=o.data.vertices[o.data.loops[li].vertex_index].co
        probe=point.lerp(face.center,.001)
        hit=original_bvh.find_nearest(probe)
        assert hit[0] is not None
        ids=list(original.polygons[hit[2]].vertices);assert len(ids)==3
        a,b,c=[original.vertices[i].co for i in ids]
        na,nb,nc=[Vector(data['normals'][i]) for i in ids]
        normal=barycentric_transform(hit[0],a,b,c,na,nb,nc)
        transferred.append(normal.normalized() if normal.length_squared>1e-12 else hit[1])
o.data.normals_split_custom_set(transferred);o.data.update()
render('after')
bpy.ops.wm.save_as_mainfile(filepath=str(out/'optimized-chair.blend'))
# Export split corner attributes as Unity vertex data; preserve both UV channels.
final=o.data;final.calc_loop_triangles();normals=final.corner_normals
positions=[];norm=[];uv=[];uv2=[];indices=[];unique={}
for tri in final.loop_triangles:
    for li in tri.loops:
        vi=final.loops[li].vertex_index;p=tuple(final.vertices[vi].co);n=tuple(normals[li].vector)
        a=tuple(final.uv_layers['uv'].data[li].uv);b=tuple(final.uv_layers['uv2'].data[li].uv);key=p+n+a+b
        if key not in unique:
            unique[key]=len(positions);positions.append(dict(zip('xyz',p)));norm.append(dict(zip('xyz',n)));uv.append(dict(zip('xy',a)));uv2.append(dict(zip('xy',b)))
        indices.append(unique[key])
payload={'name':manifest['name'],'positions':positions,'normals':norm,'uv':uv,'uv2':uv2,'indices':indices}
(out/'optimized-mesh.json').write_text(json.dumps(payload,separators=(',',':')),encoding='utf-8')
result={'beforeTriangles':manifest['triangles'],'afterTriangles':len(indices)//3,'beforeVertices':manifest['vertices'],
        'afterVertices':len(positions),'trials':trials,'done':True}
(out/'optimization.json').write_text(json.dumps(result,indent=2));print('CHAIR_OPTIMIZATION_DONE',result,flush=True)
