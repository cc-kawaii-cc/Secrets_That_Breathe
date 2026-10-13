import bpy, math, os
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/SecretsThatBreathe/Props/LouveredLight'
SOURCE = ROOT / 'ArtSource/LouveredLight'
OUT.joinpath('Models').mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def material(name, color, metal=0, rough=.3, emission=0):
    m = bpy.data.materials.new(name); m.diffuse_color = (*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
    p.inputs['Emission Color'].default_value=(*color,1); p.inputs['Emission Strength'].default_value=emission
    return m
white=material('Housing_White',(.78,.81,.84),.12,.28)
chrome=material('Reflector_Chrome',(.78,.82,.86),.88,.2)
glass=material('Tube_Emission',(.88,.95,1),0,.2,4)
cap=material('EndCaps_Aluminium',(.44,.48,.52),.8,.27)
socket=material('Sockets_Ivory',(.72,.73,.69),0,.5)
dark=material('Fasteners',(.13,.15,.17),.7,.3)
parts=[]
def mesh(name, verts, faces, mat):
    me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update()
    ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob); ob.data.materials.append(mat); parts.append(ob)
    return ob
def box(name, pos, size, mat, bevel=.001):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos); ob=bpy.context.object; ob.name=name
    ob.dimensions=size; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); ob.data.materials.append(mat)
    if bevel:
        mod=ob.modifiers.new('Folded softened edges','BEVEL'); mod.width=bevel; mod.segments=1
        bpy.context.view_layer.objects.active=ob; bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL'); bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(ob); return ob
def cylinder(name,pos,radius,length,mat,along_y=True,segments=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=segments,radius=radius,depth=length,location=pos)
    ob=bpy.context.object; ob.name=name
    if along_y: ob.rotation_euler[0]=math.pi/2
    ob.data.materials.append(mat)
    for p in ob.data.polygons: p.use_smooth=len(p.vertices)==4
    parts.append(ob); return ob

# Ceiling contact plane at z=0; underside faces -Z (Unity -Y).
box('Backplate',(0,0,-.003),(.31,1.24,.006),white)
for x in [-.148,.148]:
    box('Housing side',(x,0,-.039),(.014,1.24,.078),white,.002)
    box('Folded bottom rim',(x,0,-.077),(.023,1.24,.003),white)
for y in [-.608,.608]: box('Housing end',(0,y,-.039),(.282,.024,.078),white,.002)

# Two curved, closed polished reflector troughs with upward lips.
for cx in [-.074,.074]:
    n=10; verts=[]
    for y in [-.574,.574]:
        for dz in [0,.001]:
            for i in range(n+1):
                u=-1+2*i/n; verts.append((cx+.066*u,y,-.014-.040*u*u+dz))
    stride=n+1; faces=[]
    for i in range(n):
        faces += [(i,i+1,2*stride+i+1,2*stride+i),
                  (stride+i,3*stride+i,3*stride+i+1,stride+i+1),
                  (i,stride+i,stride+i+1,i+1),
                  (2*stride+i,2*stride+i+1,3*stride+i+1,3*stride+i)]
    faces += [(0,2*stride,3*stride,stride),(n,stride+n,3*stride+n,2*stride+n)]
    mesh('Parabolic reflector',verts,faces,chrome)
    cylinder('T8 fluorescent glass',(cx,0,-.039),.013,1.10,glass,segments=16)
    for y in [-.558,.558]:
        cylinder('Metal tube end cap',(cx,y,-.039),.0133,.017,cap)
        box('Lamp holder',(cx,y*1.027,-.030),(.034,.022,.047),socket,.002)
        for px in [-.006,.006]: cylinder('Contact pin',(cx+px,y*1.02,-.039),.0012,.008,cap,segments=8)

# Twelve transverse V-folded aluminium blades; narrow lower edges leave tubes visible.
for j in range(12):
    y=-.535+j*(1.07/11)
    cross=[(-.006,-.055),(-.0012,-.075),(.0012,-.075),(.006,-.055)]
    verts=[(x,y+dy,z) for x in [-.137,.137] for dy,z in cross]
    faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    mesh('Folded cross louver',verts,faces,chrome)
box('Centre spine',(0,0,-.065),(.009,1.155,.022),chrome,.001)
for x in [-.132,.132]:
    for y in [-.577,.577]: cylinder('Captive screw',(x,y,-.079),.003,.0018,dark,False,8)

# Combine by material to keep the final asset to six renderers.
for mat in [white,chrome,glass,cap,socket,dark]:
    bpy.ops.object.select_all(action='DESELECT')
    selected=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.data.materials[0]==mat]
    for o in selected: o.select_set(True)
    bpy.context.view_layer.objects.active=selected[0]; bpy.ops.object.join()
    bpy.context.object.name=mat.name
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False); bpy.ops.object.mode_set(mode='OBJECT')
models=list(bpy.context.scene.objects)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'LouveredLight_T8_2x36W.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in models: o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/LouveredLight_T8_2x36W.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)

scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=48
scene.cycles.use_denoising=True
scene.world.color=(.25,.25,.25)
def area(name,pos,power,size):
    bpy.ops.object.light_add(type='AREA',location=pos); ob=bpy.context.object; ob.name=name; ob.data.energy=power; ob.data.shape='DISK'; ob.data.size=size
    ob.rotation_euler=(Vector((0,0,-.03))-ob.location).to_track_quat('-Z','Y').to_euler()
area('Studio key',(-1,-.7,-1.3),100,1.8)
area('Studio strip',(1,.8,-.5),80,1.2)
bpy.ops.object.camera_add(location=(1.05,-1.45,-1.2)); camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,-.035))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'; camera.data.ortho_scale=1.6; scene.camera=camera
scene.render.resolution_x=1400; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
scene.render.image_settings.file_format='PNG'; scene.render.filepath=str(SOURCE/'LouveredLight_Preview.png')
bpy.ops.render.render(write_still=True)
print('LAMP_MODEL_PASS',sum(len(o.data.polygons) for o in models),'polygons')
