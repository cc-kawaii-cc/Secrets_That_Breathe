import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'Assets/SecretsThatBreathe/Props/OfficeGlassDoor'; SRC=ROOT/'ArtSource/OfficeGlassDoor'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene; scene.unit_settings.system='METRIC'
def mat(name,color,metal,rough):
    m=bpy.data.materials.new(name); m.use_nodes=True; p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1); p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough; return m
al=mat('Aluminium_Silver',(.59,.62,.65),.72,.29)
rubber=mat('Rubber_Seal',(.017,.020,.022),0,.83)
chrome=mat('Handle_Chrome',(.72,.75,.78),.9,.19)
glass=mat('OfficeNight_Glass',(1,1,1),.12,.24)
tex=glass.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=bpy.data.images.load(str(OUT/'Textures/OfficeNight_Glass.png')); glass.node_tree.links.new(tex.outputs['Color'],glass.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
uvnode=glass.node_tree.nodes.new('ShaderNodeUVMap'); uvnode.uv_map='UVMap'; glass.node_tree.links.new(uvnode.outputs['UV'],tex.inputs['Vector'])
parts=[]
def finish(ob,name,m,bevel=0):
    ob.name=name; ob.data.materials.append(m); bpy.context.view_layer.objects.active=ob
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=ob.modifiers.new('Small manufactured edge','BEVEL'); mod.width=bevel; mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=ob.modifiers.new('Planar weighted normals','WEIGHTED_NORMAL'); mod.keep_sharp=True; bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(ob); return ob
def box(name,pos,size,m,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos); ob=bpy.context.object; ob.dimensions=size; return finish(ob,name,m,bevel)
def ring(name,x0,x1,z0,z1,border,depth,m,bevel=0):
    outer=[(x0,z0),(x1,z0),(x1,z1),(x0,z1)]; inner=[(x0+border,z0+border),(x1-border,z0+border),(x1-border,z1-border),(x0+border,z1-border)]
    v=[(x,y,z) for y in [-depth/2,depth/2] for loop in [outer,inner] for x,z in loop]; f=[]
    for i in range(4):
        j=(i+1)%4
        f.extend([(i,j,4+j,4+i),(8+i,12+i,12+j,8+j),(i,8+i,8+j,j),(4+i,4+j,12+j,12+i)])
    me=bpy.data.meshes.new(name); me.from_pydata(v,[],f); me.update(); ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob)
    bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    return finish(ob,name,m,bevel)
def tube(name,x,side):
    # Continuous bent pull, 8 sides, six path stations, capped solid ends.
    path=[Vector((x,side*.024,.94)),Vector((x,side*.052,.94)),Vector((x,side*.068,.956)),Vector((x,side*.068,1.244)),Vector((x,side*.052,1.26)),Vector((x,side*.024,1.26))]
    v=[]; f=[]
    for i,p in enumerate(path):
        t=(path[min(i+1,5)]-path[max(0,i-1)]).normalized(); a=Vector((1,0,0)); b=t.cross(a).normalized()
        for j in range(8): v.append(tuple(p+.009*(math.cos(j*math.tau/8)*a+math.sin(j*math.tau/8)*b)))
    for i in range(5):
        for j in range(8): f.append((i*8+j,i*8+(j+1)%8,(i+1)*8+(j+1)%8,(i+1)*8+j))
    f.extend([tuple(reversed(range(8))),tuple(range(40,48))])
    me=bpy.data.meshes.new(name); me.from_pydata(v,[],f); me.update(); ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob); finish(ob,name,chrome)
    bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth=len(p.vertices)==4
    return ob
def combine(name,objects,pivot):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]; bpy.ops.object.join(); ob=bpy.context.object; ob.name=name
    if ob.data.uv_layers:
        ob.data.uv_layers.active_index=0; ob.data.uv_layers[0].active_render=True
    scene.cursor.location=pivot; bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    return ob
frameparts=[]
for x in [-.8925,.8925]: frameparts.append(box('Outer jamb',(x,0,1.10),(.055,.09,2.20),al,.001))
frameparts.append(box('Outer header',(0,0,2.1725),(1.73,.09,.055),al,.001))
# Three hinge barrels per leaf, attached to the fixed frame; moving rail abuts barrel.
for x in [-.86,.86]:
    for z in [.24,1.07,1.90]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=.013,depth=.078,location=(x,-.035,z))
        frameparts.append(finish(bpy.context.object,'Hinge barrel',al))
frame=combine('Frame',frameparts,(0,0,0)); leaves=[]
for side in [-1,1]:
    x0,x1=(-.86,-.003) if side==-1 else (.003,.86)
    group=[ring('Door frame',x0,x1,.008,2.14,.060,.045,al,.001)]
    group.append(ring('Glass gasket',x0+.056,x1-.056,.064,2.084,.009,.016,rubber))
    panel=box('Glass panel',((x0+x1)/2,0,1.074),(x1-x0-.12,.006,2.012-.012),glass)
    uv=panel.data.uv_layers.active
    image_aspect=tex.image.size[0]/tex.image.size[1]
    for p in panel.data.polygons:
        for li in p.loop_indices:
            co=panel.matrix_world@panel.data.vertices[panel.data.loops[li].vertex_index].co
            uv.data[li].uv=(.5+co.x/(2.0*image_aspect),(co.z-.074)/2.0)
    group.append(panel)
    for face in [-1,1]:group.append(tube('Pull handle',side*.035,face))
    if side==1:
        for face in [-1,1]:
            bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=.011,depth=.003,location=(.035,face*.024,.85),rotation=(math.pi/2,0,0))
            group.append(finish(bpy.context.object,'Lock escutcheon',chrome))
    leaf=combine('DoorLeft' if side==-1 else 'DoorRight',group,(side*.86,-.035,0)); leaves.append(leaf)
models=[frame,*leaves]
root=bpy.data.objects.new('OfficeGlassDoor',None); bpy.context.collection.objects.link(root)
for o in models:o.parent=root
report={'overall_m':[1.84,.09,2.20],'leaf_width_m':.857,'center_gap_m':.006,'leaf_frame_depth_m':.045,'glass_thickness_m':.006,'hinges_x_m':[-.86,.86],'parts':{},'glass':'opaque supplied photograph, center crop with matching continuous UV, not transmissive'}
for ob in models:
    bm=bmesh.new(); bm.from_mesh(ob.data)
    assert all(e.is_manifold for e in bm.edges),ob.name+' contains open or non-manifold edges'
    assert all(f.calc_area()>1e-10 for f in bm.faces),ob.name+' contains degenerate faces'
    bm.free()
    report['parts'][ob.name]={'triangles':sum(len(p.vertices)-2 for p in ob.data.polygons),'pivot':list(ob.location)}
report['triangles']=sum(p['triangles'] for p in report['parts'].values()); assert report['triangles']<1600
# Analytic frame clearances: no hinge-side x extension crosses jamb during +/-90-degree swings.
report['swing_check']='0..90 degrees outward, 15-degree samples: vertices within fixed-jamb depth stay in the opening. Not triangle intersection or physics simulation.'
for leaf,sign in zip(leaves,[-1,1]):
    for angle in range(0,91,15):
        leaf.rotation_euler.z=math.radians(sign*angle)
        bpy.context.view_layer.update()
        points=[leaf.matrix_world@v.co for v in leaf.data.vertices]
        for p in points:
            if abs(p.y)<.045 and .008<p.z<2.14:
                assert -.8651<p.x<.8651,'Moving leaf intersects fixed jamb depth'
    leaf.rotation_euler.z=0
bpy.context.view_layer.update()
(SRC/'verification.json').write_text(json.dumps(report,indent=2))
bpy.ops.object.select_all(action='DESELECT'); root.select_set(True)
for ob in models:ob.select_set(True)
bpy.context.view_layer.objects.active=frame
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'OfficeGlassDoor.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/OfficeGlassDoor.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
floor=mat('StudioFloor',(.10,.12,.14),0,.85)
box('PreviewFloor',(0,0,-.035),(200,200,.06),floor)
for pos,power,size in [((-3,-4,5),420,4),((3,-1,3),260,3),((0,3,4),330,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos); o=bpy.context.object; o.data.energy=power; o.data.size=size; aim(o,(0,0,1))
bpy.ops.object.camera_add(location=(2.5,-5.4,2.7)); camera=bpy.context.object; camera.data.type='ORTHO'; camera.data.ortho_scale=2.9; aim(camera,(0,0,1.1)); scene.camera=camera
scene.world.color=(.20,.20,.20); scene.render.engine='CYCLES'; scene.cycles.samples=40
scene.render.resolution_x=1100; scene.render.resolution_y=1200; scene.render.resolution_percentage=100; scene.view_settings.view_transform='AgX'
for name,pos,angles in [('Front',(2.5,-5.4,2.7),(0,0)),('Back',(-2.5,5.4,2.7),(0,0)),('Open',(2.5,-5.4,2.7),(-65,65))]:
    for leaf,a in zip(leaves,angles):leaf.rotation_euler.z=math.radians(a)
    camera.location=pos; aim(camera,(0,0,1.1)); scene.render.filepath=str(SRC/f'OfficeGlassDoor_{name}.png'); bpy.ops.render.render(write_still=True)
print(json.dumps(report))
