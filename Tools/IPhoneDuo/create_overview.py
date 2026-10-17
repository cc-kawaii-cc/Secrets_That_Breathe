"""Create a presentation sheet from the final, actual Blender model renders."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageCms

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "ArtSource/IPhoneDuo"
FONTS = Path("C:/Windows/Fonts")
SRGB = ImageCms.ImageCmsProfile(ImageCms.createProfile("sRGB")).tobytes()


def tracked(draw, xy, text, ft, fill, tracking=2):
    x, y = xy
    for char in text:
        draw.text((x, y), char, font=ft, fill=fill)
        x += draw.textlength(char, font=ft) + tracking


def main():
    first = Image.open(ART / "01_Open_Front.png").convert("RGB")
    bg = first.getpixel((0, 0))
    sheet = Image.new("RGB", (2200, 1600), bg)
    d = ImageDraw.Draw(sheet)
    title = ImageFont.truetype(str(FONTS / "segoeuisl.ttf"), 28)
    label = ImageFont.truetype(str(FONTS / "segoeui.ttf"), 20)
    tracked(d, (90, 24), "iPHONE DUO", title, (237, 241, 245), tracking=4)
    # Full frames are scaled without cropping or retouching the model evidence.
    views = [
        ("01_Open_Front.png", "OPEN · 164.6 × 117.8 mm", (65, 72)),
        ("03_HalfOpen.png", "FOLDING HINGE", (1165, 72)),
        ("04_Closed_Camera.png", "DUAL REAR CAMERA", (65, 834)),
        ("05_Closed_Cover.png", "COVER DISPLAY", (1165, 834)),
    ]
    for filename, caption, (x, y) in views:
        render = Image.open(ART / filename).convert("RGB")
        render = render.resize((970, 762), Image.Resampling.LANCZOS)
        sheet.paste(render, (x, y))
        d = ImageDraw.Draw(sheet)
        tracked(d, (x + 25, y + 715), caption, label, (225, 233, 240), tracking=2)
    d.line((1100, 90, 1100, 1535), fill=(88, 96, 106), width=1)
    d.line((90, 831, 2110, 831), fill=(88, 96, 106), width=1)
    sheet.save(ART / "IPhoneDuo_Overview.png", icc_profile=SRGB, optimize=True)


if __name__ == "__main__":
    main()
