"""Owned, repeatable Blender build. Execute in Blender 5.2 (MCP or CLI).
Coordinates: metres; open display XY, +Z front; hinge along Y at Z=3.05 mm.
Only IPhoneDuo-owned scene/data are replaced. Default/user scenes are preserved.
"""
import bpy, math, json, os, bmesh
from mathutils import Vector, Matrix
from pathlib import Path

ROOT = Path(r'D:\Unity\ปี3\Secrets_That_Breathe')
ART = ROOT / 'ArtSource/IPhoneDuo'
ASSET = ROOT / 'Assets/SecretsThatBreathe/Props/IPhoneDuo'
OWNER = 'IPhoneDuo'
ART.mkdir(parents=True, exist_ok=True)

def own(obj):
    obj['asset_owner'] = OWNER
    return obj

def prepare():
    old = bpy.data.scenes.get('IPhoneDuo_Studio')
    if old:
        for o in list(old.objects):
            if o.get('asset_owner') == OWNER:
                bpy.data.objects.remove(o, do_unlink=True)
        for c in list(old.collection.children):
            if c.get('asset_owner') == OWNER:
                bpy.data.collections.remove(c)
    else:
        old = bpy.data.scenes.new('IPhoneDuo_Studio')
    bpy.context.window.scene = old
    old.unit_settings.system = 'METRIC'
    old.unit_settings.scale_length = 1
    old.unit_settings.length_unit = 'MILLIMETERS'
    old.render.engine = 'CYCLES'
    old.cycles.samples = 48
    old.cycles.use_denoising = True
    old.render.image_settings.file_format = 'PNG'
    old.view_settings.view_transform = 'AgX'
    old.render.resolution_percentage = 100
    old.world = bpy.data.worlds.new('DUO_StudioWorld')
    old.world.use_nodes = True
    bg = next(n for n in old.world.node_tree.nodes if n.type == 'BACKGROUND')
    bg.inputs['Color'].default_value = (.19,.22,.28,1)
    bg.inputs['Strength'].default_value = .35
    return old

scene = prepare()
model = bpy.data.collections.new('IPhoneDuo_Model')
model['asset_owner'] = OWNER
scene.collection.children.link(model)
studio = bpy.data.collections.new('IPhoneDuo_StudioRig')
studio['asset_owner'] = OWNER
scene.collection.children.link(studio)

def link_obj(name, data=None, coll=model):
    o = own(bpy.data.objects.new(name, data))
    coll.objects.link(o)
    return o

def material(name, rgb, metallic=0, rough=.35, emission=0, texture=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    n = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    n.inputs['Base Color'].default_value = (*rgb,1)
    n.inputs['Metallic'].default_value = metallic
    n.inputs['Roughness'].default_value = rough
    n.inputs['Coat Weight'].default_value = .28
    n.inputs['Coat Roughness'].default_value = .12
    m.diffuse_color = (*rgb,1)
    if texture:
        path = ASSET / 'Textures' / texture
        if path.exists():
            for old in list(m.node_tree.nodes):
                if old.type=='TEX_IMAGE':m.node_tree.nodes.remove(old)
            t = m.node_tree.nodes.new('ShaderNodeTexImage')
            t.image = bpy.data.images.load(str(path), check_existing=True)
            m.node_tree.links.new(t.outputs['Color'], n.inputs['Base Color'])
            m.node_tree.links.new(t.outputs['Color'], n.inputs['Emission Color'])
    if emission:
        n.inputs['Emission Color'].default_value = (*rgb,1)
        n.inputs['Emission Strength'].default_value = emission
    return m

M = {}
M['metal'] = material('DUO_PolishedTitanium',(.56,.565,.53),1,.205)
M['white'] = material('DUO_CeramicWhite',(.82,.815,.77),.08,.24)
M['black'] = material('DUO_Graphite',(.009,.012,.016),.12,.29)
M['antenna'] = material('DUO_Antenna',(.32,.34,.33),.05,.48)
M['glass'] = material('DUO_LensGlass',(.018,.028,.045),.32,.115)
M['coating'] = material('DUO_LensCoating',(.024,.075,.12),.55,.16)
M['flash'] = material('DUO_Flash',(.76,.71,.56),.15,.24)
M['logo'] = material('DUO_Logo',(.59,.60,.56),.4,.3)
M['inner'] = material('DUO_InnerDisplay',(.28,.44,.58),.05,.32,.18,'Inner_Display.png')
M['cover'] = material('DUO_CoverDisplay',(.13,.23,.38),.05,.32,.18,'Cover_Display.png')

root = link_obj('IPhoneDuo')
root.empty_display_size = .018
root['nominal_open_mm'] = '164.6 x 117.8 x 5.2 (camera protrusion excluded)'
root['nominal_closed_mm'] = '84.1 x 117.8 x 11.3 (camera protrusion excluded)'
root['fold_degrees'] = 0.0
left = link_obj('LeftLeaf'); left.parent = root
hinge = link_obj('HingePivot'); hinge.parent = root
hinge.location.z = .00305
hinge.empty_display_size = .012
hinge['fold_axis'] = 'local Y, -180 degrees closes; 0 is fully open'
right = link_obj('RightLeaf'); right.parent = hinge
right.location.z = -.00305

def finish(o, mat, parent):
    if mat: o.data.materials.append(mat)
    o.parent = parent
    return o

def outline(w,h,r,segments=10):
    # CCW: bottom right, top right, top left, bottom left; independent corner radii.
    rr = [r]*4 if isinstance(r,(float,int)) else r
    pts=[]
    for (sx,sy,a),rad in zip([(1,-1,-90),(1,1,0),(-1,1,90),(-1,-1,180)],rr):
        cx=sx*(w/2-rad); cy=sy*(h/2-rad)
        for j in range(segments+1):
            t=math.radians(a+90*j/segments)
            pts.append((cx+rad*math.cos(t),cy+rad*math.sin(t)))
    return pts

def prism(name,w,h,d,r,loc,mat,parent=left,edge=.00012,seg=10):
    # Parallel offset rings give a real edge fillet without topology-heavy subdiv.
    edge=min(edge,d*.45)
    levels=[(-d/2,edge),(-d/2+edge*.293,edge*.293),(-d/2+edge,0),
            (d/2-edge,0),(d/2-edge*.293,edge*.293),(d/2,edge)] if edge>0 else [(-d/2,0),(d/2,0)]
    verts=[]
    radii=[r]*4 if isinstance(r,(float,int)) else r
    for z,inset in levels:
        verts.extend((x,y,z) for x,y in outline(w-2*inset,h-2*inset,[max(v-inset,.00001) for v in radii],seg))
    n=len(verts)//len(levels)
    faces=[tuple(range(n-1,-1,-1))]
    for k in range(len(levels)-1):
        for i in range(n):
            j=(i+1)%n
            faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i))
    faces.append(tuple((len(levels)-1)*n+i for i in range(n)))
    mesh=bpy.data.meshes.new(name+'_Mesh');mesh.from_pydata(verts,[],faces);mesh.update()
    o=link_obj(name,mesh);o.location=loc;finish(o,mat,parent)
    for f in mesh.polygons: f.use_smooth=len(f.vertices)==4
    uv=mesh.uv_layers.new(name='UV0')
    for f in mesh.polygons:
        for li in f.loop_indices:
            co=mesh.vertices[mesh.loops[li].vertex_index].co
            uv.data[li].uv=(co.x/w+.5,co.y/h+.5)
    return o

def disk(name,r,depth,loc,mat,parent=left,segments=48,rotation=None):
    # Closed cylindrical body, with two small edge-chamfer rings.
    e=min(.000065,depth*.25)
    levels=[(-depth/2,r-e),(-depth/2+e,r),(depth/2-e,r),(depth/2,r-e)]
    vs=[(rr*math.cos(i*2*math.pi/segments),rr*math.sin(i*2*math.pi/segments),z) for z,rr in levels for i in range(segments)]
    fs=[tuple(range(segments-1,-1,-1))]
    for k in range(3):
        fs.extend((k*segments+i,k*segments+(i+1)%segments,(k+1)*segments+(i+1)%segments,(k+1)*segments+i) for i in range(segments))
    fs.append(tuple(3*segments+i for i in range(segments)))
    me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(vs,[],fs);me.update()
    ob=link_obj(name,me);ob.location=loc;finish(ob,mat,parent)
    if rotation:ob.rotation_euler=rotation
    for f in me.polygons:f.use_smooth=len(f.vertices)==4
    uv=me.uv_layers.new(name='UV0')
    for f in me.polygons:
        for i in f.loop_indices:
            v=me.vertices[me.loops[i].vertex_index].co
            uv.data[i].uv=(v.x/(2*r)+.5,v.y/(2*r)+.5)
    return ob

def ring(name,r1,r2,depth,loc,mat,parent=left,segments=64):
    # Solid annulus; both its opening and inner wall are intentional.
    vs=[]
    e=min(depth*.2,.0001)
    profile=[(r1,-depth/2),(r2,-depth/2),(r2+e,-depth/2+e),
             (r2+e,depth/2-e),(r2,depth/2),(r1,depth/2),(r1-e,depth/2-e),(r1-e,-depth/2+e)]
    for rr,z in profile:
        vs.extend((rr*math.cos(i*2*math.pi/segments),rr*math.sin(i*2*math.pi/segments),z) for i in range(segments))
    fs=[]
    for k in range(len(profile)):
        kn=(k+1)%len(profile)
        fs.extend((k*segments+i,k*segments+(i+1)%segments,kn*segments+(i+1)%segments,kn*segments+i) for i in range(segments))
    me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(vs,[],fs);me.update()
    ob=link_obj(name,me);ob.location=loc;finish(ob,mat,parent)
    # Recalculate avoids assuming profile winding.
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(me);bm.free()
    for f in me.polygons:
        f.use_smooth=(f.index//segments not in (0,4))
    uv=me.uv_layers.new(name='UV0')
    for f in me.polygons:
        for li in f.loop_indices:
            v=me.vertices[me.loops[li].vertex_index].co
            uv.data[li].uv=(v.x/(2*r2)+.5,v.y/(2*r2)+.5)
    return ob

frames=[]
for label,cx,parent in [('Left',-.0414,left),('Right',.0414,right)]:
    radii=[.0008,.0008,.0062,.0062] if label=='Left' else [.0062,.0062,.0008,.0008]
    frame=prism(label+'_TitaniumFrame',.0818,.1178,.0052,radii,(cx,0,0),M['metal'],parent,edge=.00038,seg=12)
    frames.append(frame)
    prism(label+'_BackCeramic',.08055,.11655,.00016,[max(v-.00062,.00035) for v in radii],(cx,0,-.00256),M['white'],parent,edge=.000035,seg=12)
    prism(label+'_InnerBezel',.0808,.1168,.00013,[max(v-.0005,.0003) for v in radii],(cx,0,.002555),M['black'],parent,edge=.000025,seg=12)
    # One continuous artwork, split exactly at the fold in UV space.
    display=prism(label+'_InnerDisplay',.08095,.1138,.000035,[max(v-.00135,.0003) for v in radii],(cx+(.00075 if label=='Left' else -.00075),0,.002615),M['inner'],parent,edge=0,seg=14)
    for lp in display.data.uv_layers.active.data:
        lp.uv.x=(lp.uv.x*.5) + (0 if label=='Left' else .5)

# Flexible fold gasket is split between the leaves; crease remains visible when open.
for x,parent,side in [(-.00027,left,'Left'),(.00027,right,'Right')]:
    prism(side+'_FlexibleDisplayEdge',.00048,.114,.00007,.00015,(x,0,.00259),M['black'],parent,edge=.000015,seg=4)

# Exterior spine fills the fold corner without intersecting the opposing displays.
spine=disk('Hinge_Spine',.0018,.1150,(0,0,-.00035),M['metal'],left,48,(math.pi/2,0,0))
spine['assembly_note']='Static spine; pivot at front offset permits 0.835 mm glass clearance when closed.'
for y in [-.054,-.027,0,.027,.054]:
    disk('Hinge_Knuckle_'+str(y),.00181,.00065,(0,y,-.00035),M['antenna'],left,32,(math.pi/2,0,0))

# Interleaved end links physically connect the offset pivot to each leaf.
# Their Y positions differ, so the links can pass one another when folding.
for sign in [-1,1]:
    fixed=prism('Hinge_FixedLink_'+str(sign),.00125,.0046,.00048,.0005,(-.00065,sign*.05785,.00135),M['metal'],left,edge=.00009,seg=8)
    fixed.rotation_euler.x=math.pi/2
    moving=prism('Hinge_MovingLink_'+str(sign),.00125,.0046,.00048,.0005,(.00065,sign*.05845,.00135),M['metal'],right,edge=.00009,seg=8)
    moving.rotation_euler.x=math.pi/2
    disk('Hinge_EndPin_'+str(sign),.00078,.00118,(0,sign*.05815,.00305),M['metal'],left,32,(math.pi/2,0,0))

# Rear dual-camera island, rings, lenses, microphone and flash.
island=prism('RearCamera_CeramicIsland',.061,.027,.0012,.0105,(-.0475,.0392,-.00310),M['white'],left,edge=.00024,seg=14)
for index,x in enumerate([-.0665,-.0434],1):
    disk(f'Camera{index}_Seat',.01065,.00036,(x,.0392,-.00376),M['black'])
    ring(f'Camera{index}_TitaniumRim',.00925,.01020,.00155,(x,.0392,-.00430),M['metal'])
    disk(f'Camera{index}_FrontGlass',.0092,.00016,(x,.0392,-.00502),M['glass'])
    ring(f'Camera{index}_OpticBarrel',.00350,.00575,.00006,(x,.0392,-.005115),M['black'],segments=48)
    disk(f'Camera{index}_CoatedOptic',.00335,.000045,(x,.0392,-.00516),M['coating'])
    disk(f'Camera{index}_Aperture',.00160,.000025,(x,.0392,-.00519),M['glass'])
    ring(f'Camera{index}_OpticRim',.00270,.00283,.000025,(x,.0392,-.00520),M['coating'],segments=48)
disk('RearFlash_Rim',.00225,.00010,(-.0236,.0333,-.00379),M['metal'],segments=32)
disk('RearFlash_Diffuser',.00190,.00012,(-.0236,.0333,-.00387),M['flash'],segments=32)
disk('RearFlash_Core',.00092,.00003,(-.0236,.0333,-.00395),M['white'],segments=24)
mic=prism('RearMicrophone_Recess',.0025,.00115,.00010,.0005,(-.0236,.0444,-.00377),M['black'],edge=.00002,seg=6)
for i in range(6):
    disk('RearMicrophone_Grille_%02d'%i,.00009,.000035,(-.0244+i*.00032,.0444,-.00384),M['antenna'],segments=8)

# Outer display on the opposite leaf. Mirrored UV on the back provides readable text.
prism('CoverDisplay_BlackBezel',.0803,.1160,.00017,[.0008,.0008,.0053,.0053],(.0414,0,-.00257),M['black'],right,edge=.00004,seg=12)
cover=prism('CoverDisplay',.0769,.1122,.000035,[.00065,.00065,.0046,.0046],(.0414,0,-.002675),M['cover'],right,edge=0,seg=14)
for lp in cover.data.uv_layers.active.data: lp.uv.x=1-lp.uv.x
prism('CoverCamera_Pill',.0046,.011,.00008,.0022,(.0680,.0425,-.00273),M['black'],right,edge=.00002,seg=10)
disk('CoverCamera_Lens',.00125,.00003,(.0680,.0443,-.002795),M['coating'],right,32)
disk('CoverCamera_Indicator',.00030,.00002,(.0680,.0400,-.00280),M['antenna'],right,16)

# Vector silhouette of the inset rear emblem, with a separate leaf.
def emblem(name,points,loc,scale):
    # Intentional shallow solid inlay rather than a floating decal plane.
    # Closed Catmull-Rom outline removes visible corners at inspection distance.
    smooth=[]
    for i,p1 in enumerate(points):
        p0=Vector(points[(i-1)%len(points)]);p1=Vector(p1)
        p2=Vector(points[(i+1)%len(points)]);p3=Vector(points[(i+2)%len(points)])
        for step in range(5):
            t=step/5
            p=.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t)
            smooth.append(tuple(p))
    points=smooth
    vs=[(x*scale,y*scale,z) for z in [-.000012,.000012] for x,y in points]
    n=len(points);fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
    fs.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(vs,[],fs);me.update()
    ob=link_obj(name,me);ob.location=loc;finish(ob,M['logo'],left)
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(me);bm.free()
    uv=me.uv_layers.new(name='UV0')
    for f in me.polygons:
        for li in f.loop_indices:
            v=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(v.x/(scale*3)+.5,v.y/(scale*3)+.5)
    return ob
fruit=[(-.06,.73),(-.35,.89),(-.68,.86),(-.96,.65),(-1.08,.3),(-1.05,-.13),(-.91,-.57),(-.69,-.92),(-.45,-1.08),(-.17,-1.01),(.06,-.97),(.32,-1.07),(.56,-1.02),(.78,-.79),(.99,-.37),(.75,-.25),(.59,-.06),(.56,.18),(.63,.39),(.84,.57),(.61,.79),(.33,.88),(.10,.82)]
emblem('Rear_Emblem',fruit,(-.0414,-.009,-.002655),.0072)
emblem('Rear_EmblemLeaf',[(-.05,.95),(-.02,1.21),(.13,1.45),(.43,1.59),(.42,1.32),(.27,1.08)],(-.0414,-.009,-.002658),.0072)

# Match the reference's handedness: rear cameras are on the moving leaf and
# the outer cover screen on the fixed leaf. Reflect positions, never object scales.
for o in list(model.objects):
    if o.type!='MESH':continue
    if o.name.startswith(('RearCamera','Camera1_','Camera2_','RearFlash','RearMicrophone','Rear_Emblem')):
        o.location.x=-o.location.x;o.parent=right
        if o.name.startswith('Rear_Emblem'):
            for v in o.data.vertices:v.co.x=-v.co.x
            bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
    elif o.name.startswith(('CoverDisplay','CoverCamera')):
        o.location.x=-o.location.x;o.parent=left

# Side controls sit on small gasket seats and remain part of their own leaf.
for idx,(cx,cy,length,parent) in enumerate([(-.08230,.005,.016,left),(-.061,.0589,.011,left),(-.047,.0589,.011,left),(.08230,.010,.014,right)]):
    side=(abs(cx)>.08)
    if side:
        seat=prism('Control_%d_Gasket'%idx,.00045,length+.0006,.0020,.0002,(cx,cy,0),M['black'],parent,edge=.00008,seg=6)
        sign=1 if cx>0 else -1
        button=prism('Control_%d_Titanium'%idx,.00070,length,.00170,.0003,(cx+sign*.00018,cy,0),M['metal'],parent,edge=.00014,seg=8)
    else:
        seat=prism('Control_%d_Gasket'%idx,length+.0006,.00045,.0020,.0002,(cx,cy,0),M['black'],parent,edge=.00008,seg=6)
        button=prism('Control_%d_Titanium'%idx,length,.0007,.00170,.0003,(cx,cy+.00018,0),M['metal'],parent,edge=.00014,seg=8)

# Real speaker and USB-C cavities: boolean through the frame edge, interior at depth.
def apply_cutters(frame, cutters):
    bpy.ops.object.select_all(action='DESELECT')
    for c in cutters: c.select_set(True)
    bpy.context.view_layer.objects.active=cutters[0]
    bpy.ops.object.join()
    cut=bpy.context.object
    bpy.context.view_layer.objects.active=frame
    mod=frame.modifiers.new('MachinedPorts','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cut
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cut,do_unlink=True)
    # All new boolean faces receive a planar UV if the inherited UV layer is absent.
    bm=bmesh.new();bm.from_mesh(frame.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(frame.data);bm.free()

for label,frame,cx,parent in [('Left',frames[0],-.0414,left),('Right',frames[1],.0414,right)]:
    cutters=[]
    for edge_y in [-.05875,.05875]:
        # top and bottom sound channels; avoids overlap with the USB socket.
        xs=[cx-.014+i*.0018 for i in range(6)] if edge_y>0 else [cx+.016+i*.0018 for i in range(6)]
        for idx,x in enumerate(xs):
            c=disk('CUT_Sound',.00048,.0025,(x,edge_y,0),None,parent,16,(math.pi/2,0,0));cutters.append(c)
            inside_y=edge_y-(.00070 if edge_y>0 else -.00070)
            disk(label+'_SoundPocket_'+str(edge_y)+'_'+str(idx),.00044,.00012,(x,inside_y,0),M['black'],parent,16,(math.pi/2,0,0))
    if label=='Right':
        cut=prism('CUT_USBC',.0090,.00245,.0030,.0010,(cx,-.0586,0),None,parent,edge=0,seg=8)
        cut.rotation_euler.x=math.pi/2;cutters.append(cut)
        port=prism('USBC_RecessLiner',.0087,.00222,.0007,.00085,(cx,-.05785,0),M['black'],parent,edge=.00010,seg=8)
        port.rotation_euler.x=math.pi/2
        tongue=prism('USBC_ContactTongue',.0061,.00045,.00065,.00017,(cx,-.0583,0),M['antenna'],parent,edge=.00006,seg=6)
        tongue.rotation_euler.x=math.pi/2
    apply_cutters(frame,cutters)
    normals=frame.modifiers.new('MachinedFrameWeightedNormals','WEIGHTED_NORMAL')
    normals.keep_sharp=True
    normals.weight=50
    # Flush dielectric antenna bands, each within 0.05 mm of the metal edge.
    sign=-1 if label=='Left' else 1
    for yi in [-.041,.041]:
        prism(label+'_AntennaSide_'+str(yi),.000055,.00065,.0041,.000025,(sign*.082305,yi,0),M['antenna'],parent,edge=.000012,seg=3)
    for yi in [-.058905,.058905]:
        prism(label+'_AntennaEnd_'+str(yi),.00065,.000055,.0041,.000025,(cx+sign*.025,yi,0),M['antenna'],parent,edge=.000012,seg=3)

def consolidate_details():
    # Keep mechanical leaves and visually useful names; merge tiny repeated grilles per leaf.
    for parent in [left,right]:
        obs=[o for o in model.objects if o.type=='MESH' and o.parent==parent and ('SoundPocket' in o.name or 'Antenna' in o.name or 'Microphone_Grille' in o.name)]
        bpy.ops.object.select_all(action='DESELECT')
        for o in obs:o.select_set(True)
        if obs:
            bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join()
            bpy.context.object.name=('Left' if parent==left else 'Right')+'_AcousticAndAntennaDetails'
consolidate_details()

# Ground, lights and cameras stay outside the export collection.
ground=prism('DUO_StudioGround',200,200,.005,.1,(0,0,-.014),None,None,edge=0,seg=1)
model.objects.unlink(ground);studio.objects.link(ground)
ground.hide_set(True)
ground.data.materials.append(material('DUO_StudioGroundMat',(.055,.072,.095),.1,.37))

def aim(o,point):o.rotation_euler=(Vector(point)-o.location).to_track_quat('-Z','Y').to_euler()
def area(name,loc,power,size,color,target=(0,0,0)):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
    o=link_obj(name,d,studio);o.location=loc;aim(o,target);return o
area('DUO_Key_Softbox',(-.12,.13,.23),.60,.17,(.91,.96,1))
area('DUO_Fill_Strip',(.17,-.02,.13),.36,.12,(.80,.89,1))
area('DUO_Rim_Strip',(-.12,-.09,.08),.48,.10,(1,.88,.69))
area('DUO_Overhead',(.02,.12,.28),.24,.18,(1,1,1))
camera=link_obj('DUO_PreviewCamera',bpy.data.cameras.new('DUO_PreviewCamera'),studio)
camera.data.type='ORTHO';camera.data.ortho_scale=.225;camera.data.lens=55
camera.location=(.15,-.19,.26)
direction=(-camera.location).normalized();cam_right=direction.cross(Vector((0,1,0))).normalized();cam_up=cam_right.cross(direction).normalized()
camera.rotation_euler=Matrix((cam_right,cam_up,-direction)).transposed().to_quaternion().to_euler()
scene.camera=camera
scene.render.resolution_x=1500;scene.render.resolution_y=1200
scene.frame_start=1;scene.frame_end=60

# Stable selections and view clipping for a true-size handheld prop.
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True);bpy.context.view_layer.objects.active=root
for screen in bpy.data.screens:
    for ar in screen.areas:
        if ar.type=='VIEW_3D':
            ar.spaces.active.clip_start=.0001;ar.spaces.active.clip_end=100
            ar.spaces.active.region_3d.view_distance=.27
            ar.spaces.active.region_3d.view_location=(0,0,0)

def export():
    hinge.rotation_euler=(0,0,0);bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT')
    for o in model.objects:o.select_set(True)
    bpy.context.view_layer.objects.active=root
    dest=ASSET/'Models/IPhoneDuo.fbx';dest.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,object_types={'MESH','EMPTY'},
        global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',axis_up='Y',use_space_transform=True,bake_space_transform=False,
        use_mesh_modifiers=True,mesh_smooth_type='FACE',use_triangles=True,
        add_leaf_bones=False,bake_anim=False,path_mode='STRIP',use_custom_props=True)
    return str(dest)

def optimize_for_game():
    # Mechanical leaves stay separate. Keep main frame names; combine other
    # rigid details by leaf/material to avoid one renderer per tiny optic.
    for parent in [left,right]:
        groups={}
        for o in list(model.objects):
            if o.type!='MESH' or o.parent!=parent or o in frames:continue
            key=tuple(m.name for m in o.data.materials)
            groups.setdefault(key,[]).append(o)
        for key,obs in groups.items():
            if len(obs)<2:continue
            bpy.ops.object.select_all(action='DESELECT')
            for o in obs:o.select_set(True)
            bpy.context.view_layer.objects.active=obs[0]
            bpy.ops.object.join()
            bpy.context.object.name=parent.name+'_'+key[0].removeprefix('DUO_')

def validate():
    objects=[o for o in model.objects if o.type=='MESH']
    tris=0;issues=[];stats=[]
    for o in objects:
        me=o.data;me.calc_loop_triangles();nt=len(me.loop_triangles);tris+=nt
        bm=bmesh.new();bm.from_mesh(me)
        bad=sum(1 for e in bm.edges if not e.is_manifold)
        deg=sum(1 for f in bm.faces if f.calc_area()<1e-14)
        volume=bm.calc_volume(signed=True)
        bm.free()
        if bad or deg or volume<=0:issues.append({'object':o.name,'non_manifold':bad,'degenerate':deg,'signed_volume':volume})
        stats.append({'name':o.name,'triangles':nt,'uv0':len(me.uv_layers)>0,'parent':o.parent.name})
    report={'asset':'IPhoneDuo','blender_version':bpy.app.version_string,'source_mesh_objects':source_mesh_objects,'mesh_objects':len(objects),'triangles_after_triangulation':tris,
        'materials':[m.name for m in M.values()],'topology_issues':issues,'objects':stats,
        'dimensions_source':'User reference sheet; camera bump/buttons exceed nominal chassis envelope',
        'open_chassis_m':[.1646,.1178,.0052],'closed_chassis_m':[.0841,.1178,.0113],
        'fold_axis_blender':'local Y negative 0..180 degrees','hinge_pivot_blender_m':[0,0,.00305],
        'closed_inner_glass_clearance_m':2*(.00305-.0026325),
        'uv_note':'Screen UV0 intentionally shares the complete lock-screen artwork across halves; other surfaces planar UV0 for solid material. Not a unique baked atlas or lightmap UV.',
        'texture_files':['Inner_Display.png','Cover_Display.png'],
        'scope':'Game inspection prop. No preexisting gameplay interaction replaced.'}
    (ART/'Blender_Validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k!='objects'},indent=2))

for img in bpy.data.images:
    if img.source=='FILE' and img.filepath and 'IPhoneDuo' in img.filepath:
        img.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'IPhoneDuo.blend'))
source_mesh_objects=sum(o.type=='MESH' for o in model.objects)
optimize_for_game()
export()
validate()
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active=root
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'IPhoneDuo_GameReady.blend'))
print('IPhoneDuo build/export/source saved.')
