# MangoTree_New — Unity 6000.3 URP

โมเดลต้นมะม่วงชุดใหม่ แยกจาก `Environment/MangoTree` เดิมทั้งหมด ทรงต้นสูงใหญ่ ลำต้นเพรียว แตกกิ่งต่ำและแผ่กว้างตามภาพอ้างอิง มีใบมะม่วงทรงกว้างอ้วนและผลสีเขียว และรองรับ `Wind Zone` ผ่าน GPU vertex animation

## ใช้งาน

1. ลาก `Prefabs/MangoTree_New.prefab` ลง Scene
2. ให้ค่า `Position Y` ตรงกับระดับพื้น ตำแหน่ง pivot อยู่ที่โคนต้น
3. เพิ่ม `GameObject > 3D Object > Wind Zone` ใน Scene
4. ค่าเริ่มต้นที่แนะนำ: Mode = Directional, Main = 1, Turbulence = 0.5, Pulse Magnitude = 0.28, Pulse Frequency = 0.22
5. เปิด `Demo/MangoTree_New_Preview.unity` เพื่อดูฉากตัวอย่างพร้อม Wind Zone

มี `MangoTree_New_NoFruit.prefab` สำหรับต้นที่ไม่มีผล โดยใช้ mesh และ material ร่วมกันเพื่อลดขนาด asset

## ขนาดและประสิทธิภาพ

- ขนาดประมาณ 11.2 × 11.2 เมตร สูง 8.1 เมตร
- ใบ 9,735 ใบ ผลเขียว 112 ผล ไม่มีผลสีสุก
- LOD0: 170,804 triangles
- LOD1: 73,912 triangles
- LOD2: 14,222 triangles
- LOD transition: 48%, 19%, 3.5% แล้ว cull
- มี Capsule Collider เฉพาะลำต้น เหมาะกับการเดินชน ไม่ครอบคลุมพุ่มทั้งหมด

ต้นนี้เป็น hero/near-to-mid distance asset ถ้าต้องวางเป็นสวนจำนวนมาก แนะนำลดค่า LOD Bias หรือใช้ `MangoTree_New_NoFruit` กับต้นที่อยู่ไกล

## วัสดุและ Texture

- `MangoNew_Bark`: ใช้ภาพ Base Color และ Normal Map ที่ผู้ใช้ให้มาโดยตรง
- `MangoNew_Leaves`: ใช้ atlas โปร่งใส 8 แบบ, alpha clip, render สองด้าน และมี mipmap
- `MangoNew_Fruit_Green`: geometry ผลมะม่วงจริง สีเขียวทั้งหมด
- Shader `Secrets That Breathe/Mango Tree New Wind Lit` อ้างอิง URP/Lit และทำ deformation ให้ตรงกันใน forward, deferred, depth, shadow และ motion-vector pass

## Wind Zone

คอมโพเนนต์ `MangoTreeNewWind` ที่โคน prefab จะอ่าน Directional และ Spherical Wind Zone ใน Scene เดียวกันอัตโนมัติ แล้วส่งแรงลมให้ shader ต่อ renderer

- `Wind Multiplier`: เพิ่ม/ลดแรงลมเฉพาะต้นนี้
- `Leaf Flutter`: เพิ่ม/ลดการกระพือของปลายใบ
- `Preview In Edit Mode`: ดูลมใน Scene View โดยไม่ต้องกด Play
- ถ้าระบุช่อง `Wind Zone` เอง จะใช้เฉพาะโซนนั้น
- ถ้าปล่อยว่าง จะรวมแรงจาก Wind Zone ที่เปิดใช้งานทั้งหมดใน Scene เดียวกัน

โคนต้นและ Collider อยู่นิ่ง กิ่งด้านบนแกว่งตามความสูง และปลายใบมี flutter เพิ่มจาก turbulence การเคลื่อนไหวเกิดบน GPU

## ไฟล์ต้นฉบับและสร้างซ้ำ

- Blender source: `ArtSource/MangoTree_New/MangoTree_New.blend`
- Geometry generator: `Tools/MangoTree_New/generate_mango_new.py`
- Unity rebuild menu: `Tools > Mango Tree New > Rebuild URP Assets`
- ผลตรวจ Unity: `ArtSource/MangoTree_New/Unity_Verification.txt`

ทดสอบแล้วด้วย Unity `6000.3.8f1` และ URP `17.3.0` โดยไม่แก้ไข asset ใน `Environment/MangoTree` เดิม
