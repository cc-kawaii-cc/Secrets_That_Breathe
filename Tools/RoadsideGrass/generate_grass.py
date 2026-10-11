"""Original narrow-blade roadside grass. Blender 5.2, metre scale, deterministic."""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/SecretsThatBreathe/Environment/RoadsideGrass'
SOURCE = ROOT / 'ArtSource/RoadsideGrass'
(OUT / 'Models').mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
mat = bpy.data.materials.new('RoadsideGrass_VertexColor')
mat.use_nodes = True
nodes = mat.node_tree.nodes
color = nodes.new('ShaderNodeVertexColor'); color.layer_name = 'GrassColor'
mat.node_tree.links.new(color.outputs['Color'], nodes.get('Principled BSDF').inputs['Base Color'])
nodes.get('Principled BSDF').inputs['Roughness'].default_value = .82
stats = []
variants = [('RoadsideGrass_Green', 160, 4, .08, 1.0),
            ('RoadsideGrass_Mixed', 128, 4, .34, 1.0),
            ('RoadsideGrass_Lite', 64, 3, .15, .70)]
for vi, (name, count, segments, dry_fraction, scale) in enumerate(variants):
    rng = random.Random(111026 + vi * 47)
    verts, faces, uvs, colors = [], [], [], []
    for i in range(count):
        # Small offset tufts break the radial symmetry; roots remain below ground.
        tuft = i % 9; a = tuft * 2.399963
        root = Vector((.11 * math.cos(a), .11 * math.sin(a), -.006)) * scale
        root += Vector((rng.uniform(-.045,.045),rng.uniform(-.045,.045),0)) * scale
        angle = rng.uniform(0, math.tau)
        radial = Vector((math.cos(angle),math.sin(angle),0))
        side = Vector((-math.sin(angle),math.cos(angle),0))
        upright = i % 5 == 0
        h = rng.uniform(.29,.54) * scale
        reach = rng.uniform(.05,.15) if upright else rng.uniform(.18,.36)
        reach *= scale
        droop = rng.uniform(.08,.26) if not upright else .04
        width = rng.uniform(.007,.013) * scale
        dry = rng.random() < dry_fraction
        if dry: base = Vector((rng.uniform(.23,.34),rng.uniform(.17,.24),rng.uniform(.06,.105)))
        else: base = Vector((rng.uniform(.09,.18),rng.uniform(.20,.34),rng.uniform(.018,.045)))
        rings = []
        for s in range(segments + 1):
            t = s / segments
            center = root + radial * (reach * t**1.65) + Vector((0,0,h*(t-droop*t**3)))
            center += side * (math.sin(t*2.4+i)*.019*t*t*scale)
            w = width * (.65 + .35*math.sin(math.pi*t)) * (1-t)**.65
            # Blade narrows into an actual triangular tip, no transparent cards.
            brightness = .40 + .60 * min(1,t*3.5)
            tint = base * brightness
            ring = []
            for sign in ([-1,1] if s < segments else [0]):
                p = center + side * w*.5*sign
                ring.append(len(verts)); verts.append(tuple(p))
                uvs.append(((sign+1)*.5,t))
                colors.append((*tint, t*t))
            rings.append(ring)
        for s in range(segments):
            a,b = rings[s]
            if s == segments-1: faces.append((a,b,rings[s+1][0]))
            else:
                c,d = rings[s+1]; faces.extend([(a,b,d),(a,d,c)])
    mesh = bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces); mesh.update()
    uv = mesh.uv_layers.new(name='UVMap')
    attr = mesh.color_attributes.new(name='GrassColor',type='FLOAT_COLOR',domain='CORNER')
    for poly in mesh.polygons:
        poly.use_smooth = True
        for li in poly.loop_indices:
            index = mesh.loops[li].vertex_index
            uv.data[li].uv = uvs[index]; attr.data[li].color = colors[index]
    # Foliage lighting normals: mostly up, with enough radial component for depth.
    mesh.normals_split_custom_set_from_vertices([Vector((p[0]*.55,p[1]*.55,.9)).normalized() for p in verts])
    obj = bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/f'{name}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,path_mode='STRIP',mesh_smooth_type='OFF',use_tspace=True,colors_type='LINEAR',add_leaf_bones=False)
    stats.append({'name':name,'blades':count,'triangles':len(faces),'height_m':max(p[2] for p in verts),'width_m':max(p[0] for p in verts)-min(p[0] for p in verts)})
    obj.location.x = (vi-1)*1.0
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'RoadsideGrass.blend'))
(SOURCE/'geometry_report.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
print('GRASS_GEOMETRY_COMPLETE',json.dumps(stats))
