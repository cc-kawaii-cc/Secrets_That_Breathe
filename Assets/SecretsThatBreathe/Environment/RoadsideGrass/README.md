# Roadside Grass — กอหญ้าริมทางใบเรียว

โมเดลสร้างใหม่จากลักษณะภาพอ้างอิง: ใบแคบยาว ปลายแหลม โค้งแผ่ออกจากโคน มีใบเขียวและใบแห้งปนกัน ไม่ได้ยืนยันชื่อพันธุ์พืชจากภาพ

สำหรับ **Unity 6000.3.8f1 / URP 17.3.0** ใช้ Paint Details บน Terrain

| Prefab | ลักษณะ | สูงโดยประมาณ | Triangles / กอ |
|---|---|---|---|
| `RoadsideGrass_Green` | กอเขียวสดแน่น มีใบแห้งเล็กน้อย | 51 ซม. | 1,120 |
| `RoadsideGrass_Mixed` | เขียวปนใบแห้ง เหมาะกับริมถนน | 48 ซม. | 896 |
| `RoadsideGrass_Lite` | กอเตี้ยเบา เหมาะกับพื้นที่กว้าง | 35 ซม. | 320 |

## วิธี Paint บน Terrain

1. เลือก Terrain ใน Hierarchy
2. เปิด **Paint Terrain → Paint Details**
3. **Edit Details → Add Detail Mesh**
4. ใส่ Prefab จากโฟลเดอร์ `Prefabs` ในช่อง **Detail Prefab**
5. เปิด **Use GPU Instancing** เพื่อใช้วัสดุและลมของโมเดลนี้
6. เริ่ม Width / Height Min **0.8**, Max **1.2** (สเกลของกอสำหรับ instanced mesh)
7. เริ่ม Paint ด้วย Target Strength ต่ำ แล้วเพิ่มทีละน้อย ผสม Green กับ Mixed เพื่อให้สีไม่สม่ำเสมอ

หรือเลือก Terrain แล้วใช้เมนู **Tools → Roadside Grass → Add to Selected Terrain** เพื่อเพิ่มทั้งสามแบบเป็นแปรงพร้อมค่าเริ่มต้น เมนูนี้ไม่ Paint ให้เองและกด Undo ได้

Terrain Settings: เริ่ม Detail Distance ประมาณ **35–60 เมตร** และใช้ Lite เมื่อ Paint พื้นที่กว้าง ความหนาแน่นที่เหมาะสมต้องปรับตามเครื่องเป้าหมาย ยังไม่ได้ benchmark เกมทั้งโปรเจกต์

## วัสดุ / ลม

ใช้วัสดุร่วม `Materials/RoadsideGrass_Wind.mat` สีมาจาก vertex color ของใบ ไม่ใช้ภาพ alpha card จึงเห็นใบจากทุกมุม โคนมีสีเข้มกว่าและลมไม่ขยับโคน

- **Tint**: ปรับสีทั้งชุด
- **Wind Strength**: ระยะไหวเป็นเมตร เริ่มที่ 0.028 ตั้ง 0 เพื่อหยุดลม
- **Wind Speed**: ความเร็วลม
- **Wind Direction XZ**: ทิศลม

ลมคำนวณใน Shader จึงใช้งานกับ Terrain GPU Instancing ได้โดยไม่ต้องมี MonoBehaviour หรือ Wind Zone วัสดุนี้ไม่รับค่า Wind Zone อัตโนมัติ Prefab ปิดการ cast shadow เพื่อลดงานเรนเดอร์ แต่ยังรับเงาจากฉาก

## ไฟล์

- `Prefabs/`: พร้อมใช้กับ Terrain Details; แต่ละ Prefab มี MeshFilter และ MeshRenderer ที่ root, 1 mesh, 1 material
- `Models/`: FBX ต้นฉบับ สเกลเมตร แกน Y ขึ้น
- `Meshes/`: mesh สำหรับ Prefab พร้อมขยาย bounds เผื่อใบไหว
- `Shaders/`: URP shader สำหรับสีใบ แสง เงา depth และ normals
- `Demo/RoadsideGrass_TerrainPreview.unity`: ฉาก Terrain ตัวอย่างที่มีหญ้า Paint ไว้แล้ว
- `ArtSource/RoadsideGrass/` ในรากโปรเจกต์: Blender source, unitypackage และภาพ preview จาก Unity

ไม่มี Collider หรือ LODGroup บนกอ Terrain Detail ใช้รุ่น Lite ร่วมกับระยะวาดเมื่อเน้นประสิทธิภาพ

เมนู **Tools → Roadside Grass → Rebuild Prefabs** สร้าง Prefab/Material/Mesh ใหม่จาก FBX ของชุดนี้และรีเซ็ตค่าของวัสดุชุดนี้เป็นค่าเริ่มต้น

คู่มือ Unity: https://docs.unity3d.com/6000.3/Documentation/Manual/terrain-Grass.html
