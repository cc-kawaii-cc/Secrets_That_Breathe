"""Compose actual Blender/Unity QA captures without changing render content."""

from pathlib import Path
import sys

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "ArtSource/LawyerMainDoor"
FONT = ROOT / "Assets/Nemo_Upgrade/Font/Kanit/Kanit-Regular.ttf"


def fit_unmodified(canvas, source_path, bounds):
    image = Image.open(source_path).convert("RGB")
    x, y, width, height = bounds
    scale = min(width / image.width, height / image.height)
    output_size = (round(image.width * scale), round(image.height * scale))
    # The only image operation is full-frame proportional resizing.
    image = image.resize(output_size, Image.Resampling.LANCZOS)
    canvas.paste(image, (x + (width - image.width) // 2, y + (height - image.height) // 2))


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    canvas = Image.new("RGB", (2200, 1250), (24, 27, 30))
    draw = ImageDraw.Draw(canvas)
    heading = ImageFont.truetype(str(FONT), 44)
    label = ImageFont.truetype(str(FONT), 28)
    footer = ImageFont.truetype(str(FONT), 28)
    draw.text((40, 20), "LAWYER ROOM · MAIN DOOR", font=heading, fill=(226, 220, 206))
    draw.line((40, 88, 2160, 88), fill=(67, 69, 68), width=1)
    draw.text((40, 104), "MODEL · 3,952 TRIANGLES", font=label, fill=(184, 174, 149))
    draw.text((1116, 104), "IN SCENE · GAME FLASHLIGHT TEST", font=label, fill=(184, 174, 149))
    left = (40, 154, 1044, 1000)
    right = (1116, 154, 1044, 1000)
    for x, y, width, height in (left, right):
        draw.rectangle((x, y, x + width - 1, y + height - 1), fill=(18, 21, 24))
    fit_unmodified(canvas, ART / "Door_Studio_Front.png", left)
    fit_unmodified(canvas, ART / "After_Flashlight_InScene.png", right)
    draw.text((40, 1188), "ภาพขวาใช้ไฟฉายตรวจชั่วคราว ไม่มีการเปลี่ยนแสงห้อง", font=footer, fill=(170, 171, 166))
    path = ART / "MainDoor_Overview.png"
    canvas.save(path, optimize=True)
    print(path)


if __name__ == "__main__":
    main()
