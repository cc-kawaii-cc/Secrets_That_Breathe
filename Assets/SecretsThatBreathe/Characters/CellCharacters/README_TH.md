# Cell Characters — นักโทษยืน / นักโทษนั่ง / ตำรวจนั่งหลับ

Unity 6000.3.8f1 + URP — ใช้ mesh และโครงกระดูกเดียวกับ ThaiPolice (964 tris, สูง 1.80 ม., Generic rig 22 กระดูก)

| Prefab | ขนาด | อนิเมชัน (วนลูป) | หายใจ |
|---|---|---|---|
| `PrisonerStanding` | สูง 1.75 ม. (เสื้อยืดแดง กางเกงสีกากี) | `Shout_RattleBars` 4.8 วิ — โวยวายคลั่ง: เขย่าซี่กรงเป็นช่วงๆ, ชี้หน้ากระแทกแขนผ่านซี่กรง, ยกกำปั้นขึ้นแล้วทุบซี่กรง, สะบัดแขนออกข้าง | หอบถี่ ทุก 0.8 วิ |
| `PrisonerSitting` | นั่งพื้น สูงตอนยืน 1.71 ม. (ไม่ใส่เสื้อ มีรอยสัก กางเกงขาสั้น) | `Sit_HoldHeadRock` 7.5 วิ — นั่งกุมหัว โยกตัวส่ายไปมา | ทุก 1.5 วิ |
| `SleepingPolice` | สูง 1.80 ม. (ThaiPolice รูปร่างอ้วนขึ้น) นั่งบน prefab `Chair` ของโปรเจกต์ | `Sleep_Breathe` 8 วิ — นั่งหลับ กอดอก หลังตรงพิงพนัก หัวโยกไปมา พร้อมสัปหงกตกแล้วสะดุ้งขึ้น | ทุก 2 วิ (อกและท้องพองชัด) |

ทุกตัว 964 tris (เท่า NavySuitVillain / ThaiPolice) texture 1024px (นักโทษ) / 512px (ตำรวจ)

## วิธีใช้
1. คลิกหน้าต่าง Unity ให้ import ระบบ setup อัตโนมัติ (หรือ `Tools > Cell Characters > 1) Setup ...`)
   - ตั้งค่า texture / FBX (Generic rig, clip วนลูป), สร้าง Material URP Lit, Animator Controller, Prefab
   - ไฟล์ที่มีอยู่แล้วจะไม่ถูกเขียนทับ (ยกเว้น prefab `SleepingPolice` ที่ถูกสร้างใหม่ครั้งเดียวให้นั่งบน Chair)
2. ลาก `PrisonerStanding.prefab`, `PrisonerSitting.prefab`, `SleepingPolice.prefab` ลงฉาก
3. `Tools > Cell Characters > 2) Create Demo Scene` — ห้องขังตัวอย่าง (ซี่กรง นักโทษยืนที่กรง นักโทษนั่งมุมห้อง ตำรวจนั่งหลับหน้าห้องขัง)

## เก้าอี้ของ SleepingPolice
- สคริปต์หา prefab ชื่อ `Chair` (ไม่มีก็ใช้ `OfficeChair`) ในโฟลเดอร์ Assets แล้วใส่เป็นลูกชื่อ `SeatChair` ใน prefab
- วัดจาก mesh ของเก้าอี้เอง: หมุนให้หันหน้า +Z, ปรับขนาดให้เบาะสูง ~0.50 ม. (ถ้าเบาะต่างจากนี้เกิน 6 ซม.), วางพนักพิงให้ชิดหลังตำรวจ
- ถ้าหาไม่เจอ/คนละตัว: เลือก prefab เก้าอี้ที่ต้องการใน Project แล้วสั่ง `Tools > Cell Characters > 3) Rebuild SleepingPolice`
- เก้าอี้ OfficeChair.fbx เดิมที่แถมมากับชุดนี้ถูกลบออกหลังสร้าง prefab ใหม่สำเร็จ

## หมายเหตุ
- ตัวละครหันหน้าไปทิศ +Z (ถ้าในโปรเจกต์หันกลับด้าน ให้หมุน prefab 180° ที่ Y)
- ท่านั่งหลับอ้างอิงภาพที่ 3, นักโทษนั่ง/ยืนอ้างอิงภาพที่ 1–2 (ท่าทาง/เสื้อผ้า ไม่ใช่ใบหน้าบุคคลจริง)
- ตำรวจใช้ texture ThaiPolice เดิม แต่วาดตาให้หลับ (`SleepingPolice/Textures/SleepingPolice_BaseColor.png`)
- ยังไม่ได้ทดสอบ Play Mode ใน Unity — ทดสอบจาก Blender (นำเข้า FBX กลับมาตรวจขนาด/อนิเมชัน) เท่านั้น
