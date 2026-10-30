import bpy,json,math
from pathlib import Path
from mathutils import Matrix,Vector
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent;reports=json.loads((W/'build-report.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene
for role,x,angle in [('Police',-.7,90),('Suit',.7,-90)]:
 with bpy.data.libraries.load(str(W/(role+'Conversation.blend')),link=False) as (src,dst):dst.objects=[role+'_ConversationRig','Body']+(['Sunglasses'] if role=='Suit' else [])
 objects=dst.objects
 for o in objects:s.collection.objects.link(o);o.hide_render=False;o.hide_set(False)
 rig=objects[0];scale=reports[role]['height_metres']/reports[role]['initial_height_source']
 rig.matrix_world=Matrix.Translation((x,0,-reports[role]['initial_floor_source']*scale))@Matrix.Rotation(math.radians(angle),4,'Z')@Matrix.Scale(scale,4)
s.frame_start=1;s.frame_end=241;s.render.fps=30;s.view_settings.view_transform='Standard';s.view_settings.look='None'
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_specular_highlight=False;s.display.shading.show_shadows=True
s.world=bpy.data.worlds.new('ReviewWorld');s.world.color=(.12,.12,.12);s.display.shading.background_type='WORLD'
s.render.resolution_x=1200;s.render.resolution_y=900;s.render.resolution_percentage=100
bpy.ops.object.camera_add(location=(.1,-6,2.8));c=bpy.context.object;c.rotation_euler=(Vector((0,0,.9))-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=3.3;s.camera=c
for f in [1,61,101,151,181,221]:s.frame_set(f);s.render.filepath=str(W/('pair_%03d.png'%f));bpy.ops.render.render(write_still=True)
s.frame_set(61);bpy.ops.wm.save_as_mainfile(filepath=str(W/'Conversation_Pair_Preview.blend'))
print('CONVERSATION_PREVIEWS_COMPLETE')
