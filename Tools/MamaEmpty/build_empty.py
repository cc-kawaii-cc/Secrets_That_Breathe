import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[2]
ORIGINAL=ROOT/'Assets/GUY/Model/MEMEE'
OUT=ROOT/'Assets/SecretsThatBreathe/Props/MamaEmpty/Models'
OUT.mkdir(parents=True,exist_ok=True)
SRC=ROOT/'ArtSource/MamaEmpty'; SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(ORIGINAL/'MEMEEE.fbx'))
for image in bpy.data.images:
 texture=OUT.parent/'Textures'/Path(bpy.path.abspath(image.filepath)).name
 if texture.is_file(): image.filepath=str(texture)
cup=bpy.data.objects['Cylinder']
bpy.context.view_layer.objects.active=cup
bpy.ops.object.select_all(action='DESELECT'); cup.select_set(True)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
# Keep the original label, rim and UVs. Remove the noodle photograph cap.
bm=bmesh.new(); bm.from_mesh(cup.data)
food=[f for f in bm.faces if f.material_index==2]
assert len(food)==1
ring=list(food[0].verts)
center=sum((v.co for v in ring),Vector())/len(ring)
ring.sort(key=lambda v: math.atan2(v.co.y-center.y,v.co.x-center.x))
bmesh.ops.delete(bm,geom=food,context='FACES_ONLY')
bottom=[]
for v in ring:
 p=center+(v.co-center)*.72; p.z=.008
 bottom.append(bm.verts.new(p))
for i in range(len(ring)):
 j=(i+1)%len(ring)
 f=bm.faces.new((ring[i],ring[j],bottom[j],bottom[i])); f.material_index=1; f.smooth=True
f=bm.faces.new(bottom); f.material_index=1
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
bm.to_mesh(cup.data); bm.free(); cup.data.update()
# Original stick geometry and wood UVs, horizontal with a narrow gap.
rim=max(v.co.z for v in cup.data.vertices)
for name,offset in [('Cylinder.001',-.0046),('Cylinder.002',.0046)]:
 ob=bpy.data.objects[name]
 # Import scale carries centimetre conversion. Local Y is the long axis.
 scale=ob.scale.copy()
 ob.rotation_euler=(0,0,-math.pi/2)
 ob.scale=(scale.x,scale.y*1.5,scale.z)
 height=max(abs(v.co.z*scale.z) for v in ob.data.vertices)
 ob.location=(center.x,center.y+.037+offset,rim+height+.0003)
 bpy.context.view_layer.objects.active=ob
 bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True)
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
parts=[cup,bpy.data.objects['Cylinder.001'],bpy.data.objects['Cylinder.002']]
for m in cup.data.materials:
 if m and m.name=='Material.002':
  m.diffuse_color=(.8,.8,.8,1)
  m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.8,.8,.8,1)
bpy.ops.object.select_all(action='DESELECT')
for ob in parts: ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'MEMEEE_Empty.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False,path_mode='RELATIVE')
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'MEMEEE_Empty.blend'))
report={'source':'Assets/GUY/Model/MEMEE/MEMEEE.fbx','food_faces_remaining':sum(p.material_index==2 for p in cup.data.polygons),'stick_count':2,'rim_height':rim,'stick_bounds':{o.name:[(min((o.matrix_world@v.co)[i] for v in o.data.vertices),max((o.matrix_world@v.co)[i] for v in o.data.vertices)) for i in range(3)] for o in parts[1:]}}
(SRC/'verification.json').write_text(json.dumps(report,indent=2))
def aim(ob,pt): ob.rotation_euler=(Vector(pt)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001))
floor=bpy.data.materials.new('PreviewFloor'); floor.diffuse_color=(.1,.12,.14,1); bpy.context.object.data.materials.append(floor)
for pos,power,size in [((-.4,-.5,.8),20,.5),((.4,.3,.6),12,.4)]:
 bpy.ops.object.light_add(type='AREA',location=pos); o=bpy.context.object; o.data.energy=power; o.data.size=size; aim(o,(0,0,.12))
bpy.ops.object.camera_add(location=(.36,-.56,.58)); cam=bpy.context.object; cam.data.type='ORTHO'; cam.data.ortho_scale=.45; aim(cam,(center.x,center.y,.13))
scene=bpy.context.scene; scene.camera=cam; scene.world.color=(.25,.25,.25); scene.render.engine='CYCLES'; scene.cycles.samples=32
scene.render.resolution_x=1000; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.render.filepath=str(SRC/'MEMEEE_Empty_Preview.png'); bpy.ops.render.render(write_still=True)
print('VERIFICATION',report)
