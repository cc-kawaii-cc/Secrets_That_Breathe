# iPhone DUO

โมเดลโทรศัพท์พับตามภาพอ้างอิงของผู้ใช้ สร้างใน Blender 5.2 และนำเข้า Unity 6000.3.8f1 / URP 17.3.0 ผ่าน CLI วันที่ 2026-10-17

## ใช้งาน

ลาก Prefab จาก `Prefabs/` ลงฉาก:

- `IPhoneDuo_Open.prefab` — กางเต็ม
- `IPhoneDuo_HalfOpen.prefab` — พับ 90°
- `IPhoneDuo_Closed.prefab` — พับปิด

ทุกแบบปรับ `Fold Angle` ของคอมโพเนนต์ `IPhoneDuoFold` ได้ตั้งแต่ 0 ถึง 180° หรือเรียก `SetFoldAngle(float)`, `Open()`, `HalfOpen()`, `Close()` จากเกม มี BoxCollider แยกตามบาน และ GripAnchor/HingeAnchor/DisplayAnchor สำหรับผูกระบบถือ/ตรวจของ

ใช้หน่วยเมตร และ scale 1 ตามจริง: ตัวเครื่องกาง 164.6 × 117.8 × 5.2 มม.; พับ 84.1 × 117.8 × 11.3 มม. ตัวเลขนี้ไม่รวมกล้องนูน ปุ่ม และชิ้นเชื่อมบานพับ ดูขนาด bounding box รวมจริงใน `UnityVerification.json`

## ไฟล์

- `Models/IPhoneDuo.fbx` — 31,926 triangles หลัง triangulation, 20 mesh renderers, 10 วัสดุ
- `Textures/Inner_Display.png` — จอใน 2048 × 1466, ภาพต่อกันสองบาน
- `Textures/Cover_Display.png` — จอนอก 1024 × 1466
- `Materials/` — วัสดุ URP Lit พร้อม remap
- `Demo/IPhoneDuo_Preview.unity` — ฉากตรวจแยก; ภาพผล Unity อยู่ใน `Preview/`
- `Editor/IPhoneDuoAssetBuilder.cs` — เมนู `Tools > Secrets That Breathe > iPhone DUO` สำหรับสร้าง Prefab ใหม่

ไฟล์ต้นฉบับนอก Assets: `ArtSource/IPhoneDuo/IPhoneDuo.blend` แยกชิ้นแก้ต่อ 60 mesh objects; `IPhoneDuo_GameReady.blend` รวมรายละเอียด rigid ตามวัสดุ/บาน; สูตรสร้างอยู่ใน `Tools/IPhoneDuo/` ภาพอ้างอิงและภาพตรวจจาก Blender อยู่กับ ArtSource

## ตรวจแล้วและข้อจำกัด

ตรวจ topology แบบ solid, normals, UV0, รอยต่อจอ, รูเสียง/USB ที่มีความลึก และภาพ Blender หลายมุม ตรวจ FBX ที่นำเข้า Unity จริงพร้อมวัสดุ สเกล hierarchy และ pivot; ทดสอบ transform การพับ 13 มุมตั้งแต่ 0–180°

บานพับเป็นกลไกแบบง่ายสำหรับเกม จอเป็นภาพนิ่ง และปุ่ม/กล้องยังไม่มีพฤติกรรมใช้งาน ไม่มีระบบรับสาย ถือของ หรือผูก input เพิ่มลงเกมเดิม BoxCollider เป็นรูปทรงประมาณ ไม่ได้จำลองฟิสิกส์ hinge หรือรับรองการชนทุกสามเหลี่ยม วัสดุสีพื้นใช้ UV0 แบบ planar; ไม่ได้ทำ UV2 สำหรับอบ lightmap และไม่มี LOD ระยะไกล

หากต้องสร้างซ้ำผ่าน CLI ให้ส่งออกด้วย `Tools/IPhoneDuo/build_iphone_duo.py` ใน Blender ก่อน แล้วรัน `Tools/IPhoneDuo/Build-IPhoneDuo.ps1` ตัวตรวจ Unity ใช้โปรเจกต์แยกใน `tmp/IPhoneDuoUnityVerify` และคัดลอกเฉพาะ generated assets/.meta กลับ
