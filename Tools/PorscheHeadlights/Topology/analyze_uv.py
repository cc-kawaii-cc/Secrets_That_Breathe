import bpy, json, os
from collections import defaultdict
BASE=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
bpy.ops.wm.open_mainfile(filepath=os.path.join(BASE,'PorscheV2_Inspection.blend'))
ob=bpy.data.objects['Body']; me=ob.data
world=[ob.matrix_world@v.co for v in me.vertices]
edge_faces=defaultdict(list)
for p in me.polygons:
    for li in p.loop_indices:
        edge_faces[me.loops[li].edge_index].append((p.index,li))
def bounds(ids):
    vs={v for i in ids for v in me.polygons[i].vertices}; pts=[world[v] for v in vs]
    return {'min':[min(p[k] for p in pts) for k in range(3)], 'max':[max(p[k] for p in pts) for k in range(3)]}
report={'materials':[],'images':[], 'layers':[]}
for m in me.materials:
    report['materials'].append({'name':m.name,'images':[(n.name,n.image.name,n.image.filepath) for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]})
for im in bpy.data.images: report['images'].append({'name':im.name,'path':im.filepath,'size':list(im.size)})
for uv in me.uv_layers:
    adj=defaultdict(set)
    def uvfor(pi,ei):
        p=me.polygons[pi]; lis=list(p.loop_indices)
        for n,li in enumerate(lis):
            if me.loops[li].edge_index==ei:
                lj=lis[(n+1)%len(lis)]
                return {me.loops[li].vertex_index:uv.data[li].uv.copy(),me.loops[lj].vertex_index:uv.data[lj].uv.copy()}
    seams=[]
    for ei, fs in edge_faces.items():
        if len(fs)!=2: continue
        a,b=fs[0][0],fs[1][0]; ua,ub=uvfor(a,ei),uvfor(b,ei)
        match=all((ua[vi]-ub[vi]).length<1e-5 for vi in ua)
        if match: adj[a].add(b); adj[b].add(a)
        else: seams.append(ei)
    remaining=set(range(len(me.polygons))); comps=[]
    while remaining:
        seed=remaining.pop(); todo=[seed]; ids=[seed]
        while todo:
            for n in adj[todo.pop()]:
                if n in remaining: remaining.remove(n); todo.append(n); ids.append(n)
        bb=bounds(ids)
        if bb['min'][1]<-1.3:
            pts=[uv.data[li].uv for i in ids for li in me.polygons[i].loop_indices]
            comps.append({'faces':len(ids),'polygon_indices':ids,**bb,'uvmin':[min(p[k] for p in pts) for k in range(2)],'uvmax':[max(p[k] for p in pts) for k in range(2)]})
    report['layers'].append({'name':uv.name,'seams':len(seams),'islands':sorted(comps,key=lambda r:-r['faces'])})
with open(os.path.join(BASE,'Topology','uv_analysis.json'),'w') as f: json.dump(report,f,indent=2)
print('MATERIALS',report['materials'])
for layer in report['layers']:
    print('LAYER',layer['name'],'SEAMS',layer['seams'])
    for c in layer['islands'][:25]: print({k:v for k,v in c.items() if k!='polygon_indices'})
