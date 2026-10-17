import bpy, math, json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2]
S=R/'ArtSource/BMWM340i'; O=R/'Assets/SecretsThatBreathe/Props/BMWM340i'
S.mkdir(parents=True,exist_ok=True); (O/'Models').mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
def mat(n,c,metal=0,rough=.4,emit=0):
 m=bpy.data.materials.new(n); m.diffuse_color=(*c,1); m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*c,1); p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
 if emit: p.inputs['Emission Color'].default_value=(*c,1); p.inputs['Emission Strength'].default_value=emit
 return m
paint=mat('SilverPaint',(.51,.57,.61),.72,.25); black=mat('GlossBlack',(.012,.017,.022),.35,.25); rubber=mat('Tire',(.018,.021,.026),0,.83); glass=mat('TintedGlass',(.035,.072,.083),.48,.18); alloy=mat('MachinedAlloy',(.57,.62,.66),.9,.22); brake=mat('BrakeDisc',(.24,.26,.28),.8,.5); red=mat('RedCaliper',(.65,.012,.018),.3,.3); lamp=mat('LEDWhite',(.8,.94,1),.1,.18,2); tail=mat('LEDRed',(.72,.004,.012),.2,.23,1.4); blue=mat('BadgeBlue',(.015,.24,.7),.4,.3); white=mat('PlateWhite',(.88,.9,.91),.1,.4)
parts=[]
def mesh(n,v,f,m):
 me=bpy.data.meshes.new(n); me.from_pydata(v,[],f); me.update(); ob=bpy.data.objects.new(n,me); bpy.context.collection.objects.link(ob); ob.data.materials.append(m); parts.append(ob); return ob
def box(n,p,d,m,b=.0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p); ob=bpy.context.object; ob.name=n; ob.dimensions=d; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); ob.data.materials.append(m); parts.append(ob)
 if b:
  mod=ob.modifiers.new('Small edge bevel','BEVEL'); mod.width=b; mod.segments=1; bpy.context.view_layer.objects.active=ob; bpy.ops.object.modifier_apply(modifier=mod.name)
 return ob
def line(n,pts,r,m):
 # Square-section strips keep trim inexpensive.
 vs=[]; fs=[]
 for j,p in enumerate(pts):
  t=Vector(pts[min(j+1,len(pts)-1)])-Vector(pts[max(0,j-1)]); t.normalize(); a=t.cross(Vector((0,0,1)))
  if a.length<.01: a=t.cross(Vector((0,1,0)))
  a.normalize(); b=t.cross(a).normalized()
  for u,w in [(1,1),(-1,1),(-1,-1),(1,-1)]: vs.append(Vector(p)+r*(u*a+w*b))
 for j in range(len(pts)-1):
  for k in range(4): fs.append((j*4+k,j*4+(k+1)%4,(j+1)*4+(k+1)%4,(j+1)*4+k))
 fs.extend([(3,2,1,0),tuple((len(pts)-1)*4+k for k in range(4))]); return mesh(n,vs,fs,m)
def panel(n,pts,m): return mesh(n,pts,[tuple(range(len(pts)))],m)
def cyl(n,p,r,depth,m,segments=24):
 bpy.ops.mesh.primitive_cylinder_add(vertices=segments,radius=r,depth=depth,location=p,rotation=(0,math.pi/2,0)); ob=bpy.context.object; ob.name=n; ob.data.materials.append(m); parts.append(ob); return ob
def width(y): return .9-.08*(abs(y)/2.36)**4
def top(y):
 if y<-.8: return 1.01-.17*((-y-.8)/1.56)**1.6
 if y>1.45: return 1.015-.10*((y-1.45)/.91)
 return 1.015
ys=sorted(set([-2.36,-2.29,-2.12,-1.98,-.85,-.72,-.4,0,.4,.85,1.0,1.98,2.15,2.30,2.36]+[round(c+.395*math.cos(math.pi*i/12),5) for c in [-1.4,1.45] for i in range(13)]))
v=[]
for y in ys:
 w=width(y); t=top(y); low=.19
 for c in [-1.4,1.45]:
  if abs(y-c)<=.39501: low=max(low,.345+math.sqrt(max(0,.395**2-(y-c)**2)))
 # Cross-section from left sill across hood to right sill.
 for x,z in [(-w*.97,low),(-w, max(low+.025,t-.16)),(-w*.97,t-.035),(-w*.72,t+.015),(0,t+.035),(w*.72,t+.015),(w*.97,t-.035),(w,max(low+.025,t-.16)),(w*.97,low)]: v.append((x,y,z))
f=[]
for j in range(len(ys)-1):
 for k in range(8): f.append((j*9+k,j*9+k+1,(j+1)*9+k+1,(j+1)*9+k))
f.extend([tuple(range(8,-1,-1)),tuple((len(ys)-1)*9+k for k in range(9))]); body=mesh('BodyShell',v,f,paint)
for poly in body.data.polygons: poly.use_smooth=True
body.data.set_sharp_from_angle(angle=.65)
box('Underbody',(0,0,.24),(1.42,3.95,.12),black,.04)
# Cabin: thin roof with separate glazing and structural pillars.
roof=[(-.69,-.20,1.41),(.69,-.20,1.41),(.70,.25,1.445),(.69,.75,1.425),(-.69,.75,1.425),(-.70,.25,1.445)]
panel('Roof',roof,paint)
panel('Windshield',[(-.79,-.88,1.045),(.79,-.88,1.045),(.675,-.21,1.397),(-.675,-.21,1.397)],glass)
panel('RearGlass',[(-.68,.78,1.407),(.68,.78,1.407),(.78,1.50,1.04),(-.78,1.50,1.04)],glass)
for s in [-1,1]:
 def side(y,z): return (s*(.85-(z-1.03)*.41),y,z)
 front=[side(-.82,1.045),side(-.19,1.39),side(.16,1.405),side(.16,1.045)]
 rear=[side(.25,1.045),side(.25,1.405),side(.74,1.39),side(1.35,1.09),side(1.26,1.045)]
 panel('FrontSideGlass',front,glass); panel('RearSideGlass',rear,glass)
 panel('CPillar',[side(.78,1.414),side(1.47,1.035),side(1.26,1.045),side(1.35,1.09),side(.74,1.39)],paint)
 outline=[side(-.88,1.03),side(-.21,1.416),side(.25,1.438),side(.78,1.414),side(1.47,1.035),side(-.88,1.03)]
 line('WindowSurround',outline,.024,paint); line('BlackWindowTrim',[side(-.84,1.045),side(1.39,1.045)],.013,black)
 line('BPillar',[side(.205,1.04),side(.205,1.416)],.035,black)
 line('QuarterWindowPillar',[side(.97,1.045),side(.93,1.30)],.013,black)
 for y in [-.84,.22,1.31]:
  bottom=.22 if y<1 else .55
  line('DoorGap',[(s*(width(y)+.002),y,top(y)-.04),(s*(width(y)+.008),y+.035,.69),(s*(width(y)*.978),y+.03,bottom)],.003,black)
 line('SideSkirt',[(s*.876,-.98,.21),(s*.905,-.68,.17),(s*.905,.88,.17),(s*.881,1.04,.23)],.024,paint)
 for y in [-.05,.93]: box('DoorHandle',(s*.901,y,.928),(.028,.15,.027),paint,.012)
 line('MirrorStem',[(s*.82,-.71,1.04),(s*.99,-.76,1.07)],.027,black)
 box('MirrorHousing',(s*1.005,-.76,1.11),(.22,.24,.10),black,.045)
 box('MirrorGlass',(s*1.006,-.637,1.115),(.16,.008,.061),alloy,.012)
 line('MirrorIndicator',[(s*.94,-.885,1.119),(s*1.09,-.86,1.119)],.006,lamp)
 # Hood character creases.
 line('HoodCrease',[(s*.39,-2.16,.907),(s*.48,-1.65,1.004),(s*.56,-.91,1.052)],.004,paint)
# Front face: paired kidney-shaped grille openings and a fine honeycomb pattern.
for s in [-1,1]:
 kidney=[(.026,.79),(.06,.875),(.18,.9),(.37,.88),(.425,.82),(.405,.675),(.33,.635),(.13,.635),(.045,.675)]
 pts=[(s*x,-2.403,z) for x,z in kidney]; panel('KidneyRecess',pts,black); line('KidneyRim',pts+[pts[0]],.013,black)
 for row in range(3):
  for col in range(5):
   x=.09+col*.061+(row%2)*.027; z=.69+row*.057
   if x>.365: continue
   pp=[(s*(x+.032*math.cos(k*math.pi/3)),-2.42,z+.028*math.sin(k*math.pi/3)) for k in range(7)]
   line('GrilleCell',pp,.0035,brake)
 # Headlight shape wraps toward fenders.
 pts=[(s*.45,-2.408,.745),(s*.79,-2.408,.755),(s*.84,-2.408,.84),(s*.76,-2.408,.882),(s*.47,-2.408,.852)]
 panel('HeadlightHousing',pts,black); line('HeadlightSeal',pts+[pts[0]],.006,brake)
 for a in [.5,.665]:
  line('DoubleLED',[(s*(a+.018),-2.43,.77),(s*a,-2.43,.824),(s*(a+.04),-2.43,.85),(s*(a+.124),-2.43,.858)],.007,lamp)
 vent=[(s*.67,-2.405,.24),(s*.83,-2.405,.27),(s*.82,-2.405,.59),(s*.76,-2.405,.62)]
 panel('SideAirCurtain',vent,black); line('VentBlade',[(s*.70,-2.42,.26),(s*.79,-2.42,.29),(s*.79,-2.42,.55)],.008,brake)
intake=[(-.58,-2.381,.25),(.58,-2.381,.25),(.65,-2.378,.50),(.53,-2.38,.60),(-.53,-2.38,.60),(-.65,-2.378,.50)]
panel('LowerIntake',intake,black); line('IntakeRim',intake+[intake[0]],.014,brake)
for z in [.29,.36,.43]: line('LowerGrilleSlat',[(-.53,-2.398,z),(.53,-2.398,z)],.009,brake)
for x in [-.48,-.30,-.15,0,.15,.30,.48]: line('LowerGrilleSupport',[(x,-2.398,.27),(x,-2.398,.49)],.006,black)
box('FrontPlate',(0,-2.411,.565),(.51,.018,.11),white,.012); box('PlateEU',(-.235,-2.423,.565),(.035,.003,.095),blue)
line('FrontSplitter',[(-.80,-2.31,.20),(-.59,-2.39,.18),(.59,-2.39,.18),(.80,-2.31,.20)],.018,black)
# Rear L signatures, diffuser, trapezoid exhausts and boot lip.
for s in [-1,1]:
 pts=[(s*.30,2.405,.76),(s*.80,2.405,.76),(s*.83,2.405,.90),(s*.77,2.405,.936),(s*.32,2.405,.89)]
 panel('TailHousing',pts,black)
 line('TailLED',[(s*.31,2.425,.786),(s*.73,2.425,.80),(s*.795,2.425,.872),(s*.80,2.425,.915)],.013,tail)
 panel('RearVent',[(s*.67,2.405,.23),(s*.84,2.405,.25),(s*.81,2.405,.58),(s*.74,2.405,.60)],black)
 line('RearReflector',[(s*.79,2.425,.33),(s*.775,2.425,.55)],.009,tail)
 ep=[(s*.52,2.406,.225),(s*.73,2.40,.225),(s*.76,2.40,.28),(s*.71,2.40,.32),(s*.55,2.406,.32),(s*.50,2.406,.27)]
 panel('ExhaustDark',ep,black); line('ExhaustChrome',ep+[ep[0]],.012,alloy)
panel('Diffuser',[(-.48,2.393,.19),(.48,2.393,.19),(.42,2.393,.40),(-.42,2.393,.40)],black)
for x in [-.4,-.22,0,.22,.4]: box('DiffuserFin',(x,2.37,.265),(.014,.17,.16),black)
box('RearPlate',(0,2.388,.69),(.51,.018,.11),white,.008)
line('BootLip',[(-.77,2.20,.951),(0,2.30,.978),(.77,2.20,.951)],.014,black)
line('TrunkSeam',[(-.60,2.38,.91),(-.58,2.38,.61),(0,2.38,.59),(.58,2.38,.61),(.60,2.38,.91)],.003,black)
# Small roundels: actual quartered blue/white badge geometry.
def badge(n,center,r,front=True):
 x,y,z=center
 for j in range(4):
  vs=[(x,y,z)]+[(x+r*math.cos((j+k/4)*math.pi/2),y,z+r*math.sin((j+k/4)*math.pi/2)) for k in range(5)]
  panel(n,vs,blue if j%2==0 else white)
 line('RoundelRing',[(x+r*1.14*math.cos(k*math.pi/8),y,z+r*1.14*math.sin(k*math.pi/8)) for k in range(17)],.004,black)
badge('FrontBMW',(0,-2.32,.935),.032); badge('RearBMW',(0,2.39,.88),.036)
def text(n,txt,p,size,m,rot):
 cu=bpy.data.curves.new(n,'FONT'); cu.body=txt; cu.size=size; cu.align_x='CENTER'; cu.extrude=.0003
 ob=bpy.data.objects.new(n,cu); bpy.context.collection.objects.link(ob); ob.location=p; ob.rotation_euler=rot; ob.data.materials.append(m); bpy.context.view_layer.objects.active=ob; ob.select_set(True); bpy.ops.object.convert(target='MESH'); ob=bpy.context.object; parts.append(ob); ob.select_set(False)
text('M340iBadge','M340i',(.53,2.389,.91),.052,alloy,(math.pi/2,0,math.pi))
text('FrontPlateText','M SE 1460',(0,-2.425,.535),.073,black,(math.pi/2,0,0))
text('RearPlateText','M SE 1460',(0,2.401,.661),.073,black,(math.pi/2,0,math.pi))
box('Sunroof',(0,.17,1.441),(1.05,.65,.008),glass,.07)
mesh('SharkFin',[(-.035,.91,1.40),(.035,.91,1.40),(0,.80,1.52),(0,.65,1.43)],[(0,1,2),(0,2,3),(1,3,2),(0,3,1)],paint)
# Wheels are joined per corner, retaining pivots at axle centres.
wheel_groups=[]
for s in [-1,1]:
 for cy,label in [(-1.4,'Front'),(1.45,'Rear')]:
  start=len(parts); cx=s*.82; cz=.345; N=24; vv=[]; ff=[]
  profile=[(-.125,.262),(-.125,.312),(-.10,.34),(.10,.34),(.125,.312),(.125,.262)]
  for dx,r in profile:
   for i in range(N): a=i*2*math.pi/N; vv.append((cx+dx,cy+r*math.sin(a),cz+r*math.cos(a)))
  for j in range(len(profile)):
   for i in range(N): ff.append((j*N+i,j*N+(i+1)%N,((j+1)%len(profile))*N+(i+1)%N,((j+1)%len(profile))*N+i))
  tire=mesh('Tire',vv,ff,rubber)
  for p in tire.data.polygons: p.use_smooth=True
  face=cx+s*.127
  cyl('RimBarrel',(cx,cy,cz),.263,.205,black)
  cyl('BrakeRotor',(face-s*.055,cy,cz),.215,.016,brake)
  box('Caliper',(face-s*.035,cy+.155,cz+.035),(.056,.066,.14),red,.012)
  for radius in [.257,.235]:
   line('RimLip',[(face,cy+radius*math.sin(i*2*math.pi/N),cz+radius*math.cos(i*2*math.pi/N)) for i in range(N+1)],.008,alloy)
  for i in range(5):
   a=i*2*math.pi/5
   for shift in [-.13,.13]:
    def wp(r,ang): return (face+s*.004,cy+r*math.sin(ang),cz+r*math.cos(ang))
    panel('SplitSpoke',[wp(.064,a+shift-.11),wp(.253,a+shift-.065),wp(.253,a+shift+.065),wp(.105,a+shift+.10)],alloy)
  cyl('Hub',(face+s*.003,cy,cz),.062,.018,black,16)
  cyl('Roundel',(face+s*.014,cy,cz),.029,.003,blue,16)
  for i in range(5):
   a=i*2*math.pi/5; cyl('Lug',(face+s*.015,cy+.043*math.sin(a),cz+.043*math.cos(a)),.007,.005,alloy,6)
  wheel_groups.append((f'Wheel_{label}_{"L" if s<0 else "R"}',parts[start:],(cx,cy,cz)))
wheelparts={ob for _,obs,_ in wheel_groups for ob in obs}
def join(obs,n,p):
 bpy.ops.object.select_all(action='DESELECT')
 for ob in obs: ob.select_set(True)
 bpy.context.view_layer.objects.active=obs[0]; bpy.ops.object.join(); ob=bpy.context.object; ob.name=n; bpy.context.scene.cursor.location=p; bpy.ops.object.origin_set(type='ORIGIN_CURSOR'); return ob
body=join([ob for ob in parts if ob not in wheelparts],'Body',(0,0,0))
wheels=[join(obs,n,p) for n,obs,p in wheel_groups]
bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.object; root.name='BMW_M340i_LowPoly'
for ob in [body]+wheels:
 ob.parent=root; bpy.context.view_layer.objects.active=ob
 bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True)
 bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.normals_make_consistent(inside=False); bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.015); bpy.ops.object.mode_set(mode='OBJECT')
 # Explicit triangulation ensures stable export topology.
 mod=ob.modifiers.new('GameTriangles','TRIANGULATE'); bpy.ops.object.modifier_apply(modifier=mod.name)
stats={ob.name:len(ob.data.polygons) for ob in [body]+wheels}; stats['total_triangles']=sum(stats.values())
bpy.context.view_layer.update()
corners=[ob.matrix_world@Vector(c) for ob in [body]+wheels for c in ob.bound_box]
stats['dimensions_m_width_length_height']=[round(max(c[i] for c in corners)-min(c[i] for c in corners),3) for i in range(3)]
(S/'model_stats.json').write_text(json.dumps(stats,indent=2))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(O/'Models/BMW_M340i_LowPoly.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
bpy.ops.export_scene.gltf(filepath=str(S/'BMW_M340i_LowPoly.glb'),export_format='GLB',use_selection=True)
bpy.ops.wm.save_as_mainfile(filepath=str(S/'BMW_M340i_LowPoly.blend'))
# Studio preview.
floor=mat('StudioFloor',(.075,.095,.12),.1,.5); box('PreviewGround',(0,0,-.06),(200,200,.1),floor)
def aim(o,p): o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for pos,power,size in [((1,-3,6),1800,5),((-4,-1,3),1200,4),((1,5,4),2200,3)]:
 bpy.ops.object.light_add(type='AREA',location=pos); ob=bpy.context.object; ob.data.energy=power; ob.data.shape='DISK'; ob.data.size=size; aim(ob,(0,0,.6))
bpy.ops.object.camera_add(location=(6,-8,3.7)); cam=bpy.context.object; aim(cam,(0,0,.7)); cam.data.type='ORTHO'; cam.data.ortho_scale=6.7
sc=bpy.context.scene; sc.camera=cam; sc.render.engine='CYCLES'; sc.cycles.samples=40; sc.world.color=(.24,.24,.24); sc.render.resolution_x=1500; sc.render.resolution_y=1000; sc.render.resolution_percentage=100
sc.render.filepath=str(S/'BMW_M340i_Front.png'); bpy.ops.render.render(write_still=True)
cam.location=(6,8,3.5); aim(cam,(0,0,.7)); sc.render.filepath=str(S/'BMW_M340i_Rear.png'); bpy.ops.render.render(write_still=True)
print('BMW_BUILD_SUCCESS',stats)
