import bpy,json,os,sys
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];out=Path(os.environ.get('GUY_OPT_OUTPUT',str(root/'Tools/GUYOptimization')))
config=json.loads((out/'config.json').read_text())
index=int(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else 2
for tag in ['before','after']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if tag=='before':bpy.ops.import_scene.fbx(filepath=str(root/config[index]['path']))
    else:bpy.ops.wm.open_mainfile(filepath=str(out/f'Model{index}/optimized.blend'))
    scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE'
    scene.world=bpy.data.worlds.new('Lit check');scene.world.color=(.3,.3,.3)
    meshes=[o for o in scene.objects if o.type=='MESH'];world=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    lo=Vector([min(v[i] for v in world) for i in range(3)]);hi=Vector([max(v[i] for v in world) for i in range(3)])
    center=(lo+hi)/2;radius=(hi-lo).length/2
    camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(camera);scene.camera=camera
    camera.location=center+Vector((.02,-.02,1))*radius*4;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO';camera.data.ortho_scale=radius*2.5
    light=bpy.data.objects.new('Key',bpy.data.lights.new('Key','AREA'));scene.collection.objects.link(light)
    light.location=center+Vector((-1,-1,2))*radius*3;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
    light.data.energy=500*radius*radius;light.data.shape='DISK';light.data.size=radius*1.5
    scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
    scene.render.filepath=str(out/f'Model{index}/lit-{tag}.png');bpy.ops.render.render(write_still=True)
