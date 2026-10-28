"""Author a hollow aged Thai checkpoint cone, export FBX and native mesh data."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent
OUT=R/'Assets/SecretsThatBreathe/Props/ThaiTrafficCone'
OUT.mkdir(parents=True,exist_ok=True);(OUT/'Textures').mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
N=24;verts=[];faces=[];faceuv=[];smooth=[]
def rounded_square(theta,half,corner):
 x,y=math.cos(theta),math.sin(theta);ax,ay=abs(x),abs(y);center=half-corner
 t=half/max(ax,ay)
 if min(ax,ay)*t<=center:return (x*t,y*t)
 dot=center*(ax+ay);t=dot+math.sqrt(max(0,dot*dot-(2*center*center-corner*corner)))
 return (x*t,y*t)
def ring(z,r=None,half=None):
 ids=[]
 for j in range(N):
  a=2*math.pi*j/N
  if half is not None:x,y=rounded_square(a,half,.024)
  else:
   # Subtle long-used PVC waviness, without collapsing the silhouette.
   worn=1+.008*math.sin(3*a+.4)*math.sin(math.pi*max(0,min(1,(z-.045)/.655)))
   x,y=r*math.cos(a)*worn,r*math.sin(a)*worn
  ids.append(len(verts));verts.append((x,y,z))
 return ids
def uv(idx,kind,u=None):
 x,y,z=verts[idx]
 if kind=='body':return (u,.29+.69*(z-.045)/.655)
 if kind=='inner':return (.93+(u-.5)*.06,.93+(z/.7)*.04)
 if kind=='rim':return (.10+x*.3,.94+y*.3)
 if kind=='base_side':return (.02+.96*u,.03+.18*z/.045)
 return (.5+x/.36*.90,.13+y/.36*.20)
def bridge(a,b,kind,sm=False):
 for j in range(N):
  j2=(j+1)%N;f=[a[j],a[j2],b[j2],b[j]]
  faces.append(f);smooth.append(sm)
  faceuv.append([uv(i,kind,k) for i,k in zip(f,[j/N,(j+1)/N,(j+1)/N,j/N])])
# Continuous outside, top rim, inside and underside: one thick closed shell.
base=[ring(0,half=.174),ring(.005,half=.18),ring(.035,half=.18),ring(.045,half=.173)]
for a,b in zip(base,base[1:]):bridge(a,b,'base_side')
body=[ring(z,r=r) for z,r in [(.045,.143),(.063,.145),(.08,.137),(.34,.085),(.58,.039),(.68,.025),(.70,.022)]]
bridge(base[-1],body[0],'base')
for a,b in zip(body,body[1:]):bridge(a,b,'body',True)
inner=[ring(z,r=r) for z,r in [(.70,.015),(.68,.018),(.08,.130),(0,.135)]]
bridge(body[-1],inner[0],'rim')
for a,b in zip(inner,inner[1:]):bridge(a,b,'inner',True)
bridge(inner[-1],base[0],'base')
me=bpy.data.meshes.new('ThaiTrafficCone_Worn');me.from_pydata(verts,[],faces);me.update()
layer=me.uv_layers.new(name='UVMap')
for p,uvs,sm in zip(me.polygons,faceuv,smooth):
 p.use_smooth=sm
 for li,co in zip(p.loop_indices,uvs):layer.data[li].uv=co
bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update()
me.set_sharp_from_angle(angle=math.radians(40))
obj=bpy.data.objects.new('ThaiTrafficCone_Worn',me);bpy.context.collection.objects.link(obj);bpy.context.view_layer.objects.active=obj;obj.select_set(True)
image=bpy.data.images.load(str(W/'Cone_Texture_Atlas.png'))
image.scale(512,512);image.filepath_raw=str(OUT/'Textures/ThaiTrafficCone_Worn_BaseColor.png');image.file_format='PNG';image.save()
mat=bpy.data.materials.new('ThaiTrafficCone_Worn');mat.use_nodes=True
bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.88;bsdf.inputs['Metallic'].default_value=0
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);me.materials.append(mat)
me.calc_loop_triangles()
bm=bmesh.new();bm.from_mesh(me)
assert all(e.is_manifold for e in bm.edges)
assert all(f.calc_area()>1e-10 for f in bm.faces)
volume=bm.calc_volume(signed=True);assert volume>0;bm.free()
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
bpy.ops.wm.save_as_mainfile(filepath=str(W/'ThaiTrafficCone_Worn.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'ThaiTrafficCone_Worn.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
# Export split corner normals/UV seams for a native Unity mesh using the same triangles.
pv=[];nv=[];uvv=[];tri=[];lookup={}
for t in me.loop_triangles:
 face=[]
 for li in t.loops:
  vi=me.loops[li].vertex_index;p=me.vertices[vi].co;n=me.corner_normals[li].vector;u=layer.data[li].uv
  key=tuple(round(v,7) for v in (*p,*n,*u))
  if key not in lookup:
   lookup[key]=len(pv);pv.append(list(p));nv.append(list(n));uvv.append(list(u))
  face.append(lookup[key])
 tri.append(face)
report={'objects':1,'vertices':len(me.vertices),'render_vertices':len(pv),'triangles':len(me.loop_triangles),'dimensions_metres':[.36,.36,.70],'material_count':1,'texture':[512,512],'hollow_open_top':True,'top_hole_diameter_metres':.030,'through_open_underside':True,'closed_manifold_material_shell':True,'signed_volume':volume,'origin':'base centre at ground','radial_segments':N}
(W/'mesh-data.json').write_text(json.dumps({'positions':pv,'normals':nv,'uv':uvv,'triangles':tri}))
(W/'build-report.json').write_text(json.dumps(report,indent=2))
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.world=bpy.data.worlds.new('NeutralWorld');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.65,.65,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.65
for loc,power,size in [((1.5,-2,2.6),160,2),((-1,1,1.8),80,1.5)]:
 bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(Vector((0,0,.32))-l.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=900;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
def render(name,eye,target=(0,0,.35),scale=.94):
 bpy.ops.object.camera_add(location=eye);c=bpy.context.object;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=scale;scene.camera=c
 scene.render.filepath=str(W/(name+'.png'));bpy.ops.render.render(write_still=True)
render('preview_front',(.9,-1.5,1.0))
render('preview_back',(-1,1.3,.85))
render('preview_top',(.6,-.7,1.8),scale=.90)
render('preview_underside',(.7,-.8,-.8),target=(0,0,.25))
print('CONE_BUILD',json.dumps(report))
