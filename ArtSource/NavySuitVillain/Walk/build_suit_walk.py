"""NavySuitVillain walking model: the suit mesh on the proven LowPolyMale skeleton with a new walk cycle.

Run: blender --background --factory-startup --python build_suit_walk.py
Environment (optional): PROJECT_ROOT = project folder to read from (default: three folders above this file),
                        OUT_ROOT     = folder the Assets/ArtSource outputs are written to (default: PROJECT_ROOT).

Only new files are written (Characters/NavySuitVillain/Walk/*, ArtSource/NavySuitVillain/Walk/*).
The original NavySuitVillain.prefab, SuitConversation.fbx and the static model are never touched.
"""
import bpy, math, json, hashlib, os, re, uuid
from pathlib import Path
from mathutils import Vector, Matrix

HERE = Path(__file__).resolve().parent
R = Path(os.environ.get('PROJECT_ROOT', HERE.parents[2]))
OUT = Path(os.environ.get('OUT_ROOT', R))
FBX_DIR = OUT / 'Assets/SecretsThatBreathe/Characters/NavySuitVillain/Walk'
ART_DIR = OUT / 'ArtSource/NavySuitVillain/Walk'
FBX_DIR.mkdir(parents=True, exist_ok=True)
ART_DIR.mkdir(parents=True, exist_ok=True)
SOURCE = R / 'ArtSource/LowPolyMaleSlowWalk/IdleTalk/LowPolyMale_IdleTalk.blend'
STATIC = R / 'ArtSource/NavySuitVillain/NavySuitVillain_LowPoly.blend'
SOURCE_HASH = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
TARGET_HEIGHT = 1.85

# ---- gait (source units; the .blend model is 4.255 units = 1.85 m) -----------------------------------------
FPS = 30
LAST = 34                    # frames 1..34 = 33 intervals = 1.1 s = two steps (about 109 steps per minute)
STANCE = .58                 # fraction of the cycle a foot is planted
HALF_SWEEP = .70             # planted foot travels from -HALF_SWEEP to +HALF_SWEEP under the body
LIFT = .26                   # swing foot clearance
HIP_BASE_DROP, HIP_BOB = .075, .055
ACTION = 'SuitWalk_InPlace'

def smooth(t): return t * t * (3 - 2 * t)
def around(point, rotation): return Matrix.Translation(point) @ rotation @ Matrix.Translation(-point)

def solve_knee(hip, ankle, upper, lower, report):
    d = ankle - hip
    reach = upper + lower - .0001
    if d.length > reach: report['overreach'] = max(report['overreach'], d.length - reach)
    distance = min(d.length, reach)
    direction = d.normalized()
    a = (upper * upper - lower * lower + distance * distance) / (2 * distance)
    pole = Vector((0, -1, 0))
    perpendicular = (pole - direction * pole.dot(direction)).normalized()
    return hip + direction * a + perpendicular * math.sqrt(max(0, upper * upper - a * a))

bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.name = 'SuitWalk'
rig = bpy.data.objects['SlowWalkRig']
body = bpy.data.objects['Cube']
rig.data.pose_position = 'POSE'
scene.frame_set(1)
bpy.context.view_layer.update()
rest = {b.name: (b.head_local.copy(), b.tail_local.copy()) for b in rig.data.bones}
length = {n: (b - a).length for n, (a, b) in rest.items()}
dg = bpy.context.evaluated_depsgraph_get()
orig = [body.matrix_world @ v.co for v in body.evaluated_get(dg).data.vertices]
original_pose_height = max(v.z for v in orig) - min(v.z for v in orig)
original_min = min(v.z for v in orig)
assert abs(original_pose_height - 4.255) < .01, original_pose_height

# ---- put the suit mesh on this skeleton (same recipe as the existing SuitConversation build) ----------------
with bpy.data.libraries.load(str(STATIC), link=False) as (src, dst):
    dst.objects = ['Body', 'Sunglasses']
skin_body, glasses = dst.objects
skin_body.name = 'Suit_UVSource_DoNotExport'
assert len(skin_body.data.vertices) == 484
assert [tuple(p.vertices) for p in skin_body.data.polygons] == [tuple(p.vertices) for p in body.data.polygons]
for a, b in zip(body.data.uv_layers.active.data, skin_body.data.uv_layers.active.data):
    a.uv = b.uv
body.data.materials.clear()
body.data.materials.append(skin_body.data.materials[0])
body.name = 'Body'
scene.collection.objects.link(glasses)
factor = TARGET_HEIGHT / original_pose_height
skin = rig.matrix_world @ rig.pose.bones['Head'].matrix @ rig.data.bones['Head'].matrix_local.inverted() @ rig.matrix_world.inverted()
inverse = skin.inverted()
for v in glasses.data.vertices:
    v.co = inverse @ ((v.co + Vector((0, 0, original_min * factor))) / factor)
glasses.parent = rig
glasses.matrix_parent_inverse = Matrix.Identity(4)
glasses.matrix_basis = Matrix.Identity(4)
group = glasses.vertex_groups.new(name='Head')
group.add(list(range(len(glasses.data.vertices))), 1, 'REPLACE')
mod = glasses.modifiers.new('Follow_Head', 'ARMATURE')
mod.object = rig
rig.name = 'Suit_WalkRig'
for constraint in list(rig.constraints): rig.constraints.remove(constraint)

# ---- walk cycle ---------------------------------------------------------------------------------------------
action = bpy.data.actions.new(ACTION)
action.use_fake_user = True
rig.animation_data.action = action
scene.frame_start, scene.frame_end = 1, LAST
scene.render.fps = FPS
gait = {'overreach': 0.0}

def pose_points(phase, corrections):
    turn = 2 * math.pi * phase
    hip_origin = rest['Hips'][0]
    drop = HIP_BASE_DROP + HIP_BOB * math.cos(2 * turn)           # lowest at double support, highest mid-stance
    sway = Vector((.055 * math.sin(turn), 0, -drop))
    body_m = Matrix.Translation(sway) @ around(hip_origin, Matrix.Rotation(math.radians(3.2) * math.sin(turn), 4, 'Z'))
    chest_m = body_m @ around(rest['Spine'][0], Matrix.Rotation(-math.radians(6.0) * math.sin(turn), 4, 'Z')
                              @ Matrix.Rotation(math.radians(2.2), 4, 'X'))
    head_m = chest_m @ around(rest['Neck'][0], Matrix.Rotation(math.radians(3.0) * math.sin(turn), 4, 'Z')
                              @ Matrix.Rotation(-math.radians(1.2), 4, 'X'))
    out = {'Root': rest['Root']}
    for name in ['Hips', 'Spine', 'Chest', 'Neck', 'Head']:
        m = body_m if name == 'Hips' else head_m if name in ('Neck', 'Head') else chest_m
        out[name] = tuple(m @ p for p in rest[name])
    for side, sign, offset in [('L', 1, 0), ('R', -1, .5)]:
        t = (phase + offset) % 1
        if t < STANCE:
            u = t / STANCE
            y = -HALF_SWEEP + 2 * HALF_SWEEP * u
            lift = 0
            pitch = -math.radians(8) * (1 - smooth(min(1, t / .09)))               # heel strike
            pitch += math.radians(22) * smooth(max(0, min(1, (t - STANCE * .78) / (STANCE * .22))))  # push-off
        else:
            s = (t - STANCE) / (1 - STANCE)
            y = HALF_SWEEP - 2 * HALF_SWEEP * smooth(s)
            lift = LIFT * math.sin(math.pi * s) ** 1.2
            pitch = math.radians(22) * (1 - smooth(min(1, s / .4))) - math.radians(10) * smooth(max(0, (s - .55) / .45))
        hip = body_m @ rest['UpperLeg.' + side][0]
        ankle = Vector((sign * .27, y + .12, .28 + lift + corrections[side]))
        knee = solve_knee(hip, ankle, length['UpperLeg.' + side], length['LowerLeg.' + side], gait)
        fr = Matrix.Rotation(pitch, 3, 'X')
        toe = ankle + fr @ (rest['Foot.' + side][1] - rest['Foot.' + side][0])
        tip = toe + fr @ (rest['Toes.' + side][1] - rest['Toes.' + side][0])
        out['UpperLeg.' + side] = hip, knee
        out['LowerLeg.' + side] = knee, ankle
        out['Foot.' + side] = ankle, toe
        out['Toes.' + side] = toe, tip
        sh_head, shoulder = (chest_m @ p for p in rest['Shoulder.' + side])
        swing = math.cos(turn + offset * 2 * math.pi)           # opposite arm and leg
        elbow_bend = .20 + .12 * (1 - swing) * .5
        elbow = shoulder + Vector((sign * .13, .34 * swing, -.93)).normalized() * length['UpperArm.' + side]
        wrist = elbow + Vector((sign * .04, -.20 - .20 * (1 - swing) * .5 + .0 * elbow_bend, -.96)).normalized() * length['Forearm.' + side]
        fingers = wrist + Vector((sign * .05, -.10, -.99)).normalized() * length['Hand.' + side]
        out['Shoulder.' + side] = sh_head, shoulder
        out['UpperArm.' + side] = shoulder, elbow
        out['Forearm.' + side] = elbow, wrist
        out['Hand.' + side] = wrist, fingers
    return out

def apply_points(pairs):
    matrices = {}
    for bone in rig.pose.bones:
        bone.rotation_mode = 'QUATERNION'
        a, b = pairs[bone.name]
        old_a, old_b = rest[bone.name]
        rotation = (old_b - old_a).rotation_difference(b - a)
        mat = Matrix.Translation(a) @ rotation.to_matrix().to_4x4() @ bone.bone.matrix_local.to_quaternion().to_matrix().to_4x4()
        if bone.parent:
            bone.matrix_basis = bone.bone.convert_local_to_pose(mat, bone.bone.matrix_local, parent_matrix=matrices[bone.parent.name],
                                                                 parent_matrix_local=bone.parent.bone.matrix_local, invert=True)
        else:
            bone.matrix_basis = bone.bone.convert_local_to_pose(mat, bone.bone.matrix_local, invert=True)
        matrices[bone.name] = mat
    bpy.context.view_layer.update()

feet = {side: [v.index for v in body.data.vertices if (body.matrix_world @ v.co).z < .30 and sign * (body.matrix_world @ v.co).x > 0]
        for side, sign in [('L', 1), ('R', -1)]}
previous = {}
min_z = 10.0
first = last = None
glasses_error = 0.0
glasses_ref = None
ankle_log = []
for frame in range(1, LAST + 1):
    scene.frame_set(frame)
    phase = (frame - 1) / (LAST - 1)
    corrections = {'L': 0.0, 'R': 0.0}
    for _ in range(3):
        apply_points(pose_points(phase, corrections))
        dg = bpy.context.evaluated_depsgraph_get()
        verts = [body.matrix_world @ v.co for v in body.evaluated_get(dg).data.vertices]
        changed = False
        for side in ['L', 'R']:
            low = min(verts[i].z for i in feet[side])
            if low < 0:
                corrections[side] += -low + .0001
                changed = True
        if not changed: break
    min_z = min(min_z, min(v.z for v in verts))
    if frame == 1: first = [v.copy() for v in verts]
    if frame == LAST: last = [v.copy() for v in verts]
    head_inv = (rig.matrix_world @ rig.pose.bones['Head'].matrix).inverted()
    g = [head_inv @ (glasses.matrix_world @ v.co) for v in glasses.evaluated_get(dg).data.vertices]
    if glasses_ref is None: glasses_ref = g
    glasses_error = max(glasses_error, max((a - b).length for a, b in zip(g, glasses_ref)))
    ankle_log.append({side: tuple(rig.pose.bones['Foot.' + side].head) for side in ['L', 'R']})
    for bone in rig.pose.bones:
        if bone.name in previous and bone.rotation_quaternion.dot(previous[bone.name]) < 0:
            bone.rotation_quaternion = -bone.rotation_quaternion
        previous[bone.name] = bone.rotation_quaternion.copy()
        for field in ['location', 'rotation_quaternion', 'scale']:
            bone.keyframe_insert(field, frame=frame, group=bone.name)
for layer in action.layers:
    for strip in layer.strips:
        for slot in action.slots:
            bag = strip.channelbag(slot)
            if bag:
                for curve in bag.fcurves:
                    for key in curve.keyframe_points: key.interpolation = 'LINEAR'
seam = max((a - b).length for a, b in zip(first, last))
assert seam < 1e-4, ('loop seam', seam)
assert min_z > -.01, ('ground penetration', min_z)
assert glasses_error < 1e-4, ('glasses drift', glasses_error)
assert gait['overreach'] < .01, ('leg overreach', gait['overreach'])

# Planted-foot check: a foot on the ground must stay still in the world while the body travels at `speed`.
units_per_metre = original_pose_height / TARGET_HEIGHT
speed_units = 2 * HALF_SWEEP / (STANCE * (LAST - 1) / FPS)       # source units / s the planted foot moves back under the body
speed_mps = speed_units / units_per_metre
max_slip = 0.0
for frame_index, entry in enumerate(ankle_log):
    phase = frame_index / (LAST - 1)
    for side, offset in [('L', 0), ('R', .5)]:
        t = (phase + offset) % 1
        if t < STANCE - .06 and t > .02:
            expected = -HALF_SWEEP + 2 * HALF_SWEEP * (t / STANCE) + .12
            max_slip = max(max_slip, abs(entry[side][1] - expected))

scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
for obj in [rig, body, glasses]:
    obj.hide_set(False); obj.hide_render = False; obj.select_set(True)
bpy.context.view_layer.objects.active = rig
fbx = FBX_DIR / 'SuitWalk.fbx'
bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
                         axis_forward='-Z', axis_up='Y', apply_unit_scale=True, bake_anim=True, bake_anim_use_nla_strips=False,
                         bake_anim_use_all_actions=False, bake_anim_simplify_factor=0, bake_anim_step=1)

# ---- previews (Blender-side only; helpers are not in the FBX) -------------------------------------------------
for obj in list(bpy.data.objects):
    if obj.name.startswith('Suit_UVSource'): bpy.data.objects.remove(obj, do_unlink=True)
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x, scene.render.resolution_y = 520, 700
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'MATERIAL'
scene.display.shading.show_shadows = True
if scene.world is None: scene.world = bpy.data.worlds.new('PreviewOnly_World')
scene.world.color = (.09, .09, .09)
cam_data = bpy.data.cameras.new('PreviewOnly_Camera')
cam = bpy.data.objects.new('PreviewOnly_Camera', cam_data)
scene.collection.objects.link(cam)
cam_data.type, cam_data.ortho_scale = 'ORTHO', 4.9
scene.camera = cam
prev_dir = ART_DIR / 'Preview'
prev_dir.mkdir(exist_ok=True)
views = {'side': (14, 0, 2.2), 'front': (0, -14, 2.2)}
frames = [1, 5, 9, 13, 18, 22, 26, 30]
for view, location in views.items():
    cam.location = location
    cam.rotation_euler = (Vector((0, 0, 2.15)) - Vector(location)).to_track_quat('-Z', 'Y').to_euler()
    for frame in frames:
        scene.frame_set(frame)
        scene.render.filepath = str(prev_dir / f'{view}_{frame:03d}.png')
        bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ART_DIR / 'SuitWalk.blend'))

# ---- Unity metadata -----------------------------------------------------------------------------------------
template = (R / 'Assets/SecretsThatBreathe/Characters/NavySuitVillain/Conversation/SuitConversation.fbx.meta').read_text()
meta_path = Path(str(fbx) + '.meta')
guid = re.search(r'^guid: ([a-f0-9]+)', meta_path.read_text(), re.M)[1] if meta_path.exists() else uuid.uuid5(uuid.NAMESPACE_URL, 'NavySuitVillain/Walk/SuitWalk.fbx').hex
meta = re.sub(r'^guid: [a-f0-9]+', f'guid: {guid}', template, flags=re.M)
meta = re.sub(r'    clipAnimations:.*?(?=    isReadable:)', '    clipAnimations: []\n', meta, flags=re.S)
meta = re.sub(r'  userData: [^\n]*', '  userData: ', meta)
if '  userData:' not in meta: meta = meta.replace('  assetBundleName:', '  userData: \n  assetBundleName:', 1)
meta_path.write_text(meta)
folder_meta = Path(str(FBX_DIR) + '.meta')
if not folder_meta.exists():
    folder_meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid5(uuid.NAMESPACE_URL, "NavySuitVillain/Walk").hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')

report = dict(source=str(SOURCE.relative_to(R)), source_sha256=SOURCE_HASH, clip=ACTION, fps=FPS, frames=[1, LAST],
              seconds=(LAST - 1) / FPS, bones=len(rig.data.bones), body_triangles=964, sunglasses_triangles=108,
              height_metres=TARGET_HEIGHT, loop_surface_error=seam, minimum_foot_z_source=min_z, glasses_head_relative_error=glasses_error,
              leg_overreach=gait['overreach'], planted_foot_max_slip_source=max_slip,
              planted_foot_travel_metres_per_second=speed_mps, steps_per_minute=60 * 2 / ((LAST - 1) / FPS),
              half_sweep_source=HALF_SWEEP, stance_fraction=STANCE)
(ART_DIR / 'build-report.json').write_text(json.dumps(report, indent=2))
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest() == SOURCE_HASH
print('SUIT_WALK_BUILD', json.dumps(report))
