import bpy, os, json
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__));BASE=os.path.dirname(OUT);ROOT=os.path.dirname(os.path.dirname(BASE))
def render(path,source):
    if source:
        bpy.ops.wm.open_mainfile(filepath=os.path.join(BASE,'Source','PorscheV2_HeadlightsSeparated.blend'))
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT,'Assets','Model Palm','Porsche V.2','PorscheV.2.fbx'))
    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    sh=scene.display.shading
    sh.light='STUDIO';sh.studiolight_rotate_z=.6;sh.use_world_space_lighting=True
    sh.color_type='SINGLE';sh.single_color=(.5,.5,.5)
    sh.show_shadows=False;sh.show_cavity=False;sh.show_specular_highlight=True;sh.show_object_outline=False
    sh.background_type='WORLD';sh.background_color=(.06,.06,.06)
    scene.view_settings.view_transform='Standard'
    scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
    camdata=bpy.data.cameras.new('CompareCamera');cam=bpy.data.objects.new('CompareCamera',camdata);scene.collection.objects.link(cam);scene.camera=cam
    camdata.type='ORTHO';camdata.ortho_scale=2.75
    cam.location=(0,-6,2.6);cam.rotation_euler=(Vector((0,-.55,.55))-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=path;bpy.ops.render.render(write_still=True)
    im=bpy.data.images.load(path,check_existing=False)
    return list(im.pixels)
a=render(os.path.join(OUT,'before_shaded_front.png'),False)
b=render(os.path.join(OUT,'after_shaded_front.png'),True)
delta=[abs(x-y) for x,y in zip(a,b)]
diffpixels=sum(any(d>1.01/255 for d in delta[i:i+3]) for i in range(0,len(delta),4))
report={'maxChannelDifference':max(delta),'meanChannelDifference':sum(delta)/len(delta),'pixelsDifferentByMoreThan1Of255':diffpixels,'imagePixels':len(delta)//4}
with open(os.path.join(OUT,'shaded_comparison.json'),'w') as f:json.dump(report,f,indent=2)
print(report)
