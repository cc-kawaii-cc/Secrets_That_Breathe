"""Reproducible original lock-screen art for the iPhone DUO game prop.

No downloaded images or bundled third-party artwork. The backgrounds are
mathematical landscapes; UI is rasterized with the Windows Segoe UI fonts.
Run from any working directory with Python + Pillow + NumPy.
"""
from pathlib import Path
import math
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageCms

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/SecretsThatBreathe/Props/IPhoneDuo/Textures"
PREVIEW = ROOT / "ArtSource/IPhoneDuo"
FONTS = Path("C:/Windows/Fonts")
PROFILE = ImageCms.ImageCmsProfile(ImageCms.createProfile("sRGB")).tobytes()


def font(size, weight="light"):
    name = {"light": "segoeuil.ttf", "normal": "segoeui.ttf", "semi": "segoeuisl.ttf"}[weight]
    return ImageFont.truetype(str(FONTS / name), int(size))


def blend(arr, rgb, alpha):
    a = np.clip(alpha, 0, 1)[..., None]
    return arr * (1 - a) + np.asarray(rgb) * a


def landscape(w, h):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    x, y = xx / (w - 1), yy / (h - 1)
    t = np.clip(y / .77, 0, 1)
    arr = np.array([104, 152, 179]) + (np.array([234, 224, 198]) - [104, 152, 179]) * t[..., None]
    # A broad late-afternoon glow, kept behind the quiet central clock area.
    glow = np.exp(-(((x - .76) / .5) ** 2 + ((y - .41) / .37) ** 2)) * .32
    arr = blend(arr, [255, 243, 211], glow)
    # Distant angular ridges become warmer and more detailed towards the viewer.
    for i, (base, amp, rgb) in enumerate([
        (.561, .022, [153, 157, 150]),
        (.587, .034, [132, 139, 134]),
        (.622, .043, [111, 122, 117]),
        (.675, .061, [89, 108, 104]),
    ]):
        seed = 70 + i * 37
        rng = np.random.default_rng(seed)
        ridge1 = np.zeros(w)
        for frequency in (3, 6, 11, 20, 37, 65):
            ridge1 += np.sin(x[0] * frequency * math.pi + rng.uniform(0, math.tau)) / frequency ** .65
        ridge1 /= max(np.max(np.abs(ridge1)), .01)
        ridge = base + amp * ridge1
        depth = y - ridge[None, :]
        mask = depth >= 0
        slope = np.gradient(ridge) * w
        lighting = np.clip(.94 + .07 * slope, .76, 1.12)
        plane = np.tile(np.asarray(rgb)[None, None, :] * lighting[None, :, None], (h, 1, 1))
        # Fine erosion streaks are deliberately low contrast at phone scale.
        striation = np.sin(x * (260 + i * 40) + depth * (90 + i * 25)) * np.clip(depth * 100, 0, 3)
        plane += striation[..., None] * .4
        haze = np.clip(depth * 1.5, 0, .17)
        plane = blend(plane, [208, 200, 174], haze)
        arr[mask] = plane[mask]
    # Wind-sculpted foreground dune sweeping down to the left.
    ridge = .768 - .095 * np.sin(x * 2.55 + .1) + .018 * np.sin(x * 8.2)
    depth = y - ridge
    m = depth >= 0
    dune = np.zeros_like(arr) + [220, 207, 179]
    dune -= np.clip(depth, 0, .4)[..., None] * np.array([70, 67, 58])
    dune += (np.cos(x * 2.4 + .6) * 10)[..., None]
    # Perspective compression of sand ripples, with a very soft ridge highlight.
    ripple = np.sin((depth * 310 + np.sin(x * 3.7) * 1.8) * math.pi)
    amplitude = np.clip(depth * 16, 0, 2.5)
    dune += (ripple * amplitude)[..., None]
    dune += (np.exp(-np.maximum(depth, 0) * 130) * 7)[..., None]
    arr[m] = dune[m]
    # A second, darker dune gives a clean curved silhouette at the very bottom.
    ridge2 = 1.035 - .20 * x ** 1.9
    depth2 = y - ridge2
    m2 = depth2 >= 0
    dune2 = np.zeros_like(arr) + [172, 153, 122]
    dune2 += (np.exp(-np.maximum(depth2, 0) * 75) * 19)[..., None]
    dune2 -= np.clip(depth2, 0, .3)[..., None] * [80, 70, 55]
    dune2 += (np.sin(depth2 * 990 + x * 14) * np.clip(depth2 * 12, 0, 1.2))[..., None]
    arr[m2] = dune2[m2]
    rng = np.random.default_rng(941)
    arr += rng.normal(0, .32, (h, w, 1))
    return Image.fromarray(np.clip(arr, 0, 255).astype("uint8"), "RGB")


def blue_waves(w, h):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    x, y = xx / (w - 1), yy / (h - 1)
    arr = np.array([38, 74, 107]) + (np.array([15, 33, 63]) - [38, 74, 107]) * y[..., None]
    glow = np.exp(-(((x - .85) / .8) ** 2 + ((y - .15) / .6) ** 2))
    arr = blend(arr, [128, 175, 192], glow * .6)
    for i, (base, color) in enumerate([
        (.53, [81, 136, 158]), (.67, [52, 109, 140]),
        (.79, [28, 82, 120]), (.91, [18, 55, 94]),
    ]):
        ridge = base + .10 * np.sin(x * 3.5 + i * .8) - .13 * x
        d = y - ridge
        mask = d >= 0
        plane = np.zeros_like(arr) + color
        plane += np.exp(-np.maximum(d, 0) * 24)[..., None] * [31, 45, 48]
        plane -= np.clip(d, 0, .5)[..., None] * [20, 28, 30]
        plane += (np.sin(d * 120 + x * 2) * .35)[..., None]
        arr[mask] = plane[mask]
    rng = np.random.default_rng(1741)
    arr += rng.normal(0, .23, (h, w, 1))
    return Image.fromarray(np.clip(arr, 0, 255).astype("uint8"), "RGB")


def centered_text(draw, xy, text, ft, fill):
    box = draw.textbbox((0, 0), text, font=ft)
    draw.text((xy[0] - (box[2] - box[0]) / 2 - box[0], xy[1] - box[1]), text, font=ft, fill=fill)


def status(draw, w, h):
    s = h / 1466
    white = (249, 252, 253, 235)
    # Signal bars, Wi-Fi arcs and a simple battery outline, above clock.
    bx, by = w - 158 * s, 64 * s
    for i in range(4):
        bh = (7 + i * 4) * s
        draw.rounded_rectangle((bx + i * 7 * s, by - bh, bx + (i * 7 + 4) * s, by), radius=1.4 * s, fill=white)
    cx, cy = w - 104 * s, 64 * s
    for rad in (9, 16, 23):
        draw.arc((cx - rad * s, cy - rad * s, cx + rad * s, cy + rad * s), 220, 320, fill=white, width=max(2, round(2 * s)))
    draw.ellipse((cx - 2 * s, cy - 3 * s, cx + 2 * s, cy + 1 * s), fill=white)
    x, y = w - 66 * s, 43 * s
    draw.rounded_rectangle((x, y, x + 34 * s, y + 18 * s), radius=4 * s, outline=white, width=max(1, round(2 * s)))
    draw.rounded_rectangle((x + 4 * s, y + 4 * s, x + 28 * s, y + 14 * s), radius=2 * s, fill=white)
    draw.rounded_rectangle((x + 36 * s, y + 5 * s, x + 39 * s, y + 13 * s), radius=1 * s, fill=white)


def shortcuts(overlay, w, h):
    d = ImageDraw.Draw(overlay)
    s = h / 1466
    r = 39 * s
    cy = h - 125 * s
    positions = [w * .13, w * .87] if w < 1500 else [w * .10, w * .90]
    for cx in positions:
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(240, 247, 250, 27), outline=(250, 253, 255, 85), width=max(1, round(s)))
    white = (255, 255, 255, 225)
    cx = positions[0]
    d.polygon([(cx - 10 * s, cy - 17 * s), (cx + 10 * s, cy - 17 * s), (cx + 7 * s, cy - 6 * s), (cx - 7 * s, cy - 6 * s)], fill=white)
    d.rounded_rectangle((cx - 6 * s, cy - 5 * s, cx + 6 * s, cy + 17 * s), radius=3 * s, outline=white, width=max(2, round(2.5 * s)))
    d.line((cx - 8 * s, cy - 21 * s, cx + 8 * s, cy - 21 * s), fill=white, width=max(2, round(2 * s)))
    cx = positions[1]
    d.rounded_rectangle((cx - 18 * s, cy - 10 * s, cx + 18 * s, cy + 15 * s), radius=5 * s, outline=white, width=max(2, round(2.4 * s)))
    d.polygon([(cx - 11 * s, cy - 10 * s), (cx - 7 * s, cy - 17 * s), (cx + 6 * s, cy - 17 * s), (cx + 10 * s, cy - 10 * s)], fill=white)
    d.ellipse((cx - 7 * s, cy - 5 * s, cx + 7 * s, cy + 9 * s), outline=white, width=max(2, round(2 * s)))
    # Home indicator follows the physical bottom edge, with generous safe space.
    length = (390 if w > 1500 else 265) * s
    d.rounded_rectangle((w / 2 - length / 2, h - 37 * s, w / 2 + length / 2, h - 30 * s), radius=4 * s, fill=(255, 255, 255, 235))


def lockscreen(background, cover=False):
    w, h = background.size
    overlay = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(overlay)
    status(d, w, h)
    s = h / 1466
    # Light type remains readable without a heavy shadow over the quiet sky.
    date_top = 176 * s if cover else 149 * s
    centered_text(d, (w / 2, date_top), "Saturday, 17 October", font(32 * s, "normal"), (255, 255, 255, 238))
    clock_top = 237 * s if cover else 208 * s
    clock_size = 236 * s if cover else 270 * s
    centered_text(d, (w / 2, clock_top), "09:41", font(clock_size), (252, 254, 255, 248))
    shortcuts(overlay, w, h)
    return Image.alpha_composite(background.convert("RGBA"), overlay).convert("RGB")


def main():
    # Windows pipe consoles may otherwise reject Thai project-folder names.
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    inner = lockscreen(landscape(2048, 1466))
    cover = lockscreen(blue_waves(1024, 1466), cover=True)
    inner.save(OUT / "Inner_Display.png", icc_profile=PROFILE, optimize=True)
    cover.save(OUT / "Cover_Display.png", icc_profile=PROFILE, optimize=True)
    sheet = Image.new("RGB", (1600, 850), (26, 29, 34))
    a = inner.resize((1048, 750), Image.Resampling.LANCZOS)
    b = cover.resize((524, 750), Image.Resampling.LANCZOS)
    sheet.paste(a, (8, 50))
    sheet.paste(b, (1068, 50))
    sheet.save(PREVIEW / "Screen_Texture_Preview.png", icc_profile=PROFILE)
    print("Created original RGB/sRGB screen textures:")
    for path in (OUT / "Inner_Display.png", OUT / "Cover_Display.png", PREVIEW / "Screen_Texture_Preview.png"):
        with Image.open(path) as im:
            print(f"{path}: {im.size}, {im.mode}, ICC={bool(im.info.get('icc_profile'))}")


if __name__ == "__main__":
    main()
