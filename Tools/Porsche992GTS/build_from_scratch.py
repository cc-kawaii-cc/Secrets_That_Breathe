"""Original parametric 992.2 Carrera GTS study. Imports no existing vehicle meshes."""
import bpy,bmesh,math,json,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/SecretsThatBreathe/Props/Porsche992GTS';SRC=ROOT/'ArtSource/Porsche992GTS'
for d in [OUT/'Models',OUT/'Textures',SRC]:d.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
M={};objects=[];groups={}
def material(name,c,metal=0,rough=.4,emit=0,trans=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*c,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    if trans:p.inputs['Transmission Weight'].default_value=trans;p.inputs['IOR'].default_value=1.46
    if emit:p.inputs['Emission Color'].default_value=(*c,1);p.inputs['Emission Strength'].default_value=emit
    m.diffuse_color=(*c,1);M[name]=m;return m
white=material('Paint_White',(.82,.84,.86),.16,.21);black=material('GlossBlack',(.009,.012,.016),.55,.23)
rubber=material('Rubber',(.014,.016,.019),0,.82);trim=material('Trim',(.018,.022,.027),.08,.55)
alloy=material('Metal',(.40,.44,.48),.88,.29);rotor=material('BrakeRotor',(.22,.24,.26),.83,.42)
red=material('CaliperRed',(.58,.012,.018),.25,.28);glass=material('Glass',(.085,.115,.13),.05,.08,trans=.88)
lens=material('ClearLens',(.82,.86,.91),0,.07,trans=.97);led=material('LEDWhite',(.75,.88,1),.15,.16,2)
tail=material('LEDRed',(.52,.002,.006),.1,.22,1.6);leather=material('LeatherBlack',(.017,.019,.024),0,.63)
alcantara=material('Alcantara',(.027,.030,.034),0,.92);stitch=material('StitchGrey',(.15,.16,.17),0,.8)
screen=material('DisplayBlack',(.006,.009,.012),.05,.18);ui=material('DisplayWhite',(.60,.75,.8),0,.4,.7)
gold=material('BadgeGold',(.55,.35,.08),.7,.28);plate=material('Plate',(.79,.81,.82),.12,.4)
def register(o,name,m,group='Chassis',smooth=False):
    o.name=name
    if m:o.data.materials.append(m)
    if o.type=='MESH':
        for f in o.data.polygons:f.use_smooth=smooth
    objects.append(o);groups[o.name]=group;return o
def mesh(name,v,f,m,group='Chassis',smooth=True,solid=0):
    me=bpy.data.meshes.new(name);me.from_pydata([tuple(p) for p in v],[],f);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);register(o,name,m,group,smooth)
    if solid:
        mod=o.modifiers.new('Panel wall thickness','SOLIDIFY');mod.thickness=solid;mod.offset=-1
        bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.modifier_apply(modifier=mod.name);o.select_set(False)
    return o
def box(name,pos,size,m,bevel=.004,group='Chassis'):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);register(o,name,m,group)
    if bevel:
        mod=o.modifiers.new('Manufactured edge','BEVEL');mod.width=bevel;mod.segments=4;bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=o.modifiers.new('Planar normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=mod.name)
    o.select_set(False);return o
def sphere(name,pos,size,m,group='Chassis',segments=48,rings=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=pos);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);register(o,name,m,group,True);o.select_set(False);return o
def cylinder(name,pos,r,depth,m,axis=(0,0,1),group='Chassis',vertices=64):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=pos);o=bpy.context.object;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler();bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);register(o,name,m,group,True);o.select_set(False);return o
def tube(name,points,r,m,group='Chassis',sides=10,closed=False):
    pts=[Vector(p) for p in points];v=[];f=[];count=len(pts)
    for i,p in enumerate(pts):
        tangent=(pts[(i+1)%count]-pts[(i-1)%count]) if closed else pts[min(i+1,count-1)]-pts[max(0,i-1)]
        tangent.normalize();a=tangent.cross(Vector((0,0,1)))
        if a.length<.01:a=tangent.cross(Vector((0,1,0)))
        a.normalize();b=tangent.cross(a)
        for j in range(sides):v.append(p+r*(a*math.cos(j*math.tau/sides)+b*math.sin(j*math.tau/sides)))
    for i in range(count if closed else count-1):
        for j in range(sides):f.append((i*sides+j,i*sides+(j+1)%sides,((i+1)%count)*sides+(j+1)%sides,((i+1)%count)*sides+j))
    if not closed:f.extend([tuple(reversed(range(sides))),tuple(range((count-1)*sides,count*sides))])
    return mesh(name,v,f,m,group)
def text(name,body,pos,size,m,rotation=(math.pi/2,0,0),group='Chassis',extrude=.0004):
    curve=bpy.data.curves.new(name,'FONT');curve.body=body;curve.align_x='CENTER';curve.align_y='CENTER';curve.size=size;curve.extrude=extrude;curve.bevel_depth=.00015;curve.resolution_u=4
    o=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler=rotation;register(o,name,m,group);return o
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
keys=[(-2.2765,.74,.585,.625),(-2.12,.84,.655,.755),(-1.80,.881,.704,.825),(-1.225,.901,.752,.891),(-.70,.872,.824,.866),(0,.865,.824,.866),(.70,.90,.858,.906),(1.225,.926,.865,.943),(1.85,.89,.80,.861),(2.2765,.775,.74,.78)]
def cross(y):
    for i in range(len(keys)-1):
        a,b=keys[i],keys[i+1]
        if y<=b[0]:t=smooth((y-a[0])/(b[0]-a[0]));return tuple(a[k]+(b[k]-a[k])*t for k in range(1,4))
    return keys[-1][1:]
def topz(x,y):
    w,c,h=cross(y);u=min(1,abs(x)/w);return c+(h-c)*math.sin(math.pi*.5*u)**4-.028*u**16
def arch(y):
    low=.155
    for wy,r,z in [(-1.225,.372,.33975),(1.225,.397,.3612)]:
        d=y-wy
        if abs(d)<r:low=max(low,z+math.sqrt(max(0,r*r-d*d)))
    return low
def side_panel(side,y0,y1,name,group):
    ny=max(30,int((y1-y0)*66));nz=14;v=[];f=[]
    for i in range(ny+1):
        y=y0+(y1-y0)*i/ny;w,_,h=cross(y);bottom=arch(y)
        for j in range(nz+1):
            t=j/nz;z=bottom+(h-.028-bottom)*t;x=side*(w-.032*(1-t)**2-.014*math.sin(math.pi*t));v.append((x,y,z))
    for i in range(ny):
        for j in range(nz):a=i*(nz+1)+j;f.append((a,a+1,a+nz+2,a+nz+1) if side>0 else (a+nz+1,a+nz+2,a+1,a))
    return mesh(name,v,f,white,group,solid=.0025)
def top_patch(name,y0,y1,u0,u1,group='Chassis',ny=80,nx=40,cut=None):
    v=[];f=[]
    for i in range(ny+1):
        y=y0+(y1-y0)*i/ny;w=cross(y)[0]
        for j in range(nx+1):u=u0+(u1-u0)*j/nx;x=w*u;v.append((x,y,topz(x,y)))
    for i in range(ny):
        for j in range(nx):
            a=i*(nx+1)+j;c=(Vector(v[a])+Vector(v[a+nx+2]))*.5
            if cut and cut(c.x,c.y):continue
            f.append((a,a+1,a+nx+2,a+nx+1))
    return mesh(name,v,f,white,group,solid=.002)
# Three distinct original body sections; real wheel arch openings and panel gaps.
for side in [-1,1]:
    suffix='L' if side<0 else 'R';door='Door_'+suffix
    side_panel(side,-2.2765,-.642,'FrontQuarter_'+suffix,'Chassis')
    side_panel(side,-.637,.655,'DoorSkin_'+suffix,door)
    side_panel(side,.660,2.2765,'RearQuarter_'+suffix,'Chassis')
    # Shoulder ribbons continue around the cabin without filling the interior.
    top_patch('Shoulder_'+suffix,-.70,1.45,side*.76,side,ny=100,nx=18)
    tube('Sill_'+suffix,[(side*.815,-.84,.161),(side*.834,-.6,.143),(side*.842,.6,.143),(side*.88,.85,.164)],.022,black,sides=16)
def nose_cut(x,y):
    if -.2<y:return False
    return any(((x-s*.697)/.164)**2+((y+1.842)/.205)**2<.97 for s in [-1,1])
# Bonnet separated from fenders; the seam is a physical 4 mm reveal.
top_patch('Bonnet',-2.065,-.705,-.60,.60,'Bonnet',ny=88,nx=64)
top_patch('NoseBridge',-2.2765,-2.070,-1,1,ny=25,nx=90,cut=nose_cut)
for side in [-1,1]:top_patch('FrontFenderTop_'+str(side),-2.070,-.705,side*.605,side,ny=90,nx=32,cut=nose_cut)
top_patch('RearDeck',1.445,2.2765,-1,1,'EngineCover',ny=64,nx=84)
# Roof and glass patch surfaces. A separate roof skin leaves an actual cabin void.
def param_patch(name,func,m,group='Chassis',nu=36,nv=30,solid=.002):
    v=[func(i/nu,j/nv) for i in range(nu+1) for j in range(nv+1)];f=[]
    for i in range(nu):
        for j in range(nv):a=i*(nv+1)+j;f.append((a,a+nv+1,a+nv+2,a+1))
    return mesh(name,v,f,m,group,solid=solid)
def windsurf(u,t):
    x=(2*u-1)*(.676*(1-t)+.551*t);y=-.737*(1-t)-.205*t-.035*math.sin(math.pi*u)*math.sin(math.pi*t);z=.858*(1-t)+1.235*t+.024*math.sin(math.pi*u);return(x,y,z)
windshield=param_patch('Windshield',windsurf,glass,solid=.004)
param_patch('Roof',lambda u,t:((2*u-1)*(.551+.014*math.sin(t*math.pi)),-.205+.77*t,1.296-.061*(2*u-1)**2-.008*math.sin(t*math.pi)**2),white,nu=64,nv=50,solid=.014)
def rearsurf(u,t):return((2*u-1)*(.55*(1-t)+.696*t),.565+.88*t,1.235*(1-t)+.901*t+.028*math.sin(math.pi*u))
rearwindow=param_patch('RearWindow',rearsurf,glass,solid=.004)
for func,name in [(windsurf,'WindshieldSeal'),(rearsurf,'RearWindowSeal')]:
    points=[func(i/40,0) for i in range(41)]+[func(1,i/40) for i in range(1,41)]+[func(1-i/40,1) for i in range(1,41)]+[func(0,1-i/40) for i in range(1,40)]
    tube(name,points,.006,trim,sides=10,closed=True)
for side in [-1,1]:
    suffix='L' if side<0 else 'R';door='Door_'+suffix
    a=Vector((side*.704,-.667,.858));b=Vector((side*.551,-.205,1.235));c=Vector((side*.565,.555,1.235));d=Vector((side*.774,.655,.874))
    param_patch('DoorGlass_'+suffix,lambda u,t:a.lerp(d,u).lerp(b.lerp(c,u),t)+Vector((side*.012*math.sin(u*math.pi)*math.sin(t*math.pi),0,0)),glass,door,nu=28,nv=20,solid=.003)
    tube('A_Pillar_'+suffix,[a,b],.020,white,sides=18)
    tube('B_Pillar_'+suffix,[d,c],.017,black,sides=16)
    e=Vector((side*.708,1.35,.905));param_patch('QuarterGlass_'+suffix,lambda u,t:c.lerp(e,u).lerp(d.lerp(e,u),t),glass,nu=25,nv=20,solid=.003)
    tube('C_Pillar_'+suffix,[c,c.lerp(e,.34)+Vector((side*.028,0,.008)),e],.047,white,sides=20)
    tube('WindowBelt_'+suffix,[a,d,e],.010,black,door,sides=12)
    tube('DoorHandleRecess_'+suffix,[(side*.875,.35,.72),(side*.875,.49,.72)],.014,black,door,sides=12)
    box('FlushHandle_'+suffix,(side*.879,.42,.726),(.015,.13,.019),white,.006,door)
    stem=tube('MirrorStem_'+suffix,[(side*.74,-.59,.855),(side*.86,-.59,.91),(side*.935,-.54,.945)],.018,black,door,sides=16)
    sphere('MirrorCap_'+suffix,(side*.939,-.52,.956),(.076,.126,.047),white,door)
    sphere('MirrorGlass_'+suffix,(side*.94,-.425,.952),(.063,.006,.033),alloy,door)
# Front/rear fascia shells with intake apertures, not painted-on black rectangles.
def fascia(front):
    nx,nz=112,48;v=[];f=[]
    for i in range(nx+1):
        x=-.84+1.68*i/nx
        for j in range(nz+1):
            z=.155+.49*j/nz;y=(-2.2765+.11*(abs(x)/.84)**2) if front else (2.2765-.12*(abs(x)/.84)**2)
            y+=(-1 if front else 1)*.022*math.sin(j/nz*math.pi);v.append((x,y,z))
    for i in range(nx):
        for j in range(nz):
            a=i*(nz+1)+j;p=(Vector(v[a])+Vector(v[a+nz+2]))*.5
            hole=front and ((.305<abs(p.x)<.810 and .232<p.z<.492) or (abs(p.x)<.28 and .20<p.z<.355))
            if not hole:f.append((a,a+nz+1,a+nz+2,a+1) if front else (a+1,a+nz+2,a+nz+1,a))
    return mesh('FrontFascia' if front else 'RearFascia',v,f,white,solid=.004)
fascia(True);fascia(False)
for side in [-1,1]:
    # Recess walls extend into the bumper behind the visible flap faces.
    x0,x1=side*.304,side*.812
    mesh('IntakeDuct_'+str(side),[(x0,-2.20,.23),(x1,-2.15,.23),(x1,-2.15,.495),(x0,-2.20,.495),(x0,-2.02,.25),(x1,-2.02,.25),(x1,-2.02,.47),(x0,-2.02,.47)],[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],trim,solid=.003)
    for j in range(5):
        x=side*(.351+j*.097);foil=box('ActiveCoolingFlap_'+str(side)+'_'+str(j+1),(x,-2.225+.095*(abs(x)/.84)**2,.363),(.074,.054,.248),black,.018);foil.rotation_euler.z=side*.14
    for j in range(9):box('RadiatorFin_'+str(side)+'_'+str(j),(side*.56,-2.015,.25+j*.026),(.43,.012,.005),alloy,.001)
box('CentralIntake',(0,-2.16,.275),(.55,.07,.15),trim,.02)
box('RadarSensor',(0,-2.293,.285),(.17,.009,.062),black,.008)
mesh('FrontSplitter',[(-.84,-2.215,.158),(-.7,-2.314,.148),(.7,-2.314,.148),(.84,-2.215,.158),(.77,-2.07,.14),(-.77,-2.07,.14)],[(0,1,2,3,4,5)],black,solid=.012)
# Matrix lamps: four light modules in each recessed elliptical housing.
for side in [-1,1]:
    center=Vector((side*.697,-1.842,topz(side*.697,-1.842)+.012));normal=Vector((0,-.68,.733)).normalized();h=Vector((1,0,0));v=normal.cross(h).normalized()
    rim=[center+h*.163*math.cos(i*math.tau/96)+v*.193*math.sin(i*math.tau/96) for i in range(96)]
    tube('HeadlightRim_'+str(side),rim,.0065,alloy,sides=12,closed=True)
    sphereOb=sphere('HeadlightHousing_'+str(side),center-normal*.025,(.156,.047,.19),black,segments=64,rings=32);sphereOb.rotation_euler.x=math.atan2(-normal.y,normal.z)
    for a in [-1,1]:
        for b in [-1,1]:
            pos=center+h*(a*.058)+v*(b*.066)
            cylinder('MatrixProjector_'+str(side)+str(a)+str(b),pos,.027,.024,alloy,normal,vertices=48)
            cylinder('LEDModule_'+str(side)+str(a)+str(b),pos+normal*.013,.020,.005,led,normal,vertices=48)
    # Bulged outer lens is a separate transparent surface.
    param_patch('HeadlightLens_'+str(side),lambda u,t:center+h*(.156*math.cos(u*math.tau)*math.sin(t*math.pi/2))+v*(.187*math.sin(u*math.tau)*math.sin(t*math.pi/2))+normal*(.032*math.cos(t*math.pi/2)),lens,nu=96,nv=24,solid=.002)
# Distinct 992.2 rear band, deck grille and retracted adaptive spoiler.
rearline=[(x,2.275-.17*(abs(x)/.87)**2,.755+.015*(abs(x)/.87)) for x in [-.87+i*1.74/120 for i in range(121)]]
tube('RearLampBlackBezel',rearline,.021,black,sides=16);tube('RearLightBand',[(x,y+.014,z) for x,y,z in rearline],.009,tail,sides=16)
text('RearPORSCHE','P O R S C H E',(0,2.297,.811),.049,alloy,rotation=(math.pi/2,0,math.pi))
text('CarreraGTSBadge','carrera GTS',(0,2.29,.686),.040,black,rotation=(math.pi/2,0,math.pi))
for side in [-1,1]:
    for j in range(5):
        x=side*(.14+j*.085);tube('EngineDeckStrake_'+str(side)+'_'+str(j),[(x,y,topz(x,y)+.012) for y in [1.48+i*.35/16 for i in range(17)]],.013,black,sides=12)
    tube('ThirdBrakeLight_'+str(side),[(side*.048,y,topz(side*.048,y)+.018) for y in [1.50+i*.25/12 for i in range(13)]],.006,tail,sides=10)
    for j in range(2):
        x=side*(.30+j*.015)
    tube('RearReflector_'+str(side),[(side*.65,2.20,.38),(side*.78,2.15,.38)],.009,tail,sides=12)
box('RearDiffuser',(0,2.16,.223),(1.48,.22,.155),black,.026)
for x in [-.60,-.30,0,.30,.60]:box('DiffuserFin_'+str(x),(x,2.16,.135),(.014,.27,.078),black,.004)
for side in [-1,1]:
    # Open annular exhaust tip: no cap over the bore.
    n=80;v=[];f=[];cx=side*.275
    for y,r in [(2.19,.061),(2.30,.061),(2.30,.050),(2.18,.050)]:
        for j in range(n):a=j*math.tau/n;v.append((cx+math.cos(a)*r*1.15,y,.25+math.sin(a)*r*.84))
    for k in range(3):
        for j in range(n):f.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    mesh('ExhaustTip_'+str(side),v,f,alloy);cylinder('ExhaustDarkBore_'+str(side),(cx,2.175,.25),.050,.005,trim,(0,1,0))
spoiler=box('AdaptiveSpoiler',(0,1.99,.846),(1.43,.18,.028),white,.012,'Spoiler')
for front,y in [(True,-2.293),(False,2.288)]:
    box('NumberPlateFront' if front else 'NumberPlateRear',(0,y,.463),(.43,.014,.105),plate,.006)
    text('PlateTextFront' if front else 'PlateTextRear','992.2 GTS',(0,y+(-.009 if front else .009),.463),.052,black,rotation=(math.pi/2,0,0 if front else math.pi))
badge=mesh('BonnetCrest',[(-.022,-2.02,topz(-.022,-2.02)+.003),(.022,-2.02,topz(.022,-2.02)+.003),(.019,-1.967,topz(.019,-1.967)+.004),(0,-1.953,topz(0,-1.953)+.004),(-.019,-1.967,topz(-.019,-1.967)+.004)],[(0,1,2,3,4)],gold,solid=.001)
# Wheel meshes are built around their own axle pivots, including genuine rotor perforations.
def annulus(name,cx,y,z,rin,rout,depth,m,group,n=128):
    vs=[];fs=[]
    for x,r in [(cx-depth/2,rin),(cx-depth/2,rout),(cx+depth/2,rout),(cx+depth/2,rin)]:
        for j in range(n):a=j*math.tau/n;vs.append((x,y+r*math.cos(a),z+r*math.sin(a)))
    for k in range(4):
        for j in range(n):fs.append((k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j))
    return mesh(name,vs,fs,m,group)
for front,wy,r,width,rr in [(True,-1.225,.33975,.245,.254),(False,1.225,.3612,.315,.2667)]:
    for side in [-1,1]:
        suffix=('F' if front else 'R')+('L' if side<0 else 'R');group='Wheel_'+suffix;wx=side*(.794 if front else .765);z=r
        # Closed lathed tyre with three circumferential grooves and angled tread sipes.
        profile=[(-.46,.755),(-.53,.82),(-.54,.89),(-.49,.965)]
        profile += [(a,.998) for a in [-.40+i*.8/40 for i in range(41)]]
        profile += [(.49,.965),(.54,.89),(.53,.82),(.46,.755)]
        vs=[];fs=[];n=224
        for j in range(n):
            theta=j*math.tau/n
            for ax,rad in profile:
                groove=sum(.006*math.exp(-((ax-g)/.019)**6) for g in [-.245,0,.245]) if abs(ax)<.42 else 0
                sipe=.0016*(max(0,math.cos(theta*70+ax*24))**12) if abs(ax)<.42 else 0
                radius=r*rad-groove-sipe;vs.append((wx+ax*width,wy+math.cos(theta)*radius,z+math.sin(theta)*radius))
        pcount=len(profile)
        for j in range(n):
            for k in range(pcount):fs.append((j*pcount+k,j*pcount+(k+1)%pcount,((j+1)%n)*pcount+(k+1)%pcount,((j+1)%n)*pcount+k))
        mesh('Tyre_'+suffix,vs,fs,rubber,group)
        outer=wx+side*width*.435
        annulus('WheelBarrel_'+suffix,wx,wy,z,rr*.90,rr,width*.88,black,group)
        annulus('RimLip_'+suffix,outer,wy,z,rr*.935,rr*.98,.015,black,group)
        for i in range(5):
            a=i*math.tau/5+.15
            # Split five-spoke design, with machined chamfers and open gaps.
            for fork in [-1,1]:
                points=[]
                for radius,angle,w in [(.045,a,.022),(.135,a,.020),(rr*.94,a+fork*.12,.014)]:
                    center=Vector((outer,wy+math.cos(angle)*radius,z+math.sin(angle)*radius));t=Vector((0,-math.sin(angle),math.cos(angle)))
                    points.extend([center-t*w/2,center+t*w/2])
                v=[p+Vector((dx,0,0)) for dx in [-.012,.012] for p in points]
                f=[(0,1,3,2),(2,3,5,4),(6,8,9,7),(8,10,11,9),(0,6,7,1),(4,5,11,10),(0,2,8,6),(2,4,10,8),(1,7,9,3),(3,9,11,5)]
                o=mesh('SplitSpoke_'+suffix+str(i)+str(fork),v,f,black,group)
                mod=o.modifiers.new('Spoke chamfer','BEVEL');mod.width=.0023;mod.segments=3;bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.modifier_apply(modifier=mod.name);o.select_set(False)
        cylinder('CenterLock_'+suffix,(outer+side*.018,wy,z),.040,.027,black,(1,0,0),group,vertices=12)
        cylinder('CenterLockCap_'+suffix,(outer+side*.033,wy,z),.018,.004,gold,(1,0,0),group,vertices=48)
        rotorx=wx+side*width*.24;rotorr=.204 if front else .190
        disc=annulus('DrilledBrakeRotor_'+suffix,rotorx,wy,z,.070,rotorr,.018,rotor,'Chassis',n=160)
        cutters=[]
        for ringR,count in [(rotorr*.58,12),(rotorr*.77,18),(rotorr*.91,22)]:
            for k in range(count):
                a=k*math.tau/count+ringR*5
                bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.0032,depth=.05,location=(rotorx,wy+math.cos(a)*ringR,z+math.sin(a)*ringR),rotation=(0,math.pi/2,0));cutters.append(bpy.context.object)
        bpy.ops.object.select_all(action='DESELECT')
        for c in cutters:c.select_set(True)
        bpy.context.view_layer.objects.active=cutters[0];bpy.ops.object.join();cutter=bpy.context.object
        bpy.context.view_layer.objects.active=disc;mod=disc.modifiers.new('Through-drilled rotor holes','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cutter
        try:bpy.ops.object.modifier_apply(modifier=mod.name)
        except RuntimeError:disc.modifiers.remove(mod)
        bpy.data.objects.remove(cutter,do_unlink=True)
        cylinder('RotorHat_'+suffix,(rotorx,wy,z),.074,.027,trim,(1,0,0),vertices=64)
        cal=box('BrakeCaliper_'+suffix,(rotorx+side*.008,wy+.15,z+.02),(.065,.080,.205 if front else .175),red,.025)
        for zz in [-.052,0,.052]:cylinder('CaliperPiston_'+suffix+str(zz),(rotorx+side*.035,wy+.15,z+zz),.027,.012,red,(1,0,0),vertices=32)
        # Valve and moulded sidewall lettering are real geometry.
        cylinder('TyreValve_'+suffix,(outer,wy-.04,z+rr*.87),.004,.025,trim,(side,0,0),group,vertices=12)
        tirelabel=text('SidewallType_'+suffix,'245/35 ZR20' if front else '315/30 ZR21',(outer+side*.008,wy,z+r*.90),.018,trim,rotation=(math.pi/2,0,side*math.pi/2),group=group)
        # Visible strut/spring and wishbones, not a full mechanical suspension simulation.
        cylinder('Damper_'+suffix,(wx-side*.12,wy,z+.18),.028,.32,alloy,vertices=32)
        coil=[]
        for k in range(193):a=k*math.tau/24;coil.append((wx-side*.12+math.cos(a)*.043,wy+math.sin(a)*.043,z+.08+k/192*.22))
        tube('Spring_'+suffix,coil,.006,black,sides=8)
        for dy in [-.14,.14]:tube('Wishbone_'+suffix+str(dy),[(wx-side*.05,wy,z-.12),(wx-side*.33,wy+dy,z-.13)],.019,alloy,sides=12)
# Underfloor and rear engine bay: enclosed chassis, visual mechanical detail only.
box('Undertray',(0,.12,.143),(1.48,3.48,.035),trim,.025)
for y in [-.85,.15,.85]:box('ChassisCrossmember_'+str(y),(0,y,.19),(1.45,.07,.06),alloy,.01)
box('BoxerEngineCrankcase',(0,1.40,.40),(.75,.69,.31),alloy,.07)
for side in [-1,1]:
    box('CylinderHead_'+str(side),(side*.46,1.40,.40),(.23,.58,.19),alloy,.045)
    for j in range(3):
        box('CamCover_'+str(side)+str(j),(side*.56,1.20+j*.19,.44),(.07,.14,.13),black,.02)
        tube('ExhaustPrimary_'+str(side)+str(j),[(side*.53,1.19+j*.19,.33),(side*.64,1.30+j*.18,.25),(side*.4,1.88,.24)],.019,alloy,sides=12)
box('Transmission',(0,.77,.31),(.40,.56,.24),alloy,.055)
cylinder('eTurbo',(0,1.81,.45),.13,.20,alloy,(1,0,0),vertices=64)
box('HVPowerUnit',(0,1.41,.64),(.62,.45,.095),trim,.02)
box('FrontHybridBattery',(0,-1.20,.45),(.57,.39,.18),trim,.02,'Bonnet')
for side in [-1,1]:tube('CoolingLine_'+str(side),[(side*.60,-1.8,.25),(side*.66,-.6,.22),(side*.66,.9,.24),(side*.48,1.4,.61)],.010,trim,sides=10)
# Black right-hand-drive cabin, front two seats and carpeted rear shelf.
box('CabinFloor',(0,.12,.263),(1.34,1.90,.042),alcantara,.025)
box('RearLuggageShelf',(0,.94,.59),(1.30,.66,.14),alcantara,.04)
for side in [-1,1]:
    sx=side*.365;group='Interior'
    box('SeatRail_'+str(side),(sx,.04,.29),(.34,.53,.028),alloy,.004,group)
    sphere('SeatCushion_'+str(side),(sx,.03,.406),(.204,.264,.091),leather,group)
    box('SeatCenterPad_'+str(side),(sx,-.005,.468),(.235,.37,.027),alcantara,.015,group)
    back=sphere('SeatBack_'+str(side),(sx,.258,.779),(.205,.100,.329),leather,group);back.rotation_euler.x=math.radians(12)
    centerpad=box('SeatBackInsert_'+str(side),(sx,.151,.799),(.24,.033,.40),alcantara,.035,group);centerpad.rotation_euler.x=math.radians(12)
    sphere('Headrest_'+str(side),(sx,.306,1.08),(.130,.073,.11),leather,group)
    for sign in [-1,1]:
        sphere('SeatBolster_'+str(side)+str(sign),(sx+sign*.154,.156,.77),(.047,.108,.25),leather,group)
        tube('SeatSeam_'+str(side)+str(sign),[(sx+sign*.11,.112,.56),(sx+sign*.115,.11,.75),(sx+sign*.10,.17,.99)],.0018,stitch,group,sides=8)
    for j in range(7):tube('CushionStitch_'+str(side)+str(j),[(sx-.10,-.15+j*.045,.488),(sx+.10,-.15+j*.045,.488)],.0010,stitch,group,sides=6)
    box('SeatbeltBuckle_'+str(side),(sx-side*.19,.1,.43),(.045,.065,.05),trim,.008,group)
    box('BuckleButton_'+str(side),(sx-side*.19,.093,.46),(.03,.03,.009),red,.003,group)
    # Shaped interior door panel is carried by the moving door.
    door='Door_'+('L' if side<0 else 'R')
    box('DoorCard_'+str(side),(side*.752,.0,.58),(.032,1.08,.35),leather,.055,door)
    tube('DoorGrab_'+str(side),[(side*.718,-.18,.65),(side*.705,-.03,.56),(side*.72,.20,.56)],.024,leather,door,sides=16)
    box('DoorRelease_'+str(side),(side*.722,-.27,.70),(.014,.13,.022),alloy,.004,door)
    for j in range(6):box('DoorSpeakerSlat_'+str(side)+str(j),(side*.725,.27+j*.025,.46),(.009,.008,.14),trim,.002,door)
    tube('BeltWebbing_'+str(side),[(side*.64,.62,1.00),(side*.53,.32,.55),(side*.22,.12,.42)],.012,trim,group,sides=4)
dash=box('Dashboard',(0,-.51,.797),(1.38,.33,.17),leather,.065,'Interior')
box('DashLower',(0,-.45,.65),(1.31,.22,.14),leather,.04,'Interior')
tube('DashSilverTrim',[(-.66,-.324,.754),(.66,-.324,.754)],.006,alloy,'Interior',sides=10)
# Five-gauge digital cluster, new 992.2 start button, PCM and console toggles.
box('DigitalClusterHood',(.36,-.48,.88),(.48,.18,.18),leather,.06,'Interior')
box('DigitalClusterScreen',(.36,-.378,.884),(.42,.010,.113),screen,.018,'Interior')
for k in range(5):
    x=.36+(k-2)*.075;radius=.046 if k==2 else .032
    tube('GaugeRing_'+str(k),[(x+math.cos(i*math.tau/64)*radius,-.369,.884+math.sin(i*math.tau/64)*radius) for i in range(64)],.0014,ui,'Interior',sides=6,closed=True)
    for j in range(9):
        a=math.radians(-130+j*32);tube('GaugeTick_'+str(k)+str(j),[(x+math.sin(a)*radius*.82,-.367,.884+math.cos(a)*radius*.82),(x+math.sin(a)*radius*.96,-.367,.884+math.cos(a)*radius*.96)],.0007,ui,'Interior',sides=4)
text('GaugeSpeed','0',(.36,-.362,.885),.026,ui,rotation=(math.pi/2,0,math.pi),group='Interior')
box('PCMDisplay',(-.13,-.330,.828),(.30,.012,.15),screen,.008,'Interior')
text('PCMLabel','PORSCHE',(-.13,-.320,.845),.020,ui,rotation=(math.pi/2,0,math.pi),group='Interior')
for j in range(4):box('PCMIcon_'+str(j),(-.235+j*.070,-.319,.792),(.032,.002,.018),ui,.002,'Interior')
for x in [-.27,-.09,.52]:
    box('DashVent_'+str(x),(x,-.323,.697),(.15,.014,.062),trim,.004,'Interior')
    for j in range(4):box('VentLouvre_'+str(x)+str(j),(x,-.312,.679+j*.012),(.13,.009,.003),black,.001,'Interior')
for j in range(5):box('ConsoleToggle_'+str(j),(-.24+j*.06,-.300,.737),(.040,.027,.013),alloy,.003,'Interior')
box('CenterConsole',(0,-.005,.46),(.225,.72,.18),leather,.04,'Interior')
box('ConsoleTop',(0,-.08,.563),(.182,.46,.016),black,.015,'Interior')
box('PDKSelector',(0,-.22,.596),(.032,.04,.051),alloy,.008,'Interior')
for side in [-1,1]:box('ConsoleSwitch_'+str(side),(side*.06,-.22,.580),(.022,.05,.008),trim,.002,'Interior')
for y in [.015,.17]:
    tube('CupHolder_'+str(y),[(math.cos(j*math.tau/64)*.043,y+math.sin(j*math.tau/64)*.043,.573) for j in range(64)],.005,alloy,'Interior',sides=8,closed=True)
box('ArmRest',(0,.27,.56),(.19,.27,.087),leather,.027,'Interior')
# Steering rim and spokes stand apart from the dashboard for a proper cockpit view.
steerCenter=Vector((.365,-.205,.827));steerNormal=Vector((0,.94,.342));steerU=Vector((1,0,0));steerV=steerNormal.cross(steerU).normalized()
tube('SteeringRim',[steerCenter+steerU*math.cos(j*math.tau/128)*.174+steerV*math.sin(j*math.tau/128)*.174 for j in range(128)],.018,leather,'Steering',sides=16,closed=True)
cylinder('SteeringHub',steerCenter,.073,.045,leather,steerNormal,'Steering',vertices=64)
for a in [0,math.pi,math.pi*1.5]:
    tube('SteeringSpoke_'+str(a),[steerCenter+(steerU*math.cos(a)+steerV*math.sin(a))*.052,steerCenter+(steerU*math.cos(a)+steerV*math.sin(a))*.156],.015,alloy,'Steering',sides=8)
cylinder('WheelCrest',steerCenter+steerNormal*.026,.018,.003,gold,steerNormal,'Steering',vertices=32)
cylinder('DriveModeDial',steerCenter+steerU*.09-steerV*.06+steerNormal*.018,.017,.012,trim,steerNormal,'Steering',vertices=32)
cylinder('StartButton',(.62,-.314,.775),.014,.009,black,(0,1,0),'Interior',vertices=32)
for x in [.23,.36,.47]:box('Pedal_'+str(x),(x,-.62,.34),(.065,.025,.11),alloy,.008,'Interior')
for side in [-1,1]:tube('Wiper_'+str(side),[(side*.16,-.72,.875),(side*.31,-.68,.905),(side*.47,-.67,.91)],.005,trim,sides=8)
box('RearViewMirror',(0,-.21,1.18),(.23,.045,.063),trim,.015,'Interior')
box('RearViewMirrorFace',(0,-.182,1.181),(.208,.004,.047),alloy,.008,'Interior')
for side in [-1,1]:box('SunVisor_'+str(side),(side*.30,-.16,1.203),(.30,.12,.018),leather,.01,'Interior')

# Exportable hierarchy; visible motion components retain dedicated pivots.
root=bpy.data.objects.new('Porsche_911_992_2_Carrera_GTS',None);bpy.context.collection.objects.link(root)
pivots={'Door_L':(-.80,-.635,.76),'Door_R':(.80,-.635,.76),'Bonnet':(0,-.70,.82),'EngineCover':(0,1.445,.90),'Spoiler':(0,1.99,.846),'Steering':tuple(steerCenter),'Interior':(0,0,0),'Chassis':(0,0,0)}
for front,y,r in [(True,-1.225,.33975),(False,1.225,.3612)]:
    for side in [-1,1]:pivots['Wheel_'+('F' if front else 'R')+('L' if side<0 else 'R')]=(side*(.794 if front else .765),y,r)
parents={}
for group,pivot in pivots.items():
    o=bpy.data.objects.new(group,None);bpy.context.collection.objects.link(o);o.parent=root;o.location=pivot;parents[group]=o
bpy.context.view_layer.update()
for o in objects:
    group=groups[o.name];matrix=o.matrix_world.copy();o.parent=parents[group];o.matrix_world=matrix
    if o.type=='FONT':
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');o.select_set(False)
    if o.type=='MESH':
        bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
        # Explicit finite UVs for all geometry; separate lightmap UVs are generated in Unity.
        if not o.data.uv_layers:
            uv=o.data.uv_layers.new(name='UVMap')
            for loop in o.data.loops:
                p=o.data.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(p.x,p.y)
        o.data.uv_layers.active_index=0;o.data.uv_layers[0].active_render=True
bpy.context.view_layer.update()
report={'source':'Created from scratch with original parametric geometry. No existing vehicle mesh imported.','variant':'992.2 Carrera GTS Coupe, RHD','nominal_dimensions_m':{'length':4.553,'body_width':1.852,'height':1.296,'wheelbase':2.45},'paint':'White','wheels':'Black split five-spoke, 20-inch front / 21-inch rear','objects':len(objects),'triangles':0,'groups':{},'limitations':['Reference-based visual reconstruction, not OEM CAD or a dimension-certified scan.','Mechanical parts are visual representations, not complete engineering internals.','No driving physics, collision simulation, or exact opening-clearance guarantee yet.']}
for group in pivots:
    obs=[o for o in objects if groups[o.name]==group and o.type=='MESH'];tri=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in obs);report['groups'][group]={'meshes':len(obs),'triangles':tri,'pivot':pivots[group]};report['triangles']+=tri
(SRC/'geometry_report.json').write_text(json.dumps(report,indent=2))
bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
for p in parents.values():p.select_set(True)
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Porsche_992_2_GTS_FromScratch.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/Porsche_992_2_GTS.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
floor=material('StudioFloor',(.11,.12,.14),0,.65);box('StudioFloor',(0,0,-.035),(200,200,.06),floor)
for pos,power,size in [((-4,-3,6),1100,5),((4,1,4),1000,4),((0,5,5),1500,4)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.size=size;aim(o,(0,0,.7))
bpy.ops.object.camera_add(location=(4.9,-6.7,3.25));camera=bpy.context.object;camera.data.type='ORTHO';camera.data.ortho_scale=5.8;aim(camera,(0,0,.68));scene.camera=camera
scene.world.color=(.25,.25,.25);scene.render.engine='CYCLES';scene.cycles.samples=48;scene.render.resolution_x=1500;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
for name,pos in [('Front',(4.9,-6.7,3.25)),('Rear',(-4.9,6.7,3.25)),('Side',(6.5,0,2.0))]:
    camera.location=pos;aim(camera,(0,0,.68));scene.render.filepath=str(SRC/f'Porsche992GTS_{name}.png');bpy.ops.render.render(write_still=True)
camera.data.type='PERSP';camera.data.lens=22;camera.location=(.38,.50,1.14);aim(camera,(.06,-.46,.78));scene.render.filepath=str(SRC/'Porsche992GTS_Interior.png');bpy.ops.render.render(write_still=True)
print(json.dumps(report))
