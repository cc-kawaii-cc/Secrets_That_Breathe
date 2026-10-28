"""Replace only the mitten hands; keep the original wrist boundary and body UVs."""
import bpy,bmesh,json,shutil,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
ROOT=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent
SOURCE=ROOT/'Assets/Nemo_Upgrade/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx'
BACKUP=W/'lowpoly_male_base_mesh-TPOSE.before.fbx'
if not BACKUP.exists():
 shutil.copy2(SOURCE,BACKUP);shutil.copy2(str(SOURCE)+'.meta',W/'original.fbx.meta.txt')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(BACKUP))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');me=o.data
oldcoords=[v.co.copy() for v in me.vertices];oldfaces=[list(f.vertices) for f in me.polygons]
oldUV=[[me.uv_layers.active.data[i].uv.copy() for i in f.loop_indices] for f in me.polygons]
oldNormals=[[me.corner_normals[i].vector.copy() for i in f.loop_indices] for f in me.polygons]
original_matrix=o.matrix_world.copy()
positive=set(range(33,103));wrist={34,35,41,43,46,49}
def mirrored(i):
 p=oldcoords[i].copy();p.x=-p.x
 j=min(range(len(oldcoords)),key=lambda k:(oldcoords[k]-p).length)
 assert (oldcoords[j]-p).length<1e-5
 return j
negative={mirrored(i) for i in positive};negwrist={mirrored(i) for i in wrist}
remove=(positive-wrist)|(negative-negwrist)
keep=[i for i in range(len(oldcoords)) if i not in remove];mapping={v:i for i,v in enumerate(keep)}
verts=[tuple(oldcoords[i]) for i in keep];faces=[];uvs=[]
for f,uv in zip(oldfaces,oldUV):
 if not any(v in remove for v in f):faces.append([mapping[v] for v in f]);uvs.append([tuple(v) for v in uv])
bodyfacecount=len(faces)
body_normals={}
for f,uv,ns in zip(oldfaces,oldUV,oldNormals):
 if not any(v in remove for v in f):
  body_normals[frozenset(mapping[v] for v in f)]={mapping[v]:(normal.copy(),tex.copy()) for v,normal,tex in zip(f,ns,uv)}
me.calc_loop_triangles()
tris=list(me.loop_triangles)
bvh=BVHTree.FromPolygons(oldcoords,[t.vertices[:] for t in tris],all_triangles=True)
def projected_uv(p):
 hit,normal,idx,distance=bvh.find_nearest(Vector(p));t=tris[idx]
 uv=[Vector((*me.uv_layers.active.data[i].uv,0)) for i in t.loops]
 return tuple(barycentric_transform(hit,*[oldcoords[i] for i in t.vertices],*uv))[:2]
def V(p):verts.append(tuple(p));return len(verts)-1
def F(f):faces.append(list(f));uvs.append([projected_uv(verts[i]) for i in f])
digits=[]
for sign in [1,-1]:
 def M(v):return Vector((sign*v[0],v[1],v[2]))
 ids=[i if sign==1 else mirrored(i) for i in wrist]
 center=sum((oldcoords[i] for i in ids),Vector())/6
 d=M((.46,-.15,-.875)).normalized();w=M((-.25,-.96,.035));w=(w-d*w.dot(d)).normalized();n=d.cross(w).normalized()*sign
 # Ten vertices around the palm: five on each broad side, width increasing to the knuckles.
 def palm_ring(t,width,thick):
  return [V(center+d*t+w*u*width+n*thick) for u in [-1,-.5,0,.5,1]]+[V(center+d*t+w*u*width-n*thick) for u in [1,.5,0,-.5,-1]]
 base=palm_ring(.075,.105,.038);tip=palm_ring(.185,.118,.033)
 # Bridge the original six-vertex wrist to the ten-vertex palm without cutting the arm.
 boundary=[mapping[i] for i in ids]
 boundary.sort(key=lambda i:math.atan2((Vector(verts[i])-center).dot(n),(Vector(verts[i])-center).dot(w)))
 # Build a monotone angular zipper for unequal rings.
 def angle(i):return math.atan2((Vector(verts[i])-center).dot(n),(Vector(verts[i])-center).dot(w))%(2*math.pi)
 a=sorted(boundary,key=angle);b=sorted(base,key=angle);ia=ib=0
 while ia<len(a) or ib<len(b):
  na=angle(a[(ia+1)%len(a)])+(2*math.pi if ia+1>=len(a) else 0) if ia<len(a) else 1e9
  nb=angle(b[(ib+1)%len(b)])+(2*math.pi if ib+1>=len(b) else 0) if ib<len(b) else 1e9
  if na<nb:F([a[ia%len(a)],a[(ia+1)%len(a)],b[ib%len(b)]]);ia+=1
  else:F([a[ia%len(a)],b[(ib+1)%len(b)],b[ib%len(b)]]);ib+=1
 for j in range(10):
  if j!=4:F([base[j],base[(j+1)%10],tip[(j+1)%10],tip[j]])
 # Root corner order is consistent around each socket. Eight sides round off the digits.
 octagon=[(-1,-.65),(-.65,-1),(.65,-1),(1,-.65),(1,.65),(.65,1),(-.65,1),(-1,.65)]
 def digit(name,root,axis,across,length,width,depth,spread=0):
  c=sum((Vector(verts[i]) for i in root),Vector())/4
  axis=axis.normalized();across=(across-axis*across.dot(axis)).normalized();normal=axis.cross(across).normalized()*sign
  rings=[]
  for t,taper in [(.16,1),(.5,.90),(.83,.78),(.98,.57)]:
   p=c+axis*(length*t)+across*(spread*t*t)+normal*(-.006*t*t)
   rings.append([V(p+across*(u*width*taper)+normal*(v*depth*taper)) for u,v in octagon])
  # Align the new cross section to the four actual socket corners by their coordinates.
  roots=sorted(root,key=lambda i:math.atan2((Vector(verts[i])-c).dot(normal),(Vector(verts[i])-c).dot(across)))
  target=[(-1,-1),(1,-1),(1,1),(-1,1)]
  roots=[min(root,key=lambda i:(Vector(verts[i])-c-across*(u*width)-normal*(v*depth)).length) for u,v in target]
  assert len(set(roots))==4
  for j in range(4):
   F([roots[j],rings[0][2*j],rings[0][2*j+1]])
   F([roots[j],roots[(j+1)%4],rings[0][(2*j+2)%8],rings[0][2*j+1]])
  for r1,r2 in zip(rings,rings[1:]):
   for j in range(8):F([r1[j],r1[(j+1)%8],r2[(j+1)%8],r2[j]])
  F(rings[-1])
  digits.append(dict(side=sign,name=name,tip=list(sum((Vector(verts[i]) for i in rings[-1]),Vector())/8)))
 # Low-poly relaxed spread, little finger shortest and middle finger longest.
 for j,(name,length,spread) in enumerate([('Little',.125,-.021),('Ring',.166,-.009),('Middle',.184,.004),('Index',.163,.021)]):
  root=[tip[j],tip[j+1],tip[8-j],tip[9-j]]
  digit(name,root,d,w,length,.0225 if j else .020,.025,spread)
 # Thumb grows from a genuine open side socket, so it belongs to the same closed surface.
 root=[base[4],base[5],tip[5],tip[4]]
 digit('Thumb',root,(d*.60+w*.80).normalized(),(d*.80-w*.60).normalized(),.127,.034,.028,0)
new=bpy.data.meshes.new(me.name+'_FiveFingers');new.from_pydata(verts,[],faces);new.update()
for mat in me.materials:new.materials.append(mat)
uvlayer=new.uv_layers.new(name=me.uv_layers.active.name)
for f,uv in zip(new.polygons,uvs):
 f.use_smooth=True
 for li,co in zip(f.loop_indices,uv):uvlayer.data[li].uv=co
o.data=new
bm=bmesh.new();bm.from_mesh(new);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(new);bm.free();new.update()
normals=[n.vector.copy() for n in new.corner_normals]
for f in new.polygons:
 preserved=body_normals.get(frozenset(f.vertices))
 if preserved:
  for li,vi in zip(f.loop_indices,f.vertices):
   normals[li]=preserved[vi][0]
   assert (new.uv_layers.active.data[li].uv-preserved[vi][1]).length<1e-7
new.normals_split_custom_set(normals)
new.calc_loop_triangles()
report={'before_vertices':len(oldcoords),'after_vertices':len(new.vertices),'before_triangles':len(me.loop_triangles),'after_triangles':len(new.loop_triangles),'unchanged_body_vertices':len(keep),'unchanged_body_faces':bodyfacecount,'digits':digits,'object_name':o.name,'matrix':[list(r) for r in o.matrix_world]}
bm=bmesh.new();bm.from_mesh(new)
report['nonmanifold_edges']=sum(not e.is_manifold for e in bm.edges)
report['boundary_edges']=sum(e.is_boundary for e in bm.edges)
report['degenerate_faces']=sum(f.calc_area()<1e-10 for f in bm.faces);bm.free()
assert report['nonmanifold_edges']==0,report
assert report['degenerate_faces']==0,report
(W/'build-report.json').write_text(json.dumps(report,indent=2))
bpy.context.view_layer.objects.active=o;o.select_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(W/'LowPolyMale_FiveFingers.blend'))
bpy.ops.export_scene.fbx(filepath=str(W/'candidate-blender.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_NONE',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
# Keep the original FBX object IDs, names, transforms, materials and connections exactly.
# Transfer only the newly exported geometry payload into the original document.
from io_scene_fbx import parse_fbx,encode_bin
original,version=parse_fbx.parse(str(BACKUP));edited,_=parse_fbx.parse(str(W/'candidate-blender.fbx'))
def geometry(doc):return next(n for ob in doc.elems if ob.id==b'Objects' for n in ob.elems if n.id==b'Geometry')
geometry(original).elems[:]=geometry(edited).elems
methods={'B':'add_bool','C':'add_char','Z':'add_int8','Y':'add_int16','I':'add_int32','L':'add_int64','F':'add_float32','D':'add_float64','R':'add_bytes','S':'add_string','f':'add_float32_array','d':'add_float64_array','i':'add_int32_array','l':'add_int64_array','b':'add_bool_array','c':'add_byte_array'}
def encode(node):
 out=encode_bin.FBXElem(node.id)
 for kind,p in zip(node.props_type,node.props):getattr(out,methods[chr(kind)])(bool(p) if chr(kind)=='B' else p)
 out.elems=[encode(c) for c in node.elems]
 return out
encode_bin.write(str(W/'candidate.fbx'),encode(original),version)
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH'
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.display.shading.light='STUDIO';scene.display.shading.color_type='SINGLE';scene.display.shading.single_color=(.6,.6,.6)
scene.display.shading.show_shadows=False;scene.display.shading.show_cavity=True
scene.world=bpy.data.worlds.new('InspectionWorld');scene.world.color=(.12,.12,.12)
def render(name,target,offset,scale,handonly=False):
 if handonly:
  for p in new.polygons:p.hide=False
  # Workbench respects object visibility, so use a temporary hand-only geometry copy.
  copy=o.copy();copy.data=new.copy();scene.collection.objects.link(copy)
  bm=bmesh.new();bm.from_mesh(copy.data)
  bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.x<1.05],context='VERTS');bm.to_mesh(copy.data);bm.free();o.hide_render=True
 bpy.ops.object.camera_add(location=Vector(target)+Vector(offset));c=bpy.context.object
 c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=scale;scene.camera=c
 scene.render.filepath=str(W/(name+'.png'));bpy.ops.render.render(write_still=True)
 if handonly:bpy.data.objects.remove(copy,do_unlink=True);o.hide_render=False
render('after_overview',(0,0,2.1),(0,-12,1),5.6)
render('after_hand_back',(1.29,-.07,2.14),(5,1,2),.72,True)
render('after_hand_palm',(1.29,-.07,2.14),(-5,-1,-2),.72,True)
render('after_hand_front',(1.29,-.07,2.14),(0,-5,0),.72,True)
print('BUILD_REPORT',json.dumps(report))
