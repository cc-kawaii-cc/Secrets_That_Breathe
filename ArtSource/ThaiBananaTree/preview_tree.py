import bpy,math
from pathlib import Path
from mathutils import Vector
r=Path.cwd();s=r/'ArtSource/ThaiBananaTree';bpy.ops.wm.open_mainfile(filepath=str(s/'ThaiBanana_Ripe.blend'))
for o in list(bpy.data.objects):
 if 'LOD0' not in o.name:bpy.data.objects.remove(o,do_unlink=True)
for o in bpy.data.objects:o.hide_render=False;o.hide_viewport=False
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('World');scene.world.color=(.28,.28,.28);scene.view_settings.view_transform='AgX'
for loc,power,size in [((2,-4,7),1100,5),((-4,-1,4),700,4),((1,4,6),950,3)]:
 d=bpy.data.lights.new('Studio','AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new('Studio',d);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1.8))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Camera');o=bpy.data.objects.new('Camera',d);bpy.context.collection.objects.link(o);scene.camera=o;d.type='ORTHO';d.ortho_scale=5.3
for name,loc in [('NewTree_Front',(5,-7,4.2)),('NewTree_Back',(-5,6,3.8))]:
 o.location=loc;o.rotation_euler=(Vector((0,0,1.85))-o.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(s/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(s/'ThaiBanana_Preview.blend'))


