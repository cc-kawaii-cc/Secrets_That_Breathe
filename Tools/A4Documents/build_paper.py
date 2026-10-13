import bpy, math, json, random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'Assets/SecretsThatBreathe/Props/A4Documents'; SRC=ROOT/'ArtSource/A4Documents'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
printed=bpy.data.materials.new('A4_Printed'); printed.use_nodes=True
p=printed.node_tree.nodes.get('Principled BSDF'); p.inputs['Roughness'].default_value=.92
tex=printed.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=bpy.data.images.load(str(OUT/'Textures/A4Document_Reference.png')); tex.extension='EXTEND'
printed.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
blank=bpy.data.materials.new('A4_Blank'); blank.use_nodes=True
p=blank.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(.92,.92,.90,1); p.inputs['Roughness'].default_value=.92
def sheet():
    bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,.00009)); ob=bpy.context.object
    ob.name='A4Document'; ob.dimensions=(.210,.297,.00018); bpy.ops.object.transform_apply(location=True,rotation=False,scale=True)
    ob.data.materials.append(printed); ob.data.materials.append(blank)
    uv=ob.data.uv_layers.active
    # Preserve the reference's printed aspect ratio within the A4 sheet.
    image_ratio=tex.image.size[0]/tex.image.size[1]; vscale=(.297/.210)*image_ratio
    for poly in ob.data.polygons:
        poly.material_index=0 if poly.normal.z>.9 else 1
        for li in poly.loop_indices:
            x,y,z=ob.data.vertices[ob.data.loops[li].vertex_index].co
            uv.data[li].uv=(x/.210+.5,y/.297*vscale+.5)
    return ob
single=sheet()
def export(ob,name):
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active=ob
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/f'{name}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
export(single,'A4Document_Single')
stacks=[]
for count in [10,20]:
    rng=random.Random(71+count); layers=[]
    for i in range(count):
        ob=single.copy(); ob.data=single.data.copy(); bpy.context.collection.objects.link(ob)
        ob.location=(rng.uniform(-.008,.008),rng.uniform(-.009,.009),i*.00018)
        ob.rotation_euler.z=math.radians(rng.uniform(-5,5)); layers.append(ob)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in layers: ob.select_set(True)
    bpy.context.view_layer.objects.active=layers[0]; bpy.ops.object.join(); ob=bpy.context.object; ob.name=f'A4Documents_Stack{count}'
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True); export(ob,ob.name); stacks.append(ob)
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'A4Documents.blend'))
report={'a4_size_m':[.210,.297],'sheet_thickness_m':.00018,'triangles':{'single':12,'stack10':120,'stack20':240},'texture':'original supplied image, print aspect ratio preserved'}
(SRC/'verification.json').write_text(json.dumps(report,indent=2))
single.hide_render=True; stacks[0].hide_render=True
floor=bpy.data.materials.new('PreviewFloor'); floor.diffuse_color=(.12,.14,.16,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001)); bpy.context.object.data.materials.append(floor)
def aim(ob,point): ob.rotation_euler=(Vector(point)-ob.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((-.7,-.8,1),22,.8),((.3,.6,.8),15,.6)]:
    bpy.ops.object.light_add(type='AREA',location=pos); light=bpy.context.object; light.data.energy=power; light.data.size=size; aim(light,(0,0,0))
bpy.ops.object.camera_add(location=(.38,-.48,.65)); cam=bpy.context.object; cam.data.type='ORTHO'; cam.data.ortho_scale=.48; aim(cam,(0,0,.002))
scene=bpy.context.scene; scene.camera=cam; scene.world.color=(.15,.15,.15); scene.render.engine='CYCLES'; scene.cycles.samples=32
scene.render.resolution_x=1000; scene.render.resolution_y=1100; scene.render.resolution_percentage=100; scene.view_settings.view_transform='AgX'
scene.render.filepath=str(SRC/'A4Documents_Stack20_Preview.png'); bpy.ops.render.render(write_still=True)
