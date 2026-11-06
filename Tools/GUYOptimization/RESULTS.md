# GUY — ผลลดความหนาแน่นโมเดล

สถานะล่าสุด: ลดเพิ่มรอบสองแล้ว ดู [ผลรอบสอง](Round2/RESULTS.md) ตารางด้านล่างเก็บผลรอบแรกสำหรับเทียบและย้อนคืน

ปรับเฉพาะ FBX ที่หนัก 6 ไฟล์ใน Assets/GUY/Model โดยเก็บต้นฉบับและ .meta ไว้ใน `Tools/GUYOptimization/Backups/Assets/GUY/Model/` ชื่อโฟลเดอร์ตรงกับต้นฉบับ

| โมเดล | Triangles เดิม | Triangles หลังลด |
|---|---:|---:|
| Toolbook2 | 4,047,200 | 218,856 |
| Award display cabinet | 726,188 | 37,217 |
| ToolBox4 | 707,372 | 49,526 |
| Toolbox | 281,296 | 23,760 |
| Paint bucket Open | 225,624 | 12,182 |
| Paint bucket | 224,726 | 11,284 |
| รวม | 6,212,406 | 352,825 |

ลด triangles ใน geometry ของ FBX รวม 94.32% ขนาดหกไฟล์รวมลดจาก 121.89 MiB เหลือ 10.89 MiB การนับนี้เป็นก่อน Unity ตัดหน้าเสื่อมสภาพ/optimize; จำนวน triangles ที่ Unity นำเข้าอาจต่ำกว่านี้

วิธีลด: ลด geometry บนผิวเกือบราบ แล้ว simplify ชิ้นที่ยังหนาแน่นภายใต้งบและระยะคลาดเคลื่อนที่ตรวจได้ ใช้ unsubdivide กับตาราง subdivision ที่ collapse แล้วเสียรูป ยังคง mesh objects แยกตามเดิม ไม่รวมชิ้นเคลื่อนไหวเข้าด้วยกัน

ก่อนแทนที่ นำเฉพาะข้อมูล geometry ที่เปลี่ยนกลับใส่โครงสร้าง FBX ต้นฉบับ เก็บ Geometry/Model IDs, hierarchy, pivots/transforms, materials/textures/connections และข้อมูลอื่นเดิมไว้ ไม่ใช้การ export ทับทั้งโครงสร้างซึ่งอาจทำให้รหัสอ้างอิงเปลี่ยน `.meta` เดิมไม่เปลี่ยน

ผลตรวจ:

- สุ่มระยะจากผิวก่อน/หลังสองทิศทางและเทียบ bounding box รายชิ้น จำกัดระยะคลาดเคลื่อนที่ยอมรับไม่เกิน 0.25% ของเส้นทแยงมุมชิ้นนั้น ไม่ใช่หลักฐานว่าเหมือนกันทุกจุด
- ตรวจภาพก่อน/หลัง 4 มุมต่อโมเดลใน Blender Workbench ดูได้ใน `comparison.png`; ตรวจ ToolBox4 ซ้ำด้วย Eevee และ texture จริง (`Model2/lit-before.png`, `Model2/lit-after.png`) เนื่องจากเงา Workbench มีความต่างจาก tessellation
- นำเข้าทั้งก่อนและหลังด้วย Unity 6000.3.8f1 ในโปรเจกต์ตรวจแยก: mesh names/local file IDs, hierarchy/local transforms และจำนวน material slots ตรงกันทั้งหกไฟล์ (`unity-mesh-validation.json`)
- โปรเจกต์ตรวจปิดการสร้าง UV2 เฉพาะสำเนาทั้งสองเวอร์ชันเพื่อทดสอบตัวโมเดลและรหัสอ้างอิง ค่า Import ในโปรเจกต์เกมจริงไม่เปลี่ยน
- เปลี่ยนใน Assets/GUY เฉพาะ FBX หกไฟล์ ตรวจด้วย SHA256 ว่าไฟล์อื่น รวม Prefab, material, texture และ .meta ไม่เปลี่ยน
- ไม่แก้ Scene, แสง, Canvas หรือระบบ gameplay

ยังไม่ได้วัดเวลาโหลดหรือ FPS ในฉากเกมจริงหลังเปลี่ยน โมเดลที่ใช้ baked lightmap อาจต้อง Bake ใหม่เมื่อจะตรวจผลแสงสุดท้าย เพราะ geometry เปลี่ยน; ไม่ได้สั่ง Bake หรือเปลี่ยนการตั้งค่าแสงในงานนี้

ไฟล์แก้ต่อ: `Model0` ถึง `Model5/optimized.blend` ตามลำดับในตาราง และ `config.json` ระบุเส้นทางต้นฉบับ ส่วนต้นฉบับใน Backups เก็บไว้สำหรับย้อนคืน

รายงาน audit ใน Tools/GUYAudit เป็นสถานะก่อนลดโมเดล งานนี้มีผลหลังปรับอยู่ใน `applied.json` และเอกสารนี้
