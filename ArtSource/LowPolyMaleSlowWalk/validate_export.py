"""Reopen the FBX, compare deformed surfaces against the .blend, and render proof."""
import bpy
import json
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT/'ArtSource/LowPolyMaleSlowWalk'
FBX = ROOT/'Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_SlowWalk.fbx'
bpy.ops.wm.open_mainfile(filepath=str(WORK/'LowPolyMale_SlowWalk.blend'))
scene = bpy.context.scene
rig = bpy.data.objects['SlowWalkRig']
mesh = bpy.data.objects['Cube']
inplace = next(a for a in bpy.data.actions if a.name == 'SlowWalk_InPlace')
rig.animation_data.action = inplace

def positions(obj):
    bpy.context.view_layer.update()
    e = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    return [e.matrix_world@v.co for v in e.data.vertices]

reference = {}
for frame in [1, 19, 37, 55, 73]:
    scene.frame_set(frame)
    reference[frame] = [v.copy() for v in positions(mesh)]
scene.render.resolution_x, scene.render.resolution_y = 420, 510
folder = WORK/'Preview/AnimationFrames'
folder.mkdir(exist_ok=True)
for frame in range(1, 73, 3):
    scene.frame_set(frame)
    scene.render.filepath = str(folder/('frame_%03d.png'%frame))
    bpy.ops.render.render(write_still=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
bpy.ops.import_scene.fbx(filepath=str(FBX))
meshes = [o for o in scene.objects if o.type == 'MESH']
rigs = [o for o in scene.objects if o.type == 'ARMATURE']
assert len(meshes) == 1 and len(rigs) == 1, 'Unexpected exported objects'
mesh, rig = meshes[0], rigs[0]
assert len(rig.data.bones) == 22
assert all(sum(g.weight for g in v.groups) > .99 for v in mesh.data.vertices)
assert max(len(v.groups) for v in mesh.data.vertices) <= 4
mesh.data.calc_loop_triangles()
assert len(mesh.data.loop_triangles) == 964
actions = list(bpy.data.actions)
clips = {name: next(a for a in actions if name in a.name) for name in ['SlowWalk_InPlace','SlowWalk_Forward']}
report = {'fbx_objects': [o.name for o in scene.objects], 'fbx_triangles': len(mesh.data.loop_triangles),
          'fbx_bones':len(rig.data.bones), 'clips': {name:list(a.frame_range) for name,a in clips.items()}}
rig.animation_data.action = clips['SlowWalk_InPlace']
errors = []
for frame, baseline in reference.items():
    scene.frame_set(frame)
    actual = positions(mesh)
    tree = KDTree(len(actual))
    for i,v in enumerate(actual): tree.insert(v,i)
    tree.balance()
    errors.append(max(tree.find(v)[2] for v in baseline))
report['maximum_export_surface_error'] = max(errors)
assert max(errors) < .005, ('Export deformed mesh differs', errors)
for name,a in clips.items():
    rig.animation_data.action = a
    scene.frame_set(1)
    start = positions(mesh)
    scene.frame_set(73)
    end = positions(mesh)
    delta = sum((b-a for a,b in zip(start,end)), Vector())/len(start)
    report[name+'_world_displacement'] = list(delta)
    if name.endswith('InPlace'): assert delta.length < .0001
    else: assert abs(delta.z) < .0001 and abs(delta.y+ .86/.55) < .001

scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x,scene.render.resolution_y = 700,850
scene.render.resolution_percentage = 100
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'SINGLE'
scene.display.shading.single_color = (.55,.61,.67)
scene.display.shading.show_shadows = True
scene.display.shading.show_cavity = True
scene.world = bpy.data.worlds.new('ValidationWorld')
scene.world.color = (.09,.09,.09)
scene.display.shading.background_type = 'WORLD'
rig.animation_data.action = clips['SlowWalk_InPlace']
for name,location,frame in [('front',(0,-12,2.1),19),('side',(12,0,2.1),19),('back',(0,12,2.1),55)]:
    scene.frame_set(frame)
    bpy.ops.object.camera_add(location=location)
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0,0,2.1))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type,camera.data.ortho_scale = 'ORTHO',4.9
    scene.camera = camera
    scene.render.filepath = str(WORK/'Preview'/('export_'+name+'.png'))
    bpy.ops.render.render(write_still=True)
(WORK/'export-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('EXPORT_VALIDATION',json.dumps(report))
