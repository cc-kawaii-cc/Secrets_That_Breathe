"""Two complementary eight-second planted-foot conversation loops, real skinning."""
import bpy,math,json,hashlib,re,uuid
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent
FPS=30;LAST=241
SOURCE=R/'ArtSource/LowPolyMaleSlowWalk/IdleTalk/LowPolyMale_IdleTalk.blend'
SOURCE_HASH=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
roles=[('Police','ThaiPolice',1.8,'ThaiPolice_Body'),('Suit','NavySuitVillain',1.85,'Body')]
allreports={}
def around(point,rotation):return Matrix.Translation(point)@rotation@Matrix.Translation(-point)
def window(phase,start,end):
 if not start<phase<end:return 0.0
 u=(phase-start)/(end-start)
 return math.sin(math.pi*u)**2*(.82+.18*math.sin(6*math.pi*u))
def knee(hip,ankle,upper,lower):
 delta=ankle-hip;distance=min(delta.length,upper+lower-.0001);direction=delta.normalized()
 a=(upper*upper-lower*lower+distance*distance)/(2*distance);pole=Vector((0,-1,0));perp=(pole-direction*pole.dot(direction)).normalized()
 return hip+direction*a+perp*math.sqrt(max(0,upper*upper-a*a))
for role,folder,target_height,static_name in roles:
 O=R/'Assets/SecretsThatBreathe/Characters'/folder/'Conversation';O.mkdir(exist_ok=True)
 bpy.ops.wm.open_mainfile(filepath=str(SOURCE));s=bpy.context.scene
 rig=bpy.data.objects['SlowWalkRig'];body=bpy.data.objects['Cube'];rig.data.pose_position='POSE';s.frame_set(1);bpy.context.view_layer.update()
 rest={b.name:(b.head_local.copy(),b.tail_local.copy()) for b in rig.data.bones};length={n:(b-a).length for n,(a,b) in rest.items()}
 dg=bpy.context.evaluated_depsgraph_get();orig=[body.matrix_world@v.co for v in body.evaluated_get(dg).data.vertices]
 original_pose_height=max(v.z for v in orig)-min(v.z for v in orig);original_min=min(v.z for v in orig)
 # Import only the new character's visual data; retain the proven original weights.
 static=R/'ArtSource'/folder/(folder+'_LowPoly.blend')
 with bpy.data.libraries.load(str(static),link=False) as (src,dst):dst.objects=[static_name]+(['Sunglasses'] if role=='Suit' else [])
 skinbody=dst.objects[0]
 skinbody.name=role+'_UVSource_DoNotExport'
 assert len(skinbody.data.vertices)==484
 assert [tuple(p.vertices) for p in skinbody.data.polygons]==[tuple(p.vertices) for p in body.data.polygons]
 for a,b in zip(body.data.uv_layers.active.data,skinbody.data.uv_layers.active.data):a.uv=b.uv
 body.data.materials.clear();body.data.materials.append(skinbody.data.materials[0]);body.name='Body'
 glasses=None
 if role=='Suit':
  glasses=dst.objects[1];s.collection.objects.link(glasses)
  factor=target_height/original_pose_height
  skin=rig.matrix_world@rig.pose.bones['Head'].matrix@rig.data.bones['Head'].matrix_local.inverted()@rig.matrix_world.inverted()
  inverse=skin.inverted()
  for v in glasses.data.vertices:v.co=inverse@((v.co+Vector((0,0,original_min*factor)))/factor)
  glasses.parent=rig;glasses.matrix_parent_inverse=Matrix.Identity(4);glasses.matrix_basis=Matrix.Identity(4)
  group=glasses.vertex_groups.new(name='Head');group.add(list(range(len(glasses.data.vertices))),1,'REPLACE')
  modifier=glasses.modifiers.new('Follow_Head','ARMATURE');modifier.object=rig
 rig.name=role+'_ConversationRig'
 for constraint in list(rig.constraints):rig.constraints.remove(constraint)
 action=bpy.data.actions.new(role+'Conversation');action.use_fake_user=True;rig.animation_data.action=action
 s.frame_start=1;s.frame_end=LAST;s.render.fps=FPS
 def points(phase,corrections):
  t=2*math.pi*phase;speaking=window(phase,.045,.47) if role=='Police' else window(phase,.52,.955)
  listening=window(phase,.52,.955) if role=='Police' else window(phase,.045,.47)
  delta=Vector((.016*math.sin(t),.003*math.sin(2*t),.035+.003*math.sin(2*t)))
  trunk=Matrix.Translation(delta)@around(rest['Hips'][0],Matrix.Rotation(math.radians(.45)*math.sin(t),4,'Y'))
  torso=trunk@around(rest['Spine'][0],Matrix.Rotation(math.radians(1.0)*speaking*math.sin(2*t),4,'Z')@Matrix.Rotation(math.radians(.45)*math.sin(2*t),4,'X'))
  nod=math.radians(2.8)*listening*math.sin(6*t)+math.radians(.7)*speaking
  head=torso@around(rest['Head'][0],Matrix.Rotation(nod,4,'X')@Matrix.Rotation(math.radians(1.7)*math.sin(t),4,'Z'))
  out={'Root':rest['Root']}
  for name in ['Hips','Spine','Chest','Neck','Head']:
   mat=trunk if name=='Hips' else head if name=='Head' else torso;out[name]=tuple(mat@v for v in rest[name])
  for side,sign in [('L',1),('R',-1)]:
   hip=trunk@rest['UpperLeg.'+side][0];ankle=Vector((sign*.27,.12,.28+corrections[side]));joint=knee(hip,ankle,length['UpperLeg.'+side],length['LowerLeg.'+side])
   toe=ankle+(rest['Foot.'+side][1]-rest['Foot.'+side][0]);tip=toe+(rest['Toes.'+side][1]-rest['Toes.'+side][0])
   out['UpperLeg.'+side]=hip,joint;out['LowerLeg.'+side]=joint,ankle;out['Foot.'+side]=ankle,toe;out['Toes.'+side]=toe,tip
   dominant=(side=='R' if role=='Police' else side=='L');amount=speaking*(1 if dominant else .18)
   shoulder_start,shoulder=(torso@v for v in rest['Shoulder.'+side])
   upper=Vector((sign*(.15+.24*amount),.025-.19*amount,-.99+.12*amount)).normalized()
   elbow=shoulder+upper*length['UpperArm.'+side]
   fore=Vector((sign*(.035-.14*amount),-.13-.77*amount,-.98+1.36*amount)).normalized()
   wrist=elbow+fore*length['Forearm.'+side]
   fingers=wrist+Vector((sign*.05,-.08-.77*amount,-.99+.75*amount)).normalized()*length['Hand.'+side]
   out['Shoulder.'+side]=shoulder_start,shoulder;out['UpperArm.'+side]=shoulder,elbow;out['Forearm.'+side]=elbow,wrist;out['Hand.'+side]=wrist,fingers
  return out
 def apply(pairs):
  matrices={}
  for bone in rig.pose.bones:
   bone.rotation_mode='QUATERNION';a,b=pairs[bone.name];old_a,old_b=rest[bone.name]
   rotation=(old_b-old_a).rotation_difference(b-a)
   mat=Matrix.Translation(a)@rotation.to_matrix().to_4x4()@bone.bone.matrix_local.to_quaternion().to_matrix().to_4x4()
   if bone.parent:bone.matrix_basis=bone.bone.convert_local_to_pose(mat,bone.bone.matrix_local,parent_matrix=matrices[bone.parent.name],parent_matrix_local=bone.parent.bone.matrix_local,invert=True)
   else:bone.matrix_basis=bone.bone.convert_local_to_pose(mat,bone.bone.matrix_local,invert=True)
   matrices[bone.name]=mat
  bpy.context.view_layer.update()
 feet={side:[v.index for v in body.data.vertices if (body.matrix_world@v.co).z<.30 and sign*(body.matrix_world@v.co).x>0] for side,sign in [('L',1),('R',-1)]}
 first=last=None;previous={};minz=10;max_ankle_drift=0;max_hand_motion=0;initial_hands=None;bounds0=None;glasses_error=0;glasses_ref=None
 for frame in range(1,LAST+1):
  s.frame_set(frame);phase=(frame-1)/(LAST-1);correct={'L':0,'R':0}
  for iteration in range(3):
   apply(points(phase,correct));dg=bpy.context.evaluated_depsgraph_get();vertices=[body.matrix_world@v.co for v in body.evaluated_get(dg).data.vertices]
   changed=False
   for side in ['L','R']:
    low=min(vertices[i].z for i in feet[side])
    if low<0:correct[side]+=-low+.0001;changed=True
   if not changed:break
  minz=min(minz,min(v.z for v in vertices))
  for side,sign in [('L',1),('R',-1)]:
   ankle=rig.pose.bones['Foot.'+side].head;max_ankle_drift=max(max_ankle_drift,math.hypot(ankle.x-sign*.27,ankle.y-.12))
  hands=[rig.pose.bones['Hand.'+side].head.copy() for side in ['L','R']]
  if initial_hands is None:initial_hands=hands
  max_hand_motion=max(max_hand_motion,max((a-b).length for a,b in zip(hands,initial_hands)))
  if frame==1:first=[v.copy() for v in vertices];bounds0=[min(v.z for v in vertices),max(v.z for v in vertices)]
  if frame==LAST:last=[v.copy() for v in vertices]
  if glasses:
   head_inverse=(rig.matrix_world@rig.pose.bones['Head'].matrix).inverted();g=[head_inverse@(glasses.matrix_world@v.co) for v in glasses.evaluated_get(dg).data.vertices]
   if glasses_ref is None:glasses_ref=g
   glasses_error=max(glasses_error,max((a-b).length for a,b in zip(g,glasses_ref)))
  for bone in rig.pose.bones:
   if bone.name in previous and bone.rotation_quaternion.dot(previous[bone.name])<0:bone.rotation_quaternion=-bone.rotation_quaternion
   previous[bone.name]=bone.rotation_quaternion.copy()
   for field in ['location','rotation_quaternion','scale']:bone.keyframe_insert(field,frame=frame,group=bone.name)
 for layer in action.layers:
  for strip in layer.strips:
   for slot in action.slots:
    bag=strip.channelbag(slot)
    if bag:
     for curve in bag.fcurves:
      for key in curve.keyframe_points:key.interpolation='LINEAR'
 seam=max((a-b).length for a,b in zip(first,last));assert seam<1e-4 and minz>-.01 and max_ankle_drift<.0001
 assert max_hand_motion>.5 and glasses_error<1e-4
 s.frame_set(1);bpy.ops.object.select_all(action='DESELECT')
 export=[rig,body]+([glasses] if glasses else [])
 for obj in export:obj.hide_set(False);obj.hide_render=False;obj.select_set(True)
 bpy.context.view_layer.objects.active=rig
 fbx=O/(role+'Conversation.fbx')
 bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_simplify_factor=0,bake_anim_step=1)
 bpy.ops.wm.save_as_mainfile(filepath=str(W/(role+'Conversation.blend')))
 # Use the original generic animated importer, remapping only this character's materials.
 meta=(R/'Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_IdleTalk.fbx.meta').read_text();mp=Path(str(fbx)+'.meta')
 g=re.search(r'^guid: ([a-f0-9]+)',mp.read_text(),re.M)[1] if mp.exists() else uuid.uuid5(uuid.NAMESPACE_URL,'PoliceSuitConversation/'+role).hex
 meta=re.sub(r'^guid: [a-f0-9]+',f'guid: {g}',meta,flags=re.M)
 entries=[]
 for name in [folder]+(['SunglassesBlack'] if role=='Suit' else []):
  matpath=O.parent/(name+'.mat');mg=re.search(r'^guid: ([a-f0-9]+)',Path(str(matpath)+'.meta').read_text(),re.M)[1]
  entries.append(f'  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: {name}\n    second: {{fileID: 2100000, guid: {mg}, type: 2}}\n')
 meta=re.sub(r'  externalObjects:.*?(?=  materials:)','  externalObjects:\n'+''.join(entries),meta,flags=re.S)
 # Discard the old IdleTalk clip frame/name overrides; the Unity installer configures this new take.
 meta=re.sub(r'    clipAnimations:.*?(?=    isReadable:)','    clipAnimations: []\n',meta,flags=re.S)
 meta=re.sub(r'  userData: [^\n]*','  userData: police-suit-conversation-v1',meta);mp.write_text(meta)
 foldermeta=Path(str(O)+'.meta')
 if not foldermeta.exists():foldermeta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid5(uuid.NAMESPACE_URL,str(O.relative_to(R))).hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n')
 allreports[role]={'fbx':str(fbx.relative_to(R)),'height_metres':target_height,'seconds':8,'frames':[1,LAST],'fps':FPS,'bones':len(rig.data.bones),'body_triangles':964,'sunglasses_triangles':108 if glasses else 0,'loop_surface_error':seam,'minimum_foot_z_source':minz,'max_ankle_xy_drift_source':max_ankle_drift,'max_hand_motion_source':max_hand_motion,'glasses_head_relative_error':glasses_error,'initial_floor_source':bounds0[0],'initial_height_source':bounds0[1]-bounds0[0]}
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==SOURCE_HASH
(W/'build-report.json').write_text(json.dumps(allreports,indent=2));print('CONVERSATION_BUILD',json.dumps(allreports))
