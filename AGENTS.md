# ข้อกำหนดประจำโปรเจกต์ Secrets_That_Breathe

## UI ใน MainScene_2_Evening

Canvas และ UI ที่สร้างหรือแก้ใน `Assets/MainScenes/From_PM/MainScene_2_Evening.unity` ให้จัดไว้ใต้ `SeatingCanvas` ทั้งหมด และบันทึกองค์ประกอบที่ผู้ใช้ต้องแก้ข้อความ/หน้าตาไว้ในฉาก เพื่อแก้ได้ใน Hierarchy/Inspector นอก Play Mode ไม่สร้าง Canvas แยกที่ root ตอนรันเกม ใช้ nested Canvas ใต้ SeatingCanvas ได้เมื่อจำเป็นต้องจัดลำดับภาพ

## งานโมเดล Blender

ผู้ใช้ขอให้จดจำกฎ Blender ที่แนบมาไว้ในเครื่องเมื่อวันที่ 2026-10-17 ให้ใช้กฎนี้กับงานสร้าง ปรับปรุง และตรวจโมเดล Blender ในโปรเจกต์นี้ โดยเฉพาะ hard-surface props

ก่อนเริ่มงานประเภทนี้ ให้อ่านกฎฉบับเต็มที่ `Docs/Blender-Modeling-Rules-2026-10-17/blender-modeling-rules/SKILL.md` และใช้ตามขอบเขตงานกับคำสั่งปัจจุบันของผู้ใช้

- ตรวจสัดส่วนและรูปทรงหลักจาก ref ก่อนเพิ่มรายละเอียด
- รักษาชื่อ object, hierarchy, transform, pivot และชิ้นส่วนที่ต้องเคลื่อนไหวของงานเดิม
- ตรวจความหนา การประกอบ รอยต่อ topology, normals, bevel, shading และ UV จากโมเดลจริง
- จัดงบ polygon และ texture ตามระยะมองกับการใช้งานในเกม
- ตรวจหลายมุมและตรวจผล export เมื่ออยู่ในขอบเขตงาน รายงานไฟล์ที่บันทึกและข้อที่ยังไม่ได้ตรวจตามจริง

ชุดเอกสารใน `Docs/Blender-Modeling-Rules-2026-10-17/` เป็นสำเนาจากไฟล์ที่ผู้ใช้ให้มา ไม่ใช่ Blender add-on หรือหลักฐานว่าเชื่อมต่อเครื่องมือควบคุม Blender แล้ว
