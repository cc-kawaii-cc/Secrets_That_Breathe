# Lawyer Room — ลด mesh เก้าอี้

| รายการ | ก่อน | หลัง |
|---|---:|---:|
| Triangles ต่อเก้าอี้ | 77,696 | 7,999 |
| Vertices ต่อ mesh | 133,650 | 7,153 |
| Triangles เก้าอี้ 6 ตัว | 466,176 | 47,994 |
| Mesh ที่เปิดในฉากที่บันทึกไว้รวม | 599,460 | 181,278 |

ลด geometry ตามยอดเปิดในฉาก 69.76% ไม่ใช่ผลวัด FPS

แก้เฉพาะ standalone Mesh asset ใต้ Assets/SecretsThatBreathe/Props/SmoothedChairs ซึ่งเก้าอี้ทั้ง 6 ตัวใน Lawyer Room อ้างอยู่ ไม่เปลี่ยน FBX รวมเฟอร์นิเจอร์ ต้นฉบับถูกสำรองใน Backups ตามโครงเส้นทางเดิม

ลดจุดซ้ำและหน้าเสื่อมสภาพ แล้ว simplify ภายใต้งบประมาณ 8,000 triangles ตรวจระยะผิวจาก/ไปต้นฉบับ โดยความคลาดเคลื่อนตัวอย่างสูงสุดประมาณ 0.0634% ของเส้นทแยงมุมโมเดล ถ่ายทอด normals จากผิว Smooth60 ต้นฉบับเพื่อลดรอยเงาเป็นคลื่น เก็บ UV0 และ UV2 ไว้

ตรวจภาพก่อน/หลัง 5 มุมใน Blender Workbench (comparison.png) และนำ mesh กลับเข้า Unity 6000.3.8f1 ในโปรเจกต์สำเนา สร้าง native .asset โดยคง GUID d33858a2ac6bb694a93c7b4c42df5b08 และ local file ID 4300000

ตรวจฉากในสำเนาว่ามีเพียง vertices/triangles ของ mesh เก้าอี้ 6 รายการเปลี่ยน รายการไฟและ MeshCollider เท่าเดิมทุกค่า ตรวจ SHA256 ของ Scene, chair.prefab และ ChairSmoothShadingSetup ว่าไม่เปลี่ยน การใช้งานจุดนั่ง/สคริปต์ไม่ถูกแก้ แต่ยังไม่ได้ทดสอบนั่งจริงใน Play Mode

chair.prefab/FBX เดิมเก็บไว้ตามเดิม งานนี้เปลี่ยน mesh ที่ฉาก Lawyer Room override ใช้อยู่ ส่วน ChairSmoothShadingSetup จะโหลด asset เดิมที่ลดแล้วเมื่อมีเก้าอี้ใหม่เข้าฉาก

ยังไม่ได้วัด FPS หรือ CPU/GPU Profiler ในเกมจริง และไม่ได้ Bake แสงใหม่หรือแก้ไฟ/Canvas จำนวน triangles เป็นยอดรายการ active/enabled ในไฟล์ฉากก่อน Play Mode ไม่ใช่จำนวนที่กล้องวาดต่อเฟรม

ต้นฉบับยังมีไฟล์ Recovery อ้าง mesh เดียวกัน ดังนั้นเมื่อเปิด Recovery ภาพเก้าอี้ก็จะใช้ข้อมูล mesh รุ่นใหม่ผ่าน GUID เดียวกันด้วย; backup ในโฟลเดอร์นี้เก็บรุ่นเก่าไว้ครบ
