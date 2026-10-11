# MangoTree_Update — Unity 6000.3.8f1 / URP 17.3.0

ต้นมะม่วงชุดใหม่รุ่น Low Poly ไม่เกิน 5,000 triangles ทั้งต้น แยกจาก MangoTree และ MangoTree_New พร้อมลำต้นเอนเล็กน้อย กิ่งแผ่ พุ่มกลม ใบห้อย และมะม่วงสีเขียวเป็นพวง

## เริ่มใช้งาน

1. ลาก `Prefabs/MangoTree_Update.prefab` ลง Scene ของโปรเจกต์ URP
2. วาง pivot ที่โคนต้นให้ตรงระดับพื้น (1 Unity unit = 1 เมตร)
3. เพิ่ม `GameObject > 3D Object > Wind Zone` ลง Scene
4. ใช้ค่าเริ่มต้น: Directional, Main = 1, Turbulence = 0.5, Pulse Magnitude = 0.25, Pulse Frequency = 0.2
5. กด Play หรือเปิดการแสดงผลเคลื่อนไหวใน Scene View เพื่อดูใบและกิ่งขยับ

เปิด `Demo/MangoTree_Update_Preview.unity` เพื่อดูฉากพร้อมต้นไม้ แสง กล้อง และ Wind Zone โดยใช้ URP Pipeline ของโปรเจกต์เดิม

มี `Prefabs/MangoTree_Update_NoFruit.prefab` สำหรับต้นไม่มีผล ใช้ mesh/material ร่วมกับต้นหลัก

## ขนาดและรายละเอียด

- ขนาด bounds สูงประมาณ 7.4 เมตร พุ่มกว้างประมาณ 7.3 × 7.3 เมตร เมื่อ Scale = 1
- ใบใช้แผ่นกลุ่มใบ 664 แผ่น (แต่ละแผ่นมีใบหลายใบใน Texture) ผลสีเขียว 75 ผล ไม่มีผลสีสุก
- LOD0: 4,950 triangles รวมกิ่ง/ลำต้น/ก้านผล 2,122 + ใบ 1,328 + ผล 1,500
- LOD1: 2,382 triangles
- LOD2: 950 triangles
- เปลี่ยน LOD เมื่อขนาดบนจอลดผ่าน 50%, 20%, 3.5% แล้ว cull พร้อม crossfade
- Capsule Collider เฉพาะลำต้น โคนต้นเป็น pivot และต้นวางตั้งตามแกน Y

ลดจาก LOD0 รุ่นเดิม 168,224 เหลือ 4,950 triangles (ประมาณ 97.1%) โดยรักษาตำแหน่งกิ่งหลักและทรงพุ่มไว้ ลดกิ่งเล็ก ใช้ใบแบบกลุ่ม ลดจำนวนผลครึ่งหนึ่ง และใช้ geometry ผลที่เรียบขึ้น หากวางสวนจำนวนมาก ให้ทดสอบบนเครื่องเป้าหมาย เพราะการวาด alpha และเงายังใช้เวลาแม้จำนวน triangles ต่ำลง

## Texture และวัสดุ

- `Textures/MangoUpdate_Leaves_Atlas.png`: เก็บภาพที่ 4 ต้นฉบับ เป็นใบมะม่วง 8 แบบพร้อม alpha
- `Textures/MangoUpdate_LeafClusters.png`: atlas 2 × 2 ของกลุ่มใบ เรนเดอร์จากใบต้นฉบับโดยใช้สีแบบ unlit เพื่อไม่ฝังแสงซ้ำ วัสดุแสดงสองด้านและ alpha clip
- `Textures/MangoUpdate_Bark_BaseColor.png`: ภาพที่ 5 โดยตรง
- `Textures/MangoUpdate_Bark_Normal.png`: ภาพที่ 6 โดยตรง ตั้งประเภทนำเข้าเป็น Normal Map
- `Materials/MangoUpdate_Fruit_Green.mat`: สีเขียวของผลมะม่วง ปรับ `_BaseColor` ได้ใน Material Inspector
- Texture ต้นฉบับทั้งสามไฟล์ยังตรงกับไฟล์ที่ให้มา กลุ่มใบเป็นไฟล์ที่สร้างเพิ่มเติมจากภาพใบต้นฉบับ
- Shader `Secrets That Breathe/Mango Tree Update Wind Lit` ดัดแปลงจาก URP 17.3.0 Lit พร้อม deformation ตรงกันใน Forward, GBuffer, Shadow, Depth, DepthNormals และ MotionVectors ดูใบอนุญาตของ Unity ใน `Shaders/Unity-LICENSE.md`

## Wind Zone

Prefab มี `MangoTreeUpdateWind` อยู่ที่โคนต้น ระบบอ่าน Directional และ Spherical Wind Zone ใน Scene เดียวกันอัตโนมัติ หรือระบุโซนเฉพาะในช่อง `Wind Zone`

- `Wind Multiplier`: ปรับแรงลมของต้นนี้
- `Leaf Flutter`: ปรับการกระพือของใบ
- `Preview In Edit Mode`: ดูการเคลื่อนไหวใน Scene View
- ไม่พบโซน / ปิด GameObject ของโซน / Main = 0: หยุดการเคลื่อนไหว
- โคนต้นยึดอยู่กับที่ กิ่งและผลเคลื่อนตามความสูง ส่วนกลุ่มใบมี flutter เพิ่ม โครง Collider ไม่ขยับ

โมเดล FBX ทั่วไปต้องมีตัวเชื่อม Wind Zone กับ shader เนื่องจากการรองรับลมอัตโนมัติของ Unity ใช้กับ Tree Creator และ SpeedTree เป็นหลัก: [Unity WindZone API](https://docs.unity.cn/6000.3/Documentation/ScriptReference/WindZone.html)

Prefab ปกติใช้เป็น GameObject ใน Scene ส่วนการระบายต้นไม้บน Terrain ให้ใช้ชุด Terrain ด้านล่าง

## Terrain Paint Trees

1. เลือก Terrain ที่ต้องการใน Hierarchy
2. เลือก `Tools > Mango Tree Update > Add Mango to Selected Terrain` หนึ่งครั้ง เมนูนี้เพิ่ม prototype ของ `MangoTree_Update_Terrain` และเพิ่ม `MangoTreeTerrainWind` หนึ่งตัวให้ Scene โดยไม่ลบต้นหรือ prototype ที่มีอยู่
3. เปิด `Paint Trees` ใน Inspector เลือก `MangoTree_Update_Terrain` แล้วระบายตามปกติ
4. ให้ Scene มี Wind Zone เพื่อขยับต้นไม้ ปรับแรงผ่าน Wind Zone หรือ `Wind Multiplier` / `Leaf Flutter` ของ `MangoTreeTerrainWind`

อีกวิธีคือ `Paint Trees > Edit Trees > Add Tree` แล้วเลือก `Prefabs/MangoTree_Update_Terrain.prefab` จากนั้นเพิ่มคอมโพเนนต์ `MangoTreeTerrainWind` ที่ Terrain หรือ GameObject หนึ่งตัวใน Scene

- `Prefabs/MangoTree_Update_Terrain.prefab`: เมชรวมสามวัสดุ LOD0 **4,950**, LOD1 **2,382**, LOD2 **950** triangles
- `Prefabs/MangoTree_Update_Terrain_NoFruit.prefab`: เมชรวมสองวัสดุ LOD0 **3,450**, LOD1 **1,774**, LOD2 **770** triangles
- ใช้ `Terrain.treeInstances` ของ Unity โดยตรง ไม่ต้องสร้าง GameObject หรือคอมโพเนนต์ลมแยกทุกต้น
- รองรับการสุ่มขนาด/ความสูง/การหมุนของ Paint Trees มี Capsule Collider ที่โคน prototype
- ใช้ `MangoTreeTerrainWind` เพียงหนึ่งตัวต่อ Scene อ่าน Wind Zone ใน Scene เดียวกันได้สูงสุด 8 โซน หรือระบุโซนเฉพาะในช่อง `Wind Zone` วัสดุ Terrain แยกจากวัสดุของต้น GameObject
- shader คำนวณ pivot/scale จาก matrix ของต้นแต่ละ instance ใช้ลมร่วมกับทุกต้นบน Terrain และรองรับ Directional/Spherical รวม pulse, turbulence และขอบเขตโซน
- เปิด `Terrain Collider > Enable Tree Colliders` หากต้องการให้ตัวละครชนลำต้น [Unity Terrain Collider reference](https://docs.unity.com/en-us/engine/6000.3/manual/physics-section/physics-overview/collision-section/collider-shapes/terrain-colliders/class-terrain-collider)
- ใช้ Mesh LOD 3 ระดับ รุ่นนี้ไม่ได้เพิ่ม SpeedTree Billboard; ปรับระยะตัดต้นไม้ผ่าน `Terrain > Settings > Tree Distance` และ LOD
- สีใบ/ผล/ลำต้นปรับที่ `MangoTerrain_*` materials; Color Variation ของ Paint Trees ไม่ได้ถูกนำมาใช้กับ shader นี้

เปิด `Demo/MangoTree_Update_Terrain_Preview.unity` เพื่อดูต้นที่ระบายบน Terrain จริง พร้อมต้น 9 ต้นที่มีขนาดและการหมุนต่างกัน พื้นเนินอ่อน แสง กล้อง และ Wind Zone

การระบายต้นไม้ของ Unity: [Paint Trees](https://docs.unity3d.com/6000.3/Documentation/Manual/terrain-Trees.html)

## ไฟล์ต้นฉบับและผลตรวจ

- Blender ต้นฉบับพร้อม Texture ฝังในไฟล์: `ArtSource/MangoTree_Update/MangoTree_Update.blend`
- ภาพโมเดลและภาพ URP จริง: `ArtSource/MangoTree_Update/`
- ชุดส่งออก: `ArtSource/MangoTree_Update/MangoTree_Update_URP.unitypackage`
- สร้างโมเดลซ้ำ: `Tools/MangoTree_Update/generate_mango_update.py` (Blender 5.2)
- สร้างวัสดุ/Prefab/ฉากใหม่: `Tools > Mango Tree Update > Rebuild URP Assets` หลังบันทึก Scene ปัจจุบัน
- ผลทดสอบ: `ArtSource/MangoTree_Update/Unity_Verification.txt`
- ผลทดสอบ Terrain และลมจากภาพ render ของ native instances: `ArtSource/MangoTree_Update/Terrain_Verification.txt`
- รุ่นรายละเอียดสูงก่อนลด Polygon เก็บที่ `ArtSource/MangoTree_Update/Archive_168k/`

ตรวจในโปรเจกต์ทดสอบแยกด้วย Unity 6000.3.8f1 / URP 17.3.0: งบ LOD0 ไม่เกิน 5,000 triangles ทั้งต้น, references ของ Prefab, ขนาด/แกน, 3 LOD, normal map, alpha สองด้าน, ผลสีเขียว, collider, ค่าแรง/ทิศทาง/จังหวะลม, falloff แบบ Spherical, การขยับจริงจากภาพ render ทั้ง 3 LOD และการหยุดเมื่อปิดลม ไม่มี dependency ไปยังโมเดลสองโฟลเดอร์เดิม ดูสถานะ PASS ในผลทดสอบหลังการสร้าง

สีและความสว่างใน Scene หลักขึ้นอยู่กับแสง เงา และการปรับภาพของ Scene นั้น
