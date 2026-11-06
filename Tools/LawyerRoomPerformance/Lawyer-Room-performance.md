# Lawyer Room — จำนวน triangles และจุดที่ควรลด

หมายเหตุ: ตารางนี้เป็นผลก่อนลดเก้าอี้ ปัจจุบันลดเรียบร้อยแล้ว ดู [ผลหลังปรับ](../LawyerRoomOptimization/RESULTS.md): ยอด mesh ที่เปิดในฉากลดจาก 599,460 เหลือ 181,278 triangles

ตรวจฉากที่บันทึกไว้ `Assets/MainScenes/From_PM/Lawyer Room.unity` ด้วย Unity 6000.3.8f1 ในโปรเจกต์แยก อ่านอย่างเดียว ไม่แก้โมเดล/Prefab/แสงหรือบันทึกฉากใหม่

- Mesh renderer ที่มี mesh: 331 รายการ; เปิด GameObject และ Renderer อยู่ 295 รายการ
- รวม triangles ของรายการที่เปิด: 599,460; รวม inactive ด้วย: 603,180
- เก้าอี้ 6 ตัว × 77,696 = 466,176 triangles คิดเป็น 77.77% ของยอดที่เปิด
- เก้าอี้ใช้ mesh Smooth60 ซึ่งมี 133,650 vertices ต่อ mesh แม้หนึ่ง mesh จะใช้ซ้ำได้ การวาดซ้ำและเงายังคงมีงาน geometry ตามการมองเห็นจริง

## ตารางครบทุกโมเดล/Prefab ที่อยู่ในฉาก

จำนวนชิ้นใช้จาก instance root ของ Prefab ที่ใกล้ mesh ที่สุด รวมวัตถุ inactive ด้วย คอลัมน์เปิดคือ GameObject active และ Renderer enabled ในไฟล์ฉาก ไม่ใช่จำนวนที่กล้องมองเห็นในเฟรมจริง

| โมเดล/Prefab | Tris ต่อชิ้น | จำนวนทั้งหมด | จำนวนเปิด | Tris รวมทั้งหมด | Tris รวมที่เปิด |
|---|---:|---:|---:|---:|---:|
| chair | 77,696 | 6 | 6 | 466,176 | 466,176 |
| 3-tier cabinet | 3,010 | 19 | 19 | 57,190 | 57,190 |
| Table | 4,476 | 7 | 7 | 31,332 | 31,332 |
| Glass | 1,258 | 8 | 8 | 10,064 | 10,064 |
| LouveredLight_T8_2x36W_Off | 1,472 | 5 | 5 | 7,360 | 7,360 |
| printer | 624 | 8 | 8 | 4,992 | 4,992 |
| LouveredLight_T8_2x36W | 1,472 | 3 | 3 | 4,416 | 4,416 |
| Glass have Coffee | 1,322 | 2 | 2 | 2,644 | 2,644 |
| DocumentBinder | 28 | 93 | 93 | 2,604 | 2,604 |
| KraftEnvelopes_MessyStack | 260 | 7 | 7 | 1,820 | 1,820 |
| Kitchen counter | 468 | 3 | 3 | 1,404 | 1,404 |
| Phone | 176 | 7 | 7 | 1,232 | 1,232 |
| A4Documents_Stack20 | 240 | 5 | 5 | 1,200 | 1,200 |
| OfficeGlassDoor | 1,004 | 1 | 1 | 1,004 | 1,004 |
| LowPolyMale_IdleTalk | 964 | 1 | 0 | 964 | 0 |
| Portable gas stove | 860 | 1 | 1 | 860 | 860 |
| PlayerCapsule | 832 | 1 | 0 | 832 | 0 |
| Capsule | 832 | 1 | 0 | 832 | 0 |
| Fridge | 698 | 1 | 1 | 698 | 698 |
| Water vending machine | 632 | 1 | 1 | 632 | 632 |
| MEMEEE_Empty | 564 | 1 | 0 | 564 | 0 |
| Tabletop photo frame | 140 | 4 | 4 | 560 | 560 |
| MEMEEE | 500 | 1 | 1 | 500 | 500 |
| NotebookC | 148 | 3 | 3 | 444 | 444 |
| Coffee kettle | 208 | 2 | 2 | 416 | 416 |
| Plane | 200 | 2 | 2 | 400 | 400 |
| KraftEnvelope_Open_WithA4 | 124 | 3 | 3 | 372 | 372 |
| Phone_Blank | 264 | 1 | 0 | 264 | 0 |
| carpet | 12 | 21 | 0 | 252 | 0 |
| microwave | 152 | 1 | 1 | 152 | 152 |
| picture frame | 68 | 2 | 2 | 136 | 136 |
| Book1 | 44 | 3 | 3 | 132 | 132 |
| Book2 | 44 | 3 | 3 | 132 | 132 |
| Notebook | 124 | 1 | 1 | 124 | 124 |
| A4Documents_Stack10 | 120 | 1 | 1 | 120 | 120 |
| Cube | 12 | 8 | 7 | 96 | 84 |
| Book3 | 44 | 2 | 2 | 88 | 88 |
| Bookshelf | 12 | 5 | 5 | 60 | 60 |
| booklet | 56 | 1 | 1 | 56 | 56 |
| Air conditioner | 16 | 2 | 2 | 32 | 32 |
| KraftEnvelope_Closed | 12 | 1 | 1 | 12 | 12 |
| A4Document_Single | 12 | 1 | 1 | 12 | 12 |

## ลดตรงไหนคุ้มที่สุด

1. **เก้าอี้** ควรตรวจ topology และลดก่อน เป้าลองประมาณ 5,000–10,000 triangles ต่อเก้าอี้ โดยเทียบเบาะ พนักพิง ที่วางแขน ขา ล้อ และ UV ในระยะนั่งใกล้ ตัวเลขนี้เป็นเป้าลอง ไม่ใช่ผลที่ผ่านการตรวจภาพแล้ว ต้องรักษา pivot/collider/script ที่ใช้นั่ง
   ถ้าทำได้ 8,000 ต่อเก้าอี้ จะประเมินยอด mesh ที่เปิดทั้งฉากได้ประมาณ 181,284 triangles แทน 599,460 ลด geometry ตามยอดนี้ราว 69.8% ไม่ได้แปลว่า FPS จะเพิ่มเท่ากัน
2. **ตู้ 3 ชั้น** ตัวละ 3,010 แต่มี 19 ตัว รวม 57,190 เป็นลำดับถัดไป พิจารณาลดรายละเอียดที่มองไม่เห็น/LOD โดยตรวจระยะใกล้ก่อน
3. **โต๊ะ** ตัวละ 4,476 มี 7 ตัว รวม 31,332 ลดได้บ้าง แต่ผลรวมน้อยกว่าเก้าอี้มาก
4. **พร็อพส่วนใหญ่** อยู่หลักสิบถึงหลักร้อย triangles ไม่ควรลดแรงจนเสียรูป แฟ้ม 93 ตัวมีแค่ 2,604 triangles รวม ให้ตรวจ batching/จำนวน renderers ถ้า CPU rendering หนักก่อนลด polygon เพิ่ม

## ปัจจัยอื่นที่ตรวจพบ

- มีไฟเปิด 6 ดวง โดยมี Spot ที่เป็น Realtime/Mixed และเปิดเงา 3 ดวง ไฟฉาย/ไฟบอกใบ้บางดวงปิดไว้ในฉากและจะขึ้นกับ gameplay ต้องดู Profiler ขณะเล่นเพื่อประเมินต้นทุนจริง
- MeshCollider 2 ตัว รวม 400 triangles จากไฟล์ฉาก ไม่ใช่ตัวหนักหลักที่พบ
- ไม่พบ baked occlusion data ที่เชื่อมอยู่ในไฟล์ฉาก การทำ occlusion อาจช่วยชิ้นที่ถูกผนังบัง แต่ควรตรวจผลจริงตามเส้นทางกล้อง
- Scene มีหลายโมเดลใช้ซ้ำ ความหนัก geometry, material slots, shadow passes และงาน CPU ไม่ใช่ตัวเลขเดียวกัน จำนวน Material Slot ใน CSV เป็นขอบเขตงานที่อาจต้องวาด ไม่ใช่ draw call ที่วัดจริง เพราะ batching/instancing/culling อาจเปลี่ยน

## ขอบเขตและข้อจำกัด

- นับ triangles ด้วย Mesh.GetIndexCount()/3 ของ Unity ที่ import แล้ว ไม่ใช่จำนวน faces/quads ใน Blender
- ค่ารวมคือจำนวนที่บันทึกอยู่ในฉาก ไม่ใช่ยอด GPU ต่อเฟรม ไม่มี frustum/occlusion culling หรือ shadow-pass multiplication ในตัวเลขนี้
- โปรเจกต์ตรวจไม่ได้คัดลอกสคริปต์เกม เสียง วิดีโอ หรือใช้ render pipeline ของเกม และปิดสร้าง Lightmap UV เฉพาะสำเนาโมเดล การนับนี้จึงเป็นสถานะก่อน Play Mode ไม่รวม mesh ที่สคริปต์สร้าง/สลับระหว่างเล่น รวมถึง UI/TextMeshPro ที่สร้าง mesh ตอนรัน
- ยังไม่ได้จับ Unity Profiler/Frame Debugger ในเกมจริง จึงยังสรุปไม่ได้ว่า CPU, GPU, เงา หรือสคริปต์เป็นคอขวด FPS
- ยังไม่ลดโมเดลหรือแก้แสงจากงานตรวจครั้งนี้ รายงานทุก instance พร้อม Hierarchy path อยู่ใน mesh-instances.csv; สรุปทุกชนิดอยู่ใน models.csv; แสงอยู่ใน lights.csv
