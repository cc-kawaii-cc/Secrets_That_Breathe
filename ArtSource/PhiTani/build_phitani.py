"""Phi Tani (Thai banana-tree ghost) low-poly game character.

Builds the model, paints the 512px atlas, saves the .blend, exports the FBX and renders previews.
Run: python build_phitani.py [output_dir]   (needs the `bpy` and `Pillow` packages)
Model space: Blender, metres, Z up, character faces -Y, origin at the soles of the feet (same convention as ThaiPolice).
"""
import bpy, bmesh, math, json, os, sys, random
from pathlib import Path
from mathutils import Vector, Matrix
from PIL import Image, ImageDraw, ImageFilter

OUT = Path(sys.argv[1] if len(sys.argv) > 1 else os.environ.get('PHITANI_OUT', '/home/claude/phitani/out'))
OUT.mkdir(parents=True, exist_ok=True)
(OUT / 'Textures').mkdir(exist_ok=True)
random.seed(7)
ATLAS = 512

# ----------------------------------------------------------------------------- atlas layout (pixel rects x0,y0,x1,y1; y from the top)
FACE = (0, 0, 256, 256)
SKIN = (256, 0, 320, 64)
GOLD = (320, 0, 384, 64)
FLOWER = (384, 0, 448, 64)
HAIR = (256, 64, 512, 192)
SABAI = (256, 192, 512, 256)
SKIRT = (0, 256, 256, 512)
TOP = (256, 256, 512, 384)

def uvmap(rect, u, v):
    """u,v in 0..1 inside the rect (v up) -> atlas UV."""
    x0, y0, x1, y1 = rect
    px = x0 + 1 + u * (x1 - x0 - 2)
    py = y0 + 1 + (1 - v) * (y1 - y0 - 2)
    return (px / ATLAS, 1 - py / ATLAS)

# ----------------------------------------------------------------------------- mesh helpers
class Part:
    def __init__(self):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new('UVMap')

    def face(self, verts, uvs):
        try:
            f = self.bm.faces.new(verts)
        except ValueError:
            return None
        for loop, uv in zip(f.loops, uvs):
            loop[self.uv].uv = uv
        return f

def loft(part, rings, rect, closed=True, caps=(True, True), swap_uv=False, u_repeat=1.0, flat=False, double_sided=False):
    """Skin a list of rings (each a list of Vectors, same length). Ring i runs top->bottom, so v goes 1 -> 0."""
    R, n = len(rings), len(rings[0])
    verts = [[part.bm.verts.new(p) for p in ring] for ring in rings]
    segs = n if closed else n - 1

    def uv_for(ju, iv):  # ju: 0..1 around the ring, iv: 0..1 down the loft
        u, v = ju * u_repeat, 1 - iv
        if flat:
            return uvmap(rect, .5, .5)
        return uvmap(rect, v, u) if swap_uv else uvmap(rect, u, v)

    for i in range(R - 1):
        for j in range(segs):
            j2 = (j + 1) % n
            vs = [verts[i][j], verts[i][j2], verts[i + 1][j2], verts[i + 1][j]]
            uvs = [uv_for(j / segs, i / (R - 1)), uv_for((j + 1) / segs, i / (R - 1)),
                   uv_for((j + 1) / segs, (i + 1) / (R - 1)), uv_for(j / segs, (i + 1) / (R - 1))]
            part.face(vs, uvs)
            if double_sided:
                part.face(vs[::-1], uvs[::-1])
    if closed:
        for ring_index, on in ((0, caps[0]), (R - 1, caps[1])):
            if not on:
                continue
            centre = part.bm.verts.new(sum((v.co for v in verts[ring_index]), Vector()) / n)
            iv = ring_index / (R - 1)
            for j in range(n):
                j2 = (j + 1) % n
                part.face([verts[ring_index][j], verts[ring_index][j2], centre],
                          [uv_for(j / n, iv), uv_for((j + 1) / n, iv), uv_for((j + .5) / n, iv)])
    return verts

def ring(cx, cy, z, rx, rf, rb, n):
    """Ellipse-ish ring: x half width rx, front (-Y) radius rf, back (+Y) radius rb."""
    pts = []
    for j in range(n):
        t = 2 * math.pi * j / n
        c, s = math.cos(t), math.sin(t)
        pts.append(Vector((cx + rx * s, cy - (rf if c > 0 else rb) * c, z)))
    return pts

def catmull(points, count):
    out = []
    P = [points[0]] + list(points) + [points[-1]]
    segs = len(points) - 1
    for k in range(count):
        f = k / (count - 1) * segs
        i = min(int(f), segs - 1)
        t = f - i
        p0, p1, p2, p3 = P[i], P[i + 1], P[i + 2], P[i + 3]
        out.append(.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    return out

def tube_along(points, radii, n=8, count=None, ellipse=(1.0, 1.0), hint=Vector((0, -1, 0))):
    """Rings around a smoothed path. radii: list sampled along the path. hint: preferred direction of ring axis 'a'."""
    count = count or len(points)
    path = catmull(points, count) if count != len(points) else list(points)
    rr = [radii[min(int(k / (count - 1) * (len(radii) - 1) + .5), len(radii) - 1)] if len(radii) != count else radii[k] for k in range(count)]
    # smooth radius interpolation
    rr = []
    for k in range(count):
        f = k / (count - 1) * (len(radii) - 1)
        i = min(int(f), len(radii) - 2) if len(radii) > 1 else 0
        t = f - i
        rr.append(radii[i] * (1 - t) + radii[min(i + 1, len(radii) - 1)] * t)
    rings = []
    N = None
    for k, p in enumerate(path):
        T = (path[min(k + 1, count - 1)] - path[max(k - 1, 0)]).normalized()
        if N is None:
            N = (hint - T * hint.dot(T))
            if N.length < 1e-4:
                N = Vector((1, 0, 0)) - T * T.x
            N.normalize()
        else:
            N = (N - T * N.dot(T)).normalized()
        B = T.cross(N)
        ringpts = []
        for j in range(n):
            a = 2 * math.pi * j / n
            ringpts.append(p + (N * math.cos(a) * ellipse[0] + B * math.sin(a) * ellipse[1]) * rr[k])
        rings.append(ringpts)
    return rings

def to_object(name, part, material):
    bm = part.bm
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    for e in bm.edges:
        if len(e.link_faces) == 2 and e.calc_face_angle(0) > math.radians(58):
            e.smooth = False
    for f in bm.faces:
        f.smooth = True
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    mesh.materials.append(material)
    return obj

def recalc(part):
    bmesh.ops.recalc_face_normals(part.bm, faces=part.bm.faces)

# ----------------------------------------------------------------------------- body tables (z, half width, front, back)
TORSO = [(.93, .155, .095, .085), (1.00, .135, .085, .080), (1.06, .118, .074, .074), (1.14, .128, .092, .076),
         (1.22, .148, .112, .080), (1.30, .163, .090, .076), (1.335, .150, .070, .068), (1.357, .104, .056, .056), (1.372, .062, .046, .046), (1.385, .050, .044, .044)]
HEAD = [(1.395, .016, .040, .030), (1.425, .048, .066, .050), (1.465, .070, .084, .074), (1.515, .078, .090, .090),
        (1.565, .077, .083, .095), (1.608, .066, .070, .088), (1.636, .040, .046, .066), (1.652, .014, .016, .026)]

def interp_table(table, z):
    for a, b in zip(table, table[1:]):
        if a[0] <= z <= b[0]:
            t = (z - a[0]) / (b[0] - a[0])
            return tuple(a[k] * (1 - t) + b[k] * t for k in range(1, 4))
    return table[0][1:] if z < table[0][0] else table[-1][1:]

def torso_radius_at(z, angle):
    """Distance from the torso axis at height z toward angle (0 = front, +90deg = character's left)."""
    rx, rf, rb = interp_table(TORSO, z)
    c, s = math.cos(angle), math.sin(angle)
    ry = rf if c > 0 else rb
    # ellipse radius along the direction
    return (rx * ry) / math.sqrt((ry * s) ** 2 + (rx * c) ** 2) if (ry or rx) else 0

# ----------------------------------------------------------------------------- build
def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0

    img = bpy.data.images.new('PhiTani_BaseColor', ATLAS, ATLAS, alpha=False)
    material = bpy.data.materials.new('PhiTani')
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = nodes.get('Principled BSDF')
    bsdf.inputs['Roughness'].default_value = .85
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = img
    links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])

    objects = {}
    N12 = 12

    # ---------------- body (skin)
    body = Part()
    head_rings = [ring(0, 0, z, w, f, b, N12) for z, w, f, b in reversed(HEAD)]
    hv = loft(body, head_rings, FACE, caps=(True, True))
    # planar front projection for the face
    head_vertices = {v for row in hv for v in row}
    for face in body.bm.faces:
        if all(v in head_vertices for v in face.verts) or any(v in head_vertices for v in face.verts):
            for loop in face.loops:
                if loop.vert in head_vertices:
                    x, y, z = loop.vert.co
                    loop[body.uv].uv = uvmap(FACE, (x + .10) / .20, (z - 1.385) / (1.665 - 1.385))
    # nose: a small wedge on the face (bridge, tip, two wings), mapped with the same front projection
    def face_uv(v):
        return uvmap(FACE, (v.x + .10) / .20, (v.z - 1.385) / (1.665 - 1.385))
    nb = [Vector((0, -.086, 1.526)), Vector((0, -.109, 1.489)), Vector((-.0145, -.087, 1.486)), Vector((.0145, -.087, 1.486))]
    nv = [body.bm.verts.new(p) for p in nb]
    for a, b, c in ((0, 1, 2), (0, 3, 1), (1, 3, 2), (0, 2, 3)):
        body.face([nv[a], nv[b], nv[c]], [face_uv(nb[a]), face_uv(nb[b]), face_uv(nb[c])])
    # neck
    loft(body, tube_along([Vector((0, .004, 1.345)), Vector((0, 0, 1.43))], [.042, .036], n=10, count=3), SKIN, caps=(False, False), flat=True)
    # torso (skin under the clothes)
    torso_rings = [ring(0, 0, z, w, f, b, N12) for z, w, f, b in reversed(TORSO)]
    loft(body, torso_rings, SKIN, caps=(True, False), flat=True)
    # arms, hands, feet
    for s in (-1, 1):
        arm = [Vector((s * .178, .005, 1.315)), Vector((s * .224, -.014, 1.07)), Vector((s * .254, -.074, .835))]
        loft(body, tube_along(arm, [.043, .035, .030, .022], n=8, count=7), SKIN, caps=(True, True), flat=True)
        hand = [Vector((s * .254, -.074, .835)), Vector((s * .259, -.088, .745)), Vector((s * .264, -.104, .665))]
        loft(body, tube_along(hand, [.026, .034, .030, .012], n=8, count=5, ellipse=(1.0, .46), hint=Vector((1, 0, 0))), SKIN, caps=(True, True), flat=True)
        thumb = [Vector((s * .238, -.085, .81)), Vector((s * .226, -.100, .755)), Vector((s * .224, -.108, .715))]
        loft(body, tube_along(thumb, [.012, .010, .007], n=6, count=4), SKIN, caps=(True, True), flat=True)
        # dangling legs: stub below the hem, foot pointing down (she floats)
        leg = [Vector((s * .058, 0, .30)), Vector((s * .054, -.012, .17)), Vector((s * .052, -.024, .11))]
        loft(body, tube_along(leg, [.030, .025, .021], n=8, count=4), SKIN, caps=(True, False), flat=True)
        foot = [Vector((s * .052, -.024, .11)), Vector((s * .052, -.050, .045)), Vector((s * .052, -.074, .004))]
        loft(body, tube_along(foot, [.022, .026, .014], n=8, count=4, ellipse=(.85, 1.0), hint=Vector((1, 0, 0))), SKIN, caps=(True, True), flat=True)
    recalc(body)
    objects['PhiTani_Body'] = to_object('PhiTani_Body', body, material)

    # ---------------- hair
    hair = Part()
    cap_rings = []
    for z, w, f, b in reversed(HEAD[4:]):
        cap_rings.append(ring(0, .004, z + .004 * (z > 1.6), w * 1.10, f * 1.07, b * 1.13, N12))
    # open at the front below the hairline: build the skull cap from the crown down to z=1.575, closed at top
    skull = []
    for z, w, f, b in [(1.66, .014, .018, .028), (1.636, .043, .050, .074), (1.608, .071, .075, .100), (1.572, .084, .088, .108), (1.535, .087, .094, .106), (1.505, .084, .088, .102)]:
        pts = ring(0, .004, z, w * 1.10, f * 1.06, b * 1.14, N12)
        if z < 1.62:
            # lift the front of the lower rings: a hairline above the brows, centre parting a little higher
            lift = {1.608: .0, 1.572: .026, 1.535: .052, 1.505: .075}[z]
            for k, p in enumerate(pts):
                c = math.cos(2 * math.pi * k / N12)
                front = max(0.0, c) ** 1.2
                part = .008 * max(0.0, c) ** 6
                pts[k] = Vector((p.x, p.y, p.z + lift * front + part))
        skull.append(pts)
    loft(hair, skull, HAIR, caps=(True, False))
    # long hair behind the head and back: thick curved sheet
    back = []
    zs = [1.60, 1.52, 1.42, 1.30, 1.18, 1.05, .93, .84, .78]
    for k, z in enumerate(zs):
        t = k / (len(zs) - 1)
        rx = .098 - .052 * t ** 1.6
        depth = .046 - .022 * t
        cy = .088 + .024 * math.sin(t * math.pi * .9) + .006 * t
        pts = ring(0, cy, z, rx, depth, depth, 10)
        if k >= 5:   # ragged ends
            pts = [Vector((p.x * (1 + .10 * math.sin(j * 2.3 + k)), p.y, p.z + .018 * math.sin(j * 1.7 + 2 * k) * t)) for j, p in enumerate(pts)]
        back.append(pts)
    loft(hair, back, HAIR, caps=(True, True))
    # locks falling in front of the shoulders
    for s in (-1, 1):
        lock = [Vector((s * .084, -.020, 1.505)), Vector((s * .089, -.050, 1.40)), Vector((s * .106, -.085, 1.30)), Vector((s * .110, -.093, 1.18))]
        loft(hair, tube_along(lock, [.014, .018, .015, .005], n=6, count=6, ellipse=(1.0, .8)), HAIR, caps=(True, True))
    recalc(hair)
    objects['PhiTani_Hair'] = to_object('PhiTani_Hair', hair, material)

    # ---------------- skirt (phaasin) + waist band
    skirt = Part()
    skirt_rows = [(1.075, .127, .097), (1.03, .143, .108), (.95, .168, .121), (.80, .164, .118), (.60, .157, .113), (.40, .160, .115), (.17, .188, .140)]
    skirt_rings = [ring(0, 0, z, rx, ry, ry, 14) for z, rx, ry in skirt_rows]
    loft(skirt, skirt_rings, SKIRT, caps=(False, False))
    # dark lining inside the hem so no light shows through
    lining = [ring(0, 0, .30, .120, .090, .090, 14), ring(0, 0, .31, .001, .001, .001, 14)]
    loft(skirt, lining, SKIN, caps=(False, False), flat=True)
    recalc(skirt)
    objects['PhiTani_Skirt'] = to_object('PhiTani_Skirt', skirt, material)

    # ---------------- top (tube top + halter straps)
    top = Part()
    top_rows = [(1.325, .006), (1.21, .008), (1.12, .006)]
    top_rings = []
    for z in (1.318, 1.26, 1.20, 1.14, 1.115):
        rx, rf, rb = interp_table(TORSO, z)
        top_rings.append(ring(0, 0, z, rx + .008, rf + .009, rb + .008, N12))
    loft(top, top_rings, TOP, caps=(False, False))
    for s in (-1, 1):
        strap = [Vector((s * .040, -.080, 1.300)), Vector((s * .036, -.052, 1.345)), Vector((s * .028, -.010, 1.402)), Vector((s * .016, .030, 1.405))]
        loft(top, tube_along(strap, [.008, .007, .007, .006], n=4, count=5, ellipse=(1.4, .6)), GOLD, caps=(True, True), flat=True)
    recalc(top)
    objects['PhiTani_Top'] = to_object('PhiTani_Top', top, material)

    # ---------------- sabai (diagonal sash)
    sabai = Part()
    rows = []
    steps = 16
    for k in range(steps + 1):
        t = k / steps
        z = 1.375 - t * (1.375 - 1.03)
        ang = math.radians(78 - 150 * t)           # over the left shoulder, across the chest, to the right hip
        r = torso_radius_at(z, ang) + .016 + .006 * math.sin(math.pi * t)
        cx, cy = r * math.sin(ang), -r * math.cos(ang)
        centre = Vector((cx, cy, z))
        # sash width runs along the downhill direction of the torso surface
        width = .050 + .006 * math.sin(math.pi * t)
        slope = Vector((math.cos(ang), math.sin(ang), 0))   # tangent around the torso
        slope = Vector((slope.x, -slope.y * -1, 0)).normalized()
        rows.append((centre, width))
    # build ribbon along the path: two edges offset perpendicular to the path (in the surface)
    edge_rows = []
    pts = [c for c, w in rows]
    for k, (c, w) in enumerate(rows):
        T = (pts[min(k + 1, len(pts) - 1)] - pts[max(k - 1, 0)]).normalized()
        outward = Vector((c.x, c.y, 0)).normalized()
        side = T.cross(outward).normalized()
        edge_rows.append([c + side * w, c - side * w])
    # tail: continues down the right hip like a hanging end
    last = rows[-1][0]
    tail_top = pts[-1]
    tail_zs = [1.02, .92, .80, .68, .58]
    for k, z in enumerate(tail_zs):
        t = (k + 1) / len(tail_zs)
        x = tail_top.x * (1 - .25 * t) - .018 * t
        y = tail_top.y * (1 - .45 * t) - .004
        # keep clear of the skirt surface
        rxs, rfs, _ = (lambda z: (interp_table([(r[0], r[1], r[2], r[2]) for r in skirt_rows[::-1]], z)))(z)
        edge_rows.append([Vector((x + .045 + .012 * t, y, z)), Vector((x - .045 - .012 * t, y, z))])
    # thin two-sided strip: an outer and an inner face, each with its own vertices
    rowsN = len(edge_rows)
    outer, inner = [], []
    for k, (a, b) in enumerate(edge_rows):
        mid = (a + b) / 2
        out = Vector((mid.x, mid.y, 0)).normalized() if abs(mid.z) < 1.4 else Vector((0, -1, 0))
        outer.append([sabai.bm.verts.new(a + out * .0015), sabai.bm.verts.new(b + out * .0015)])
        inner.append([sabai.bm.verts.new(a - out * .0015), sabai.bm.verts.new(b - out * .0015)])
    for k in range(rowsN - 1):
        u0, u1 = k / (rowsN - 1), (k + 1) / (rowsN - 1)
        uvs = [uvmap(SABAI, u0, 1), uvmap(SABAI, u0, 0), uvmap(SABAI, u1, 0), uvmap(SABAI, u1, 1)]
        o = [outer[k][0], outer[k][1], outer[k + 1][1], outer[k + 1][0]]
        i = [inner[k][0], inner[k][1], inner[k + 1][1], inner[k + 1][0]]
        sabai.face(o, uvs)
        sabai.face(i[::-1], uvs[::-1])

    objects['PhiTani_Sabai'] = to_object('PhiTani_Sabai', sabai, material)

    # ---------------- jewellery (gold)
    jewel = Part()
    # necklace
    for z, rx, ry in ((1.372, .050, .042),):
        loft(jewel, [ring(0, 0, z + dz, rx + dr, ry + dr, ry + dr, 14) for dz, dr in ((.007, .002), (.002, .005), (-.003, .002))], GOLD, caps=(False, False), flat=True)
    # pendant
    loft(jewel, tube_along([Vector((0, -.048, 1.368)), Vector((0, -.060, 1.335))], [.009, .006], n=5, count=3), GOLD, caps=(True, True), flat=True)
    # bracelets
    for s in (-1, 1):
        for dz in (0, .028):
            centre = Vector((s * (.254 + dz * .0), -.074 - dz * .24, .838 + dz * .0 - dz * .35))
            p0 = Vector((s * .2535, -.0735, .85 - dz))
            loft(jewel, tube_along([p0 + Vector((0, 0, .008)), p0 - Vector((0, 0, .008))], [.0345, .0345], n=10, count=2), GOLD, caps=(False, False), flat=True)
    # waist belt
    belt = [ring(0, 0, z, rx, ry, ry, 14) for z, rx, ry in ((1.085, .1305, .1005), (1.062, .1335, .1035), (1.040, .1465, .1115))]
    loft(jewel, belt, GOLD, caps=(False, False), flat=True)
    recalc(jewel)
    objects['PhiTani_Jewelry'] = to_object('PhiTani_Jewelry', jewel, material)

    # ---------------- flower above the character's right ear (viewer's left)
    flower = Part()
    base = Vector((-.088, -.050, 1.585))
    normal = Vector((-.55, -.80, .22)).normalized()
    ax = normal.cross(Vector((0, 0, 1))).normalized()
    ay = normal.cross(ax).normalized()
    for k in range(5):
        a = 2 * math.pi * k / 5
        d = (ax * math.cos(a) + ay * math.sin(a))
        e = (ax * -math.sin(a) + ay * math.cos(a))
        c0 = base + normal * .004
        tip = base + normal * .008 + d * .052
        left = base + normal * .010 + d * .028 + e * .020
        right = base + normal * .010 + d * .028 - e * .020
        vc = [flower.bm.verts.new(c0), flower.bm.verts.new(left), flower.bm.verts.new(tip), flower.bm.verts.new(right)]
        flower.face(vc, [uvmap(FLOWER, .5, 0), uvmap(FLOWER, .2, .55), uvmap(FLOWER, .5, 1), uvmap(FLOWER, .8, .55)])
        flower.face(vc[::-1], [uvmap(FLOWER, .5, 0), uvmap(FLOWER, .8, .55), uvmap(FLOWER, .5, 1), uvmap(FLOWER, .2, .55)])
    # centre
    cv = [flower.bm.verts.new(base + normal * (.012 if i % 2 == 0 else .014) + (ax * math.cos(2 * math.pi * i / 6) + ay * math.sin(2 * math.pi * i / 6)) * .008) for i in range(6)]
    flower.face(cv, [uvmap(FLOWER, .5, .02)] * 6)
    objects['PhiTani_Flower'] = to_object('PhiTani_Flower', flower, material)
    return scene, img, material, objects

# ----------------------------------------------------------------------------- texture
def px(rect, u, v):
    x0, y0, x1, y1 = rect
    return (x0 + 1 + u * (x1 - x0 - 2), y0 + 1 + (1 - v) * (y1 - y0 - 2))

def paint_texture():
    im = Image.new('RGB', (ATLAS, ATLAS), (20, 20, 20))
    d = ImageDraw.Draw(im, 'RGBA')
    SKINC = (146, 176, 118)
    # ---- skin patch
    d.rectangle(SKIN, fill=SKINC)
    # ---- gold patch
    d.rectangle(GOLD, fill=(214, 172, 62))
    d.rectangle((GOLD[0], GOLD[1], GOLD[2], GOLD[1] + 20), fill=(232, 196, 92))
    # ---- flower: yellow centre (bottom) -> orange -> red -> deep red tip
    x0, y0, x1, y1 = FLOWER
    for yy in range(y0, y1):
        t = (y1 - 1 - yy) / (y1 - y0 - 1)   # 0 bottom, 1 top
        if t < .15: c = (236, 196, 60)
        elif t < .5: k = (t - .15) / .35; c = (int(236 - 16 * k), int(196 - 120 * k), int(60 - 20 * k))
        else: k = (t - .5) / .5; c = (int(220 - 70 * k), int(76 - 40 * k), int(40 - 14 * k))
        d.line((x0, yy, x1, yy), fill=c)
    # ---- face
    fx0, fy0, fx1, fy1 = FACE
    def F(x, z):
        return px(FACE, (x + .10) / .20, (z - 1.385) / (1.665 - 1.385))
    face = Image.new('RGB', (256, 256), SKINC)
    fd = ImageDraw.Draw(face, 'RGBA')
    # soft shading: darker toward the temples and jaw
    for yy in range(256):
        for xx in range(0, 256, 1):
            pass
    shade = Image.new('L', (256, 256), 0)
    sd = ImageDraw.Draw(shade)
    sd.ellipse((40, 20, 216, 250), fill=255)
    shade = shade.filter(ImageFilter.GaussianBlur(26))
    dark = Image.new('RGB', (256, 256), (104, 132, 86))
    face = Image.composite(face, dark, shade)
    fd = ImageDraw.Draw(face, 'RGBA')
    def Fl(x, z):
        u, v = (x + .10) / .20, (z - 1.385) / (1.665 - 1.385)
        return (1 + u * 254, 1 + (1 - v) * 254)
    # eye sockets / dark circles
    for s in (-1, 1):
        cx, cy = Fl(s * .034, 1.537)
        fd.ellipse((cx - 22, cy - 11, cx + 22, cy + 13), fill=(88, 112, 92, 90))
    # eyes
    for s in (-1, 1):
        cx, cy = Fl(s * .034, 1.538)
        fd.ellipse((cx - 15, cy - 7, cx + 15, cy + 7), fill=(226, 230, 214, 255), outline=(30, 34, 30, 255), width=2)
        fd.ellipse((cx - 7, cy - 7, cx + 7, cy + 7), fill=(58, 74, 60, 255))
        fd.ellipse((cx - 4, cy - 5, cx + 4, cy + 5), fill=(8, 8, 8, 255))
        fd.ellipse((cx + 1, cy - 6, cx + 4, cy - 3), fill=(255, 255, 255, 255))
        fd.arc((cx - 19, cy - 14, cx + 19, cy + 6), 200, 340, fill=(18, 18, 18, 255), width=3)   # upper lid
        fd.arc((cx - 17, cy - 4, cx + 17, cy + 12), 20, 160, fill=(80, 100, 82, 170), width=2)   # lower lid
    # brows
    for s in (-1, 1):
        a = Fl(s * .014, 1.569); b = Fl(s * .036, 1.578); c = Fl(s * .056, 1.564)
        fd.line([a, b, c], fill=(36, 34, 30, 255), width=2)
    # nose
    nx, ny = Fl(0, 1.495)
    fd.line([Fl(0, 1.545), (nx, ny)], fill=(110, 132, 96, 150), width=4)
    for s in (-1, 1):
        px_, py_ = Fl(s * .011, 1.487)
        fd.ellipse((px_ - 4, py_ - 3, px_ + 4, py_ + 3), fill=(70, 88, 66, 220))
    fd.ellipse((nx - 6, ny - 10, nx + 6, ny), fill=(196, 212, 164, 120))
    # lips
    lx, ly = Fl(0, 1.452)
    fd.ellipse((lx - 17, ly - 6, lx + 17, ly + 9), fill=(104, 44, 54, 255))
    fd.line([(lx - 17, ly + 1), (lx, ly + 3), (lx + 17, ly + 1)], fill=(40, 14, 20, 255), width=2)
    fd.ellipse((lx - 9, ly - 6, lx + 9, ly - 1), fill=(136, 62, 72, 255))
    im.paste(face, (fx0, fy0))
    # ---- hair: black with long strands
    hx0, hy0, hx1, hy1 = HAIR
    d.rectangle(HAIR, fill=(13, 13, 16))
    for _ in range(260):
        xx = random.randint(hx0, hx1 - 1); w = random.choice((1, 1, 2, 3))
        c = random.choice(((30, 30, 40), (24, 24, 32), (6, 6, 8), (38, 38, 52)))
        d.line((xx, hy0, xx + random.randint(-3, 3), hy1), fill=c + (200,), width=w)
    # ---- sabai: cream-gold cloth with emerald borders and tiny gold motifs along its length
    sx0, sy0, sx1, sy1 = SABAI
    d.rectangle(SABAI, fill=(232, 214, 160))
    d.rectangle((sx0, sy0, sx1, sy0 + 9), fill=(20, 96, 58)); d.rectangle((sx0, sy1 - 10, sx1, sy1), fill=(20, 96, 58))
    d.rectangle((sx0, sy0 + 10, sx1, sy0 + 13), fill=(214, 168, 56)); d.rectangle((sx0, sy1 - 14, sx1, sy1 - 11), fill=(214, 168, 56))
    for xx in range(sx0 + 6, sx1, 16):
        cy = (sy0 + sy1) // 2
        d.polygon([(xx, cy), (xx + 6, cy - 9), (xx + 12, cy), (xx + 6, cy + 9)], fill=(206, 154, 52))
        d.ellipse((xx + 4, cy - 2, xx + 8, cy + 2), fill=(20, 96, 58))
    # ---- skirt: emerald silk, gold diamond lattice and a broad flame-pattern hem
    kx0, ky0, kx1, ky1 = SKIRT
    d.rectangle(SKIRT, fill=(16, 84, 50))
    for yy in range(ky0, ky1, 2):                      # subtle silk sheen stripes
        d.line((kx0, yy, kx1, yy), fill=(20, 96, 58, 90), width=1)
    d.rectangle((kx0, ky0, kx1, ky0 + 22), fill=(210, 166, 58))   # waist band (hidden under the belt)
    d.rectangle((kx0, ky0 + 24, kx1, ky0 + 27), fill=(232, 196, 92))
    body_top, body_bottom = ky0 + 34, ky1 - 62
    for row, yy in enumerate(range(body_top, body_bottom, 26)):
        for xx in range(kx0 - 16 + (16 if row % 2 else 0), kx1 + 16, 32):
            d.polygon([(xx, yy), (xx + 12, yy + 13), (xx, yy + 26), (xx - 12, yy + 13)], outline=(214, 168, 56, 255), width=2)
            d.ellipse((xx - 3, yy + 10, xx + 3, yy + 16), fill=(214, 168, 56, 255))
    hem_top = ky1 - 56
    d.rectangle((kx0, hem_top, kx1, ky1), fill=(190, 146, 48))
    d.rectangle((kx0, hem_top + 4, kx1, hem_top + 7), fill=(240, 208, 112))
    for xx in range(kx0, kx1, 21):                        # flame / lotus-petal motif
        d.polygon([(xx, ky1 - 6), (xx + 10, hem_top + 14), (xx + 21, ky1 - 6)], fill=(16, 84, 50))
        d.polygon([(xx + 5, ky1 - 6), (xx + 10, hem_top + 28), (xx + 16, ky1 - 6)], fill=(232, 196, 92))
    d.rectangle((kx0, ky1 - 5, kx1, ky1), fill=(16, 84, 50))
    # ---- top: darker emerald with gold trim and a small lattice
    tx0, ty0, tx1, ty1 = TOP
    d.rectangle(TOP, fill=(12, 70, 42))
    d.rectangle((tx0, ty0, tx1, ty0 + 10), fill=(210, 166, 58)); d.rectangle((tx0, ty1 - 10, tx1, ty1), fill=(210, 166, 58))
    for row, yy in enumerate(range(ty0 + 16, ty1 - 24, 18)):
        for xx in range(tx0 - 10 + (9 if row % 2 else 0), tx1 + 10, 18):
            d.polygon([(xx, yy), (xx + 7, yy + 9), (xx, yy + 18), (xx - 7, yy + 9)], outline=(214, 168, 56, 220), width=1)
    path = OUT / 'Textures' / 'PhiTani_BaseColor.png'
    im.save(path)
    return im, path

if __name__ == '__main__':
    scene, img, material, objects = build()
    im, tex_path = paint_texture()
    img.filepath_raw = str(tex_path)
    img.source = 'FILE'
    img.reload()
    img.colorspace_settings.name = 'sRGB'
    img.pack()   # keep the atlas inside the .blend so the source opens anywhere
    # report
    total_tris = 0
    report = {}
    for name, obj in objects.items():
        obj.data.calc_loop_triangles()
        tris = len(obj.data.loop_triangles)
        total_tris += tris
        report[name] = {'vertices': len(obj.data.vertices), 'triangles': tris}
    zs = [(obj.matrix_world @ v.co).z for obj in objects.values() for v in obj.data.vertices]
    xs = [(obj.matrix_world @ v.co).x for obj in objects.values() for v in obj.data.vertices]
    ys = [(obj.matrix_world @ v.co).y for obj in objects.values() for v in obj.data.vertices]
    print(json.dumps({'objects': report, 'triangles': total_tris, 'height': max(zs) - min(zs), 'zmin': min(zs), 'width': max(xs) - min(xs), 'depth': max(ys) - min(ys)}, indent=1))
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'PhiTani.blend'))
