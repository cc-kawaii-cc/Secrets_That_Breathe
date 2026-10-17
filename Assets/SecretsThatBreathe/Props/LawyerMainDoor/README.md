# Lawyer Room — Main Door

ประตูไม้บานคู่ Lowpoly สำหรับแทน Cube สีดำของ `Main Door` ในฉาก `Assets/Tonpalm/Lawyer Room.unity` สร้างให้เข้ากับสำนักงานโทนหม่น พื้นหินขัด และแสงเย็นของเกม

- **3,952 triangles** หลัง triangulation รวมด้านหน้า/หลัง วงกบ มือจับ บานพับ ป้าย และแผ่นกันเตะ
- 12 mesh renderers / 5 URP materials / 2 textures
- ขนาดช่องเดิม 2 × 2.5 ม.; รวมวงกบ 2.215 × 2.6 ม. จุดต่ำสุดคงระดับพื้นเดิม
- ไม้ walnut หม่น ลายแนวตั้ง มือจับทองเหลืองเก่า ป้าย `สำนักงานกฎหมาย / LEGAL OFFICE`
- มี UV0 และ Unity-generated UV2; บานซ้าย/ขวามี pivot แยกสำหรับแก้ต่อ

Prefab พร้อมใช้: `Prefabs/LawyerMainDoor.prefab`

ไฟล์ต้นฉบับ: `ArtSource/LawyerMainDoor/LawyerMainDoor_Source.blend` และ `LawyerMainDoor_GameReady.blend` สูตรสร้าง: `Tools/LawyerMainDoor/build_lawyer_door.py` / `create_door_textures.py`

การแทนในฉากรักษา Main Door root เดิม รวม transform, collider, LookTarget trigger และ references ของระบบเกม ปิดเฉพาะ renderer ของกล่องเดิม แล้วเพิ่ม visual child `MainDoor_LowPoly` ที่ชดเชย scale โดยไม่แก้ขนาด collider

ประตูยังเป็นบานปิดตามการทำงานเดิม เพราะผนังด้านหลังไม่มีช่อง ไม่มีการเพิ่มระบบเปิดประตู/ตัดผนัง/เปลี่ยนแสงห้อง โมเดลใหม่ยังไม่ได้รับการ rebake lighting ทั้งฉาก ภาพที่ระบุ Flashlight เป็นแสงตรวจชั่วคราว ไม่ใช่ไฟที่เพิ่มไว้ในเกม

หลักฐานภาพ/รายงาน/สำรองฉากก่อนแก้เก็บใน `ArtSource/LawyerMainDoor/` เครื่องมือ Editor รับเฉพาะคำขอของงานประตูนี้จากไฟล์ request และต้องบริโภคคำขอหลังทำงาน เพื่อไม่ทำซ้ำเมื่อเปิด Unity ใหม่
