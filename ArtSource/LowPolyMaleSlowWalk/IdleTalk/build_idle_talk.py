"""Add a gentle planted-foot conversation idle in a separate .blend and FBX.
The working slow-walk .blend, FBX, controllers and prefabs are not overwritten.
"""
import bpy
import json
import math
import hashlib
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT/'ArtSource/LowPolyMaleSlowWalk/LowPolyMale_SlowWalk.blend'
WORK = Path(__file__).resolve().parent
EXPORT = ROOT/'Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_IdleTalk.fbx'
SOURCE_HASH = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
FPS, LAST = 30, 145
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
rig, mesh = bpy.data.objects['SlowWalkRig'], bpy.data.objects['Cube']
rest = {b.name:(b.head_local.copy(),b.tail_local.copy()) for b in rig.data.bones}
length = {name:(tail-head).length for name,(head,tail) in rest.items()}
idle = bpy.data.actions.new('IdleTalk')
idle.use_fake_user = True
rig.animation_data.action = idle
scene.frame_start,scene.frame_end = 1,LAST
scene.render.fps = FPS

def around(point, rotation):
    return Matrix.Translation(point) @ rotation @ Matrix.Translation(-point)

def solve_knee(hip, ankle, upper, lower):
    d = ankle-hip
    distance = min(d.length,upper+lower-.0001)
    direction = d.normalized()
    a = (upper*upper-lower*lower+distance*distance)/(2*distance)
    pole = Vector((0,-1,0))
    perpendicular = (pole-direction*pole.dot(direction)).normalized()
    return hip+direction*a+perpendicular*math.sqrt(max(0,upper*upper-a*a))

def points(phase,corrections):
    t = 2*math.pi*phase
    sway = math.sin(t)
    delta = Vector((.045*sway,.008*math.sin(2*t),.035+.004*math.sin(2*t)))
    body = Matrix.Translation(delta) @ around(rest['Hips'][0],
        Matrix.Rotation(math.radians(.7)*sway,4,'Y') @ Matrix.Rotation(math.radians(.6)*sway,4,'Z'))
    torso = body @ around(rest['Spine'][0],Matrix.Rotation(-math.radians(.3)*sway,4,'Y')
        @ Matrix.Rotation(math.radians(.35)*math.sin(2*t),4,'X'))
    head = torso @ around(rest['Head'][0],Matrix.Rotation(math.radians(.6)*math.sin(2*t),4,'X')
        @ Matrix.Rotation(math.radians(.65)*math.sin(t+math.pi/4),4,'Z'))
    result = {'Root':rest['Root']}
    for name in ['Hips','Spine','Chest','Neck','Head']:
        matrix = body if name=='Hips' else head if name=='Head' else torso
        result[name] = tuple(matrix@point for point in rest[name])
    for side,sign in [('L',1),('R',-1)]:
        hip = body@rest['UpperLeg.'+side][0]
        ankle = Vector((sign*.27,.12,.28+corrections[side]))
        knee = solve_knee(hip,ankle,length['UpperLeg.'+side],length['LowerLeg.'+side])
        toe = ankle+(rest['Foot.'+side][1]-rest['Foot.'+side][0])
        tip = toe+(rest['Toes.'+side][1]-rest['Toes.'+side][0])
        result['UpperLeg.'+side] = hip,knee
        result['LowerLeg.'+side] = knee,ankle
        result['Foot.'+side] = ankle,toe
        result['Toes.'+side] = toe,tip
        shoulder_head,shoulder = (torso@point for point in rest['Shoulder.'+side])
        elbow = shoulder+Vector((sign*.15,.025+.012*math.sin(t+sign*.3),-.99)).normalized()*length['UpperArm.'+side]
        wrist = elbow+Vector((sign*.035,-.13+.015*math.sin(t),-.98)).normalized()*length['Forearm.'+side]
        fingers = wrist+Vector((sign*.05,-.08,-.99)).normalized()*length['Hand.'+side]
        result['Shoulder.'+side] = shoulder_head,shoulder
        result['UpperArm.'+side] = shoulder,elbow
        result['Forearm.'+side] = elbow,wrist
        result['Hand.'+side] = wrist,fingers
    return result

def apply(points):
    matrices = {}
    for p in rig.pose.bones:
        p.rotation_mode = 'QUATERNION'
        start,end = points[p.name]
        old_start,old_end = rest[p.name]
        rotation = (old_end-old_start).rotation_difference(end-start)
        matrix = Matrix.Translation(start) @ rotation.to_matrix().to_4x4() @ p.bone.matrix_local.to_quaternion().to_matrix().to_4x4()
        if p.parent:
            p.matrix_basis = p.bone.convert_local_to_pose(matrix,p.bone.matrix_local,
                parent_matrix=matrices[p.parent.name],parent_matrix_local=p.parent.bone.matrix_local,invert=True)
        else:
            p.matrix_basis = p.bone.convert_local_to_pose(matrix,p.bone.matrix_local,invert=True)
        matrices[p.name] = matrix
    bpy.context.view_layer.update()

foot_vertices = {side:[v.index for v in mesh.data.vertices
    if (mesh.matrix_world@v.co).z<.3 and sign*(mesh.matrix_world@v.co).x>0]
    for side,sign in [('L',1),('R',-1)]}
first = last = None
previous = {}
min_z = 100
max_foot_xy_drift = 0
for frame in range(1,LAST+1):
    scene.frame_set(frame)
    phase = (frame-1)/(LAST-1)
    correction = {'L':0,'R':0}
    for iteration in range(3):
        apply(points(phase,correction))
        evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        vertices = [evaluated.matrix_world@v.co for v in evaluated.data.vertices]
        changed = False
        for side in ['L','R']:
            floor = min(vertices[i].z for i in foot_vertices[side])
            if floor<0:
                correction[side] += -floor+.0001
                changed = True
        if not changed: break
    min_z = min(min_z,min(v.z for v in vertices))
    for side,sign in [('L',1),('R',-1)]:
        foot = rig.pose.bones['Foot.'+side].head
        max_foot_xy_drift = max(max_foot_xy_drift,math.hypot(foot.x-sign*.27,foot.y-.12))
    if frame==1: first=[v.copy() for v in vertices]
    if frame==LAST: last=[v.copy() for v in vertices]
    for p in rig.pose.bones:
        if p.name in previous and p.rotation_quaternion.dot(previous[p.name])<0:
            p.rotation_quaternion = -p.rotation_quaternion
        previous[p.name] = p.rotation_quaternion.copy()
        for field in ['location','rotation_quaternion','scale']:
            p.keyframe_insert(field,frame=frame,group=p.name)
for layer in idle.layers:
    for strip in layer.strips:
        for slot in idle.slots:
            bag = strip.channelbag(slot)
            if bag:
                for curve in bag.fcurves:
                    for key in curve.keyframe_points: key.interpolation='LINEAR'
loop_error = max((a-b).length for a,b in zip(first,last))
assert loop_error<.0001 and min_z>-.025 and max_foot_xy_drift<.0001
scene.name = 'IdleTalk'
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
mesh.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(EXPORT),use_selection=True,object_types={'ARMATURE','MESH'},
    add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,
    bake_anim_simplify_factor=0,bake_anim_step=1)
# Save as a new working file; the original slow-walk source and both actions remain untouched.
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'LowPolyMale_IdleTalk.blend'))
preview = WORK/'Preview'
preview.mkdir(exist_ok=True)
scene.render.resolution_x,scene.render.resolution_y = 420,510
scene.render.resolution_percentage = 100
for frame in range(1,LAST,4):
    scene.frame_set(frame)
    scene.render.filepath = str(preview/('idle_%03d.png'%frame))
    bpy.ops.render.render(write_still=True)
mesh.data.calc_loop_triangles()
report = dict(clip='IdleTalk',seconds=(LAST-1)/FPS,fps=FPS,frames=[1,LAST],bones=len(rig.data.bones),
    triangles=len(mesh.data.loop_triangles),vertices=len(mesh.data.vertices),loop_vertex_error=loop_error,
    minimum_ground_z=min_z,maximum_ankle_xy_drift=max_foot_xy_drift,walk_blend_source_hash=SOURCE_HASH,
    note='Separate FBX; original walking FBX, blend, controllers and prefabs are not changed.')
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==SOURCE_HASH
(WORK/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('IDLE_TALK_REPORT',json.dumps(report))
