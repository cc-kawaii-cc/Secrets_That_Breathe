import bpy, os, json
from collections import defaultdict
OUT=os.path.dirname(os.path.abspath(__file__)); BASE=os.path.dirname(OUT)
bpy.ops.wm.open_mainfile(filepath=os.path.join(BASE,'PorscheV2_Inspection.blend'))
ob=bpy.data.objects['Body'];me=ob.data
with open(os.path.join(OUT,'uv_analysis.json')) as f:r=json.load(f)
islands=r['layers'][0]['islands']; fmap={i:j for j,c in enumerate(islands) for i in c['polygon_indices']}
seed=next(j for j,c in enumerate(islands) if c['faces']==47 and c['min'][0]>.5)
edgef=defaultdict(list)
for p in me.polygons:
    for li in p.loop_indices:edgef[me.loops[li].edge_index].append(p.index)
neighbors=defaultdict(list)
for ei,fs in edgef.items():
    for a in fs:
        if fmap.get(a)==seed:
            for b in fs:
                if fmap.get(b)!=seed:neighbors[fmap.get(b)].append((a,b,ei))
uv=me.uv_layers[0]
im=next(n.image for n in me.materials[0].node_tree.nodes if n.type=='TEX_IMAGE')
im.reload();pix=list(im.pixels);w,h=im.size
def col(pi):
    p=me.polygons[pi];u=sum(uv.data[li].uv.x for li in p.loop_indices)/len(p.loop_indices);v=sum(uv.data[li].uv.y for li in p.loop_indices)/len(p.loop_indices)
    x=max(0,min(w-1,int(u*w)));y=max(0,min(h-1,int(v*h)));off=4*(y*w+x);return pix[off:off+3]
print('SEED',seed)
for j,edges in neighbors.items():
    c=islands[j]
    print('NEIGHBOR',j,{k:v for k,v in c.items() if k!='polygon_indices'},'LINKS',edges,'COLS',[(i,col(i)) for i in c['polygon_indices']])
loc=[]
for p in me.polygons:
    ce=ob.matrix_world@p.center
    if .5<ce.x<.8 and -2<ce.y<-1.6 and .55<ce.z<.81: loc.append({'index':p.index,'island':fmap.get(p.index),'material':p.material_index,'center':list(ce),'color':col(p.index),'normal':list(ob.matrix_world.to_3x3()@p.normal),'verts':[list(ob.matrix_world@me.vertices[vi].co) for vi in p.vertices]})
with open(os.path.join(OUT,'local_faces.json'),'w') as f:json.dump(loc,f,indent=2)
with open(os.path.join(OUT,'seed_neighbors.json'),'w') as f:json.dump({'seed':seed,'neighbors':{j:edges for j,edges in neighbors.items()}},f,indent=2)
