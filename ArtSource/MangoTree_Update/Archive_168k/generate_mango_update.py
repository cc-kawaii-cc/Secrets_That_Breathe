"""Original orchard mango geometry, metres, independent of all earlier mango models.
Run with Blender 5.2 --background --factory-startup --python this_file.
The supplied textures are used without repainting; leaves already have alpha.
"""
import bpy
import math
import random
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/SecretsThatBreathe/Environment/MangoTree_Update'
SOURCE = ROOT / 'ArtSource/MangoTree_Update'
for p in [OUT / 'Models', SOURCE]:
    p.mkdir(parents=True, exist_ok=True)
R = random.Random(381017)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def image(name, normal=False):
    im = bpy.data.images.load(str(OUT / 'Textures' / name))
    if normal:
        im.colorspace_settings.name = 'Non-Color'
    im.pack()
    return im

def material(name, color=None, tex=None, normal=None, alpha=False, roughness=.6):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Roughness'].default_value = roughness
    if color:
        bs.inputs['Base Color'].default_value = (*color, 1)
    if tex:
        node = m.node_tree.nodes.new('ShaderNodeTexImage')
        node.image = tex
        m.node_tree.links.new(node.outputs['Color'], bs.inputs['Base Color'])
        if alpha:
            m.node_tree.links.new(node.outputs['Alpha'], bs.inputs['Alpha'])
            m.surface_render_method = 'DITHERED'
            m.use_transparency_overlap = False
    if normal:
        node = m.node_tree.nodes.new('ShaderNodeTexImage')
        node.image = normal
        nm = m.node_tree.nodes.new('ShaderNodeNormalMap')
        nm.inputs['Strength'].default_value = .65
        m.node_tree.links.new(node.outputs['Color'], nm.inputs['Color'])
        m.node_tree.links.new(nm.outputs['Normal'], bs.inputs['Normal'])
    return m

bark = material('MangoUpdate_Bark', tex=image('MangoUpdate_Bark_BaseColor.png'),
                normal=image('MangoUpdate_Bark_Normal.png', True), roughness=.85)
leaf = material('MangoUpdate_Leaves', tex=image('MangoUpdate_Leaves_Atlas.png'), alpha=True, roughness=.58)
leaf.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.24
fruit = material('MangoUpdate_Fruit_Green', color=(.24, .43, .045), roughness=.44)
# Subtle skin pores on the source preview; the Unity material uses a baked green map.
bs = fruit.node_tree.nodes.get('Principled BSDF')
noise = fruit.node_tree.nodes.new('ShaderNodeTexNoise')
noise.inputs['Scale'].default_value = 130
bump = fruit.node_tree.nodes.new('ShaderNodeBump')
bump.inputs['Strength'].default_value = .13
bump.inputs['Distance'].default_value = .008
fruit.node_tree.links.new(noise.outputs['Fac'], bump.inputs['Height'])
fruit.node_tree.links.new(bump.outputs['Normal'], bs.inputs['Normal'])

wood, blades, mangos = [], [], []
UP = Vector((0, 0, 1))

def polar(radius, angle, z):
    return Vector((radius * math.cos(angle), radius * math.sin(angle), z))

def limb(points, radii, sides=8):
    wood.append(([Vector(p) for p in points], radii, sides))

def shoot(start, end, radius=.013):
    middle = start.lerp(end, .54) + UP * .06
    limb([start, middle, end], [radius, radius*.55, .0022], 6)
    return middle

def foliage(origin, outward, count=11, scale=1):
    outward = outward.normalized()
    for k in range(count):
        a = k * 2.39996 + R.uniform(-.3, .3)
        d = (Vector((math.cos(a), math.sin(a), R.uniform(-1.2, .05))) * .8
             + outward * .32).normalized()
        base = origin + outward * R.uniform(-.06, .05)
        length = R.uniform(.43, .60) * scale
        # Rectangular curved cards retain the supplied leaf silhouette in alpha.
        blades.append((base, d, length, length * R.uniform(.46,.54),
                       R.uniform(-.85,.85), R.randrange(8), R.uniform(.07,.17)))

def bunch(anchor, count):
    stalk = anchor + UP * -.12
    limb([anchor, stalk], [.0055,.0035], 5)
    for k in range(count):
        a = k*2.39996 + R.random()
        spread = polar(R.uniform(.035,.15),a,-R.uniform(.14,.35))
        end = stalk + spread
        limb([stalk, stalk.lerp(end,.45), end], [.0035,.0028,.0018], 5)
        mangos.append((end, R.uniform(.28,.39), a, R.uniform(.91,1.09), R.uniform(-.17,.17)))

# Grounded root flare and a gently leaning single trunk, with forks above 1.7m.
trunk = [Vector(p) for p in [(0,0,-.045),(-.05,.02,.3),(-.10,.03,.95),
          (.01,0,1.65),(.17,.04,2.28),(.27,.02,2.94),(.31,.06,3.55)]]
limb(trunk,[.285,.235,.205,.178,.145,.095,.047],16)
for k in range(7):
    a = k*math.tau/7 + .21
    limb([Vector((-.025,.01,.18)),polar(.32,a,.035),polar(R.uniform(.55,.85),a,-.018)],
         [.10,.064,.007],8)

anchors = []
for k in range(8):
    a = k*math.tau/8 + R.uniform(-.17,.17)
    base = trunk[3].lerp(trunk[5], k/11)
    elbow = polar(R.uniform(.65,1.0), a, R.uniform(2.8,3.4))
    fork = polar(R.uniform(1.6,2.0),a+R.uniform(-.1,.1),R.uniform(4.15,4.8))
    limb([base,base.lerp(elbow,.48)+UP*.15,elbow,elbow.lerp(fork,.6)+UP*.15,fork],
         [.117,.10,.074,.048,.026],11)
    for j in range(6):
        az = a + (j-2.5)*.25 + R.uniform(-.15,.15)
        rad = R.uniform(2.05,2.78)
        z = 6.45 - .22*rad*rad + R.uniform(-.27,.27)
        if j in (0,5):
            z -= R.uniform(.8,1.15)
        tip = polar(rad,az,z)
        start = elbow.lerp(fork,R.uniform(.48,.94))
        mid = start.lerp(tip,.6)+UP*.14
        limb([start,mid,tip],[.030,.017,.006],8)
        foliage(mid,tip-start,10,.94)
        for t in range(4):
            end = tip+polar(R.uniform(.22,.50),az+R.uniform(-1.1,1.1),R.uniform(-.27,.32))
            twig_start = mid.lerp(tip,R.uniform(.38,.90))
            twig_mid = shoot(twig_start,end)
            foliage(twig_mid,end-twig_start,8,.9)
            foliage(end,end-twig_start,R.randint(10,13))
            anchors.append(end)
        if j%2 == 0:
            bunch(tip-UP*.025,R.randint(3,5))
    # Drooping lower skirt and inner foliage are connected to real scaffold limbs.
    for j in range(4):
        az=a+(j-1.5)*.32
        end=polar(R.uniform(2.15,2.85),az,R.uniform(3.0,3.8))
        start=elbow.lerp(fork,R.uniform(.3,.8))
        mid=shoot(start,end,.022)
        foliage(mid,end-start,12)
        foliage(end,end-start,14,1.03)
    for j in range(3):
        az=a+R.uniform(-.45,.45)
        end=polar(R.uniform(1.05,1.85),az,R.uniform(4.65,5.5))
        mid=shoot(fork,end,.018)
        foliage(mid,end-fork,10)
        foliage(end,end-fork,12)

# Connected inner leaders fill the dome; no floating branches in the crown.
for k in range(6):
    a=k*2.39996+.8
    base=trunk[4].lerp(trunk[6],R.uniform(.2,.85))
    high=polar(R.uniform(.45,1.3),a,R.uniform(6.1,6.55))
    elbow=base.lerp(high,.57)+polar(.2,a+.5,.03)
    limb([base,elbow,high],[.057,.032,.008],9)
    for j in range(10):
        az=a+j*2.39996
        end=high+polar(R.uniform(.30,.69),az,R.uniform(-.32,.30))
        start=elbow.lerp(high,R.uniform(.42,.93))
        mid=shoot(start,end,.016)
        foliage(mid,end-start,9,.94)
        foliage(end,end-start,12,1.02)

# Lower hanging fruit groups remain readable through gaps in the skirt.
for end in sorted(anchors,key=lambda p:p.z)[::12]:
    bunch(end,R.randint(2,4))

class Mesh:
    def __init__(self):
        self.v=[]; self.uv=[]; self.faces=[]
    def vertex(self,p,uv):
        self.v.append(tuple(p)); self.uv.append(uv); return len(self.v)-1
    def quad(self,a,b,c,d):
        self.faces.extend([(a,b,c),(a,c,d)])
    def obj(self,name,mat):
        me=bpy.data.meshes.new(name)
        me.from_pydata(self.v,[],self.faces); me.update()
        layer=me.uv_layers.new(name='UVMap')
        for poly in me.polygons:
            poly.use_smooth=True
        for lp in me.loops:
            layer.data[lp.index].uv=self.uv[lp.vertex_index]
        ob=bpy.data.objects.new(name,me)
        bpy.context.collection.objects.link(ob); me.materials.append(mat)
        return ob

def smooth(points,radii,n):
    result=[]; widths=[]
    for k in range(len(points)-1):
        a,b,c,d=points[max(0,k-1)],points[k],points[k+1],points[min(len(points)-1,k+2)]
        for j in range(n):
            t=j/n
            result.append(.5*(2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t))
            widths.append(radii[k]*(1-t)+radii[k+1]*t)
    return result+[points[-1]],widths+[radii[-1]]

def build_wood(lod):
    m=Mesh()
    for points,radii,sides in wood:
        if lod==2 and radii[0]<.02: continue
        if radii[0]>.04: points,radii=smooth(points,radii,[4,2,1][lod])
        sides=max(4,sides-2*lod)
        rings=[]; distance=0
        reference=None
        for k,(p,r) in enumerate(zip(points,radii)):
            tangent=(points[min(k+1,len(points)-1)]-points[max(0,k-1)]).normalized()
            if reference is None:
                reference=tangent.cross(Vector((0,1,0)) if abs(tangent.y)<.85 else Vector((1,0,0))).normalized()
            u=(reference-tangent*reference.dot(tangent)).normalized()
            v=tangent.cross(u).normalized(); reference=u
            if k: distance+=(p-points[k-1]).length
            ring=[]
            for j in range(sides+1):
                a=j*math.tau/sides
                uneven=1+.04*math.sin(a*5+k*.4)
                ring.append(m.vertex(p+r*uneven*(u*math.cos(a)+v*math.sin(a)),
                                     (j/sides*max(.12,radii[0]*7),distance/1.1)))
            rings.append(ring)
        for k in range(len(rings)-1):
            for j in range(sides): m.quad(rings[k][j],rings[k][j+1],rings[k+1][j+1],rings[k+1][j])
        for j in range(1,sides-1):
            m.faces.append((rings[0][0],rings[0][j+1],rings[0][j]))
            m.faces.append((rings[-1][0],rings[-1][j],rings[-1][j+1]))
    return m.obj(f'Bark_LOD{lod}',bark)

def build_leaves(lod):
    m=Mesh(); segments=[4,3,2][lod]
    for i,(origin,d,length,width,twist,variant,droop) in enumerate(blades):
        if (i*.61803398875)%1 > [1,.52,.19][lod]: continue
        scale=[1,1.16,1.5][lod]; length*=scale; width*=scale
        side=d.cross(UP).normalized()
        normal=side.cross(d).normalized()
        side=(side*math.cos(twist)+normal*math.sin(twist)).normalized()
        normal=side.cross(d).normalized()
        rows=[]
        for j in range(segments+1):
            t=j/segments
            center=origin+d*length*t-UP*droop*t*t
            row=[]
            for s in (-1,0,1):
                p=center+side*width*s*.5-normal*(abs(s)*width*.12*math.sin(math.pi*t))
                # Match the exact 4x2 reference layout, preserving diagonal leaves.
                uv=((variant%4+(s+1)*.5)/4,(variant//4+.005+.99*t)/2)
                row.append(m.vertex(p,uv))
            rows.append(row)
        for j in range(segments):
            for s in range(2): m.quad(rows[j][s],rows[j][s+1],rows[j+1][s+1],rows[j+1][s])
    return m.obj(f'Leaves_LOD{lod}',leaf)

def build_fruit(lod):
    m=Mesh(); sides=[12,9,6][lod]; stacks=[11,8,5][lod]
    for i,(origin,length,a,fullness,tilt) in enumerate(mangos):
        if (i*.754877666)%1 > [1,.85,.52][lod]: continue
        x=Vector((math.cos(a),math.sin(a),0)); y=Vector((-math.sin(a),math.cos(a),0))
        rings=[]
        for k in range(stacks+1):
            t=k/stacks
            # Full shoulders, asymmetric cheek and a rounded hooked lower beak.
            radius=math.sin(math.pi*t)**.53*length*.325*fullness*(1.1-.20*t)
            center=origin-UP*length*t+x*length*(tilt*t+.17*t*t*t)
            ring=[]
            for j in range(sides+1):
                angle=j*math.tau/sides
                p=center+x*radius*math.cos(angle)+y*radius*.82*math.sin(angle)
                ring.append(m.vertex(p,(j/sides,t)))
            rings.append(ring)
        for k in range(stacks):
            for j in range(sides): m.quad(rings[k][j],rings[k+1][j],rings[k+1][j+1],rings[k][j+1])
    return m.obj(f'Fruit_Green_LOD{lod}',fruit)

objects=[]
stats={'seed':381017,'leaves':len(blades),'green_fruit':len(mangos),'lods':[]}
for lod in range(3):
    group=[build_wood(lod),build_leaves(lod),build_fruit(lod)]
    objects+=group
    stats['lods'].append({'level':lod,'triangles':sum(len(o.data.polygons) for o in group),
                          'vertices':sum(len(o.data.vertices) for o in group)})
    bpy.ops.object.select_all(action='DESELECT')
    for ob in group: ob.select_set(True)
    bpy.context.view_layer.objects.active=group[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/f'MangoTree_Update_LOD{lod}.fbx'),
        use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,bake_space_transform=True,path_mode='STRIP',use_tspace=True,
        mesh_smooth_type='OFF',add_leaf_bones=False)
    if lod:
        for ob in group: ob.hide_render=True; ob.hide_set(True)
vertices=[v.co for ob in objects[:3] for v in ob.data.vertices]
stats['bounds_blender']={'min':[min(v[k] for v in vertices) for k in range(3)],
                         'max':[max(v[k] for v in vertices) for k in range(3)]}
(SOURCE/'geometry_report.json').write_text(json.dumps(stats,indent=2))

def aim(ob,point):
    ob.rotation_euler=(Vector(point)-ob.location).to_track_quat('-Z','Y').to_euler()

bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.052))
bpy.context.object.name='Studio ground (not exported)'
bpy.context.object.data.materials.append(material('Studio',color=(.16,.19,.13),roughness=.9))
bpy.ops.object.camera_add(location=(10,-15,7.0))
camera=bpy.context.object; aim(camera,(0,0,3.45))
camera.data.type='ORTHO'; camera.data.ortho_scale=8.6
bpy.context.scene.camera=camera
for name,loc,energy,size in [('Key',(-3,-7,12),2200,6),('Fill',(7,-2,8),1400,6),('Rim',(1,6,10),2600,5)]:
    bpy.ops.object.light_add(type='AREA',location=loc)
    light=bpy.context.object; light.name=name; light.data.energy=energy; light.data.shape='DISK'; light.data.size=size
    aim(light,(0,0,4))
scene=bpy.context.scene; scene.render.engine='BLENDER_EEVEE'
scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.32,.38,.45,1)
scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.35
scene.view_settings.look='AgX - Medium High Contrast'
scene.view_settings.exposure=-.45
scene.render.resolution_x=1300; scene.render.resolution_y=1300; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'MangoTree_Update.blend'))
scene.render.filepath=str(SOURCE/'MangoTree_Update_Preview.png')
bpy.ops.render.render(write_still=True)
camera.location=(5,-8,5.5); aim(camera,(.6,-.7,4.1)); camera.data.ortho_scale=3.25
scene.render.resolution_x=1100; scene.render.resolution_y=1100
scene.render.filepath=str(SOURCE/'MangoTree_Update_Detail.png')
bpy.ops.render.render(write_still=True)
print('MANGO_UPDATE_COMPLETE',json.dumps(stats))
