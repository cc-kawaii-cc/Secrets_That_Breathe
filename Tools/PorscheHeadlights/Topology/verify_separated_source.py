import bpy, os, json, math, sys
from collections import Counter, defaultdict
OUT=os.path.dirname(os.path.abspath(__file__)); BASE=os.path.dirname(OUT)
ROOT=os.path.dirname(os.path.dirname(BASE))
def snapshot():
    obs=[]; polys=[]; normals=[]; corners=[]; normalmap=defaultdict(list)
    for ob in bpy.context.scene.objects:
        if ob.type!='MESH':continue
        me=ob.data; me.calc_loop_triangles();uv=me.uv_layers[0]
        tex={}
        for mat in me.materials:
            if mat and mat.use_nodes:
                tex[mat.name]=[bpy.path.abspath(n.image.filepath) for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
        obs.append({'name':ob.name,'triangles':len(me.loop_triangles),'matrix':[list(row) for row in ob.matrix_world],'materials':list(tex),'textures':tex,'hasCustomNormals':me.has_custom_normals,'min':[min((ob.matrix_world@v.co)[i] for v in me.vertices) for i in range(3)],'max':[max((ob.matrix_world@v.co)[i] for v in me.vertices) for i in range(3)]})
        normalmat=ob.matrix_world.to_3x3().inverted().transposed()
        for p in me.polygons:
            cr=[]
            for li in p.loop_indices:
                co=ob.matrix_world@me.vertices[me.loops[li].vertex_index].co
                u=uv.data[li].uv; no=(normalmat@me.corner_normals[li].vector).normalized()
                corners.append(tuple(round(x,6) for x in (*co,*u)))
                cr.append(tuple(round(x,6) for x in (*co,*u)))
                normals.append(tuple(round(x,5) for x in (*co,*u,*no)))
                normalmap[tuple(round(x,6) for x in (*co,*u))].append(tuple(no))
            polys.append(tuple(sorted(cr)))
    return obs,Counter(corners),Counter(normals),Counter(polys),normalmap
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT,'Assets','Model Palm','Porsche V.2','PorscheV.2.fbx'))
before=snapshot()
export_mode='--export' in sys.argv
if export_mode:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(BASE,'Source','PorscheV2_HeadlightsSeparated.fbx'))
else:
    bpy.ops.wm.open_mainfile(filepath=os.path.join(BASE,'Source','PorscheV2_HeadlightsSeparated.blend'))
after=snapshot()
original={x['name']:x for x in before[0]};current={x['name']:x for x in after[0]}
angles=[];dists=[];worst=[]
for key,nlist in before[4].items():
    for n in nlist:
        candidates=after[4].get(key,[])
        if not candidates:continue
        best=min(candidates,key=lambda m:sum((a-b)**2 for a,b in zip(n,m)))
        d=math.sqrt(sum((a-b)**2 for a,b in zip(n,best)));dists.append(d)
        angle=math.degrees(2*math.asin(min(1,d/2)));angles.append(angle)
        if angle>.1:worst.append({'posUV':key,'angle':angle,'before':n,'after':best})
r={'beforeTriangles':sum(o['triangles'] for o in before[0]),'afterTriangles':sum(o['triangles'] for o in after[0]),'positionUvCornerMultisetEqual6Decimals':before[1]==after[1],'polygonSurfaceMultisetEqual6Decimals':before[3]==after[3],'positionUvNormalMultisetEqual5Decimals':before[2]==after[2],'missingNormalCorners':sum((before[2]-after[2]).values()),'addedNormalCorners':sum((after[2]-before[2]).values()),'maxNormalAngleDegrees':max(angles),'avgNormalAngleDegrees':sum(angles)/len(angles),'maxNormalVectorDistance':max(dists),'normalsOver0_1Degree':len(worst),'normalDifferenceExamples':worst[:10],'originalObjectTransformsPreserved':all(x['matrix']==current[x['name']]['matrix'] for x in before[0]),'leftSideMatchesOriginalWheel':current['Headlight_Left']['min'][0]>0 and current['Front_left_wheel']['min'][0]>0,'rightSideMatchesOriginalWheel':current['Headlight_Right']['max'][0]<0 and current['Front_right_wheel']['max'][0]<0,'texturesExist':all(os.path.isfile(p) for o in after[0] for ps in o['textures'].values() for p in ps),'objects':after[0]}
with open(os.path.join(OUT,'independent_export_validation.json' if export_mode else 'independent_source_validation.json'),'w') as f:json.dump(r,f,indent=2)
print({k:v for k,v in r.items() if k!='objects'})
