# Mango Tree (ต้นมะม่วง) — Unity 6000.3.8f1 / URP

4 แบบ: `MangoTree_NoFruit` (ไม่มีผล), `MangoTree_NearlyRipe` (ผลเกือบสุก เขียวอมเหลือง), `MangoTree_Ripe` (ผลสุก เหลือง), `MangoTree_Mixed` (สุกปนเกือบสุก)

| Variant | LOD0 | LOD1 | LOD2 |
|---|---|---|---|
| NoFruit | 3,668 tris | 828 | 364 |
| NearlyRipe / Ripe | 4,772 | 1,332 | 556 |
| Mixed | 4,956 | 1,416 | 588 |

ขนาดต้น ≈ 5.5 ม. สูง × 5 ม. กว้าง (หน่วยเมตร, pivot ที่โคนต้น)

## วิธีใช้
1. วางโฟลเดอร์ `MangoTree` ไว้ใน `Assets/` แล้วคลิกหน้าต่าง Unity ให้ import
2. ระบบจะรัน Setup อัตโนมัติครั้งแรก (หรือเมนู **Tools > Mango Tree > 1) Setup Materials & Prefabs**)
   - ตั้งค่า texture (normal map, alpha cutout, wrap), สร้าง Material, ผูก Material เข้า FBX, สร้าง Prefab + LOD Group
   - Prefab ที่มีอยู่แล้วจะไม่ถูกเขียนทับ
3. **Tools > Mango Tree > 2) Create Demo Scene** — ฉากตัวอย่าง 4 ต้น + Wind Zone
4. ใช้กับ Terrain: เลือก Terrain แล้ว **Tools > Mango Tree > 5) Add Tree Prefabs to Selected Terrain** จากนั้นใช้ Paint Trees

## Wind Zone
- ใช้ shader `MangoTree/Wind URP` (อิง URP Lit — เงา, Forward+, Deferred, LOD cross-fade, SRP Batcher)
- สคริปต์ `MangoTreeWind` สร้างตัวเองอัตโนมัติ อ่านค่าจาก **Wind Zone** ในฉาก (Main, Turbulence, Pulse Magnitude, Pulse Frequency, ทิศทาง)
- ลำต้นนิ่ง กิ่งบน/ปลายพุ่มแกว่งมากกว่า ใบสั่นตาม Turbulence ผลแกว่งไปกับกิ่ง
- ปรับต่อ Material: `Bend`, `Flutter`, `Tree Height`, `Crown Radius` (Bend ต้องเท่ากันทุก Material ของต้นเดียวกัน)
- ถ้าไม่มี Wind Zone ในฉาก ต้นไม้จะนิ่ง

## LOD
LOD Group 3 ระดับ (0.22 / 0.07 / 0.02 ของความสูงหน้าจอ) เปิด Cross Fade — LOD2 ไม่ตกเงา; ต่ำกว่า 2% จะถูก cull

## Texture
- `T_Mango_Bark_Albedo/Normal` — เปลือกลำต้น (tile ได้) จากภาพอ้างอิงที่ 3
- `T_Mango_Leaf_Albedo/Normal` — atlas ใบ (ใบแก่/ใบอ่อน/กิ่งใบ) โทนสีตามภาพที่ 5–6
- `T_Mango_Fruit_Ripe_Albedo`, `T_Mango_Fruit_NearlyRipe_Albedo` — สีผลตามภาพที่ 4 (สุก) และภาพที่ 1 (เกือบสุก)

## หมายเหตุ
- ใบเป็น card ที่ขยายใหญ่กว่าใบจริงประมาณ 1.4–2 เท่า เพื่อให้พุ่มเต็มภายในงบ 3,000–5,000 polygon
- Terrain tree ใน Unity ใช้ wind shader นี้ได้ตามปกติ (ไม่ใช้ SpeedTree wind)
