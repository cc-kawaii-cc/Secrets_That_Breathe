"""Create a rigged slow walk from a COPY of the user's unrigged FBX.
Run with Blender --factory-startup --background --python this_file.
Only the output paths in LowPolyMaleSlowWalk are owned by this script.
"""
import bpy
import math
import json
import hashlib
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx'
WORK = ROOT / 'ArtSource/LowPolyMaleSlowWalk'
EXPORT = ROOT / 'Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_SlowWalk.fbx'
FPS, LAST = 30, 73  # 2.4 seconds / two short, slow steps.
LAYER = bpy.context.view_layer
WORK.mkdir(parents=True, exist_ok=True)
EXPORT.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE))
mesh = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
source_matrix = [list(row) for row in mesh.matrix_world]
source_vertices = len(mesh.data.vertices)
mesh.data.calc_loop_triangles()
source_triangles = len(mesh.data.loop_triangles)

armature = bpy.data.armatures.new('SlowWalkSkeleton')
rig = bpy.data.objects.new('SlowWalkRig', armature)
bpy.context.collection.objects.link(rig)
rig.show_in_front = True
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
mesh.select_set(False)
bpy.ops.object.mode_set(mode='EDIT')

def bone(name, head, tail, parent=None, deform=True):
    b = armature.edit_bones.new(name)
    b.head, b.tail = head, tail
    b.use_deform = deform
    if parent:
        b.parent = armature.edit_bones[parent]
    return b

bone('Root', (0, 0, 0), (0, 0, .25), deform=False)
bone('Hips', (0, .08, 2.12), (0, .08, 2.43), 'Root')
bone('Spine', (0, .08, 2.43), (0, .08, 2.8), 'Hips')
bone('Chest', (0, .08, 2.8), (0, .08, 3.15), 'Spine')
bone('Neck', (0, .08, 3.15), (0, .04, 3.55), 'Chest')
bone('Head', (0, .04, 3.55), (0, .01, 4.18), 'Neck')
for side, sign in [('L', 1), ('R', -1)]:
    bone('Shoulder.'+side, (sign*.12, .08, 3.12), (sign*.5, .08, 3.12), 'Chest')
    bone('UpperArm.'+side, (sign*.5, .08, 3.12), (sign*.85, .10, 2.77), 'Shoulder.'+side)
    bone('Forearm.'+side, (sign*.85, .10, 2.77), (sign*1.21, -.035, 2.24), 'UpperArm.'+side)
    bone('Hand.'+side, (sign*1.21, -.035, 2.24), (sign*1.38, -.09, 2.03), 'Forearm.'+side)
    bone('UpperLeg.'+side, (sign*.22, .09, 2.12), (sign*.45, .12, 1.15), 'Hips')
    bone('LowerLeg.'+side, (sign*.45, .12, 1.15), (sign*.67, .12, .28), 'UpperLeg.'+side)
    bone('Foot.'+side, (sign*.67, .12, .28), (sign*.70, -.19, .09), 'LowerLeg.'+side)
    bone('Toes.'+side, (sign*.70, -.19, .09), (sign*.70, -.30, .065), 'Foot.'+side)
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT')
mesh.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
totals = [sum(g.weight for g in v.groups) for v in mesh.data.vertices]
print('WEIGHT_TOTALS', min(totals), max(totals), 'zero', sum(v < 1e-6 for v in totals))
for v in mesh.data.vertices:
    entries = sorted([(g.group, g.weight) for g in v.groups], key=lambda pair: pair[1], reverse=True)
    keep = entries[:4]  # Match Unity's four bone influences per vertex.
    total = sum(weight for index, weight in keep)
    if total > 1e-6:
        for index, weight in entries[4:]:
            mesh.vertex_groups[index].remove([v.index])
        for index, weight in keep:
            mesh.vertex_groups[index].add([v.index], weight/total, 'REPLACE')
assert all(sum(g.weight for g in v.groups) > .99 for v in mesh.data.vertices), 'Unweighted vertices'
for p in rig.pose.bones:
    p.rotation_mode = 'QUATERNION'

rest = {b.name: (b.head_local.copy(), b.tail_local.copy()) for b in armature.bones}
length = {name: (tail-head).length for name, (head, tail) in rest.items()}
scene = bpy.context.scene
scene.render.fps = FPS
scene.frame_start, scene.frame_end = 1, LAST
rig.animation_data_create()
action = bpy.data.actions.new('SlowWalk_InPlace')
rig.animation_data.action = action
action.use_fake_user = True
previous_quaternions = {}

def smooth(t):
    return t*t*(3-2*t)

def solve_knee(hip, ankle, upper, lower):
    d = ankle-hip
    distance = min(d.length, upper+lower-.0001)
    direction = d.normalized()
    a = (upper*upper-lower*lower+distance*distance)/(2*distance)
    pole = Vector((0, -1, 0))
    perpendicular = (pole-direction*pole.dot(direction)).normalized()
    return hip+direction*a+perpendicular*math.sqrt(max(0, upper*upper-a*a))

def pose_points(phase, foot_corrections):
    turn = 2*math.pi*phase
    hip_delta = Vector((.035*math.sin(turn), 0, -.025-.018*math.cos(2*turn)))
    hip_origin = rest['Hips'][0]
    body = Matrix.Translation(hip_delta+hip_origin) @ Matrix.Rotation(math.radians(2)*math.sin(turn), 4, 'Z') @ Matrix.Translation(-hip_origin)
    upper = body @ Matrix.Translation(rest['Spine'][0]) @ Matrix.Rotation(-math.radians(3)*math.sin(turn), 4, 'Z') @ Matrix.Translation(-rest['Spine'][0])
    result = {'Root': rest['Root']}
    for name in ['Hips', 'Spine', 'Chest', 'Neck', 'Head']:
        matrix = body if name == 'Hips' else upper
        result[name] = tuple(matrix @ point for point in rest[name])
    for side, sign, offset in [('L', 1, 0), ('R', -1, .5)]:
        t = (phase+offset) % 1
        stance = .55
        if t < stance:
            y = -.43+.86*t/stance
            lift = 0
            pitch = -math.radians(4)*(1-smooth(min(1, t/.1)))
            pitch += math.radians(9)*smooth(max(0, min(1, (t-.43)/.12)))
        else:
            swing = (t-stance)/(1-stance)
            y = .43-.86*smooth(swing)
            lift = .22*math.sin(math.pi*swing)**1.3
            pitch = -math.radians(6)*math.sin(math.pi*swing)
        hip = body @ rest['UpperLeg.'+side][0]
        ankle = Vector((sign*.27, y+.12, .28+lift+foot_corrections[side]))
        knee = solve_knee(hip, ankle, length['UpperLeg.'+side], length['LowerLeg.'+side])
        foot_rotation = Matrix.Rotation(pitch, 3, 'X')
        toe = ankle+foot_rotation @ (rest['Foot.'+side][1]-rest['Foot.'+side][0])
        tip = toe+foot_rotation @ (rest['Toes.'+side][1]-rest['Toes.'+side][0])
        result['UpperLeg.'+side] = hip, knee
        result['LowerLeg.'+side] = knee, ankle
        result['Foot.'+side] = ankle, toe
        result['Toes.'+side] = toe, tip
        shoulder_head, shoulder = (upper @ point for point in rest['Shoulder.'+side])
        arm_swing = math.cos(turn+offset*2*math.pi)
        elbow = shoulder+Vector((sign*.15, .22*arm_swing, -.96)).normalized()*length['UpperArm.'+side]
        wrist = elbow+Vector((sign*.035, -.13+.16*arm_swing, -.98)).normalized()*length['Forearm.'+side]
        fingers = wrist+Vector((sign*.05, -.08, -.99)).normalized()*length['Hand.'+side]
        result['Shoulder.'+side] = shoulder_head, shoulder
        result['UpperArm.'+side] = shoulder, elbow
        result['Forearm.'+side] = elbow, wrist
        result['Hand.'+side] = wrist, fingers
    return result

def apply_points(points):
    matrices = {}
    for p in rig.pose.bones:
        head, tail = points[p.name]
        old_head, old_tail = rest[p.name]
        rotation = (old_tail-old_head).rotation_difference(tail-head)
        matrix = Matrix.Translation(head) @ rotation.to_matrix().to_4x4() @ p.bone.matrix_local.to_quaternion().to_matrix().to_4x4()
        if p.parent:
            p.matrix_basis = p.bone.convert_local_to_pose(matrix, p.bone.matrix_local,
                parent_matrix=matrices[p.parent.name], parent_matrix_local=p.parent.bone.matrix_local, invert=True)
        else:
            p.matrix_basis = p.bone.convert_local_to_pose(matrix, p.bone.matrix_local, invert=True)
        matrices[p.name] = matrix
    bpy.context.view_layer.update()

foot_vertices = {}
for side, sign in [('L', 1), ('R', -1)]:
    foot_vertices[side] = [v.index for v in mesh.data.vertices
        if (mesh.matrix_world @ v.co).z < .3 and sign*(mesh.matrix_world @ v.co).x > 0]
minimum_z = 100
first_positions = last_positions = None
for frame in range(1, LAST+1):
    scene.frame_set(frame)
    phase = (frame-1)/(LAST-1)
    correction = {'L': 0, 'R': 0}
    for iteration in range(3):
        apply_points(pose_points(phase, correction))
        evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        positions = [evaluated.matrix_world @ v.co for v in evaluated.data.vertices]
        changed = False
        for side in ['L', 'R']:
            floor = min(positions[i].z for i in foot_vertices[side])
            if floor < 0:
                correction[side] += -floor+.0001
                changed = True
        if not changed:
            break
    minimum_z = min(minimum_z, min(v.z for v in positions))
    if frame == 1: first_positions = [v.copy() for v in positions]
    if frame == LAST: last_positions = [v.copy() for v in positions]
    for p in rig.pose.bones:
        q = p.rotation_quaternion.copy()
        if p.name in previous_quaternions and q.dot(previous_quaternions[p.name]) < 0:
            p.rotation_quaternion = -q
        previous_quaternions[p.name] = p.rotation_quaternion.copy()
        for field in ['location', 'rotation_quaternion', 'scale']:
            p.keyframe_insert(data_path=field, frame=frame, group=p.name)

loop_error = max((a-b).length for a, b in zip(first_positions, last_positions))
assert loop_error < .0001, ('Loop seam', loop_error)
assert minimum_z > -.025, ('Ground penetration', minimum_z)
forward = action.copy()
forward.name = 'SlowWalk_Forward'
forward.use_fake_user = True
rig.animation_data.action = forward
for frame in range(1, LAST+1):
    scene.frame_set(frame)
    travel = Vector((0, -.86/.55*(frame-1)/(LAST-1), 0))
    rig.pose.bones['Root'].location = armature.bones['Root'].matrix_local.to_3x3().inverted() @ travel
    rig.pose.bones['Root'].keyframe_insert('location', frame=frame, group='Root')
for clip in [action, forward]:
    for slot in clip.slots:
        for layer in clip.layers:
            for strip in layer.strips:
                channels = strip.channelbag(slot)
                if channels:
                    for curve in channels.fcurves:
                        for key in curve.keyframe_points:
                            key.interpolation = 'LINEAR'
rig.animation_data.action = action
scene.frame_set(1)
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
mesh.select_set(True)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(EXPORT), use_selection=True, object_types={'ARMATURE', 'MESH'},
    add_leaf_bones=False, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    bake_anim=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_simplify_factor=0, bake_anim_step=1, path_mode='AUTO')

# A preview stage is intentionally excluded from the FBX selection above.
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x, scene.render.resolution_y = 700, 850
scene.render.resolution_percentage = 100
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'SINGLE'
scene.display.shading.single_color = (.55, .61, .67)
scene.display.shading.show_shadows = True
scene.display.shading.show_cavity = True
scene.display.shading.background_type = 'WORLD'
if scene.world is None:
    scene.world = bpy.data.worlds.new('PreviewOnly_World')
scene.world.color = (.09, .09, .09)
bpy.ops.object.camera_add(location=(7, -12, 4.2))
camera = bpy.context.object
camera.name = 'PreviewOnly_Camera'
target = Vector((0, 0, 2.05))
camera.rotation_euler = (target-camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type, camera.data.ortho_scale = 'ORTHO', 4.9
scene.camera = camera
rig.animation_data.action = action
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(WORK / 'LowPolyMale_SlowWalk.blend'))
preview = WORK / 'Preview'
preview.mkdir(exist_ok=True)
for frame in [1, 10, 19, 28, 37, 46, 55, 64]:
    scene.frame_set(frame)
    scene.render.filepath = str(preview / ('walk_%03d.png' % frame))
    bpy.ops.render.render(write_still=True)
report = dict(source=str(SOURCE.relative_to(ROOT)), source_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
    vertices=source_vertices, triangles=source_triangles, bones=len(armature.bones),
    clips=['SlowWalk_InPlace', 'SlowWalk_Forward'], duration_seconds=(LAST-1)/FPS,
    fps=FPS, frames=[1, LAST], loop_vertex_error=loop_error, minimum_foot_z=minimum_z,
    maximum_bone_influences=max(len(v.groups) for v in mesh.data.vertices),
    source_mesh_name=mesh.name, source_world_matrix=source_matrix,
    forward_distance_source_units=.86/.55,
    note='Original model geometry and imported scale retained. Preview helpers excluded from FBX.')
(WORK / 'validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('SLOW_WALK_REPORT', json.dumps(report))
