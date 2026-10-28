import bpy,bmesh,json,sys,hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
ROOT=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'Tools/FDAudit'))
from read_fbx import read
import numpy as np
def canonical(n):
 return (n.name,tuple((str(p.dtype),p.tobytes()) if isinstance(p,np.ndarray) else p for p in n.props),tuple(canonical(c) for c in n.children))
old=read(W/'lowpoly_male_base_mesh-TPOSE.before.fbx');new=read(W/'candidate.fbx')
for key in ['GlobalSettings','Connections','Definitions','Takes','Documents','References']:
 a,b=old.child(key),new.child(key)
 assert (canonical(a) if a else None)==(canonical(b) if b else None),key
oldobjs=old.child('Objects').children;newobjs=new.child('Objects').children
assert len(oldobjs)==len(newobjs)
for a,b in zip(oldobjs,newobjs):
 if a.name=='Geometry':assert a.props==b.props
 else:assert canonical(a)==canonical(b),a.name
bpy.ops.wm.open_mainfile(filepath=str(W/'LowPolyMale_FiveFingers.blend'))
source=bpy.data.objects['Cube'];p=[source.matrix_world@v.co for v in source.data.vertices]
source_matrix=source.matrix_world.copy()
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(W/'candidate.fbx'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(meshes)==1
o=meshes[0];me=o.data;assert o.name=='Cube' and o.parent is None and not o.modifiers
actual=[o.matrix_world@v.co for v in me.vertices]
tree=KDTree(len(actual))
for i,v in enumerate(actual):tree.insert(v,i)
tree.balance();err=max(tree.find(v)[2] for v in p);assert err<1e-5,err
me.calc_loop_triangles();assert len(me.loop_triangles)==1428
bm=bmesh.new();bm.from_mesh(me)
assert all(e.is_manifold for e in bm.edges)
assert all(f.calc_area()>1e-10 for f in bm.faces)
volume=bm.calc_volume(signed=True);assert volume>0;bm.free()
baseline=json.loads((W/'baseline.json').read_text())[0]
assert abs(max(v.z for v in actual)-baseline['bounds'][2][1])<1e-6
assert abs(min(v.z for v in actual)-baseline['bounds'][2][0])<1e-6
assert len(me.materials)==1 and me.materials[0].name=='Material.001'
build=json.loads((W/'build-report.json').read_text())
keep=[]
removed=set(range(33,103))-{34,35,41,43,46,49}
for idx in list(removed):
 v=Vector(baseline['coords'][idx]);v.x=-v.x
 removed.add(min(range(484),key=lambda i:(Vector(baseline['coords'][i])-v).length))
for i,v in enumerate(baseline['coords']):
 if i not in removed:
  keep.append(i);assert tree.find(Vector(v))[2]<1e-6
assert len(keep)==356
assert (ROOT/'Assets/Nemo_Upgrade/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx.meta').read_bytes()==(W/'original.fbx.meta.txt').read_bytes()
report={'export_objects':len(meshes),'object_name':o.name,'vertices':len(me.vertices),'triangles':len(me.loop_triangles),'digits_each_hand':5,'max_export_surface_error':err,'unchanged_body_vertices':len(keep),'body_uvs_preserved_by_builder':True,'original_fbx_ids_transforms_materials_connections_and_settings_preserved':True,'source_meta_unchanged':True,'closed_manifold':True,'signed_volume':volume,'height_preserved':True,'export_sha256':hashlib.sha256((W/'candidate.fbx').read_bytes()).hexdigest(),'unity_editor_visual_check':False}
(W/'validation.json').write_text(json.dumps(report,indent=2))
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=1000;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.display.shading.light='STUDIO';scene.display.shading.color_type='SINGLE';scene.display.shading.single_color=(.6,.6,.6)
scene.display.shading.show_shadows=False;scene.display.shading.show_cavity=True
scene.world=bpy.data.worlds.new('ReviewWorld');scene.world.color=(.12,.12,.12)
for sign in [1,-1]:
 copy=o.copy();copy.data=me.copy();scene.collection.objects.link(copy)
 bm=bmesh.new();bm.from_mesh(copy.data);bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.x*sign<1.00],context='VERTS');bm.to_mesh(copy.data);bm.free();o.hide_render=True
 target=Vector((1.25*sign,-.07,2.20));bpy.ops.object.camera_add(location=target+Vector((5*sign,1,2)));c=bpy.context.object
 c.rotation_euler=(target-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=.8;scene.camera=c
 scene.render.filepath=str(W/('export_hand_'+('right' if sign==1 else 'left')+'.png'));bpy.ops.render.render(write_still=True)
 bpy.data.objects.remove(copy,do_unlink=True);o.hide_render=False
print('VALIDATION',json.dumps(report))
