"""Reimport the delivered FBX and measure it (no Unity needed)."""
import bpy, bmesh, json, sys, math
from mathutils import Vector
fbx, source = sys.argv[1], sys.argv[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
res = {'objects': len(objs), 'parts': {}}
zs, xs, ys = [], [], []
tri_total = 0
bad_uv = 0
for o in objs:
    me = o.data
    me.calc_loop_triangles()
    tris = len(me.loop_triangles)
    tri_total += tris
    bm = bmesh.new(); bm.from_mesh(me)
    nonman = sum(1 for e in bm.edges if len(e.link_faces) != 2)
    degenerate = sum(1 for f in bm.faces if f.calc_area() < 1e-10)
    uvl = me.uv_layers.active
    uvs = [l.uv for l in uvl.data] if uvl else []
    out_uv = sum(1 for uv in uvs if uv.x < -1e-4 or uv.x > 1 + 1e-4 or uv.y < -1e-4 or uv.y > 1 + 1e-4)
    bad_uv += out_uv
    mats = [m.name if m else None for m in me.materials]
    res['parts'][o.name] = {'triangles': tris, 'vertices': len(me.vertices), 'open_or_nonmanifold_edges': nonman, 'degenerate_faces': degenerate, 'uv_outside_0_1': out_uv, 'materials': mats}
    for v in me.vertices:
        w = o.matrix_world @ v.co
        xs.append(w.x); ys.append(w.y); zs.append(w.z)
    bm.free()
res['triangles'] = tri_total
res['height_m'] = max(zs) - min(zs)
res['bounds_min'] = [min(xs), min(ys), min(zs)]
res['bounds_max'] = [max(xs), max(ys), max(zs)]
res['uv_outside_total'] = bad_uv
# compare with the source .blend
bpy.ops.wm.open_mainfile(filepath=source)
src = [o for o in bpy.context.scene.objects if o.type == 'MESH']
sz = [(o.matrix_world @ v.co).z for o in src for v in o.data.vertices]
st = sum(len(o.data.polygons) for o in src)
res['source_height_m'] = max(sz) - min(sz)
res['height_matches_source'] = abs(res['source_height_m'] - res['height_m']) < 1e-4
print(json.dumps(res, indent=1))
open(sys.argv[3], 'w').write(json.dumps(res, indent=1))
