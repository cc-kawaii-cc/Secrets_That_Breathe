# Banana Tree — Unity 6 (6000.3) / URP

> **ชุดนี้ติดตั้งใน `Assets/AImodie/BananaTree`** (แยกจากแพ็กเกจเก่า `Environment/BananaTree_Unity6_URP`)
> เปลี่ยนชื่อเพื่อไม่ให้ชนกับของเก่า: namespace `AIModelBananaTree`, Shader `Custom/AIModel/Banana Tree (URP Wind)`,
> เมนู **Tools ▸ AI Model ▸ Banana Tree ▸ 1. Setup** และ **GameObject ▸ AI Model ▸ Banana Tree ▸ Wind Zone + Controller**
> ไฟล์ต้นทาง (.blend, สคริปต์ Python, ภาพ preview) อยู่ที่ `ArtSource/AImodie_BananaTree/`
> ตัวควบคุมลมของทั้งสองชุดใช้ค่า global เดียวกัน (`_BananaWindDir`, `_BananaWindParams`) มี controller ตัวใดตัวหนึ่งในซีนก็พอ

ต้นกล้วย **3 ทรง** (A / B / C) พร้อม LOD 3 ระดับ และ Shader ที่ตอบสนอง Wind Zone
ทุกทรงมี 2 สีผล: **ผลเขียว** และ **ผลสุกสีเหลือง** (ชื่อลงท้าย `_Ripe`) รวม 6 โมเดล / 6 prefab

| ทรง | โมเดล (เขียว / สุก) | ลักษณะ |
|---|---|---|
| A | `BananaTree` / `BananaTree_Ripe` | ต้นอ่อนสูงราว 3.0 ม. ใบโค้งลู่ลง (ทรงเดิม ตามรูปที่ 1) |
| B | `BananaTree_B` / `BananaTree_B_Ripe` | สูงเรียวราว 3.7 ม. ลำต้นเล็ก ใบตั้งขึ้น |
| C | `BananaTree_C_Cloth` / `BananaTree_C_Cloth_Ripe` | เตี้ยราว 2.5 ม. ลำต้นอวบ ใบแผ่กว้าง **มีผ้า 3 สี (เหลือง/เขียว/ชมพู) พันลำต้น** พร้อมชายผ้าห้อยพลิ้วตามลม |

## ในแพ็กเกจนี้

| ไฟล์ | หน้าที่ |
|---|---|
| `Models/*.fbx` | โมเดล 6 ไฟล์ (LOD0 / LOD1 / LOD2 ในไฟล์เดียว, หน่วยเมตร) |
| `Textures/banana_leaf_albedo.png` | ใบ (RGBA, Alpha Clip) |
| `Textures/banana_trunk_albedo.png` / `_normal.png` | ลำต้น + Normal Map |
| `Textures/banana_fruit_albedo.png` / `banana_fruit_ripe_albedo.png` | ผลเขียว / ผลสุก |
| `Textures/banana_cloth_albedo.png` | ผ้า 3 สี |
| `Shaders/AIBananaTreeURP.shader` + `AIBananaWind.hlsl` | URP shader: Alpha Clip, Normal Map, สองหน้า, Wind |
| `Scripts/BananaWindController.cs` | อ่านค่าจาก WindZone แล้วส่งให้ shader |
| `Scripts/Editor/AIBananaTreeSetup.cs` | ตั้งค่าอัตโนมัติ (Material + FBX + Prefab ทั้ง 6) |
| `Source/` | ไฟล์ .blend และสคริปต์ Python ที่ใช้สร้างโมเดล/Texture |
| `../Preview/` | ภาพ preview |

## วิธีใช้

1. **ลบของเก่าก่อน (ถ้าเคย import รุ่นก่อนหน้า):** `Models/BananaTree_Cloth.fbx`, `BananaTree_Cloth.prefab` และ `Source/BananaTree_Cloth_source.blend`
   (รุ่นก่อนหน้าเป็นทรง A + ผ้า ตอนนี้เปลี่ยนเป็นทรง C แล้ว) จากนั้นลากโฟลเดอร์ `BananaTree` ทั้งโฟลเดอร์ใส่ `Assets/` เขียนทับ (โปรเจกต์ต้องเป็น URP)
2. รอ Unity import เสร็จ สคริปต์ Setup จะรันเองอัตโนมัติ 1 ครั้ง
   ถ้าไม่รัน หรืออยากรันซ้ำ: เมนู **Tools ▸ AI Model ▸ Banana Tree ▸ 1. Setup (textures, materials, FBX, prefabs)**
   - ตั้งค่า Texture/FBX, สร้าง Material 5 ตัวใน `Materials/`, สร้าง prefab 6 อัน
3. ลาก prefab ลงซีน
4. ใส่ลม: **GameObject ▸ AI Model ▸ Banana Tree ▸ Wind Zone + Controller**
   (หรือถ้ามี WindZone อยู่แล้ว ให้เพิ่ม component `Banana Wind Controller` ที่ GameObject ไหนก็ได้ 1 ตัว)

### ถ้าใบหาย/บางจากบางมุม

เกือบแน่นอนว่าใบยังใช้ Material ที่ไม่ใช่ของชุดนี้ (เช่น URP/Lit ที่ซ่อนด้านหลังของใบ)
ตรวจว่า BananaLeaf ใช้ Shader `Custom/AIModel/Banana Tree (URP Wind)` และ Cull = Off
ถ้าใช้ URP/Lit เอง ให้ตั้ง Render Face = **Both** และเปิด Alpha Clipping (Threshold 0.5)

## ขนาดใบ

ใบเล็กลงเหลือ **58%** ของรุ่นแรก ทั้งความยาว ความกว้าง และก้านใบ (ค่า `LEAF_SCALE` ใน `Source/build_banana.py`)
ทุกทรงมี 11 ใบกระจายรอบลำต้น แผ่นใบพับตัว V + บิด จึงเห็นใบได้ครบทุกมุม 360° (ดู `Preview/preview_360_degrees_B_C.png`)

## จำนวน Polygon

| LOD | ทรง A และ B | ทรง C (มีผ้า) | โครงสร้าง |
|---|---|---|---|
| LOD0 | **4,732** | **4,812** | ลำต้น + ใบ 11 + หน่อ 2 + เครือกล้วย 5 หวี (C: + แถบผ้า + ชายผ้า 3 เส้น) |
| LOD1 | 1,779 | 1,807 | ใบ 8, เครือย่อ |
| LOD2 | 416 | 430 | ใบ 6, เครือเป็นก้อนง่าย ๆ |

(สีผลเขียว/สุกใช้ polygon เท่ากัน)
mesh ชื่อ `…_LOD0/1/2` Unity จึงสร้าง LODGroup ให้เอง สคริปต์ Setup ตั้งจุดเปลี่ยน LOD ไว้ที่ 30% / 10% / 3% ของความสูงบนจอ

## Material slots

0. `BananaTrunk` — ลำต้น, ก้านใบ (Cull Back, Normal Map)
1. `BananaLeaf` — แผ่นใบ (Alpha Clip, Cull Off = สองหน้า)
2. `BananaFruit` (เขียว) หรือ `BananaFruitRipe` (สุก) — ผลและก้านเครือ
3. `BananaCloth` — เฉพาะทรง C (Cull Off, โปร่งแสงเล็กน้อย)

## ข้อมูลที่ฝังใน mesh (Wind)

Vertex Color ใช้เป็น "ข้อมูลลม" ไม่ใช่สีจริง

| Channel | ความหมาย |
|---|---|
| R | น้ำหนักการเอนของลำต้น (0 โคน → 1 ยอด) |
| G | น้ำหนักการแกว่งของใบ/เครือ/ชายผ้า |
| B | น้ำหนักการสั่นของขอบใบ/ปลายชายผ้า |
| A | เฟสสุ่ม |

UV0 = Texture, UV1.x = น้ำหนักสีใบอ่อน (ใช้กับใบหน่อ)
ตั้ง `Mesh Compression = Off` ตอน import (สคริปต์ Setup ตั้งให้แล้ว)

## ปรับแต่งใน Material

- **Trunk Bend / Leaf Sway / Leaf Flutter** — ความแรงของลมแต่ละส่วน
- **Wind** — เปิด/ปิดลมต่อ Material (ปิดที่ BananaCloth ถ้าไม่อยากให้ชายผ้าพลิ้ว)
- **Leaf Translucency** — แสงทะลุใบ/ผ้าเมื่อย้อนแสง
- **Young Leaf Tint** — สีใบอ่อนของหน่อ

## หมายเหตุ

- Shader เขียนด้วยมือสำหรับ URP 17 (Unity 6) และตรวจ syntax ด้วยเครื่องมือภายนอกแล้ว แต่**ยังไม่เคยรันใน Unity จริง**
  ถ้ามี error ที่ Console หรือสีเพี้ยน ส่งข้อความ error / ภาพ Inspector ของ Material มาได้
- WindZone ของ Unity ใช้กับ shader ทั่วไปไม่ได้โดยตรง จึงต้องมี `BananaWindController` เป็นตัวกลาง
- ใช้ WindZone แบบ **Directional** เป็นหลัก (ทิศลม = แกน Z สีน้ำเงินของ WindZone)
- ปรับรูปทรงเองได้ที่ `Source/build_banana.py` (ตัวแปร `BANANA_SHAPE` = A/B/C, `BANANA_VARIANT` = green/ripe)
