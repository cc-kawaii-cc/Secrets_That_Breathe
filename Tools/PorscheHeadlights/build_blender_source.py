"""Separate only the measured original headlight faces; preserve the car source."""
import bpy, os, json, hashlib
from collections import defaultdict
from mathutils import Vector
root=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
work=os.path.join(root,'Tools','PorscheHeadlights')
out=os.path.join(work,'Source');os.makedirs(out,exist_ok=True)
model=os.path.join(root,'Assets','Model Palm','Porsche V.2','PorscheV.2.fbx')
selection=json.load(open(os.path.join(work,'lens_selection.json'),encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=model)
def tris(o): return sum(len(p.vertices)-2 for p in o.data.polygons)
initial=sum(tris(o) for o in bpy.context.scene.objects if o.type=='MESH')
baseline={o.name:{'matrix':[list(row) for row in o.matrix_world], 'triangles':tris(o),'customNormals':o.data.has_custom_normals} for o in bpy.context.scene.objects if o.type=='MESH'}
body=bpy.data.objects['Body']; light=bpy.data.objects['Light']
# Edit-mode separation changes the normal spaces at the lens boundary. Keep
# original corner vectors as ordinary corner data through separate and join,
# then encode them back into each resulting mesh's new normal spaces.
normal_attribute='_porsche_original_corner_normal'
assert body.matrix_world==light.matrix_world
for ob in (body,light):
    normals=[tuple(n.vector) for n in ob.data.corner_normals]
    normal_attr=ob.data.attributes.new(normal_attribute,'FLOAT_VECTOR','CORNER')
    for value,normal in zip(normal_attr.data,normals):value.vector=normal
attr=body.data.attributes.new('_headlight_selection','INT','FACE')
for gid,group in enumerate(selection['groups'],1):
    for i in group['blenderPolygonIndices']:attr.data[i].value=gid

def separate(ob,selected,name):
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
    bpy.context.tool_settings.mesh_select_mode=(False,False,True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='DESELECT');bpy.ops.object.mode_set(mode='OBJECT')
    for p,flag in zip(ob.data.polygons,selected):p.select=flag
    before=set(bpy.context.scene.objects)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.separate(type='SELECTED');bpy.ops.object.mode_set(mode='OBJECT')
    added=list(set(bpy.context.scene.objects)-before)
    assert len(added)==1, (name,len(added))
    child=added[0];child.name=name;child.data.name=name
    return child

for gid,group in enumerate(selection['groups'],1):
    flags=[x.value==gid for x in body.data.attributes['_headlight_selection'].data]
    lens=separate(body,flags,'Headlight_'+group['side'])
    assert tris(lens)==group['expectedTriangles']
    side=1 if group['side']=='Left' else -1
    flags=[]
    for p in light.data.polygons:
        c=light.matrix_world@p.center
        flags.append(c.y < -1 and c.x*side>0)
    bulb=separate(light,flags,'_Projector_'+group['side'])
    bpy.ops.object.select_all(action='DESELECT');lens.select_set(True);bulb.select_set(True);bpy.context.view_layer.objects.active=lens
    bpy.ops.object.join()
    # All selected faces use exactly the same original atlas material.
    atlas=next(m for m in lens.data.materials if m and m.name=='tripo_mat_c3595359.001')
    lens.data.materials.clear();lens.data.materials.append(atlas)
    for p in lens.data.polygons:p.material_index=0
for ob in bpy.context.scene.objects:
    if ob.type=='MESH' and normal_attribute in ob.data.attributes:
        normal_attr=ob.data.attributes[normal_attribute]
        # Some source FBX corners differ slightly inside one smooth fan. The
        # setter averages those unless the corresponding edge is explicitly
        # split. Preserve the supplied vectors rather than averaging them.
        edge_corners=defaultdict(lambda:defaultdict(list))
        for polygon in ob.data.polygons:
            loops=list(polygon.loop_indices)
            for offset,li in enumerate(loops):
                lj=loops[(offset+1)%len(loops)]
                edge=ob.data.loops[li].edge_index
                for corner in (li,lj):
                    edge_corners[edge][ob.data.loops[corner].vertex_index].append(normal_attr.data[corner].vector.copy())
        for edge,vertices in edge_corners.items():
            if any((normal-values[0]).length>1e-6 for values in vertices.values() for normal in values[1:]):
                ob.data.edges[edge].use_edge_sharp=True
        ob.data.normals_split_custom_set([tuple(value.vector) for value in normal_attr.data])
        ob.data.attributes.remove(normal_attr)
    if ob.type=='MESH' and '_headlight_selection' in ob.data.attributes:
        ob.data.attributes.remove(ob.data.attributes['_headlight_selection'])
    if ob.type=='MESH' and ob.name in baseline:
        assert [list(row) for row in ob.matrix_world]==baseline[ob.name]['matrix'],ob.name
after=sum(tris(o) for o in bpy.context.scene.objects if o.type=='MESH')
assert initial==after,(initial,after)
# Re-resolve source texture paths before saving in the dedicated Source directory.
texdir=os.path.join(root,'Assets','Model Palm','Porsche V.2','Texture')
for im in bpy.data.images:
    basename=bpy.path.abspath(im.filepath).replace('\\','/').split('/')[-1]
    candidate=os.path.join(texdir,basename)
    if os.path.exists(candidate):im.filepath=candidate;im.reload()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'PorscheV2_HeadlightsSeparated.blend'))
bpy.ops.export_scene.fbx(filepath=os.path.join(out,'PorscheV2_HeadlightsSeparated.fbx'),use_selection=True,object_types={'MESH'},add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE',use_tspace=True,mesh_smooth_type='EDGE')
summary={'originalTriangles':initial,'separatedTriangles':after,'objects':[{'name':o.name,'triangles':tris(o),'customNormals':o.data.has_custom_normals,'uvLayers':[u.name for u in o.data.uv_layers]} for o in bpy.context.scene.objects if o.type=='MESH'],'baseline':baseline}
json.dump(summary,open(os.path.join(out,'source_validation.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=2)
print('PORSCHE_SEPARATED_SOURCE_COMPLETE',initial,after)
