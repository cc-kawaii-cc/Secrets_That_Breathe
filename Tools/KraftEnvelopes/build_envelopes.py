import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'Assets/SecretsThatBreathe/Props/KraftEnvelopes'; SRC=ROOT/'ArtSource/KraftEnvelopes'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
mat=bpy.data.materials.new('KraftPaper'); mat.use_nodes=True
p=mat.node_tree.nodes.get('Principled BSDF'); p.inputs['Roughness'].default_value=.9
tex=mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=bpy.data.images.load(str(OUT/'Textures/KraftEnvelope_Reference.png'))
mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
def make(name,opened):
    verts=[]; faces=[]; kinds=[]; uvcoords=[]
    def face(points,kind):
        ids=[]
        for pt in points:
            if pt not in verts: verts.append(pt)
            ids.append(verts.index(pt))
        faces.append(ids); kinds.append(kind)
    xs=[-.1175,-.1165,.1165,.1175]; ys=[-.1625,-.1615,.1625]; zs=[0,.001,.003,.004]
    if not opened:
        xs=[-.1175,.1175]; ys=[-.1625,.1625]; zs=[0,.004]
    cells={(i,j,k) for i in range(len(xs)-1) for j in range(len(ys)-1) for k in range(len(zs)-1) if not opened or j==0 or i in (0,2) or k in (0,2)}
    dirs=[((-1,0,0),[(0,0,0),(0,0,1),(0,1,1),(0,1,0)]),((1,0,0),[(1,0,0),(1,1,0),(1,1,1),(1,0,1)]),((0,-1,0),[(0,0,0),(1,0,0),(1,0,1),(0,0,1)]),((0,1,0),[(0,1,0),(0,1,1),(1,1,1),(1,1,0)]),((0,0,-1),[(0,0,0),(0,1,0),(1,1,0),(1,0,0)]),((0,0,1),[(0,0,1),(1,0,1),(1,1,1),(0,1,1)])]
    for i,j,k in sorted(cells):
        for d,q in dirs:
            if (i+d[0],j+d[1],k+d[2]) in cells: continue
            points=[(xs[i+a],ys[j+b],zs[k+c]) for a,b,c in q]
            face(points,'body' if d==(0,0,1) and k==len(zs)-2 else 'plain')
    if opened:
        # Thick six-sided flap, unfolded beyond the mouth and lifted 30 degrees.
        outline=[(-.1175,0),(.1175,0),(.113,.049),(.101,.063),(-.101,.063),(-.113,.049)]
        start=len(verts)
        for dz in [0,.001]:
            for x,t in outline: verts.append((x,.1625+t*math.cos(math.pi/6),.004+t*math.sin(math.pi/6)+dz))
        faces.extend([list(reversed(range(start,start+6))),list(range(start+6,start+12))]); kinds.extend(['flap','flap'])
        for i in range(6): faces.append([start+i,start+(i+1)%6,start+(i+1)%6+6,start+i+6]); kinds.append('plain')
    me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update(); ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob); me.materials.append(mat)
    uv=me.uv_layers.new(name='PhotoUV')
    for poly,kind in zip(me.polygons,kinds):
        for li in poly.loop_indices:
            x,y,z=me.vertices[me.loops[li].vertex_index].co
            if kind=='body': u=.183+(x+.1175)/.235*.632; v=.06+(y+.1625)/.325*(.722 if opened else .878)
            elif kind=='flap': u=.183+(x+.1175)/.235*.632; v=.938-(y-.1625)/(.063*math.cos(math.pi/6))*.12
            else: u=.25+(x+.1175)/.235*.48; v=.22+(y+.1625)/.40*.45
            uv.data[li].uv=(u,v)
    bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.triangulate(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    return ob
closed=make('KraftEnvelope_Closed',False); opened=make('KraftEnvelope_Open',True)
def export(ob,name):
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active=ob
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/f'{name}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
export(closed,'KraftEnvelope_Closed'); export(opened,'KraftEnvelope_Open')
layers=[]
for i,(isopen,x,y,angle) in enumerate([(True,-.022,.047,-13),(False,.024,-.008,9),(False,-.036,-.032,-19),(False,.032,.003,15),(True,.005,.002,-4)]):
    ob=(opened if isopen else closed).copy(); ob.data=ob.data.copy(); bpy.context.collection.objects.link(ob)
    ob.name=f'Envelope_{i+1}'; ob.location=(x,y,i*.0048); ob.rotation_euler.z=math.radians(angle); layers.append(ob)
bpy.data.objects.remove(closed,do_unlink=True); bpy.data.objects.remove(opened,do_unlink=True)
bpy.ops.object.select_all(action='DESELECT')
for ob in layers: ob.select_set(True)
bpy.context.view_layer.objects.active=layers[0]; bpy.ops.object.join(); stack=bpy.context.object; stack.name='KraftEnvelopes_MessyStack'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
export(stack,'KraftEnvelopes_MessyStack'); bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'KraftEnvelopes.blend'))
report={'envelopes':5,'open_flaps':2,'mesh_objects':1,'material_count':1,'triangles':len(stack.data.polygons)}
(SRC/'verification.json').write_text(json.dumps(report,indent=2)); print(report)
def aim(ob,point): ob.rotation_euler=(Vector(point)-ob.location).to_track_quat('-Z','Y').to_euler()
floor=bpy.data.materials.new('PreviewFloor'); floor.diffuse_color=(.16,.18,.20,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001)); bpy.context.object.data.materials.append(floor)
for pos,power,size in [((-.7,-.8,1),35,.8),((.3,.6,.8),25,.6)]:
    bpy.ops.object.light_add(type='AREA',location=pos); light=bpy.context.object; light.data.energy=power; light.data.size=size; aim(light,(0,0,0))
bpy.ops.object.camera_add(location=(.50,-.68,.65)); cam=bpy.context.object; cam.data.type='ORTHO'; cam.data.ortho_scale=.57; aim(cam,(0,.025,.015))
scene=bpy.context.scene; scene.camera=cam; scene.world.color=(.22,.22,.22); scene.render.engine='CYCLES'; scene.cycles.samples=40
scene.render.resolution_x=1100; scene.render.resolution_y=1000; scene.render.resolution_percentage=100; scene.view_settings.view_transform='AgX'
scene.render.filepath=str(SRC/'KraftEnvelopes_MessyStack_Preview.png'); bpy.ops.render.render(write_still=True)

