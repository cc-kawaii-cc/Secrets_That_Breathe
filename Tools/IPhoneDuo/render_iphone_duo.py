"""Render actual Blender geometry for delivery/visual QA; invoke after saved build."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(r'D:\Unity\ปี3\Secrets_That_Breathe')
ART=ROOT/'ArtSource/IPhoneDuo'
scene=bpy.data.scenes['IPhoneDuo_Studio']
bpy.context.window.scene=scene
hinge=bpy.data.objects['HingePivot']
camera=scene.camera
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.render.resolution_x=1400
scene.render.resolution_y=1100
scene.render.resolution_percentage=100
scene.render.film_transparent=False
ground=bpy.data.objects['DUO_StudioGround']

def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
def aim_camera(o,p):
    forward=(Vector(p)-o.location).normalized()
    right=forward.cross(Vector((0,1,0))).normalized()
    up=right.cross(forward).normalized()
    o.rotation_euler=Matrix((right,up,-forward)).transposed().to_quaternion().to_euler()

poses=[
    ('01_Open_Front',0,(.12,-.18,.28),(0,0,0),.214),
    ('02_Open_Back',0,(-.10,.08,-.29),(0,0,-.001),.214),
    ('03_HalfOpen',-110,(.18,.10,-.22),(-.020,0,.025),.196),
    ('04_Closed_Camera',-180,(-.13,.09,.27),(-.04,0,.003),.178),
    ('05_Closed_Cover',-180,(-.10,.07,-.26),(-.04,0,.003),.162),
    ('06_Hinge_and_Ports',-110,(.13,-.20,.13),(-.015,-.014,.025),.165),
]
for name,angle,pos,target,scale in poses:
    hinge.rotation_euler.y=math.radians(angle)
    camera.location=pos;aim_camera(camera,target);camera.data.ortho_scale=scale
    camera.data.clip_start=.001
    ground.hide_render=True
    # Broad neutral studio illumination from both sides for honest surface checks.
    for o in scene.objects:
        if o.type=='LIGHT':o.hide_render=False
    if 'DUO_BackSoftbox' not in bpy.data.objects:
        data=bpy.data.lights.new('DUO_BackSoftbox','AREA');data.energy=.6;data.shape='DISK';data.size=.17
        light=bpy.data.objects.new('DUO_BackSoftbox',data);scene.collection.objects.link(light);light['asset_owner']='IPhoneDuo'
        light.location=(-.10,.12,-.22);aim(light,(0,0,0))
    scene.render.filepath=str(ART/(name+'.png'))
    bpy.ops.render.render(write_still=True)
    print('RENDER_COMPLETE',name,flush=True)
hinge.rotation_euler.y=0
print('ALL_PREVIEWS_COMPLETE',flush=True)
