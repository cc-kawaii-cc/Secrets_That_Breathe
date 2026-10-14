import bpy, json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(root/'Assets/GUY/Model/MEMEE/MEMEEE.fbx'))
for o in bpy.context.scene.objects:
 if o.type=='MESH':
  v=[o.matrix_world@Vector(c) for c in o.bound_box]
  print('OBJECT',o.name,'loc',list(o.location),'rot',list(o.rotation_euler),'bounds',[(min(p[i] for p in v),max(p[i] for p in v)) for i in range(3)],'verts',len(o.data.vertices),'mats',[m.name for m in o.data.materials])
  print('SLOTS',[(i,sum(p.material_index==i for p in o.data.polygons)) for i in range(len(o.data.materials))])
for m in bpy.data.materials:
 print('MATERIAL',m.name,list(m.diffuse_color))
 if m.use_nodes:
  for n in m.node_tree.nodes:
   if n.type=='TEX_IMAGE': print('IMAGE',n.image.filepath if n.image else None)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'tmp/mama_inspect.blend'))
