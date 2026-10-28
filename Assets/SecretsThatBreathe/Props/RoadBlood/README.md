# รอยเลือดแบบ Texture Atlas — Unity URP

Prefab เดิมทั้ง 3 แบบเปลี่ยนเป็น Quad แบบละ 4 vertices / 2 triangles แล้ว:
- Prefabs/RoadBlood_Pool.prefab — คราบหลัก
- Prefabs/RoadBlood_Smear.prefab — รอยปาด
- Prefabs/RoadBlood_Drops.prefab — หยดเลือด

ใช้ Textures/RoadBlood_Atlas.png ขนาด 1024 × 1024 RGBA ร่วมกันหนึ่งแผ่น
Bake จาก Mesh และสี Material เดิม ไม่ได้สร้างลายใหม่ด้วย AI
ใช้ URP/Lit รับแสงและเงา แทน Shader Unlit เดิม ไม่มี Emission

ชื่อ, GUID, pivot และตำแหน่งของ Prefab เดิมยังอยู่ จึงอัปเดตชิ้นที่วางในฉากแล้วได้
Mesh GUID เดิมถูกเก็บไว้ แต่เนื้อหาลดเหลือ 2 triangles และเปลี่ยน UV ไปยังช่อง Atlas
ไม่มี Collider, Rigidbody หรือ Runtime Script

## ใช้งาน
ลาก Prefab เดิมลงถนนได้ตามปกติ ตั้ง Y ให้ตรงผิวถนน
Mesh ยกเหนือ pivot 0.003 เมตรเพื่อหลีกเลี่ยงภาพกะพริบ
หมุนแกน Y / ปรับ Scale ตามต้องการ พื้นลาดให้หมุนตามความลาด
เป็นแผ่นแบน ไม่ฉายตามพื้นขรุขระหรือหญ้าแบบ Decal Projector

Material ปัจจุบัน: Materials/RoadBlood_Atlas.mat
- Base Map / สี: ปรับสีรวม (ค่าเริ่มต้นขาว หมายถึงใช้สีที่ Bake มา)
- Alpha ของสี Base Map: ปรับความโปร่ง
- Base Map: ภาพรวมทั้ง 3 แบบ อย่าเปลี่ยน Tiling/Offset เพราะ UV ถูกจัดไว้แล้ว
ตั้ง Transparent / Alpha พร้อม Alpha Clipping 0.005 เพื่อคงขอบนุ่มและตัดส่วนใสทิ้ง
ปิด Specular Highlights และ Environment Reflections เพื่อไม่ให้สะท้อนเป็นแผ่นสี่เหลี่ยม
เปิด Receive Shadows และ Light Probes; Mesh มี UV2 สำหรับ Lightmap แล้ว
ไฟ Realtime รับแสงได้ทันที สำหรับไฟ Baked ให้ตั้ง Contribute GI และ Bake ใหม่หลังเปลี่ยน Material
Static อย่างเดียวไม่ทำให้ Shader Unlit รับแสงได้
ทั้งสามแบบใช้ Material ร่วมกัน หากต้องการสีต่างกันให้ Duplicate Material
Material และ Shader แบบ Mesh เดิมยังเก็บไว้เพื่ออ้างอิง แต่ไม่ใช่ Material ที่ Prefab ใช้แล้ว

## ทรัพยากร
รวมวางอย่างละชิ้น: 3,552 → 6 triangles
Windows ใช้ BC7 พร้อม mipmaps: Atlas ประมาณ 1.33 MiB GPU memory ร่วมกัน
ไฟล์ PNG ประมาณ 156 KiB ไม่ใช่ขนาดหน่วยความจำบน GPU
จำนวน draw calls ไม่ได้ลดลงโดยอัตโนมัติ และยังมีต้นทุนการซ้อนแผ่นโปร่งใส
การลดโพลิกอนไม่ได้แปลว่า FPS จะเพิ่มในสัดส่วนเดียวกัน

## ตรวจและย้อนกลับ
Tools/RoadBlood/atlas-validation.json เก็บผลตรวจจำนวนสามเหลี่ยม รูปภาพ ขอบโปร่งใส และ backup
Tools/ConvertRoadBloodToAtlas.py เป็นตัว Bake โดยอ่าน native Mesh ของเดิม
ต้นฉบับก่อนแปลงอยู่ใน Tools/RoadBlood/BeforeAtlas-* นอก Assets
Tools/BuildRoadBloodPrefabs.ps1 จะปฏิเสธการสร้าง Mesh แบบเก่าทับเมื่อพบ Atlas
ตรวจข้อมูล Mesh/UV/ภาพและภาพคอมโพสิตแล้ว ยังไม่ได้ตรวจเรนเดอร์หรือ Play Mode ใน Unity
