# Rice Field Pack: ต้นข้าว หญ้าในนา และพื้นนา (Unity 6000.3 / URP)

แพ็กนี้ใช้กับ Unity 6000.3.x ที่ใช้ Universal Render Pipeline (URP)
หน่วยในแพ็กเป็นเมตร (1 unit = 1 m) และ pivot ของทุกโมเดลอยู่ที่โคนต้นบนพื้น

---

## 1. การติดตั้ง

1. ลากโฟลเดอร์ `RiceField` ทั้งโฟลเดอร์ไปวางใต้ `Assets/` ในหน้าต่าง Project
2. รอให้ Unity import เสร็จ จากนั้นสคริปต์จะ**ตั้งค่าทุกอย่างให้อัตโนมัติ 1 ครั้ง**
   (ดูข้อความ `[RiceField] Setup complete` ใน Console)
   - ถ้าไม่ขึ้น ให้กดเมนู **Tools > Rice Field > 1) Setup Materials, Prefabs & Terrain Layers**
3. เมื่อเสร็จจะมีโฟลเดอร์ใหม่ดังนี้
   - `Materials/` มี material แบบ URP Lit ทั้งของต้นไม้/หญ้าและของพื้น
   - `Prefabs/` มี prefab ที่ใส่ LOD Group ไว้แล้ว **ให้ใช้ไฟล์ในโฟลเดอร์นี้**
   - `TerrainLayers/` มี Terrain Layer ของพื้นนาทุกแบบ พร้อมใช้กับ Terrain
4. ถ้าอยากดูตัวอย่าง ให้กด **Tools > Rice Field > 2) Create Demo Scene**
   ฉากตัวอย่างเป็นนาขนาด 30×30 ม. แบ่งเป็น 4 แปลงพร้อมคันนา มีแปลงข้าวสุก แปลงข้าวเกือบสุก แปลงหลังเกี่ยว และแปลงไถ

---

## 2. โมเดล (FBX ใน `Models/`)

**รุ่นหลัก (Low-poly สำหรับวางทีละเยอะ ๆ) อยู่ใน `Models/` และ `Prefabs/`**
- รุ่นนี้ใช้แผ่นภาพไขว้กัน 3–4 แผ่น ภาพบนแผ่นอบมาจากโมเดลละเอียดทั้ง สี, normal และ AO
- เสริมรวงข้าวกับช่อหญ้าแบบ 3 มิติจริงไว้ไม่กี่อัน เพื่อให้มีมิติเวลามองใกล้
- ทุกตัวมี 2 LOD คือ `_LOD0` และ `_LOD1`

| Prefab | คืออะไร | LOD0 | LOD1 |
|---|---|---|---|
| Rice_Ripe_A / B / C | กอข้าว**สุก** รวงเหลืองทองโน้มลง | 176 | 8 |
| Rice_AlmostRipe_A / B / C | กอข้าว**เกือบสุก** รวงเหลืองอมเขียว ยังเชิดขึ้น ใบเขียว | 176 | 8 |
| Grass_Barnyard_A / B | หญ้าข้าวนก | 150 | 6 |
| Grass_DikeTuft_A / B | หญ้าพุ่มเตี้ยบนคันนา | 134 | 6 |
| Grass_Sedge_A / B | กกทราย | 138 | 6 |
| Grass_Cogon_A / B | หญ้าคา | 150 | 6 |

ตัวอย่างการคำนวณ: ข้าว 1,000 กอ ระยะใกล้จะใช้ราว 176,000 polygon ส่วนที่อยู่ไกลจะเหลือกอละ 8 polygon
Material ของรุ่นนี้คือ `M_RiceCards_Ripe`, `M_RiceCards_AlmostRipe`, `M_GrassCards`

**รุ่นละเอียด (High Detail) อยู่ใน `Models/HighDetail/` และ `Prefabs/HighDetail/`** ชื่อลงท้ายด้วย `_HD`
- เป็นโมเดลชุดแรก LOD0 ใช้ 3,000–4,000 polygon และมี 3 LOD
- เหมาะกับฉากที่กล้องเข้าใกล้มาก ๆ หรือฉากคัตซีน ไม่ควรเอาไปวางเป็นร้อยต้น
- ใช้ material `M_Rice_Ripe`, `M_Rice_AlmostRipe`, `M_FieldGrass`

ความต่างของข้าวสุกกับข้าวเกือบสุก
- **ข้าวสุก**: รวงแน่นสีทองห้อยลงมาก ทรงกอเตี้ยลง (ประมาณ 0.95–1.0 ม.) ใบเหลืองฟาง
- **ข้าวเกือบสุก**: รวงเหลืองอมเขียว ปลายรวงเหลืองก่อน รวงยังเชิดขึ้น กอดูสูงกว่า (1.05–1.13 ม.) ใบยังเขียว

---|---|---|---|---|---|
| Rice_Ripe_A | กอข้าว**สุก** รวงเหลืองทองโน้มลง ใบเหลืองฟาง | 3,864 | 1,218 | 345 | 1.00 m |
| Rice_Ripe_B | กอข้าวสุก (รูปทรงต่างกัน) | 3,836 | 1,218 | 345 | 0.96 m |
| Rice_Ripe_C | กอข้าวสุก (รูปทรงต่างกัน) | 3,994 | 1,276 | 345 | 0.96 m |
| Rice_AlmostRipe_A | กอข้าว**เกือบสุก** รวงเหลืองอมเขียว ยังตั้งชันกว่า ใบยังเขียว | 3,794 | 1,218 | 345 | 1.10 m |
| Rice_AlmostRipe_B | กอข้าวเกือบสุก (รูปทรงต่างกัน) | 3,594 | 1,160 | 322 | 1.04 m |
| Rice_AlmostRipe_C | กอข้าวเกือบสุก (รูปทรงต่างกัน) | 3,738 | 1,218 | 345 | 1.13 m |
| Grass_Barnyard_A / B | หญ้าข้าวนก พุ่มสูง มีช่อเมล็ดสีเขียวอมม่วง (วัชพืชหลักในนา) | ~3,130 | ~640 | 100 | 0.85 m |
| Grass_DikeTuft_A / B | หญ้าพุ่มเตี้ยบนคันนา ใบเล็กแน่น มีช่อแบบหญ้าแพรก | 3,082 | 1,343 | 294 | 0.30 m |
| Grass_Sedge_A / B | กกทราย มีช่อดอกสีทองแผ่เป็นร่ม | 3,063 | 753 | 162 | 0.55 m |
| Grass_Cogon_A / B | หญ้าคา ใบยาวตั้ง มีช่อดอกขาวฟู | 3,210 | 714 | 177 | 0.90 m |

ข้าวสุกกับข้าวเกือบสุกต่างกันทั้งสีและขนาด/ทรง
- **ข้าวสุก**: รวงแน่น สีทอง โค้งห้อยลงมาก ทำให้ทรงกอเตี้ยลง ใบเหลืองฟางและมีใบล่างแห้ง
- **ข้าวเกือบสุก**: รวงเหลืองอมเขียว ปลายรวงเริ่มเหลืองก่อน รวงยังเชิดขึ้น ทำให้กอดูสูงกว่า ใบยังเขียวเกือบทั้งต้น

Material ที่ใช้มีแค่ 3 ตัว: `M_Rice_Ripe`, `M_Rice_AlmostRipe`, `M_FieldGrass`
แต่ละตัวใช้ texture atlas เดียว จึงเป็น 1 draw call ต่อโมเดล และเปิด GPU Instancing ไว้แล้ว

---

## 3. Texture พื้นนา (ใน `Textures/Ground/`) ต่อกันได้ไร้รอยต่อ (seamless) ทั้ง 9 แบบ

| ชื่อ | พื้นแบบไหน |
|---|---|
| Paddy_WetMud | โคลนนาเปียก มีแอ่งน้ำขังเป็นหย่อม มีเศษฟาง |
| Paddy_DampSoil | ดินนาชื้น ใช้เป็นพื้นทั่วไปได้ |
| Paddy_Flooded | นาน้ำขัง น้ำขุ่นสีโคลน มีแหนและฟางลอย มีโคลนโผล่บางส่วน |
| Paddy_AlgaeMud | โคลนมีตะไคร่และแหน |
| Paddy_DryCracked | ดินนาแห้งแตกระแหง ขอบแผ่นดินม้วนขึ้น |
| Paddy_Stubble | นาหลังเกี่ยว มีตอซังเป็นแถวห่างกัน 25 ซม. และฟางกระจาย |
| Paddy_StrawMat | ฟางข้าวคลุมดินหนา ๆ |
| Paddy_Plowed | ดินไถ เป็นร่องและก้อนดิน |
| Dike_GrassySoil | ดินคันนาอัดแน่น มีหญ้าสั้นและกรวด |

แต่ละแบบมี 3 ไฟล์
- `_Albedo` คือสีพื้นผิว (2048 px)
- `_Normal` คือ normal map แบบ OpenGL/Unity (1024 px)
- `_Mask` (1024 px) แพ็กช่องสีไว้ดังนี้: **R = Metallic, G = AO, B = Height, A = Smoothness**
  - ไฟล์นี้ใช้เป็น **Mask Map ของ Terrain Layer** ได้ตรง ๆ
  - ใช้ใน URP Lit ได้ทั้งช่อง Metallic Map และ Occlusion Map พร้อมกัน (สคริปต์ตั้งให้แล้ว)

**ขนาดจริงของ 1 tile คือ 2 × 2 เมตร** (Terrain Layer ตั้ง Tile Size = 2 ไว้แล้ว)
ส่วน material ของพื้นตั้ง tiling = 5 ไว้ ให้พอดีกับ Plane มาตรฐาน 10 ม. ของ Unity

---

## 4. ใช้กับ Terrain ของคุณเอง

**ทาพื้น:** เลือก Terrain แล้วไปที่ Paint Texture > Edit Terrain Layers > Add Layer แล้วเลือก `TL_...` ที่ต้องการ
ถ้าอยากให้ขอบระหว่างพื้นแต่ละแบบเนียนแบบอิงความสูง ให้เปิด **Enable Height-based Blend** ใน material ของ Terrain

**ปลูกข้าวเป็นแถว:** ใช้ **Tools > Rice Field > 3) Open Planter Window**
- ใส่ Terrain และ prefab ที่ต้องการ (ใส่หลายตัวได้ ระบบจะสุ่มสลับให้)
- ตั้ง Area เป็นเมตร (นับจากมุมของ Terrain)
- ค่าเริ่มต้นคือระยะแถว 0.30 ม. และระยะกอ 0.25 ม. ใกล้เคียงนาดำทั่วไป
- โหมด Scatter ใช้โรยหญ้า/วัชพืชแบบสุ่ม
- ปุ่ม Clear ใช้ลบเฉพาะในพื้นที่ที่กำหนด และกด Undo ได้

ระบบจะวางทุกต้นเป็น Terrain Trees ซึ่งรองรับ LOD Group และเบากว่าการวาง GameObject ทีละอัน
หรือจะลาก prefab ไปใส่ใน Paint Trees ของ Terrain เองก็ได้

---

## 5. หมายเหตุทางเทคนิค

- Material ของต้นไม้และหญ้าใช้ **URP/Lit**: Alpha Clipping = 0.5, Render Face = Both และมี normal map
- normal ของ vertex ในโมเดลถูกจัดให้ชี้ออกจากกอและเอียงขึ้นฟ้า ใบด้านหน้าและด้านหลังจึงได้แสงเท่ากันเมื่อใช้ Render Face = Both (ไม่มีด้านมืดดำ)
- Texture ของต้นไม้เปิด **Mip Maps Preserve Coverage** ไว้ รวงข้าวจึงไม่บางหายเมื่อมองจากไกล
- **Vertex Color เก็บข้อมูลไว้ทำลมพัด** ใช้ใน Shader Graph ได้
  - R = น้ำหนักการโยกตามความสูง (โคน 0 ถึงยอด 1)
  - G = ค่า phase สุ่มต่อหนึ่งต้นย่อย (กันไม่ให้ทุกต้นโยกพร้อมกัน)
  - B = น้ำหนักการสั่นของปลายใบ/ปลายรวง
  - วิธีทำลมง่าย ๆ ใน Shader Graph: `offset = windDir * sin(time*speed + G*6.28) * R * strength` แล้วต่อเข้า Position (Object space)
- ระยะสลับ LOD ที่ตั้งไว้ (screen height) ปรับได้ที่ LOD Group บน prefab
  - รุ่น Low-poly ข้าว: เปลี่ยนเป็น LOD1 ที่ 5% และหายไปที่ 0.4% (ประมาณ 145 ม. เมื่อ FOV 60°)
  - รุ่น Low-poly หญ้า: 5% / 0.6%
  - รุ่น HD ข้าว: 30% / 10% / 0.8% และรุ่น HD หญ้า: 22% / 7% / 0.6%
- LOD2 ของรุ่น HD ปิดการทอดเงาไว้เพื่อประหยัด ส่วนรุ่น Low-poly ทอดเงาทุก LOD (เพราะเบามากอยู่แล้ว)
- ถ้าวางหลายพันกอแล้วยังหนัก ให้ปิด Cast Shadows ของ LOD1 หรือลด Shadow Distance ใน URP Asset
- ถ้าต้องการให้ LOD เปลี่ยนแบบค่อย ๆ จาง ให้เปิด **LOD Cross Fade** ใน URP Asset แล้วตั้ง Fade Mode ของ LOD Group เป็น Cross Fade
- ถ้าจะทำทุ่งนาใหญ่มาก (หลายหมื่นกอ) ควรใช้ Terrain Trees และลดระยะ Tree Distance ของ Terrain

ถ้าต้องการรันการตั้งค่าใหม่ (เช่น หลังจากแก้ texture) ให้กดเมนู 1) ซ้ำได้ จะไม่สร้างไฟล์ซ้ำซ้อน

---

## 6. ลมพัด (Wind Zone) มีเฉพาะรุ่น Low-poly

- Material `M_RiceCards_Ripe`, `M_RiceCards_AlmostRipe` และ `M_GrassCards` ใช้ shader **RiceField/Foliage Wind URP**
  - shader นี้ทำต่อจาก URP Lit จึงได้แสง, เงา, Forward+, Deferred และ GPU Instancing เหมือนเดิม
  - **ต้นข้าวและหญ้าจะโยกตาม Wind Zone ในฉากเอง ไม่ต้องใส่ component เพิ่ม**
- **เพิ่ม Wind Zone:** กด **Tools > Rice Field > 5) Add Wind Zone to Scene** หรือใช้ GameObject > 3D Object > Wind Zone ก็ได้
  - แนะนำให้ตั้ง Mode เป็น **Directional**
  - ทิศลมดูจากลูกศรของ Wind Zone หมุนแกน Y เพื่อเปลี่ยนทิศ
  - **Main** คือความแรงที่กอเอน
  - **Turbulence** คือการสั่นของใบและรวง
  - **Pulse Magnitude / Pulse Frequency** คือลมกระโชกเป็นจังหวะ
- **ปรับแยกตาม material** (เลือก material แล้วดูหัวข้อ Wind)
  - `Bend, Flutter, Plant Height, Sway Speed` คือระยะเอน (เมตรต่อค่า Main 1 หน่วย), ระยะสั่น, ความสูงต้น และความเร็วโยก
  - `Gust Wave Scale` คือความถี่ของคลื่นลมที่วิ่งผ่านทุ่ง ค่ามากระลอกจะถี่ขึ้น
- **ถ้าในฉากไม่มี Wind Zone ต้นจะนิ่ง**
- ถ้าอยากเห็นการโยกใน Scene view ตอนยังไม่กด Play ให้เปิด **Always Refresh** ในเมนู Effects ของ Scene view
- รุ่น HD ยังใช้ URP Lit ธรรมดาและไม่โยก
- ถ้า shader มีปัญหา สคริปต์จะสลับกลับไปใช้ URP Lit ให้เอง และเขียนข้อความ `[RiceField] Wind shader ...` ไว้ใน Console

## 7. ใช้กับ Terrain (รุ่น Low-poly)

เลือก Terrain ใน Hierarchy แล้วกด **Tools > Rice Field > 4) Add Low-poly Prefabs to Selected Terrain (Trees + Details)**
ระบบจะเพิ่มต้นไม้ให้ทั้งใน Paint Trees และ Paint Details ทั้งสองแบบโยกตาม Wind Zone

| ใช้ทำอะไร | เครื่องมือ Terrain | Prefab ที่ใช้ |
|---|---|---|
| ปลูกข้าวเป็นแถว หรือวางแบบมี LOD | **Paint Trees** / Mass Place Trees / Planter Window | `Prefabs/*.prefab` (มี LOD Group) |
| ใช้แปรงระบายข้าวหรือหญ้าเป็นผืน | **Paint Details** | `Prefabs/TerrainDetail/*_Detail.prefab` (mesh เดียว, GPU Instancing) |

- Detail ต้องใช้แบบ Instanced
  - ถ้าใช้ปุ่มเมนูข้างบน ระบบตั้งให้แล้ว
  - ถ้าเพิ่ม Detail เอง ให้เลือก Detail Mesh แล้วติ๊ก **Use GPU Instancing**
- Detail ไม่มี LOD ระยะที่หายไปขึ้นกับ **Detail Distance** ใน Terrain Settings (ปุ่มเมนูตั้งให้อย่างน้อย 120 ม.)
- ค่า Wind Settings for Grass ของ Terrain ไม่มีผลกับ shader นี้ เพราะ shader อ่านค่าจาก Wind Zone
