"""Original deterministic star-gooseberry model. Run with Blender 5.2."""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/SecretsThatBreathe/Environment/StarGooseberryTree'
SOURCE=ROOT/'ArtSource/StarGooseberryTree'
SOURCE.mkdir(parents=True,exist_ok=True)
random.seed(110326)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def texture_mat(name,file,normal=None,alpha=False):
    m=bpy.data.materials.new(name); m.use_nodes=True
    nodes=m.node_tree.nodes; links=m.node_tree.links
    p=nodes.get('Principled BSDF'); p.inputs['Roughness'].default_value=.72 if not alpha else .55
    t=nodes.new('ShaderNodeTexImage'); t.image=bpy.data.images.load(str(OUT/'Textures'/file))
    links.new(t.outputs['Color'],p.inputs['Base Color'])
    if alpha:
        links.new(t.outputs['Alpha'],p.inputs['Alpha']); m.surface_render_method='DITHERED'
        m.use_transparency_overlap=False
        p.inputs['Subsurface Weight'].default_value=.045
    if normal:
        n=nodes.new('ShaderNodeTexImage'); n.image=bpy.data.images.load(str(OUT/'Textures'/normal)); n.image.colorspace_settings.name='Non-Color'
        nm=nodes.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value=.55
        links.new(n.outputs['Color'],nm.inputs['Color']); links.new(nm.outputs['Normal'],p.inputs['Normal'])
    return m

bark=texture_mat('Mayom_Bark','Mayom_Bark_BaseColor.png','Mayom_Bark_Normal.png')
foliage=texture_mat('Mayom_Leaves','Mayom_Leaves_Atlas.png',alpha=True)
fruitmat=bpy.data.materials.new('Mayom_Fruit'); fruitmat.use_nodes=True
p=fruitmat.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(.72,.79,.18,1)
p.inputs['Roughness'].default_value=.37; p.inputs['Subsurface Weight'].default_value=.08
branches=[]; leaves=[]; fruits=[]; clusters=[]
def V(x): return Vector(x)
def polar(r,a,z): return V((r*math.cos(a),r*math.sin(a),z))
def branch(points,radii,sides): branches.append(([V(p) for p in points],radii,sides))

# Tall irregular trunk and upward open forks, modelled in metres.
trunk=[(0,0,0),(.025,-.015,.25),(-.035,.025,.85),(.03,0,1.4),(.09,-.025,2.1),(.015,.055,3.05),(.10,.02,4.0),(.04,-.02,4.85)]
branch(trunk,[.18,.147,.123,.098,.071,.05,.025,.004],10)
for i in range(4):
    a=i*math.tau/4+.3
    branch([(0,0,.1),polar(.38,a,.025)],[.085,.003],5)

scaffolds=[]
for i in range(8):
    a=i*2.399963+.3
    base=V(trunk[3]).lerp(V(trunk[4]),(i%3)/3)
    h=3.1+(i%4)*.48+random.uniform(-.15,.15)
    reach=1.45+(.25 if i%2 else 0)
    pts=[base,polar(.32,a,base.z+.43),polar(.75,a+.08,2.48+(i%3)*.18),polar(1.17,a+.12,h-.43),polar(reach,a+.16,h)]
    branch(pts,[.075,.061,.044,.026,.009],6)
    scaffolds.append(pts)
    for j in range(3):
        start=pts[2].lerp(pts[4],.25+j*.27)
        sa=a+(-.48 if j%2 else .48)+random.uniform(-.13,.13)
        tip=polar(1.7+j*.19,sa,h-.60+j*.32)
        mid=start.lerp(tip,.55)+V((0,0,.10))
        branch([start,mid,tip],[.022,.013,.003],4)
        # Alpha shoots carry the many alternating leaves efficiently.
        for k in range(9):
            origin=start.lerp(tip,.10+(k//3)*.36)+polar(.03,sa,0)
            if k==8: origin=pts[4]
            angle=sa+(k%3-1)*.80+random.uniform(-.22,.22)
            length=random.uniform(.85,1.22)
            direction=polar(1,angle,random.uniform(-.32,.32)).normalized()
            leaves.append((origin,direction,length,length*.43,random.uniform(-.9,.9),random.choices([0,1],[8,2])[0]))

for i in range(4):
    origin=V(trunk[-2]).lerp(V(trunk[-1]),.3+i*.23)
    direction=polar(1,i*2.399963,.40).normalized()
    leaves.append((origin,direction,.88,.38,i*.3,0))

# Trade six overlapping shoots for the small fruit-bearing peduncles.
del leaves[:6]

# Fruit is cauliflorous: short, crowded bunches on older trunk/branches.
for ci,count in enumerate([8,9,10,11,10]):
    a=ci*2.399963-.6
    limb=scaffolds[ci%8]
    anchor=limb[1].lerp(limb[2],.25+(ci%3)*.25)
    anchor+=polar(.028,a,0)
    branch([anchor,anchor+V((0,0,-.22))],[.0035,.0013],3)
    clusters.append({'id':ci,'fruit_count':count,'anchor':list(anchor)})
    for n in range(count):
        angle=n*2.399963+a
        # Small overlapping lobed fruit, in close bunches attached to old wood.
        center=anchor+polar(.036+(n%3)*.013,angle,-.045-(n//3)*.063+random.uniform(-.014,.014))
        radius=random.uniform(.037,.047)
        fruits.append((center,radius,angle))

class MeshBuilder:
    def __init__(self): self.vertices=[]; self.faces=[]; self.uvs=[]
    def vertex(self,p,uv=(0,0)):
        self.vertices.append(tuple(p)); self.uvs.append(uv); return len(self.vertices)-1
    def tri(self,a,b,c): self.faces.append((a,b,c))
    def quad(self,a,b,c,d): self.tri(a,b,c); self.tri(a,c,d)
    def object(self,name,mat):
        mesh=bpy.data.meshes.new(name); mesh.from_pydata(self.vertices,[],self.faces); mesh.update()
        uv=mesh.uv_layers.new(name='UVMap')
        for p in mesh.polygons:
            p.use_smooth=True
            for li in p.loop_indices: uv.data[li].uv=self.uvs[mesh.loops[li].vertex_index]
        obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj); mesh.materials.append(mat)
        return obj

def wood(lod):
    b=MeshBuilder()
    for pts,radii,sides in branches:
        if lod==2 and radii[0]<.008: continue
        sides=max(3,sides-lod*2); rings=[]; distance=0
        for i,(point,r) in enumerate(zip(pts,radii)):
            tangent=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized()
            ref=V((0,1,0)) if abs(tangent.y)<.9 else V((1,0,0))
            u=tangent.cross(ref).normalized(); v=tangent.cross(u).normalized()
            if i: distance+=(point-pts[i-1]).length
            ring=[]
            for s in range(sides+1):
                a=s/sides*math.tau; r2=r*(1+.035*math.sin(a*3+i))
                ring.append(b.vertex(point+r2*(u*math.cos(a)+v*math.sin(a)),(s/sides,distance/.6)))
            rings.append(ring)
        for i in range(len(rings)-1):
            for s in range(sides): b.quad(rings[i][s],rings[i][s+1],rings[i+1][s+1],rings[i+1][s])
        if radii[0]>.03:
            for s in range(1,sides-1):
                b.tri(rings[0][0],rings[0][s+1],rings[0][s]); b.tri(rings[-1][0],rings[-1][s],rings[-1][s+1])
    return b.object('Bark_LOD'+str(lod),bark)

def leafmesh(lod):
    b=MeshBuilder(); keep=[1,.58,.27][lod]
    for i,(origin,direction,length,width,twist,variant) in enumerate(leaves):
        if (i*.61803398875)%1>keep: continue
        scale=[1,1.13,1.38][lod]; length*=scale; width*=scale
        side=direction.cross(V((0,0,1))).normalized(); normal=side.cross(direction).normalized()
        side=side*math.cos(twist)+normal*math.sin(twist)
        col=variant%2; strips=[]; segments=[3,2,1][lod]
        for s in range(segments+1):
            t=s/segments; center=origin+direction*length*t-V((0,0,.20*t*t))
            strips.append([b.vertex(center-side*width*.5,(col*.5+.006,.006+.988*t)),b.vertex(center+side*width*.5,(col*.5+.494,.006+.988*t))])
        for s in range(segments): b.quad(strips[s][0],strips[s][1],strips[s+1][1],strips[s+1][0])
    return b.object('Leaves_LOD'+str(lod),foliage)

def fruitmesh(lod):
    b=MeshBuilder(); sides=[12,12,6][lod]
    for ci,(center,r,rotation) in enumerate(fruits):
        if lod==2 and ci%2: continue
        rings=[]
        latitudes=[math.pi/5,math.pi*2/5,math.pi*3/5,math.pi*4/5] if lod==0 else [math.pi/3,math.pi*2/3]
        for lat in latitudes:
            ring=[]
            for s in range(sides):
                a=s/sides*math.tau+rotation
                # Six deep longitudinal grooves; yellow lobes are geometry, not painted circles.
                rr=r*math.sin(lat)*(1+.15*math.cos(6*(a-rotation)))
                ring.append(b.vertex(center+V((rr*math.cos(a),rr*math.sin(a),r*.77*math.cos(lat))),(s/sides,lat/math.pi)))
            rings.append(ring)
        top=b.vertex(center+V((0,0,r*.65)),(.5,0)); bottom=b.vertex(center-V((0,0,r*.65)),(.5,1))
        for s in range(sides):
            nxt=(s+1)%sides
            b.tri(top,rings[0][s],rings[0][nxt])
            for ri in range(len(rings)-1): b.quad(rings[ri][s],rings[ri+1][s],rings[ri+1][nxt],rings[ri][nxt])
            b.tri(bottom,rings[-1][nxt],rings[-1][s])
    return b.object('Fruit_LOD'+str(lod),fruitmat)

all_objects=[]; stats={'species':'Phyllanthus acidus','leafy_shoot_cards':len(leaves),'approx_visible_leaves':len(leaves)*25,'fruit_count':len(fruits),'clusters':clusters,'lods':[]}
for lod in range(3):
    objs=[wood(lod),leafmesh(lod),fruitmesh(lod)]; all_objects+=objs
    stats['lods'].append({'level':lod,'triangles':sum(len(o.data.polygons) for o in objs),'parts':{o.name:len(o.data.polygons) for o in objs}})
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/f'StarGooseberryTree_LOD{lod}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,path_mode='STRIP',mesh_smooth_type='OFF',use_tspace=True,add_leaf_bones=False)
    if lod>0:
        for o in objs: o.hide_render=True; o.hide_set(True)
assert 5000<=stats['lods'][0]['triangles']<=7000,stats
verts=[v.co for o in all_objects[:3] for v in o.data.vertices]
stats['bounds_blender']={'min':[min(v[i] for v in verts) for i in range(3)],'max':[max(v[i] for v in verts) for i in range(3)]}
(SOURCE/'geometry_report.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')

def aim(o,p): o.rotation_euler=(V(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.015))
ground=bpy.context.object; ground.name='Preview Ground (not exported)'
gm=bpy.data.materials.new('Studio Ground'); gm.diffuse_color=(.18,.21,.17,1); ground.data.materials.append(gm)
bpy.ops.object.camera_add(location=(8,-11,6.0)); camera=bpy.context.object; aim(camera,(0,0,2.55))
camera.data.type='ORTHO'; camera.data.ortho_scale=5.95; bpy.context.scene.camera=camera
for loc,power,size in [((2,-6,10),1700,6),((-6,-1,6),1100,5),((3,6,8),2100,5)]:
    bpy.ops.object.light_add(type='AREA',location=loc); light=bpy.context.object; light.data.energy=power; light.data.size=size; aim(light,(0,0,3))
scene=bpy.context.scene; scene.render.engine='BLENDER_EEVEE'; scene.world.color=(.10,.12,.10)
scene.render.resolution_x=1400; scene.render.resolution_y=1500; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.view_settings.look='AgX - Medium High Contrast'
for image in bpy.data.images:
    if image.source=='FILE': image.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'StarGooseberryTree.blend'))
scene.render.filepath=str(SOURCE/'StarGooseberryTree_Preview.png'); bpy.ops.render.render(write_still=True)
camera.location=(3.2,-4.6,3.4); aim(camera,tuple(V(clusters[2]['anchor'])+V((0,0,-.05))))
camera.data.ortho_scale=1.6; scene.render.resolution_x=1200; scene.render.resolution_y=1200
scene.render.filepath=str(SOURCE/'StarGooseberryTree_Detail.png'); bpy.ops.render.render(write_still=True)
print('MAYOM_COMPLETE',json.dumps(stats))
