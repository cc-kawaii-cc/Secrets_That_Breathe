import bpy,sys,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=root/'Assets/GUY/Model/Paint bucket/Paint bucket.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(p))
print('IMPORTED',[(o.name,len(o.data.vertices)) for o in bpy.context.scene.objects if o.type=='MESH'],flush=True)
bpy.ops.export_scene.fbx(filepath=str(root/'Tools/GUYOptimization/roundtrip.fbx'),use_selection=False,object_types={'MESH','EMPTY'},add_leaf_bones=False,bake_anim=False,path_mode='AUTO',axis_forward='-Z',axis_up='Y')
print('ROUNDTRIP_DONE',flush=True)
