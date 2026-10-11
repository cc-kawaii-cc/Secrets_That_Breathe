"""Original, seeded mango tree geometry. Run with Blender --background --python.
Blender Z-up source; FBX exporter converts to Unity Y-up, metres.
Meshes are original; bark and leaf textures were supplied by the user.
"""
import bpy, math, random, json, os
import numpy as np
from mathutils import Vector
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/SecretsThatBreathe/Environment/MangoTree'
SOURCE = ROOT / 'ArtSource/MangoTree'
for p in (OUT/'Models', OUT/'Textures', SOURCE): p.mkdir(parents=True, exist_ok=True)
random.seed(27019)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def save_image(name, rgb, noncolor=False):
    h,w = rgb.shape[:2]
    im=bpy.data.images.new(name, width=w, height=h, alpha=True)
    if noncolor: im.colorspace_settings.name='Non-Color'
    rgba=np.ones((h,w,4),np.float32); rgba[:,:,:3]=np.clip(rgb,0,1)
    im.pixels.foreach_set(rgba.ravel()); im.filepath_raw=str(OUT/'Textures'/f'{name}.png')
    im.file_format='PNG'; im.save(); return im

# Supplied textures are used verbatim; rebuilding geometry never overwrites them.
bark_im=bpy.data.images.load(str(OUT/'Textures/Mango_Bark_BaseColor.png'))
bark_normal=bpy.data.images.load(str(OUT/'Textures/Mango_Bark_Normal.png'))
bark_normal.colorspace_settings.name='Non-Color'
leaf_im=bpy.data.images.load(str(OUT/'Textures/Mango_Leaves_BaseColor.png'))
fruit_im=bpy.data.images.load(str(OUT/'Textures/Mango_Fruit_BaseColor.png'))
# Fit UV strips inside all eight leaves of the supplied atlas.
# This changes mesh UVs only; source pixels are not edited or regenerated.
atlas=np.array(leaf_im.pixels[:],dtype=np.float32).reshape(1254,1254,4)[::-1]
LEAF_BOXES=[(35,375,580,72),(415,630,580,50),(670,1025,570,113),(1030,1250,590,62),
            (30,410,1175,655),(400,637,1180,665),(660,960,1170,680),(915,1253,1160,700)]
def leaf_uv(variant,t,side):
    x0,x1,ybase,ytip=LEAF_BOXES[variant]
    y=ybase+(ytip-ybase)*t
    row=atlas[int(round(y)),x0:x1,:3]
    mask=(row[:,1]>.015)&(row[:,1]>row[:,0]*1.02)
    indices=np.flatnonzero(mask)
    if len(indices)==0: raise RuntimeError(f'Empty leaf atlas row {variant}, {y}')
    # Use the longest contiguous span, ignoring isolated coloured edge pixels.
    spans=np.split(indices,np.where(np.diff(indices)>1)[0]+1)
    span=max(spans,key=len)
    left=x0+span[0]; right=x0+span[-1]
    middle=(left+right)*.5
    half=(right-left)*.40
    if t<.0001 or t>.9999: side=0
    return ((middle+side*half)/1254,1-y/1254)

def material(name, image, roughness, normal_image=None):
    m=bpy.data.materials.new(name); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Roughness'].default_value=roughness
    tex=m.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=image
    m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if normal_image:
        tex=m.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=normal_image
        n=m.node_tree.nodes.new('ShaderNodeNormalMap'); n.inputs['Strength'].default_value=.5
        m.node_tree.links.new(tex.outputs['Color'],n.inputs['Color']); m.node_tree.links.new(n.outputs['Normal'],bs.inputs['Normal'])
    return m
MATS=[material('Mango_Bark',bark_im,.91,bark_normal),material('Mango_Leaves',leaf_im,.4),material('Mango_Fruit',fruit_im,.4)]
MATS[1].node_tree.nodes.get('Principled BSDF').inputs['Subsurface Weight'].default_value=.06

branches=[]; leaves=[]; fruits=[]
def branch(points,radii,sides=9): branches.append((list(map(Vector,points)),radii,sides))
def polar(r,a,z): return Vector((r*math.cos(a),r*math.sin(a),z))

# Flared root collar, buttress roots, a crooked trunk and seven unequal scaffold limbs.
branch([(0,0,-.035),(.01,-.015,.12),(-.045,.02,.48),(-.02,.02,.89),(.07,.025,1.22),(.13,.02,1.57),(.12,.015,1.9)], [.29,.245,.20,.178,.144,.11,.06],16)
for i in range(7):
    a=i*math.tau/7+.14; end=polar(random.uniform(.50,.78),a,.008)
    branch([(0,0,.17),polar(.25,a,.075),end], [.14,.10,.012],9)

def rosette(origin, direction, size=1):
    direction=direction.normalized()
    for k in range(random.randint(10,14)):
        a=k*2.39996+random.uniform(-.25,.25)
        r=Vector((math.cos(a),math.sin(a),random.uniform(-.6,.27)))
        d=(r*.83+direction*.38).normalized()
        start=origin+direction*random.uniform(-.08,.10)
        length=random.uniform(.24,.38)*size
        width=length*random.uniform(.19,.255)
        variant=random.choices([0,1,2,3],[.37,.37,.23,.03])[0]
        leaves.append((start,d,length,width,random.uniform(-.75,.75),variant,random.uniform(.05,.16)))

for i in range(7):
    a=i*math.tau/7+.28+random.uniform(-.12,.12)
    base=Vector((-.02,.02,random.uniform(.78,1.15)))
    elbow=polar(random.uniform(.42,.64),a,random.uniform(1.62,1.94))
    crown=polar(random.uniform(.92,1.20),a,random.uniform(2.38,2.83))
    branch([base,base.lerp(elbow,.46),elbow,elbow.lerp(crown,.56),crown], [.145,.115,.086,.058,.035],12)
    for j in range(7):
        aj=a+(j-3)*.25+random.uniform(-.15,.15)
        # Dome: peripheral shoots lower, inner shoots higher.
        radius=random.uniform(1.42,2.02) if j%3 else random.uniform(.8,1.38)
        z=3.62-.27*radius**2+random.uniform(-.23,.27)
        target=polar(radius,aj,z)
        start=elbow.lerp(crown,random.uniform(.35,.92))
        mid=start.lerp(target,.54)+Vector((0,0,.11))
        branch([start,mid,target],[.045,.026,.011],8)
        for k in range(5):
            ak=aj+random.uniform(-.6,.6)
            spread=polar(random.uniform(.20,.47),ak,random.uniform(-.10,.48))
            tip=target+spread
            tstart=mid.lerp(target,random.uniform(.38,.94))
            tm=tstart.lerp(tip,.58)+Vector((0,0,.05))
            branch([tstart,tm,tip],[.013,.007,.002],6)
            rosette(tip,(tip-tm),random.uniform(.90,1.12))
            rosette(tm,tip-tm,.92)
            if random.random()<.12 and tip.z<3.25:
                stemend=tip+Vector((.015,-.015,-random.uniform(.18,.31)))
                branch([tip,stemend],[.0025,.0015],5)
                fruits.append((stemend,random.uniform(.20,.27),random.uniform(0,math.tau)))
# Fill the upper centre, keeping visible openings through the lower crown.
for i in range(5):
    a=i*2.4
    top=polar(random.uniform(.28,.70),a,random.uniform(3.48,3.83))
    base=Vector((.10,.02,1.65)); mid=base.lerp(top,.58)
    branch([base,mid,top],[.065,.030,.008],9)
    for k in range(7):
        end=top+polar(random.uniform(.15,.45),k*2.4,random.uniform(-.24,.18))
        branch([mid.lerp(top,.73),end],[.011,.002],6)
        rosette(end,end-mid); rosette(end.lerp(top,.6),end-mid)

# Keep every original leaf's centreline/length and the existing branch skeleton.
# Add leaves using a separate random stream so regeneration cannot change that tree.
original_leaf_count=len(leaves)
extra_rng=random.Random(31010)
for index in extra_rng.sample(range(original_leaf_count),round(original_leaf_count*.30)):
    p,d,length,width,twist,variant,droop=leaves[index]
    angle=extra_rng.choice([-1,1])*extra_rng.uniform(.35,.7)
    rotated=Vector((d.x*math.cos(angle)-d.y*math.sin(angle),d.x*math.sin(angle)+d.y*math.cos(angle),d.z))
    leaves.append((p.copy(),rotated,length,width,twist+.25,variant,droop))

class MeshBuilder:
    def __init__(self): self.v=[]; self.f=[]; self.uv=[]
    def vert(self,p,uv): self.v.append(tuple(p)); self.uv.append(uv); return len(self.v)-1
    def tri(self,a,b,c): self.f.append((a,b,c))
    def quad(self,a,b,c,d): self.tri(a,b,c); self.tri(a,c,d)
    def object(self,name,mat):
        me=bpy.data.meshes.new(name); me.from_pydata(self.v,[],self.f); me.update()
        layer=me.uv_layers.new(name='UVMap')
        for poly in me.polygons:
            poly.use_smooth=True
            for li in poly.loop_indices: layer.data[li].uv=self.uv[me.loops[li].vertex_index]
        ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob); me.materials.append(mat); return ob

def build_bark(lod):
    b=MeshBuilder()
    for ps,rs,ns in branches:
        if lod==2 and rs[0]<.014: continue
        rs=[r*(.85 if rs[0]>=.06 else 1.0) for r in rs]
        if rs[0]>.025 and lod<2:
            smooth_ps=[]; smooth_rs=[]
            for seg in range(len(ps)-1):
                a,bp,c,d=ps[max(0,seg-1)],ps[seg],ps[seg+1],ps[min(len(ps)-1,seg+2)]
                for sub in range(3-lod):
                    t=sub/(3-lod)
                    smooth_ps.append(.5*((2*bp)+(-a+c)*t+(2*a-5*bp+4*c-d)*t*t+(-a+3*bp-3*c+d)*t*t*t))
                    smooth_rs.append(rs[seg]*(1-t)+rs[seg+1]*t)
            ps=smooth_ps+[ps[-1]]; rs=smooth_rs+[rs[-1]]
        sides=max(4,ns-lod*2); rings=[]; along=0
        for j,(p,r) in enumerate(zip(ps,rs)):
            tangent=ps[min(j+1,len(ps)-1)]-ps[max(0,j-1)]
            t=tangent.normalized(); ref=Vector((0,1,0)) if abs(t.y)<.9 else Vector((1,0,0))
            u=t.cross(ref).normalized(); v=t.cross(u).normalized()
            if j: along+=(p-ps[j-1]).length
            ring=[]
            for k in range(sides+1):
                ang=k/sides*math.tau; irr=1+.07*math.sin(ang*5+j*.8)+.035*math.cos(ang*3-j)
                ring.append(b.vert(p+r*irr*(u*math.cos(ang)+v*math.sin(ang)),(k/sides,along/1.15)))
            rings.append(ring)
        for j in range(len(rings)-1):
            for k in range(sides): b.quad(rings[j][k],rings[j][k+1],rings[j+1][k+1],rings[j+1][k])
        for k in range(1,sides-1):
            b.tri(rings[0][0],rings[0][k+1],rings[0][k]); b.tri(rings[-1][0],rings[-1][k],rings[-1][k+1])
    return b.object(f'Bark_LOD{lod}',MATS[0])

def build_leaves(lod):
    b=MeshBuilder(); segments=[5,3,2][lod]; keep=[1,.59,.28][lod]
    for idx,(p,d,length,width,twist,variant,droop) in enumerate(leaves):
        if ((idx*0.61803398875)%1)>keep: continue
        size=[1,1.12,1.40][lod]; length*=size; width*=size*1.35
        side=d.cross(Vector((0,0,1))).normalized()
        if side.length<.1: side=Vector((1,0,0))
        n=side.cross(d).normalized(); side=(side*math.cos(twist)+n*math.sin(twist)).normalized()
        normal=side.cross(d).normalized(); rings=[]
        for j in range(segments+1):
            t=j/segments
            # Narrow pointed lanceolate leaf with a raised midrib, drooping tip.
            w=(math.sin(math.pi*t)**.85)*width*.5
            center=p+d*length*t+Vector((0,0,-droop*t*t))
            row=[]
            for s in [-1,0,1]:
                edge=normal*(-abs(s)*width*.13*math.sin(math.pi*t))
                edge+=normal*(s*width*.08*math.sin(t*math.pi*1.8+idx))
                row.append(b.vert(center+side*w*s+edge,leaf_uv(variant+4*(idx%2),t,s)))
            rings.append(row)
        for j in range(segments):
            for k in range(2):
                a,c=rings[j][k],rings[j+1][k+1]; bb,dd=rings[j][k+1],rings[j+1][k]
                if j==0: b.tri(a,c,dd)
                elif j==segments-1: b.tri(a,bb,c)
                else: b.quad(a,bb,c,dd)
    return b.object(f'Leaves_LOD{lod}',MATS[1])

def build_fruit(lod):
    b=MeshBuilder(); sides=[12,9,6][lod]; stacks=[10,7,5][lod]
    for p,length,angle in fruits:
        rings=[]
        for j in range(stacks+1):
            t=j/stacks; r=math.sin(math.pi*t)**.63*length*.26*(1+.06*math.sin(t*math.pi))
            center=p+Vector((length*.20*t*t,0,-length*t))
            ring=[]
            for k in range(sides+1):
                a=k/sides*math.tau+angle
                ring.append(b.vert(center+Vector((r*math.cos(a),r*.78*math.sin(a),0)),(k/sides,t)))
            rings.append(ring)
        for j in range(stacks):
            for k in range(sides): b.quad(rings[j][k],rings[j+1][k],rings[j+1][k+1],rings[j][k+1])
    return b.object(f'Fruit_LOD{lod}',MATS[2])

objects=[]; stats={'seed':27019,'leaves':len(leaves),'original_leaves':original_leaf_count,'leaf_width_multiplier':1.35,'existing_leaf_length_multiplier':1.0,'trunk_radius_multiplier':.85,'fruits':len(fruits),'lods':[]}
for lod in range(3):
    obs=[build_bark(lod),build_leaves(lod),build_fruit(lod)]
    objects.extend(obs)
    stats['lods'].append({'level':lod,'triangles':sum(len(o.data.polygons) for o in obs),'vertices':sum(len(o.data.vertices) for o in obs)})
    bpy.ops.object.select_all(action='DESELECT')
    for ob in obs: ob.select_set(True)
    bpy.context.view_layer.objects.active=obs[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/f'MangoTree_LOD{lod}.fbx'), use_selection=True, object_types={'MESH'}, use_mesh_modifiers=True, add_leaf_bones=False, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, bake_space_transform=True, path_mode='STRIP', mesh_smooth_type='OFF', use_tspace=True)
    if lod>0:
        for ob in obs: ob.hide_render=True; ob.hide_set(True)
stats['bounds_blender']={'min':list(map(float,np.min(np.array([v.co[:] for o in objects[:3] for v in o.data.vertices]),axis=0))), 'max':list(map(float,np.max(np.array([v.co[:] for o in objects[:3] for v in o.data.vertices]),axis=0)))}
(SOURCE/'geometry_report.json').write_text(json.dumps(stats,indent=2))

# Reusable studio preview, separate from exported model.
studio=bpy.data.collections.new('Preview Studio - not exported'); bpy.context.scene.collection.children.link(studio)
def to_studio(ob):
    for c in list(ob.users_collection): c.objects.unlink(ob)
    studio.objects.link(ob)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.052)); ground=bpy.context.object; to_studio(ground)
mat=bpy.data.materials.new('Studio warm grey'); mat.diffuse_color=(.22,.245,.23,1); ground.data.materials.append(mat)
def aim(ob,p): ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(7,-10,6.0)); cam=bpy.context.object; to_studio(cam); aim(cam,(0,0,2.04)); cam.data.type='ORTHO'; cam.data.ortho_scale=5.95; bpy.context.scene.camera=cam
for name,loc,power,size in [('Key',(0,-5,8),1700,5),('Fill',(-5,-1,5),1100,5),('Rim',(2,5,7),2100,4)]:
    bpy.ops.object.light_add(type='AREA',location=loc); ob=bpy.context.object; ob.name=name; ob.data.energy=power; ob.data.shape='DISK'; ob.data.size=size; aim(ob,(0,0,2)); to_studio(ob)
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.samples=32; sc.cycles.use_denoising=True
sc.world.color=(.3,.3,.3); sc.render.resolution_x=1400; sc.render.resolution_y=1400; sc.render.resolution_percentage=100
sc.view_settings.view_transform='AgX'; sc.render.image_settings.file_format='PNG'
for im in [bark_im,bark_normal,leaf_im,fruit_im]: im.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'MangoTree.blend'))
sc.render.filepath=str(SOURCE/'MangoTree_Preview.png'); bpy.ops.render.render(write_still=True)
cam.location=(4.8,-6,3.5); aim(cam,(.25,0,2.75)); cam.data.ortho_scale=2.15
sc.render.resolution_x=1200; sc.render.resolution_y=1200; sc.render.filepath=str(SOURCE/'MangoTree_Detail.png'); bpy.ops.render.render(write_still=True)
print('MANGO_COMPLETE',json.dumps(stats))
