# โคมตะแกรงติดลอย T8 สองหลอด

โมเดลสร้างจากภาพอ้างอิง: กรอบสีขาว ตะแกรงอะลูมิเนียมพับ 12 ซี่ รีเฟลกเตอร์โค้ง หลอดสองหลอดพร้อมขั้วและน็อต ขนาดประมาณ 1.24 × 0.31 × 0.08 เมตร

ลาก `Prefabs/LouveredLight_T8_2x36W.prefab` เข้า Scene ได้เลย จุดกำเนิดอยู่กลางด้านบนสำหรับแนบเพดาน ตัวโคมหันลงแกน -Y และด้านยาวอยู่แกน Z

แบบปิดไฟ: ใช้ `Prefabs/LouveredLight_T8_2x36W_Off.prefab` หลอดใช้วัสดุ `Tube_Off.mat` ที่ไม่มี Emission และปิด Light ทุกดวง รูปทรงและ Collider เหมือนโคมเดิม สร้างใหม่ได้ที่ `Tools > Louvered Light > Create Off Prefab`

วัสดุ `Materials/Tube_Emission.mat` ใช้ URP/Lit และเปิด Emission สีขาวอมฟ้า ค่า HDR สูงสุด 4 ปรับสีและความสว่างที่ Emission ใน Inspector ได้ หากต้องการแสงฟุ้ง ให้เปิด Bloom และ Post Processing ของกล้อง ตัวอย่างอยู่ใน `Demo/LouveredLight_Preview.unity`

ลูก `Downward Light (optional)` เป็น Spot Light ส่องพื้น ปิดได้เมื่อใช้แสงจากการ Bake หรือไฟของฉากเอง Emission ทำให้ผิวหลอดสว่าง ส่วนแสงที่ตกบนวัตถุอื่นใช้ Light หรือ baked GI

โมเดล FBX อยู่ใน `Models/` และต้นฉบับ Blender อยู่ใน `ArtSource/LouveredLight/` ที่รากโปรเจกต์ รวมชิ้นส่วนตามวัสดุเป็น 6 กลุ่ม มี UV สำหรับ lightmap และ Box Collider ครอบตัวโคม

เมนู `Tools > Louvered Light > Rebuild Prefab` สร้างวัสดุและ Prefab ใหม่ (จะรีเซ็ตค่าปรับวัสดุและ Prefab ของโคมนี้)
