import bpy, sys
blend, fbx = sys.argv[1], sys.argv[2]
bpy.ops.wm.open_mainfile(filepath=blend)
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type == 'MESH':
        o.select_set(True)
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                         apply_unit_scale=True, use_mesh_modifiers=False, add_leaf_bones=False, bake_anim=False,
                         mesh_smooth_type='EDGE', path_mode='AUTO')
print('EXPORTED', fbx)
