"""Build the banana-tree textures from the reference photos.

Outputs (in OUT):
  banana_leaf_albedo.png    RGBA  512x1024  leaf cut-out from photo 4 (alpha = leaf silhouette)
  banana_trunk_albedo.png   RGB  1024x1024  pseudostem from photos 6 + 5 (tileable around the trunk)
  banana_trunk_normal.png   RGB  1024x1024  tangent-space normal map (OpenGL / Unity convention)
  banana_fruit_albedo.png   RGB   256x256   green fingers sampled from photos 2 + 3
  leaf_profile.json         silhouette widths used by the mesh builder
"""
import json
import sys

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

UP = "/mnt/user-data/uploads/"
OUT = sys.argv[1]


def load(name):
    return np.array(Image.open(UP + name).convert("RGB"))


leaf_src = load("1791801887895_image.png")    # photo 4
fruit2 = load("1791801839713_image.png")      # photo 2
fruit3 = load("1791801865106_image.png")      # photo 3
trunk5 = load("1791801919449_image.png")      # photo 5
trunk6 = load("1791801934288_image.png")      # photo 6

# --------------------------------------------------------------------------- leaf
h, w, _ = leaf_src.shape
dist_white = 255 - leaf_src.min(axis=2).astype(int)
green_dom = (leaf_src[..., 1].astype(int) - leaf_src[..., 2].astype(int)) > 25
mask = (dist_white > 14) | green_dom
mask = ndi.binary_opening(mask, iterations=1)
lab, n = ndi.label(mask)
sizes = ndi.sum(mask, lab, range(1, n + 1))
mask = lab == (1 + int(np.argmax(sizes)))
mask = ndi.binary_fill_holes(mask)

# leaf blade region only (the petiole stub below the blade is removed from the cut-out)
ys, xs = np.where(mask)
blade_bottom = int(ys.max())
# find where the stub starts: rows whose width collapses
row_w = mask.sum(axis=1)
stub_start = blade_bottom
for y in range(blade_bottom, 0, -1):
    if row_w[y] > 25:
        stub_start = y
        break
mask[stub_start + 1:, :] = False
ys, xs = np.where(mask)
y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
print("leaf bbox", x0, x1, y0, y1)

# erode 1px so no white fringe survives, then colour-bleed outward
core = ndi.binary_erosion(mask, iterations=2)
rgb = leaf_src.astype(np.float32)
idx = ndi.distance_transform_edt(~core, return_distances=False, return_indices=True)
bled = rgb[idx[0], idx[1]]
alpha = ndi.gaussian_filter(core.astype(np.float32), 0.8)
alpha = np.clip((alpha - 0.2) / 0.6, 0, 1)

# midrib x position (brightest narrow vertical line near the centre)
cx_guess = (x0 + x1) // 2
col_profile = rgb[y0 + 40:y1 - 40, cx_guess - 25:cx_guess + 25].mean(axis=(0, 2))
midrib_x = cx_guess - 25 + int(np.argmax(ndi.gaussian_filter1d(col_profile, 1.0)))
print("midrib x", midrib_x, "centre", cx_guess)

# crop to bbox and pad into a 1:2 canvas -> 512x1024
crop_rgb = bled[y0:y1 + 1, x0:x1 + 1]
crop_a = alpha[y0:y1 + 1, x0:x1 + 1]
ch, cw = crop_a.shape
# profile for the mesh (in crop pixels, per row from base (bottom) to tip (top))
core_crop = core[y0:y1 + 1, x0:x1 + 1]
mid_c = midrib_x - x0
rows = np.linspace(ch - 1, 0, 33)
profile = []
for r in rows:
    r = int(round(r))
    cols = np.where(mask[y0 + r, x0:x1 + 1])[0]
    if len(cols) == 0:
        profile.append([0.0, 0.0])
        continue
    profile.append([float(max(mid_c - cols.min(), 0)), float(max(cols.max() - mid_c, 0))])

json.dump({"crop_w": int(cw), "crop_h": int(ch), "midrib_x": int(mid_c),
           "profile_left_right_px": profile}, open(OUT + "/leaf_profile.json", "w"))

# the texture IS the cropped leaf stretched to 512x1024 (UV 0..1 maps to the bbox)
tex = np.dstack([crop_rgb, crop_a * 255]).astype(np.uint8)
leaf_img = Image.fromarray(tex, "RGBA").resize((512, 1024), Image.LANCZOS)
# mild unsharp to compensate for the ~2x up-scale
la = np.array(leaf_img).astype(np.float32)
blur = cv2.GaussianBlur(la[..., :3], (0, 0), 1.6)
la[..., :3] = np.clip(la[..., :3] + 0.6 * (la[..., :3] - blur), 0, 255)
Image.fromarray(la.astype(np.uint8), "RGBA").save(OUT + "/banana_leaf_albedo.png")

# --------------------------------------------------------------------------- trunk
def fit(img, size):
    return np.array(Image.fromarray(img).resize(size, Image.LANCZOS)).astype(np.float32)


H = W = 1024
rng0 = np.random.default_rng(11)


def strip_band(crops, width=320, centres=None):
    """lay vertical strips side by side with soft cross-fades; wraps around in U"""
    n = len(crops)
    acc = np.zeros((H, W, 3), np.float32)
    wsum = np.zeros((1, W, 1), np.float32)
    centres = centres or [int((i + 0.5) * W / n) for i in range(n)]
    win = np.hanning(width + 2)[1:-1].astype(np.float32) ** 1.2
    for img, c in zip(crops, centres):
        s = fit(img, (width, H))
        cols = (np.arange(width) + c - width // 2) % W
        acc[:, cols] += s * win[None, :, None]
        wsum[:, cols] += win[None, :, None]
    return acc / np.maximum(wsum, 1e-4)


# clean green / cream / pink sheath areas from photo 6 (straw background excluded)
photo6 = [
    trunk6[30:370, 62:132],
    trunk6[70:250, 330:400],
    trunk6[210:420, 362:428],
    trunk6[100:300, 468:522],
]
upper = strip_band(photo6)
# remove any leftover straw (saturated orange-yellow with strong fine texture) by inpainting
_u8 = np.clip(upper, 0, 255).astype(np.uint8)
_hsv = cv2.cvtColor(_u8, cv2.COLOR_RGB2HSV)
_gray = cv2.cvtColor(_u8, cv2.COLOR_RGB2GRAY).astype(np.float32)
_hf = np.abs(_gray - cv2.GaussianBlur(_gray, (0, 0), 2.5))
_hf = cv2.GaussianBlur(_hf, (0, 0), 4)
_straw = ((_hsv[..., 0] >= 10) & (_hsv[..., 0] <= 26) & (_hsv[..., 1] > 120) & (_hf > 3.2)).astype(np.uint8)
_straw = cv2.dilate(_straw, np.ones((9, 9), np.uint8))
print("straw px inpainted:", int(_straw.sum()))
upper = cv2.inpaint(_u8, _straw * 255, 6, cv2.INPAINT_TELEA).astype(np.float32)

# dry fibrous brown from photo 5 (trunk body; no sky / leaves / tag), mirrored for a seamless wrap
fib_a = fit(trunk5[700:1300, 1760:1900], (256, H))
fib_b = fit(trunk5[300:800, 1700:1850], (256, H))
lower = np.concatenate([fib_a, fib_a[:, ::-1], fib_b, fib_b[:, ::-1]], axis=1)
lower = cv2.GaussianBlur(lower, (0, 0), 0.6)
lower = lower * np.array([1.00, 0.80, 0.66], np.float32)           # reddish-brown base like photo 1

# greener upper sheath (photo 1 trunk is green with cream/pink streaks)
hsv = cv2.cvtColor(np.clip(upper, 0, 255).astype(np.uint8), cv2.COLOR_RGB2HSV).astype(np.float32)
hsv[..., 0] = np.clip(hsv[..., 0] + 4, 0, 179)
hsv[..., 1] = np.clip(hsv[..., 1] * 1.2 + 8, 0, 255)
upper = cv2.cvtColor(hsv.astype(np.uint8), cv2.COLOR_HSV2RGB).astype(np.float32)

# blend: rows 0..640 sheath, 640..1024 transitions to dry base, with a ragged edge
ragged = ndi.gaussian_filter(rng0.normal(size=(H, W)), (10, 3))
ragged = ragged / np.abs(ragged).max() * 70
rows = np.arange(H, dtype=np.float32)[:, None] + ragged
t = np.clip((rows - 700) / 160.0, 0, 1)
t = (t * t * (3 - 2 * t))[..., None]
canvas = upper * (1 - t) + lower * t

# even out large-scale lighting baked into the photos
lab = cv2.cvtColor(np.clip(canvas, 0, 255).astype(np.uint8), cv2.COLOR_RGB2LAB).astype(np.float32)
low = cv2.GaussianBlur(lab[..., 0], (0, 0), 60)
lab[..., 0] = np.clip(lab[..., 0] - 0.7 * (low - low.mean()), 0, 255)
trunk_albedo = cv2.cvtColor(lab.astype(np.uint8), cv2.COLOR_LAB2RGB)
Image.fromarray(trunk_albedo).save(OUT + "/banana_trunk_albedo.png")

# normal map: height = fibre / ridge detail of both photos
gray = cv2.cvtColor(trunk_albedo, cv2.COLOR_RGB2GRAY).astype(np.float32) / 255.0
fine = gray - cv2.GaussianBlur(gray, (0, 0), 6)           # high-pass: veins, fibres, scars
mid = cv2.GaussianBlur(gray, (0, 0), 2.0)
# vertical ribbing so the sheath layers read as folds (fibres run along V)
xx = np.linspace(0, 8 * np.pi * 2, 1024, dtype=np.float32)
ribs = (np.sin(xx)[None, :] * 0.5 + 0.5) * 0.06
height = 0.55 * fine + 0.35 * (mid - mid.mean()) + ribs
height = cv2.GaussianBlur(height, (0, 0), 0.8)
# wrap-aware sobel in U (the mirror join already makes this seamless)
dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
hp = np.pad(height, ((1, 1), (0, 0)), mode="edge")
dy = (hp[2:] - hp[:-2]) * 0.5
strength = 5.0
nx, ny, nz = -dx * strength, dy * strength, np.ones_like(dx)   # +Y = up in texture space (OpenGL)
ln = np.sqrt(nx * nx + ny * ny + nz * nz)
nrm = np.dstack([nx / ln, ny / ln, nz / ln]) * 0.5 + 0.5
Image.fromarray((nrm * 255).astype(np.uint8)).save(OUT + "/banana_trunk_normal.png")

# --------------------------------------------------------------------------- fruit
def green_stats(img):
    r, g, b = [img[..., i].astype(int) for i in range(3)]
    m = (g > r + 8) & (g > b + 30) & (g > 70)
    px = img[m]
    return np.percentile(px, [15, 50, 85], axis=0)


s3 = green_stats(fruit3[:, 120:560])   # photo 3 fingers
s2 = green_stats(fruit2[:330])         # photo 2 upper (green) hands
print("fruit green percentiles photo3", s3.tolist())
print("fruit green percentiles photo2", s2.tolist())
dark, mid_c_, light = [(s3[i] * 0.6 + s2[i] * 0.4) for i in range(3)]
dark, mid_c_, light = [np.clip(c, 0, 255) for c in (dark, mid_c_, light)]

size = 256
fr = np.zeros((size, size, 3), np.float32)
u = np.linspace(0, 1, size)[None, :]
v = np.linspace(0, 1, size)[:, None]
# 5 ridges around the finger -> lighter on the ridge crest, darker in the trough
ridge = 0.5 + 0.5 * np.cos(u * 2 * np.pi * 5)
body = dark[None, None, :] * (1 - ridge[..., None]) + light[None, None, :] * ridge[..., None]
body = body + (mid_c_ - (dark + light) / 2)[None, None, :] * 0.5
fr[:] = body
# pedicel (v < .14): yellower, darker green; tip (v > .90): dry brown/black
stem_col = np.array([105, 128, 52], np.float32)
tip_col = np.array([66, 52, 30], np.float32)
t_stem = np.clip((0.15 - v) / 0.04, 0, 1)[..., None]
t_tip = np.clip((v - 0.90) / 0.05, 0, 1)[..., None]
fr = fr * (1 - t_stem) + stem_col * t_stem
fr = fr * (1 - t_tip) + tip_col * t_tip
# faint streaks / freckles
rng = np.random.default_rng(7)
noise = ndi.gaussian_filter(rng.normal(size=(size, size)), (6, 1.2))
fr += noise[..., None] * 140
Image.fromarray(np.clip(fr, 0, 255).astype(np.uint8)).save(OUT + "/banana_fruit_albedo.png")
print("done")

# --------------------------------------------------------------------------- ripe (yellow) fruit
def yellow_stats(img):
    r, g, b = [img[..., i].astype(int) for i in range(3)]
    m = (r > 150) & (g > 120) & (b < 120) & (r > b + 80) & (g > b + 50)
    px = img[m]
    print("ripe yellow pixels:", len(px))
    return np.percentile(px, [15, 50, 85], axis=0)


y2 = yellow_stats(fruit2[240:])          # photo 2: the ripe lower hands
print("ripe percentiles", y2.tolist())
dark_y, mid_y, light_y = [np.clip(c, 0, 255) for c in y2]
ridge = 0.5 + 0.5 * np.cos(u * 2 * np.pi * 5)
body = dark_y[None, None, :] * (1 - ridge[..., None]) + light_y[None, None, :] * ridge[..., None]
body = body + (mid_y - (dark_y + light_y) / 2)[None, None, :] * 0.5
fr2 = (body * 0.6 + np.array([248, 210, 48], np.float32) * 0.4).astype(np.float32)   # push towards a clear ripe yellow
stem_y = np.array([150, 160, 60], np.float32)          # pedicel stays a little green
tip_y = np.array([58, 44, 24], np.float32)             # dry black-brown tip
t_stem = np.clip((0.15 - v) / 0.04, 0, 1)[..., None]
t_tip = np.clip((v - 0.91) / 0.04, 0, 1)[..., None]
fr2 = fr2 * (1 - t_stem) + stem_y * t_stem
fr2 = fr2 * (1 - t_tip) + tip_y * t_tip
fr2 += (ndi.gaussian_filter(rng.normal(size=(size, size)), (6, 1.2)))[..., None] * 90
# sugar spots: small brown freckles on the body
spots = rng.random((size, size)) > 0.9935
spots = ndi.binary_dilation(spots, iterations=1) & (v > 0.2) & (v < 0.88)
fr2[spots] = fr2[spots] * 0.45 + np.array([95, 60, 20]) * 0.55
Image.fromarray(np.clip(fr2, 0, 255).astype(np.uint8)).save(OUT + "/banana_fruit_ripe_albedo.png")
print("ripe done")
