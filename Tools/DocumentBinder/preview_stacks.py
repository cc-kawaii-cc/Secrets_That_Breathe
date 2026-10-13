import bpy, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'ArtSource/DocumentBinder'
bpy.ops.wm.open_mainfile(filepath=str(SRC/'DocumentBinder.blend'))
single=bpy.data.objects['DocumentBinder_SingleMesh']
single.hide_render=True
def mat(name,col):
    m=bpy.data.materials.new(name); m.diffuse_color=(*col,1); return m
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001)); bpy.context.object.data.materials.append(mat('Floor',(.16,.18,.20)))
def aim(o,p): o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((-.7,-.8,1),55,.8),((.3,.6,.8),40,.6),((-.6,.2,.5),20,.5)]:
    bpy.ops.object.light_add(type='AREA',location=pos); light=bpy.context.object; light.data.energy=power; light.data.shape='DISK'; light.data.size=size; aim(light,(0,-.16,.1))
bpy.ops.object.camera_add(location=(-.65,-.85,.6)); cam=bpy.context.object; cam.data.type='ORTHO'; cam.data.ortho_scale=.54
scene=bpy.context.scene; scene.camera=cam; scene.world.color=(.25,.25,.25)
scene.render.engine='CYCLES'; scene.cycles.samples=32
scene.render.resolution_x=1000; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
layers=[]
for count in [2,3]:
    for ob in layers: bpy.data.objects.remove(ob,do_unlink=True)
    layers=[]
    for i in range(count):
        ob=single.copy(); ob.data=single.data; bpy.context.collection.objects.link(ob); ob.hide_render=False
        ob.rotation_euler=(math.pi/2,0,0); ob.location=(0,0,.0375+i*.075); layers.append(ob)
    aim(cam,(0,-.16,count*.075/2)); scene.render.filepath=str(SRC/f'DocumentBinder_Stack{count}_Preview.png'); bpy.ops.render.render(write_still=True)
