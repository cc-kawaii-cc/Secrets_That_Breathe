"""Procedural banana tree -> FBX (LOD0/1/2) for Unity 6 URP.

Run with the bpy venv:   python build_banana.py <out_dir> <leaf_profile.json>

Mesh conventions (documented again in the README that ships with the package)
  UV0       : texture coordinates (trunk / leaf / fruit atlases)
  UV1       : x = "young leaf" tint weight (suckers), y = unused
  Colour    : R = trunk bend weight, G = leaf / bunch sway weight,
              B = blade flutter weight, A = per-leaf random phase
  Materials : 0 BananaTrunk (trunk, petioles, sucker stems), 1 BananaLeaf (blades, alpha clip),
              2 BananaFruit (fingers + bunch stalk)
Units: metres, Z-up in Blender, exported Y-up / -Z forward for Unity (no extra scale needed).
"""
import json
import math
import sys

import bpy
import numpy as np
from scipy.interpolate import splev, splprep

OUT = sys.argv[-2]
PROFILE = json.load(open(sys.argv[-1]))

import os
RIPE = os.environ.get('BANANA_RIPE') == '1'
FRUIT_MAT = 'BananaFruitRipe' if RIPE else 'BananaFruit'
FRUIT_TEX = 'banana_fruit_ripe_albedo.png' if RIPE else 'banana_fruit_albedo.png'
BASE = 'BananaTree_Ripe' if RIPE else 'BananaTree'
TRUNK_H = 2.1
V_SLOPE = float(__import__('os').environ.get('V_SLOPE', 1.1))      # blade half-angle near the midrib (tan) -> ~43 deg, keeps the blade visible from every side
TWIST_SCALE = float(__import__('os').environ.get('TWIST_SCALE', 2.0))
LEAF_SCALE = float(__import__('os').environ.get('LEAF_SCALE', 0.72))   # uniform scale of the big leaves (length, width and petiole)
V_OUTER = float(__import__('os').environ.get('V_OUTER', 0.8))
TRUNK_R_T = [0, 0.06, 0.25, 0.7, 1.0]
TRUNK_R_V = [0.215, 0.20, 0.175, 0.14, 0.12]
M_TRUNK, M_LEAF, M_FRUIT = 0, 1, 2


# ----------------------------------------------------------------------------- mesh container
class Mesh:
    def __init__(self):
        self.v, self.n, self.uv, self.uv2, self.col, self.mat = [], [], [], [], [], []
        self.tris = []
        self.blades = []

    @property
    def nv(self):
        return len(self.v)

    def add(self, p, n, uv, col, mat, uv2=(0.0, 0.0)):
        self.v.append(p); self.n.append(n); self.uv.append(uv)
        self.col.append(col); self.mat.append(mat); self.uv2.append(uv2)
        return len(self.v) - 1

    def tri(self, a, b, c, mat):
        self.tris.append((a, b, c, mat))

    def tri_count(self):
        return len(self.tris)


def norm(v):
    v = np.asarray(v, np.float64)
    l = np.linalg.norm(v, axis=-1, keepdims=True)
    return v / np.maximum(l, 1e-9)


def rot_about(v, axis, ang):
    axis = norm(axis)
    c, s = math.cos(ang), math.sin(ang)
    return v * c + np.cross(axis, v) * s + axis * np.dot(axis, v) * (1 - c)


def parallel_frames(P):
    P = np.asarray(P, np.float64)
    T = np.gradient(P, axis=0)
    T = norm(T)
    ref = np.array([0.0, 0.0, 1.0]) if abs(T[0][2]) < 0.9 else np.array([1.0, 0.0, 0.0])
    N0 = norm(np.cross(np.cross(T[0], ref), T[0]))
    Ns, Bs = [N0], [np.cross(T[0], N0)]
    for i in range(1, len(P)):
        axis = np.cross(T[i - 1], T[i])
        if np.linalg.norm(axis) > 1e-8:
            ang = math.asin(min(1.0, np.linalg.norm(axis)))
            n = rot_about(Ns[-1], axis, ang)
        else:
            n = Ns[-1]
        n = norm(n - T[i] * np.dot(n, T[i]))
        Ns.append(n)
        Bs.append(np.cross(T[i], n))
    return T, np.array(Ns), np.array(Bs)


def add_tube(m, P, radii, sides, mat, v_of_ring, colfn, uv_u_scale=1.0, cap_end=False, cap_start=False,
             uv_v_fixed=None, ridge=None, uv2=(0.0, 0.0), phase_u=0.0):
    """swept tube. colfn(i, ring_fraction) -> RGBA ; v_of_ring(i) -> v"""
    P = np.asarray(P, np.float64)
    R = len(P)
    T, N, B = parallel_frames(P)
    rr = np.asarray(radii, np.float64)
    seg = np.linalg.norm(np.diff(P, axis=0), axis=1)
    seg = np.concatenate([seg, seg[-1:]])
    drds = np.gradient(rr) / np.maximum(seg, 1e-6)
    first = m.nv
    for i in range(R):
        for k in range(sides + 1):
            th = 2 * math.pi * k / sides
            d = N[i] * math.cos(th) + B[i] * math.sin(th)
            rmul = 1.0 if ridge is None else ridge(th, i)
            p = P[i] + d * rr[i] * rmul
            nrm = norm(d - T[i] * drds[i])
            v = v_of_ring(i) if uv_v_fixed is None else uv_v_fixed
            m.add(p, nrm, (uv_u_scale * k / sides + phase_u, v), colfn(i, i / max(R - 1, 1)), mat, uv2)
    S = sides + 1
    for i in range(R - 1):
        for k in range(sides):
            a = first + i * S + k
            b = a + 1
            c = a + S + 1
            d = a + S
            m.tri(a, b, c, mat)
            m.tri(a, c, d, mat)
    if cap_end:
        i = R - 1
        ci = m.add(P[i], T[i], (0.5, v_of_ring(i) if uv_v_fixed is None else uv_v_fixed),
                   colfn(i, 1.0), mat, uv2)
        for k in range(sides):
            a = first + i * S + k
            m.tri(ci, a, a + 1, mat)
    if cap_start:
        i = 0
        ci = m.add(P[i], -T[i], (0.5, v_of_ring(i) if uv_v_fixed is None else uv_v_fixed),
                   colfn(i, 0.0), mat, uv2)
        for k in range(sides):
            a = first + i * S + k
            m.tri(ci, a + 1, a, mat)
    return first


# ----------------------------------------------------------------------------- trunk
def build_trunk(m, sides, rings):
    zs = np.linspace(-0.02, TRUNK_H, rings)
    t = zs / TRUNK_H
    lean = 0.035 * t ** 2
    P = np.stack([lean, np.zeros_like(zs), zs], axis=1)
    # base flare -> slim -> slight swelling under the crown
    r = np.interp(t, TRUNK_R_T, TRUNK_R_V)
    r = r * (1 + 0.018 * np.sin(2 * math.pi * zs / 0.42))        # leaf-sheath rings

    def ridge(th, i):
        return 1 + 0.035 * math.sin(3 * th + 0.7) + 0.02 * math.sin(5 * th + 2.1)

    def colfn(i, f):
        w = max(zs[i], 0) / TRUNK_H
        return (w * w, 0.0, 0.0, 0.0)

    add_tube(m, P, r, sides, M_TRUNK, lambda i: float(np.clip(t[i], 0, 1)), colfn, cap_end=True, ridge=ridge)
    return P, r


# ----------------------------------------------------------------------------- leaves
PROF = np.array(PROFILE["profile_left_right_px"])      # 33 x 2  (left, right) from base to tip
CROP_W, CROP_H, MID_X = PROFILE["crop_w"], PROFILE["crop_h"], PROFILE["midrib_x"]
PROF_T = np.linspace(0, 1, len(PROF))


def midrib_curve(phi, z0, Lp, Lb, th0, kappa, lean_x, step=0.02):
    L = Lp + Lb
    n = max(int(L / step), 8)
    s = np.linspace(0, L, n + 1)
    th = th0 - kappa * (s / L) ** 1.35
    d = np.array([math.cos(phi), math.sin(phi), 0.0])
    pts = [np.array([0.0, 0.0, 0.0])]
    for i in range(1, len(s)):
        ds = s[i] - s[i - 1]
        a = 0.5 * (th[i] + th[i - 1])
        pts.append(pts[-1] + (d * math.cos(a) + np.array([0, 0, 1.0]) * math.sin(a)) * ds)
    return s, np.array(pts), th, d


def sample_curve(s, pts, q):
    return np.array([np.interp(q, s, pts[:, k]) for k in range(3)])


def add_leaf(m, attach, phi, Lp, Lb, th0_deg, kappa_deg, wstretch, twist_deg, nr, nc, pet_sides, pet_rings,
             phase, young=0.0, lanceolate=False, ripple=1.0, bend_w=1.0, trunk_r=0.105, pet_r=(0.058, 0.022)):
    s, pts, th, d = midrib_curve(phi, 0, Lp, Lb, math.radians(th0_deg), math.radians(kappa_deg), 0)
    Ltot = Lp + Lb
    start = np.array(attach, np.float64)
    # petiole
    pet_q = np.linspace(0, Lp, pet_rings)
    PP = np.array([sample_curve(s, pts, q) for q in pet_q]) + start
    pr = np.interp(pet_q, [0, Lp], pet_r)

    def pcol(i, f):
        return (bend_w, pet_q[i] / Ltot, 0.0, phase)

    add_tube(m, PP, pr, pet_sides, M_TRUNK, lambda i: 0.35 + 0.5 * pet_q[i] / Lp, pcol, cap_start=False, uv2=(young, 0.0),
             uv_u_scale=1.0)
    # blade
    sx = Lb / CROP_H * wstretch
    cols = 2 * nc + 1
    first = m.nv
    ts = np.linspace(0, 1, nr + 1)
    mid_u = MID_X / CROP_W
    zup = np.array([0.0, 0.0, 1.0])
    lat_axis = np.array([-d[1], d[0], 0.0])
    for i, t in enumerate(ts):
        q = Lp + t * Lb
        c = sample_curve(s, pts, q) + start
        qa = min(q + 0.01, Ltot)
        qb = max(q - 0.01, 0)
        T = norm(sample_curve(s, pts, qa) - sample_curve(s, pts, qb))
        L = norm(lat_axis - T * np.dot(lat_axis, T))
        Nrm = norm(np.cross(T, L))
        ang = math.radians(twist_deg * TWIST_SCALE) * t ** 1.4
        L2 = L * math.cos(ang) + Nrm * math.sin(ang)
        N2 = Nrm * math.cos(ang) - L * math.sin(ang)
        if lanceolate:
            hw_l = hw_r = math.sin(math.pi * min(t * 1.04 + 0.02, 1.0)) ** 0.8 * Lb * 0.15 * wstretch / 0.84
            lpx, rpx = hw_l, hw_r
            lw, rw = lpx, rpx
        else:
            lp = np.interp(t, PROF_T, PROF[:, 0])
            rp = np.interp(t, PROF_T, PROF[:, 1])
            lw, rw = lp * sx, rp * sx
        for j in range(cols):
            f = -1.0 + 2.0 * j / (cols - 1)           # -1 (image left) .. +1 (image right)
            lat = -f * (lw if f < 0 else rw)
            half = max(lw if f < 0 else rw, 1e-4)
            aa = abs(lat) / half
            fold = half * (V_SLOPE * min(aa, 0.55) + V_OUTER * max(aa - 0.55, 0.0))
            rip = ripple * 0.012 * Lb * math.sin(11.0 * t + 3.0 * phase + (2 if f < 0 else 0)) * f * f
            p = c + L2 * lat + N2 * (fold + rip)
            lp_px = np.interp(t, PROF_T, PROF[:, 0]); rp_px = np.interp(t, PROF_T, PROF[:, 1])
            px = f * (lp_px if f < 0 else rp_px) * (0.93 if lanceolate else 1.0)
            uvv = ((MID_X + px) / CROP_W, t)
            colv = (bend_w, (pet_q[-1] + t * Lb) / Ltot, abs(f) * (0.35 + 0.65 * t) ** 1.2, phase)
            m.add(p, (0, 0, 1), uvv, colv, M_LEAF, (young, 0.0))
    # blade triangles (orientation fixed after we know normals -> test one quad)
    S = cols
    a0 = first
    tris = []
    for i in range(nr):
        for j in range(cols - 1):
            a = first + i * S + j
            b = a + 1
            c = a + S + 1
            dd = a + S
            tris.append((a, c, b)); tris.append((a, dd, c))
    # check orientation vs. expected "up" normal (N of first blade row)
    va, vb, vc = [np.array(m.v[k]) for k in tris[0]]
    fn = np.cross(vb - va, vc - va)
    ref = np.cross(norm(sample_curve(s, pts, min(Lp + 0.02, Ltot)) - sample_curve(s, pts, Lp)), lat_axis)
    if np.dot(fn, np.array([0, 0, 1.0]) if True else ref) < 0:
        tris = [(a, c, b) for (a, b, c) in tris]
    _t0 = m.tri_count()
    for a, b, c in tris:
        m.tri(a, b, c, M_LEAF)
    m.blades.append((_t0, m.tri_count()))
    # smooth normals for the blade
    acc = np.zeros((cols * (nr + 1), 3))
    for a, b, c in tris:
        pa, pb, pc = np.array(m.v[a]), np.array(m.v[b]), np.array(m.v[c])
        fnn = np.cross(pb - pa, pc - pa)
        for k in (a, b, c):
            acc[k - first] += fnn
    nn = norm(acc)
    for k in range(cols * (nr + 1)):
        m.n[first + k] = tuple(nn[k])
    return PP


# ----------------------------------------------------------------------------- fruit
def build_bunch(m, cfg, beta_deg=250.0):
    beta = math.radians(beta_deg)
    d = np.array([math.cos(beta), math.sin(beta), 0.0])
    ctrl = np.array([[0.0, 1.95], [0.03, 2.07], [0.15, 2.14], [0.31, 2.08], [0.42, 1.90], [0.45, 1.70], [0.45, 1.32],
                     [0.445, 1.05]])
    tck, _ = splprep([ctrl[:, 0], ctrl[:, 1]], s=0, k=3)
    u = np.linspace(0, 1, cfg["rachis_rings"])
    xs, zs = splev(u, tck)
    P = np.stack([xs * d[0], xs * d[1], zs + 0.0], axis=1)
    trunk_lean = 0.035 * (np.clip(zs, 0, TRUNK_H) / TRUNK_H) ** 2
    P[:, 0] += trunk_lean
    rr = np.interp(u, [0, 0.35, 1.0], [0.040, 0.034, 0.024])
    phase = 0.61

    def col(i, f):
        w = float(np.clip(zs[i], 0, 3) / TRUNK_H)
        return (w * w, 0.15 + 0.85 * max(0.0, (2.0 - zs[i]) / 1.0), 0.0, phase)

    add_tube(m, P, rr, cfg["rachis_sides"], M_FRUIT, lambda i: 0.93, col, cap_end=True, uv_v_fixed=0.93)

    # hands hang from the straight part of the rachis
    n_h = cfg["hands"]
    hz = np.linspace(1.58, 1.20, n_h) if n_h > 1 else [1.4]
    per = cfg["fingers"]            # fingers per hand (two rows)
    half = per // 2
    for h, z in enumerate(hz):
        # point on rachis at this height
        k = int(np.argmin(np.abs(P[:, 2] - z) + (np.arange(len(P)) < len(P) // 2) * 10))
        base = P[k].copy()
        gam = math.radians(beta_deg + 20.0 + h * 118.0)
        out = np.array([math.cos(gam), math.sin(gam), 0.0])
        tang = np.array([-math.sin(gam), math.cos(gam), 0.0])
        for row in range(2):
            for fi in range(half):
                off = (fi - (half - 1) / 2.0)
                fan = off * 0.26
                a0 = math.radians(-52.0 - 20.0 * row + 6 * (h % 2))
                a1 = math.radians(38.0 - 6.0 * row)
                flen = (0.25 - 0.016 * abs(off) / max(half, 1) - 0.012 * row) * cfg.get('flen_scale', 1.0)
                bp = base + out * 0.046 + tang * off * 0.056 + np.array([0, 0, -0.042 * row - 0.010 * abs(off)])
                rings = cfg["finger_rings"]
                if rings == 2:       # LOD2 : one fat segment
                    ss = np.array([0.0, 1.0])
                else:
                    ss = np.interp(np.linspace(0, 1, rings), [0, 1], [0, 1])
                    if rings == 4:
                        ss = np.array([0.0, 0.28, 0.62, 1.0])
                pts = [bp]
                nsteps = 24
                dirs = []
                for q in range(1, nsteps + 1):
                    s_ = q / nsteps
                    a = a0 + (a1 - a0) * s_
                    dv = norm(out * math.cos(a) + np.array([0, 0, 1.0]) * math.sin(a) + tang * fan)
                    pts.append(pts[-1] + dv * flen / nsteps)
                pts = np.array(pts)
                sg = np.linspace(0, 1, nsteps + 1)
                FP = np.array([[np.interp(s, sg, pts[:, c]) for c in range(3)] for s in ss])
                fr = np.interp(ss, [0, 0.12, 0.5, 0.9, 1.0], [0.012, 0.019, 0.037, 0.029, 0.008])
                if cfg.get("finger_scale", 1.0) != 1.0:
                    fr = fr * cfg["finger_scale"]
                ph = 0.61

                def fcol(i, f, base=base, ph=ph):
                    w = float(np.clip(base[2], 0, 3) / TRUNK_H)
                    return (w * w, 0.15 + 0.85 * max(0.0, (2.0 - base[2]) / 1.0), 0.0, ph)

                add_tube(m, FP, fr, cfg["finger_sides"], M_FRUIT, lambda i, ss=ss: 1.0 - float(ss[i]), fcol,
                         cap_end=True)


# ----------------------------------------------------------------------------- tree
LEAVES = [
    # phi, z_attach, Lp, Lb, th0, kappa, wstretch, twist
    (10.0, 1.62, 0.38, 2.10, 10, 112, 1.00, 18),
    (147.5, 1.72, 0.42, 2.25, 14, 108, 1.00, -22),
    (285.0, 1.81, 0.46, 2.40, 18, 102, 1.00, 20),
    (62.5, 1.89, 0.50, 2.45, 22, 96, 1.00, -18),
    (200.0, 1.95, 0.54, 2.40, 28, 90, 1.00, 24),
    (337.5, 2.01, 0.57, 2.30, 34, 82, 1.00, -20),
    (115.0, 2.05, 0.60, 2.10, 44, 72, 0.96, 16),
    (252.5, 2.08, 0.60, 1.70, 58, 60, 0.92, -14),
    (30.0, 2.10, 0.50, 1.10, 80, 24, 0.60, 0),
    (88.0, 1.78, 0.45, 2.20, 24, 100, 1.00, 20),
    (312.0, 1.86, 0.50, 2.30, 30, 94, 1.00, -20),
]

PUPS = [
    # azimuth deg, radius from trunk, [(phi, Lp, Lb, th0, kappa, w)]
    (200.0, 0.32, [(215.0, 0.26, 0.95, 58, 42, 0.8), (165.0, 0.20, 0.80, 66, 34, 0.75), (262.0, 0.16, 0.65, 72, 24, 0.7)]),
    (18.0, 0.30, [(25.0, 0.24, 0.88, 62, 38, 0.8), (345.0, 0.18, 0.66, 70, 26, 0.7)]),
]

LOD_CFG = {
    0: dict(trunk_sides=14, trunk_rings=17, leaves=list(range(11)), nr=12, nc=3, pet_sides=6, pet_rings=6,
            pup_nr=8, pup_nc=2, pup_pet_sides=4, pup_pet_rings=4, pup_leaf_counts=(3, 2), pup_stem_sides=6, pup_stem_rings=4,
            rachis_rings=9, rachis_sides=6, hands=5, fingers=8, finger_sides=5, finger_rings=4, finger_scale=1.12, flen_scale=1.08),
    1: dict(trunk_sides=10, trunk_rings=9, leaves=[0, 1, 2, 3, 5, 6, 8, 9], nr=10, nc=2, pet_sides=4, pet_rings=5,
            pup_nr=5, pup_nc=1, pup_pet_sides=3, pup_pet_rings=3, pup_leaf_counts=(2, 2), pup_stem_sides=5, pup_stem_rings=3,
            rachis_rings=6, rachis_sides=5, hands=4, fingers=6, finger_sides=4, finger_rings=3, finger_scale=1.3, flen_scale=1.05),
    2: dict(trunk_sides=6, trunk_rings=5, leaves=[1, 3, 5, 6, 8, 10], nr=5, nc=1, pet_sides=3, pet_rings=3,
            pup_nr=2, pup_nc=1, pup_pet_sides=3, pup_pet_rings=2, pup_leaf_counts=(1, 1), pup_stem_sides=3, pup_stem_rings=2,
            rachis_rings=4, rachis_sides=4, hands=4, fingers=2, finger_sides=4, finger_rings=2, finger_scale=3.4, flen_scale=1.15),
}


def build_lod(lod):
    cfg = LOD_CFG[lod]
    m = Mesh()
    trunk_P, trunk_r = build_trunk(m, cfg["trunk_sides"], cfg["trunk_rings"])
    for idx in cfg["leaves"]:
        phi, za, Lp, Lb, th0, kap, ws, tw = LEAVES[idx]
        Lp, Lb = Lp * LEAF_SCALE, Lb * LEAF_SCALE
        phi = math.radians(phi)
        t = za / TRUNK_H
        rt = float(np.interp(t, TRUNK_R_T, TRUNK_R_V))
        lean = 0.035 * t ** 2
        attach = (lean + math.cos(phi) * rt * 0.82, math.sin(phi) * rt * 0.82, za)
        add_leaf(m, attach, phi, Lp, Lb, th0, kap, ws, tw, cfg["nr"], cfg["nc"], cfg["pet_sides"], cfg["pet_rings"],
                 phase=(idx * 0.381) % 1.0, bend_w=1.0)
    # suckers
    for pi, (az, rad, leaves) in enumerate(PUPS):
        az = math.radians(az)
        base = np.array([math.cos(az) * rad, math.sin(az) * rad, -0.02])
        stem_h = 0.46 - 0.06 * pi
        zs = np.linspace(0, stem_h, cfg["pup_stem_rings"])
        P = np.stack([np.full_like(zs, base[0]) + 0.02 * (zs / stem_h) ** 2, np.full_like(zs, base[1]), zs + base[2]], axis=1)
        r = np.interp(zs / stem_h, [0, 1], [0.034, 0.020])
        bw = (stem_h / TRUNK_H) ** 2
        add_tube(m, P, r, cfg["pup_stem_sides"], M_TRUNK, lambda i, zs=zs: float(zs[i] / stem_h * 0.4 + 0.45),
                 lambda i, f: (bw * f * f, 0.0, 0.0, 0.3 + 0.2 * pi), cap_end=True, uv2=(1.0, 0.0))
        nleaf = cfg["pup_leaf_counts"][pi]
        for li in range(nleaf):
            phi, Lp, Lb, th0, kap, w = leaves[li]
            top = np.array([P[-1][0], P[-1][1], P[-1][2] - 0.02 * li])
            add_leaf(m, tuple(top), math.radians(phi), Lp, Lb, th0, kap, w / 0.84 * 0.84, 0.0, cfg["pup_nr"], cfg["pup_nc"],
                     cfg["pup_pet_sides"], cfg["pup_pet_rings"], phase=(0.2 + 0.31 * li + 0.17 * pi) % 1.0,
                     young=1.0, lanceolate=True, ripple=0.5, bend_w=bw * 1.0, pet_r=(0.016, 0.008))
    build_bunch(m, cfg)
    return m


# ----------------------------------------------------------------------------- visibility check
def analyze(m, res=200):
    """Rasterised silhouette of every leaf blade seen from 36 azimuths x 3 elevations.
    (projected-area sums hide the edge-on case, the silhouette does not)"""
    from PIL import Image, ImageDraw
    V = np.array(m.v, np.float64)
    T = np.array([(a, b, c) for a, b, c, _ in m.tris])
    lo, hi = V.min(0) - 0.1, V.max(0) + 0.1
    R = float(np.linalg.norm(hi - lo)) * 0.5
    ctr = (lo + hi) * 0.5
    out = {}
    for el in (0, 12, 25):
        e = math.radians(el)
        per = [[] for _ in m.blades]
        tot = []
        for azd in range(0, 360, 10):
            az = math.radians(azd)
            fwd = np.array([math.cos(e) * math.cos(az), math.cos(e) * math.sin(az), math.sin(e)])
            right = norm(np.cross(fwd, [0, 0, 1.0]))
            up = np.cross(right, fwd)
            P = np.stack([((V - ctr) @ right) / R * 0.5 * res + res / 2, res / 2 - ((V - ctr) @ up) / R * 0.5 * res], axis=1)
            allim = Image.new("L", (res, res), 0)
            dall = ImageDraw.Draw(allim)
            for li, (t0, t1) in enumerate(m.blades):
                im = Image.new("L", (res, res), 0)
                d = ImageDraw.Draw(im)
                for tri in T[t0:t1]:
                    pts = [tuple(P[k]) for k in tri]
                    d.polygon(pts, fill=255)
                    dall.polygon(pts, fill=255)
                per[li].append((np.array(im) > 0).sum())
            tot.append((np.array(allim) > 0).sum())
        per = np.array(per, np.float64)
        pix_m2 = (2 * R / res) ** 2
        out[el] = (per.min(axis=1) / np.maximum(per.max(axis=1), 1), per.min(axis=1) * pix_m2, min(tot) / max(tot))
    return out


# ----------------------------------------------------------------------------- blender objects
def to_object(m, name, mats):
    mesh = bpy.data.meshes.new(name)
    verts = [tuple(v) for v in m.v]
    faces = [(a, b, c) for a, b, c, _ in m.tris]
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    for pi, (_, _, _, mi) in enumerate(m.tris):
        mesh.polygons[pi].material_index = mi
    uv = mesh.uv_layers.new(name="UVMap")
    uv2 = mesh.uv_layers.new(name="UV_Tint")
    for li, loop in enumerate(mesh.loops):
        vi = loop.vertex_index
        uv.data[li].uv = m.uv[vi]
        uv2.data[li].uv = m.uv2[vi]
    ca = mesh.color_attributes.new(name="WindData", type='FLOAT_COLOR', domain='POINT')
    for vi in range(len(verts)):
        ca.data[vi].color = m.col[vi]
    mesh.color_attributes.active_color = ca
    mesh.color_attributes.render_color_index = 0
    for p in mesh.polygons:
        p.use_smooth = True
    mesh.normals_split_custom_set_from_vertices([tuple(n) for n in m.n])
    obj = bpy.data.objects.new(name, mesh)
    for mt in mats:
        mesh.materials.append(mt)
    return obj


def make_materials(tex_dir):
    mats = []
    for name, tex, kind in (("BananaTrunk", "banana_trunk_albedo.png", "trunk"),
                            ("BananaLeaf", "banana_leaf_albedo.png", "leaf"),
                            (FRUIT_MAT, FRUIT_TEX, "fruit")):
        mt = bpy.data.materials.new(name)
        mt.use_nodes = True
        nt = mt.node_tree
        bsdf = nt.nodes["Principled BSDF"]
        img = bpy.data.images.load(f"{tex_dir}/{tex}")
        tn = nt.nodes.new("ShaderNodeTexImage")
        tn.image = img
        nt.links.new(tn.outputs["Color"], bsdf.inputs["Base Color"])
        bsdf.inputs["Roughness"].default_value = 0.55 if kind != "fruit" else 0.4
        if kind == "leaf":
            nt.links.new(tn.outputs["Alpha"], bsdf.inputs["Alpha"])
        if kind == "trunk":
            nimg = bpy.data.images.load(f"{tex_dir}/banana_trunk_normal.png")
            nimg.colorspace_settings.name = "Non-Color"
            nn = nt.nodes.new("ShaderNodeTexImage"); nn.image = nimg
            nm = nt.nodes.new("ShaderNodeNormalMap")
            nt.links.new(nn.outputs["Color"], nm.inputs["Color"])
            nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
        mats.append(mt)
    return mats


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = make_materials(OUT + "/Textures")
    root = bpy.data.objects.new(BASE, None)
    bpy.context.scene.collection.objects.link(root)
    stats = {}
    for lod in (0, 1, 2):
        m = build_lod(lod)
        obj = to_object(m, f"{BASE}_LOD{lod}", mats)
        bpy.context.scene.collection.objects.link(obj)
        obj.parent = root
        stats[f"LOD{lod}"] = {"triangles": m.tri_count(), "vertices": m.nv}
        print(f"LOD{lod}: {m.tri_count()} tris, {m.nv} verts")
        if lod == 0:
            for el, (lr, area_min, tot_ratio) in analyze(m).items():
                print(f"  view elev {el:2d}: per-leaf silhouette min/max  worst={lr.min():.2f} mean={lr.mean():.2f}"
                      f" | thinnest leaf view {area_min[:11].min():.2f} m2 (mean {area_min[:11].mean():.2f}) | whole tree {tot_ratio:.2f}")
    json.dump(stats, open(OUT + f"/{BASE}_lod_stats.json", "w"), indent=1)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(
        filepath=OUT + f"/{BASE}.fbx",
        use_selection=True,
        object_types={'EMPTY', 'MESH'},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        mesh_smooth_type='OFF',
        use_mesh_modifiers=True,
        colors_type='LINEAR',
        prioritize_active_color=True,
        add_leaf_bones=False,
        path_mode='STRIP',
    )
    bpy.ops.wm.save_as_mainfile(filepath=OUT + f"/{BASE}_source.blend")
    print("exported")


main()
