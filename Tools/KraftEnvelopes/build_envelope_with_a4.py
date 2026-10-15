import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/SecretsThatBreathe/Props/KraftEnvelopes'
SRC=ROOT/'ArtSource/KraftEnvelopes'
# Reuse the existing envelope's exact mesh construction and original photo UVs.
source=ROOT/'Tools/KraftEnvelopes/build_envelopes.py'
scope={'__file__':str(source)}
exec(compile(source.read_text(encoding='utf-8-sig').split('closed=make')[0],str(source),'exec'),scope)
envelope=scope['make']('KraftEnvelope_Open_WithA4',True)
assert len(envelope.data.vertices)>=12
flap=list(envelope.data.vertices)[-12:]
assert all(v.co.y>=.16249 for v in flap)
for i,v in enumerate(flap): v.co.z=.0008+(i//6)*.001
envelope.data.update()
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/SecretsThatBreathe/Props/A4Documents/Models/A4Document_Single.fbx'))
paper=next(ob for ob in bpy.context.selected_objects if ob.type=='MESH')
bpy.context.view_layer.objects.active=paper
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
assert abs(paper.dimensions.y-.297)<.0001 and abs(paper.dimensions.x-.21)<.0001
paper.location=(0,.1625+.297*.30-.297/2,.002)
paper.data.uv_layers.active.name='PhotoUV'
# Make the paper look identical to the A4 prefab, including its blank reverse.
for mat in paper.data.materials:
    if not mat: continue
    mat.use_nodes=True
    p=mat.node_tree.nodes.get('Principled BSDF'); p.inputs['Roughness'].default_value=.92
    if 'Printed' in mat.name:
        tex=mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.extension='EXTEND'
        tex.image=bpy.data.images.load(str(ROOT/'Assets/SecretsThatBreathe/Props/A4Documents/Textures/A4Document_Reference.png'))
        mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
    else: p.inputs['Base Color'].default_value=(.92,.92,.90,1)
bpy.ops.object.select_all(action='DESELECT'); envelope.select_set(True); paper.select_set(True)
bpy.context.view_layer.objects.active=envelope; bpy.ops.object.join()
combined=bpy.context.object; combined.name='KraftEnvelope_Open_WithA4'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/KraftEnvelope_Open_WithA4.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'KraftEnvelope_Open_WithA4.blend'))
triangles=sum(len(p.vertices)-2 for p in combined.data.polygons)
assert triangles==124
(SRC/'EnvelopeWithA4Verification.json').write_text(json.dumps({'triangles':triangles,'exposed_fraction':.30,'exposed_length_m':.0891,'paper_size_m':[.21,.297],'paper_bottom_height_m':.002,'flap_top_height_m':.0018},indent=2))
floor=bpy.data.materials.new('PreviewFloor'); floor.diffuse_color=(.12,.14,.16,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001)); bpy.context.object.data.materials.append(floor)
def aim(ob,p): ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((-.7,-.8,1),28,.8),((.3,.6,.8),18,.6)]:
    bpy.ops.object.light_add(type='AREA',location=pos); light=bpy.context.object; light.data.energy=power; light.data.size=size; aim(light,(0,0,0))
bpy.ops.object.camera_add(location=(.28,-.48,.82)); cam=bpy.context.object; cam.data.type='ORTHO'; cam.data.ortho_scale=.52; aim(cam,(0,.045,.002))
scene=bpy.context.scene; scene.camera=cam; scene.world.color=(.17,.17,.17); scene.render.engine='CYCLES'; scene.cycles.samples=40
scene.render.resolution_x=1000; scene.render.resolution_y=1100; scene.render.resolution_percentage=100; scene.view_settings.view_transform='AgX'
scene.render.filepath=str(SRC/'KraftEnvelope_Open_WithA4_Preview.png'); bpy.ops.render.render(write_still=True)
