"""Optimize copies; do not write Assets. Preserve separate objects and local transforms."""
import bpy, bmesh, math, json, random, sys, os
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[2]; OUT=Path(os.environ.get('GUY_OPT_OUTPUT',str(ROOT/'Tools/GUYOptimization')))
config=json.loads((OUT/'config.json').read_text(encoding='utf-8'))
index=int(sys.argv[sys.argv.index('--')+1]); item=config[index]
folder=OUT/('Model'+str(index));folder.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(ROOT/item['path']))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
def bounds(o):
    v=[Vector(c) for c in o.bound_box]
    return [min(p[i] for p in v) for i in range(3)],[max(p[i] for p in v) for i in range(3)]
baseline={o.name:{'matrix':[list(r) for r in o.matrix_local], 'parent':o.parent.name if o.parent else None,
                  'materials':[m.name if m else '' for m in o.data.materials], 'UVs':[u.name for u in o.data.uv_layers],
                  'triangles':tris(o),'bounds':bounds(o)} for o in meshes}
world=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=Vector([min(v[i] for v in world) for i in range(3)]);hi=Vector([max(v[i] for v in world) for i in range(3)])
center=(lo+hi)/2;radius=(hi-lo).length/2
scene=bpy.context.scene
scene.world=bpy.data.worlds.new('Audit World')
scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=480;scene.render.resolution_y=480;scene.render.resolution_percentage=100
scene.world.color=(.12,.12,.12)
shade=scene.display.shading;shade.light='STUDIO';shade.color_type='MATERIAL';shade.show_shadows=True;shade.show_cavity=True
shade.cavity_type='BOTH';shade.show_specular_highlight=True;shade.background_type='WORLD'
camera=bpy.data.objects.new('_AuditCamera',bpy.data.cameras.new('_AuditCamera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=max(.01,radius*2.5)
directions=[(1,-1,.65),(-1,1,.65),(0,0,1),(.3,1,-.55)]
def render(tag):
    for n,d in enumerate(directions):
        camera.location=center+Vector(d).normalized()*max(.1,radius*4)
        camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(folder/(tag+str(n)+'.png'));bpy.ops.render.render(write_still=True)
render('before')
results=[]
for o in meshes:
    before=baseline[o.name]['triangles'];record={'name':o.name,'before':before,'changed':False}
    if before>item.get('minimumTriangles',5000) and o.name not in item.get('keepMeshes',[]):
        original=o.data; original.use_fake_user=True
        dg=bpy.context.evaluated_depsgraph_get();original_bvh=BVHTree.FromObject(o,dg)
        original_points=[original.vertices[i].co.copy() for i in range(0,len(original.vertices),max(1,len(original.vertices)//1600))]
        b0,b1=bounds(o);diag=(Vector(b1)-Vector(b0)).length
        budget=max(item.get('minimumBudget',1000),int(before*item['ratio']))
        for attempt in range(5):
            if o.data!=original:
                previous=o.data;o.data=original;bpy.data.meshes.remove(previous)
            o.data=original.copy()
            bpy.context.view_layer.objects.active=o
            if item.get('weldCoincidentVertices',False):
                bm=bmesh.new();bm.from_mesh(o.data)
                bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=diag*1e-7)
                bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=diag*1e-7)
                bm.to_mesh(o.data);bm.free();o.data.update()
            mod=o.modifiers.new('GUY planar cleanup','DECIMATE');mod.decimate_type='DISSOLVE'
            mod.angle_limit=math.radians(.75/(2**attempt));mod.delimit={'NORMAL','MATERIAL','UV','SHARP'}
            bpy.ops.object.modifier_apply(modifier=mod.name)
            planar=tris(o)
            if planar>budget:
                mod=o.modifiers.new('GUY controlled simplification','DECIMATE');mod.decimate_type='COLLAPSE'
                mod.ratio=min(1,budget/planar);mod.use_collapse_triangulate=True
                bpy.ops.object.modifier_apply(modifier=mod.name)
            bpy.context.view_layer.update()
            bvh=BVHTree.FromObject(o,bpy.context.evaluated_depsgraph_get())
            samples=[o.data.vertices[i].co.copy() for i in range(0,len(o.data.vertices),max(1,len(o.data.vertices)//1600))]
            errors=[]
            for tree,points in [(original_bvh,samples),(bvh,original_points)]:
                for point in points:
                    hit=tree.find_nearest(point)
                    errors.append(hit[3] if hit[0] is not None else float('inf'))
            error=max(errors,default=0);newbounds=bounds(o)
            bounderror=max(abs(newbounds[j][i]-baseline[o.name]['bounds'][j][i]) for j in range(2) for i in range(3))
            print('SHAPE_CHECK',o.name,attempt,error/max(diag,.000001),bounderror/max(diag,.000001),flush=True)
            if error<=diag*.0025 and bounderror<=diag*.0025:break
            budget=min(before,int(budget*2))
        else:
            # Dense subdivision grids can collapse badly. Try removing regular
            # grid subdivisions instead, accepting only the same shape tolerance.
            for iterations in [8,6,4,2]:
                previous=o.data;o.data=original.copy();bpy.data.meshes.remove(previous)
                mod=o.modifiers.new('GUY regular grid reduction','DECIMATE');mod.decimate_type='UNSUBDIV';mod.iterations=iterations
                bpy.ops.object.modifier_apply(modifier=mod.name);bpy.context.view_layer.update()
                bvh=BVHTree.FromObject(o,bpy.context.evaluated_depsgraph_get())
                samples=[o.data.vertices[i].co.copy() for i in range(0,len(o.data.vertices),max(1,len(o.data.vertices)//1600))]
                errors=[]
                for tree,points in [(original_bvh,samples),(bvh,original_points)]:
                    for point in points:
                        hit=tree.find_nearest(point);errors.append(hit[3] if hit[0] is not None else float('inf'))
                error=max(errors,default=0);nb=bounds(o)
                bounderror=max(abs(nb[j][i]-baseline[o.name]['bounds'][j][i]) for j in range(2) for i in range(3))
                print('GRID_CHECK',o.name,iterations,tris(o),error/max(diag,.000001),bounderror/max(diag,.000001),flush=True)
                if error<=diag*.0025 and bounderror<=diag*.0025:
                    record['unsubdivideIterations']=iterations;break
            else:
                # Preserve detail instead of forcing a reduction that fails the shape check.
                previous=o.data;o.data=original;bpy.data.meshes.remove(previous)
                error=bounderror=0;record['keptOriginalForShape']=True
        assert len(o.data.uv_layers)==len(original.uv_layers)
        assert [m.name if m else '' for m in o.data.materials]==baseline[o.name]['materials']
        record.update(changed=tris(o)<before,after=tris(o),surfaceSampleMaxError=error,localDiagonal=diag,boundsMaxError=bounderror,
                      planarTriangles=planar,budget=budget)
        original.use_fake_user=False
        if original.users==0:bpy.data.meshes.remove(original)
        del original_bvh,bvh
    else:record['after']=before
    assert [list(r) for r in o.matrix_local]==baseline[o.name]['matrix']
    assert (o.parent.name if o.parent else None)==baseline[o.name]['parent']
    results.append(record);print('OPTIMIZED',o.name,before,'->',record['after'],flush=True)
render('after')
bpy.data.objects.remove(camera,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=str(folder/'optimized.blend'))
bpy.ops.export_scene.fbx(filepath=str(folder/'optimized.fbx'),use_selection=False,object_types={'MESH','EMPTY'},add_leaf_bones=False,
                         bake_anim=False,path_mode='AUTO',axis_forward='-Z',axis_up='Y')
result={'path':item['path'],'before':sum(r['before'] for r in results),'after':sum(r['after'] for r in results),
        'meshes':results,'baseline':baseline,'views':len(directions),'done':True}
(folder/'result.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('GUY_OPTIMIZATION_DONE',index,result['before'],result['after'],flush=True)
