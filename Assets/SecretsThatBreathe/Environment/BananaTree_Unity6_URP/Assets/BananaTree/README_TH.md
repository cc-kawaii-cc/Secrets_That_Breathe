# Banana Tree — Unity 6 (6000.3) / URP

โมเดลต้นกล้วย **2 แบบ**: ผลสีเขียว (`BananaTree`) และผลสุกสีเหลือง (`BananaTree_Ripe`)
LOD 3 ระดับ, Shader ที่ตอบสนอง Wind Zone, ใบมองเห็นได้ครบทุกมุม 360°

## ในแพ็กเกจนี้

| ไฟล์ | หน้าที่ |
|---|---|
| `Models/BananaTree.fbx` | ต้นกล้วยผลสีเขียว (LOD0/1/2 ในไฟล์เดียว, หน่วยเมตร) |
| `Models/BananaTree_Ripe.fbx` | ต้นกล้วยเดียวกัน แต่ผลสุกสีเหลือง |
| `Textures/banana_leaf_albedo.png` | ใบ (RGBA, ตัดจากรูปที่ 4) |
| `Textures/banana_trunk_albedo.png` / `_normal.png` | ลำต้น + Normal Map (จากรูปที่ 5–6) |
| `Textures/banana_fruit_albedo.png` | ผลสีเขียว |
| `Textures/banana_fruit_ripe_albedo.png` | ผลสุกสีเหลือง (มีจุดน้ำตาล, ก้านเขียว, ปลายดำ) |
| `Shaders/BananaTreeURP.shader` + `BananaWind.hlsl` | URP shader: Alpha Clip, Normal Map, ใบสองหน้า, Wind |
| `Scripts/BananaWindController.cs` | อ่านค่าจาก WindZone แล้วส่งให้ shader |
| `Scripts/Editor/BananaTreeSetup.cs` | ตั้งค่าอัตโนมัติ (Material + FBX + Prefab ทั้งสองแบบ) |
| `Source/` | ไฟล์ .blend และสคริปต์ Python ที่ใช้สร้างโมเดล/Texture |
| `Preview/` | ภาพ preview |

## วิธีใช้

1. ลากโฟลเดอร์ `BananaTree` ทั้งโฟลเดอร์ใส่ `Assets/` (โปรเจกต์ต้องเป็น URP)
2. รอ Unity import เสร็จ — สคริปต์ Setup จะรันเองอัตโนมัติ 1 ครั้ง
   ถ้าไม่รัน หรืออยากรันซ้ำ: เมนู **Tools ▸ Banana Tree ▸ 1. Setup (textures, materials, FBX, prefabs)**
   - ตั้งค่า Texture/FBX, สร้าง Material 4 ตัวใน `Materials/`, สร้าง `BananaTree.prefab` และ `BananaTree_Ripe.prefab`
3. ลาก prefab ลงซีน
4. ใส่ลม: **GameObject ▸ Banana Tree ▸ Wind Zone + Controller**
   (หรือถ้ามี WindZone อยู่แล้ว ให้เพิ่ม component `Banana Wind Controller` ที่ GameObject ไหนก็ได้ 1 ตัว)

### ถ้าใบยังหาย/บางจากบางมุม

สาเหตุเกือบแน่นอนคือใบยังใช้ Material ที่ไม่ใช่ของชุดนี้ (เช่น URP/Lit ที่ซ่อนด้านหลังของใบ)
ตรวจที่ prefab → ใบ (BananaLeaf) ต้องใช้ Shader `Custom/Banana Tree (URP Wind)` และ **Render Face / Cull = Both (Off)**
ถ้าใช้ URP/Lit เอง ให้ตั้ง Render Face = **Both** และเปิด Alpha Clipping (Threshold 0.5)

## การปรับปรุงเรื่องการมองเห็นใบ 360°

- ใบมี 11 ใบ (เดิม 9) กระจายรอบลำต้นทุกทิศ ขนาดใบลดลงเหลือ 72% ของรุ่นก่อน (ปรับได้ที่ `LEAF_SCALE` ใน Source/build_banana.py)
- แผ่นใบพับเป็นรูปตัว V ลึกขึ้น และบิดตามความยาวใบ ทำให้มองจากขอบใบก็ยังเห็นเป็นพื้นผิว ไม่บางเป็นเส้น
- ตรวจด้วยการ render silhouette ของทั้งต้นจากทุกทิศ (azimuth) และหลายมุมก้ม-เงย: พื้นที่ใบที่เห็นต่ำสุดอยู่ที่ ≥ 83% ของค่าสูงสุด
- Shader ใช้ไฟสองหน้า (ด้านหลังใบได้แสงปกติ ไม่ดำ) และ Alpha Clip

## จำนวน Polygon (ทั้งสองโมเดลเท่ากัน)

| LOD | Triangles | Vertices | โครงสร้าง |
|---|---|---|---|
| LOD0 | **4,732** | 3,166 | ลำต้น + ใบ 11 + หน่อ 2 + เครือกล้วย 5 หวี |
| LOD1 | 1,779 | 1,319 | ใบ 8, เครือย่อ |
| LOD2 | 416 | 377 | ใบ 6, เครือเป็นก้อนง่าย ๆ |

mesh ชื่อ `…_LOD0/1/2` Unity จึงสร้าง LODGroup ให้เอง สคริปต์ Setup ตั้งจุดเปลี่ยน LOD ไว้ที่ 30% / 10% / 3% ของความสูงบนจอ

## Material slots

0. `BananaTrunk` — ลำต้น, ก้านใบ (Cull Back, Normal Map)
1. `BananaLeaf` — แผ่นใบ (Alpha Clip, Cull Off = สองหน้า)
2. `BananaFruit` (ต้นเขียว) หรือ `BananaFruitRipe` (ต้นสุก) — ผลและก้านเครือ

## ข้อมูลที่ฝังใน mesh (Wind)

Vertex Color ใช้เป็น "ข้อมูลลม" ไม่ใช่สีจริง

| Channel | ความหมาย |
|---|---|
| R | น้ำหนักการเอนของลำต้น (0 โคน → 1 ยอด) |
| G | น้ำหนักการแกว่งของใบ/เครือ |
| B | น้ำหนักการสั่นของขอบใบ/ปลายใบ |
| A | เฟสสุ่มต่อใบ |

UV0 = Texture, UV1.x = น้ำหนักสีใบอ่อน (ใช้กับใบหน่อ)
ตั้ง `Mesh Compression = Off` ตอน import (สคริปต์ Setup ตั้งให้แล้ว)

## ปรับแต่งใน Material

- **Trunk Bend / Leaf Sway / Leaf Flutter** — ความแรงของลมแต่ละส่วน
- **Wind** — เปิด/ปิดลมต่อ Material
- **Leaf Translucency** — แสงทะลุใบเมื่อย้อนแสง (ค่าเริ่มต้น 0.12)
- **Young Leaf Tint** — สีใบอ่อนของหน่อ

## หมายเหตุ

- Shader เขียนด้วยมือสำหรับ URP 17 (Unity 6) และตรวจ syntax ด้วยเครื่องมือภายนอกแล้ว แต่**ยังไม่เคยรันใน Unity จริง**
  ถ้ามี error ที่ Console หรือสีเพี้ยน ส่งข้อความ error / ภาพ Inspector ของ Material ใบมาได้
- WindZone ของ Unity ใช้กับ shader ทั่วไปไม่ได้โดยตรง จึงต้องมี `BananaWindController` เป็นตัวกลาง
- ใช้ WindZone แบบ **Directional** เป็นหลัก (ทิศลม = แกน Z สีน้ำเงินของ WindZone)
- ถ้าอยากเปลี่ยนสีผลสุกให้เหลืองขึ้น/น้ำตาลขึ้น แก้ที่ `banana_fruit_ripe_albedo.png`
