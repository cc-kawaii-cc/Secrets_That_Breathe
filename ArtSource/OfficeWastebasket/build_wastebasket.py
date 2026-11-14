"""Run with Blender --background --python from the project root."""
import math
from pathlib import Path

import bpy
from mathutils import Vector

root = Path.cwd()
source = root / "ArtSource/OfficeWastebasket"
asset = root / "Assets/SecretsThatBreathe/Props/OfficeWastebasket"
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"

metal = bpy.data.materials.new("Wastebasket_Frame")
metal.diffuse_color = (.034, .037, .043, 1)
metal.use_nodes = True
steel = metal.node_tree.nodes.get("Principled BSDF")
steel.inputs["Base Color"].default_value = metal.diffuse_color
steel.inputs["Metallic"].default_value = .55
steel.inputs["Roughness"].default_value = .37
wire = metal.copy()
wire.name = "Wastebasket_Weave"

# A tile repeats around the circumference and along the sloped wall.
# Crossed lines are filtered into an opacity mask, rather than thousands of wires.
image = bpy.data.images.new("Wastebasket_Weave", 128, 128, alpha=True)
pixels = []
for y in range(128):
    for x in range(128):
        u, v = (x + .5) / 128, (y + .5) / 128
        distance = min(abs((u + v + .5) % 1 - .5), abs((u - v + .5) % 1 - .5))
        pixels.extend((1, 1, 1, max(0, min(1, (.095 - distance) * 128))))
image.pixels.foreach_set(pixels)
image.filepath_raw = str(asset / "Wastebasket_Weave.png")
image.file_format = "PNG"
image.save()
image.pack()
nodes = wire.node_tree.nodes
texture = nodes.new("ShaderNodeTexImage")
texture.image = image
wire.node_tree.links.new(texture.outputs["Alpha"], nodes.get("Principled BSDF").inputs["Alpha"])

segments = 64
def lathe(name, profile, material, *, closed=False, caps=False):
    vertices = [(r * math.cos(i * math.tau / segments),
                 r * math.sin(i * math.tau / segments), z)
                for r, z in profile for i in range(segments)]
    faces = []
    for j in range(len(profile) if closed else len(profile) - 1):
        following = (j + 1) % len(profile)
        for i in range(segments):
            nxt = (i + 1) % segments
            faces.append((j*segments+i, j*segments+nxt, following*segments+nxt, following*segments+i))
    if caps:
        faces += [tuple(reversed(range(segments))),
                  tuple(range(len(vertices)-segments, len(vertices)))]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    mesh.materials.append(material)
    for polygon in mesh.polygons:
        polygon.use_smooth = len(polygon.vertices) == 4
    return obj

base = lathe("Base", [(.119, 0), (.124, .0015), (.127, .005), (.127, .016),
                     (.125, .020), (.122, .021)], metal, caps=True)
rim = lathe("Rolled_Rim", [(.158 + .005 * math.cos(i * math.tau / 10),
                          .335 + .005 * math.sin(i * math.tau / 10))
                         for i in range(10)], metal, closed=True)
wall = lathe("Tapered_Mesh", [(.124, .014), (.158, .335)], wire)
uv = wall.data.uv_layers.new()
for face in wall.data.polygons:
    u = face.index / segments * 124
    end = (face.index + 1) / segments * 124
    for loop, coords in zip(face.loop_indices, ((u, 0), (end, 0), (end, 45), (u, 45))):
        uv.data[loop].uv = coords

parts = [base, rim, wall]
for obj in parts:
    obj.select_set(True)
    obj.data.calc_loop_triangles()
    assert obj.data.validate() is False
bpy.context.view_layer.objects.active = base
bpy.ops.wm.save_as_mainfile(filepath=str(source / "OfficeWastebasket.blend"))
bpy.ops.export_scene.fbx(filepath=str(asset / "OfficeWastebasket.fbx"), use_selection=True,
                         axis_forward="-Z", axis_up="Y", bake_anim=False)
print("Export triangles:", sum(len(obj.data.loop_triangles) for obj in parts))

scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new("Preview World")
scene.world.color = (.25, .25, .25)
for pos, power, size in [((.45, -.45, .75), 32, .6), ((-.35, .25, .55), 24, .5)]:
    bpy.ops.object.light_add(type="AREA", location=pos)
    bpy.context.object.data.energy = power
    bpy.context.object.data.shape = "DISK"
    bpy.context.object.data.size = size
    bpy.context.object.rotation_euler = (Vector((0, 0, .17)) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
for name, pos in [("OfficeWastebasket", (.65, -.85, .58)), ("OfficeWastebasket_Inside", (-.38, .47, .92))]:
    bpy.ops.object.camera_add(location=pos)
    scene.camera = bpy.context.object
    scene.camera.rotation_euler = (Vector((0, 0, .17)) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
    scene.camera.data.type = "ORTHO"
    scene.camera.data.ortho_scale = .46
    scene.render.filepath = str(source / (name + ".png"))
    bpy.ops.render.render(write_still=True)
