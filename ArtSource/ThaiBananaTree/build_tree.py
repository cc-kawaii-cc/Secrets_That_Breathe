import bpy, math, json, random
from pathlib import Path
from mathutils import Vector
R=Path.cwd(); A=R/'Assets/SecretsThatBreathe/Environment/ThaiBananaTree'; S=R/'ArtSource/ThaiBananaTree'
random.seed(31026)
bpy.ops.wm.read_factory_settings(use_empty=True)
# Bake skin samples from all three supplied fruit photos into one small shared atlas.
patches=[('Fruit_Green_6.jpg',(310,100,344,158)),('Fruit_Green_6.jpg',(244,260,265,330)),('Fruit_Mixed_7.jpg',(126,307,141,340)),('Fruit_Ripe_8.jpg',(270,160,283,190)),('Fruit_Green_6.jpg',(323,184,331,191))]
for index,(name,rect) in enumerate(patches):
 image=bpy.data.images.load(str(S/name)); w,h=image.size
 mat=bpy.data.materials.new('PhotoSample');mat.use_nodes=True;n=mat.node_tree.nodes;n.clear()
 tex=n.new('ShaderNodeTexImage');tex.image=image;em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(tex.outputs['Color'],em.inputs[0]);mat.node_tree.links.new(em.outputs[0],out.inputs[0])
 x=index/5
 mesh=bpy.data.meshes.new('Sample');mesh.from_pydata([(x,0,0),(x+.2,0,0),(x+.2,.5,0),(x,.5,0)],[],[(0,1,2,3)]);mesh.materials.append(mat)
 uv=mesh.uv_layers.new();l,t,r,b=rect
 for p in mesh.polygons:
  for li,co in zip(p.loop_indices,[(l/w,1-b/h),(r/w,1-b/h),(r/w,1-t/h),(l/w,1-t/h)]):uv.data[li].uv=co
 obj=bpy.data.objects.new('PhotoSample',mesh);bpy.context.collection.objects.link(obj)
camdata=bpy.data.cameras.new('AtlasCamera');cam=bpy.data.objects.new('AtlasCamera',camdata);bpy.context.collection.objects.link(cam);cam.location=(.5,.25,2);cam.rotation_euler=(0,0,0);camdata.type='ORTHO';camdata.ortho_scale=1
scene=bpy.context.scene;scene.camera=cam;scene.render.engine='CYCLES';scene.cycles.samples=1;scene.render.resolution_x=512;scene.render.resolution_y=256;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(A/'Textures/Fruit_PhotoAtlas.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
def material(name,filename,normal=None):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.79
 tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(A/'Textures'/filename));m.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
 if normal:
  n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(A/'Textures'/normal));n.image.colorspace_settings.name='Non-Color';nm=m.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.6;m.node_tree.links.new(n.outputs['Color'],nm.inputs['Color']);m.node_tree.links.new(nm.outputs['Normal'],p.inputs['Normal'])
 return m
mats=[material('ThaiBanana_Trunk','Trunk_Albedo.png','Trunk_Normal.png'),material('ThaiBanana_Leaf','Leaf_Photo.png'),material('ThaiBanana_Fruit','Fruit_PhotoAtlas.png')]
class Mesh:
 def __init__(self,name,mat):self.name=name;self.mat=mat;self.v=[];self.f=[];self.uv=[];self.col=[]
 def vert(self,p,uv,color):self.v.append(tuple(p));self.uv.append(uv);self.col.append(color);return len(self.v)-1
 def face(self,*v):self.f.append(tuple(v))
 def object(self,parent):
  m=bpy.data.meshes.new(self.name);m.from_pydata(self.v,[],self.f);m.materials.append(mats[self.mat]);m.update();uv=m.uv_layers.new(name='UVMap');c=m.color_attributes.new(name='WindData',type='FLOAT_COLOR',domain='POINT')
  for p in m.polygons:
   p.use_smooth=self.mat!=1
   for li in p.loop_indices:uv.data[li].uv=self.uv[m.loops[li].vertex_index]
  for i,color in enumerate(self.col):c.data[i].color=color
  o=bpy.data.objects.new(self.name,m);bpy.context.collection.objects.link(o);o.parent=parent
  m.calc_loop_triangles();return o,len(m.loop_triangles)
def pipe(mesh,points,radii,sides,colors,uvpatch=None):
 rings=[];previous_a=None
 for j,p in enumerate(points):
  direction=(points[min(j+1,len(points)-1)]-points[max(0,j-1)]).normalized();ref=Vector((1,0,0)) if abs(direction.x)<.8 else Vector((0,1,0));a=(previous_a-direction*previous_a.dot(direction)).normalized() if previous_a is not None else direction.cross(ref).normalized();previous_a=a.copy();b=direction.cross(a).normalized();ring=[]
  for k in range(sides+1):
   u=k/sides;t=j/(len(points)-1);uv=(u,t)
   if uvpatch is not None:uv=((uvpatch+.08+.84*u)/5,.04+.92*t)
   ring.append(mesh.vert(p+radii[j]*(a*math.cos(u*math.tau)+b*math.sin(u*math.tau)),uv,colors[j]))
  rings.append(ring)
 for j in range(len(rings)-1):
  for k in range(sides):mesh.face(rings[j][k],rings[j][k+1],rings[j+1][k+1],rings[j+1][k])
 mesh.face(*reversed(rings[0][:-1]));mesh.face(*rings[-1][:-1])
def trunkpoint(z):return Vector((.035*math.sin(z*1.3),.018*z,z))
def build(variant,lod):
 root=bpy.data.objects.new('ThaiBanana_'+variant+'_LOD'+str(lod),None);bpy.context.collection.objects.link(root)
 trunk=Mesh('Trunk_LOD'+str(lod),0);leaf=Mesh('Leaves_LOD'+str(lod),1);fruit=Mesh('Fruit_LOD'+str(lod),2)
 sides=[10,8,6][lod];segments=[7,5,3][lod]
 z=[2.65*i/segments for i in range(segments+1)];pipe(trunk,[trunkpoint(h) for h in z],[.18*(1-h/3.8)+.01*math.cos(h*7) for h in z],sides,[(min(1,h/2.65)**2,0,0,.1) for h in z])
 # Ten independent petioles emerge in a spiral, not woody branches.
 count=[10,7,5][lod]
 profile=[(0,239,257),(.10,171,311),(.22,142,342),(.36,125,364),(.50,113,368),(.64,111,358),(.77,133,339),(.9,181,308),(1,245,254)]
 def photo(t,q):
  for j in range(len(profile)-1):
   a,l,r=profile[j];b,ll,rr=profile[j+1]
   if t<=b:
    f=(t-a)/(b-a);left=l+(ll-l)*f+3;right=r+(rr-r)*f-3;mid=247;xx=mid+(right-mid)*q if q>0 else mid+(mid-left)*q
    return (xx/500,(25+550*t)/600)
  return (.505,.96)
 for i in range(count):
  angle=i*2.39996+.3;d=Vector((math.cos(angle),math.sin(angle),0));cross=Vector((-d.y,d.x,0));young=i>=count-2
  h=2.02+.055*i;length=(1.7 if not young else 1.35)*(1+.10*math.sin(i*4));width=.66 if not young else .53
  base=trunkpoint(h);reach=.50 if not young else .32;start=base+d*reach+Vector((0,0,.55 if not young else .65))
  mid=base+d*reach*.45+Vector((0,0,.38));phase=(i*.173)%1;bend=(h/2.65)**2
  pipe(trunk,[base,mid,start],[.045,.032,.022],[5,4,3][lod],[(bend,0,0,phase),(bend,.09,0,phase),(bend,.20,0,phase)])
  n=[9,6,4][lod];rings=[]
  for j in range(n+1):
   t=j/n;rise=(.75*t-.16*t*t) if young else (.50*t-1.08*t*t)
   center=start+d*(length*t)+Vector((0,0,rise));w=width*.5*(math.sin(math.pi*t)**.62 if 0<t<1 else .015)
   if not young and j%3==1:w*=.82
   row=[]
   for q in [-1,0,1]:
    p=center+cross*(q*w)+Vector((0,0,-abs(q)*w*.23+math.sin(t*9+i)*q*.025))
    row.append(leaf.vert(p,photo(t,q),(bend,.20+.80*t,abs(q)*t,phase)))
   rings.append(row)
  for j in range(n):
   for k in range(2):leaf.face(rings[j][k],rings[j+1][k],rings[j+1][k+1],rings[j][k+1])
  pipe(trunk,[start,start+d*(length*.50)+Vector((0,0,-.02 if not young else .335)),start+d*length+Vector((0,0,-.58 if not young else .59))],[.019,.012,.004],[4,3,3][lod],[(bend,.20,0,phase),(bend,.6,0,phase),(bend,1,0,phase)])
 # A single pendant stalk leaves the crown and bends outward to the hanging bunch.
 stalk=[trunkpoint(2.44),Vector((.28,-.18,2.81)),Vector((.62,-.41,2.70)),Vector((.74,-.49,2.38)),Vector((.77,-.51,1.35))]
 pipe(trunk,stalk,[.065,.058,.050,.043,.024],[6,5,4][lod],[(.85,.1,0,.38)]*5)
 tiers=[5,4,3][lod];per=[8,6,5][lod]
 for tier in range(tiers):
  z=2.31-tier*(.8/max(1,tiers-1));radius=.055+.012*math.sin(tier)
  for k in range(per):
   angle=math.tau*k/per+tier*.31;d=Vector((math.cos(angle),math.sin(angle),0));base=Vector((.77,-.51,z));scale=1-.07*tier
   points=[base+d*(radius*.35)+Vector((0,0,.02)),base+d*(radius+.10*scale)+Vector((0,0,-.06)),base+d*(radius+.19*scale)+Vector((0,0,.05*scale)),base+d*(radius+.21*scale)+Vector((0,0,.21*scale)),base+d*(radius+.17*scale)+Vector((0,0,.30*scale))]
   patch=(k+tier)%2 if variant=='Green' else (2+(k%2) if (tier<3 or variant=='Ripe') else (k%2))
   if lod==2:points=[points[0],points[2],points[4]]
   pipe(fruit,points,[.018]+([.066*scale,.082*scale,.065*scale] if lod!=2 else [.060*scale])+[.018],[5,4,3][lod],[(.85,.10,0,.38)]*len(points),patch)
   tip=points[-1];pipe(fruit,[tip,tip+Vector((0,0,.023))],[.012,.007],3,[(.85,.10,0,.38)]*2,4)
 # Two small suckers, with their own unfurled blades, kept modest for repeated scene use.
 if lod<2:
  for i in range(2):
   angle=1.8+i*2.8;d=Vector((math.cos(angle),math.sin(angle),0));b=d*.30;top=b+Vector((0,0,.72+i*.20));pipe(trunk,[b,top],[.045,.018],5,[(0,0,0,.4),(.03,.1,0,.4)])
   for k in range(2):
    direction=Vector((math.cos(angle+k*2.4),math.sin(angle+k*2.4),0));side=Vector((-direction.y,direction.x,0));rows=[]
    for j in range(5):
     t=j/4;c=top+direction*(.58*t)+Vector((0,0,.48*t));w=.13*math.sin(math.pi*t)+.003
     rows.append([leaf.vert(c+side*q*w+Vector((0,0,-abs(q)*w*.2)),photo(t,q),(.03,t*.65,abs(q)*t,.4+i*.2)) for q in [-1,0,1]])
    for j in range(4):
     for k in range(2):leaf.face(rows[j][k],rows[j+1][k],rows[j+1][k+1],rows[j][k+1])
 objects=[];report={}
 for m in [trunk,leaf,fruit]:
  o,tris=m.object(root);objects.append(o);report[o.name]=tris
 return root,objects,report
report={}
for variant in ['Green','Ripening','Ripe']:
 roots=[];allobjects=[];report[variant]={}
 for lod in range(3):
  root,objects,stats=build(variant,lod);roots.append(root);allobjects+=[root]+objects;report[variant]['LOD'+str(lod)]=stats
 bpy.ops.object.select_all(action='DESELECT')
 for o in allobjects:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(A/'Models'/('ThaiBanana_'+variant+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_anim=False,add_leaf_bones=False,use_mesh_modifiers=True)
 for root in roots:
  root.hide_render=not root.name.endswith('LOD0');root.hide_viewport=root.hide_render
  for child in root.children:child.hide_render=root.hide_render;child.hide_viewport=root.hide_viewport
 bpy.ops.wm.save_as_mainfile(filepath=str(S/('ThaiBanana_'+variant+'.blend')))
 # Keep the other variant out of the next export and source file.
 if variant!='Ripe':
  for o in allobjects:bpy.data.objects.remove(o,do_unlink=True)
(S/'geometry-report.json').write_text(json.dumps(report,indent=2));print('NEW_TREE',json.dumps(report))




