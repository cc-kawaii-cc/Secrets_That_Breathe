import bpy, os, json
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__)); BASE=os.path.dirname(OUT)
bpy.ops.wm.open_mainfile(filepath=os.path.join(BASE,'PorscheV2_Inspection.blend'))
scene=bpy.context.scene
for im in bpy.data.images:
    p=bpy.path.abspath(im.filepath)
    if os.path.exists(p): im.reload()
for mat in bpy.data.materials:
    if not mat.use_nodes: continue
    bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    out=next(n for n in mat.node_tree.nodes if n.type=='OUTPUT_MATERIAL')
    em=mat.node_tree.nodes.new('ShaderNodeEmission')
    if bs and bs.inputs['Base Color'].is_linked: mat.node_tree.links.new(bs.inputs['Base Color'].links[0].from_socket,em.inputs['Color'])
    elif bs: em.inputs['Color'].default_value=bs.inputs['Base Color'].default_value
    mat.node_tree.links.new(em.outputs[0],out.inputs['Surface'])
scene.render.engine='CYCLES'; scene.cycles.samples=8
scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard'
if scene.world is None: scene.world=bpy.data.worlds.new('World')
scene.world.color=(.06,.06,.06)
camdata=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',camdata);scene.collection.objects.link(cam);scene.camera=cam
camdata.type='ORTHO';camdata.ortho_scale=.62
cam.location=(-.65,-4,1.8);cam.rotation_euler=(Vector((-.65,-1.67,.69))-cam.location).to_track_quat('-Z','Y').to_euler()
with open(os.path.join(OUT,'uv_analysis.json')) as f: r=json.load(f)
selected=next(c for c in r['layers'][1]['islands'] if c['faces']==100 and c['max'][0]<-.5)
ids=set(selected['polygon_indices'])
ob=bpy.data.objects['Body']
mat=bpy.data.materials.new('Highlight');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Emission Color'].default_value=(.02,1,.02,1);bs.inputs['Emission Strength'].default_value=1;bs.inputs['Base Color'].default_value=(0,0,0,1)
ob.data.materials.append(mat);mi=len(ob.data.materials)-1
for p in ob.data.polygons:
    if p.index in ids:p.material_index=mi
scene.render.filepath=os.path.join(OUT,'highlight_100faces.png');bpy.ops.render.render(write_still=True)
me=ob.data.copy();dup=ob.copy();dup.data=me;scene.collection.objects.link(dup)
import bmesh
bm=bmesh.new();bm.from_mesh(me);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.index not in ids],context='FACES');bm.to_mesh(me);bm.free()
for p in dup.data.polygons:p.material_index=0
for obj in scene.objects:
    if obj.type=='MESH':obj.hide_render=obj!=dup
scene.render.filepath=os.path.join(OUT,'isolated_100faces.png');bpy.ops.render.render(write_still=True)
