# สกิล Blender สำหรับแชร์

ไฟล์หลักคือ `blender-modeling-rules/SKILL.md` เป็นกฎสำหรับ AI ช่วยปั้นและตรวจโมเดล เน้นสัดส่วน การประกอบ geometry, bevel, shading, UV และการรักษางานเดิม ใช้อ่านเป็นแนวทางทำโมเดลได้ด้วย

## ใช้กับ Codex บน Windows

1. แตก ZIP นี้
2. คัดลอกโฟลเดอร์ `blender-modeling-rules` ทั้งโฟลเดอร์ไปไว้ใน `%USERPROFILE%\.codex\skills\` หรือโฟลเดอร์ `skills` ใต้ `CODEX_HOME` หากกำหนดไว้เอง
3. โครงสร้างต้องเป็น `.codex/skills/blender-modeling-rules/SKILL.md` และมีโฟลเดอร์ `agents` อยู่ข้างกัน
4. เปิดแชตใหม่ แล้วเรียก `$blender-modeling-rules` พร้อมแนบ ref และระบุไฟล์/ชิ้นส่วนที่ต้องการแก้

ตัวอย่าง: “ใช้ $blender-modeling-rules ปรับตู้จากภาพ ref ให้สัดส่วนถูก มีผนังปิดครบและ bevel สมจริง เก็บชื่อ object และ pivot เดิม แล้วส่งภาพตรวจด้านหน้า ด้านข้าง และรอยต่อสำคัญ”

## ใช้กับ AI อื่น

นำเนื้อหา `SKILL.md` ไปเพิ่มเป็น project instructions หรือแนบให้ AI อ่านก่อนเริ่มงานตามวิธีที่เครื่องมือนั้นรองรับ

สกิลเป็นแนวทางทำงาน ไม่ใช่ Blender add-on และไม่ได้ติดตั้งเครื่องมือควบคุม Blender ผู้ใช้ต้องมี Blender และช่องทางทำงานที่ AI ของตนใช้งานได้ เช่น Blender Python/CLI หรือ MCP ที่เชื่อมไว้แล้ว
