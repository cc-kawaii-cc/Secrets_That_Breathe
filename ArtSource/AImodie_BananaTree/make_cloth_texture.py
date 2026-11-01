"""Three-colour sheer cloth (yellow / green / pink) for the sacred-cloth banana tree.  python make_cloth_texture.py <out_dir>

banana_cloth_albedo.png  RGB 512x512, tileable in u.  Stripes run along u, the colour changes with v:
    v 0.00-0.50 pink, 0.50-0.86 green, 0.86-1.00 yellow   (v = 0 is the bottom edge of the wrapped band)
"""
import sys

import numpy as np
from PIL import Image
from scipy import ndimage as ndi

OUT = sys.argv[-1]
N = 512
r = np.random.RandomState(7)


def wn(sy, sx, seed):
    a = ndi.gaussian_filter(np.random.RandomState(seed).standard_normal((N, N)), (sy, sx), mode='wrap')
    return (a - a.mean()) / a.std()


v = 1.0 - (np.arange(N)[:, None] + 0.5) / N * np.ones((1, N))     # top image row = v 1
pink = np.array([228, 54, 152], np.float32)
green = np.array([48, 178, 96], np.float32)
yellow = np.array([244, 208, 46], np.float32)


def sm(x, a, b):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


w_g = sm(v, 0.485, 0.515)
w_y = sm(v, 0.845, 0.875)
col = pink[None, None] * (1 - w_g[..., None]) + green[None, None] * w_g[..., None]
col = col * (1 - w_y[..., None]) + yellow[None, None] * w_y[..., None]

# woven / tulle structure + soft folds
yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
weave = 0.5 * np.sin(xx * 2 * np.pi / 4.0) * np.sin(yy * 2 * np.pi / 4.0) + 0.5 * np.sin(xx * 2 * np.pi / 8.0 + yy * 0.7)
folds = wn(40, 6, 1)
shade = 1 + 0.045 * weave + 0.07 * folds + 0.03 * wn(2, 2, 2)
col = col * shade[..., None]
Image.fromarray(np.clip(col, 0, 255).astype(np.uint8), "RGB").save(f"{OUT}/banana_cloth_albedo.png")
print("cloth texture ok")
