import bpy,bmesh,math,random,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/SecretsThatBreathe/Environment/RuralBushes';SRC=ROOT/'ArtSource/RuralBushes'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
configs=[('Khoi',.70,.82,.118,.59,12,12,9,False,71),('SiamWeed',.92,.82,.162,.72,11,11,8,True,83),('Lantana',.78,.92,.152,.70,12,12,8,True,91)]
def atlas(name):
    w,h=256,512;image=bpy.data.images.new(name+'_Atlas',width=w,height=h,alpha=False);pixels=[];rng=random.Random(900)
    for y in range(h):
        v=y/(h-1)
        for x in range(w):
            u=x/(w-1);noise=rng.uniform(-.028,.028)
            if u<.52:
                center=abs(u/.52-.5)*2;vein=max(0,1-abs(u/.52-.5)/.015)
                sidevein=max(0,1-abs(((v*9-center*1.8)%1)-.5)/.035)*(.3+.7*center)
                a=.92-.22*center+.10*math.sin(v*3)+noise+vein*.16+sidevein*.09
                rgb=(.19*a,.39*a,.095*a) if name=='Khoi' else (.24*a,.43*a,.12*a) if name=='SiamWeed' else (.22*a,.37*a,.095*a)
            elif u<.90:
                if name=='SiamWeed':rgb=(.83+noise,.77+noise,.88+noise)
                else:rgb=(.95+noise,.63+noise,.055+noise) if u<.70 else (.94+noise,.26+noise,.045+noise)
            else:rgb=(.22+noise,.145+noise,.07+noise)
            pixels.extend((*rgb,1))
    image.pixels.foreach_set(pixels);image.filepath_raw=str(OUT/'Textures'/f'{name}_Atlas.png');image.file_format='PNG';image.save()
    mat=bpy.data.materials.new(name+'_Foliage');mat.use_nodes=True;p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.86
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color']);return mat
reports={};allmodels=[]
for name,height,radius,leaflen,widthRatio,arms,shoots,pairs,flowers,seed in configs:
    material=atlas(name);rng=random.Random(seed);segments=[];leaves=[];heads=[]
    for arm in range(arms):
        az=arm*math.tau/arms+rng.uniform(-.2,.2)
        start=Vector((rng.uniform(-.05,.05),rng.uniform(-.05,.05),0))
        end=Vector((math.cos(az)*radius*.42,math.sin(az)*radius*.42,height*rng.uniform(.42,.66)))
        segments.append((start,end,.018 if name=='Khoi' else .013,.004))
        for shoot in range(shoots):
            # Branch tips fill a low, irregular domed crown from its skirt to top.
            t=rng.uniform(.20,.95);base=start.lerp(end,t)
            angle=az+rng.uniform(-.40,.40)
            crownT=(shoot+rng.uniform(.10,.90))/shoots
            lobe=1+.07*math.sin(angle*5+seed)+.04*math.sin(angle*9)
            reach=radius*math.sqrt(max(.02,1-crownT*crownT))*lobe
            tip=Vector((math.cos(angle)*reach,math.sin(angle)*reach,height*(.16+.80*crownT)))
            segments.append((base,tip,.004,.0011));direction=(tip-base).normalized()
            for j in range(pairs):
                anchor=base.lerp(tip,.42+j/(pairs-1)*.58)
                for side in [-1,1]:
                    leafAngle=angle+side*rng.uniform(.65,1.70)
                    outward=Vector((math.cos(leafAngle),math.sin(leafAngle),rng.uniform(-.10,.95))).normalized()
                    if name=='Khoi' and side==1:anchor=base.lerp(tip,.38+j/(pairs-1)*.60)
                    length=leaflen*rng.uniform(.78,1.23)
                    leaves.append((anchor,outward,length,length*widthRatio,rng.random()))
            if flowers and shoot%3==0 and rng.random()<.58:heads.append(tip)
    # Interior leafy sprays hide the radial scaffold from low viewing angles.
    for spray in range(30):
        angle=rng.uniform(0,math.tau);r=radius*rng.uniform(.12,.48)
        tip=Vector((math.cos(angle)*r,math.sin(angle)*r,height*rng.uniform(.24,.72)))
        base=segments[(spray%arms)*(shoots+1)][0].lerp(segments[(spray%arms)*(shoots+1)][1],.25)
        segments.append((base,tip,.0025,.0009))
        for j in range(4):
            anchor=base.lerp(tip,.48+j*.16)
            for side in [-1,1]:
                a=angle+side*rng.uniform(.65,1.5)
                d=Vector((math.cos(a),math.sin(a),rng.uniform(.1,.7))).normalized()
                length=leaflen*rng.uniform(.8,1.16)
                leaves.append((anchor,d,length,length*widthRatio,rng.random()))
    def create(lod):
        verts=[];faces=[];uvs=[]
        def face(points,texcoords):
            ids=[]
            for p,uv in zip(points,texcoords):ids.append(len(verts));verts.append((p.x,p.y,max(0,p.z)));uvs.append(uv)
            faces.append(ids)
        for segmentIndex,(a,b,r0,r1) in enumerate(segments):
            if lod==1 and r0<.01 and segmentIndex%3:continue
            n=(b-a).normalized();v=n.cross(Vector((0,0,1)))
            if v.length<.01:v=Vector((1,0,0))
            v.normalize();u=n.cross(v);sides=3
            for i in range(sides):
                p=math.tau*i/sides;q=math.tau*(i+1)/sides
                d=v*math.cos(p)+u*math.sin(p);e=v*math.cos(q)+u*math.sin(q)
                face([a+d*r0,a+e*r0,b+e*r1,b+d*r1],[(.94,0)]*4)
            # Both end caps are intentional solids.
            face([a+(v*math.cos(math.tau*i/sides)+u*math.sin(math.tau*i/sides))*r0 for i in reversed(range(sides))],[(.94,0)]*sides)
            face([b+(v*math.cos(math.tau*i/sides)+u*math.sin(math.tau*i/sides))*r1 for i in range(sides)],[(.94,0)]*sides)
        for idx,(anchor,d,length,width,phase) in enumerate(leaves):
            if lod==1 and idx%3:continue
            if lod==1:length*=1.24;width*=1.38
            n=d.cross(Vector((0,0,1))).normalized();normal=n.cross(d).normalized()
            # Six-point blade with a slightly raised central rib.
            pts=[anchor,anchor+d*length*.32+n*width*.48,anchor+d*length*.72+n*width*.36,anchor+d*length,anchor+d*length*.72-n*width*.36,anchor+d*length*.32-n*width*.48]
            center=anchor+d*length*.48+normal*length*.045
            tex=[(.26,0),(.012,.27),(.05,.58),(.26,.78),(.47,.58),(.508,.27)]
            # Two folded triangles per blade: spend the budget on canopy density.
            face([pts[0],pts[1],pts[3],pts[5]],[tex[0],tex[1],tex[3],tex[5]])
        for hi,p in enumerate(heads):
            if lod==1 and hi%2:continue
            for j in range(9 if lod==0 else 5):
                angle=j*2.39996;r=.007*math.sqrt(j);c=p+Vector((math.cos(angle)*r,math.sin(angle)*r,.01+.005*math.sin(j)))
                size=.008 if name=='Lantana' else .006
                colorU=.59 if j%3 else .79
                # Small flower corollas; detail belongs to silhouette rather than dense geometry.
                points=[c+Vector((math.cos(k*math.tau/5)*size,math.sin(k*math.tau/5)*size,0)) for k in range(5)]
                face(points,[(colorU,.90)]*5)
        me=bpy.data.meshes.new(f'{name}_LOD{lod}');me.from_pydata(verts,[],faces);me.update();ob=bpy.data.objects.new(f'Leaves_{name}_LOD{lod}',me);bpy.context.collection.objects.link(ob);me.materials.append(material)
        uv=me.uv_layers.new(name='FoliageUV');uv.active_render=True
        for loop in me.loops:uv.data[loop.index].uv=uvs[loop.vertex_index]
        bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update()
        for poly in me.polygons:assert poly.area>1e-12
        bpy.context.view_layer.objects.active=ob;ob.select_set(True)
        bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/f'{name}_LOD{lod}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
        ob.select_set(False);return ob
    bpy.ops.object.select_all(action='DESELECT');models=[create(0),create(1)]
    reports[name]={'LOD0_triangles':len(models[0].data.polygons),'LOD1_triangles':len(models[1].data.polygons),'dimensions_m':list(models[0].dimensions),'individual_leaves':len(leaves),'flower_heads':len(heads),'revision':'dense low dome, two triangles per leaf'}
    allmodels.extend(models);models[1].hide_render=True
    for o in models:o.location.x=(len(reports)-2)*2.0
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'RuralBushes.blend'))
(SRC/'geometry_report.json').write_text(json.dumps(reports,indent=2))
floor=bpy.data.materials.new('PreviewGround');floor.diffuse_color=(.13,.11,.07,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.007));bpy.context.object.data.materials.append(floor)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((-3,-5,7),650,5),((4,2,5),450,4)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;aim(o,(0,0,.7))
bpy.ops.object.camera_add(location=(3,-7,4));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=7;aim(cam,(0,0,.6))
scene=bpy.context.scene;scene.camera=cam;scene.world.color=(.25,.25,.25);scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1500;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
scene.render.filepath=str(SRC/'RuralBushes_Overview.png');bpy.ops.render.render(write_still=True)
cam.data.ortho_scale=2.2;scene.render.resolution_x=1000;scene.render.resolution_y=1000
for i,(name,*_) in enumerate(configs):
    x=(i-1)*2;cam.location=(x+1.6,-3,1.9);aim(cam,(x,0,.60));scene.render.filepath=str(SRC/f'{name}_Preview.png');bpy.ops.render.render(write_still=True)
