# บทเรียน Blender → Unity สำหรับ props

ตรวจพบจากงาน iPhone DUO วันที่ 2026-10-17 (Blender 5.2 / Unity 6000.3.8f1 URP 17.3.0) ใช้เมื่อเกี่ยวข้องกับงาน ไม่บังคับค่าของ asset นี้กับชิ้นอื่น

## ชิ้นพับและรูปทรงไม่สมมาตร

- กำหนดระนาบจอ แกน hinge และระยะ offset เป็นค่าที่วัดได้ การใช้ pivot ตรงกึ่งกลางความหนาอาจทำให้จอสองฝั่งชนกันเมื่อพับสนิท ตรวจ glass clearance และ chassis clearance แยกกัน
- การตรวจว่าบานซ้าย/ขวาไม่ชนยังไม่พอ ต้องตรวจว่ามีชิ้นกลไกเชื่อมต่ออยู่ตลอดช่วงหมุนด้วย งานนี้พบ spine อยู่ห่างจากบานเมื่อพับครึ่ง จึงเพิ่ม fixed/moving links ที่สลับระยะตามแนวแกนและ end pins
- เมื่อย้ายจอหรือฝาครอบไปอีกฝั่งของรูปทรงแบบ outer-corner/hinge-corner อย่าปรับเฉพาะตำแหน่ง ต้องสลับรัศมีมุมด้วย การ flip UV เปลี่ยนทิศภาพเท่านั้น ไม่แก้ silhouette หรือมุม geometry
- ตรวจจอและสัญลักษณ์จากด้านที่ผู้ใช้เห็นจริง ทั้งก่อนและหลังพับ การมองผิวด้านหลังกับการหมุนบาน 180° อาจกลับซ้าย/ขวา ให้แก้ UV หรือรูปทรงโดยไม่ใช้ negative object scale ที่ไม่ได้ตรวจ normals

## ส่งออกและตรวจใน Unity

- ตรวจ FBX ที่ Unity นำเข้าจริง: metre scale, top direction, display normal, hinge local axis, rest quaternion, ชื่อ hierarchy และ parent ของบานเคลื่อนที่ ขนาด bounding box ที่ถูกไม่ได้ยืนยันว่าภาพบนจอตั้งตรง
- ถ้าต้องปรับแกนรวม ให้ใช้ wrapper ที่ระดับบนและคง descendant transforms/pivots ของ source แล้วหาแกนหมุนใน local space ของ hinge จากผลนำเข้าจริง อย่าสมมุติว่า Blender Y จะเป็น Unity local Y เสมอ
- ใช้ข้อมูล top direction ของ leaf ร่วมกับ display normal ในการปรับแกน และเลือกเครื่องหมายการหมุนจากด้านที่บานเคลื่อนที่อยู่ งานนี้ตรวจเฉพาะ normal/bounds แล้วเคยได้จอกลับหัว แม้ตัวเลขสเกลจะผ่าน
- แยก nominal chassis dimensions จาก bounding box รวมปุ่ม กล้อง และ hinge links รายงานทั้งสองเมื่อมีผลต่อการเทียบ ref
- ตรวจ normals, UV0, material slots, shader errors และหลายมุมพับหลัง reimport การ sample transform ไม่ใช่ physics simulation หรือหลักฐานว่าไม่มี triangle intersection ทุกคู่
- Material procedural ของ Blender ไม่เดินทางผ่าน FBX โดยอัตโนมัติ ใช้ texture ที่ส่งออกจริงและสร้าง/remap วัสดุของ render pipeline เป้าหมาย ตรวจภาพที่เรนเดอร์จาก Unity ด้วย
- สำหรับจอ OLED ให้ตรวจ emission หลัง SaveAndReimport ด้วย งานนี้ URP 17.3 `BaseShaderGUI` ปิด `_EMISSION` ตอน material validation เมื่อใช้ `globalIlluminationFlags=None` จึงต้องตั้ง flag ที่ validator รองรับ (ทดสอบแล้วว่า `BakedEmissive` คง keyword/map ไว้ แม้ renderer ไม่ได้ static) อย่าเชื่อการเรียก EnableKeyword ก่อน import เพียงอย่างเดียว และอย่าให้ specular highlight บัง UI ในมุมพับ
- UV0 ที่ซ้อนสำหรับสีพื้น/texture หน้าจอไม่ใช่ lightmap atlas ถ้า prop ต้องขยับและไม่ได้อบแสง ไม่จำเป็นต้องบังคับสร้าง UV2; ระบุข้อจำกัดไว้
- เก็บ source ที่แยกชิ้นไว้ได้ แล้วรวมชิ้น rigid ตาม moving leaf/material สำหรับเกม ลด renderer count โดยไม่ทำลาย hinge hierarchy พิจารณา LOD จากระยะมองจริง; อย่า decimate จอบาง/ช่องพอร์ตอย่างรุนแรงทั้งชิ้น

## CLI และไฟล์งาน

- ถ้า Unity หลักเปิดอยู่ ให้พิจารณาใช้โปรเจกต์ตรวจแยกภายใน workspace ที่มี editor/package versions ตรงกัน นำเข้า สร้าง prefab และตรวจที่นั่น แล้วคัดลอกเฉพาะ generated assets และ .meta กลับ ห้ามคัดลอก FBX/texture เก่าจาก staging ทับ source ที่เพิ่งแก้
- ให้โปรเจกต์ตรวจใช้ color space ตรงกับเกมหลักด้วย งานนี้ stage ค่าเริ่มต้น Gamma ต่างจากเกม Linear และการอ่าน HDR render target แบบ linear เป็น PNG โดยไม่แปลง sRGB ทำให้จอดูมืด จึงปรับ stage ตาม PlayerSettings และใช้ sRGB target สำหรับภาพตรวจ
- หลังเปลี่ยน URP pipeline ใน batch preview ให้ warm/render ก่อนอ่านภาพจริง งานนี้ render แรกยังได้แสงค้างที่สว่างเกินไป จึง render สองครั้งแล้วตรวจผลภาพ; อย่าถือว่าภาพแรกยืนยันค่าวัสดุสุดท้าย
- การสร้าง preview scene ต้องเหมาะกับ editor context: งานนี้ additive NewScene ติด guard เมื่อฉากเดิมยัง untitled และ SceneManager.CreateScene ใช้ไม่ได้ในบริบท editor ที่ทดสอบ จึงใช้ NewScene แบบ Single เฉพาะโปรเจกต์ batch แยก ห้ามนำวิธีแทนฉากนี้ไปใช้กับ Unity หลักของผู้ใช้โดยไม่รักษางานเดิม
- บน Windows การรัน Unity ใน sandbox อาจค้างที่ licensing แม้ติดตั้ง editor แล้ว ให้ตรวจ log และใช้การเข้าถึง licensed host ตามสิทธิ์ที่มี ก่อนหยุด process ต้องยืนยัน PID และ projectPath ว่าเป็นงานตรวจของตัวเอง ห้ามปิด Unity ของผู้ใช้เพื่อแก้ปัญหานี้
- ตัวกรอง process ใน sandbox อาจมองไม่เห็น process ของ host จึงใช้ยืนยันว่า Unity ไม่ได้เปิดอยู่ไม่ได้
- Python ที่พิมพ์ path ภาษาไทยผ่าน Windows pipe อาจเจอ encoding cp1252; เมื่อต้องพิมพ์ path ให้ตั้ง stdout เป็น UTF-8 หรือเขียน JSON/log UTF-8 โดยตรง

## ภาพตรวจโมเดลขนาดเล็ก

- ไฟ studio ที่เหมาะกับวัตถุขนาดเมตรอาจทำให้โทรศัพท์ขนาดจริง overexpose มาก ตรวจสีดำ กระจก และข้อมูลจอจากภาพจริง แล้วปรับกำลังไฟตามขนาด/ระยะ ไม่แก้วัสดุเพื่อชดเชยแสงที่ผิด
- กล้องที่ใช้ world Z-up จะทำให้ prop ซึ่งวาง top ตาม Blender Y เอียงหรือกลับหัวในบางมุม ให้กำหนด up vector ตาม asset และ fit framing ใหม่แต่ละท่าพับ
- พื้น studio อาจบังการตรวจด้านหลัง/ใต้โมเดล ซ่อนพื้นระหว่างตรวจด้านนั้น และอย่าตีความภาพว่างว่า geometry หาย
