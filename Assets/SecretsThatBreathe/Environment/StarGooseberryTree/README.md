# ต้นมะยม — Star Gooseberry / Phyllanthus acidus

สร้างสำหรับ Unity **6000.3.8f1**, **URP 17.3.0** ตามภาพอ้างอิงของผู้ใช้

## ใช้งาน

1. ลาก `Prefabs/StarGooseberryTree.prefab` ลง Scene แล้ววางโคนต้นให้ตรงกับพื้น (1 Unity unit = 1 เมตร)
2. เพิ่ม Wind Zone หรือใช้ `Prefabs/Mayom_WindZone.prefab` ค่าเริ่มต้น Main = 1, Turbulence = 0.52, Pulse Magnitude = 0.28, Pulse Frequency = 0.22
3. คอมโพเนนต์ `MayomTreeWind` บน Prefab จะรับ Wind Zone ใน Scene เดียวกันอัตโนมัติ รองรับ Directional และ Spherical
4. ปรับ `Wind Multiplier`, `Leaf Flutter` และ `Preview In Edit Mode` ได้ หรือระบุ Wind Zone เฉพาะต้นในช่อง `Wind Zone`
5. เปิด `Demo/StarGooseberryTree_Preview.unity` เพื่อดูต้นไม้พร้อมลม

มี `StarGooseberryTree_NoFruit.prefab` เป็นตัวเลือกซ่อนผล โดยใช้ Mesh/วัสดุร่วมกัน

## รูปร่างและงบโมเดล

- ความสูงประมาณ **5.1 เมตร** พุ่มกว้างประมาณ **6.1 เมตร**; ภาพไม่มีมาตราส่วนแน่นอน จึงประมาณขนาดจากคน/รั้วในภาพ
- ลำต้นสูง แตกกิ่งยกขึ้นหลายระดับตามภาพ 1–2; กิ่งย่อยมีใบเรียงสองข้างและพุ่มแน่นตามภาพ 3–4
- ผลมะยมเป็นทรงแป้น มี 6 พู สีเหลืองอมเขียว รวม **48 ลูก / 5 พวง** พวงละ **8, 9, 10, 11, 10 ลูก** ตามลักษณะพวงในภาพ 5–6
- **LOD0 = 6,974 triangles** (ลำต้น/กิ่ง 1,082 + แผ่นใบ 1,284 + ผล 4,608)
- LOD1 = 3,562 triangles; LOD2 = 1,312 triangles
- จำนวน Polygon นับเป็น **triangles หลังนำเข้า Unity** ซึ่งรวมใบ กิ่ง และผลทั้งต้น ไม่มีการนับ quad เพื่อลดตัวเลข
- ใบใช้แผ่นโปร่งใสโค้ง 214 กิ่งย่อย แต่ละภาพกิ่งมีใบประมาณ 25 ใบ รวมประมาณ 5,350 ใบที่วาดใน Texture (ไม่ใช่ Mesh ใบแยก 5,350 ชิ้น)
- Pivot อยู่โคนต้น, UV ครบ, Normal/Tangent พร้อม, Capsule Collider ครอบลำต้นส่วนล่าง
- ตัวเลือก NoFruit มีจำนวนสามเหลี่ยมในไฟล์เท่ากัน แต่ Renderer ผลถูกปิด

## วัสดุและ Texture

- `Mayom_Bark_BaseColor.png` และ `Mayom_Bark_Normal.png` เป็นสำเนาภาพ 9 และ 10 โดยตรง; Normal Map นำเข้าเป็น NormalMap / linear
- `Mayom_Leaves_Atlas.png` เป็น RGBA โปร่งใสที่สร้างด้วย **imagegen built-in** อิงรูปทรง สี และเส้นใบในภาพ 7–8 เป็น atlas กิ่งใบ 2 แบบ; มี mipmap, alpha clip และแสดงสองด้าน
- ผลใช้ geometry และวัสดุ URP สีเหลืองอมเขียว ไม่ใช้ทรงผลจาก Texture
- Shader `Secrets That Breathe/Mayom Tree Wind Lit` ใช้พื้นฐาน URP/Lit; deformation สอดคล้องกันใน forward, deferred, shadow, depth, depth normals และ motion vectors
- โคนต้นและ Collider อยู่นิ่ง ลำต้นส่วนบน/กิ่งไหวตามแรงลม และกิ่งใบมี flutter เพิ่มตาม turbulence
- สำหรับต้นไม้ที่เคลื่อนไหว ให้คง Prefab เป็น non-static; ใช้ Prefab นี้เป็น GameObject พร้อม MayomTreeWind
- หากใช้ Terrain tree painting ซึ่งไม่ได้รัน MonoBehaviour ของต้นแต่ละต้น ต้องมีระบบส่งค่าลมให้ shader แยกต่างหาก

## ต้นฉบับและตรวจสอบ

- Blender พร้อม Texture packed: `ArtSource/StarGooseberryTree/StarGooseberryTree.blend`
- FBX ทั้ง 3 LOD อยู่ใน `Models`
- ภาพตัวอย่าง Unity และรายงานทดสอบ: `ArtSource/StarGooseberryTree/StarGooseberryTree_Unity_URP.png`, `Unity_Verification.txt`, `geometry_report.json`
- แพ็กเกจ: `ArtSource/StarGooseberryTree/StarGooseberryTree_URP.unitypackage`
- สร้าง Mesh ซ้ำ: Blender รัน `Tools/StarGooseberryTree/generate_mayom.py`
- สร้างวัสดุ/Prefab ซ้ำ: `Tools > Mayom Tree > Rebuild URP Assets` (จะเขียนทับวัสดุ/Prefab ของชุดนี้)
- การตรวจสอบใช้ Unity รุ่นเดียวกันในโปรเจคแยก พร้อม URP 17.3 และการตั้งค่า Render Pipeline ของโปรเจคนี้ เพื่อรักษา Scene ที่เปิดอยู่

## Prompt ของ Texture ใบที่ใช้จริง

Built-in imagegen, transparent_background = true:

> Use case: photorealistic-natural. Asset: transparent RGBA albedo foliage shoot atlas for a realistic 3D Phyllanthus acidus / Thai star gooseberry tree. Create a square image with precisely TWO isolated leafy shoots in two vertical equal-width columns, transparent background. Each shoot slender center stem from bottom to top, about 14 alternating pairs of small smooth ovate pointed leaves on left and right of stem. The leaves match user's image 7: entire edges, short petioles, soft fine pale veins, natural medium green. Shoot left has mature green leaves; shoot right has fresh slightly lighter green leaves with a few tiny bronze new leaves at top. Botanical ramulus structure like user image 8. Each entire shoot fits fully inside its own half image with 4% transparent padding. Shoot aligned vertically, stem base bottom, growing tip up, with subtle natural curve. Leaves fan laterally slightly upward, no overlap between columns, no extra branches outside the two shoots. Perfectly flat front view with neutral diffuse lighting, detailed natural surface, no baked cast shadows, no labels, no logos or watermark. Genuine transparent alpha background.

