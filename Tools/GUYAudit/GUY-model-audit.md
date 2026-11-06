# ผลตรวจโมเดลใน Assets/GUY

ตรวจแบบอ่านไฟล์อย่างเดียว: FBX 45 ไฟล์, Prefab 80 ไฟล์, texture 99 ไฟล์ รวม dependencies ที่พบ ไม่แก้โมเดล ค่า Import ฉาก หรือ Prefab

จำนวน triangle ใน geometry ของไฟล์ FBX รวม 6,336,594; หกไฟล์แรกมี 6,212,406 (98.0%) การนับนี้รวมไฟล์สำเนา และไม่ใช่จำนวน triangle ที่เปิดพร้อมกันในเกม

## สาเหตุที่พบ

- Geometry หนาแน่นมากใน Toolbox2, ตู้รางวัล, Toolbox4, Toolbox และถังสีทั้งสองแบบ เป็นหลักฐานชัดว่ามีภาระประมวลผลและข้อมูล mesh สูงกว่าพร็อพอื่นในชุด
- Generate Lightmap UVs เปิดใน 38/45 ไฟล์ รวมไฟล์หนักหกอันดับแรก การ unwrap/pack UV เพิ่มงานระหว่าง Import/Reimport โดยไม่ได้ทำซ้ำทุกครั้งที่โหลด Scene ใน Build
- Toolbox2 มี Cube.023 ชิ้นเดียว 1,072,484 triangles และชิ้นย่อยหลายชิ้นชิ้นละ 139,964 triangles; ถังสีทั้งสองมี Torus.001 ชิ้นละ 216,644 triangles
- ไม่มี LODGroup ที่บันทึกโดยตรงใน Prefab ทั้ง 80 ไฟล์ และไม่มี Auto Mesh LOD ใน FBX ทั้ง 45 ไฟล์ การไม่มี LOD เพิ่มภาระวาดระยะไกล แต่ไม่ได้แปลว่าเป็นสาเหตุของเวลาโหลดเพียงอย่างเดียว
- ToolBox4 มี material ภายใน FBX 25 ตัว จึงมีโอกาสเพิ่ม draw calls ตามการใช้ submesh/material และ batching จริง
- พบ texture เนื้อหาไฟล์เหมือนกัน 22 กลุ่ม กระจายหลายตำแหน่ง หากอ้างหลาย GUID ใน Scene อาจโหลดหลาย asset ต้องตรวจการใช้จริงก่อนรวมไฟล์
- Read/Write ของ mesh ปิดครบทุกไฟล์; Model Importer ไม่ได้เปิดสร้าง MeshCollider อัตโนมัติ; ไม่พบ MeshCollider บันทึกโดยตรงใน Prefab ชุดนี้
- Import Animation เปิดครบ 45 ไฟล์ แต่ต้นฉบับไม่มี AnimationCurve เป็นค่าที่ทำความสะอาดได้ภายหลัง ไม่ใช่หลักฐานว่านี่คือคอขวดหลัก

## ผลครบทุก FBX

| ไฟล์ (ใต้ Assets/GUY) | Triangles | Mesh | Material ใน FBX | MiB | สร้าง Lightmap UV | ประเด็น |
|---|---:|---:|---:|---:|:---:|---|
| Model/Toolbox2/Toolbook2.fbx | 4,047,200 | 34 | 5 | 74.45 | เปิด | หนักมาก |
| Model/Award display cabinet/Award display cabinet.fbx | 726,188 | 5 | 14 | 15.70 | เปิด | หนักมาก |
| Model/ToolBox4/ToolBox4.fbx | 707,372 | 15 | 25 | 15.39 | เปิด | หนักมาก |
| Model/Toolbox/Toolbox.fbx | 281,296 | 34 | 4 | 6.70 | เปิด | หนักมาก |
| Model/Paint bucket Open/Paint bucket Open.fbx | 225,624 | 4 | 3 | 4.83 | เปิด | หนักมาก |
| Model/Paint bucket/Paint bucket.fbx | 224,726 | 4 | 2 | 4.82 | เปิด | หนักมาก |
| Model/Box/Box.fbx | 54,324 | 2 | 3 | 1.02 | ปิด | ควรตรวจ geometry |
| Model/Enging Oil Box/Enging Oil Box.fbx | 34,988 | 1 | 2 | 0.99 | เปิด | ควรตรวจ geometry |
| Perfeb/HomeGUY1/Home1.fbx | 5,736 | 11 | 12 | 0.16 | ปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Tire/Tire.fbx | 5,696 | 1 | 1 | 0.09 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Car battery/Car battery.fbx | 2,952 | 8 | 10 | 0.14 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Glass/Glass.fbx | 2,580 | 4 | 5 | 0.09 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/MEMEE2/MEMEEE2.fbx | 2,152 | 6 | 12 | 5.08 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้; embedded data 4.99 MiB |
| Model/Alloy wheel/Alloy wheel.fbx | 1,866 | 1 | 1 | 0.04 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/screwdriver/screwdriver.fbx | 1,532 | 1 | 3 | 0.09 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/paper/paper.fbx | 1,236 | 7 | 20 | 0.07 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Perfeb/Paper/paper.fbx | 1,236 | 7 | 20 | 0.07 | ปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Perfeb/HomeGUY2/Home2.fbx | 884 | 6 | 8 | 1.57 | ปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้; embedded data 1.51 MiB |
| Model/Portable gas stove/Portable gas stove.fbx | 860 | 1 | 3 | 0.25 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Fridge/Fridge.fbx | 722 | 1 | 3 | 1.91 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้; embedded data 1.88 MiB |
| Model/printer/printer.fbx | 716 | 1 | 3 | 0.17 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Water vending machine/Water vending machine.fbx | 632 | 3 | 5 | 0.36 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Perfeb/Water vending machine/Water vending machine.fbx | 632 | 3 | 5 | 0.04 | ปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Sofa/Sofa.fbx | 528 | 11 | 13 | 0.15 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/MEMEE/MEMEEE.fbx | 500 | 3 | 5 | 2.49 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้; embedded data 2.45 MiB |
| Perfeb/MEMEEEE/MEMEEE.fbx | 500 | 3 | 5 | 0.04 | ปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Engine oil/Engine oil.fbx | 494 | 8 | 8 | 1.65 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้; embedded data 1.60 MiB |
| Model/Kitchen counter/Kitchen counter.fbx | 492 | 1 | 3 | 0.32 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Work desk/Work desk.fbx | 470 | 1 | 3 | 0.09 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/window/window.fbx | 440 | 2 | 6 | 0.12 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Tool cabinet/Tool cabinet.fbx | 370 | 1 | 3 | 0.21 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Coffee kettle/Coffee kettle.fbx | 216 | 6 | 8 | 0.08 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/wrench/wrench.fbx | 198 | 1 | 1 | 0.02 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Phone/Phone.fbx | 176 | 2 | 4 | 0.08 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Perfeb/Phone/Phone.fbx | 176 | 2 | 4 | 0.03 | ปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/microwave/microwave.fbx | 152 | 1 | 4 | 1.68 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้; embedded data 1.65 MiB |
| Model/NotebookC/NotebookC.fbx | 148 | 1 | 3 | 0.25 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Notebook/Notebook.fbx | 140 | 1 | 3 | 0.08 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/picture frame/Tabletop photo frame.fbx | 140 | 1 | 3 | 0.14 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Book/Book.fbx | 132 | 3 | 6 | 0.12 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Tabletop photo frame/picture frame2.fbx | 68 | 1 | 3 | 0.09 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/booklet/booklet.fbx | 64 | 1 | 3 | 0.07 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Air conditioner/Air conditioner.fbx | 16 | 1 | 2 | 0.11 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Bookshelf/Bookshelf.fbx | 12 | 1 | 2 | 0.24 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |
| Model/Carpet/carpet.fbx | 12 | 1 | 2 | 0.11 | เปิด | geometry ไม่หนักเมื่อเทียบในชุดนี้ |

## ชิ้นส่วนที่หนักในหกอันดับแรก

- **Toolbook2.fbx**: Cube.023 1,072,484 tris, Cube.003 139,964 tris, Cube.004 139,964 tris, Cube.005 139,964 tris
- **Award display cabinet.fbx**: Cube.001 439,040 tris, Cube.003 108,104 tris, Cube.004 108,104 tris, Cube.002 69,980 tris
- **ToolBox4.fbx**: Cube.005 75,812 tris, Cube.006 70,844 tris, Cube.015 70,844 tris, Cube.001 69,980 tris
- **Toolbox.fbx**: Cube 139,964 tris, Cube.002 139,964 tris, Cube.020 44 tris, Cube.022 44 tris
- **Paint bucket Open.fbx**: Torus.001 216,644 tris, Cylinder.005 6,460 tris, Cylinder.003 1,564 tris, Cylinder.004 956 tris
- **Paint bucket.fbx**: Torus.001 216,644 tris, Cylinder.005 6,460 tris, Cylinder.004 956 tris, Cylinder.003 666 tris

## อ้างอิงใน Scene ที่ตรวจพบ

ค้นการอ้างอิงที่ serialize ไว้ใน Scene ทั้งหมดใต้ Assets รวม dependency ของ Prefab GUY ที่ตรวจไว้ รวมวัตถุ inactive ด้วย จึงไม่ใช่หลักฐานว่า asset กำลังถูกแสดงหรือโหลดในเส้นทางเล่นจริงทุกกรณี การอ้างผ่าน Prefab นอก GUY หรือโค้ดโหลดแบบ dynamic อาจไม่อยู่ในผลนี้

- **Toolbook2.fbx**: Assets/Tonpalm/Garage.unity
- **Award display cabinet.fbx**: Assets/Tonpalm/Garage.unity
- **ToolBox4.fbx**: Assets/Tonpalm/Garage.unity
- **Toolbox.fbx**: Assets/Tonpalm/Garage.unity
- **Paint bucket Open.fbx**: ไม่พบอ้างอิงในขอบเขต Scene ที่สแกน
- **Paint bucket.fbx**: Assets/Tonpalm/Garage.unity
- **Box.fbx**: Assets/Tonpalm/Garage.unity
- **Enging Oil Box.fbx**: Assets/Tonpalm/Garage.unity

## ลำดับที่ควรแก้

1. ลดความหนาแน่น geometry ในสำเนาของ Toolbook2 ก่อน โดยตรวจ silhouette และรักษาชิ้นเปิด/ขยับกับ pivot เดิม จากนั้นตู้รางวัล, ToolBox4, Toolbox และ Torus ของถังสี
2. ตรวจว่าพร็อพแต่ละตัวใช้ baked lightmap หรือไม่ ก่อนปิด Generate Lightmap UVs ในตัวที่ไม่ใช้ อย่าปิดรวมทั้งโฟลเดอร์เพราะอาจกระทบแสง bake
3. เพิ่ม LOD สำหรับพร็อพที่เห็นไกล/วางซ้ำ และตรวจ material/submesh ของ ToolBox4
4. รวม texture สำเนาเมื่อยืนยัน dependency แล้ว และตั้ง compression/resolution ให้เหมาะกับขนาดบนจอ

## ข้อจำกัดของผลตรวจ

- Triangles คำนวณจาก polygon ใน FBX (จำนวนมุมลบสองต่อ polygon) ก่อน Unity แยก vertices จาก UV/normal/material และ optimize
- ขนาดไฟล์ FBX บนดิสก์ไม่เท่ากับ RAM/VRAM ในเกม; texture CSV ระบุขนาด RGBA32+mip แบบ upper bound ไม่ใช่ค่าหน่วยความจำหลัง platform compression
- บันทึก Import ที่ยังอยู่ไม่มีระยะเวลานำเข้า FBX เหล่านี้ครบ จึงยังระบุเป็นวินาทีไม่ได้ว่าไฟล์ใดทำให้รอเท่าไร ไม่ได้สั่ง Reimport เพื่อทดสอบ
- ยังไม่ได้จับ Unity Profiler ตอนเปิด Scene/โหลดระหว่างเล่น และไม่ได้ตรวจหน้าตา topology/UV/normals ผ่าน viewport จึงระบุสาเหตุจากข้อมูลไฟล์และค่า Import ไม่ใช่ผล benchmark
- รายละเอียด Prefab ทั้ง 80 ตัวอยู่ใน prefabs.csv, texture ทั้ง 99 ตัวอยู่ใน textures.csv, dependency/ชิ้นส่วน mesh/สำเนาอยู่ใน audit.json
