import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/SecretsThatBreathe/Props/DocumentBinder'
SRC=ROOT/'ArtSource/DocumentBinder'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
def material(name, color, roughness):
    m=bpy.data.materials.new(name); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=color; p.inputs['Roughness'].default_value=roughness
    return m
leather=material('Binder_Leather',(1,1,1,1),.62)
im=bpy.data.images.load(str(OUT/'Textures/Binder_Leather.png'))
tex=leather.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=im
leather.node_tree.links.new(tex.outputs['Color'],leather.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
edge=material('Binder_Edge',(.018,.018,.02,1),.72)
# Extruded U section: empty cavity, 6 mm covers (previously 4 mm),
# 8 mm spine and covers narrowing toward a 6 mm opening at the tips.
section=[(-.14,-.0375),(.14,-.009),(.14,-.003),(-.132,-.0315),
         (-.132,.0315),(.14,.003),(.14,.009),(-.14,.0375)]
verts=[(x,y,z) for z in [0,.32] for x,y in section]
faces=[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
faces += [tuple(reversed(range(8))),tuple(range(8,16))]
mats=[0,1,1,1,1,1,0,0,1,1]
mesh=bpy.data.meshes.new('DocumentBinder_SingleMesh'); mesh.from_pydata(verts,[],faces); mesh.update()
obj=bpy.data.objects.new('DocumentBinder_SingleMesh',mesh); bpy.context.collection.objects.link(obj)
for m in [leather,edge]: mesh.materials.append(m)
uv=mesh.uv_layers.new(name='BinderAtlas')
for p,mi in zip(mesh.polygons,mats):
    p.material_index=mi
    for li in p.loop_indices:
        x,y,z=mesh.vertices[mesh.loops[li].vertex_index].co
        if p.normal.x<-.9: u=.447+((y+.0375)/.075)*.106
        elif p.normal.y<-.9: u=.567+((x+.14)/.28)*.428
        else: u=.004+(1-(x+.14)/.28)*.428
        uv.data[li].uv=(u,.005+z/.32*.990)
bpy.context.view_layer.objects.active=obj; obj.select_set(True)
# Triangulate concave top/bottom caps explicitly for reliable FBX import.
bm=bmesh.new(); bm.from_mesh(mesh)
bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if len(f.verts)>4]); bm.to_mesh(mesh); bm.free()
bm=bmesh.new(); bm.from_mesh(mesh); bm.verts.ensure_lookup_table()
assert all(e.is_manifold for e in bm.edges), 'Mesh must be closed'
# Connected component check without relying on object count.
seen=set(); pending=[bm.verts[0]]
while pending:
    v=pending.pop()
    if v in seen: continue
    seen.add(v); pending.extend(e.other_vert(v) for e in v.link_edges)
assert len(seen)==len(bm.verts), 'Mesh must be one connected piece'
bm.free()
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'DocumentBinder.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/DocumentBinder.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=48
scene.world.color=(.25,.25,.25)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001)); floor=bpy.context.object; floor.data.materials.append(material('PreviewFloor',(.16,.18,.20,1),.9))
def aim(o,p): o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((-.7,-.8,1),55,.8),((.3,.6,.8),40,.6),((-.6,.2,.5),20,.5)]:
    bpy.ops.object.light_add(type='AREA',location=pos); light=bpy.context.object; light.data.energy=power; light.data.shape='DISK'; light.data.size=size; aim(light,(0,0,.15))
bpy.ops.object.camera_add(location=(-.62,-.8,.55)); cam=bpy.context.object; aim(cam,(0,0,.16)); cam.data.type='ORTHO'; cam.data.ortho_scale=.47; scene.camera=cam
scene.render.resolution_x=1000; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'; scene.render.filepath=str(SRC/'DocumentBinder_Preview.png'); bpy.ops.render.render(write_still=True)
cam.location=(.65,-.8,.6); aim(cam,(0,0,.16)); scene.render.filepath=str(SRC/'DocumentBinder_EmptyPreview.png'); bpy.ops.render.render(write_still=True)
cam.location=(-.05,-.14,.9); aim(cam,(0,0,.16)); scene.render.filepath=str(SRC/'DocumentBinder_TopPreview.png'); bpy.ops.render.render(write_still=True)
report={'connected_components':1,'closed_manifold':True,'mesh_objects':1,'vertices':len(mesh.vertices),'triangles':sum(len(p.vertices)-2 for p in mesh.polygons),'dimensions_m':[.28,.075,.32],'hole':False,'paper':False,'cover_thickness_m':.006,'tip_gap_m':.006}
(SRC/'verification.json').write_text(json.dumps(report,indent=2)); print(report)
