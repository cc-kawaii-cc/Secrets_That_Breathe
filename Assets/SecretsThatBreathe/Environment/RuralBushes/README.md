# พุ่มไม้สำหรับฉากชนบทไทย

ชุดพุ่มไม้ทรงธรรมชาติ 3 แบบ อิงรูปทรงข่อย สาบเสือ และผกากรอง
ปรับรุ่นล่าสุดเป็นทรงโดมเตี้ยกว้าง ใบแน่นถึงด้านล่างและเติมใบชั้นในตามภาพอ้างอิง
เป็นโมเดลเกมที่ลดรายละเอียดใบ/ดอก ไม่ใช่แบบจำลองพฤกษศาสตร์อย่างละเอียด

| Prefab | สูงโดยประมาณ | LOD0 สามเหลี่ยม | LOD1 สามเหลี่ยม |
|---|---:|---:|---:|
| Prefabs/Khoi_Bush.prefab — พุ่มข่อย | 0.74 ม. | 7,152 | 2,448 |
| Prefabs/SiamWeed_Bush.prefab — พุ่มสาบเสือ | 0.99 ม. | 6,431 | 2,109 |
| Prefabs/Lantana_Bush.prefab — พุ่มผกากรอง | 0.84 ม. | 7,305 | 2,466 |

ตัวเลขเป็นหลัง triangulation แต่ละ LOD มี MeshRenderer เดียวและ Material เดียว
ใบแต่ละใบใช้ 2 triangles เพิ่มจำนวนใบแทนการเพิ่มรายละเอียดบนใบเดียว
ใช้ชื่อ Prefab/mesh และ GUID เดิม โดยปรับค่า bounds ของ LOD และความสูงสำหรับลมให้เข้าทรงใหม่
ขนาด scale 1, pivot อยู่ระดับพื้นตรงกลางโคนพุ่ม และมี LODGroup เปลี่ยนตามระยะกล้อง
ไม่มี Collider เพื่อให้ใช้เป็นพืชริมทางได้โดยไม่ขวางการเดิน

## วิธีใช้ Wind Zone

1. ลาก Prefab พุ่มไม้ลงฉาก Unity URP ของโปรเจกต์
2. ใช้ Wind Zone ที่มีอยู่ หรือเพิ่ม Prefabs/RuralBush_WindZone.prefab
3. เริ่มที่ Main = 1, Turbulence = 0.45, Pulse Magnitude = 0.25, Pulse Frequency = 0.25
4. หมุน Wind Zone เพื่อเปลี่ยนทิศลม ปรับ Main เพื่อเปลี่ยนแรงลม

Rural Bush Wind บน Prefab ตรวจหา Wind Zone ในฉากเดียวกันอัตโนมัติ
ช่อง Wind Zone ใช้เลือกโซนเฉพาะต้นได้ รองรับ Directional และ Spherical
ปรับ Wind Multiplier, Sway Distance และ Leaf Flutter แยกแต่ละพุ่มได้
Preview In Edit Mode เปิดไว้เพื่อดูผลใน Scene View
เมื่อไม่มี Wind Zone หรือ Main/Turbulence เป็นศูนย์ จะไม่มีการไหวเอง
การปิด component จะคืนการขยับและ bounds กลับสู่สภาพเดิม

ใช้การขยับ vertex บน GPU: โคนต้นถูกตรึง กิ่งเอนตามความสูง
UV ปลายใบใช้ควบคุม flutter ขณะที่กิ่งใช้ค่า flutter เป็นศูนย์
การขยับใช้ร่วมกันใน forward/deferred, shadow, depth, normal และ motion-vector passes
ระบบ shader/ตัวอ่านลมนี้แยกจากต้นไม้เดิม ไม่ได้แก้ไฟล์ Mango/Mayom/RoadsideGrass

## ฉากตัวอย่างและไฟล์ต้นฉบับ

- Demo/RuralBushes_WindDemo.unity — พุ่มไม้ทั้งสามพร้อม Wind Zone และไฟตัวอย่าง
- Models — FBX แบบแยก LOD
- Meshes — เมชที่ปรับแกน/สเกลสำหรับ Unity
- Textures — atlas 256 × 512 ต่อแบบ สร้างสำหรับงานนี้ ไม่มี alpha-card รอบใบ
- Shaders — URP shader พร้อมใบสองด้าน; โค้ด URP adaptation มี Unity-LICENSE.md กำกับ
- ArtSource/RuralBushes/RuralBushes.blend — ต้นฉบับ Blender ทั้งชุด
- Tools/RuralBushes — สูตรสร้างโมเดล และตัวสร้าง/ตรวจ Prefab

พุ่มไม้เป็นวัตถุขยับ ใช้ Light Probes และแสง realtime ตามฉาก
ไม่ได้ทำเป็น Contribute GI/Lightmap Static และไม่ได้รับรองการวางเป็น Terrain Detail
UV สำหรับสีซ้อนกันโดยตั้งใจ จึงไม่ใช่ lightmap UV

## ตรวจแล้ว

- สัดส่วน/ขนาด/pivot และหน้าใบสองด้านจากโมเดลจริงกับภาพใน Blender/Unity
- LOD0/LOD1 ครบ จำนวน triangle และระนาบใบที่ตั้งใจเป็น surface
- Wind Main, pulse, spherical radius/direction, inactive zone และปิด component
- ภาพ GPU ที่เวลาแตกต่างกันขณะมีลมเปลี่ยนจริง; ค่าแรงลมศูนย์ให้ภาพตรงกัน
- รายงานรายละเอียดอยู่ใน UnityVerification.txt และ ArtSource/RuralBushes

ยังไม่ได้วัดประสิทธิภาพบนเครื่องเป้าหมาย ไม่ได้ทดสอบร่วมกับฉากเกมหลัก
และไม่ได้ Bake แสงของฉากจริง

## แหล่งอ้างอิงรูปทรง

- ข่อย: https://www.zoothailand.org/plant_view.php?c_id=&detail_id=32
- ผกากรอง: https://www.nparks.gov.sg/florafaunaweb/flora/2/1/2178
- สาบเสือ (รายการพืชพื้นล่าง/ไม้ล้มลุกในเอกสารกรมอุทยานฯ): https://readcard.dnp.go.th/km/docs/64225.pdf
