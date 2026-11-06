# GUY — ผลลดโมเดลรอบสอง

| โมเดล | หลังรอบแรก | หลังรอบสอง |
|---|---:|---:|
| Toolbook2 | 218,856 | 24,274 |
| Award display cabinet | 37,217 | 8,047 |
| ToolBox4 | 49,526 | 9,904 |
| Toolbox | 23,760 | 5,844 |
| Paint bucket Open | 12,182 | 4,703 |
| Paint bucket | 11,284 | 3,805 |

รวม 352,825 → 56,577 triangles ลดเพิ่ม 83.96% เทียบรอบแรก
เทียบต้นฉบับก่อนแก้ 6,212,406 triangles ลดรวม 99.09%
ขนาดหกไฟล์รอบแรก 10.89 MiB → 2.93 MiB

ล้างจุดเกือบซ้ำและหน้าเสื่อมสภาพก่อน simplify เพื่อไม่ให้ตัวลดหน้าสูญเสียรูปทรงจาก topology ที่ผิดปกติ คงชิ้นผิวสีและขอบถังเปิด (Bucket_Rim) จากรอบแรก เพราะการลดชิ้นนี้ต่อทำให้ shading เปลี่ยนแม้ระยะผิวจะผ่านเกณฑ์

ตรวจภาพ Workbench 4 มุมต่อโมเดล และตรวจแสง/texture จริงด้วย Eevee เพิ่มสำหรับ Toolbook2, ToolBox4 และถังสีเปิด ผลภาพอยู่ใน Model0, Model2, Model4/lit-before.png และ lit-after.png

นำเข้า Unity 6000.3.8f1 ในโปรเจกต์แยกผ่านทั้งหกไฟล์: mesh local file IDs, hierarchy, local transforms และ material slot counts ตรงกับรอบแรก ใช้การทดสอบเดียวกับรอบแรก โดยปิดสร้าง lightmap UV เฉพาะสำเนาตรวจทั้งก่อน/หลัง ค่า Import ในเกมจริงไม่เปลี่ยน

แก้เฉพาะ FBX หกไฟล์ใต้ Assets/GUY รักษา .meta/GUID, Prefab, Material, Texture และฉากเดิม ใช้วิธีแทน geometry ในโครง FBX เดิม จึงคง IDs และข้อมูล node อื่นไว้

สำรองรอบแรกก่อนเปลี่ยนอยู่ใน Round2/Backups/Assets/GUY/Model ส่วนต้นฉบับก่อนลดทุกรอบยังอยู่ใน ../Backups/Assets/GUY/Model

ตัวเลข triangles ในตารางนับจาก FBX ก่อน Unity ตัดหน้าเสื่อมสภาพ/optimize ผล GPU/เวลาโหลดฉากจริงยังไม่ได้ benchmark; ถ้าใช้ baked lightmap ควร Bake ใหม่เมื่อจะตรวจแสงสุดท้าย
