# Porsche V.2 — แยกไฟหน้า (2026-10-19)

แก้ Prefab ที่ใช้งานจริง: `Assets/Model Palm/Porsche V.2/PorscheV.2.prefab`

- เพิ่ม `Headlight_Left` และ `Headlight_Right` เป็นลูกของ root เดิม แยกผิวเลนส์พร้อมหลอดด้านในออกจาก Body/Light เดิม ขอบโคมด้านนอกยังอยู่กับ Body
- ไฟหน้าใช้ `Material/Headlights.mat` ร่วมกัน เป็นสำเนาวัสดุเดิมและอ้าง Texture atlas เดิมทั้ง Base Map และ Emission Map (`tripo_rgb_62867c90-c4b3-4b96-b7c1-0cf4f20ce93e.jpg`)
- ยังไม่ได้เปิดความสว่าง Emission: เปิด Emission และปรับสี/HDR ที่วัสดุ Headlights ได้ตามต้องการ
- มี 9 mesh objects รวม 26,436 triangles เท่าเดิม: Body 23,473, Light/ไฟท้าย 1,325, Headlight ข้างละ 316, ชิ้นอื่นรวม 1,006 ตัวเลขเป็นจำนวนหลัง triangulation ไม่มี modifier เพิ่ม
- รักษา object/component IDs, hierarchy เดิม, transforms และ pivots เดิมของชิ้นเดิมทั้งหมด ชิ้นไฟใหม่ใช้จุดอ้างอิงรถเดียวกัน (local transform identity)

## ไฟล์ที่บันทึก

- `Assets/Model Palm/Porsche V.2/Meshes/`: mesh assets สี่ไฟล์สำหรับ Body, Light และไฟหน้าแต่ละข้าง คัดลอก positions, normals, tangents, UV0 และ UV1 จาก mesh ที่ Unity นำเข้าเดิมแบบตรงตาม bytes และจัด index ใหม่เฉพาะเพื่อแยกชิ้น
- FBX เดิม, importer metadata, Texture และ Material เดิมไม่เปลี่ยน
- `Source/PorscheV2_HeadlightsSeparated.blend`: ต้นฉบับ Blender ที่แยกชิ้นแล้ว
- `Source/PorscheV2_HeadlightsSeparated.fbx`: สำเนา FBX สำหรับแก้ต่อ Prefab ในเกมใช้ Unity mesh assets ที่ตรวจแล้วข้างต้น
- `Backup/`: สำรอง Prefab และสำเนาสถานะฉากก่อนแก้ ไม่บันทึกทับ MainScene ที่ผู้ใช้เปิดค้างและยังมีงานไม่บันทึก

## ผลตรวจ

ตรวจภาพจากโมเดลจริงด้านหน้า มุมเฉียง ด้านท้าย และไฟหน้าแยกเดี่ยว ตรวจใน Unity ว่า Texture กลับมาเหมือนเดิม เลือกไฟหน้าแยกได้ และวัสดุมีหนึ่งช่องตรงกับหนึ่ง submesh ต่อไฟหน้า Prefab บันทึกแล้วและไม่ dirty; MainScene ยังคง dirty เหมือนก่อนทำงาน

Unity runtime mesh attributes คงเดิมแบบตรงตาม bytes ส่วนไฟล์ Blender/FBX สำหรับแก้ต่อผ่านการตรวจ geometry และ UV ที่ความละเอียดหกตำแหน่งทศนิยม จำนวนสามเหลี่ยมเท่าเดิม หลังคืนค่า custom normals ภาพ shaded ก่อน/หลังต่างไม่เกิน 1/255 ต่อช่องสี รายงานละเอียดอยู่ใน `final_validation.json`, `final_inspection.json` และ `Topology/independent_*_validation.json`

ข้อที่ยังไม่ได้ทดสอบ: ความสว่าง/Bloom ของ Emission และการ bake แสงในฉากเกม เพราะผู้ใช้ต้องการนำไปตั้ง Emission ต่อเอง สำหรับสำเนา FBX Blender แจ้งว่าไม่ส่ง tangent space ของชิ้นล้อเดิมที่มี ngon; geometry/UV/normals ของไฟล์นี้ตรวจแล้ว ส่วน mesh assets ที่ใช้จริงใน Prefab คง tangents เดิมจาก Unity ครบถ้วน

สคริปต์สำหรับสร้างต้นฉบับ: `build_blender_source.py` พร้อม selection `lens_selection.json` เครื่องมือ Unity แบบ Editor-only อยู่ที่ `Assets/Editor/PorscheHeadlightTools.cs` และทำงานเฉพาะเมื่อมี request ที่ระบุ operation และ ID ใหม่ ไม่มี request ค้างหลังส่งงาน
