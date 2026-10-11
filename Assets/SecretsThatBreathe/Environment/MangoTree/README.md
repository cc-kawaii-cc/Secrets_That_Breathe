# Mango Tree — URP

ต้นมะม่วงทรงสวน อ้างอิงลักษณะจากภาพที่ผู้ใช้ให้: ลำต้นแตกกิ่งต่ำ พุ่มกว้าง ใบเรียวยาวปลายโค้ง และผลสีเขียว
สร้าง mesh ใหม่; Texture เปลือกไม้, Normal Map และภาพใบไม้ใช้ไฟล์ที่ผู้ใช้ส่งให้โดยไม่เปลี่ยนพิกเซล

## ใช้งาน

1. ใน Unity เปิดโฟลเดอร์ `Assets/SecretsThatBreathe/Environment/MangoTree/Prefabs`
2. ลาก `MangoTree.prefab` ลง Scene สำหรับต้นที่มีผล หรือ `MangoTree_NoFruit.prefab` สำหรับต้นที่ไม่มีผล
3. วาง Position Y = ระดับพื้น แล้วปรับ Scale ตามพื้นที่ (Scale 1 เท่ากับขนาดจริงประมาณ 4.2 เมตร)
4. เปิด `Demo/MangoTree_Preview.unity` เพื่อดูตัวอย่างในฉากแยก โดยใช้ URP ที่ตั้งไว้ในโปรเจกต์

วัสดุใช้ `Secrets That Breathe/Mango Wind Lit` ซึ่งเพิ่มการไหวตามลมบน URP/Lit: เปลือกไม้ ใบ และผล รวม 3 วัสดุ ใบใช้ geometry เป็นรูปใบจริงและแสดงสองด้าน ไม่ใช้แผ่นสี่เหลี่ยมโปร่งใส
มี Capsule Collider บริเวณลำต้น เหมาะกับการเดินชนต้นไม้ ไม่ครอบคลุมกิ่งและพุ่ม
Pivot อยู่กลางโคนต้น แกน Y ชี้ขึ้น หน่วยเมตร รากบางส่วนจมใต้ระดับพื้นเล็กน้อยตามตั้งใจ

## รายละเอียดและประสิทธิภาพ

| ระดับ | สามเหลี่ยมโดยประมาณ | เปลี่ยนระดับเมื่อขนาดบนหน้าจอต่ำกว่า |
| --- | ---: | ---: |
| LOD0 | 161,576 | 42% |
| LOD1 | 53,808 | 19% |
| LOD2 | 12,668 | 3.5% (ซ่อน) |

รายละเอียดสูงสุดมีใบ 8,680 ใบ และผล 20 ผล ขนาดพุ่มประมาณ 5.4 × 5.5 เมตร สูงประมาณ 4.2 เมตร
รุ่นปรับปรุงเพิ่มใบ 30% เพิ่มความกว้างใบ 35% โดยคงความยาวใบเดิม และลดรัศมีลำต้น/กิ่งหลัก 15%
จำนวนสามเหลี่ยมในตารางวัดหลังนำเข้า Unity 6000.3.8f1 / URP 17.3.0 แล้ว
เป็นต้นไม้ฉากระยะใกล้/กลางสำหรับเกม 3D ควรทดสอบ frame rate หากวางจำนวนมาก
LOD ใช้การสลับระดับโดยตรง ไม่มี billboard; ทั้ง 3 ระดับรองรับการไหวตาม Wind Zone
รุ่นไม่มีผลใช้ mesh ร่วมกันและปิด renderer ของผล เพื่อลดการทำสำเนา asset

## ไฟล์และการแก้ไข

- `Models/`: FBX แยก LOD 0–2
- `Textures/`: สีเปลือกไม้, Normal Map และ atlas ใบไม้ 8 ใบจากผู้ใช้ ขนาด 1254²; สีผล 256²
- UV ของ mesh จับคู่กับใบไม้ใน atlas โดยไม่ต้องแก้ภาพต้นฉบับ
- `Materials/`: วัสดุ URP/Lit พร้อม texture
- `Prefabs/`: พร้อม LODGroup, Collider และ MangoTreeWind
- `Scripts/`: MangoTreeWind อ่านค่า Wind Zone แล้วส่งให้ shader; ไม่แก้ mesh ทุกเฟรม
- `Shaders/`: URP Lit พร้อม wind deformation ในแสง เงา depth normals และ motion vectors
- `Editor/`: เมนู `Tools > Mango Tree > Rebuild URP Prefabs` สำหรับสร้างวัสดุและ Prefab ใหม่จาก FBX (จะทับการปรับค่าในไฟล์ที่สร้าง)
- ต้นฉบับ Blender อยู่ที่ `ArtSource/MangoTree/MangoTree.blend` ที่ root ของโปรเจกต์ พร้อม texture ที่ pack ไว้ในไฟล์
- ภาพตัวอย่างและผลตรวจ Unity อยู่ใน `ArtSource/MangoTree/`
- ตัวสร้าง geometry อยู่ที่ `Tools/MangoTree/generate_mango.py` (Blender 5.2) สำหรับแก้ทรงต้นและสร้างซ้ำด้วย seed เดิม

ฉากเดิมและการตั้งค่า render pipeline ของโปรเจกต์ไม่ได้ถูกแก้โดยชุด asset นี้

## Wind Zone

1. ใช้ Prefab เดิม `MangoTree` หรือ `MangoTree_NoFruit` รุ่นที่อัปเดตแล้ว
2. เพิ่ม `GameObject > 3D Object > Wind Zone` ในฉาก หรือใช้ Wind Zone ที่มีอยู่
3. เริ่มด้วย Mode = Directional, Main = 1, Turbulence = 0.5, Pulse Magnitude = 0.25, Pulse Frequency = 0.2
4. หมุน Wind Zone เพื่อเปลี่ยนทิศทางลม กด Play เพื่อดูการไหว; ใน Scene view มี preview ขณะไม่ได้เล่นด้วย

ต้นไม้ค้นหา Wind Zone ใน scene เดียวกันอัตโนมัติ (โซนที่สร้างใหม่อาจใช้เวลาสูงสุด 1 วินาทีในการค้นพบ)
รองรับ Directional และ Spherical โดย Spherical ลดแรงตามระยะของโคนต้นถึงจุดศูนย์กลางและหยุดนอก Radius
ลมหลายโซนใน scene เดียวกันถูกรวมเข้าด้วยกัน หากต้องการใช้เพียงโซนเดียว หรือโซนใน additive scene ให้ลากโซนใส่ช่อง **Wind Zone** ของคอมโพเนนต์ **Mango Tree Wind** ที่โคนต้น

- **Wind Multiplier**: ปรับแรงเฉพาะต้นนี้; 0 = หยุด
- **Leaf Flutter**: ปรับความกระพือที่ปลายใบ
- **Preview In Edit Mode**: เปิด/ปิดการไหวใน Scene view ขณะไม่ได้กด Play
- หากไม่มี Wind Zone ที่ส่งแรง หรือ Main = 0 จะหยุดไหว
- โคนต้นและ Collider อยู่นิ่ง ส่วนด้านบนโค้งตามลม ใบเพิ่มการกระพือที่ปลาย
- วัสดุเดิมยังเปลี่ยน Base Map, Normal Map, สี และ Tiling ได้; อย่าเปลี่ยน shader กลับเป็น URP/Lit ปกติหากต้องการให้ลมทำงาน
- ฉาก `Demo/MangoTree_Preview.unity` มี Wind Zone ตัวอย่างให้แล้ว

ใช้กับต้นไม้ที่วางเป็น GameObject/Prefab ในฉาก การลงต้นไม้ผ่าน Terrain Paint Trees จะไม่รันคอมโพเนนต์นี้
การไหวทำบน GPU และใช้ค่าต่อ renderer จึงไม่ใช้ SRP Batcher สำหรับต้นไม้ชุดนี้; ควรทดสอบ frame rate เมื่อวางจำนวนมาก
ลมนี้เป็นการเลียนแบบการไหวด้วยค่าจาก Wind Zone ไม่ใช่ระบบฟิสิกส์กิ่งไม้หรือ SpeedTree

เมนู `Tools > Mango Tree > Enable Wind on Existing Prefabs` ใช้อัปเกรด Prefab โดยรักษา texture สี และ tiling เดิม
ส่วน `Rebuild URP Prefabs` จะสร้างใหม่และรีเซ็ตวัสดุตามค่าเริ่มต้น
