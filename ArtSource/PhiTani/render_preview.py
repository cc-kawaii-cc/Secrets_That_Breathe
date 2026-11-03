import bpy, math, sys
from mathutils import Vector
blend, outdir = sys.argv[1], sys.argv[2]
views = sys.argv[3].split(',') if len(sys.argv) > 3 else ['front', 'three_quarter', 'back', 'side']
res = (int(sys.argv[4]) if len(sys.argv) > 4 else 420)
bpy.ops.wm.open_mainfile(filepath=blend)
s = bpy.context.scene
s.render.engine = 'CYCLES'; s.cycles.device = 'CPU'; s.cycles.samples = 24; s.cycles.use_denoising = False
s.render.resolution_x = res; s.render.resolution_y = int(res * 1.7); s.render.resolution_percentage = 100
s.view_settings.view_transform = 'Standard'
w = bpy.data.worlds.new('W'); w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (.12, .13, .14, 1); w.node_tree.nodes['Background'].inputs[1].default_value = 1.0
s.world = w
def light(kind, loc, energy, size=3):
    d = bpy.data.lights.new(kind, 'AREA'); d.energy = energy; d.size = size
    o = bpy.data.objects.new(kind, d); o.location = loc
    o.rotation_euler = (Vector((0, 0, .9)) - o.location).to_track_quat('-Z', 'Y').to_euler()
    s.collection.objects.link(o)
light('key', (2.5, -3.5, 3.0), 650); light('fill', (-3.5, -2.0, 1.5), 260); light('rim', (0, 3.5, 2.5), 300)
cams = {'front': (0, -4.6, .9), 'three_quarter': (3.2, -3.4, 1.0), 'back': (0, 4.6, .9), 'side': (4.6, 0, .9), 'face': (0, -1.15, 1.52), 'face34': (.75, -1.0, 1.52)}
for name in views:
    loc = cams[name]
    cam_data = bpy.data.cameras.new('c'); cam = bpy.data.objects.new('c', cam_data); cam.location = loc
    tgt = Vector((0, 0, 1.52)) if name.startswith('face') else Vector((0, 0, .85))
    cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam_data.lens = 85
    if name.startswith('face'):
        s.render.resolution_y = res
    s.collection.objects.link(cam); s.camera = cam
    s.render.filepath = f'{outdir}/preview_{name}.png'
    bpy.ops.render.render(write_still=True)
print('RENDERED')
