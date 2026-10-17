"""Low-poly double office door for the existing Lawyer Room blockout.
Blender metres: X width, Z height, -Y room-facing front; centered old door origin.
Standalone Blender CLI source; does not open or modify a live Blender session.
"""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

ROOT=Path(r'D:\Unity\ปี3\Secrets_That_Breathe')
ART=ROOT/'ArtSource/LawyerMainDoor'
ASSET=ROOT/'Assets/SecretsThatBreathe/Props/LawyerMainDoor'
ART.mkdir(parents=True,exist_ok=True)
OWNER='LawyerMainDoor'

# The CLI starts an isolated Blender, so its startup objects are disposable.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.name='LawyerMainDoor_Studio'
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
model=bpy.data.collections.new('LM_Export');scene.collection.children.link(model)
studio=bpy.data.collections.new('LM_Studio');scene.collection.children.link(studio)

def relocate(o,coll):
    for c in list(o.users_collection):c.objects.unlink(o)
    coll.objects.link(o);o['asset_owner']=OWNER
    return o
def empty(name,loc,parent=None):
    o=bpy.data.objects.new(name,None);model.objects.link(o);o.location=loc;o.empty_display_size=.08;o['asset_owner']=OWNER
    if parent:o.parent=parent
    return o
root=empty('LawyerMainDoor',(0,0,0))
root['opening_metres']='2.0 wide x 2.5 high, centred at original Main Door'
root['design']='Opaque double walnut office door; static visual replacement; original game object/collider retained.'
fixed=empty('Frame_Fixed',(0,0,0),root)
left=empty('LeftLeaf_Pivot',(-.988,0,0),root)
right=empty('RightLeaf_Pivot',(.988,0,0),root)
empty('LM_AxisTop',(0,0,1),root)
empty('LM_AxisFront',(0,-1,0),root)

def mat(name,color,metal=0,rough=.7,texture=None):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    if texture:
        t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(ASSET/'Textures'/texture),check_existing=True)
        m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
    return m
M={
    'wood':mat('LM_Wood',(.18,.12,.075),rough=.78,texture='Door_Wood_BaseColor.png'),
    'inset':mat('LM_PanelInset',(.12,.075,.04),rough=.86,texture='Door_Wood_BaseColor.png'),
    'brass':mat('LM_Brass',(.27,.20,.095),metal=.72,rough=.49),
    'dark':mat('LM_DarkMetal',(.035,.038,.034),metal=.35,rough=.7),
    'plaque':mat('LM_Plaque',(.13,.11,.07),metal=.35,rough=.64,texture='Door_Plaque.png'),
}
# Tint the recessed board while retaining the same restrained grain texture.
node=next(n for n in M['inset'].node_tree.nodes if n.type=='BSDF_PRINCIPLED')
tex=next(n for n in M['inset'].node_tree.nodes if n.type=='TEX_IMAGE')
mix=M['inset'].node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
mix.inputs[2].default_value=(.76,.76,.76,1)
M['inset'].node_tree.links.new(tex.outputs['Color'],mix.inputs[1]);M['inset'].node_tree.links.new(mix.outputs[0],node.inputs['Base Color'])

def parent_world(o,parent):
    mw=o.matrix_world.copy();o.parent=parent;o.matrix_world=mw
def uv_planar(o,plaque=False):
    me=o.data
    uv=me.uv_layers.active or me.uv_layers.new(name='UV0')
    for p in me.polygons:
        for li in p.loop_indices:
            co=o.matrix_world@me.vertices[me.loops[li].vertex_index].co
            if plaque:
                # Room-facing -Y: +X is right, Z is up.
                uv.data[li].uv=((co.x+.50)/.63+.5,(co.z-.72)/.1575+.5)
            elif abs(p.normal.x)>.75:
                uv.data[li].uv=(co.y/.18+.5,co.z/2.5+.5)
            else:uv.data[li].uv=(co.x/2+.5,co.z/2.5+.5)

def box(name,loc,dims,material,parent,bevel=.002):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=relocate(bpy.context.object,model);o.name=name;o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        bevel=min(bevel,min(dims)*.24)
        b=o.modifiers.new('LowpolyEdgeChamfer','BEVEL');b.width=bevel;b.segments=1
        bpy.ops.object.modifier_apply(modifier=b.name)
        n=o.modifiers.new('WeightedPlanarNormals','WEIGHTED_NORMAL');n.keep_sharp=True;n.weight=50
        bpy.ops.object.modifier_apply(modifier=n.name)
    o.data.materials.append(material);uv_planar(o);parent_world(o,parent)
    return o

def cyl(name,loc,r,depth,material,parent,segments=8,axis='Z'):
    bpy.ops.mesh.primitive_cylinder_add(vertices=segments,radius=r,depth=depth,location=loc)
    o=relocate(bpy.context.object,model);o.name=name
    if axis=='Y':o.rotation_euler.x=math.pi/2
    elif axis=='X':o.rotation_euler.y=math.pi/2
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    o.data.materials.append(material);uv_planar(o);parent_world(o,parent)
    return o

# Frame has no bottom crossbar, so its foot remains 1 mm above the existing floor.
for sx in [-1,1]:
    box('Frame_Jamb_'+str(sx),(sx*1.052,0,.025),(.104,.148,2.55),M['wood'],fixed,.004)
    box('Frame_OuterTrim_'+str(sx),(sx*1.078,-.074,.028),(.045,.023,2.556),M['inset'],fixed,.002)
    box('Frame_Stop_'+str(sx),(sx*.998,.025,0),(.018,.022,2.49),M['dark'],fixed,.001)
box('Frame_Head',(0,0,1.30),(2.208,.148,.10),M['wood'],fixed,.004)
box('Frame_HeadTrim',(0,-.074,1.323),(2.215,.023,.047),M['inset'],fixed,.002)
box('Threshold',(0,-.008,-1.247),(1.99,.128,.006),M['dark'],fixed,.001)

def pull_handle(name,x,sign,parent):
    # A continuous eight-sided tube with low-poly curved ends; no piled cylinders.
    profile=[(-.053,-.185),(-.078,-.185),(-.095,-.178),(-.105,-.162),
             (-.105,.042),(-.095,.058),(-.078,.065),(-.053,.065)]
    path=[Vector((x,y*sign,z)) for y,z in profile]
    n=8;vs=[];radius=.009
    for i,p in enumerate(path):
        tangent=(path[min(i+1,len(path)-1)]-path[max(i-1,0)]).normalized()
        a=Vector((1,0,0));b=tangent.cross(a).normalized()
        for j in range(n):
            t=j*math.tau/n;v=p+radius*(a*math.cos(t)+b*math.sin(t));vs.append(tuple(v))
    fs=[tuple(range(n-1,-1,-1))]
    for i in range(len(path)-1):
        fs.extend((i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j) for j in range(n))
    fs.append(tuple((len(path)-1)*n+j for j in range(n)))
    me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(vs,[],fs);me.update()
    o=bpy.data.objects.new(name,me);model.objects.link(o);o['asset_owner']=OWNER
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(me);bm.free()
    me.materials.append(M['brass']);uv_planar(o);parent_world(o,parent)

for sx,parent,label in [(-1,left,'Left'),(1,right,'Right')]:
    cx=sx*.497;w=.982
    box(label+'_SolidCore',(cx,0,-.003),(w,.064,2.474),M['inset'],parent,.0025)
    # Front and back have the same assembled panel joinery.
    for face_sign in [-1,1]:
        y=.041*face_sign
        for dx in [-.434,.434]:
            box(label+'_Stile',(cx+dx,y,-.003),(.114,.018,2.472),M['wood'],parent,.0025)
        for z,h in [(1.177,.12),(-.15,.12),(-1.145,.18)]:
            box(label+'_Rail',(cx,y,z),(.77,.018,h),M['wood'],parent,.0025)
        for z,h in [(.515,1.205),(-.635,.845)]:
            box(label+'_InsetPanel',(cx,.0335*face_sign,z),(.747,.006,h),M['inset'],parent,.003)
            # Narrow miter-like panel lips read well under the game's side lighting.
            for px in [-.377,.377]:
                box(label+'_PanelLipV',(cx+px,.041*face_sign,z),(.013,.014,h+.014),M['wood'],parent,0)
            for pz in [z-h/2,z+h/2]:
                box(label+'_PanelLipH',(cx,.041*face_sign,pz),(.754,.014,.013),M['wood'],parent,0)
        hx=sx*.102
        box(label+'_HandlePlate',(hx,.052*face_sign,-.06),(.037,.005,.318),M['brass'],parent,.002)
        pull_handle(label+'_PullHandle',hx,-face_sign,parent)
        for z in [-.195,.075]:
            cyl(label+'_PlateScrew',(hx,.0554*face_sign,z),.0035,.0013,M['dark'],parent,8,'Y')
        cyl(label+'_KeyEscutcheon',(hx,.053*face_sign,-.304),.014,.004,M['brass'],parent,12,'Y')
        cyl(label+'_Keyhole',(hx,.0553*face_sign,-.302),.0046,.0006,M['dark'],parent,8,'Y')
        box(label+'_Keyslot',(hx,.0554*face_sign,-.310),(.0045,.0008,.009),M['dark'],parent,0)
    for hz in [-.90,0,.90]:
        # Small barrel and leaf plate stay attached to the moving leaf for later rigging.
        cyl(label+'_HingeBarrel',(sx*.987,-.045,hz),.010,.078,M['dark'],parent,8,'Z')
        box(label+'_HingePlate',(sx*.967,-.051,hz),(.037,.004,.074),M['brass'],parent,.001)
    box(label+'_BottomKickPlate',(cx,-.052,-1.118),(.748,.003,.104),M['dark'],parent,.002)

# Name plaque belongs to one leaf; it never bridges the centre joint.
plaque=box('OfficeName_Plaque',(-.50,-.056,.72),(.63,.009,.1575),M['plaque'],left,.006)
uv_planar(plaque,True)
for sx in [-1,1]:
    cyl('OfficeName_Fixing',(-.50+sx*.292,-.0612,.72),.003,.0015,M['brass'],left,8,'Y')

def validate(tag):
    triangles=0;issues=[];objects=[]
    for o in model.objects:
        if o.type!='MESH':continue
        me=o.data;me.calc_loop_triangles();triangles+=len(me.loop_triangles)
        bm=bmesh.new();bm.from_mesh(me)
        boundary=sum(not e.is_manifold for e in bm.edges);deg=sum(f.calc_area()<1e-12 for f in bm.faces);vol=bm.calc_volume(signed=True);bm.free()
        if boundary or deg or vol<=0:issues.append({'name':o.name,'nonmanifold':boundary,'degenerate':deg,'volume':vol})
        objects.append({'name':o.name,'triangles':len(me.loop_triangles),'UV0':bool(me.uv_layers)})
    result={'asset':OWNER,'stage':tag,'triangles':triangles,'mesh_objects':len(objects),'issues':issues,'objects':objects,
        'opening_m':[2,2.5,.1],'front':'Blender -Y / normalized Unity -Z','floor_clearance_m':.001,
        'style':'Muted walnut, recessed panels, aged brass, restrained wear; low-poly facets on hardware.',
        'UV':'Global wood UV0; plaque face UV0. Generate unique UV2 with Unity model importer.',
        'integration':'Keep existing Main Door transform, collider, LookTarget trigger and scene references. Hide original cube renderer only. Current wall is solid; static opaque door, no new opening mechanics.'}
    (ART/(tag+'_Validation.json')).write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k!='objects'},ensure_ascii=True),flush=True)
    if issues:raise RuntimeError('Door has topology errors: '+str(issues))
    return result

validate('Source')
for img in bpy.data.images:
    if img.source=='FILE':img.pack()

# Simple neutral studio outside export collection.
scene.render.engine='CYCLES';scene.cycles.samples=40;scene.cycles.use_denoising=True
scene.view_settings.view_transform='AgX'
scene.world.use_nodes=True
bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.12,.15,.18,1);bg.inputs['Strength'].default_value=.45
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for name,loc,power,size,color in [('Key',(-3,-4,5),520,4,(1,.89,.74)),('Fill',(3,-3,2),260,3,(.72,.82,1)),('Rim',(1,2,4),420,3,(.90,.94,1))]:
    d=bpy.data.lights.new('LM_'+name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
    o=bpy.data.objects.new('LM_'+name,d);studio.objects.link(o);o.location=loc;aim(o,(0,0,0))
camera=bpy.data.objects.new('LM_PreviewCamera',bpy.data.cameras.new('LM_PreviewCamera'));studio.objects.link(camera)
camera.location=(3.7,-6,2.6);camera.data.type='ORTHO';camera.data.ortho_scale=3.7;camera.data.clip_start=.01;aim(camera,(0,0,.03));scene.camera=camera
scene.render.resolution_x=1400;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
for s in bpy.data.screens:
    for a in s.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.overlay.show_overlays=False;a.spaces.active.clip_start=.01;a.spaces.active.region_3d.view_distance=5
bpy.ops.object.select_all(action='DESELECT')
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'LawyerMainDoor_Source.blend'))

# Combine rigid pieces per leaf/material. Retain leaf pivots; one renderer per
# visible material rather than one renderer for every panel or screw.
for parent in [fixed,left,right]:
    groups={}
    for o in list(model.objects):
        if o.type=='MESH' and o.parent==parent:groups.setdefault(o.data.materials[0].name,[]).append(o)
    for key,obs in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in obs:o.select_set(True)
        bpy.context.view_layer.objects.active=obs[0]
        if len(obs)>1:bpy.ops.object.join()
        bpy.context.object.name=parent.name+'_'+key.removeprefix('LM_')
stats=validate('GameReady')
if stats['triangles']>4500:raise RuntimeError('Exceeded 4500-triangle door budget')
bpy.ops.object.select_all(action='DESELECT')
for o in model.objects:o.select_set(True)
bpy.context.view_layer.objects.active=root
dest=ASSET/'Models/LawyerMainDoor.fbx';dest.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,object_types={'MESH','EMPTY'},global_scale=1,
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_space_transform=True,
    bake_space_transform=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_triangles=True,bake_anim=False,
    add_leaf_bones=False,path_mode='STRIP',use_custom_props=True)
bpy.ops.object.select_all(action='DESELECT')
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'LawyerMainDoor_GameReady.blend'))
for name,pos,target,scale in [('Door_Studio_Front',(2.6,-6,1.8),(0,0,.02),3.5),('Door_Studio_Back',(-2.6,6,1.6),(0,0,.02),3.5),('Door_Detail',(1,-3,1),(-.1,0,.25),1.5)]:
    camera.location=pos;aim(camera,target);camera.data.ortho_scale=scale;scene.render.filepath=str(ART/(name+'.png'))
    bpy.ops.render.render(write_still=True)
    print('RENDER_COMPLETE '+name,flush=True)
print('LAWYER_MAIN_DOOR_EXPORT_COMPLETE',flush=True)
