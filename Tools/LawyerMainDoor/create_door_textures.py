"""Create restrained, original diffuse textures for the Lawyer Room main door.

These are authored image assets, not photographs or generated modelling evidence.
Wood front/back UV: u = world_x / 2 + 0.5; v = world_y / 2.5 + 0.5.
The procedural recipe is deterministic and keeps grain broad enough to read in
the game's dim light without adding noisy photographic detail to a lowpoly prop.
"""

from pathlib import Path
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
TEXTURES = ROOT / "Assets/SecretsThatBreathe/Props/LawyerMainDoor/Textures"
ART = ROOT / "ArtSource/LawyerMainDoor"
SEED = 170126
FONT = ROOT / "Assets/Nemo_Upgrade/Font/Kanit/Kanit-Regular.ttf"


def noise(rng, width, height, outsize):
    values = rng.normal(0, 1, (height, width)).astype(np.float32)
    result = np.asarray(Image.fromarray(values).resize(outsize, Image.Resampling.BICUBIC))
    return result / max(float(result.std()), 0.001)


def ellipse(u, v, cx, cy, sx, sy):
    return np.exp(-(((u - cx) / sx) ** 2 + ((v - cy) / sy) ** 2) * 1.7)


def make_wood():
    rng = np.random.default_rng(SEED)
    size = 1024
    u, v = np.meshgrid(np.linspace(0, 1, size), np.linspace(1, 0, size))

    # Long, gently wandering fibres. There are no dark drawn plank outlines:
    # actual joins and recessed panels come from the door's geometry.
    wander = 0.0035 * np.sin(v * 8.0 + u * 5.0) + 0.0016 * np.sin(v * 19.0)
    x = u + wander
    bands = 3.2 * noise(rng, 13, 3, (size, size))
    long_grain = 2.0 * noise(rng, 156, 6, (size, size))
    close_grain = 0.9 * noise(rng, 440, 27, (size, size))
    fibres = (0.65 * np.sin(x * 312.0 + np.sin(v * 10.0))
              + 0.38 * np.sin(x * 701.0 + v * 4.5)
              + 0.2 * np.sin(x * 1573.0 + v * 13.0))
    large_age = 1.7 * noise(rng, 7, 9, (size, size))
    subtle_pores = rng.normal(0, 0.24, (size, size))

    # Use-related variation is soft and local, never a graphic painted mask.
    wear = np.zeros((size, size))
    for cx in (0.45, 0.55):
        wear += 2.6 * ellipse(u, v, cx, 0.47, 0.045, 0.085)
    for cx in (0.015, 0.985):
        for cy in (0.015, 0.985):
            wear += 2.6 * ellipse(u, v, cx, cy, 0.11, 0.075)
    bottom_age = -5.0 * np.clip((0.12 - v) / 0.12, 0, 1) ** 1.5
    low_scuff = 1.4 * ellipse(u, v, 0.67, 0.075, 0.25, 0.055)
    value = bands + long_grain + close_grain + fibres + large_age + wear + bottom_age + low_scuff + subtle_pores
    value -= value.mean()

    rgb = np.zeros((size, size, 3), dtype=float)
    target = (95.0, 76.0, 57.0)
    for channel, scale in enumerate((1.0, 0.84, 0.65)):
        rgb[..., channel] = target[channel] + value * scale
    # A slight desaturated finish keeps the wood compatible with cool fog.
    rgb = np.clip(np.rint(rgb), 0, 255).astype(np.uint8)
    img = Image.fromarray(rgb, "RGB")
    img.save(TEXTURES / "Door_Wood_BaseColor.png", optimize=True)
    return img


def centered_text(draw, text, font, center, fill, tracking=0):
    if not tracking:
        box = draw.textbbox((0, 0), text, font=font)
        pos = (center[0] - (box[2] - box[0]) / 2 - box[0],
               center[1] - (box[3] - box[1]) / 2 - box[1])
        draw.text(pos, text, font=font, fill=fill)
        return
    advances = [draw.textlength(c, font=font) for c in text]
    x = center[0] - (sum(advances) + tracking * (len(text) - 1)) / 2
    box = draw.textbbox((0, 0), text, font=font)
    y = center[1] - (box[3] - box[1]) / 2 - box[1]
    for char, advance in zip(text, advances):
        draw.text((x, y), char, font=font, fill=fill)
        x += advance + tracking


def make_plaque():
    rng = np.random.default_rng(SEED + 1)
    width, height = 1024, 256
    u, v = np.meshgrid(np.linspace(0, 1, width), np.linspace(0, 1, height))
    variation = 1.2 * noise(rng, 20, 7, (width, height))
    variation += rng.normal(0, 0.35, (height, width))
    variation += 2.0 * np.sin(u * 3.5) * np.cos(v * 2.6)
    rgb = np.zeros((height, width, 3), dtype=float)
    for channel, base in enumerate((55, 49, 38)):
        rgb[..., channel] = base + variation
    plaque = Image.fromarray(np.clip(np.rint(rgb), 0, 255).astype(np.uint8), "RGB")
    draw = ImageDraw.Draw(plaque)
    draw.rounded_rectangle((13, 13, 1010, 242), radius=16, outline=(137, 118, 80), width=2)
    draw.rounded_rectangle((20, 20, 1003, 235), radius=12, outline=(79, 70, 50), width=1)
    # The original game font is used, without introducing a lawyer's name.
    thai = ImageFont.truetype(str(FONT), 84)
    english = ImageFont.truetype(str(FONT), 34)
    centered_text(draw, "สำนักงานกฎหมาย", thai, (512, 105), (204, 190, 154))
    centered_text(draw, "LEGAL OFFICE", english, (512, 190), (169, 155, 122), tracking=6)
    plaque.save(TEXTURES / "Door_Plaque.png", optimize=True)
    return plaque


def make_preview(wood, plaque):
    # Neutral inspection sheet; the source PNGs above remain unlabelled textures.
    canvas = Image.new("RGB", (1464, 1120), (33, 35, 37))
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.truetype(str(FONT), 28)
    draw.text((32, 18), "LAWYER ROOM / MAIN DOOR / DIFFUSE TEXTURES", font=font, fill=(210, 206, 196))
    canvas.paste(wood, (32, 70))
    canvas.paste(plaque.resize((352, 88), Image.Resampling.LANCZOS), (1080, 100))
    draw.text((1080, 220), "Aged walnut", font=font, fill=(210, 206, 196))
    draw.text((1080, 258), "1024 x 1024", font=font, fill=(150, 150, 147))
    draw.text((1080, 320), "Tarnished bronze", font=font, fill=(210, 206, 196))
    draw.text((1080, 358), "1024 x 256", font=font, fill=(150, 150, 147))
    draw.text((1080, 440), "sRGB mean wood", font=font, fill=(150, 150, 147))
    draw.text((1080, 478), "95 / 76 / 57", font=font, fill=(210, 206, 196))
    canvas.save(ART / "Texture_Preview.png", optimize=True)


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    TEXTURES.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    wood = make_wood()
    plaque = make_plaque()
    make_preview(wood, plaque)
    means = np.asarray(wood).mean(axis=(0, 1)).round(2).tolist()
    print(f"Wood: {TEXTURES / 'Door_Wood_BaseColor.png'} / RGB mean {means}")
    print(f"Plaque: {TEXTURES / 'Door_Plaque.png'}")


if __name__ == "__main__":
    main()
