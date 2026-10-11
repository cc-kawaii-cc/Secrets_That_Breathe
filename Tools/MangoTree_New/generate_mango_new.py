"""Build the independent MangoTree_New source, three FBX LODs, and preview renders.

The geometry is deterministic and original. Blender is Z-up; FBX export converts to
Unity Y-up in metres. Textures are loaded only from MangoTree_New/Textures.
"""
import bpy
import json
import math
import random
from mathutils import Vector
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/SecretsThatBreathe/Environment/MangoTree_New"
SOURCE = ROOT / "ArtSource/MangoTree_New"
for directory in (OUT / "Models", OUT / "Textures", SOURCE):
    directory.mkdir(parents=True, exist_ok=True)

SEED = 604038
random.seed(SEED)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)


def load_image(filename, non_color=False):
    image = bpy.data.images.load(str(OUT / "Textures" / filename), check_existing=True)
    if non_color:
        image.colorspace_settings.name = "Non-Color"
    return image


def image_material(name, image, roughness, normal_image=None, alpha=False):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Roughness"].default_value = roughness
    texture = material.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = image
    material.node_tree.links.new(texture.outputs["Color"], bsdf.inputs["Base Color"])
    if alpha:
        material.node_tree.links.new(texture.outputs["Alpha"], bsdf.inputs["Alpha"])
        try:
            material.surface_render_method = "DITHERED"
        except Exception:
            pass
        material.use_transparency_overlap = False
        bsdf.inputs["Subsurface Weight"].default_value = 0.035
    if normal_image:
        normal_texture = material.node_tree.nodes.new("ShaderNodeTexImage")
        normal_texture.image = normal_image
        normal = material.node_tree.nodes.new("ShaderNodeNormalMap")
        normal.inputs["Strength"].default_value = 0.72
        material.node_tree.links.new(normal_texture.outputs["Color"], normal.inputs["Color"])
        material.node_tree.links.new(normal.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def color_material(name, base_color, roughness):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*base_color, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    return material


bark_image = load_image("Mango_Bark_BaseColor.png")
bark_normal = load_image("Mango_Bark_Normal.png", non_color=True)
leaf_image = load_image("Mango_Leaves_Atlas.png")
MATERIALS = {
    "bark": image_material("MangoNew_Bark", bark_image, 0.88, bark_normal),
    "leaves": image_material("MangoNew_Leaves", leaf_image, 0.36, alpha=True),
    "green": color_material("MangoNew_Fruit_Green", (0.37, 0.60, 0.075), 0.40),
}

branches = []
leaves = []
fruits_green = []
fruits_ripe = []


def branch(points, radii, sides=9):
    branches.append(([Vector(p) for p in points], list(radii), sides))


def polar(radius, angle, z):
    return Vector((radius * math.cos(angle), radius * math.sin(angle), z))


def rosette(origin, direction, scale=1.0, density=12):
    direction = direction.normalized()
    up = Vector((0, 0, 1))
    for index in range(density + random.randint(-2, 2)):
        angle = index * 2.399963 + random.uniform(-0.25, 0.25)
        spread = Vector((math.cos(angle), math.sin(angle), random.uniform(-0.66, 0.34)))
        leaf_direction = (spread * 0.78 + direction * 0.36 + up * random.uniform(-0.10, 0.12)).normalized()
        start = origin + direction * random.uniform(-0.09, 0.10)
        length = random.uniform(0.37, 0.56) * scale
        # Keep the reference length, but use the broader, fuller blade from the revision image.
        width = length * random.uniform(0.34, 0.42)
        atlas_variant = random.choices(range(8), [14, 15, 13, 11, 13, 15, 10, 9])[0]
        leaves.append((start, leaf_direction, length, width, random.uniform(-1.1, 1.1), atlas_variant,
                       random.uniform(0.06, 0.18)))


def add_fruit_cluster(anchor, count, ripe_probability):
    for index in range(count):
        spread = Vector((random.uniform(-0.16, 0.16), random.uniform(-0.16, 0.16), 0))
        stem_end = anchor + spread + Vector((0, 0, -random.uniform(0.20, 0.34)))
        branch([anchor + spread * 0.25, stem_end], [0.005, 0.0025], 5)
        fruit = (stem_end, random.uniform(0.27, 0.37), random.uniform(0, math.tau), random.uniform(0.88, 1.10))
        random.random()  # Preserve the deterministic branch/leaf layout from the previous version.
        fruits_green.append(fruit)


# Broad root flare, surface roots, and an irregular trunk that forks low.
branch([(0, 0, -0.08), (0.04, -0.03, 0.28), (-0.05, 0.03, 0.82),
        (0.08, 0.01, 1.42), (0.02, -0.03, 2.08), (0.10, 0.02, 2.58)],
       [0.38, 0.34, 0.285, 0.225, 0.15, 0.070], 16)
for index in range(10):
    angle = index * math.tau / 10 + random.uniform(-0.10, 0.10)
    branch([(0, 0, 0.16), polar(0.52, angle, 0.07), polar(random.uniform(1.0, 1.65), angle, 0.015)],
           [0.15, 0.09, 0.012], 9)

# Nine scaffold limbs make the open, low-branching mango silhouette.
outer_tips = []
for scaffold in range(9):
    base_angle = scaffold * math.tau / 9 + random.uniform(-0.13, 0.13)
    base = Vector((random.uniform(-0.08, 0.08), random.uniform(-0.08, 0.08), random.uniform(1.30, 2.12)))
    elbow = polar(random.uniform(0.85, 1.30), base_angle, random.uniform(2.85, 3.65))
    crown = polar(random.uniform(2.25, 2.90), base_angle + random.uniform(-0.12, 0.12), random.uniform(4.45, 5.30))
    branch([base, base.lerp(elbow, 0.48) + Vector((0, 0, 0.15)), elbow,
            elbow.lerp(crown, 0.56) + Vector((0, 0, 0.18)), crown],
           [0.16, 0.135, 0.105, 0.062, 0.035], 12)
    for lateral in range(7):
        offset = (lateral - 3) * 0.30 + random.uniform(-0.12, 0.12)
        angle = base_angle + offset
        radius = random.uniform(3.25, 4.65)
        dome_height = 7.10 - 0.115 * radius * radius + random.uniform(-0.30, 0.34)
        target = polar(radius, angle, dome_height)
        start = elbow.lerp(crown, random.uniform(0.30, 0.92))
        mid = start.lerp(target, 0.53) + Vector((0, 0, random.uniform(0.16, 0.36)))
        branch([start, mid, target], [0.044, 0.024, 0.010], 9)
        outer_tips.append(target)
        rosette(target, target - mid, random.uniform(0.96, 1.16), random.randint(13, 17))
        rosette(mid, target - start, random.uniform(0.86, 1.05), random.randint(11, 15))
        for twig in range(4):
            twig_angle = angle + random.uniform(-0.52, 0.52)
            delta = polar(random.uniform(0.38, 0.78), twig_angle, random.uniform(-0.22, 0.48))
            tip = target + delta
            twig_start = mid.lerp(target, random.uniform(0.50, 0.93))
            twig_mid = twig_start.lerp(tip, 0.58) + Vector((0, 0, 0.08))
            branch([twig_start, twig_mid, tip], [0.016, 0.009, 0.003], 6)
            rosette(tip, tip - twig_mid, random.uniform(0.88, 1.10), random.randint(10, 13))
            rosette(twig_mid, tip - twig_start, random.uniform(0.78, 0.98), random.randint(9, 12))
            if tip.z < 5.75 and random.random() < 0.10:
                add_fruit_cluster(tip, random.randint(2, 4), ripe_probability=0.34)

# Upright leaders fill the upper crown while leaving view gaps below.
for leader in range(6):
    angle = leader * 2.399963 + 0.31
    top = polar(random.uniform(0.55, 1.65), angle, random.uniform(6.75, 7.65))
    base = Vector((0.05, 0.0, random.uniform(1.85, 2.42)))
    middle = base.lerp(top, 0.55) + polar(0.35, angle + 1.1, 0.15)
    branch([base, middle, top], [0.095, 0.045, 0.011], 9)
    rosette(middle, top - base, 1.05, random.randint(14, 18))
    for twig in range(9):
        twig_angle = angle + twig * 2.399963
        end = top + polar(random.uniform(0.28, 0.78), twig_angle, random.uniform(-0.36, 0.28))
        branch([middle.lerp(top, random.uniform(0.62, 0.90)), end], [0.014, 0.0025], 6)
        rosette(end, end - middle, random.uniform(0.86, 1.08), random.randint(10, 13))

# Fine interior shoots close the canopy into the broad, dense orchard-tree mass
# while keeping the lowest metre of the crown open enough to read the trunk forks.
for filler in range(125):
    angle = random.uniform(0, math.tau)
    radius = random.uniform(1.05, 4.45)
    z = 7.15 - 0.115 * radius * radius + random.uniform(-0.48, 0.38)
    endpoint = polar(radius, angle, z)
    direction = (Vector((math.cos(angle), math.sin(angle), random.uniform(-0.18, 0.42)))).normalized()
    start = endpoint - direction * random.uniform(0.34, 0.62)
    branch([start, start.lerp(endpoint, 0.56) + Vector((0, 0, 0.06)), endpoint],
           [0.009, 0.005, 0.002], 5)
    rosette(endpoint, direction, random.uniform(0.90, 1.12), random.randint(11, 15))

# Deliberate mixed-colour mango bunches around the visible lower canopy.
fruit_candidates = sorted(outer_tips, key=lambda point: point.z)[:28]
for index, anchor in enumerate(fruit_candidates):
    if index % 2 == 0 or random.random() < 0.42:
        add_fruit_cluster(anchor + Vector((0, 0, -0.08)), random.randint(2, 5), ripe_probability=0.40)


class MeshBuilder:
    def __init__(self):
        self.vertices = []
        self.faces = []
        self.uvs = []

    def vertex(self, position, uv):
        self.vertices.append(tuple(position))
        self.uvs.append(tuple(uv))
        return len(self.vertices) - 1

    def triangle(self, a, b, c):
        self.faces.append((a, b, c))

    def quad(self, a, b, c, d):
        self.triangle(a, b, c)
        self.triangle(a, c, d)

    def object(self, name, material):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(self.vertices, [], self.faces)
        mesh.update()
        uv_layer = mesh.uv_layers.new(name="UVMap")
        for polygon in mesh.polygons:
            polygon.use_smooth = True
            for loop_index in polygon.loop_indices:
                uv_layer.data[loop_index].uv = self.uvs[mesh.loops[loop_index].vertex_index]
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.collection.objects.link(obj)
        mesh.materials.append(material)
        return obj


def catmull_rom(points, radii, subdivisions):
    smooth_points = []
    smooth_radii = []
    for segment in range(len(points) - 1):
        a = points[max(0, segment - 1)]
        b = points[segment]
        c = points[segment + 1]
        d = points[min(len(points) - 1, segment + 2)]
        for sub in range(subdivisions):
            t = sub / subdivisions
            point = 0.5 * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t
                           + (-a + 3 * b - 3 * c + d) * t * t * t)
            smooth_points.append(point)
            smooth_radii.append(radii[segment] * (1 - t) + radii[segment + 1] * t)
    smooth_points.append(points[-1])
    smooth_radii.append(radii[-1])
    return smooth_points, smooth_radii


def build_bark(lod):
    builder = MeshBuilder()
    for points, radii, requested_sides in branches:
        if lod == 2 and radii[0] < 0.021:
            continue
        subdivisions = max(1, 3 - lod) if radii[0] > 0.032 else 1
        if subdivisions > 1:
            points, radii = catmull_rom(points, radii, subdivisions)
        sides = max(5, requested_sides - lod * 3)
        rings = []
        distance_along = 0.0
        for ring_index, (point, radius) in enumerate(zip(points, radii)):
            tangent = points[min(ring_index + 1, len(points) - 1)] - points[max(0, ring_index - 1)]
            tangent.normalize()
            reference = Vector((0, 1, 0)) if abs(tangent.y) < 0.9 else Vector((1, 0, 0))
            axis_u = tangent.cross(reference).normalized()
            axis_v = tangent.cross(axis_u).normalized()
            if ring_index:
                distance_along += (point - points[ring_index - 1]).length
            ring = []
            for side in range(sides + 1):
                angle = side / sides * math.tau
                irregularity = 1 + 0.055 * math.sin(angle * 5 + ring_index * 0.8) + 0.025 * math.cos(angle * 3 - ring_index)
                offset = irregularity * radius * (axis_u * math.cos(angle) + axis_v * math.sin(angle))
                ring.append(builder.vertex(point + offset, (side / sides, distance_along / 1.55)))
            rings.append(ring)
        for ring_index in range(len(rings) - 1):
            for side in range(sides):
                builder.quad(rings[ring_index][side], rings[ring_index][side + 1],
                             rings[ring_index + 1][side + 1], rings[ring_index + 1][side])
        for side in range(1, sides - 1):
            builder.triangle(rings[0][0], rings[0][side + 1], rings[0][side])
            builder.triangle(rings[-1][0], rings[-1][side], rings[-1][side + 1])
    return builder.object(f"Bark_LOD{lod}", MATERIALS["bark"])


def build_leaves(lod):
    builder = MeshBuilder()
    # Four longitudinal segments preserve the gentle mango-leaf bend while
    # keeping the hero LOD practical for real-time use.
    segments = [4, 3, 2][lod]
    keep = [1.0, 0.56, 0.23][lod]
    for index, (origin, direction, length, width, twist, variant, droop) in enumerate(leaves):
        if (index * 0.61803398875) % 1 > keep:
            continue
        size = [1.0, 1.10, 1.32][lod]
        length *= size
        width *= size
        side = direction.cross(Vector((0, 0, 1)))
        if side.length < 0.1:
            side = Vector((1, 0, 0))
        side.normalize()
        normal = side.cross(direction).normalized()
        side = (side * math.cos(twist) + normal * math.sin(twist)).normalized()
        normal = side.cross(direction).normalized()
        column = variant % 4
        row = variant // 4
        margin_u = 0.013
        margin_v = 0.018
        u_min = column / 4 + margin_u
        u_max = (column + 1) / 4 - margin_u
        v_min = row / 2 + margin_v
        v_max = (row + 1) / 2 - margin_v
        strips = []
        for segment in range(segments + 1):
            t = segment / segments
            half_width = (math.sin(math.pi * t) ** 0.88) * width * 0.5
            center = origin + direction * length * t + Vector((0, 0, -droop * t * t))
            rib = normal * (0.018 * width * math.sin(math.pi * t))
            strip = []
            for across in (-1, 0, 1):
                cup = normal * (-abs(across) * width * 0.10 * math.sin(math.pi * t))
                position = center + side * half_width * across + cup + (rib if across == 0 else Vector())
                u = u_min + (u_max - u_min) * (across + 1) * 0.5
                v = v_min + (v_max - v_min) * t
                strip.append(builder.vertex(position, (u, v)))
            strips.append(strip)
        for segment in range(segments):
            for across in range(2):
                builder.quad(strips[segment][across], strips[segment][across + 1],
                             strips[segment + 1][across + 1], strips[segment + 1][across])
    return builder.object(f"Leaves_LOD{lod}", MATERIALS["leaves"])


def build_fruits(lod, fruit_data, name, material):
    builder = MeshBuilder()
    sides = [12, 9, 6][lod]
    stacks = [11, 8, 5][lod]
    keep = [1.0, 0.82, 0.48][lod]
    for index, (origin, length, rotation, fullness) in enumerate(fruit_data):
        if (index * 0.754877666) % 1 > keep:
            continue
        rings = []
        direction = Vector((math.cos(rotation), math.sin(rotation), 0))
        side_axis = Vector((-math.sin(rotation), math.cos(rotation), 0))
        for stack in range(stacks + 1):
            t = stack / stacks
            radius = math.sin(math.pi * t) ** 0.64 * length * 0.30 * fullness
            radius *= 0.84 + 0.22 * t
            center = origin + Vector((0, 0, -length * t)) + direction * (length * (0.08 * t + 0.18 * t * t))
            ring = []
            for side in range(sides + 1):
                angle = side / sides * math.tau
                position = center + direction * (radius * math.cos(angle)) + side_axis * (radius * 0.82 * math.sin(angle))
                ring.append(builder.vertex(position, (side / sides, t)))
            rings.append(ring)
        for stack in range(stacks):
            for side in range(sides):
                builder.quad(rings[stack][side], rings[stack + 1][side],
                             rings[stack + 1][side + 1], rings[stack][side + 1])
    return builder.object(f"{name}_LOD{lod}", material)


all_objects = []
statistics = {"seed": SEED, "leaves": len(leaves), "green_fruit": len(fruits_green),
              "ripe_fruit": len(fruits_ripe), "lods": []}
for lod in range(3):
    objects = [
        build_bark(lod),
        build_leaves(lod),
        build_fruits(lod, fruits_green, "Fruit_Green", MATERIALS["green"]),
    ]
    all_objects.extend(objects)
    statistics["lods"].append({
        "level": lod,
        "triangles": sum(len(obj.data.polygons) for obj in objects),
        "vertices": sum(len(obj.data.vertices) for obj in objects),
    })
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=str(OUT / "Models" / f"MangoTree_New_LOD{lod}.fbx"),
        use_selection=True,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=True,
        path_mode="STRIP",
        mesh_smooth_type="OFF",
        use_tspace=True,
    )
    if lod > 0:
        for obj in objects:
            obj.hide_render = True
            obj.hide_set(True)

lod0_vertices = [vertex.co[:] for obj in all_objects[:4] for vertex in obj.data.vertices]
statistics["bounds_blender"] = {
    "min": [float(min(value[i] for value in lod0_vertices)) for i in range(3)],
    "max": [float(max(value[i] for value in lod0_vertices)) for i in range(3)],
}
(SOURCE / "geometry_report.json").write_text(json.dumps(statistics, indent=2), encoding="utf-8")

# Reusable source and a fast neutral preview for visual QA.
studio = bpy.data.collections.new("Preview Studio - not exported")
bpy.context.scene.collection.children.link(studio)


def move_to_studio(obj):
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    studio.objects.link(obj)


def aim(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat("-Z", "Y").to_euler()


bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, -0.09))
ground = bpy.context.object
move_to_studio(ground)
ground.data.materials.append(color_material("Studio Ground", (0.20, 0.23, 0.19), 0.95))
bpy.ops.object.camera_add(location=(12.4, -15.5, 8.2))
camera = bpy.context.object
move_to_studio(camera)
aim(camera, (0, 0, 3.55))
camera.data.type = "ORTHO"
camera.data.ortho_scale = 10.9
bpy.context.scene.camera = camera
for name, location, power, size in [
    ("Key", (2, -8, 14), 2300, 7),
    ("Fill", (-8, -2, 8), 1500, 6),
    ("Rim", (7, 6, 11), 2600, 5),
]:
    bpy.ops.object.light_add(type="AREA", location=location)
    light = bpy.context.object
    light.name = name
    light.data.energy = power
    light.data.shape = "DISK"
    light.data.size = size
    aim(light, (0, 0, 3.8))
    move_to_studio(light)

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.world.color = (0.055, 0.065, 0.055)
scene.render.resolution_x = 1500
scene.render.resolution_y = 1300
scene.render.resolution_percentage = 100
scene.view_settings.look = "AgX - Medium High Contrast"
scene.render.image_settings.file_format = "PNG"
for image in (bark_image, bark_normal, leaf_image):
    image.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "MangoTree_New.blend"))
scene.render.filepath = str(SOURCE / "MangoTree_New_Preview.png")
bpy.ops.render.render(write_still=True)
camera.location = (7.5, -8.6, 6.2)
aim(camera, (1.1, -0.2, 5.25))
camera.data.ortho_scale = 4.2
scene.render.resolution_x = 1200
scene.render.resolution_y = 1200
scene.render.filepath = str(SOURCE / "MangoTree_New_Detail.png")
bpy.ops.render.render(write_still=True)
print("MANGO_NEW_COMPLETE", json.dumps(statistics))
